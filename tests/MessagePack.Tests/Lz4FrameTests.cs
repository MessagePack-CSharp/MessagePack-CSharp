using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Numerics;
using MessagePack;
using NativeCompressions;
using Xunit;
using V4Options = MessagePack.MessagePackSerializerOptions;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Lz4FrameProcessor: every message is one standard LZ4 frame with no MessagePack envelope. The binding's own frame
// decoder stands in for "any LZ4 implementation" as the interop oracle, and the async entries find frame boundaries
// through the processor's header walk instead of the MessagePack scanner.
public class Lz4FrameTests
{
    static V4Options Options { get; } = V4Options.Default.WithLz4Frame();

    static readonly byte[] Magic = [0x04, 0x22, 0x4D, 0x18];

    static int[] BigCompressible()
    {
        var data = new int[100_000];
        for (int i = 0; i < data.Length; i++) data[i] = i % 100;
        return data;
    }

    [Fact]
    public void Message_IsAStandardLz4Frame()
    {
        var big = BigCompressible();
        var plain = V4.Serialize(big, V4Options.Default);
        var framed = V4.Serialize(big, Options);

        Assert.Equal(Magic, framed.AsSpan(0, 4).ToArray());
        Assert.True(framed.Length < plain.Length / 2, $"compression should pay on this payload: {framed.Length} vs {plain.Length}");
        Assert.True(LZ4.TryGetFrameInfo(framed, out var info));
        Assert.Equal((ulong)plain.Length, info.ContentSize); // the content size travels in the header
        Assert.Equal(plain, LZ4.Decompress(framed)); // the frame decoder reads it without knowing MessagePack exists
        Assert.Equal(big, V4.Deserialize<int[]>(framed, Options));

        // no threshold: a tiny message is a frame too
        var one = V4.Serialize(1, Options);
        Assert.Equal(Magic, one.AsSpan(0, 4).ToArray());
        Assert.Equal(1, V4.Deserialize<int>(one, Options));
    }

    [Fact]
    public void ForeignFrame_WithoutContentSize_IsRead()
    {
        // the lz4 tool's default: no content size in the header, so the reader grows under its cap
        var big = BigCompressible();
        var plain = V4.Serialize(big, V4Options.Default);
        var foreign = CompressWithoutContentSize(plain);
        Assert.True(LZ4.TryGetFrameInfo(foreign, out var info));
        Assert.Equal(0UL, info.ContentSize);

        Assert.Equal(big, V4.Deserialize<int[]>(foreign, Options));
        Assert.Equal(big, V4.Deserialize<int[]>(Split(foreign, foreign.Length / 3), Options));
        Assert.Equal(big, V4.Deserialize<int[]>(new MemoryStream(foreign), Options));
    }

    [Fact]
    public void PlainMessagePack_IsRejected()
    {
        // strict: the frame processor never passes anything through, so the magic can never be confused with a
        // MessagePack value that happens to start with the same bytes
        var plain = V4.Serialize(BigCompressible(), V4Options.Default);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(plain, Options));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(Split(plain, 10), Options));
    }

    [Fact]
    public void Cap_IsEnforced_FromTheHeaderAndWhileGrowing()
    {
        var plain = V4.Serialize(BigCompressible(), V4Options.Default);
        var small = V4Options.Default.WithLz4Frame(maxDecompressedSize: 1024);

        var sized = V4.Serialize(BigCompressible(), Options);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(sized, small)); // from the header

        var unsized = CompressWithoutContentSize(plain);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<int[]>(unsized, small)); // while growing
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

        // the concatenation arrives in odd-sized pieces with the writer never completing until the end: every
        // boundary has to come from the frame headers
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

        // the single-message entry leaves the following frames unconsumed
        var single = new Pipe();
        await single.Writer.WriteAsync(bytes);
        await single.Writer.CompleteAsync();
        Assert.Equal(values[0], await V4.DeserializeAsync<int[]>(single.Reader, Options));
        var rest = await single.Reader.ReadAsync();
        Assert.Equal(Magic, rest.Buffer.Slice(0, 4).ToArray()); // the next frame starts here
    }

    [Fact]
    public async Task Pipe_BogusBlockSize_IsRejectedFromTheHeader()
    {
        // a frame whose first block claims 1MB on a 64KB-block frame is refused by the header walk, before any of
        // the claimed bytes are waited for
        var frame = V4.Serialize(new[] { 1, 2, 3 }, Options);
        Assert.True(LZ4.TryGetFrameInfo(frame, out var info));
        Assert.Equal(BlockSizeId.Max64KB, info.BlockSizeID);
        var headerLength = 4 + 2 + 8 + 1; // magic, FLG/BD, content size, header checksum
        var corrupt = (byte[])frame.Clone();
        corrupt[headerLength] = 0x00;
        corrupt[headerLength + 1] = 0x00;
        corrupt[headerLength + 2] = 0x10; // 0x00100000 = 1MB
        corrupt[headerLength + 3] = 0x00;

        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(corrupt);
        await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await V4.DeserializeAsync<int[]>(pipe.Reader, Options));
    }

    [Fact]
    public async Task DeserializeElementsAsync_IsNotSupported()
    {
        var pipe = new Pipe();
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await foreach (var _ in V4.DeserializeElementsAsync<int>(pipe.Reader, Options))
            {
            }
        });
    }

    // The binding pledges the input length on every Compress, so a frame without a content size (the lz4 tool's
    // default) is made by hand: clear the C.Size flag, drop the 8-byte field and recompute the header checksum (the
    // second byte of xxHash32 over the descriptor). Block data and the content checksum are unchanged.
    static byte[] CompressWithoutContentSize(byte[] plain)
    {
        var options = new LZ4CompressionOptions();
        var destination = new byte[19 + plain.Length + 8 * (plain.Length / 65536 + 1) + 8];
        var length = LZ4.Compress(plain, destination, in options);
        var sized = destination.AsSpan(0, length);
        Assert.Equal(0x08, sized[4] & 0x08); // C.Size set
        Assert.Equal(0, sized[4] & 0x01); // no dictionary id
        var unsized = new byte[length - 8];
        sized.Slice(0, 6).CopyTo(unsized);
        unsized[4] &= unchecked((byte)~0x08);
        unsized[6] = (byte)(XxHash32(unsized.AsSpan(4, 2)) >> 8);
        sized.Slice(15).CopyTo(unsized.AsSpan(7)); // past magic(4) + FLG/BD(2) + content size(8) + HC(1)
        return unsized;
    }

    static uint XxHash32(ReadOnlySpan<byte> data)
    {
        const uint Prime1 = 2654435761, Prime2 = 2246822519, Prime3 = 3266489917, Prime4 = 668265263, Prime5 = 374761393;
        var h = Prime5 + (uint)data.Length; // seed 0, input shorter than 16 bytes
        var i = 0;
        for (; i + 4 <= data.Length; i += 4)
        {
            h += BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(i)) * Prime3;
            h = BitOperations.RotateLeft(h, 17) * Prime4;
        }
        for (; i < data.Length; i++)
        {
            h += data[i] * Prime5;
            h = BitOperations.RotateLeft(h, 11) * Prime1;
        }
        h ^= h >> 15;
        h *= Prime2;
        h ^= h >> 13;
        h *= Prime3;
        h ^= h >> 16;
        return h;
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
        var processor = Lz4Compression.Frame;
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
        var first = new Segment(bytes.AsMemory(0, Math.Min(segmentSize, length)));
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
        var first = new Segment(bytes.AsMemory(0, at));
        var last = first.Append(bytes.AsMemory(at));
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}
