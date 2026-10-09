using NativeCompressions;
using SerializerFoundation;
using static MessagePack.MessagePackPrimitives;

namespace MessagePack;

// The shared half of the two v3 envelope processors: both transparently read BOTH codes (and pass non-enveloped
// messages through, as v3 did), and both guard the declared lengths the same way. Internal so that the only public
// LZ4 types are Lz4FrameProcessor and the two obsolete v3 envelopes.
internal static class Lz4BlockEnvelope
{
    /// <summary>Messages below this many bytes are written without an envelope.</summary>
    public const int CompressionThreshold = 64;

    /// <summary>
    /// Default cap on the declared decompressed size of one message: 64MB, matching v3's
    /// MessagePackSecurity.UntrustedData default and <see cref="MessagePackSerializerOptions.MaxBufferedMessageSize"/>'s default.
    /// </summary>
    public const long DefaultMaxDecompressedSize = 64 * 1024 * 1024;

    // LZ4 cannot produce more than 255 output bytes per compressed byte (each match-length
    // extension byte emits at most 255), so a declared length above compressed * 255 is
    // provably a lie — reject it BEFORE renting from it, the same doctrine as the
    // str/bin/ext allocation-bomb guard in ReadBufferExtensions.
    const long MaxExpansionPerCompressedByte = 255;

    public static long ValidateMaxDecompressedSize(long maxDecompressedSize)
    {
        if (maxDecompressedSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDecompressedSize));
        }
        return maxDecompressedSize;
    }

    static void GuardDeclaredLength(int declaredLength, int compressedLength, long totalDeclared, long maxDecompressedSize)
    {
        if (declaredLength > compressedLength * MaxExpansionPerCompressedByte)
        {
            Lz4Throws.ImplausibleDeclaredLength(declaredLength, compressedLength);
        }
        if (totalDeclared > maxDecompressedSize)
        {
            Lz4Throws.DeclaredLengthExceedsMaximum(totalDeclared, maxDecompressedSize);
        }
    }

    // ---- shared read side: both processors transparently read both codes ----

    public static bool TryDecode(ReadOnlySpan<byte> source, long maxDecompressedSize, out DecodedMessage message)
    {

        message = default;

        // ext 99: [0xd2 uncompressedLength][lz4 bytes]
        if (TryReadExtHeader(source, out var typeCode, out var dataLength, out var tokenSize) == DecodeResult.Success)
        {
            if (typeCode != ThisLibraryExtensionTypeCodes.Lz4Block)
            {
                return false; // user-data ext: not our envelope
            }
            if (dataLength > source.Length - tokenSize)
            {
                Lz4Throws.InvalidEnvelope(); // header claims more payload than the message holds
            }
            var data = source.Slice(tokenSize, dataLength);
            if (TryReadInt32(data, out var uncompressedLength, out var intSize) != DecodeResult.Success || uncompressedLength < 0)
            {
                Lz4Throws.InvalidEnvelope();
            }
            var lz4 = data.Slice(intSize);
            GuardDeclaredLength(uncompressedLength, lz4.Length, uncompressedLength, maxDecompressedSize);
            message = new DecodedMessage(DecodeBlock(lz4, uncompressedLength, out var rented), new RentedSingleOwner(rented));
            return true;
        }

        // ext 98 inside an array: [array n+1][ext 98: sizes][bin block]...
        if (TryReadArrayHeader(source, out var count, out tokenSize) == DecodeResult.Success && count >= 1)
        {
            var rest = source.Slice(tokenSize);
            if (TryReadExtHeader(rest, out typeCode, out var sizesLength, out var extSize) != DecodeResult.Success || typeCode != ThisLibraryExtensionTypeCodes.Lz4BlockArray)
            {
                return false; // a perfectly ordinary msgpack array message
            }

            if (sizesLength > rest.Length - extSize)
            {
                Lz4Throws.InvalidEnvelope(); // header claims more sizes than the message holds
            }
            var blockCount = count - 1;
            var sizes = rest.Slice(extSize, sizesLength);
            var blocks = rest.Slice(extSize + sizesLength);
            // The array header's count is attacker-controlled; sizing the per-block owner
            // array from it directly is an allocation bomb (array32 can claim ~4B blocks =
            // a 32GB pointer array) reached BEFORE the loop rejects the payload. The sizes
            // region must hold blockCount length-prefixes of >= 1 byte each, so a blockCount
            // past sizesLength is provably a lie - reject it before renting from it, the same
            // count-vs-BytesRemaining doctrine as the str/bin/ext guard in ReadBufferExtensions.
            if (blockCount > sizesLength)
            {
                Lz4Throws.InvalidEnvelope();
            }
            var rentedBlocks = new byte[]?[blockCount];
            try
            {
                Lz4DecodedSegment? first = null, last = null;
                long totalDeclared = 0;
                for (int i = 0; i < blockCount; i++)
                {
                    if (TryReadInt32(sizes, out var uncompressedLength, out var intSize) != DecodeResult.Success || uncompressedLength < 0)
                    {
                        Lz4Throws.InvalidEnvelope();
                    }
                    sizes = sizes.Slice(intSize);

                    if (TryReadBinHeader(blocks, out var binLength, out var binSize) != DecodeResult.Success || binLength > blocks.Length - binSize)
                    {
                        Lz4Throws.InvalidEnvelope();
                    }
                    totalDeclared += uncompressedLength;
                    GuardDeclaredLength(uncompressedLength, binLength, totalDeclared, maxDecompressedSize);
                    var memory = DecodeBlockMemory(blocks.Slice(binSize, binLength), uncompressedLength, out rentedBlocks[i]);
                    blocks = blocks.Slice(binSize + binLength);

                    var segment = new Lz4DecodedSegment(memory, last);
                    first ??= segment;
                    last = segment;
                }

                var sequence = first == null
                    ? ReadOnlySequence<byte>.Empty
                    : new ReadOnlySequence<byte>(first, 0, last!, last!.Memory.Length);
                message = new DecodedMessage(sequence, new RentedManyOwner(rentedBlocks));
                return true;
            }
            catch
            {
                foreach (var r in rentedBlocks)
                {
                    if (r != null) ArrayPool<byte>.Shared.Return(r);
                }
                throw;
            }
        }

        return false;
    }

    public static bool TryDecode(in ReadOnlySequence<byte> source, long maxDecompressedSize, out DecodedMessage message)
    {
        if (source.IsSingleSegment)
        {
            return TryDecode(source.FirstSpan, maxDecompressedSize, out message);
        }

        // identify non-envelopes from a stitched header prefix first: passthrough input
        // must never pay a whole-message flatten just to be recognized
        Span<byte> prefix = stackalloc byte[MaxEnvelopeHeaderLength];
        var prefixLength = (int)Math.Min(source.Length, MaxEnvelopeHeaderLength);
        source.Slice(0, prefixLength).CopyTo(prefix);
        if (!IsEnvelopeHeader(prefix.Slice(0, prefixLength)))
        {
            message = default;
            return false;
        }

        // our envelope: flatten and reuse the span logic (LZ4 block decompression needs
        // contiguous compressed bytes anyway; per-block stitching for ext 98 could avoid
        // the whole-message flatten, deferred until demanded)
        var length = checked((int)source.Length);
        var flat = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            source.CopyTo(flat);
            return TryDecode(flat.AsSpan(0, length), maxDecompressedSize, out message);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(flat);
        }
    }

    // array32 header (5) + ext32 header (6): a prefix this long always yields a
    // definitive envelope verdict, and a shorter prefix is the whole message
    const int MaxEnvelopeHeaderLength = 11;

    static bool IsEnvelopeHeader(ReadOnlySpan<byte> prefix)
    {
        if (TryReadExtHeader(prefix, out var typeCode, out _, out var tokenSize) == DecodeResult.Success)
        {
            return typeCode == ThisLibraryExtensionTypeCodes.Lz4Block;
        }
        return TryReadArrayHeader(prefix, out var count, out tokenSize) == DecodeResult.Success
            && count >= 1
            && TryReadExtHeader(prefix.Slice(tokenSize), out typeCode, out _, out _) == DecodeResult.Success
            && typeCode == ThisLibraryExtensionTypeCodes.Lz4BlockArray;
    }

    static ReadOnlySequence<byte> DecodeBlock(ReadOnlySpan<byte> lz4, int uncompressedLength, out byte[]? rented)
        => new(DecodeBlockMemory(lz4, uncompressedLength, out rented));

    static ReadOnlyMemory<byte> DecodeBlockMemory(ReadOnlySpan<byte> lz4, int uncompressedLength, out byte[]? rented)
    {
        rented = ArrayPool<byte>.Shared.Rent(uncompressedLength);
        int decoded;
        try
        {
            decoded = LZ4.Block.Decompress(lz4, rented.AsSpan(0, uncompressedLength));
        }
        catch (LZ4Exception)
        {
            // the native binding throws on malformed input; surface it as the same
            // domain exception as any other envelope corruption
            decoded = -1;
        }
        if (decoded != uncompressedLength)
        {
            // return before throwing: no DecodedMessage owner exists yet to release this,
            // and null the out slot so the BlockArray catch does not return it a second time
            ArrayPool<byte>.Shared.Return(rented);
            rented = null;
            Lz4Throws.InvalidEnvelope();
        }
        return rented.AsMemory(0, uncompressedLength);
    }

    sealed class Lz4DecodedSegment : ReadOnlySequenceSegment<byte>
    {
        public Lz4DecodedSegment(ReadOnlyMemory<byte> memory, Lz4DecodedSegment? previous)
        {
            Memory = memory;
            if (previous != null)
            {
                RunningIndex = previous.RunningIndex + previous.Memory.Length;
                previous.Next = this;
            }
        }
    }

    // DecodedMessage owners: release exactly once even if the message struct is copied
    // and disposed twice, by taking the pooled references atomically on the first call.
    // Dispose is not contractually thread-safe, but the failure mode of a racing double
    // dispose would be a double ArrayPool.Return - the same array handed to two renters,
    // silent aliasing - so the Interlocked hardening is worth its one instruction.

    sealed class RentedSingleOwner : IDisposable
    {
        byte[]? array;

        public RentedSingleOwner(byte[]? array)
        {
            this.array = array;
        }

        public void Dispose()
        {
            var taken = Interlocked.Exchange(ref array, null);
            if (taken != null)
            {
                ArrayPool<byte>.Shared.Return(taken);
            }
        }
    }

    sealed class RentedManyOwner : IDisposable
    {
        byte[]?[]? arrays;

        public RentedManyOwner(byte[]?[] arrays)
        {
            this.arrays = arrays;
        }

        public void Dispose()
        {
            var taken = Interlocked.Exchange(ref arrays, null);
            if (taken != null)
            {
                foreach (var array in taken)
                {
                    if (array != null)
                    {
                        ArrayPool<byte>.Shared.Return(array);
                    }
                }
            }
        }
    }
}

/// <summary>
/// Whole message as a single LZ4 block: ext 99 { int32 uncompressedLength, lz4 }, the MessagePack-CSharp v3 format.
/// Reads both v3 envelopes (ext 99 and ext 98) and passes non-enveloped messages through; messages below
/// <see cref="CompressionThreshold"/> bytes are written bare. Neither this nor <see cref="Lz4FrameProcessor"/> reads what the other writes.
/// </summary>
[Obsolete("v3 wire format (ext 99). Use WithLz4Frame for new data; Block stays for data shared with MessagePack-CSharp v3 readers and writers.")]
public sealed class Lz4BlockProcessor : MessagePackMessageProcessor
{
    /// <summary>Messages below this many bytes are written without an envelope.</summary>
    public const int CompressionThreshold = Lz4BlockEnvelope.CompressionThreshold;

    /// <summary>
    /// Default cap on the declared decompressed size of one message: 64MB, matching v3's
    /// MessagePackSecurity.UntrustedData default and <see cref="MessagePackSerializerOptions.MaxBufferedMessageSize"/>'s default.
    /// Construct a processor with a different value to raise or tighten it.
    /// </summary>
    public const long DefaultMaxDecompressedSize = Lz4BlockEnvelope.DefaultMaxDecompressedSize;

    /// <summary>
    /// Gets the cap on the total declared decompressed size of one message; a payload
    /// declaring more is rejected before any allocation (decompression-bomb guard, CWE-409).
    /// </summary>
    public long MaxDecompressedSize { get; }

    public Lz4BlockProcessor()
        : this(DefaultMaxDecompressedSize)
    {
    }

    /// <param name="maxDecompressedSize">Cap on the declared decompressed size of one message (decompression-bomb guard); the default is <see cref="DefaultMaxDecompressedSize"/>.</param>
    public Lz4BlockProcessor(long maxDecompressedSize)
    {
        MaxDecompressedSize = Lz4BlockEnvelope.ValidateMaxDecompressedSize(maxDecompressedSize);
    }

    /// <inheritdoc/>
    public override bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message)
        => Lz4BlockEnvelope.TryDecode(source, MaxDecompressedSize, out message);

    /// <inheritdoc/>
    public override bool TryDecode(in ReadOnlySequence<byte> source, out DecodedMessage message)
        => Lz4BlockEnvelope.TryDecode(in source, MaxDecompressedSize, out message);

    public override bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output)
    {
        if (message.Length < CompressionThreshold)
        {
            return false;
        }
        var (rented, start, length) = EncodeCore(ref message, checked((int)message.Length));
        try
        {
            output.Write(rented.AsSpan(start, length));
            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

#if NET9_0_OR_GREATER
    public override bool TryEncode<TWriteBuffer>(ref BufferSegments message, ref TWriteBuffer output)
    {
        if (message.Length < CompressionThreshold)
        {
            return false;
        }
        var (rented, start, length) = EncodeCore(ref message, checked((int)message.Length));
        try
        {
            rented.AsSpan(start, length).CopyTo(output.GetSpan(length));
            output.Advance(length);
            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
#endif

    const int MaxHeaderLength = 6 /* ext header */ + 5 /* forced int32 length prefix */;

    static (byte[] Rented, int Start, int Length) EncodeCore(ref BufferSegments message, int messageLength)
    {
        // a single block needs the uncompressed message contiguous; flatten segments into
        // a rented buffer (inherent to the whole-message mode, BlockArray avoids it)
        var flat = ArrayPool<byte>.Shared.Rent(messageLength);
        try
        {
            var offset = 0;
            while (message.TryGetNext(out var segment))
            {
                segment.CopyTo(flat.AsSpan(offset));
                offset += segment.Length;
            }

            // encode at the max-header offset, then right-align the header so envelope and
            // block end up contiguous: [start .. 11) header, [11 ..) lz4 bytes
            var rented = ArrayPool<byte>.Shared.Rent(MaxHeaderLength + LZ4.Block.GetMaxCompressedLength(messageLength));
            try
            {
                var lz4Length = LZ4.Block.Compress(flat.AsSpan(0, messageLength), rented.AsSpan(MaxHeaderLength));
                if (lz4Length <= 0) // native LZ4_compress_default reports failure as 0
                {
                    Lz4Throws.InvalidEnvelope(); // cannot happen with a GetMaxCompressedLength-sized target
                }

                Span<byte> header = stackalloc byte[MaxHeaderLength];
                var headerLength = UnsafeWriteExtHeader(ref header[0], ThisLibraryExtensionTypeCodes.Lz4Block, 5 + lz4Length);
                headerLength += UnsafeWriteForcedInt32(ref header[headerLength], messageLength);
                var start = MaxHeaderLength - headerLength;
                header.Slice(0, headerLength).CopyTo(rented.AsSpan(start));
                return (rented, start, headerLength + lz4Length);
            }
            catch
            {
                // the caller owns the array only once it is returned; a failing encode hands it back
                ArrayPool<byte>.Shared.Return(rented);
                throw;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(flat);
        }
    }
}

/// <summary>
/// One LZ4 block per write segment: [array n+1][ext 98: sizes][bin lz4]..., the MessagePack-CSharp v3 format.
/// Reads both v3 envelopes (ext 99 and ext 98) and passes non-enveloped messages through; messages below
/// <see cref="CompressionThreshold"/> bytes are written bare. Neither this nor <see cref="Lz4FrameProcessor"/> reads what the other writes.
/// </summary>
[Obsolete("v3 wire format (ext 98). Use WithLz4Frame for new data; BlockArray stays for data shared with MessagePack-CSharp v3 readers and writers.")]
public sealed class Lz4BlockArrayProcessor : MessagePackMessageProcessor
{
    /// <summary>Messages below this many bytes are written without an envelope.</summary>
    public const int CompressionThreshold = Lz4BlockEnvelope.CompressionThreshold;

    /// <summary>
    /// Default cap on the declared decompressed size of one message: 64MB, matching v3's
    /// MessagePackSecurity.UntrustedData default and <see cref="MessagePackSerializerOptions.MaxBufferedMessageSize"/>'s default.
    /// Construct a processor with a different value to raise or tighten it.
    /// </summary>
    public const long DefaultMaxDecompressedSize = Lz4BlockEnvelope.DefaultMaxDecompressedSize;

    /// <summary>
    /// Gets the cap on the total declared decompressed size of one message; a payload
    /// declaring more is rejected before any allocation (decompression-bomb guard, CWE-409).
    /// </summary>
    public long MaxDecompressedSize { get; }

    public Lz4BlockArrayProcessor()
        : this(DefaultMaxDecompressedSize)
    {
    }

    /// <param name="maxDecompressedSize">Cap on the total declared decompressed size of one message (decompression-bomb guard); the default is <see cref="DefaultMaxDecompressedSize"/>.</param>
    public Lz4BlockArrayProcessor(long maxDecompressedSize)
    {
        MaxDecompressedSize = Lz4BlockEnvelope.ValidateMaxDecompressedSize(maxDecompressedSize);
    }

    /// <inheritdoc/>
    public override bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message)
        => Lz4BlockEnvelope.TryDecode(source, MaxDecompressedSize, out message);

    /// <inheritdoc/>
    public override bool TryDecode(in ReadOnlySequence<byte> source, out DecodedMessage message)
        => Lz4BlockEnvelope.TryDecode(in source, MaxDecompressedSize, out message);

    public override bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output)
    {
        if (message.Length < CompressionThreshold)
        {
            return false;
        }
        var (rented, written) = EncodeCore(ref message);
        try
        {
            output.Write(rented.AsSpan(0, written));
            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

#if NET9_0_OR_GREATER
    public override bool TryEncode<TWriteBuffer>(ref BufferSegments message, ref TWriteBuffer output)
    {
        if (message.Length < CompressionThreshold)
        {
            return false;
        }
        var (rented, written) = EncodeCore(ref message);
        try
        {
            rented.AsSpan(0, written).CopyTo(output.GetSpan(written));
            output.Advance(written);
            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
#endif

    static (byte[] Rented, int Written) EncodeCore(ref BufferSegments message)
    {
        // the array and ext headers and the rented size depend on the segment count, which the view reports up front.
        // LZ4_COMPRESSBOUND is linear plus a constant (n + n/255 + 16), so the sum of the per-block bounds is at most
        // the bound of the whole message plus that constant (GetMaxCompressedLength(0)) once per extra block
        var blockCount = message.SegmentCount;
        var messageLength = checked((int)message.Length);
        long maxTotal = MaxArrayHeaderLength + 6 /* ext header */ + 5L * blockCount /* forced int32 sizes */
            + 5L * blockCount /* bin32 headers */ + LZ4.Block.GetMaxCompressedLength(messageLength)
            + (long)LZ4.Block.GetMaxCompressedLength(0) * Math.Max(blockCount - 1, 0);

        var rented = ArrayPool<byte>.Shared.Rent(checked((int)maxTotal));
        try
        {
            var span = rented.AsSpan();
            var offset = 0;

            // [array n+1][ext 98 { int32 sizes... }]: the sizes are forced int32, so each entry has a fixed slot
            // and is filled from the compress loop below instead of a walk of its own
            offset += UnsafeWriteArrayHeader(ref span[0], blockCount + 1);
            offset += UnsafeWriteExtHeader(ref span[offset], ThisLibraryExtensionTypeCodes.Lz4BlockArray, 5 * blockCount);
            var sizes = offset;
            offset += 5 * blockCount;

            // [bin lz4block]...
            while (message.TryGetNext(out var segment))
            {
                UnsafeWriteForcedInt32(ref span[sizes], segment.Length);
                sizes += 5;
                var lz4Length = LZ4.Block.Compress(segment, span.Slice(offset + 5));
                if (lz4Length <= 0) // native LZ4_compress_default reports failure as 0
                {
                    Lz4Throws.InvalidEnvelope();
                }
                // bin header is 2..5 bytes; the block was encoded at the max-header offset,
                // so shift it back over the gap when the header comes out shorter
                var binHeaderLength = UnsafeWriteBinHeader(ref span[offset], lz4Length);
                if (binHeaderLength != 5)
                {
                    span.Slice(offset + 5, lz4Length).CopyTo(span.Slice(offset + binHeaderLength));
                }
                offset += binHeaderLength + lz4Length;
            }

            return (rented, offset);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(rented); // see EncodeCore
            throw;
        }
    }
}
