using System.Buffers;
using SerializerFoundation;
using Xunit;

namespace MessagePack.Tests;

// Read-side Advance contract (the write side's single-unsigned-compare shape, with the
// bound widened from the window to the DATA): negative values and advancing past the
// end of the data both throw InvalidOperationException; advancing past the current
// WINDOW stays legal on the sequence buffers (skip semantics — the payload crosses
// segments and the next GetSpan repositions). Truncation of malformed input is detected
// by the extension layer BEFORE advancing, so the guard only fires on contract bugs.
public class ReadBufferAdvanceValidationTest
{
    static ReadOnlySequence<byte> TwoSegments(byte[] data, int splitAt)
    {
        var second = new Segment(data.AsMemory(splitAt), null, splitAt);
        var first = new Segment(data.AsMemory(0, splitAt), second, 0);
        return new ReadOnlySequence<byte>(first, 0, second, data.Length - splitAt);
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

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(17)]           // past the end of 16 bytes of data
    [InlineData(int.MaxValue)]
    public void Advance_RejectsNegativeAndPastTheData_OnEveryReadBuffer(int bytesConsumed)
    {
        var data = new byte[16];

        var spanThrown = false;
        try
        {
            var buffer = new ReadOnlySpanReadBuffer(data);
            buffer.Advance(bytesConsumed);
        }
        catch (InvalidOperationException) { spanThrown = true; }
        Assert.True(spanThrown, "ReadOnlySpanReadBuffer");

        var unsafeThrown = false;
        unsafe
        {
            fixed (byte* p = data)
            {
                try
                {
                    var buffer = new CompatibleReadOnlySpanReadBuffer(p, data.Length);
                    buffer.Advance(bytesConsumed);
                }
                catch (InvalidOperationException) { unsafeThrown = true; }
            }
        }
        Assert.True(unsafeThrown, "UnsafeReadBuffer");

        var sequenceThrown = false;
        {
            var buffer = new ReadOnlySequenceReadBuffer(new ReadOnlySequence<byte>(data));
            try
            {
                buffer.Advance(bytesConsumed);
            }
            catch (InvalidOperationException) { sequenceThrown = true; }
            finally { buffer.Dispose(); }
        }
        Assert.True(sequenceThrown, "ReadOnlySequenceReadBuffer");

        var compatibleThrown = false;
        {
            var buffer = new CompatibleReadOnlySequenceReadBuffer(new ReadOnlySequence<byte>(data));
            try
            {
                buffer.Advance(bytesConsumed);
            }
            catch (InvalidOperationException) { compatibleThrown = true; }
            finally { buffer.Dispose(); }
        }
        Assert.True(compatibleThrown, "CompatibleReadOnlySequenceReadBuffer");
    }

    // the bound is the DATA remaining (a long): once more than 2 GB remain, a negative argument
    // zero-extended through (uint) would fit under it (int.MinValue past 2 GB, -1 past 4 GB), so
    // the guard must sign-extend. The sequence is one 64 KB block linked 2^16 + 1 times (no real
    // 4 GB allocation).
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Advance_RejectsNegative_WhenMoreThan4GBRemain(int bytesConsumed)
    {
        var block = new byte[64 * 1024];
        var segmentCount = (1 << 16) + 1;
        Segment? next = null;
        Segment? last = null;
        for (var i = segmentCount - 1; i >= 0; i--)
        {
            next = new Segment(block, next, (long)i * block.Length);
            last ??= next;
        }
        var sequence = new ReadOnlySequence<byte>(next!, 0, last!, block.Length);
        Assert.True(sequence.Length > uint.MaxValue);

        var sequenceThrown = false;
        {
            var buffer = new ReadOnlySequenceReadBuffer(sequence);
            try
            {
                buffer.Advance(bytesConsumed);
            }
            catch (InvalidOperationException) { sequenceThrown = true; }
            finally { buffer.Dispose(); }
        }
        Assert.True(sequenceThrown, "ReadOnlySequenceReadBuffer");

        var compatibleThrown = false;
        {
            var buffer = new CompatibleReadOnlySequenceReadBuffer(sequence);
            try
            {
                buffer.Advance(bytesConsumed);
            }
            catch (InvalidOperationException) { compatibleThrown = true; }
            finally { buffer.Dispose(); }
        }
        Assert.True(compatibleThrown, "CompatibleReadOnlySequenceReadBuffer");
    }

    // a negative sizeHint is a caller bug; the sequence buffers' fast path (data remaining in
    // the current segment) used to accept it while only the slow path rejected it
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void TryGetSpan_RejectsNegativeSizeHint_OnBothSequenceBuffers_FastPathIncluded(int sizeHint)
    {
        var data = new byte[16];

        var sequenceThrown = false;
        {
            var buffer = new ReadOnlySequenceReadBuffer(new ReadOnlySequence<byte>(data));
            try
            {
                buffer.TryGetSpan(sizeHint, out _);
            }
            catch (ArgumentOutOfRangeException) { sequenceThrown = true; }
            finally { buffer.Dispose(); }
        }
        Assert.True(sequenceThrown, "ReadOnlySequenceReadBuffer");

        var compatibleThrown = false;
        {
            var buffer = new CompatibleReadOnlySequenceReadBuffer(new ReadOnlySequence<byte>(data));
            try
            {
                buffer.TryGetSpan(sizeHint, out _);
            }
            catch (ArgumentOutOfRangeException) { compatibleThrown = true; }
            finally { buffer.Dispose(); }
        }
        Assert.True(compatibleThrown, "CompatibleReadOnlySequenceReadBuffer");
    }

    [Fact]
    public void Advance_ExactFillIsLegal_AndGetSpanReportsExhaustion()
    {
        var data = new byte[16];

        var buffer = new ReadOnlySpanReadBuffer(data);
        buffer.Advance(16);                    // exact fill: legal
        Assert.Equal(16, buffer.BytesConsumed);
        Assert.Equal(0, buffer.BytesRemaining);
        Assert.True(buffer.GetCurrentSpan().IsEmpty); // peek: empty, never throws

        // a positive demand on exhausted data is false — the CALLER owns how
        // truncation surfaces, the foundation never throws for it
        Assert.False(buffer.TryGetSpan(1, out _));
    }

    [Fact]
    public void SequenceBuffer_SkipAcrossSegmentBoundary_StillReads()
    {
        // past the current WINDOW but within the DATA must stay legal: skipping across
        // the segment boundary repositions into the next segment
        var data = new byte[20];
        for (var i = 0; i < data.Length; i++) data[i] = (byte)i;

        var buffer = new ReadOnlySequenceReadBuffer(TwoSegments(data, 8));
        try
        {
            buffer.Advance(12); // past the 8-byte first segment/window
            Assert.True(buffer.TryGetSpan(1, out var span));
            Assert.Equal(12, span[0]);

            // and the data-end bound still holds from the repositioned cursor
            var thrown = false;
            try
            {
                buffer.Advance(9); // only 8 bytes remain
            }
            catch (InvalidOperationException) { thrown = true; }
            Assert.True(thrown);
        }
        finally { buffer.Dispose(); }
    }
}
