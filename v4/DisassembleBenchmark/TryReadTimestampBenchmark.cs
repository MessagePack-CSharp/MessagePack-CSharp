using BenchmarkDotNet.Attributes;

// TryReadTimestamp is the last two-stage reader: a real call into TryReadExtHeader
// (8-case switch), then a second switch on dataLength, then two-stage range checks that
// exist only for ts96 generality. But real timestamps are THREE fixed byte patterns —
// ts32 [d6 ff|4B], ts64 [d7 ff|8B], ts96 [c7 0c ff|12B] — so one 16-bit load compares
// code+type at once: ts64 needs a single nanos < 1e9 check (34-bit seconds max out at
// year 2514, inside DateTime range) and ts32 needs NO range check at all (u32 seconds
// max = year 2106). The candidate: ts64/ts32 inline behind head compares, everything
// else (ts96, tails, mismatches) in the current body demoted to a NoInlining Rare.
//   Ts32  - whole-second dates in u32 range (fixext4)
//   Ts64  - sub-second dates (fixext8, the dominant modern-writer form)
//   Mixed - random 50/50 of the two classes
//
// MEASURED (Zen 5, ShortRun; two-stage ext-header reader -> fused-head fast paths):
//   Ts32 4.19 -> 1.03 ns (4.1x)   Ts64 4.40 -> 1.63 ns (2.7x)   Mixed 7.09 -> 3.88 ns (1.8x)
// VERDICT: adopted — the ext-header call, both switches, and the ts96-generality range
// checks all disappear from the hot classes; what remains per read is one fused 16-bit
// head compare, one payload load+bswap, the ticks arithmetic, and (ts64 only) the
// nanos < 1e9 check. Mixed still pays the class branch (2 unpredictable classes, same
// as the byte reader's fix/uint8 situation) but on half the former work.
public class TryReadTimestampBenchmark
{
    const int Count = 100_000;

    [Params("Ts32", "Ts64", "Mixed")]
    public string Class = "Ts64";

    byte[] buffer = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        var baseDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime Next()
        {
            var d = baseDate.AddSeconds(rand.Next(0, 200_000_000));
            bool subSecond = Class switch
            {
                "Ts32" => false,
                "Ts64" => true,
                _ => rand.Next(2) != 0,
            };
            return subSecond ? d.AddTicks(rand.Next(1, 10_000_000)) : d;
        }

        buffer = new byte[Count * 15 + 8];
        var expected = new DateTime[Count];
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            expected[n] = Next();
            UltraMessagePack.MessagePackPrimitives.TryWriteTimestamp(buffer.AsSpan(offset), expected[n], out var written);
            offset += written;
        }

        // decoded values must match the source dates exactly
        int o = 0;
        for (int n = 0; n < Count; n++)
        {
            var r = UltraMessagePack.MessagePackPrimitives.TryReadTimestamp(buffer.AsSpan(o), out var v, out var c);
            if (r != UltraMessagePack.DecodeResult.Success || v != expected[n] || v.Kind != DateTimeKind.Utc)
            {
                throw new InvalidOperationException($"verify failed at message {n}");
            }
            o += c;
        }

        // contract spot-checks: ts96 path, wrong ext type, truncation reports exactly
        Span<byte> probe = stackalloc byte[15];
        var ts96 = new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        UltraMessagePack.MessagePackPrimitives.TryWriteTimestamp(probe, ts96, out var w96);
        if (w96 != 15 || UltraMessagePack.MessagePackPrimitives.TryReadTimestamp(probe, out var v96, out var t96) != UltraMessagePack.DecodeResult.Success
            || v96 != ts96 || t96 != 15)
        {
            throw new InvalidOperationException("verify failed: ts96");
        }
        for (int cut = 1; cut < 15; cut++)
        {
            var r = UltraMessagePack.MessagePackPrimitives.TryReadTimestamp(probe[..cut], out _, out var tc);
            if (r != UltraMessagePack.DecodeResult.InsufficientBuffer || tc <= cut || tc > 15)
            {
                throw new InvalidOperationException($"verify failed: truncation cut={cut}");
            }
        }
        probe[2] = 42; // ext8(12) with a non-timestamp type code
        if (UltraMessagePack.MessagePackPrimitives.TryReadTimestamp(probe, out _, out var tm) != UltraMessagePack.DecodeResult.TokenMismatch || tm != 0)
        {
            throw new InvalidOperationException("verify failed: wrong ext type");
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public long Ultra()
    {
        ReadOnlySpan<byte> buf = buffer;
        long sum = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadTimestamp(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                sum += v.Ticks;
            }
            offset += consumed;
        }
        return sum;
    }
}
