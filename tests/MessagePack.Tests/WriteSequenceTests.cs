using SerializerFoundation;
using System.Buffers;
using Xunit;

namespace MessagePack.Tests;

// the ReadOnlySequence<byte> overloads of WriteBinary and WriteRaw: byte-identical to their span twins
// whatever the segmentation, and the empty sequence behaves like the empty span
public class WriteSequenceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(65536)]
    public void WriteBinary_MatchesSpanTwin_AcrossSegmentations(int length)
    {
        var payload = Payload(length);
        var expected = Encode((ref BufferWriterWriteBuffer b) => b.WriteBinary(payload));

        foreach (var seq in Segmentations(payload))
        {
            Assert.Equal(expected, Encode((ref BufferWriterWriteBuffer b) => b.WriteBinary(seq)));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(65536)]
    public void WriteRaw_MatchesSpanTwin_AcrossSegmentations(int length)
    {
        var payload = Payload(length);
        var expected = Encode((ref BufferWriterWriteBuffer b) => b.WriteRaw(payload));
        Assert.Equal(payload, expected);

        foreach (var seq in Segmentations(payload))
        {
            Assert.Equal(expected, Encode((ref BufferWriterWriteBuffer b) => b.WriteRaw(seq)));
        }
    }

    [Fact]
    public void WriteBinary_DefaultSequence_IsEmptyBin()
    {
        var bytes = Encode((ref BufferWriterWriteBuffer b) => b.WriteBinary(default(ReadOnlySequence<byte>)));
        Assert.Equal(new byte[] { MessagePackCode.Bin8, 0 }, bytes);

        var reader = new ReadOnlySpanReadBuffer(bytes);
        Assert.Empty(reader.ReadBinary());
        Assert.Equal(0, reader.BytesRemaining);
    }

    [Fact]
    public void WriteBinHeader_ThenWriteRawSequence_RoundTrips()
    {
        var payload = Payload(1000);
        var seq = TwoSegments(payload, 333);

        var bytes = Encode((ref BufferWriterWriteBuffer b) =>
        {
            b.WriteBinHeader(payload.Length);
            b.WriteRaw(seq);
        });

        var reader = new ReadOnlySpanReadBuffer(bytes);
        Assert.Equal(payload, reader.ReadBinary());
        Assert.Equal(0, reader.BytesRemaining);
    }

    // ---------------------------------------------------------------- helpers

    delegate void WriteAction(ref BufferWriterWriteBuffer buffer);

    static byte[] Encode(WriteAction write)
    {
        // initial capacity below every header reservation, so large cases cross the writer's own segments too
        var inner = new ArrayBufferWriter<byte>(16);
        var b = new BufferWriterWriteBuffer(inner);
        write(ref b);
        b.Dispose();
        return inner.WrittenSpan.ToArray();
    }

    static byte[] Payload(int length)
    {
        var payload = new byte[length];
        for (int i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)(i * 31 + 7);
        }
        return payload;
    }

    static IEnumerable<ReadOnlySequence<byte>> Segmentations(byte[] data)
    {
        yield return new ReadOnlySequence<byte>(data);
        if (data.Length >= 2)
        {
            yield return TwoSegments(data, 1);
            yield return TwoSegments(data, data.Length / 2);
            yield return TwoSegments(data, data.Length - 1);
        }
        if (data.Length is >= 1 and <= 4096)
        {
            yield return SingleByteSegments(data);
        }
    }

    static ReadOnlySequence<byte> TwoSegments(byte[] data, int splitAt)
    {
        var second = new Segment(data.AsMemory(splitAt), null, splitAt);
        var first = new Segment(data.AsMemory(0, splitAt), second, 0);
        return new ReadOnlySequence<byte>(first, 0, second, data.Length - splitAt);
    }

    static ReadOnlySequence<byte> SingleByteSegments(byte[] data)
    {
        var last = new Segment(data.AsMemory(data.Length - 1, 1), null, data.Length - 1);
        var next = last;
        for (int i = data.Length - 2; i >= 0; i--)
        {
            next = new Segment(data.AsMemory(i, 1), next, i);
        }
        return new ReadOnlySequence<byte>(next, 0, last, 1);
    }

    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, Segment? next, long runningIndex)
        {
            Memory = memory;
            Next = next;
            RunningIndex = runningIndex;
        }
    }
}
