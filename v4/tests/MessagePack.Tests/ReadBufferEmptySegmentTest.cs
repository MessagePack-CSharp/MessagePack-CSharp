using System.Buffers;
using SerializerFoundation;
using Xunit;

namespace MessagePack.Tests;

// Empty segments are legal in a ReadOnlySequence (a split-at-0, a flushed-but-empty
// pipe segment, hand-built segment chains). The sequence buffers must WALK past them
// and serve the next non-empty segment whole — an empty window would be misread as
// truncation by the primitives, and the previous minimum-1-byte stitch fallback paid a
// pointless copy (plus a pool rent without scratch) and a re-stitch on the retry.
public class ReadBufferEmptySegmentTest
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

    static ReadOnlySequence<byte> Chain(params byte[][] segments)
    {
        var first = new Segment(segments[0]);
        var last = first;
        for (var i = 1; i < segments.Length; i++)
        {
            last = last.Append(segments[i]);
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    static byte[] Bytes(int start, int count)
    {
        var data = new byte[count];
        for (var i = 0; i < count; i++) data[i] = (byte)(start + i);
        return data;
    }

    [Fact]
    public void EmptyLeadingSegment_ServesNextSegmentWhole()
    {
        var sequence = Chain([], Bytes(0, 20));

        var buffer = new ReadOnlySequenceReadBuffer(in sequence);
        try
        {
            var span = buffer.GetCurrentSpan();
            Assert.Equal(20, span.Length); // the WHOLE segment, not a 1-byte stitch
            Assert.Equal(0, span[0]);
            Assert.Equal(19, span[19]);
        }
        finally { buffer.Dispose(); }
    }

    [Fact]
    public void ConsecutiveEmptySegments_AreWalkedInOneGo()
    {
        var sequence = Chain([], [], [], Bytes(7, 12));

        var buffer = new ReadOnlySequenceReadBuffer(in sequence);
        try
        {
            Assert.True(buffer.TryGetSpan(12, out var span));
            Assert.Equal(12, span.Length);
            Assert.Equal(7, span[0]);
        }
        finally { buffer.Dispose(); }
    }

    [Fact]
    public void EmptyMiddleSegment_IsWalkedAfterConsumingTheFirst()
    {
        var sequence = Chain(Bytes(0, 8), [], Bytes(8, 12));

        var buffer = new ReadOnlySequenceReadBuffer(in sequence);
        try
        {
            Assert.True(buffer.TryGetSpan(8, out var first));
            Assert.Equal(0, first[0]);
            buffer.Advance(8);

            var second = buffer.GetCurrentSpan();
            Assert.Equal(12, second.Length); // whole third segment, empty one skipped
            Assert.Equal(8, second[0]);
            Assert.Equal(8, buffer.BytesConsumed); // empty segments moved no bookkeeping
        }
        finally { buffer.Dispose(); }
    }

    [Fact]
    public void StitchAcrossEmptySegment_StillWorks()
    {
        // a straddling request spanning [3B][empty][rest] must stitch through the seam
        var sequence = Chain(Bytes(0, 3), [], Bytes(3, 9));

        var buffer = new ReadOnlySequenceReadBuffer(in sequence);
        try
        {
            Assert.True(buffer.TryGetSpan(5, out var span)); // > first segment: stitched
            Assert.True(span.Length >= 5);
            for (var i = 0; i < 5; i++)
            {
                Assert.Equal(i, span[i]);
            }
        }
        finally { buffer.Dispose(); }
    }

    [Fact]
    public void Exhausted_AfterTrailingEmptySegments_ReportsEmpty()
    {
        var sequence = Chain(Bytes(0, 4), [], []);

        var buffer = new ReadOnlySequenceReadBuffer(in sequence);
        try
        {
            buffer.Advance(4);
            Assert.True(buffer.GetCurrentSpan().IsEmpty); // exhausted, not misread as data
            Assert.Equal(0, buffer.BytesRemaining);
        }
        finally { buffer.Dispose(); }
    }

    [Fact]
    public void CompatibleVariant_WalksEmptySegmentsToo()
    {
        var sequence = Chain([], [], Bytes(5, 10));

        var buffer = new CompatibleReadOnlySequenceReadBuffer(in sequence);
        try
        {
            var span = buffer.GetCurrentSpan();
            Assert.Equal(10, span.Length);
            Assert.Equal(5, span[0]);
        }
        finally { buffer.Dispose(); }
    }
}
