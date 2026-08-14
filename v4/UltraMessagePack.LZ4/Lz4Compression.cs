// TODO: Since MessageProcessor and NativeCompressions.LZ4 are both still incomplete
// this is a provisional implementation.
// I'll verify basic behavior first and then move on to finalizing the API, so this code is not yet at the stage to be evaluated.

using NativeCompressions;
using SerializerFoundation;
using static UltraMessagePack.MessagePackPrimitives;

namespace UltraMessagePack;

/// <summary>
/// v3-compatible LZ4 envelopes as <see cref="MessagePackMessageProcessor"/>s:
/// Block = whole message as one LZ4 block inside ext 99, BlockArray = one LZ4 block per
/// pooled write segment inside ext 98 (zero-copy on the uncompressed side — the
/// serializer's segments map 1:1 to blocks). Reading is transparent for BOTH codes and
/// passes non-enveloped messages through, matching MessagePack-CSharp semantics.
/// Messages smaller than <see cref="Lz4MessageProcessor.CompressionThreshold"/> are
/// written without an envelope (compression of tiny messages is a pure loss), and messages the
/// codec fails to shrink (match-based LZ4 cannot compress patternless data) fall back
/// to raw AFTER compressing — unlike MessagePack-CSharp, which ships the expanded
/// envelope. Readers on either side handle raw messages transparently.
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

    /// <summary>Options writing ext 98 (block per segment); shares this instance's resolver.</summary>
    public static MessagePackSerializerOptions WithLz4BlockArray(this MessagePackSerializerOptions options)
        => options with { MessageProcessor = Lz4Compression.BlockArray };
}

public abstract class Lz4MessageProcessor : MessagePackMessageProcessor
{
    // MessagePack-CSharp extension type codes (ThisLibraryExtensionTypeCode)
    private protected const sbyte Lz4BlockType = 99;
    private protected const sbyte Lz4BlockArrayType = 98;

    /// <summary>Messages below this many bytes are written without an envelope.</summary>
    public const int CompressionThreshold = 64;

    // ---- shared read side: both processors transparently read both codes ----

    public sealed override bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message)
    {

        message = default;

        // ext 99: [0xd2 uncompressedLength][lz4 bytes]
        if (TryReadExtHeader(source, out var typeCode, out var dataLength, out var tokenSize) == DecodeResult.Success)
        {
            if (typeCode != Lz4BlockType)
            {
                return false; // user-data ext: not our envelope
            }
            var data = source.Slice(tokenSize, dataLength);
            if (TryReadInt32(data, out var uncompressedLength, out var intSize) != DecodeResult.Success || uncompressedLength < 0)
            {
                Lz4Throws.InvalidEnvelope();
            }
            message = new DecodedMessage(DecodeBlock(data.Slice(intSize), uncompressedLength, out var rented), new RentedSingleOwner(rented));
            return true;
        }

        // ext 98 inside an array: [array n+1][ext 98: sizes][bin block]...
        if (TryReadArrayHeader(source, out var count, out tokenSize) == DecodeResult.Success && count >= 1)
        {
            var rest = source.Slice(tokenSize);
            if (TryReadExtHeader(rest, out typeCode, out var sizesLength, out var extSize) != DecodeResult.Success || typeCode != Lz4BlockArrayType)
            {
                return false; // a perfectly ordinary msgpack array message
            }

            var blockCount = count - 1;
            var sizes = rest.Slice(extSize, sizesLength);
            var blocks = rest.Slice(extSize + sizesLength);
            var rentedBlocks = new byte[]?[blockCount];
            try
            {
                Lz4DecodedSegment? first = null, last = null;
                for (int i = 0; i < blockCount; i++)
                {
                    if (TryReadInt32(sizes, out var uncompressedLength, out var intSize) != DecodeResult.Success || uncompressedLength < 0)
                    {
                        Lz4Throws.InvalidEnvelope();
                    }
                    sizes = sizes.Slice(intSize);

                    if (TryReadBinHeader(blocks, out var binLength, out var binSize) != DecodeResult.Success)
                    {
                        Lz4Throws.InvalidEnvelope();
                    }
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
            return typeCode == Lz4BlockType;
        }
        return TryReadArrayHeader(prefix, out var count, out tokenSize) == DecodeResult.Success
            && count >= 1
            && TryReadExtHeader(prefix.Slice(tokenSize), out typeCode, out _, out _) == DecodeResult.Success
            && typeCode == Lz4BlockArrayType;
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
            Lz4Throws.InvalidEnvelope();
        }
        return rented.AsMemory(0, uncompressedLength);
    }

    // ---- shared write-side helpers ----

    private protected static byte[] Passthrough(ArrayPoolListWriteBuffer.WrittenSegmentIterator message, long messageLength)
    {
        var result = GC.AllocateUninitializedArray<byte>(checked((int)messageLength));
        var offset = 0;
        while (message.TryGetNext(out var segment))
        {
            segment.CopyTo(result.AsSpan(offset));
            offset += segment.Length;
        }
        return result;
    }

    private protected static void Passthrough(ArrayPoolListWriteBuffer.WrittenSegmentIterator message, IBufferWriter<byte> output)
    {
        while (message.TryGetNext(out var segment))
        {
            output.Write(segment);
        }
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
    // and disposed twice, by nulling the pooled references on the first call

    sealed class RentedSingleOwner : IDisposable
    {
        byte[]? array;

        public RentedSingleOwner(byte[]? array)
        {
            this.array = array;
        }

        public void Dispose()
        {
            if (array != null)
            {
                ArrayPool<byte>.Shared.Return(array);
                array = null;
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
            if (arrays != null)
            {
                foreach (var array in arrays)
                {
                    if (array != null)
                    {
                        ArrayPool<byte>.Shared.Return(array);
                    }
                }
                arrays = null;
            }
        }
    }
}

/// <summary>Whole message as a single LZ4 block: ext 99 { int32 uncompressedLength, lz4 }.</summary>
public sealed class Lz4BlockProcessor : Lz4MessageProcessor
{
    public override byte[] Encode(ArrayPoolListWriteBuffer.WrittenSegmentIterator message, long messageLength)
    {
        if (messageLength < CompressionThreshold)
        {
            return Passthrough(message, messageLength);
        }
        var (rented, start, length) = EncodeCore(message, checked((int)messageLength));
        try
        {
            if (length >= messageLength)
            {
                return Passthrough(message, messageLength); // incompressible: raw is smaller and readers accept it transparently
            }
            return rented.AsSpan(start, length).ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    public override void Encode(ArrayPoolListWriteBuffer.WrittenSegmentIterator message, long messageLength, IBufferWriter<byte> output)
    {
        if (messageLength < CompressionThreshold)
        {
            Passthrough(message, output);
            return;
        }
        var (rented, start, length) = EncodeCore(message, checked((int)messageLength));
        try
        {
            if (length >= messageLength)
            {
                Passthrough(message, output);
                return;
            }
            output.Write(rented.AsSpan(start, length));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    const int MaxHeaderLength = 6 /* ext header */ + 5 /* forced int32 length prefix */;

    static (byte[] Rented, int Start, int Length) EncodeCore(ArrayPoolListWriteBuffer.WrittenSegmentIterator message, int messageLength)
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
            var lz4Length = LZ4.Block.Compress(flat.AsSpan(0, messageLength), rented.AsSpan(MaxHeaderLength));
            if (lz4Length <= 0) // native LZ4_compress_default reports failure as 0
            {
                Lz4Throws.InvalidEnvelope(); // cannot happen with a GetMaxCompressedLength-sized target
            }

            Span<byte> header = stackalloc byte[MaxHeaderLength];
            var headerLength = UnsafeWriteExtHeader(ref header[0], Lz4BlockType, 5 + lz4Length);
            headerLength += UnsafeWriteForcedInt32(ref header[headerLength], messageLength);
            var start = MaxHeaderLength - headerLength;
            header.Slice(0, headerLength).CopyTo(rented.AsSpan(start));
            return (rented, start, headerLength + lz4Length);
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
    public override byte[] Encode(ArrayPoolListWriteBuffer.WrittenSegmentIterator message, long messageLength)
    {
        if (messageLength < CompressionThreshold)
        {
            return Passthrough(message, messageLength);
        }
        var (rented, written) = EncodeCore(message);
        try
        {
            if (written >= messageLength)
            {
                return Passthrough(message, messageLength); // incompressible: raw is smaller and readers accept it transparently
            }
            return rented.AsSpan(0, written).ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    public override void Encode(ArrayPoolListWriteBuffer.WrittenSegmentIterator message, long messageLength, IBufferWriter<byte> output)
    {
        if (messageLength < CompressionThreshold)
        {
            Passthrough(message, output);
            return;
        }
        var (rented, written) = EncodeCore(message);
        try
        {
            if (written >= messageLength)
            {
                Passthrough(message, output);
                return;
            }
            output.Write(rented.AsSpan(0, written));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    static (byte[] Rented, int Written) EncodeCore(ArrayPoolListWriteBuffer.WrittenSegmentIterator message)
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
        var span = rented.AsSpan();
        var offset = 0;

        // [array n+1][ext 98 { int32 sizes... }]
        offset += UnsafeWriteArrayHeader(ref span[0], blockCount + 1);
        offset += UnsafeWriteExtHeader(ref span[offset], Lz4BlockArrayType, 5 * blockCount);
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
}

static class Lz4Throws
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void InvalidEnvelope() => throw new MessagePackSerializationException("Invalid LZ4 envelope.");
}
