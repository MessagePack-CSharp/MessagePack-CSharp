using System.Buffers.Binary;
using SerializerFoundation;
using static MessagePack.MessagePackPrimitives;
#if NET11_0_OR_GREATER
using FrameEncoder = System.IO.Compression.ZstandardEncoder;
using FrameDecoder = System.IO.Compression.ZstandardDecoder;
#else
using System.Runtime.InteropServices;
using NativeCompressions;
using NativeCompressions.Interop;
using static NativeCompressions.Interop.ZstandardNativeMethods;
using FrameEncoder = MessagePack.InteropZstandardEncoder;
using FrameDecoder = NativeCompressions.ZstandardDecoder;
#endif

namespace MessagePack;

/// <summary>
/// Zstandard as a <see cref="MessagePackMessageProcessor"/>, in two shapes. <see cref="Envelope"/> wraps the whole message
/// as one Zstandard frame inside ext 96 { int32 uncompressedLength, frame }, passes non-enveloped messages through and
/// leaves messages under a size threshold uncompressed. <see cref="Frame"/> writes every message as one standard
/// Zstandard frame with nothing around it, the container the zstd tool and every Zstandard implementation read.
/// Codec: the in-box System.IO.Compression.ZstandardEncoder/Decoder on .NET 11, NativeCompressions.Zstandard on .NET 10;
/// both are libzstd, and the frame is a standard Zstandard frame either way.
/// </summary>
public static class ZstandardCompression
{
    /// <summary>Every message as one Zstandard frame inside a MessagePack ext 96 envelope at the default compression level (3) and decompression cap, see <see cref="ZstandardEnvelopeProcessor"/>.</summary>
    public static MessagePackMessageProcessor Envelope { get; } = new ZstandardEnvelopeProcessor();

    /// <summary>Every message as one standard Zstandard frame with no MessagePack envelope, see <see cref="ZstandardFrameProcessor"/>.</summary>
    public static MessagePackMessageProcessor Frame { get; } = new ZstandardFrameProcessor();
}

public static class ZstandardMessagePackOptionsExtensions
{
    /// <summary>Options writing ext 96 at the default compression level; shares this instance's resolver.</summary>
    public static MessagePackSerializerOptions WithZstandardEnvelope(this MessagePackSerializerOptions options)
        => options with { MessageProcessor = ZstandardCompression.Envelope };

    /// <summary>Options writing ext 96 at <paramref name="compressionLevel"/> (1 = fastest, 3 = zstd default, 19 = slowest, 22 with ultra).</summary>
    public static MessagePackSerializerOptions WithZstandardEnvelope(this MessagePackSerializerOptions options, int compressionLevel)
        => options with { MessageProcessor = new ZstandardEnvelopeProcessor(compressionLevel) };

    /// <summary>Options writing ext 96 at <paramref name="compressionLevel"/> with a custom decompression-bomb cap (see <see cref="ZstandardEnvelopeProcessor.MaxDecompressedSize"/>).</summary>
    public static MessagePackSerializerOptions WithZstandardEnvelope(this MessagePackSerializerOptions options, int compressionLevel, long maxDecompressedSize)
        => options with { MessageProcessor = new ZstandardEnvelopeProcessor(compressionLevel, maxDecompressedSize) };

    /// <summary>Options writing every message as one standard Zstandard frame with no MessagePack envelope, see <see cref="ZstandardFrameProcessor"/>.</summary>
    public static MessagePackSerializerOptions WithZstandardFrame(this MessagePackSerializerOptions options)
        => options with { MessageProcessor = ZstandardCompression.Frame };

    /// <summary>Options writing Zstandard frames at <paramref name="compressionLevel"/> (1 = fastest, 3 = zstd default, 19 = slowest, 22 with ultra).</summary>
    public static MessagePackSerializerOptions WithZstandardFrame(this MessagePackSerializerOptions options, int compressionLevel)
        => options with { MessageProcessor = new ZstandardFrameProcessor(compressionLevel) };

    /// <summary>Options writing Zstandard frames at <paramref name="compressionLevel"/> with a custom decompression-bomb cap (see <see cref="ZstandardFrameProcessor.MaxDecompressedSize"/>).</summary>
    public static MessagePackSerializerOptions WithZstandardFrame(this MessagePackSerializerOptions options, int compressionLevel, long maxDecompressedSize)
        => options with { MessageProcessor = new ZstandardFrameProcessor(compressionLevel, maxDecompressedSize) };
}

/// <summary>
/// Whole message as a single Zstandard frame: ext 96 { int32 uncompressedLength, frame }. Reading passes non-enveloped
/// messages through, so a reader configured with the processor accepts plain messages as well. Messages smaller than
/// <see cref="CompressionThreshold"/> are written without an envelope; above it the envelope is always written, whether
/// or not the frame came out smaller (same rule as MessagePack.LZ4: the envelope depends only on the size threshold,
/// never on the data content).
/// </summary>
public sealed class ZstandardEnvelopeProcessor : MessagePackMessageProcessor
{
    /// <summary>Messages below this many bytes are written without an envelope.</summary>
    public const int CompressionThreshold = 64;

    /// <summary>zstd's own default level (3): the speed/ratio balance the reference CLI ships with.</summary>
    public const int DefaultCompressionLevel = 3;

    /// <summary>
    /// Default cap on the declared decompressed size of one message: 64MB, matching MessagePack.LZ4 and
    /// <see cref="MessagePackSerializerOptions.MaxBufferedMessageSize"/>'s default.
    /// </summary>
    public const long DefaultMaxDecompressedSize = 64 * 1024 * 1024;

    /// <summary>The zstd compression level frames are written at.</summary>
    public int CompressionLevel { get; }

    /// <summary>
    /// Cap on the declared decompressed size of one message; a payload declaring more is rejected before any
    /// allocation (decompression-bomb guard, CWE-409). Unlike LZ4 there is no per-byte expansion bound to check
    /// against: a Zstandard RLE block turns a handful of bytes into 128KB legitimately, so the cap is the guard.
    /// </summary>
    public long MaxDecompressedSize { get; }

    readonly ZstandardCodec codec;

    public ZstandardEnvelopeProcessor()
        : this(DefaultCompressionLevel, DefaultMaxDecompressedSize)
    {
    }

    /// <param name="compressionLevel">The zstd compression level (1 = fastest, 3 = zstd default, 19 = slowest, 22 with ultra).</param>
    public ZstandardEnvelopeProcessor(int compressionLevel)
        : this(compressionLevel, DefaultMaxDecompressedSize)
    {
    }

    /// <param name="compressionLevel">The zstd compression level (1 = fastest, 3 = zstd default, 19 = slowest, 22 with ultra).</param>
    /// <param name="maxDecompressedSize">Cap on the declared decompressed size of one message (decompression-bomb guard); the default is <see cref="DefaultMaxDecompressedSize"/>.</param>
    public ZstandardEnvelopeProcessor(int compressionLevel, long maxDecompressedSize)
    {
        if (maxDecompressedSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDecompressedSize));
        }
        CompressionLevel = compressionLevel;
        MaxDecompressedSize = maxDecompressedSize;
        codec = new ZstandardCodec(compressionLevel);
    }

    // ---- write side ----

    // The frame is compressed straight into the output buffer behind a fixed-width header: ext32 + forced int32 is
    // always 11 bytes, so the header slot is known before the frame length is, and nothing is staged or flattened
    // (the codec streams the message segment by segment). ext32 costs at most 3 bytes over the smallest width, and
    // every reader accepts any ext width.
    const int HeaderLength = 6 /* ext32 header */ + 5 /* forced int32 length prefix */;

    public override bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output)
    {
        if (message.Length < CompressionThreshold)
        {
            return false;
        }
        var messageLength = checked((int)message.Length);
        var destination = output.GetSpan(HeaderLength + FrameCodec.MaxCompressedLength(messageLength));
        output.Advance(EncodeCore(message, messageLength, destination));
        return true;
    }

#if NET9_0_OR_GREATER
    public override bool TryEncode<TWriteBuffer>(ref BufferSegments message, ref TWriteBuffer output)
    {
        if (message.Length < CompressionThreshold)
        {
            return false;
        }
        var messageLength = checked((int)message.Length);
        var destination = output.GetSpan(HeaderLength + FrameCodec.MaxCompressedLength(messageLength));
        output.Advance(EncodeCore(message, messageLength, destination));
        return true;
    }
#endif

    int EncodeCore(BufferSegments message, int messageLength, Span<byte> destination)
    {
        var frameLength = codec.CompressFrame(message, messageLength, destination.Slice(HeaderLength));
        destination[0] = MessagePackCode.Ext32;
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(1), (uint)(5 + frameLength));
        destination[5] = unchecked((byte)ThisLibraryExtensionTypeCodes.Zstandard);
        UnsafeWriteForcedInt32(ref destination[6], messageLength);
        return HeaderLength + frameLength;
    }

    // ---- read side ----

    public override bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message)
    {
        message = default;

        // ext 96: [0xd2 uncompressedLength][zstd frame]
        if (TryReadExtHeader(source, out var typeCode, out var dataLength, out var tokenSize) != DecodeResult.Success)
        {
            return false; // not an ext at all: a plain message
        }
        if (typeCode != ThisLibraryExtensionTypeCodes.Zstandard)
        {
            return false; // user-data ext, or another processor's envelope: not ours
        }
        if (dataLength > source.Length - tokenSize)
        {
            ZstandardThrows.InvalidEnvelope(); // header claims more payload than the message holds
        }
        var data = source.Slice(tokenSize, dataLength);
        if (TryReadInt32(data, out var uncompressedLength, out var intSize) != DecodeResult.Success || uncompressedLength < 0)
        {
            ZstandardThrows.InvalidEnvelope();
        }
        if (uncompressedLength > MaxDecompressedSize)
        {
            ZstandardThrows.DeclaredLengthExceedsMaximum(uncompressedLength, MaxDecompressedSize);
        }
        var frame = data.Slice(intSize);
        // a frame that carries its own content size must agree with the envelope; the envelope's number is the one
        // the buffer is sized from, so a disagreement is corruption, not a preference
        if (FrameCodec.TryGetContentSize(frame, out var frameContentSize) && frameContentSize != uncompressedLength)
        {
            ZstandardThrows.InvalidEnvelope();
        }

        var rented = ArrayPool<byte>.Shared.Rent(uncompressedLength);
        var decoded = codec.DecompressFrame(frame, rented.AsSpan(0, uncompressedLength));
        if (decoded != uncompressedLength)
        {
            // return before throwing: no DecodedMessage owner exists yet to release this
            ArrayPool<byte>.Shared.Return(rented);
            ZstandardThrows.InvalidEnvelope();
        }
        message = new DecodedMessage(new ReadOnlySequence<byte>(rented, 0, uncompressedLength), new ZstandardRentedOwner(rented));
        return true;
    }

    public override bool TryDecode(in ReadOnlySequence<byte> source, out DecodedMessage message)
    {
        if (source.IsSingleSegment)
        {
            return TryDecode(source.FirstSpan, out message);
        }

        // identify non-envelopes from a stitched header prefix first: passthrough input must never pay a
        // whole-message flatten just to be recognized
        Span<byte> prefix = stackalloc byte[MaxExtHeaderLength];
        var prefixLength = (int)Math.Min(source.Length, MaxExtHeaderLength);
        source.Slice(0, prefixLength).CopyTo(prefix);
        if (TryReadExtHeader(prefix.Slice(0, prefixLength), out var typeCode, out _, out _) != DecodeResult.Success
            || typeCode != ThisLibraryExtensionTypeCodes.Zstandard)
        {
            message = default;
            return false;
        }

        // our envelope: flatten and reuse the span logic (the frame must be contiguous for the decoder anyway)
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

    // ext32 header: a prefix this long always yields a definitive envelope verdict, and a shorter prefix is the whole message
    const int MaxExtHeaderLength = 6;
}

/// <summary>
/// Every message as one standard Zstandard frame with nothing around it: the container the zstd tool and every
/// Zstandard implementation read, and a stream of messages is a concatenation of frames, which those tools decode as
/// one stream. Unlike the ext envelope there is no size threshold and no passthrough: every message is a frame, and
/// input that is not one is rejected. Set through <see cref="ZstandardMessagePackOptionsExtensions.WithZstandardFrame(MessagePackSerializerOptions)"/>.
/// </summary>
public sealed class ZstandardFrameProcessor : MessagePackMessageProcessor
{
    /// <summary>zstd's own default level (3).</summary>
    public const int DefaultCompressionLevel = ZstandardEnvelopeProcessor.DefaultCompressionLevel;

    /// <summary>Default cap on the decompressed size of one message, the same 64MB as the ext envelope.</summary>
    public const long DefaultMaxDecompressedSize = ZstandardEnvelopeProcessor.DefaultMaxDecompressedSize;

    /// <summary>The zstd compression level frames are written at.</summary>
    public int CompressionLevel { get; }

    /// <summary>
    /// Cap on the decompressed size of one message (decompression-bomb guard, CWE-409). A frame that carries its content
    /// size is rejected from the header; one that does not is decompressed into a buffer that never grows past the cap.
    /// </summary>
    public long MaxDecompressedSize { get; }

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
    {
        if (maxDecompressedSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDecompressedSize));
        }
        CompressionLevel = compressionLevel;
        MaxDecompressedSize = maxDecompressedSize;
        codec = new ZstandardCodec(compressionLevel);
    }

    // ---- write side ----

    // Every message is compressed straight into the output buffer, streamed segment by segment: no flatten, no
    // staging copy. The encoder pledges the content size, so the frame header carries it and a reader applies its
    // cap before decompressing anything.

    public override bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output)
    {
        var messageLength = checked((int)message.Length);
        var destination = output.GetSpan(FrameCodec.MaxCompressedLength(messageLength));
        output.Advance(codec.CompressFrame(message, messageLength, destination));
        return true;
    }

#if NET9_0_OR_GREATER
    public override bool TryEncode<TWriteBuffer>(ref BufferSegments message, ref TWriteBuffer output)
    {
        var messageLength = checked((int)message.Length);
        var destination = output.GetSpan(FrameCodec.MaxCompressedLength(messageLength));
        output.Advance(codec.CompressFrame(message, messageLength, destination));
        return true;
    }
#endif

    // ---- read side ----

    public override bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message)
    {
        source = ZstandardFrameWalker.SkipToFrame(source);
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
            written = codec.DecompressFrame(source, rented.AsSpan(0, expected));
            if (written != expected)
            {
                ArrayPool<byte>.Shared.Return(rented);
                ZstandardThrows.InvalidEnvelope();
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
}

// The codec behind both processors: cached contexts and the one-frame compress / decompress calls.
// A zstd compression/decompression context costs microseconds to create and free, a large share of the whole encode of
// a small message (measured on the 1.6KB Answer graph, CompressionProcessorBenchmark: one-shot Compress/Decompress
// 6.7/4.4 us, cached contexts 5.1/3.0 us, LZ4 1.5/1.1 us for a 15% larger wire), so each processor keeps one encoder
// and one decoder cached for reuse. The cache is a single slot taken and put back with interlocked exchanges:
// uncontended use never allocates, and a second thread arriving while the slot is empty creates its own context and
// disposes it afterwards instead of waiting.
sealed class ZstandardCodec(int compressionLevel)
{
    sealed class EncoderBox(int compressionLevel)
    {
        public FrameEncoder Encoder = new(compressionLevel);
    }

    sealed class DecoderBox
    {
        public FrameDecoder Decoder = new();
    }

    EncoderBox? encoderCache;
    DecoderBox? decoderCache;

    EncoderBox RentEncoder() => Interlocked.Exchange(ref encoderCache, null) ?? new EncoderBox(compressionLevel);

    void ReturnEncoder(EncoderBox box)
    {
        box.Encoder.Reset();
        if (Interlocked.CompareExchange(ref encoderCache, box, null) != null)
        {
            box.Encoder.Dispose(); // the slot was refilled meanwhile
        }
    }

    DecoderBox RentDecoder() => Interlocked.Exchange(ref decoderCache, null) ?? new DecoderBox();

    void ReturnDecoder(DecoderBox box)
    {
        box.Decoder.Reset();
        if (Interlocked.CompareExchange(ref decoderCache, box, null) != null)
        {
            box.Decoder.Dispose();
        }
    }

    // One complete frame from the message through the cached encoder, streamed segment by segment so the message is
    // never flattened; the encoder is told the content size first so the frame header carries it (readers size their
    // buffers from it and the envelope cross-checks it). The destination is MaxCompressedLength-sized for the whole
    // message, so anything but a finished frame that consumed every byte is a codec fault, and a call that makes no
    // progress against it is reported instead of spun on.
    public int CompressFrame(BufferSegments message, int messageLength, Span<byte> destination)
    {
        var box = RentEncoder();
        try
        {
            box.Encoder.SetSourceLength(messageLength);
            var written = CompressSegments(box.Encoder, message, messageLength, destination);
            if (written <= 0)
            {
                ZstandardThrows.InvalidEnvelope();
            }
            ReturnEncoder(box);
            return written;
        }
        catch
        {
            box.Encoder.Dispose(); // a context that threw is not trusted back into the cache
            throw;
        }
    }

    static int CompressSegments(FrameEncoder encoder, BufferSegments message, int messageLength, Span<byte> destination)
    {
        var written = 0;
        var remaining = messageLength;
        var finished = false;
        while (!finished && message.TryGetNext(out var segment))
        {
            if (segment.IsEmpty)
            {
                continue;
            }
            remaining -= segment.Length;
            if (remaining < 0)
            {
                ZstandardThrows.InvalidEnvelope(); // the segments hold more than the declared message
            }
            // the last non-empty segment ends the frame; the iterator may still yield empty segments after it, and
            // feeding the encoder again would open a second frame
            finished = remaining == 0;
            written += CompressSegment(encoder, segment, destination.Slice(written), finished);
        }
        if (!finished)
        {
            if (remaining != 0)
            {
                ZstandardThrows.InvalidEnvelope(); // the segments hold less than the declared message
            }
            written += CompressSegment(encoder, ReadOnlySpan<byte>.Empty, destination.Slice(written), isFinalBlock: true); // the empty message
        }
        return written;
    }

    static int CompressSegment(FrameEncoder encoder, ReadOnlySpan<byte> segment, Span<byte> destination, bool isFinalBlock)
    {
        var written = 0;
        while (true)
        {
            var status = encoder.Compress(segment, destination.Slice(written), out var consumed, out var produced, isFinalBlock);
            written += produced;
            segment = segment.Slice(consumed);
            if (status == OperationStatus.InvalidData)
            {
                ZstandardThrows.InvalidEnvelope();
            }
            if (isFinalBlock ? (status == OperationStatus.Done && segment.IsEmpty) : segment.IsEmpty)
            {
                return written;
            }
            if (consumed == 0 && produced == 0)
            {
                ZstandardThrows.InvalidEnvelope(); // no progress against a bound-sized destination
            }
        }
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
            // the BCL decoder reports malformed input as InvalidData; the caller throws after returning the buffer
            return status == OperationStatus.Done && consumed == frame.Length ? written : -1;
        }
#if !NET11_0_OR_GREATER
        catch (ZstandardException)
        {
            // the native binding throws on malformed input instead; same outcome as InvalidData above
            box.Decoder.Dispose();
            return -1;
        }
#endif
        catch
        {
            box.Decoder.Dispose();
            throw;
        }
    }

    // a frame without a content size: decompress through the streaming decoder into a buffer that grows under the cap
    public (byte[] Rented, int Written) DecompressUnsized(ReadOnlySpan<byte> frame, long maxDecompressedSize)
    {
        var cap = (int)Math.Min(maxDecompressedSize, int.MaxValue);
        var rented = ArrayPool<byte>.Shared.Rent(Math.Min(Math.Max(frame.Length * 4, 1024), cap));
        var written = 0;
        var box = RentDecoder();
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
                    ArrayPool<byte>.Shared.Return(rented);
                    ZstandardThrows.DeclaredLengthExceedsMaximum(written + 1L, maxDecompressedSize);
                }
                var grown = ArrayPool<byte>.Shared.Rent((int)Math.Min((long)rented.Length * 2, cap));
                rented.AsSpan(0, written).CopyTo(grown);
                ArrayPool<byte>.Shared.Return(rented);
                rented = grown;
            }
        }
#if !NET11_0_OR_GREATER
        catch (ZstandardException)
        {
            // the native binding throws on malformed input; the fall-through below turns it into the data error
        }
#endif
        catch
        {
            ArrayPool<byte>.Shared.Return(rented);
            box.Decoder.Dispose();
            throw;
        }
        ArrayPool<byte>.Shared.Return(rented);
        box.Decoder.Dispose();
        ZstandardThrows.InvalidEnvelope();
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
                ZstandardThrows.InvalidEnvelope();
            }
            source = source.Slice(8 + (int)BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(4)));
        }
    }

    public static bool TryFindEnd(in ReadOnlySequence<byte> buffer, out long length)
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
                offset += 8 + BinaryPrimitives.ReadUInt32LittleEndian(scratch);
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

        // blocks: 3-byte header = last flag (bit 0), type (bits 1-2), size (bits 3-23); an RLE block carries one byte
        Span<byte> blockHeader = stackalloc byte[3];
        while (true)
        {
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

#if !NET11_0_OR_GREATER
// Stand-in for NativeCompressions.ZstandardEncoder until the binding exposes SetSourceLength (ZSTD_CCtx_setPledgedSrcSize),
// which the frame codec needs so a message streamed segment by segment still gets its content size into the frame
// header. The same three calls over the binding's raw interop, under the BCL ZstandardEncoder's names, so once the
// binding has the member the FrameEncoder alias points back at it and this class goes. Not thread-safe; ZstandardCodec
// hands out one per call.
sealed unsafe class InteropZstandardEncoder : IDisposable
{
    ZSTD_CCtx_s* context;

    public InteropZstandardEncoder(int compressionLevel)
    {
        context = ZSTD_createCCtx();
        if (context == null)
        {
            throw new OutOfMemoryException("ZSTD_createCCtx failed.");
        }
        try
        {
            ThrowIfError(ZSTD_CCtx_setParameter(context, ZSTD_cParameter.ZSTD_c_compressionLevel, compressionLevel));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    ~InteropZstandardEncoder() => Free();

    // the pledge holds for the frame that the next Compress opens; Reset clears it with the session
    public void SetSourceLength(long length) => ThrowIfError(ZSTD_CCtx_setPledgedSrcSize(Context, (ulong)length));

    public OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
    {
        var endOperation = isFinalBlock ? ZSTD_EndDirective.ZSTD_e_end : ZSTD_EndDirective.ZSTD_e_continue;
        fixed (byte* sourcePointer = source)
        fixed (byte* destinationPointer = destination)
        {
            var input = new ZSTD_inBuffer_s { src = sourcePointer, size = (nuint)source.Length, pos = 0 };
            var output = new ZSTD_outBuffer_s { dst = destinationPointer, size = (nuint)destination.Length, pos = 0 };
            // the return value is what the final call still has to flush from the context (0 = the frame is complete),
            // or an error code; a continue call may leave input buffered in the context, which is not an error
            var remaining = ZSTD_compressStream2(Context, &output, &input, endOperation);
            GC.KeepAlive(this); // the finalizer must not free the context under the native call
            if (ZSTD_isError(remaining) != 0)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return OperationStatus.InvalidData;
            }
            bytesConsumed = (int)input.pos;
            bytesWritten = (int)output.pos;
            if (bytesConsumed != source.Length || (isFinalBlock && remaining != 0))
            {
                return OperationStatus.DestinationTooSmall;
            }
            return OperationStatus.Done;
        }
    }

    public void Reset() => ThrowIfError(ZSTD_CCtx_reset(Context, ZSTD_ResetDirective.ZSTD_reset_session_only));

    ZSTD_CCtx_s* Context => context != null ? context : throw new ObjectDisposedException(nameof(InteropZstandardEncoder));

    public void Dispose()
    {
        Free();
        GC.SuppressFinalize(this);
    }

    void Free()
    {
        var freeing = context;
        if (freeing != null)
        {
            context = null;
            ZSTD_freeCCtx(freeing);
        }
    }

    static void ThrowIfError(nuint code)
    {
        if (ZSTD_isError(code) != 0)
        {
            throw new InvalidOperationException("zstd: " + Marshal.PtrToStringUTF8((IntPtr)ZSTD_getErrorName(code)));
        }
    }
}
#endif

static class ZstandardThrows
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void InvalidEnvelope() => throw new MessagePackSerializationException("Invalid Zstandard envelope.");

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void NotAFrame() => throw new MessagePackSerializationException("The message is not a Zstandard frame; the Zstandard frame processor accepts nothing else.");

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void DeclaredLengthExceedsMaximum(long declared, long maxDecompressedSize) => throw new MessagePackSerializationException($"Zstandard envelope declares a {declared} byte decompressed length, which exceeds the configured maximum (MaxDecompressedSize {maxDecompressedSize})");
}
