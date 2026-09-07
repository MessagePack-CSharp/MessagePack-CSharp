// TODO: NativeCompressions.LZ4 is still incomplete
// this is a provisional implementation.
// I'll verify basic behavior first and then move on to finalizing the API, so this code is not yet at the stage to be evaluated.

using NativeCompressions;
using SerializerFoundation;
using static MessagePack.MessagePackPrimitives;

namespace MessagePack;

/// <summary>
/// v3-compatible LZ4 envelopes as <see cref="MessagePackMessageProcessor"/>s:
/// Block = whole message as one LZ4 block inside ext 99, BlockArray = one LZ4 block per
/// pooled write segment inside ext 98 (zero-copy on the uncompressed side — the
/// serializer's segments map 1:1 to blocks). Reading is transparent for BOTH codes and
/// passes non-enveloped messages through, matching MessagePack-CSharp semantics.
/// Messages smaller than <see cref="Lz4MessageProcessor.CompressionThreshold"/> are
/// written without an envelope (compression of tiny messages is a pure loss). Above the
/// threshold the envelope is ALWAYS written, even when LZ4 leaves the payload larger
/// (incompressible data costs ~0.4% literal overhead plus the header) — v3 parity:
/// whether the envelope appears depends only on the size threshold, never on the data
/// content. Readers on either side handle raw sub-threshold messages transparently.
/// Codec: NativeCompressions.LZ4 (native lz4 binding). Compressed bytes are
/// format-compatible with, but not byte-identical to, MessagePack-CSharp's K4os output —
/// the cross-read tests in Lz4Tests are the compatibility contract.
/// </summary>
public static class Lz4Compression
{
    public static MessagePackMessageProcessor Block { get; } = new Lz4BlockProcessor();
    public static MessagePackMessageProcessor BlockArray { get; } = new Lz4BlockArrayProcessor();
}

public static class Lz4MessagePackOptionsExtensions
{
    /// <summary>Options writing ext 99 (whole-message block); shares this instance's resolver.</summary>
    public static MessagePackSerializerOptions WithLz4Block(this MessagePackSerializerOptions options)
        => options with { MessageProcessor = Lz4Compression.Block };

    /// <summary>Options writing ext 99 with a custom decompression-bomb cap (see <see cref="Lz4MessageProcessor.MaxDecompressedSize"/>).</summary>
    public static MessagePackSerializerOptions WithLz4Block(this MessagePackSerializerOptions options, long maxDecompressedSize)
        => options with { MessageProcessor = new Lz4BlockProcessor(maxDecompressedSize) };

    /// <summary>Options writing ext 98 with a custom decompression-bomb cap (see <see cref="Lz4MessageProcessor.MaxDecompressedSize"/>).</summary>
    public static MessagePackSerializerOptions WithLz4BlockArray(this MessagePackSerializerOptions options, long maxDecompressedSize)
        => options with { MessageProcessor = new Lz4BlockArrayProcessor(maxDecompressedSize) };

    /// <summary>Options writing ext 98 (block per segment); shares this instance's resolver.</summary>
    public static MessagePackSerializerOptions WithLz4BlockArray(this MessagePackSerializerOptions options)
        => options with { MessageProcessor = Lz4Compression.BlockArray };
}

public abstract class Lz4MessageProcessor : MessagePackMessageProcessor
{
    /// <summary>Messages below this many bytes are written without an envelope.</summary>
    public const int CompressionThreshold = 64;

    /// <summary>
    /// Default cap on the declared decompressed size of one message: 64MB, matching v3's
    /// MessagePackSecurity.UntrustedData default and <see cref="MessagePackSerializerOptions.MaxBufferedMessageSize"/>'s default.
    /// Construct a processor with a different value to raise or tighten it.
    /// </summary>
    public const long DefaultMaxDecompressedSize = 64 * 1024 * 1024;

    // LZ4 cannot produce more than 255 output bytes per compressed byte (each match-length
    // extension byte emits at most 255), so a declared length above compressed * 255 is
    // provably a lie — reject it BEFORE renting from it, the same doctrine as the
    // str/bin/ext allocation-bomb guard in ReadBufferExtensions.
    const long MaxExpansionPerCompressedByte = 255;

    /// <summary>
    /// Gets the cap on the total declared decompressed size of one message; a payload
    /// declaring more is rejected before any allocation (decompression-bomb guard, CWE-409).
    /// </summary>
    public long MaxDecompressedSize { get; }

    private protected Lz4MessageProcessor(long maxDecompressedSize)
    {
        if (maxDecompressedSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDecompressedSize));
        }
        MaxDecompressedSize = maxDecompressedSize;
    }

    private protected void GuardDeclaredLength(int declaredLength, int compressedLength, long totalDeclared)
    {
        if (declaredLength > compressedLength * MaxExpansionPerCompressedByte)
        {
            Lz4Throws.ImplausibleDeclaredLength(declaredLength, compressedLength);
        }
        if (totalDeclared > MaxDecompressedSize)
        {
            Lz4Throws.DeclaredLengthExceedsMaximum(totalDeclared, MaxDecompressedSize);
        }
    }

    // ---- shared read side: both processors transparently read both codes ----

    public sealed override bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message)
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
            GuardDeclaredLength(uncompressedLength, lz4.Length, uncompressedLength);
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
                    GuardDeclaredLength(uncompressedLength, binLength, totalDeclared);
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

    public sealed override bool TryDecode(in ReadOnlySequence<byte> source, out DecodedMessage message)
    {
        if (source.IsSingleSegment)
        {
            return TryDecode(source.FirstSpan, out message);
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
            return TryDecode(flat.AsSpan(0, length), out message);
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

/// <summary>Whole message as a single LZ4 block: ext 99 { int32 uncompressedLength, lz4 }.</summary>
public sealed class Lz4BlockProcessor : Lz4MessageProcessor
{
    public Lz4BlockProcessor()
        : base(DefaultMaxDecompressedSize)
    {
    }

    /// <param name="maxDecompressedSize">Cap on the declared decompressed size of one message (decompression-bomb guard); the default is <see cref="Lz4MessageProcessor.DefaultMaxDecompressedSize"/>.</param>
    public Lz4BlockProcessor(long maxDecompressedSize)
        : base(maxDecompressedSize)
    {
    }

    public override bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output)
    {
        if (message.Length < CompressionThreshold)
        {
            return false;
        }
        var (rented, start, length) = EncodeCore(message, checked((int)message.Length));
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

    public override bool TryEncode<TWriteBuffer>(ref BufferSegments message, ref TWriteBuffer output)
    {
        if (message.Length < CompressionThreshold)
        {
            return false;
        }
        var (rented, start, length) = EncodeCore(message, checked((int)message.Length));
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

    const int MaxHeaderLength = 6 /* ext header */ + 5 /* forced int32 length prefix */;

    static (byte[] Rented, int Start, int Length) EncodeCore(BufferSegments message, int messageLength)
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

/// <summary>One LZ4 block per write segment: [array n+1][ext 98: sizes][bin lz4]...</summary>
public sealed class Lz4BlockArrayProcessor : Lz4MessageProcessor
{
    public Lz4BlockArrayProcessor()
        : base(DefaultMaxDecompressedSize)
    {
    }

    /// <param name="maxDecompressedSize">Cap on the total declared decompressed size of one message (decompression-bomb guard); the default is <see cref="Lz4MessageProcessor.DefaultMaxDecompressedSize"/>.</param>
    public Lz4BlockArrayProcessor(long maxDecompressedSize)
        : base(maxDecompressedSize)
    {
    }

    public override bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output)
    {
        if (message.Length < CompressionThreshold)
        {
            return false;
        }
        var (rented, written) = EncodeCore(message);
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

    public override bool TryEncode<TWriteBuffer>(ref BufferSegments message, ref TWriteBuffer output)
    {
        if (message.Length < CompressionThreshold)
        {
            return false;
        }
        var (rented, written) = EncodeCore(message);
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

    static (byte[] Rented, int Written) EncodeCore(BufferSegments message)
    {
        // pass 1: count segments and total worst-case size (segments are at most 17)
        var counting = message;
        var blockCount = 0;
        long maxTotal = 0;
        while (counting.TryGetNext(out var segment))
        {
            blockCount++;
            maxTotal += 5 /* bin32 header */ + LZ4.Block.GetMaxCompressedLength(segment.Length);
        }
        maxTotal += MaxArrayHeaderLength + 6 /* ext header */ + 5L * blockCount /* forced int32 sizes */;

        var rented = ArrayPool<byte>.Shared.Rent(checked((int)maxTotal));
        try
        {
            var span = rented.AsSpan();
            var offset = 0;

            // [array n+1][ext 98 { int32 sizes... }]
            offset += UnsafeWriteArrayHeader(ref span[0], blockCount + 1);
            offset += UnsafeWriteExtHeader(ref span[offset], ThisLibraryExtensionTypeCodes.Lz4BlockArray, 5 * blockCount);
            var sizing = message;
            while (sizing.TryGetNext(out var segment))
            {
                offset += UnsafeWriteForcedInt32(ref span[offset], segment.Length);
            }

            // [bin lz4block]...
            while (message.TryGetNext(out var segment))
            {
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

static class Lz4Throws
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void InvalidEnvelope() => throw new MessagePackSerializationException("Invalid LZ4 envelope.");

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void ImplausibleDeclaredLength(int declaredLength, int compressedLength) => throw new MessagePackSerializationException($"LZ4 envelope declares a {(uint)declaredLength} byte decompressed length, which {compressedLength} compressed bytes cannot produce (LZ4 expands at most 255:1)");

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void DeclaredLengthExceedsMaximum(long totalDeclared, long maxDecompressedSize) => throw new MessagePackSerializationException($"LZ4 envelope declares a {totalDeclared} byte decompressed length, which exceeds the configured maximum (MaxDecompressedSize {maxDecompressedSize})");
}
