using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using MessagePack;
using Xunit;
using V4Options = MessagePack.MessagePackSerializerOptions;
using V4 = MessagePack.MessagePackSerializer;
#if NET11_0_OR_GREATER
using FrameEncoder = System.IO.Compression.ZstandardEncoder;
using FrameDecoder = System.IO.Compression.ZstandardDecoder;
#else
using NativeCompressions;
using FrameEncoder = NativeCompressions.ZstandardEncoder;
using FrameDecoder = NativeCompressions.ZstandardDecoder;
#endif

namespace MessagePack.Tests;

// ZstandardFrameProcessor: every message is one standard Zstandard frame with no MessagePack envelope. The codec's own
// frame decoder (in-box on .NET 11, NativeCompressions on .NET 10) stands in for "any Zstandard implementation" as the
// interop oracle, and the async entries find frame boundaries through the processor's header walk.
public class ZstandardFrameTests
{
    static V4Options Options { get; } = V4Options.Default.WithZstandardFrame();

    static readonly byte[] Magic = [0x28, 0xB5, 0x2F, 0xFD];

    static int[] BigCompressible()
    {
        var data = new int[100_000];
        for (int i = 0; i < data.Length; i++) data[i] = i % 100;
        return data;
    }

    static byte[] OracleDecompress(byte[] frame, int expectedLength)
    {
        var decoder = new FrameDecoder();
        try
        {
            var destination = new byte[expectedLength + 64];
            var status = decoder.Decompress(frame, destination, out var consumed, out var written);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(frame.Length, consumed);
            return destination.AsSpan(0, written).ToArray();
        }
        finally
        {
            decoder.Dispose();
        }
    }

    // a frame with no content size in its header, the way streamed input comes out of the zstd tool: compress in a
    // non-final call first, so the encoder cannot pledge the total
    static byte[] UnsizedFrame(byte[] plain)
    {
        var encoder = new FrameEncoder(3);
        try
        {
            var destination = new byte[plain.Length + 1024];
            var source = plain.AsSpan();
            var written = 0;
            while (!source.IsEmpty)
            {
                var status = encoder.Compress(source, destination.AsSpan(written), out var consumed, out var produced, isFinalBlock: false);
                Assert.NotEqual(OperationStatus.InvalidData, status);
                source = source.Slice(consumed);
                written += produced;
            }
            var final = encoder.Compress(ReadOnlySpan<byte>.Empty, destination.AsSpan(written), out _, out var tail, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, final);
            written += tail;
            return destination.AsSpan(0, written).ToArray();
        }
        finally
        {
            encoder.Dispose();
        }
    }

    static bool CarriesContentSize(byte[] frame)
    {
#if NET11_0_OR_GREATER
        // ZstandardDecoder.TryGetMaxDecompressedLength is ZSTD_decompressBound: it answers with the window-size bound
        // for a frame that carries no content size, so it cannot tell the two apart. Read the Frame_Header_Descriptor
        // instead (RFC 8878 3.1.1.1): a content size field is present when Frame_Content_Size_flag is non-zero or
        // Single_Segment_flag is set
        var descriptor = frame[4];
        return (descriptor >> 6) != 0 || (descriptor & 0x20) != 0;
#else
        return Zstandard.TryGetFrameContentSize(frame, out _);
#endif
    }

    [Fact]
    public void Message_IsAStandardZstandardFrame()
    {
        var big = BigCompressible();
        var plain = V4.Serialize(big, V4Options.Default);
        var framed = V4.Serialize(big, Options);

        Assert.Equal(Magic, framed.AsSpan(0, 4).ToArray());
        Assert.True(framed.Length < plain.Length / 2, $"compression should pay on this payload: {framed.Length} vs {plain.Length}");
        Assert.True(CarriesContentSize(framed)); // the content size travels in the header
        Assert.Equal(plain, OracleDecompress(framed, plain.Length)); // the frame decoder reads it without knowing MessagePack exists
        Assert.Equal(big, V4.Deserialize<int[]>(framed, Options));

        // no threshold: a tiny message is a frame too
        var one = V4.Serialize(1, Options);
        Assert.Equal(Magic, one.AsSpan(0, 4).ToArray());
        Assert.Equal(1, V4.Deserialize<int>(one, Options));
    }

    [Fact]
    public void Levels_AllRoundtrip_AndHigherLevelsAreNotLarger()
    {
        var big = BigCompressible();
        int? previous = null;
        foreach (var level in new[] { 1, 3, 9, 19 })
        {
            var options = V4Options.Default.WithZstandardFrame(level);
            var framed = V4.Serialize(big, options);
            Assert.Equal(big, V4.Deserialize<int[]>(framed, options));
            // the read side does not depend on the level the frame was written at
            Assert.Equal(big, V4.Deserialize<int[]>(framed, Options));
            if (previous is { } p)
            {
                Assert.True(framed.Length <= p, $"level {level} produced {framed.Length} bytes, more than the previous level's {p}");
            }
            previous = framed.Length;
        }
    }

    [Fact]
    public void Incompressible_IsStillAFrame()
    {
        // large random bin: zstd stores it as raw blocks (a few bytes of block headers over the input); the frame is
        // written anyway, there is no content-dependent switch
        var rand = new Random(42);
        var blob = new byte[100_000];
        rand.NextBytes(blob);
        var plain = V4.Serialize(blob, V4Options.Default);
        var framed = V4.Serialize(blob, Options);
        Assert.Equal(Magic, framed.AsSpan(0, 4).ToArray());
        Assert.True(framed.Length > plain.Length);
        Assert.Equal(plain, OracleDecompress(framed, plain.Length));
        Assert.Equal(blob, V4.Deserialize<byte[]>(framed, Options));
    }

    [Fact]
    public void BufferWriterEntry_MatchesArrayEntry()
    {
        var big = BigCompressible();
        var expected = V4.Serialize(big, Options);
        var writer = new ArrayBufferWriter<byte>();
        V4.Serialize(writer, big, Options);
        Assert.True(writer.WrittenSpan.SequenceEqual(expected));
    }

    [Fact]
    public void SequenceEntry_ReadsAtEverySplit()
    {
        // small-but-compressed payload so the every-position split stays cheap
        var text = new string('x', 300);
        var framed = V4.Serialize(text, Options);
        for (int splitAt = 1; splitAt < framed.Length; splitAt++)
        {
            Assert.Equal(text, V4.Deserialize<string>(Split(framed, splitAt), Options));
        }
    }

    [Fact]
    public void CorruptFrame_ThrowsSanctioned()
    {
        var text = new string('x', 300);
        var framed = V4.Serialize(text, Options);
        // clobber the frame past its header (magic, descriptor, content size, first block header)
        var corrupt = (byte[])framed.Clone();
        for (int i = 16; i < corrupt.Length - 4; i++) corrupt[i] ^= 0x5a;
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string>(corrupt, Options));
        // and the processor stays usable afterwards (no pooled buffer was leaked or double-returned, no poisoned
        // cached context)
        Assert.Equal(text, V4.Deserialize<string>(framed, Options));
    }

    [Fact]
    public void TruncatedFrame_Rejected()
    {
        var text = new string('x', 300);
        var framed = V4.Serialize(text, Options);
        var truncated = framed.AsSpan(0, framed.Length - 8).ToArray();
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string>(truncated, Options));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<string>(Split(truncated, 10), Options));
    }

    [Fact]
    public void ForeignFrame_WithoutContentSize_IsRead()
    {
        var big = BigCompressible();
        var plain = V4.Serialize(big, V4Options.Default);
        var foreign = UnsizedFrame(plain);
        Assert.False(CarriesContentSize(foreign));

        Assert.Equal(big, V4.Deserialize<int[]>(foreign, Options));
        Assert.Equal(big, V4.Deserialize<int[]>(Split(foreign, foreign.Length / 3), Options));
        Assert.Equal(big, V4.Deserialize<int[]>(new MemoryStream(foreign), Options));
    }

    [Fact]
    public void PlainMessagePack_IsRejected()
    {
        var plain = V4.Serialize(BigCompressible(), V4Options.Default);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(plain, Options));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(Split(plain, 10), Options));
    }

    [Fact]
    public void Cap_IsEnforced_FromTheHeaderAndWhileGrowing()
    {
        var plain = V4.Serialize(BigCompressible(), V4Options.Default);
        var small = V4Options.Default.WithZstandardFrame(3, maxDecompressedSize: 1024);

        var sized = V4.Serialize(BigCompressible(), Options);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(sized, small)); // from the header
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(UnsizedFrame(plain), small)); // while growing
    }

    [Fact]
    public async Task Pipe_FramesAreDelimitedByTheirHeaders()
    {
        var values = new[] { BigCompressible(), new[] { 1, 2, 3 }, Enumerable.Range(0, 5000).ToArray(), Array.Empty<int>() };
        var stream = new MemoryStream();
        foreach (var value in values)
        {
            V4.Serialize(stream, value, Options);
        }
        var bytes = stream.ToArray();

        var pipe = new Pipe();
        var reading = ReadAll(pipe.Reader);
        for (var offset = 0; offset < bytes.Length; offset += 777)
        {
            await pipe.Writer.WriteAsync(bytes.AsMemory(offset, Math.Min(777, bytes.Length - offset)));
        }
        await pipe.Writer.CompleteAsync();
        var results = await reading;
        Assert.Equal(values.Length, results.Count);
        for (var i = 0; i < values.Length; i++)
        {
            Assert.Equal(values[i], results[i]);
        }

        var single = new Pipe();
        await single.Writer.WriteAsync(bytes);
        await single.Writer.CompleteAsync();
        Assert.Equal(values[0], await V4.DeserializeAsync<int[]>(single.Reader, Options));
        var rest = await single.Reader.ReadAsync();
        Assert.Equal(Magic, rest.Buffer.Slice(0, 4).ToArray()); // the next frame starts here
    }

    [Fact]
    public async Task Pipe_CorruptFrameHeader_IsRejectedFromTheHeader()
    {
        // the reserved bit of the frame header descriptor is refused by the header walk before any payload is waited for
        var corrupt = V4.Serialize(new[] { 1, 2, 3 }, Options);
        corrupt[4] |= 0x08;
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(corrupt);
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await V4.DeserializeAsync<int[]>(pipe.Reader, Options));
    }

    [Fact]
    public async Task SkippableFrame_IsSteppedOver_AtEverySplit()
    {
        // a skippable frame (user data the command-line tool may emit) in front of a frame, then a second frame: the
        // header walk steps over the skippable frame and ends exactly where the first frame ends whatever the buffer
        // segmentation, reports a lower bound while the buffer is still short, and the decoder never sees the
        // skippable bytes
        var firstValue = Enumerable.Range(0, 5000).ToArray();
        var first = V4.Serialize(firstValue, Options);
        var second = V4.Serialize(new[] { 1, 2, 3 }, Options);
        var message = Concat(SkippableFrame(37), first);
        var bytes = Concat(message, second);
        var processor = new ZstandardFrameProcessor();
        for (var available = 1; available <= bytes.Length; available++)
        {
            var found = processor.TryFindMessageEnd(Segmented(bytes, available, 64), out var length);
            if (available < message.Length)
            {
                Assert.False(found, $"at {available}");
                Assert.True(length > available && length <= message.Length, $"at {available}: {length}");
            }
            else
            {
                Assert.True(found, $"at {available}");
                Assert.Equal(message.Length, length);
            }
        }

        Assert.Equal(firstValue, V4.Deserialize<int[]>(message, Options));
        Assert.Equal(firstValue, V4.Deserialize<int[]>(Segmented(message, message.Length, 64), Options));
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(bytes);
        await pipe.Writer.CompleteAsync();
        var results = await ReadAll(pipe.Reader);
        Assert.Equal(2, results.Count);
        Assert.Equal(firstValue, results[0]);
        Assert.Equal(new[] { 1, 2, 3 }, results[1]);
    }

    static byte[] SkippableFrame(int payloadLength)
    {
        var frame = new byte[8 + payloadLength];
        BinaryPrimitives.WriteUInt32LittleEndian(frame, 0x184D2A5A);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(4), (uint)payloadLength);
        frame.AsSpan(8).Fill(0xAB);
        return frame;
    }

    static byte[] Concat(byte[] left, byte[] right) => [.. left, .. right];

    static ReadOnlySequence<byte> Segmented(byte[] bytes, int length, int segmentSize)
    {
        var first = new ZstdSegment(bytes.AsMemory(0, Math.Min(segmentSize, length)));
        var last = first;
        for (var offset = first.Memory.Length; offset < length; offset += segmentSize)
        {
            last = last.Append(bytes.AsMemory(offset, Math.Min(segmentSize, length - offset)));
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    static async Task<List<int[]>> ReadAll(PipeReader reader)
    {
        var results = new List<int[]>();
        await foreach (var value in V4.DeserializeMessagesAsync<int[]>(reader, Options))
        {
            results.Add(value);
        }
        return results;
    }

    static ReadOnlySequence<byte> Split(byte[] bytes, int at)
    {
        var first = new ZstdSegment(bytes.AsMemory(0, at));
        var last = first.Append(bytes.AsMemory(at));
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    sealed class ZstdSegment : ReadOnlySequenceSegment<byte>
    {
        public ZstdSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public ZstdSegment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new ZstdSegment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}
