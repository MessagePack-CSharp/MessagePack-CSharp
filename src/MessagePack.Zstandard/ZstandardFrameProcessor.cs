using System.Buffers.Binary;
using SerializerFoundation;
#if NET11_0_OR_GREATER
using FrameEncoder = System.IO.Compression.ZstandardEncoder;
using FrameDecoder = System.IO.Compression.ZstandardDecoder;
using FrameDictionary = System.IO.Compression.ZstandardDictionary;
#else
using NativeCompressions;
using FrameEncoder = NativeCompressions.ZstandardEncoder;
using FrameDecoder = NativeCompressions.ZstandardDecoder;
using FrameDictionary = NativeCompressions.ZstandardDictionary;
#endif

namespace MessagePack;

/// <summary>
/// Every message as one standard Zstandard frame with nothing around it: the container the zstd tool and every
/// Zstandard implementation read, and a stream of messages is a concatenation of frames, which those tools decode as
/// one stream. There is no size threshold and no passthrough: every message is a frame, and input that is not one is
/// rejected. Set through <see cref="ZstandardMessagePackOptionsExtensions.WithZstandardFrame(MessagePackSerializerOptions)"/>.
/// Codec: the in-box System.IO.Compression.ZstandardEncoder/Decoder on .NET 11, NativeCompressions.Zstandard before that;
/// both are libzstd, and the frame is a standard Zstandard frame either way. Each instance keeps its own cached codec
/// contexts (created on first use), so a processor lives as long as the options that hold it.
/// </summary>
public sealed class ZstandardFrameProcessor : MessagePackMessageProcessor
{
    /// <summary>zstd's own default level (3): the speed/ratio balance the reference CLI ships with.</summary>
    public const int DefaultCompressionLevel = 3;

    /// <summary>
    /// Default cap on the decompressed size of one message: 64MB, matching MessagePack.LZ4 and
    /// <see cref="MessagePackSerializerOptions.MaxBufferedMessageSize"/>'s default.
    /// </summary>
    public const long DefaultMaxDecompressedSize = 64 * 1024 * 1024;

    /// <summary>The zstd compression level frames are written at.</summary>
    public int CompressionLevel { get; }

    /// <summary>
    /// Cap on the decompressed size of one message (decompression-bomb guard, CWE-409). A frame that carries its content
    /// size is rejected from the header; one that does not is decompressed into a buffer that never grows past the cap.
    /// Unlike LZ4 there is no per-byte expansion bound to check against: a Zstandard RLE block turns a handful of bytes
    /// into 128KB legitimately, so the cap is the guard.
    /// </summary>
    public long MaxDecompressedSize { get; }

    /// <summary>
    /// The largest back-reference window (as a power of two) a frame may declare, derived from
    /// <see cref="MaxDecompressedSize"/>: the decoder allocates the declared window up front, so without this cap a
    /// 10-byte frame could make it reserve the codec's 128 MB default. A frame whose content fits the cap never needs
    /// a larger window; one that declares more is rejected.
    /// </summary>
    public int WindowLogMax { get; }

    /// <summary>
    /// The dictionary every frame is compressed with and decompressed through: a trained zstd dictionary (the
    /// `zstd --train` format, which carries an id) or raw content (bytes typical of the messages, id 0); empty for
    /// none. See the constructor for the contract.
    /// </summary>
    public ReadOnlyMemory<byte> Dictionary { get; }

    /// <summary>The id of <see cref="Dictionary"/>, as frames written with it carry in their header (0 for raw content or none).</summary>
    public uint DictionaryId { get; }

    // ceil(log2(maxDecompressedSize)), within the codec's accepted range
    static int WindowLogFor(long maxDecompressedSize)
    {
        var log = 0;
        while (log < 31 && (1L << log) < maxDecompressedSize)
        {
            log++;
        }
        return Math.Max(10, log); // ZSTD_WINDOWLOG_MIN
    }

    readonly ZstandardCodec codec;

    public ZstandardFrameProcessor()
        : this(DefaultCompressionLevel, DefaultMaxDecompressedSize)
    {
    }

    /// <param name="compressionLevel">The zstd compression level (1 = fastest, 3 = zstd default, 19 = slowest, 22 with ultra).</param>
    public ZstandardFrameProcessor(int compressionLevel)
        : this(compressionLevel, DefaultMaxDecompressedSize)
    {
    }

    /// <param name="compressionLevel">The zstd compression level (1 = fastest, 3 = zstd default, 19 = slowest, 22 with ultra).</param>
    /// <param name="maxDecompressedSize">Cap on the decompressed size of one message; the default is <see cref="DefaultMaxDecompressedSize"/>.</param>
    public ZstandardFrameProcessor(int compressionLevel, long maxDecompressedSize)
        : this(compressionLevel, maxDecompressedSize, ReadOnlyMemory<byte>.Empty)
    {
    }

    /// <param name="compressionLevel">The zstd compression level (1 = fastest, 3 = zstd default, 19 = slowest, 22 with ultra).</param>
    /// <param name="maxDecompressedSize">Cap on the decompressed size of one message; the default is <see cref="DefaultMaxDecompressedSize"/>.</param>
    /// <param name="dictionary">
    /// A zstd dictionary every frame is compressed with and decompressed through: trained (`zstd --train`, the
    /// codecs' Train; it carries an id the frames declare) or raw content (bytes typical of the messages); empty for
    /// none. Writer and reader must hold the same dictionary. A frame that declares another dictionary id is refused;
    /// one that declares none is read, with the dictionary as history it never references.
    /// </param>
    public ZstandardFrameProcessor(int compressionLevel, long maxDecompressedSize, ReadOnlyMemory<byte> dictionary)
    {
        if (maxDecompressedSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDecompressedSize));
        }
        CompressionLevel = compressionLevel;
        MaxDecompressedSize = maxDecompressedSize;
        WindowLogMax = WindowLogFor(maxDecompressedSize);
        Dictionary = dictionary;
        DictionaryId = FrameCodec.DictionaryIdOfDictionary(dictionary.Span);
        codec = new ZstandardCodec(compressionLevel, WindowLogMax, dictionary.IsEmpty ? null : FrameDictionary.Create(dictionary.Span, compressionLevel));
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    void ThrowDictionaryMismatch(uint frameDictionaryId) => throw new MessagePackSerializationException(Dictionary.IsEmpty
        ? $"The Zstandard frame was compressed with dictionary {frameDictionaryId}, and this reader has no dictionary; configure the processor with it (WithZstandardFrame(dictionary))."
        : $"The Zstandard frame was compressed with dictionary {frameDictionaryId}, and this reader holds dictionary {DictionaryId}.");

    // ---- write side ----

    // Every message is compressed straight into the output buffer, streamed segment by segment: no flatten, no
    // staging copy. The encoder pledges the content size, so the frame header carries it and a reader applies its
    // cap before decompressing anything.

    // The message is compressed segment by segment straight into the output, a window per segment (and per retry
    // when the encoder has more to emit than the window holds) sized for that segment: the output never has to
    // provide one contiguous window for the whole frame, as it would for the whole-message bound. The encoder is
    // told the content size first so the frame header carries it (readers size their buffers from it).
    public override bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output)
    {
        var messageLength = checked((int)message.Length);
        var box = codec.BeginFrame(messageLength);
        try
        {
            var remaining = messageLength;
            var finished = false;
            while (message.TryGetNext(out var segment))
            {
                remaining -= segment.Length;
                if (remaining < 0)
                {
                    ZstandardThrows.InvalidFrame(); // the segments hold more than the declared message
                }
                // the segment that completes the declared length ends the frame; the view never yields an empty
                // segment, so it is the last one (a further segment would overshoot above)
                finished = remaining == 0;
                bool done;
                do
                {
                    var destination = output.GetSpan(FrameCodec.MaxCompressedLength(segment.Length));
                    output.Advance(ZstandardCodec.Step(box.Encoder, ref segment, destination, finished, out done));
                }
                while (!done);
            }
            if (!finished)
            {
                if (remaining != 0)
                {
                    ZstandardThrows.InvalidFrame(); // the segments hold less than the declared message
                }
                var empty = ReadOnlySpan<byte>.Empty; // the empty message: a frame with no content
                bool done;
                do
                {
                    var destination = output.GetSpan(FrameCodec.MaxCompressedLength(0));
                    output.Advance(ZstandardCodec.Step(box.Encoder, ref empty, destination, isFinalBlock: true, out done));
                }
                while (!done);
            }
            codec.EndFrame(box);
            return true;
        }
        catch
        {
            codec.Abandon(box);
            throw;
        }
    }

#if NET9_0_OR_GREATER
    /// <inheritdoc cref="TryEncode(ref BufferSegments, IBufferWriter{byte})"/>
    public override bool TryEncode<TWriteBuffer>(ref BufferSegments message, ref TWriteBuffer output)
    {
        var messageLength = checked((int)message.Length);
        var box = codec.BeginFrame(messageLength);
        try
        {
            var remaining = messageLength;
            var finished = false;
            while (message.TryGetNext(out var segment))
            {
                remaining -= segment.Length;
                if (remaining < 0)
                {
                    ZstandardThrows.InvalidFrame();
                }
                finished = remaining == 0;
                bool done;
                do
                {
                    var destination = output.GetSpan(FrameCodec.MaxCompressedLength(segment.Length));
                    output.Advance(ZstandardCodec.Step(box.Encoder, ref segment, destination, finished, out done));
                }
                while (!done);
            }
            if (!finished)
            {
                if (remaining != 0)
                {
                    ZstandardThrows.InvalidFrame();
                }
                var empty = ReadOnlySpan<byte>.Empty;
                bool done;
                do
                {
                    var destination = output.GetSpan(FrameCodec.MaxCompressedLength(0));
                    output.Advance(ZstandardCodec.Step(box.Encoder, ref empty, destination, isFinalBlock: true, out done));
                }
                while (!done);
            }
            codec.EndFrame(box);
            return true;
        }
        catch
        {
            codec.Abandon(box);
            throw;
        }
    }
#endif

    // ---- read side ----

    public override bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message)
    {
        source = ZstandardFrameWalker.SkipToFrame(source);
        var frameDictionaryId = FrameCodec.DictionaryIdOfFrame(source);
        if (frameDictionaryId != 0 && frameDictionaryId != DictionaryId)
        {
            ThrowDictionaryMismatch(frameDictionaryId); // the codec would report "dictionary wrong" later, less clearly
        }
        byte[] rented;
        int written;
        if (FrameCodec.TryGetContentSize(source, out var contentSize))
        {
            if (contentSize > MaxDecompressedSize)
            {
                ZstandardThrows.DeclaredLengthExceedsMaximum(contentSize, MaxDecompressedSize);
            }
            var expected = (int)contentSize;
            rented = ArrayPool<byte>.Shared.Rent(expected);
            try
            {
                written = codec.DecompressFrame(source, rented.AsSpan(0, expected));
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(rented); // a decoder failure (a window above the cap) must not leak the rental
                throw;
            }
            if (written != expected)
            {
                ArrayPool<byte>.Shared.Return(rented);
                ZstandardThrows.InvalidFrame();
            }
        }
        else
        {
            // no content size in the header (the zstd tool omits it for streamed input): grow under the cap
            (rented, written) = codec.DecompressUnsized(source, MaxDecompressedSize);
        }
        message = new DecodedMessage(new ReadOnlySequence<byte>(rented, 0, written), new ZstandardRentedOwner(rented));
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

    // ---- boundaries ----

    public override bool DefinesMessageBoundaries => true;

    public override bool TryFindMessageEnd(in ReadOnlySequence<byte> buffer, out long length) => ZstandardFrameWalker.TryFindEnd(in buffer, out length);

    /// <inheritdoc/>
    public override bool TryFindMessageEnd(in ReadOnlySequence<byte> buffer, ref long position, out long length) => ZstandardFrameWalker.TryFindEnd(in buffer, ref position, out length);
}

// The codec behind the processor: cached contexts and the one-frame compress / decompress calls.
// A zstd compression/decompression context costs microseconds to create and free, a large share of the whole encode of
// a small message (measured on the 1.6KB Answer graph, CompressionProcessorBenchmark: one-shot Compress/Decompress
// 6.7/4.4 us, cached contexts 5.1/3.0 us, LZ4 1.5/1.1 us for a 15% larger wire), so each processor keeps one encoder
// and one decoder cached for reuse. The cache is a single slot taken and put back with interlocked exchanges:
// uncontended use never allocates, and a second thread arriving while the slot is empty creates its own context and
// disposes it afterwards instead of waiting.
sealed class ZstandardCodec(int compressionLevel, int windowLogMax, FrameDictionary? dictionary)
{
    // the dictionary, like the window cap, is a context parameter that survives Reset (session-only), so each cached
    // context is created with it once. The BCL binds the compression level into the dictionary (ZstandardDictionary
    // .Create takes it; the encoder's (dictionary, int) overload is a window log), so the level goes in at Create.
    public sealed class EncoderBox(int compressionLevel, FrameDictionary? dictionary)
    {
#if NET11_0_OR_GREATER
        public FrameEncoder Encoder = dictionary is null ? new(compressionLevel) : new(dictionary);
#else
        public FrameEncoder Encoder = dictionary is null ? new(compressionLevel) : new(new ZstandardCompressionOptions { CompressionLevel = compressionLevel, Dictionary = dictionary });
#endif
    }

    sealed class DecoderBox(int windowLogMax, FrameDictionary? dictionary)
    {
#if NET11_0_OR_GREATER
        public FrameDecoder Decoder = dictionary is null ? new(windowLogMax) : new(dictionary, windowLogMax);
#else
        public FrameDecoder Decoder = dictionary is null
            ? new(new ZstandardDecompressionOptions { WindowLogMax = windowLogMax })
            : new(new ZstandardDecompressionOptions { WindowLogMax = windowLogMax, Dictionary = dictionary });
#endif
    }

    EncoderBox? encoderCache;
    DecoderBox? decoderCache;

    EncoderBox RentEncoder() => Interlocked.Exchange(ref encoderCache, null) ?? new EncoderBox(compressionLevel, dictionary);

    void ReturnEncoder(EncoderBox box)
    {
        box.Encoder.Reset();
        if (Interlocked.CompareExchange(ref encoderCache, box, null) != null)
        {
            box.Encoder.Dispose(); // the slot was refilled meanwhile
        }
    }

    DecoderBox RentDecoder() => Interlocked.Exchange(ref decoderCache, null) ?? new DecoderBox(windowLogMax, dictionary);

    void ReturnDecoder(DecoderBox box)
    {
        box.Decoder.Reset();
        if (Interlocked.CompareExchange(ref decoderCache, box, null) != null)
        {
            box.Decoder.Dispose();
        }
    }

    // A frame's encoder: rented, told the content size (the frame header carries it), and returned to the cache by
    // EndFrame, or disposed by Abandon when a call threw (a context that threw is not trusted back into the cache).
    public EncoderBox BeginFrame(int messageLength)
    {
        var box = RentEncoder();
        try
        {
            box.Encoder.SetSourceLength(messageLength);
            return box;
        }
        catch
        {
            box.Encoder.Dispose();
            throw;
        }
    }

    public void EndFrame(EncoderBox box) => ReturnEncoder(box);

    public void Abandon(EncoderBox box) => box.Encoder.Dispose();

    // One encoder call on the current segment into the window the caller holds, returning the bytes produced and
    // whether the caller is done with the segment: its bytes are consumed (and, for the final block, the frame is
    // closed). A window the encoder fills before it is done (DestinationTooSmall) is simply followed by another; a
    // call that neither consumes nor produces against a window it did not fill is a codec fault, reported instead
    // of spun on.
    public static int Step(FrameEncoder encoder, ref ReadOnlySpan<byte> segment, Span<byte> destination, bool isFinalBlock, out bool done)
    {
        var status = encoder.Compress(segment, destination, out var consumed, out var produced, isFinalBlock);
        segment = segment.Slice(consumed);
        if (status == OperationStatus.InvalidData)
        {
            ZstandardThrows.InvalidFrame();
        }
        done = isFinalBlock ? (status == OperationStatus.Done && segment.IsEmpty) : segment.IsEmpty;
        if (!done && consumed == 0 && produced == 0 && status != OperationStatus.DestinationTooSmall)
        {
            ZstandardThrows.InvalidFrame();
        }
        return produced;
    }

    // one complete frame through the cached decoder into a destination sized from the declared length; returns the
    // bytes written, or -1 for anything but a finished frame that filled it exactly
    public int DecompressFrame(ReadOnlySpan<byte> frame, Span<byte> destination)
    {
        var box = RentDecoder();
        try
        {
            var status = box.Decoder.Decompress(frame, destination, out var consumed, out var written);
            ReturnDecoder(box);
            // both decoders report malformed input as InvalidData; the caller throws after returning the buffer
            return status == OperationStatus.Done && consumed == frame.Length ? written : -1;
        }
        catch (Exception exception)
        {
            // a context that threw is not trusted back into the cache; the codec's own exception (the .NET 11 decoder
            // throws on a frame whose window exceeds the cap, where NativeCompressions reports InvalidData) is
            // surfaced as the serialization failure it is, and this processor's own throws pass through
            box.Decoder.Dispose();
            if (exception is MessagePackSerializationException)
            {
                throw;
            }
            throw ZstandardThrows.DecoderRejectedFrame(exception);
        }
    }

    // a frame without a content size: decompress through the streaming decoder into a buffer that grows under the cap
    public (byte[] Rented, int Written) DecompressUnsized(ReadOnlySpan<byte> frame, long maxDecompressedSize)
    {
        var cap = (int)Math.Min(maxDecompressedSize, int.MaxValue);
        var rented = ArrayPool<byte>.Shared.Rent(Math.Min(Math.Max(frame.Length * 4, 1024), cap));
        var written = 0;
        var box = RentDecoder();
        Span<byte> scratch = stackalloc byte[64]; // for the epilogue probe below (once, outside the loop)
        try
        {
            while (true)
            {
                var status = box.Decoder.Decompress(frame, rented.AsSpan(written, Math.Min(rented.Length, cap) - written), out var consumed, out var produced);
                frame = frame.Slice(consumed);
                written += produced;
                if (status == OperationStatus.Done)
                {
                    ReturnDecoder(box);
                    return (rented, written);
                }
                if (status != OperationStatus.DestinationTooSmall)
                {
                    break; // the whole frame is present: needing more data means it is corrupt
                }
                if (written >= cap)
                {
                    // the output reached the cap: what remains can still be the frame's epilogue (the end of the last
                    // block, a checksum), which produces nothing, so let the decoder finish into a scratch buffer
                    // (neither codec accepts an empty destination for that); a single produced byte is the bomb.
                    // The catch below returns the buffer; returning it here too would hand the same array to the pool twice
                    while (true)
                    {
                        var tail = box.Decoder.Decompress(frame, scratch, out var tailConsumed, out var tailProduced);
                        frame = frame.Slice(tailConsumed);
                        if (tailProduced > 0)
                        {
                            ZstandardThrows.DeclaredLengthExceedsMaximum(written + tailProduced, maxDecompressedSize);
                        }
                        if (tail == OperationStatus.Done)
                        {
                            ReturnDecoder(box);
                            return (rented, written);
                        }
                        if (tail != OperationStatus.DestinationTooSmall || tailConsumed == 0)
                        {
                            ZstandardThrows.InvalidFrame();
                        }
                    }
                }
                var grown = ArrayPool<byte>.Shared.Rent((int)Math.Min((long)rented.Length * 2, cap));
                rented.AsSpan(0, written).CopyTo(grown);
                ArrayPool<byte>.Shared.Return(rented);
                rented = grown;
            }
        }
        catch (Exception exception)
        {
            ArrayPool<byte>.Shared.Return(rented);
            box.Decoder.Dispose();
            if (exception is MessagePackSerializationException)
            {
                throw; // the cap throw above
            }
            throw ZstandardThrows.DecoderRejectedFrame(exception); // see DecompressFrame
        }
        ArrayPool<byte>.Shared.Return(rented);
        box.Decoder.Dispose();
        ZstandardThrows.InvalidFrame();
        throw null!; // unreachable, the throw helper does not return
    }
}

// DecodedMessage owner: releases exactly once even if the message struct is copied and disposed twice
// (a racing double Return would hand the same array to two renters, so the Interlocked is worth its one instruction)
sealed class ZstandardRentedOwner : IDisposable
{
    byte[]? array;

    public ZstandardRentedOwner(byte[] array)
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

// Walks a Zstandard frame by its headers only (magic, frame header, then one 3-byte block header per block) to find
// where it ends, without touching block data. Skippable frames in front of the message are stepped over.
static class ZstandardFrameWalker
{
    public const uint FrameMagic = 0xFD2FB528;
    const uint SkippableMagic = 0x184D2A50; // 0x184D2A50 .. 0x184D2A5F
    const uint SkippableMask = 0xFFFFFFF0;
    const int BlockMaximumSize = 128 * 1024;

    // Steps over the skippable frames in front of the message (user data the zstd tool may emit) and returns the
    // Zstandard frame itself. The decoders stop at the end of a skippable frame as at the end of a frame, so they must
    // never see one; the header walk counted them into the message, so a cut-short one here is corruption.
    public static ReadOnlySpan<byte> SkipToFrame(ReadOnlySpan<byte> source)
    {
        while (true)
        {
            if (source.Length < 4)
            {
                ZstandardThrows.NotAFrame();
            }
            var magic = BinaryPrimitives.ReadUInt32LittleEndian(source);
            if (magic == FrameMagic)
            {
                return source;
            }
            if ((magic & SkippableMask) != SkippableMagic)
            {
                ZstandardThrows.NotAFrame();
            }
            if (source.Length < 8 || (uint)(source.Length - 8) < BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(4)))
            {
                ZstandardThrows.InvalidFrame();
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
            if (magic != FrameMagic)
            {
                ZstandardThrows.NotAFrame();
            }
            break;
        }

        // frame header: descriptor byte, then optional window descriptor (absent for single-segment frames),
        // dictionary id (0/1/2/4) and frame content size (0/1/2/4/8)
        Span<byte> descriptor = stackalloc byte[1];
        if (!reader.TryRead(offset + 4, descriptor))
        {
            length = offset + 5;
            return false;
        }
        var fhd = descriptor[0];
        if ((fhd & 0x08) != 0)
        {
            throw new MessagePackSerializationException("Invalid Zstandard frame header.");
        }
        var singleSegment = (fhd & 0x20) != 0;
        var contentChecksum = (fhd & 0x04) != 0;
        var contentSizeBytes = (fhd >> 6) switch { 0 => singleSegment ? 1 : 0, 1 => 2, 2 => 4, _ => 8 };
        var dictionaryIdBytes = (fhd & 0x03) switch { 0 => 0, 1 => 1, 2 => 2, _ => 4 };
        offset += 5 + (singleSegment ? 0 : 1) + dictionaryIdBytes + contentSizeBytes;
        if (position > offset)
        {
            offset = position; // resume at the block header the previous call stopped in front of
        }

        // blocks: 3-byte header = last flag (bit 0), type (bits 1-2), size (bits 3-23); an RLE block carries one byte
        Span<byte> blockHeader = stackalloc byte[3];
        while (true)
        {
            position = offset;
            if (!reader.TryRead(offset, blockHeader))
            {
                length = offset + 3;
                return false;
            }
            var header = blockHeader[0] | (blockHeader[1] << 8) | (blockHeader[2] << 16);
            var last = (header & 1) != 0;
            var type = (header >> 1) & 3;
            var size = header >> 3;
            if (type == 3 || size > BlockMaximumSize)
            {
                throw new MessagePackSerializationException("Invalid Zstandard block header.");
            }
            offset += 3 + (type == 1 ? 1 : size);
            if (last)
            {
                offset += contentChecksum ? 4 : 0;
                break;
            }
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

// The two places where the two codecs' static surfaces differ. Everything else (constructor by level, SetSourceLength,
// Compress/Decompress with the OperationStatus shape, Reset, Dispose) is the same member set on both, reached
// through the FrameEncoder/FrameDecoder aliases at the top of the file.
static class FrameCodec
{
    const uint DictionaryMagic = 0xEC30A437;

    // the id a trained dictionary (RFC 8878 5: magic, then the id) carries; raw content has none
    public static uint DictionaryIdOfDictionary(ReadOnlySpan<byte> dictionary)
        => dictionary.Length >= 8 && BinaryPrimitives.ReadUInt32LittleEndian(dictionary) == DictionaryMagic
            ? BinaryPrimitives.ReadUInt32LittleEndian(dictionary.Slice(4))
            : 0;

    // the Dictionary_ID a frame header declares (RFC 8878 3.1.1.1: after the descriptor and the optional window
    // descriptor, 0/1/2/4 bytes as the descriptor's low bits select); 0 when it declares none or the header is cut short
    public static uint DictionaryIdOfFrame(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 5 || BinaryPrimitives.ReadUInt32LittleEndian(frame) != ZstandardFrameWalker.FrameMagic)
        {
            return 0;
        }
        var descriptor = frame[4];
        var offset = 5 + ((descriptor & 0x20) != 0 ? 0 : 1);
        var width = (descriptor & 0x03) switch { 0 => 0, 1 => 1, 2 => 2, _ => 4 };
        if (width == 0 || frame.Length < offset + width)
        {
            return 0;
        }
        return width switch
        {
            1 => frame[offset],
            2 => BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(offset)),
            _ => BinaryPrimitives.ReadUInt32LittleEndian(frame.Slice(offset)),
        };
    }

#if NET11_0_OR_GREATER
    public static int MaxCompressedLength(int inputLength) => checked((int)FrameEncoder.GetMaxCompressedLength(inputLength));

    // ZstandardDecoder.TryGetMaxDecompressedLength is ZSTD_decompressBound, not the content size: for a frame whose
    // header carries none it still answers true with the window-size bound (probed on 11.0 preview.7 and rc.1, an
    // unsized 200KB frame reports 262144), which this caller would then treat as a declared length and reject the
    // frame when the decoded length differs. The BCL exposes no content-size query, so read the frame header
    // directly (RFC 8878 3.1.1.1): Magic_Number, Frame_Header_Descriptor, optional Window_Descriptor, optional
    // Dictionary_ID, then the Frame_Content_Size field whose width Frame_Content_Size_flag selects.
    public static bool TryGetContentSize(ReadOnlySpan<byte> frame, out long size)
    {
        size = 0;
        if (frame.Length < 5 || BinaryPrimitives.ReadUInt32LittleEndian(frame) != ZstandardFrameWalker.FrameMagic)
        {
            return false;
        }
        var descriptor = frame[4];
        var contentSizeFlag = descriptor >> 6;
        var singleSegment = (descriptor & 0x20) != 0;
        var dictionaryIdFlag = descriptor & 0x03;
        var offset = 5 + (singleSegment ? 0 : 1) + (dictionaryIdFlag == 3 ? 4 : dictionaryIdFlag);
        var fieldSize = contentSizeFlag switch
        {
            0 => singleSegment ? 1 : 0,
            1 => 2,
            2 => 4,
            _ => 8,
        };
        if (fieldSize == 0 || frame.Length < offset + fieldSize)
        {
            return false;
        }
        var field = frame.Slice(offset, fieldSize);
        var value = fieldSize switch
        {
            1 => field[0],
            2 => BinaryPrimitives.ReadUInt16LittleEndian(field) + 256L,
            4 => BinaryPrimitives.ReadUInt32LittleEndian(field),
            _ => (long)BinaryPrimitives.ReadUInt64LittleEndian(field),
        };
        if (value < 0)
        {
            return false; // above long.MaxValue, nothing this reader could allocate anyway
        }
        size = value;
        return true;
    }
#else
    public static int MaxCompressedLength(int inputLength) => Zstandard.GetMaxCompressedLength(inputLength);

    public static bool TryGetContentSize(ReadOnlySpan<byte> frame, out long size)
    {
        if (Zstandard.TryGetFrameContentSize(frame, out var unsigned) && unsigned <= long.MaxValue)
        {
            size = (long)unsigned;
            return true;
        }
        size = 0;
        return false;
    }
#endif
}
