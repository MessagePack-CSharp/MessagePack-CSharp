using BenchmarkDotNet.Attributes;

// Round 5 rematch: can branch speculation beat the branchless table on realistic data?
// TryReadInt32CompareBenchmark proved the trade (cascade 0.82ns predicted / 9.2ns mixed
// vs table flat 2.57ns). The missing distribution is the realistic one:
//   FieldCycle - messages cycle deterministically fix -> uint8 -> int16 -> int32, values
//                random within each class. This models a POCO stream where every field
//                has a stable encoding class: one callsite, period-4 branch history,
//                which a modern TAGE predictor should learn perfectly. If cascade wins
//                here, real formatter callsites (each field its own inlined callsite,
//                mono-class history) will favor it even more strongly.
//   FixPos/Int32 - homogeneous extremes, Mixed - the adversarial case.
// Candidates all return the library's 3-value DecodeResult (drop-in shapes):
//   Ultra     - the current library table (branchless, chain-bound)
//   Mpcs      - MessagePack-CSharp v3 primitives (external reference)
//   CascadeDr - our port of the cascade under the library contract
//   HybridDr  - fixint branch + table fallback
//
// MEASURED, micro loop: CascadeDr crushed it — FieldCycle 0.27, FixPos 0.23, Int32 0.31,
// Mixed 2.34 (vs Ultra 1.00 flat; it even beat Mpcs everywhere). BUT the end-to-end
// check (DisasmProbe10 PerValueCascade, real 100k-distinct-payload POCO streams) REVERSED
// it: cascade 1.22x/1.38x/1.55x SLOWER than the table on AllFix/Half/Mixed. In the real
// formatter regime the decode chain overlaps with memory traffic and entry work across
// independent payloads, the table's latency hides, and cascade's extra branches/code only
// cost. The micro win is real but belongs to CONTIGUOUS single-callsite streams — i.e.
// array element decoding, whose true answer is SIMD batch decode anyway (deferred).
// VERDICT (final): full cascade rejected, but HybridDr survived BOTH regimes — e2e
// (DisasmProbe10 PerValueHybrid) 0.43x AllFix / 0.81x Half / 1.03x Mixed. The survival
// condition turned out to be "exactly one branch, with a branchless fallback": the fixint
// path's consumed is a CONSTANT (no chain link), and misses land on the chain-immune
// table. ADOPTED into the library TryReadInt32 (fixint-first + table, mirroring
// TryReadInt64); library-level e2e confirmed 5.3ns AllFix / 17.1 Half / 24.3 Mixed vs
// the table-only 13.9 / 23.7 / 26.0. Full cascade remains an option only for contiguous
// single-callsite streams (array elements) if SIMD batching is ever rejected.
public class TryReadInt32RematchBenchmark
{
    const int Count = 100_000;

    [Params("FixPos", "Int32", "FieldCycle", "Mixed")]
    public string Class = "FieldCycle";

    byte[] buffer = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        int i = 0;
        int Next() => Class switch
        {
            "FixPos" => rand.Next(0, 128),
            "Int32" => rand.Next(int.MinValue, -32768),
            "FieldCycle" => (i++ & 3) switch
            {
                0 => rand.Next(0, 128),            // fixint field (id, flags)
                1 => rand.Next(128, 256),          // uint8 field
                2 => rand.Next(-32768, -128),      // int16 field
                _ => rand.Next(65536, int.MaxValue), // int32 field
            },
            _ => rand.Next(8) switch
            {
                0 => rand.Next(0, 128),
                1 => rand.Next(-32, 0),
                2 => rand.Next(-128, -32),
                3 => rand.Next(128, 256),
                4 => rand.Next(-32768, -128),
                5 => rand.Next(256, 65536),
                6 => rand.Next(int.MinValue, -32768),
                _ => rand.Next(65536, int.MaxValue),
            },
        };

        buffer = new byte[Count * 5 + 8];
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            MessagePackPrimitives.TryWriteInt32Cascade(buffer.AsSpan(offset), Next(), out var written);
            offset += written;
        }

        // all four must agree on every message
        int o0 = 0, o1 = 0, o2 = 0, o3 = 0;
        for (int n = 0; n < Count; n++)
        {
            var r0 = UltraMessagePack.MessagePackPrimitives.TryReadInt32(buffer.AsSpan(o0), out var v0, out var c0);
            var r1 = MessagePack.MessagePackPrimitives.TryReadInt32(buffer.AsSpan(o1), out var v1, out var c1);
            var r2 = MessagePackPrimitives.TryReadInt32CascadeDr(buffer.AsSpan(o2), out var v2, out var c2);
            var r3 = MessagePackPrimitives.TryReadInt32HybridDr(buffer.AsSpan(o3), out var v3, out var c3);
            if (r0 != UltraMessagePack.DecodeResult.Success || r1 != MessagePack.MessagePackPrimitives.DecodeResult.Success
                || r2 != UltraMessagePack.DecodeResult.Success || r3 != UltraMessagePack.DecodeResult.Success
                || v0 != v1 || v0 != v2 || v0 != v3 || c0 != c1 || c0 != c2 || c0 != c3)
            {
                throw new InvalidOperationException($"verify failed at message {n}");
            }
            o0 += c0; o1 += c1; o2 += c2; o3 += c3;
        }

        // contract spot-checks for the new candidates: truncation and invalid codes
        Span<byte> probe = stackalloc byte[9];
        probe.Clear();
        probe[0] = 0xd2; // int32 needs 5 bytes
        for (int cut = 1; cut < 5; cut++)
        {
            if (MessagePackPrimitives.TryReadInt32CascadeDr(probe[..cut], out _, out _) != UltraMessagePack.DecodeResult.InsufficientBuffer
                || MessagePackPrimitives.TryReadInt32HybridDr(probe[..cut], out _, out _) != UltraMessagePack.DecodeResult.InsufficientBuffer)
            {
                throw new InvalidOperationException($"verify failed: truncation cut={cut}");
            }
        }
        probe[0] = 0xa0; // fixstr: not an int
        if (MessagePackPrimitives.TryReadInt32CascadeDr(probe, out _, out _) != UltraMessagePack.DecodeResult.TokenMismatch
            || MessagePackPrimitives.TryReadInt32HybridDr(probe, out _, out _) != UltraMessagePack.DecodeResult.TokenMismatch)
        {
            throw new InvalidOperationException("verify failed: token mismatch");
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int Ultra()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadInt32(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Mpcs()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (MessagePack.MessagePackPrimitives.TryReadInt32(buf.Slice(offset), out var v, out var consumed) == MessagePack.MessagePackPrimitives.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int CascadeDr()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (MessagePackPrimitives.TryReadInt32CascadeDr(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int HybridDr()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (MessagePackPrimitives.TryReadInt32HybridDr(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }
}
