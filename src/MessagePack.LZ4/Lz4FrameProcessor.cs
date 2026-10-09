using System.Buffers.Binary;
using NativeCompressions;
using SerializerFoundation;

namespace MessagePack;

/// <summary>
/// The LZ4 Frame format as a <see cref="MessagePackMessageProcessor"/>: every message is one standard LZ4 frame, the
/// container the lz4 tool and every LZ4 implementation read, with no MessagePack envelope around it. A stream of
/// messages is a concatenation of frames, which those tools decode as one stream.
/// Unlike the v3 ext envelopes there is no size threshold and no passthrough: every message is a frame, and input
/// that is not one is rejected. Set through <see cref="Lz4MessagePackOptionsExtensions.WithLz4Frame(MessagePackSerializerOptions)"/>.
/// </summary>
public sealed class Lz4FrameProcessor : MessagePackMessageProcessor
{
    /// <summary>Default cap on the decompressed size of one message, the same 64MB as the v3 ext envelopes.</summary>
    public const long DefaultMaxDecompressedSize = Lz4BlockEnvelope.DefaultMaxDecompressedSize;

    /// <summary>
    /// Cap on the decompressed size of one message (decompression-bomb guard, CWE-409). A frame that declares its content
    /// size is rejected from the header; one that does not is decompressed into a buffer that never grows past the cap.
    /// </summary>
    public long MaxDecompressedSize { get; }

    /// <summary>
    /// The dictionary every frame is compressed with and decompressed through (raw content: bytes typical of the
    /// messages, which the codec matches against), empty for none. See the constructor for the contract.
    /// </summary>
    public ReadOnlyMemory<byte> Dictionary { get; }

    /// <summary>The id frames written with <see cref="Dictionary"/> carry in their header (0 for none).</summary>
    public uint DictionaryId { get; }

    readonly LZ4Dictionary? dictionary;
    readonly LZ4DecompressionOptions decodeOptions;

    public Lz4FrameProcessor()
        : this(DefaultMaxDecompressedSize)
    {
    }

    /// <param name="maxDecompressedSize">Cap on the decompressed size of one message; the default is <see cref="DefaultMaxDecompressedSize"/>.</param>
    public Lz4FrameProcessor(long maxDecompressedSize)
        : this(maxDecompressedSize, ReadOnlyMemory<byte>.Empty, 0)
    {
    }

    /// <param name="maxDecompressedSize">Cap on the decompressed size of one message; the default is <see cref="DefaultMaxDecompressedSize"/>.</param>
    /// <param name="dictionary">
    /// A raw-content dictionary (up to 64 KB of bytes typical of the messages) every frame is compressed with and
    /// decompressed through; empty for none. Writer and reader must hold the same dictionary. The frames carry
    /// <paramref name="dictionaryId"/> in their header, and a frame that declares a different id is refused (a frame
    /// declaring none is read, with the dictionary as history it never references).
    /// </param>
    /// <param name="dictionaryId">The id written into the frame header for the dictionary, so a reader can tell a mismatch; 0 writes none.</param>
    public Lz4FrameProcessor(long maxDecompressedSize, ReadOnlyMemory<byte> dictionary, uint dictionaryId)
    {
        if (maxDecompressedSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDecompressedSize));
        }
        if (dictionary.IsEmpty && dictionaryId != 0)
        {
            throw new ArgumentException("A dictionary id needs a dictionary.", nameof(dictionaryId));
        }
        MaxDecompressedSize = maxDecompressedSize;
        Dictionary = dictionary;
        DictionaryId = dictionaryId;
        if (!dictionary.IsEmpty)
        {
            this.dictionary = LZ4Dictionary.Create(dictionary.Span, dictionaryId);
            decodeOptions = new LZ4DecompressionOptions { Dictionary = this.dictionary };
        }
    }

    LZ4CompressionOptions EncodeOptions(long contentSize) => dictionary is null
        ? new LZ4CompressionOptions { ContentSize = (ulong)contentSize }
        : new LZ4CompressionOptions { ContentSize = (ulong)contentSize, Dictionary = dictionary };

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    void ThrowDictionaryMismatch(uint frameDictionaryId) => throw new MessagePackSerializationException(dictionary is null
        ? $"The LZ4 frame was compressed with dictionary {frameDictionaryId}, and this reader has no dictionary; configure the processor with it (WithLz4Frame(dictionary, dictionaryId))."
        : $"The LZ4 frame was compressed with dictionary {frameDictionaryId}, and this reader holds dictionary {DictionaryId}.");

    // ---- write side ----

    // The streaming encoder compresses the message segment by segment straight into the output (no flattening copy,
    // no staging of the frame), declaring the content size in the frame header so a reader sizes its buffer from
    // the header and applies its cap before decompressing anything. Block boundaries follow the block size, not the
    // segment boundaries, so the frame is the one the one-shot compress would write. The native context is cached
    // in a single slot (see ZstandardCodec): uncontended use never allocates, a second thread creates its own.
    sealed class EncoderBox
    {
        public LZ4Encoder Encoder = new();
    }

    EncoderBox? encoderCache;

    EncoderBox RentEncoder() => Interlocked.Exchange(ref encoderCache, null) ?? new EncoderBox();

    void ReturnEncoder(EncoderBox box)
    {
        if (Interlocked.CompareExchange(ref encoderCache, box, null) != null)
        {
            box.Encoder.Dispose(); // the slot was refilled meanwhile
        }
    }

    public override bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output)
    {
        var box = RentEncoder();
        try
        {
            var encoder = box.Encoder;
            var options = EncodeOptions(message.Length);
            encoder.Reset(in options); // a new frame declaring this message's length
            while (message.TryGetNext(out var segment))
            {
                var destination = output.GetSpan(encoder.GetMaxCompressedLength(segment.Length, includingHeader: true, includingFooter: false));
                output.Advance(encoder.Compress(segment, destination));
            }
            var tail = output.GetSpan(encoder.GetMaxFlushBufferLength(includingFooter: true));
            output.Advance(encoder.Close(tail));
            ReturnEncoder(box);
            return true;
        }
        catch
        {
            box.Encoder.Dispose(); // a context that threw is not trusted back into the cache
            throw;
        }
    }

#if NET9_0_OR_GREATER
    public override bool TryEncode<TWriteBuffer>(ref BufferSegments message, ref TWriteBuffer output)
    {
        var box = RentEncoder();
        try
        {
            var encoder = box.Encoder;
            var options = EncodeOptions(message.Length);
            encoder.Reset(in options);
            while (message.TryGetNext(out var segment))
            {
                var destination = output.GetSpan(encoder.GetMaxCompressedLength(segment.Length, includingHeader: true, includingFooter: false));
                output.Advance(encoder.Compress(segment, destination));
            }
            var tail = output.GetSpan(encoder.GetMaxFlushBufferLength(includingFooter: true));
            output.Advance(encoder.Close(tail));
            ReturnEncoder(box);
            return true;
        }
        catch
        {
            box.Encoder.Dispose();
            throw;
        }
    }
#endif

    // ---- read side ----

    public override bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message)
    {
        source = Lz4FrameWalker.SkipToFrame(source);
        LZ4FrameInfo info;
        try
        {
            if (!LZ4.TryGetFrameInfo(source, out info))
            {
                Lz4Throws.InvalidEnvelope(); // a frame magic without a readable header
            }
        }
        catch (LZ4Exception)
        {
            Lz4Throws.InvalidEnvelope();
            throw;
        }
        if (info.DictionaryId != 0 && info.DictionaryId != DictionaryId)
        {
            ThrowDictionaryMismatch(info.DictionaryId); // the codec would decode garbage, or fail late
        }
        // 0 is "not carried" (the lz4 tool writes it only on request) or an empty message; either way the size is
        // discovered while decompressing under the cap
        var contentSize = info.ContentSize;
        if (contentSize > (ulong)MaxDecompressedSize)
        {
            Lz4Throws.DeclaredLengthExceedsMaximum((long)contentSize, MaxDecompressedSize);
        }
        byte[] rented;
        int written;
        if (contentSize > 0)
        {
            var expected = (int)contentSize;
            rented = ArrayPool<byte>.Shared.Rent(expected);
            try
            {
                written = dictionary is null
                    ? LZ4.Decompress(source, rented.AsSpan(0, expected))
                    : LZ4.Decompress(source, rented.AsSpan(0, expected), in decodeOptions);
            }
            catch (LZ4Exception)
            {
                ArrayPool<byte>.Shared.Return(rented);
                Lz4Throws.InvalidEnvelope();
                throw;
            }
            if (written != expected)
            {
                ArrayPool<byte>.Shared.Return(rented);
                Lz4Throws.InvalidEnvelope();
            }
        }
        else
        {
            (rented, written) = DecompressUnsized(source);
        }
        message = new DecodedMessage(new ReadOnlySequence<byte>(rented, 0, written), new RentedOwner(rented));
        return true;
    }

    public override bool TryDecode(in ReadOnlySequence<byte> source, out DecodedMessage message)
    {
        if (source.IsSingleSegment)
        {
            return TryDecode(source.FirstSpan, out message);
        }
        // the decoder wants the frame contiguous
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

    // a frame without a content size: decompress through the streaming decoder into a buffer that grows under the cap
    (byte[] Rented, int Written) DecompressUnsized(ReadOnlySpan<byte> source)
    {
        var cap = (int)Math.Min(MaxDecompressedSize, int.MaxValue);
        var rented = ArrayPool<byte>.Shared.Rent(Math.Min(Math.Max(source.Length * 4, 1024), cap));
        var written = 0;
        var decoder = dictionary is null ? new LZ4Decoder() : new LZ4Decoder(in decodeOptions);
        Span<byte> scratch = stackalloc byte[64]; // for the epilogue probe below (once, outside the loop)
        try
        {
            while (true)
            {
                var status = decoder.Decompress(source, rented.AsSpan(written, Math.Min(rented.Length, cap) - written), out var consumed, out var produced);
                source = source.Slice(consumed);
                written += produced;
                if (status == OperationStatus.Done)
                {
                    return (rented, written);
                }
                if (status != OperationStatus.DestinationTooSmall)
                {
                    // the whole frame is present: needing more data means it is corrupt, and the decoder reports
                    // malformed input as InvalidData rather than throwing
                    Lz4Throws.InvalidEnvelope();
                }
                if (written >= cap)
                {
                    // the output reached the cap: what remains can still be the frame's end mark (empty blocks and a
                    // checksum), which produces nothing, so let the decoder consume it block by block into a scratch
                    // buffer; a single produced byte is the bomb
                    while (true)
                    {
                        var tail = decoder.Decompress(source, scratch, out var tailConsumed, out var tailProduced);
                        source = source.Slice(tailConsumed);
                        if (tailProduced > 0)
                        {
                            Lz4Throws.DeclaredLengthExceedsMaximum(written + tailProduced, MaxDecompressedSize);
                        }
                        if (tail == OperationStatus.Done)
                        {
                            return (rented, written);
                        }
                        if (tail != OperationStatus.DestinationTooSmall || tailConsumed == 0)
                        {
                            Lz4Throws.InvalidEnvelope();
                        }
                    }
                }
                var grown = ArrayPool<byte>.Shared.Rent((int)Math.Min((long)rented.Length * 2, cap));
                rented.AsSpan(0, written).CopyTo(grown);
                ArrayPool<byte>.Shared.Return(rented);
                rented = grown;
            }
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(rented);
            throw;
        }
        finally
        {
            decoder.Dispose();
        }
    }

    // ---- boundaries ----

    public override bool DefinesMessageBoundaries => true;

    public override bool TryFindMessageEnd(in ReadOnlySequence<byte> buffer, out long length) => Lz4FrameWalker.TryFindEnd(in buffer, out length);

    /// <inheritdoc/>
    public override bool TryFindMessageEnd(in ReadOnlySequence<byte> buffer, ref long position, out long length) => Lz4FrameWalker.TryFindEnd(in buffer, ref position, out length);

    sealed class RentedOwner : IDisposable
    {
        byte[]? array;

        public RentedOwner(byte[] array)
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
}

// Walks an LZ4 frame by its headers only (magic, frame descriptor, then one block size per block) to find where it
// ends, without touching block data. Skippable frames in front of the message are stepped over; they are user data
// the lz4 tool may also emit.
static class Lz4FrameWalker
{
    const uint FrameMagic = 0x184D2204;
    const uint LegacyMagic = 0x184C2102;
    const uint SkippableMagic = 0x184D2A50; // 0x184D2A50 .. 0x184D2A5F
    const uint SkippableMask = 0xFFFFFFF0;

    // Steps over the skippable frames in front of the message (user data the lz4 tool may emit) and returns the LZ4
    // frame itself. The decoder stops at the end of a skippable frame as at the end of a frame, so it must never see
    // one; the header walk counted them into the message, so a cut-short one here is corruption.
    public static ReadOnlySpan<byte> SkipToFrame(ReadOnlySpan<byte> source)
    {
        while (true)
        {
            if (source.Length < 4)
            {
                Lz4Throws.NotAFrame();
            }
            var magic = BinaryPrimitives.ReadUInt32LittleEndian(source);
            if (magic == FrameMagic)
            {
                return source;
            }
            if (magic == LegacyMagic)
            {
                throw new MessagePackSerializationException("The legacy LZ4 frame format is not supported.");
            }
            if ((magic & SkippableMask) != SkippableMagic)
            {
                Lz4Throws.NotAFrame();
            }
            if (source.Length < 8 || (uint)(source.Length - 8) < BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(4)))
            {
                Lz4Throws.InvalidEnvelope();
            }
            source = source.Slice(8 + (int)BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(4)));
        }
    }

    public static bool TryFindEnd(in ReadOnlySequence<byte> buffer, out long length)
    {
        long position = 0;
        return TryFindEnd(in buffer, ref position, out length);
    }

    // position: the offset of the next block header to read, left here by the previous call over the same message
    // (0 for a fresh walk). The frame header is re-read on every call (a few bytes, it carries the flags the block
    // walk needs), the blocks already walked are not.
    public static bool TryFindEnd(in ReadOnlySequence<byte> buffer, ref long position, out long length)
    {
        var reader = new FrameHeaderReader(in buffer);
        Span<byte> scratch = stackalloc byte[4];
        long offset = 0;
        while (true)
        {
            if (!reader.TryRead(offset, scratch))
            {
                length = offset + 4;
                return false;
            }
            var magic = BinaryPrimitives.ReadUInt32LittleEndian(scratch);
            if ((magic & SkippableMask) == SkippableMagic)
            {
                if (!reader.TryRead(offset + 4, scratch))
                {
                    length = offset + 8;
                    return false;
                }
                offset += 8L + BinaryPrimitives.ReadUInt32LittleEndian(scratch); // 8L: int + uint is uint and wraps at 0xFFFFFFF8
                continue;
            }
            if (magic == LegacyMagic)
            {
                throw new MessagePackSerializationException("The legacy LZ4 frame format is not supported.");
            }
            if (magic != FrameMagic)
            {
                Lz4Throws.NotAFrame();
            }
            break;
        }

        // frame descriptor: FLG, BD, optional content size (8) and dictionary id (4), header checksum (1)
        Span<byte> descriptor = stackalloc byte[2];
        if (!reader.TryRead(offset + 4, descriptor))
        {
            length = offset + 7;
            return false;
        }
        var flags = descriptor[0];
        if ((flags >> 6) != 0b01 || (flags & 0b10) != 0)
        {
            throw new MessagePackSerializationException("Unsupported LZ4 frame version.");
        }
        var blockChecksum = (flags & 0x10) != 0;
        var contentSize = (flags & 0x08) != 0;
        var contentChecksum = (flags & 0x04) != 0;
        var dictionaryId = (flags & 0x01) != 0;
        var blockSizeId = (descriptor[1] >> 4) & 0x07;
        if (blockSizeId < 4)
        {
            throw new MessagePackSerializationException("Invalid LZ4 frame block size.");
        }
        var maxBlockSize = 1L << (8 + 2 * blockSizeId); // 4: 64KB, 5: 256KB, 6: 1MB, 7: 4MB
        offset += 6 + (contentSize ? 8 : 0) + (dictionaryId ? 4 : 0) + 1;
        if (position > offset)
        {
            offset = position; // resume at the block header the previous call stopped in front of
        }

        // blocks: a 4-byte size (top bit = stored uncompressed), data, optional block checksum; 0 ends the frame
        while (true)
        {
            position = offset;
            if (!reader.TryRead(offset, scratch))
            {
                length = offset + 4;
                return false;
            }
            var blockSize = BinaryPrimitives.ReadUInt32LittleEndian(scratch);
            offset += 4;
            if (blockSize == 0)
            {
                offset += contentChecksum ? 4 : 0;
                break;
            }
            var dataSize = blockSize & 0x7FFFFFFF;
            if (dataSize > maxBlockSize)
            {
                throw new MessagePackSerializationException("Invalid LZ4 frame block size.");
            }
            offset += dataSize + (blockChecksum ? 4 : 0);
        }
        length = offset;
        return offset <= buffer.Length;
    }
}

// Forward-only reader for the header walk. ReadOnlySequence.Slice starts from the first segment on every call, which
// would make a walk over many block headers in a many-segment pipe buffer cost blocks x segments; this keeps the
// segment it is in and only moves forward, so offsets must not decrease between calls.
ref struct FrameHeaderReader
{
    readonly ReadOnlySequence<byte> buffer;
    ReadOnlySpan<byte> segment;
    long segmentStart;
    SequencePosition next;

    public FrameHeaderReader(in ReadOnlySequence<byte> buffer)
    {
        this.buffer = buffer;
        segment = default;
        segmentStart = 0;
        next = buffer.Start;
    }

    // false when the sequence ends before offset + destination.Length
    public bool TryRead(long offset, scoped Span<byte> destination)
    {
        if (buffer.Length - offset < destination.Length)
        {
            return false;
        }
        while (offset >= segmentStart + segment.Length)
        {
            segmentStart += segment.Length;
            if (!buffer.TryGet(ref next, out var memory))
            {
                return false; // unreachable after the length check
            }
            segment = memory.Span;
        }
        var index = (int)(offset - segmentStart);
        var filled = Math.Min(segment.Length - index, destination.Length);
        segment.Slice(index, filled).CopyTo(destination);
        while (filled < destination.Length)
        {
            segmentStart += segment.Length;
            if (!buffer.TryGet(ref next, out var memory))
            {
                return false;
            }
            segment = memory.Span;
            var take = Math.Min(segment.Length, destination.Length - filled);
            segment.Slice(0, take).CopyTo(destination.Slice(filled));
            filled += take;
        }
        return true;
    }
}
