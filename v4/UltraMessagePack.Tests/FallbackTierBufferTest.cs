using System.Buffers;
using System.Runtime.CompilerServices;
using SerializerFoundation;

namespace UltraMessagePack.Tests;

// Runtime coverage for the pin-free fallback tier (the downlevel serializer's buffers,
// Compatible*/Unsafe*; plain structs, so they run on net10.0 too). Focus: the window
// bookkeeping — first-rent, segment growth across pooled segments, and the
// ReadOnlyMemory segment walk with temp stitching.
public class FallbackTierBufferTest
{
    [Fact]
    public void ArrayPoolList_SingleSegment()
    {
        var buffer = new CompatibleArrayPoolListWriteBuffer();
        try
        {
            var span = buffer.GetSpan(10);
            for (var i = 0; i < 10; i++)
            {
                span[i] = (byte)i;
            }
            buffer.Advance(10);

            Assert.Equal(10, buffer.BytesWritten);
            Assert.Equal(Enumerable.Range(0, 10).Select(static x => (byte)x).ToArray(), buffer.ToArray());
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Fact]
    public void ArrayPoolList_SpillsIntoPooledSegments()
    {
        // 300KB: first segment (64KB) + at least one growth segment (128KB, ...)
        var expected = new byte[300_000];
        new Random(7).NextBytes(expected);

        var buffer = new CompatibleArrayPoolListWriteBuffer();
        try
        {
            var written = 0;
            var chunk = 0;
            while (written < expected.Length)
            {
                var size = Math.Min(1 + (chunk++ % 77), expected.Length - written);
                var span = buffer.GetSpan(size);
                expected.AsSpan(written, size).CopyTo(span);
                buffer.Advance(size);
                written += size;
            }

            Assert.Equal(expected.Length, buffer.BytesWritten);
            Assert.Equal(expected, buffer.ToArray());

            var writeTo = new byte[expected.Length];
            buffer.WriteTo(writeTo);
            Assert.Equal(expected, writeTo);

            // the segment iterator (the MessageProcessor.Encode input) must agree too
            var concatenated = new List<byte>(expected.Length);
            var segments = buffer.GetWrittenSegments();
            while (segments.TryGetNext(out var segment))
            {
                concatenated.AddRange(segment.ToArray());
            }
            Assert.Equal(expected, concatenated.ToArray());
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Fact]
    public void ArrayPoolList_GetReferencePathAcrossSegments()
    {
        // 80KB of ints crosses the 64KB first segment, so the ref path must carry into
        // the second pooled segment
        var buffer = new CompatibleArrayPoolListWriteBuffer();
        try
        {
            const int Count = 20_000;
            for (var i = 0; i < Count; i++)
            {
                ref var reference = ref buffer.GetReference(4);
                Unsafe.WriteUnaligned(ref reference, i);
                buffer.Advance(4);
            }

            var result = buffer.ToArray();
            Assert.Equal(Count * 4, result.Length);
            for (var i = 0; i < Count; i++)
            {
                Assert.Equal(i, BitConverter.ToInt32(result, i * 4));
            }
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Fact]
    public void SequenceReadBuffer_WalksSegmentsAndStitches()
    {
        var data = new byte[10_000];
        new Random(11).NextBytes(data);
        // 7-byte segments with 13-byte reads: every window request straddles a boundary
        // and goes through the retained-temp stitch
        var sequence = Split(data, 7);

        var buffer = new CompatibleReadOnlySequenceReadBuffer(in sequence);
        try
        {
            var read = 0;
            var reassembled = new byte[data.Length];
            while (read < data.Length)
            {
                var want = Math.Min(13, data.Length - read);
                Assert.True(buffer.TryGetSpan(want, out var span));
                span.Slice(0, want).CopyTo(reassembled.AsSpan(read));
                buffer.Advance(want);
                read += want;
            }

            Assert.Equal(data, reassembled);
            Assert.Equal(data.Length, buffer.BytesConsumed);
            Assert.Equal(0L, buffer.BytesRemaining);
        }
        finally
        {
            buffer.Dispose();
        }
    }

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
        if (data.Length <= chunkSize)
        {
            return new ReadOnlySequence<byte>(data);
        }
        var first = new Segment(data.AsMemory(0, chunkSize));
        var last = first;
        for (var i = chunkSize; i < data.Length; i += chunkSize)
        {
            last = last.Append(data.AsMemory(i, Math.Min(chunkSize, data.Length - i)));
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }
}
