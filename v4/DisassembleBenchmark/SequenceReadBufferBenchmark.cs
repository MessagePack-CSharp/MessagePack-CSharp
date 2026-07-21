using BenchmarkDotNet.Attributes;
using SerializerFoundation;
using System.Buffers;
using UltraMessagePack;

// ReadOnlySequenceReadBuffer is expected to be the PRIMARY read entry (mirroring
// MessagePack-CSharp), but its hot path still carries the window-slicing shape the span
// buffer's index-representation round eliminated: Advance does
// currentSpan.Slice(n) (an uneliminable range check + 2-field span update) plus
// spanOffset += n plus consumed += n, and pays a tempBuffer null check every call.
// The fix mirrors the span buffer: currentSpan = the FULL current window (segment or
// stitched temp), plus one index. The fold operation (sequence.Slice(index)) is then
// UNIFORM across both window kinds, so Advance collapses to a single add — the
// single-segment hot path becomes instruction-identical to ReadOnlySpanReadBuffer.
//   SpanBuffer - ReadOnlySpanReadBuffer floor (the converged optimum)
//   SeqSingle  - single-segment sequence: the dominant real-world case
//   SeqMulti4K - 4KB segments: occasional boundaries
//   SeqMulti64 - 64B segments: boundary/stitch stress (~22 tokens per segment)
//
// MEASURED (Zen 5; the floor itself drifted ~7% between runs, so compare RATIOS to the
// same-run SpanBuffer floor, Default job):
//   SeqSingle  1.06 -> 0.98   SeqMulti4K 1.11 -> 1.00   SeqMulti64 1.67 -> 1.65
// VERDICT: adopted — single-segment and 4KB-segment sequences now run AT the span
// buffer's floor (hot path instruction-identical), boundary stress unchanged.
// PITFALL found on the way: Advance has SKIP semantics (e.g. jumping an ext payload),
// so index may legally run past the current window — the GetSpan fast check must use
// SIGNED remaining compares; the unsigned idiom read a negative remaining as huge,
// took the fast path with a negative window length, and hung the test suite.
//
// MEASURED round 2 (stitch destinations: caller scratch for <= 64B + RETAINED rented
// temp instead of per-straddle Rent/Return): SeqMulti64 ratio 1.71 vs 1.65/1.67 —
// NO measurable change; the boundary cost is dominated by ReadOnlySequence.Slice's
// segment-graph walk in the fold (~2 Slices + FirstSpans per boundary), not by the
// pool. Kept anyway: zero-cost structurally, removes Shared-pool traffic (a real cost
// under multithreaded load, invisible in this single-threaded micro) and repeated
// large rents for str/bin straddles. Next lever for the boundary regime, if ever
// needed: enumerate segments via TryGet/SequencePosition instead of Slice-based folds.
public class SequenceReadBufferBenchmark
{
    const int Count = 100_000;

    byte[] data = default!;
    ReadOnlySequence<byte> seqSingle;
    ReadOnlySequence<byte> seqMulti4K;
    ReadOnlySequence<byte> seqMulti64;
    long expectedSum;

    class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;
        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var s = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = s;
            return s;
        }
    }

    static ReadOnlySequence<byte> Chunk(byte[] data, int chunkSize)
    {
        var first = new Segment(data.AsMemory(0, Math.Min(chunkSize, data.Length)));
        var last = first;
        for (int offset = chunkSize; offset < data.Length; offset += chunkSize)
        {
            last = last.Append(data.AsMemory(offset, Math.Min(chunkSize, data.Length - offset)));
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        int i = 0;
        int Next() => (i++ & 3) switch // FieldCycle: POCO-like stable field classes
        {
            0 => rand.Next(0, 128),
            1 => rand.Next(128, 256),
            2 => rand.Next(-32768, -128),
            _ => rand.Next(65536, int.MaxValue),
        };

        var buffer = new byte[Count * 5 + 8];
        int offset = 0;
        expectedSum = 0;
        for (int n = 0; n < Count; n++)
        {
            var v = Next();
            expectedSum += v;
            UltraMessagePack.MessagePackPrimitives.TryWriteInt32(buffer.AsSpan(offset), v, out var written);
            offset += written;
        }
        data = buffer.AsSpan(0, offset).ToArray();

        seqSingle = new ReadOnlySequence<byte>(data);
        seqMulti4K = Chunk(data, 4096);
        seqMulti64 = Chunk(data, 64);

        // all four paths must produce the identical sum
        if (SpanBuffer() != expectedSum) throw new InvalidOperationException("verify failed: span");
        if (SeqSingle() != expectedSum) throw new InvalidOperationException("verify failed: single");
        if (SeqMulti4K() != expectedSum) throw new InvalidOperationException("verify failed: 4K");
        if (SeqMulti64() != expectedSum) throw new InvalidOperationException("verify failed: 64");
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public long SpanBuffer()
    {
        var buffer = new ReadOnlySpanReadBuffer(data);
        long sum = 0;
        for (int n = 0; n < Count; n++)
        {
            sum += buffer.ReadInt32();
        }
        buffer.Dispose();
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long SeqSingle()
    {
        Span<byte> scratch = stackalloc byte[64];
        var buffer = new ReadOnlySequenceReadBuffer(in seqSingle, scratch);
        long sum = 0;
        for (int n = 0; n < Count; n++)
        {
            sum += buffer.ReadInt32();
        }
        buffer.Dispose();
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long SeqMulti4K()
    {
        Span<byte> scratch = stackalloc byte[64];
        var buffer = new ReadOnlySequenceReadBuffer(in seqMulti4K, scratch);
        long sum = 0;
        for (int n = 0; n < Count; n++)
        {
            sum += buffer.ReadInt32();
        }
        buffer.Dispose();
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long SeqMulti64()
    {
        Span<byte> scratch = stackalloc byte[64];
        var buffer = new ReadOnlySequenceReadBuffer(in seqMulti64, scratch);
        long sum = 0;
        for (int n = 0; n < Count; n++)
        {
            sum += buffer.ReadInt32();
        }
        buffer.Dispose();
        return sum;
    }
}
