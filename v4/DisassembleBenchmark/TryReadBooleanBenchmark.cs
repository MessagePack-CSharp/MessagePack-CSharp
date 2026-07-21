using BenchmarkDotNet.Attributes;

// Round 6: is the library TryReadBoolean (biased compare, branchless value) actually
// faster than the naive switch-on-code shape, and is it at the limit?
//   Ultra       - library: one validity branch (always predicted on valid data),
//                 value = d != 0 setcc — value derivation is branchless
//   NaiveSwitch - switch on 0xc2/0xc3: the taken arm IS the value, so unpredictable
//                 bools should mispredict (~13-20cyc each on this machine)
//   Branchless  - zero branches after the empty guard (validity via setcc/cmov too):
//                 probes whether removing the last branch buys anything
// Distributions (100k values per the predictor-memorization pitfall): AllTrue/AllFalse
// (homogeneous — predictor learns everything), Random (50/50 — worst case for any
// value-dependent branch). tokenSize is constant 1 everywhere, so there is no
// consumed->next-address chain difference; this isolates value derivation.
//
// MEASURED (Zen 5, ShortRun):
//   AllFalse: Ultra 0.794  Naive 0.799 (1.01x)  Branchless 1.94 (2.44x)
//   AllTrue:  Ultra 0.796  Naive 0.792 (1.00x)  Branchless 2.04 (2.56x)
//   Random:   Ultra 0.852  Naive 3.28  (3.84x)  Branchless 2.03 (2.38x)
// asm confirmed the three shapes: Ultra = biased cmp + ja (validity, always predicted)
// + setne for value, consumed a CONSTANT 1; Naive = cmp 0xc2 / jne — the taken arm IS
// the value, 50% mispredict on Random costs ~+12cyc/op; Branchless = consumed from
// setbe, i.e. a DATA dependency load->setbe->add->next-address — the serial chain
// (~4cyc/iter) that the round-5 rule warns about, flat 2ns regardless of distribution.
// VERDICT: the library shape wins or ties every distribution and sits at the loop's
// throughput floor (~0.8ns = the bare offset+accumulate loop); removing its last branch
// makes things WORSE. "Exactly one always-predicted validity branch + branchless value
// + constant consumed" is the limit for a scalar per-token reader. Only SIMD batch
// decode (whole-buffer 0xc2/0xc3 classification) could go materially below this.
public class TryReadBooleanBenchmark
{
    const int Count = 100_000;

    [Params("AllTrue", "AllFalse", "Random")]
    public string Dist = "Random";

    byte[] buffer = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        buffer = new byte[Count];
        for (int i = 0; i < Count; i++)
        {
            bool b = Dist switch
            {
                "AllTrue" => true,
                "AllFalse" => false,
                _ => rand.Next(2) != 0,
            };
            buffer[i] = b ? (byte)0xc3 : (byte)0xc2;
        }

        // all three must agree on every message
        for (int i = 0; i < Count; i++)
        {
            var r0 = UltraMessagePack.MessagePackPrimitives.TryReadBoolean(buffer.AsSpan(i), out var v0, out var t0);
            var r1 = MessagePackPrimitives.TryReadBooleanNaiveSwitch(buffer.AsSpan(i), out var v1, out var t1);
            var r2 = MessagePackPrimitives.TryReadBooleanBranchless(buffer.AsSpan(i), out var v2, out var t2);
            if (r0 != UltraMessagePack.DecodeResult.Success || r1 != r0 || r2 != r0
                || v0 != v1 || v0 != v2 || t0 != 1 || t1 != 1 || t2 != 1)
            {
                throw new InvalidOperationException($"verify failed at message {i}");
            }
        }

        // contract spot-checks: empty and invalid code
        if (UltraMessagePack.MessagePackPrimitives.TryReadBoolean(default, out _, out var te0) != UltraMessagePack.DecodeResult.InsufficientBuffer || te0 != 1
            || MessagePackPrimitives.TryReadBooleanNaiveSwitch(default, out _, out var te1) != UltraMessagePack.DecodeResult.InsufficientBuffer || te1 != 1
            || MessagePackPrimitives.TryReadBooleanBranchless(default, out _, out var te2) != UltraMessagePack.DecodeResult.InsufficientBuffer || te2 != 1)
        {
            throw new InvalidOperationException("verify failed: empty");
        }
        ReadOnlySpan<byte> bad = [0xa0];
        if (UltraMessagePack.MessagePackPrimitives.TryReadBoolean(bad, out _, out var tb0) != UltraMessagePack.DecodeResult.TokenMismatch || tb0 != 0
            || MessagePackPrimitives.TryReadBooleanNaiveSwitch(bad, out _, out var tb1) != UltraMessagePack.DecodeResult.TokenMismatch || tb1 != 0
            || MessagePackPrimitives.TryReadBooleanBranchless(bad, out _, out var tb2) != UltraMessagePack.DecodeResult.TokenMismatch || tb2 != 0)
        {
            throw new InvalidOperationException("verify failed: mismatch");
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int Ultra()
    {
        ReadOnlySpan<byte> buf = buffer;
        int trues = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadBoolean(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                trues += v ? 1 : 0;
            }
            offset += consumed;
        }
        return trues;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int NaiveSwitch()
    {
        ReadOnlySpan<byte> buf = buffer;
        int trues = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (MessagePackPrimitives.TryReadBooleanNaiveSwitch(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                trues += v ? 1 : 0;
            }
            offset += consumed;
        }
        return trues;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Branchless()
    {
        ReadOnlySpan<byte> buf = buffer;
        int trues = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (MessagePackPrimitives.TryReadBooleanBranchless(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                trues += v ? 1 : 0;
            }
            offset += consumed;
        }
        return trues;
    }
}
