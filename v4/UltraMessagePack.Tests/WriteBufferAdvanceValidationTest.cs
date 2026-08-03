using System.Buffers;
using SerializerFoundation;
using Xunit;

namespace UltraMessagePack.Tests;

// Advance validation contract: a single unsigned compare per Advance rejects negative
// values and past-the-window values (both would otherwise corrupt bookkeeping and turn
// the unchecked CreateSpan fast path into an out-of-bounds span). Exactly-remaining is
// legal. Ref structs cannot be captured by Assert.Throws lambdas, so tests use
// try/catch flags.
public class WriteBufferAdvanceValidationTest
{
    // ---- FixedSpanWriteBuffer ----

    [Theory]
    [InlineData(-1)]           // (uint)-1 = 0xFFFFFFFF: the largest unsigned image
    [InlineData(int.MinValue)] // (uint)int.MinValue = 0x80000000: the SMALLEST unsigned
                               // image of any negative — the boundary closest to slipping
                               // past remaining (max 0x7FFFFFFF)
    public void FixedSpan_RejectsNegative(int bytesWritten)
    {
        var thrown = false;
        try
        {
            var buffer = new SpanWriteBuffer(new byte[16]);
            buffer.Advance(bytesWritten);
        }
        catch (InvalidOperationException) { thrown = true; }
        Assert.True(thrown);
    }

    [Fact]
    public void FixedSpan_RejectsPastTheWindow_AcceptsExactFill()
    {
        var thrown = false;
        try
        {
            var buffer = new SpanWriteBuffer(new byte[16]);
            buffer.Advance(16); // exact fill: legal
            Assert.Equal(16, buffer.BytesWritten);
            buffer.Advance(1);  // past the end: rejected
        }
        catch (InvalidOperationException) { thrown = true; }
        Assert.True(thrown);
    }

    [Fact]
    public unsafe void UnsafeFixed_RejectsNegativeAndPastTheWindow()
    {
        var negative = false;
        var past = false;
        var data = new byte[16];
        fixed (byte* p = data)
        {
            try
            {
                var buffer = new UnsafeSpanWriteBuffer(p, data.Length);
                buffer.Advance(-1);
            }
            catch (InvalidOperationException) { negative = true; }

            try
            {
                var buffer = new UnsafeSpanWriteBuffer(p, data.Length);
                buffer.Advance(16);
                buffer.Advance(1);
            }
            catch (InvalidOperationException) { past = true; }
        }
        Assert.True(negative);
        Assert.True(past);
    }

    // ---- ArrayPoolListWriteBuffer ----

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void ArrayPoolList_RejectsNegative(int bytesWritten)
    {
        var thrown = false;
        var buffer = new ArrayPoolListWriteBuffer(stackalloc byte[16]);
        try
        {
            buffer.Advance(bytesWritten);
        }
        catch (InvalidOperationException) { thrown = true; }
        finally { buffer.Dispose(); }
        Assert.True(thrown);
    }

    [Fact]
    public void ArrayPoolList_RejectsAdvanceBeforeAnyWindow()
    {
        // default scratch: no window exists yet, any nonzero Advance must throw
        var thrown = false;
        var buffer = new ArrayPoolListWriteBuffer(default);
        try
        {
            buffer.Advance(1);
        }
        catch (InvalidOperationException) { thrown = true; }
        finally { buffer.Dispose(); }
        Assert.True(thrown);
    }

    [Fact]
    public void ArrayPoolList_RejectsPastTheWindow_OnScratchAndPooledSegments()
    {
        var scratchPast = false;
        var pooledPast = false;

        var buffer = new ArrayPoolListWriteBuffer(stackalloc byte[16]);
        try
        {
            try
            {
                buffer.Advance(17); // scratch window is 16
            }
            catch (InvalidOperationException) { scratchPast = true; }

            // spill to a pooled segment, then overrun that window by one
            var window = buffer.GetSpan(64); // > scratch: rents a pooled segment
            try
            {
                buffer.Advance(window.Length + 1);
            }
            catch (InvalidOperationException) { pooledPast = true; }

            // the buffer is still usable with valid advances after rejected ones
            buffer.Advance(window.Length);
            Assert.Equal(window.Length, buffer.BytesWritten);
        }
        finally { buffer.Dispose(); }

        Assert.True(scratchPast);
        Assert.True(pooledPast);
    }

    [Fact]
    public void CompatibleArrayPoolList_RejectsNegativeAndPastTheWindow()
    {
        var negative = false;
        var beforeAnyWindow = false;
        var pooledPast = false;

        var buffer = new CompatibleArrayPoolListWriteBuffer();
        try
        {
            try
            {
                buffer.Advance(-1);
            }
            catch (InvalidOperationException) { negative = true; }

            // no window rented yet: capacity is 0, any nonzero Advance must throw
            try
            {
                buffer.Advance(1);
            }
            catch (InvalidOperationException) { beforeAnyWindow = true; }

            var window = buffer.GetSpan(64);
            try
            {
                buffer.Advance(window.Length + 1);
            }
            catch (InvalidOperationException) { pooledPast = true; }

            buffer.Advance(window.Length);
            Assert.Equal(window.Length, buffer.BytesWritten);
        }
        finally { buffer.Dispose(); }

        Assert.True(negative);
        Assert.True(beforeAnyWindow);
        Assert.True(pooledPast);
    }

    // ---- negative-capacity constructors (PointerSpan invariant) ----
    // the Advance guards fold their sign check into one unsigned compare against
    // `capacity - written`, which is only sound when capacity cannot be negative;
    // for the pointer-tier buffers that base case is established by the PointerSpan ctor

    [Fact]
    public unsafe void UnsafeFixed_RejectsNegativeLengthAtConstruction()
    {
        var thrown = false;
        var data = new byte[16];
        fixed (byte* p = data)
        {
            try
            {
                var buffer = new UnsafeSpanWriteBuffer(p, -1);
            }
            catch (ArgumentOutOfRangeException) { thrown = true; }
        }
        Assert.True(thrown);
    }

    // ---- BufferWriterWriteBuffer ----

    [Fact]
    public void BufferWriter_RejectsNegativeAndPastTheWindow_AndAfterFlush()
    {
        var negative = false;
        var past = false;
        var afterFlush = false;

        var writer = new ArrayBufferWriter<byte>(64);
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            try
            {
                buffer.Advance(-1);
            }
            catch (InvalidOperationException) { negative = true; }

            var window = buffer.GetSpan(8);
            try
            {
                buffer.Advance(window.Length + 1);
            }
            catch (InvalidOperationException) { past = true; }

            buffer.Advance(3);
            buffer.Flush(); // window dropped: any nonzero Advance must throw until the next GetSpan
            try
            {
                buffer.Advance(1);
            }
            catch (InvalidOperationException) { afterFlush = true; }
        }
        finally { buffer.Dispose(); }

        Assert.True(negative);
        Assert.True(past);
        Assert.True(afterFlush);
        Assert.Equal(3, writer.WrittenCount);
    }

    [Fact]
    public void CompatibleBufferWriter_RejectsNegativeAndPastTheWindow()
    {
        var negative = false;
        var past = false;

        var writer = new ArrayBufferWriter<byte>(64);
        var buffer = new CompatibleBufferWriterWriteBuffer(writer);
        try
        {
            try
            {
                buffer.Advance(-1);
            }
            catch (InvalidOperationException) { negative = true; }

            var window = buffer.GetSpan(8);
            try
            {
                buffer.Advance(window.Length + 1);
            }
            catch (InvalidOperationException) { past = true; }

            buffer.Advance(window.Length); // exact fill is legal
            Assert.Equal(window.Length, buffer.BytesWritten);
        }
        finally { buffer.Dispose(); }

        Assert.True(negative);
        Assert.True(past);
    }

    // ---- negative sizeHint: ArgumentOutOfRangeException on every implementation ----
    // a negative sizeHint is always a caller bug (typically an overflowed size
    // computation); clamping would hand back a small window and convert the upstream
    // overflow into an unchecked overrun. The unsigned capacity guard routes negatives
    // to the cold path, so the check costs nothing on the hot path.

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void GetSpan_RejectsNegativeSizeHint_OnEveryWriteBuffer(int sizeHint)
    {
        var results = new List<(string Buffer, bool Thrown)>();

        try
        {
            var buffer = new SpanWriteBuffer(new byte[16]);
            buffer.GetSpan(sizeHint);
            results.Add(("FixedSpan", false));
        }
        catch (ArgumentOutOfRangeException) { results.Add(("FixedSpan", true)); }

        unsafe
        {
            var data = new byte[16];
            fixed (byte* p = data)
            {
                try
                {
                    var buffer = new UnsafeSpanWriteBuffer(p, data.Length);
                    buffer.GetSpan(sizeHint);
                    results.Add(("UnsafeFixed", false));
                }
                catch (ArgumentOutOfRangeException) { results.Add(("UnsafeFixed", true)); }
            }
        }

        {
            var buffer = new ArrayPoolListWriteBuffer(stackalloc byte[16]);
            try
            {
                buffer.GetSpan(sizeHint);
                results.Add(("ArrayPoolList", false));
            }
            catch (ArgumentOutOfRangeException) { results.Add(("ArrayPoolList", true)); }
            finally { buffer.Dispose(); }
        }

        {
            var buffer = new CompatibleArrayPoolListWriteBuffer();
            try
            {
                buffer.GetSpan(sizeHint);
                results.Add(("CompatibleArrayPoolList", false));
            }
            catch (ArgumentOutOfRangeException) { results.Add(("CompatibleArrayPoolList", true)); }
            finally { buffer.Dispose(); }
        }

        {
            var buffer = new BufferWriterWriteBuffer(new ArrayBufferWriter<byte>(64));
            try
            {
                buffer.GetSpan(sizeHint);
                results.Add(("BufferWriter", false));
            }
            catch (ArgumentOutOfRangeException) { results.Add(("BufferWriter", true)); }
            finally { buffer.Dispose(); }
        }

        {
            var buffer = new CompatibleBufferWriterWriteBuffer(new ArrayBufferWriter<byte>(64));
            try
            {
                buffer.GetSpan(sizeHint);
                results.Add(("CompatibleBufferWriter", false));
            }
            catch (ArgumentOutOfRangeException) { results.Add(("CompatibleBufferWriter", true)); }
            finally { buffer.Dispose(); }
        }

        Assert.All(results, static r => Assert.True(r.Thrown, $"{r.Buffer} did not throw"));
        Assert.Equal(6, results.Count);
    }
}
