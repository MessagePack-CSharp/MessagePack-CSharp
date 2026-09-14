using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using SerializerFoundation;
using MessagePack;

// Two questions on the Answer graph (the realistic nested poco from AnswerBenchmark):
//
// 1. Buffer tier cost — how much slower is the Compatible (non-`allows ref struct`)
//    buffer pair than the fast ref struct pair, formatter-to-formatter? DirectFast and
//    DirectCompatible invoke the resolved formatter by hand over identical data, so the
//    only difference is the buffer implementation the whole graph writes/reads through
//    (ArrayPoolListWriteBuffer+scratch vs array-only Compatible twin; span-window read
//    vs pinned PointerSpan read).
//
// 2. Endpoint overhead — what does the public MessagePackSerializer.Serialize/
//    Deserialize entry point add on top of the direct formatter call? Endpoint minus
//    DirectFast = TryGetFormatter routing + options/processor plumbing + try/finally.
//    (An endpoint call on a compat-ROUTED graph without a MessageProcessor is
//    DirectCompatible + that same routing overhead, so the compat column doubles as the
//    routed-endpoint estimate.)
//
// Setup is self-verifying: all three serialize paths must be byte-identical, and both
// direct deserialize paths must roundtrip back to the endpoint payload.
//
// MEASURED (i7-13700KF, 3 runs: ShortRun x2 + 10 warmup/20 iteration x1, ns/op medians):
//   Serialize:   Endpoint 411 | DirectFast 410 | DirectCompatible 432 (+5%)
//   Deserialize: Endpoint 821 | DirectFast 805 | DirectCompatible 867 (+8%)
// VERDICT: (1) endpoint overhead is noise — Endpoint vs DirectFast flips sign across
// runs (±2-5%, process-level alignment), so the routing/try-finally/processor plumbing
// costs nothing measurable; current endpoint also matches the round-1 AnswerBenchmark
// record on this machine (396/866). (2) the Compatible tier costs ~5% serialize /
// ~8% deserialize on this graph (~20ns / ~60-75ns): buffer ops inline on both tiers,
// the delta is the array-field indirection + null check in CompatibleArrayPoolList's
// GetSpan/Advance and the PointerSpan window on the read side. Allocated identical.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class AnswerBufferTierBenchmark
{
    // mirrors MessagePackSerializer.SerializeScratchSize so DirectFast measures the same
    // scratch-then-spill behavior as the endpoint (the ~1.65KB payload overflows 1024)
    const int SerializeScratchSize = 1024;

    Answer answer = default!;
    byte[] payload = default!;
    MessagePackSerializerOptions options = default!;

    [GlobalSetup]
    public void Setup()
    {
        answer = AnswerBenchmark.CreateAnswer();
        options = MessagePackSerializerOptions.Default;
        payload = MessagePackSerializer.Serialize(answer);

        if (!SerializeDirectFast().AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: DirectFast serialize bytes != endpoint bytes");
        if (!SerializeDirectCompatible().AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: DirectCompatible serialize bytes != endpoint bytes");

        VerifyRoundtrip(DeserializeDirectFast(), "DirectFast");
        VerifyRoundtrip(DeserializeDirectCompatible(), "DirectCompatible");
    }

    void VerifyRoundtrip(Answer back, string label)
    {
        var bytes = MessagePackSerializer.Serialize(back);
        if (!bytes.AsSpan().SequenceEqual(payload)) throw new InvalidOperationException($"verify failed: {label} roundtrip");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeEndpoint() => MessagePackSerializer.Serialize(answer);

    [BenchmarkCategory("Serialize"), Benchmark]
    [System.Runtime.CompilerServices.SkipLocalsInit]
    public byte[] SerializeDirectFast()
    {
        Span<byte> scratch = stackalloc byte[SerializeScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            var state = new SerializeState(options.MaxDepth);
            options.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, Answer>().Serialize(ref buffer, ref state, answer);
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeDirectCompatible()
    {
        var buffer = new CompatibleArrayPoolListWriteBuffer();
        try
        {
            var state = new SerializeState(options.MaxDepth);
            options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, Answer>().Serialize(ref buffer, ref state, answer);
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public Answer DeserializeEndpoint() => MessagePackSerializer.Deserialize<Answer>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Answer DeserializeDirectFast()
    {
        var buffer = new ReadOnlySpanReadBuffer(payload);
        try
        {
            var state = new DeserializeState(options.MaxDepth);
            Answer result = default!;
            options.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, Answer>().Deserialize(ref buffer, ref state, ref result);
            return result;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public unsafe Answer DeserializeDirectCompatible()
    {
        fixed (byte* pointer = payload)
        {
            var buffer = new CompatibleReadOnlySpanReadBuffer(pointer, payload.Length);
            try
            {
                var state = new DeserializeState(options.MaxDepth);
                Answer result = default!;
                options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, Answer>().Deserialize(ref buffer, ref state, ref result);
                return result;
            }
            finally
            {
                buffer.Dispose();
            }
        }
    }
}
