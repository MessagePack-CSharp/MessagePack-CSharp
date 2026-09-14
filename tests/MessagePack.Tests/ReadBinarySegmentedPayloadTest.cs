using System.Buffers;
using SerializerFoundation;
using Xunit;

namespace MessagePack.Tests;

// ReadBinary over a segmented sequence: the payload dwarfs both the segments and any
// scratch tier, so it takes IReadBuffer.CopyTo's straddling path end to end (the CopyTo
// contract itself is pinned in SerializerFoundation.Tests/ReadBufferCopyToTest).
public class ReadBinarySegmentedPayloadTest
{
    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    static ReadOnlySequence<byte> Split(byte[] data, int chunkSize)
    {
        var first = new Segment(data.AsMemory(0, chunkSize));
        var last = first;
        for (var i = chunkSize; i < data.Length; i += chunkSize)
        {
            last = last.Append(data.AsMemory(i, Math.Min(chunkSize, data.Length - i)));
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(1000)]
    public void ReadBinary_LargePayloadAcrossSegments(int chunkSize)
    {
        // the payload dwarfs both the segments and any scratch tier, so it takes the
        // straddling CopyTo path end to end
        var payload = new byte[70000];
        new Random(42).NextBytes(payload);
        var encoded = new byte[5 + payload.Length];
        Assert.True(MessagePackPrimitives.TryWriteBinary(encoded, payload, out var written));
        Assert.Equal(encoded.Length, written);

        Span<byte> scratch = stackalloc byte[64];
        var buffer = new ReadOnlySequenceReadBuffer(Split(encoded, chunkSize), scratch);
        try
        {
            Assert.Equal(payload, buffer.ReadBinary());
            Assert.Equal(0, buffer.BytesRemaining);
        }
        finally { buffer.Dispose(); }
    }

    [Fact]
    public void ReadBinary_Empty_ReturnsEmptyArray()
    {
        var buffer = new ReadOnlySpanReadBuffer([0xc4, 0x00]); // bin8(0)
        Assert.Empty(buffer.ReadBinary());
        Assert.Equal(0, buffer.BytesRemaining);
    }

    [Fact]
    public void ReadBinary_TruncatedPayload_ThrowsDomainException()
    {
        // the header guard catches it before anything is allocated or copied
        Assert.Throws<MessagePackSerializationException>(() =>
        {
            var buffer = new ReadOnlySpanReadBuffer([0xc4, 0x10, 1, 2, 3]); // bin8 claiming 16
            buffer.ReadBinary();
        });
    }
}
