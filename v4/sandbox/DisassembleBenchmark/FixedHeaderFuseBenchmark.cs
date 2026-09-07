using BenchmarkDotNet.Attributes;
using MessagePack;
using SerializerFoundation;
using System.Runtime.CompilerServices;

// Round 10 (read side): fusing header validation and arity check into ONE byte compare.
// TryReadArrayHeader's fixarray fast path is mask + extract + branch, then ReadArrayHeader adds the
// implausible-count guard, then the deserialize loop compares i < count. When the generated code knows
// the expected arity N at compile time, all of that collapses to `buffer[0] == (0x90 | N)`: a single
// predicted equality compare with a constant, falling back to the full ReadArrayHeader on any other
// encoding or count (version drift, array16-coded writers). The security guard is not needed on the hit
// path because the trip count is the compile-time constant, not attacker data; the fallback keeps it.
// Prior art bounds the expectation: 9fd6f1d found count fast paths + straight-line ladders LOSE 1.2-1.8x
// at 16/4 fields (per-read layout), and PocoPerValueVsBatchBenchmark round 9 found loop-switch vs
// straight-line is a tie at 4 fields. This round probes arity 2, where the header is 1/3 of all tokens
// and the loop skeleton is at its cheapest, with a 2x2 of {header shape} x {body shape}:
//   HeaderMaskLoop (baseline)  - ReadArrayHeader + for/switch: the shipped generated shape
//   HeaderMaskStraight         - ReadArrayHeader + `if (count == 2)` straight-line, cold fallback
//   HeaderByteFuseLoop         - byte-fused header, shared for/switch body (count is a phi of 2/fallback)
//   HeaderByteFuseStraight     - byte-fused header + straight-line reads: the full candidate shape
// Distributions (50k objects x 3 tokens = 150k tokens per CLAUDE.md's predictor pitfall; values all
// uint32-coded so ReadInt32 takes one internal branch path and the header shape is the only variable):
//   SameVersion  - every object fixarray(2): the dominant case the fuse targets
//   RareMismatch - 1/64 objects are fixarray(3) new-writer payloads: realistic rolling upgrade
//   MixedVersion - random 1/2/3 arity per object: adversarial for the fuse branch predictor
// Setup cross-checks all shapes' checksums; VerifyCandidates() (wired into --verify) sweeps single-object
// payloads at arity 0..6 plus an array16-coded arity-2 object, checking field values, stale-sentinel
// preservation, and full payload consumption.
//
// MEASURED (ShortRun): SameVersion ByteFuseStraight 0.95 / ByteFuseLoop 0.97 (tie band), RareMismatch
// all shapes 0.95-0.97 (tie), MixedVersion ByteFuseLoop 1.11 / ByteFuseStraight 1.26 / MaskStraight 1.22
// (the fuse branch mispredicts per object and the out-of-line fallback pays call overhead a third of the
// time). Even at arity 2 with 1-byte values' worth of work per field the whole header path is ~6.6ns/object
// including the loop, so the ~2 ALU ops the fusion removes (extract + count compare + implausibility guard)
// hide under OoO exactly like TryReadArrayHeader's own header-fast-path note predicted. The shipped
// mask fast path already sits at the ceiling; the byte-fused check buys nothing it can keep and gives
// back 11-26% under version drift. Rejected.
public class FixedHeaderFuseBenchmark
{
    [Params("SameVersion", "RareMismatch", "MixedVersion")]
    public string Distribution = "SameVersion";

    const int ObjectCount = 50_000;

    byte[] blob = null!;
    readonly FusePair poco = new();
    long expectedSum;

    [GlobalSetup]
    public void Setup()
    {
        blob = BuildBlob(Distribution);
        expectedSum = HeaderMaskLoop();
        foreach (var (name, sum) in new (string, long)[]
        {
            (nameof(HeaderMaskStraight), HeaderMaskStraight()),
            (nameof(HeaderByteFuseLoop), HeaderByteFuseLoop()),
            (nameof(HeaderByteFuseStraight), HeaderByteFuseStraight()),
        })
        {
            if (sum != expectedSum)
            {
                throw new InvalidOperationException($"{name} checksum mismatch on {Distribution}: {sum} (expected {expectedSum})");
            }
        }
    }

    static byte[] BuildBlob(string distribution)
    {
        var rand = new Random(42);
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            for (int o = 0; o < ObjectCount; o++)
            {
                var count = distribution switch
                {
                    "SameVersion" => 2,
                    "RareMismatch" => o % 64 == 63 ? 3 : 2,
                    "MixedVersion" => 1 + rand.Next(3),
                    _ => throw new InvalidOperationException(distribution),
                };
                buffer.WriteArrayHeader(count);
                for (int i = 0; i < count; i++)
                {
                    // [100_000, int.MaxValue): always uint32-coded (0xce), keeping ReadInt32's width branches constant
                    buffer.WriteInt32(rand.Next(100_000, int.MaxValue));
                }
            }
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
        return writer.WrittenSpan.ToArray();
    }

    // ---- benchmarks -------------------------------------------------------

    [Benchmark(Baseline = true)]
    public long HeaderMaskLoop()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            ReadHeaderMaskLoop(ref buffer, value);
            sum += value.A + value.B;
        }
        return sum;
    }

    [Benchmark]
    public long HeaderMaskStraight()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            ReadHeaderMaskStraight(ref buffer, value);
            sum += value.A + value.B;
        }
        return sum;
    }

    [Benchmark]
    public long HeaderByteFuseLoop()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            ReadHeaderByteFuseLoop(ref buffer, value);
            sum += value.A + value.B;
        }
        return sum;
    }

    [Benchmark]
    public long HeaderByteFuseStraight()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            ReadHeaderByteFuseStraight(ref buffer, value);
            sum += value.A + value.B;
        }
        return sum;
    }

    // ---- shapes -----------------------------------------------------------

    const byte FixArray2 = 0x92;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadHeaderMaskLoop(ref ReadOnlySpanReadBuffer buffer, FusePair value)
    {
        var count = buffer.ReadArrayHeader();
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.A = buffer.ReadInt32(); break;
                case 1: value.B = buffer.ReadInt32(); break;
                default: buffer.Skip(); break; // version tolerance: unknown trailing member
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadHeaderMaskStraight(ref ReadOnlySpanReadBuffer buffer, FusePair value)
    {
        var count = buffer.ReadArrayHeader();
        if (count == 2)
        {
            value.A = buffer.ReadInt32();
            value.B = buffer.ReadInt32();
            return;
        }
        ReadBodyFallback(ref buffer, value, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadHeaderByteFuseLoop(ref ReadOnlySpanReadBuffer buffer, FusePair value)
    {
        int count;
        var span = buffer.GetCurrentSpan();
        if (!span.IsEmpty && span[0] == FixArray2)
        {
            buffer.Advance(1);
            count = 2;
        }
        else
        {
            count = buffer.ReadArrayHeader();
        }
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.A = buffer.ReadInt32(); break;
                case 1: value.B = buffer.ReadInt32(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadHeaderByteFuseStraight(ref ReadOnlySpanReadBuffer buffer, FusePair value)
    {
        var span = buffer.GetCurrentSpan();
        if (!span.IsEmpty && span[0] == FixArray2)
        {
            buffer.Advance(1);
            value.A = buffer.ReadInt32();
            value.B = buffer.ReadInt32();
            return;
        }
        ReadFullFallback(ref buffer, value);
    }

    // fallbacks stay out of line: 9fd6f1d showed an unexecuted fast-path block taxing the sibling loop it shares a method with

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ReadBodyFallback(ref ReadOnlySpanReadBuffer buffer, FusePair value, int count)
    {
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.A = buffer.ReadInt32(); break;
                case 1: value.B = buffer.ReadInt32(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ReadFullFallback(ref ReadOnlySpanReadBuffer buffer, FusePair value)
    {
        var count = buffer.ReadArrayHeader();
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.A = buffer.ReadInt32(); break;
                case 1: value.B = buffer.ReadInt32(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    // ---- verification -----------------------------------------------------

    internal delegate void ReadShape(ref ReadOnlySpanReadBuffer buffer, FusePair value);

    public static void VerifyCandidates()
    {
        var shapes = new (string, ReadShape)[]
        {
            (nameof(ReadHeaderMaskLoop), ReadHeaderMaskLoop),
            (nameof(ReadHeaderMaskStraight), ReadHeaderMaskStraight),
            (nameof(ReadHeaderByteFuseLoop), ReadHeaderByteFuseLoop),
            (nameof(ReadHeaderByteFuseStraight), ReadHeaderByteFuseStraight),
        };

        // fixarray payloads at every arity around the expected 2, values covering the encoding widths
        var testValues = new[] { 0, 1, 127, 128, 255, 256, 65535, 65536, int.MaxValue, -1, -32, -33, -128, -129, -32768, -32769, int.MinValue };
        for (int arity = 0; arity <= 6; arity++)
        {
            for (int seed = 0; seed < testValues.Length; seed++)
            {
                var values = Enumerable.Range(0, arity).Select(i => testValues[(seed + i) % testValues.Length]).ToArray();
                VerifyOne(shapes, BuildSingle(values, arity16: false), values);
            }
        }
        // an array16-coded arity-2 object must take the fallback and still decode correctly
        VerifyOne(shapes, BuildSingle(new[] { 42, -70000 }, arity16: true), new[] { 42, -70000 });
    }

    static byte[] BuildSingle(int[] values, bool arity16)
    {
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            if (arity16)
            {
                var span = buffer.GetSpan(3);
                span[0] = 0xdc; // array16, a non-canonical but valid encoding of a small arity
                span[1] = (byte)(values.Length >> 8);
                span[2] = (byte)values.Length;
                buffer.Advance(3);
            }
            else
            {
                buffer.WriteArrayHeader(values.Length);
            }
            foreach (var v in values)
            {
                buffer.WriteInt32(v);
            }
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
        return writer.WrittenSpan.ToArray();
    }

    static void VerifyOne((string, ReadShape)[] shapes, byte[] payload, int[] values)
    {
        const int SentinelA = unchecked((int)0xDEADBEEF);
        const int SentinelB = unchecked((int)0xCAFEBABE);
        foreach (var (name, shape) in shapes)
        {
            var value = new FusePair { A = SentinelA, B = SentinelB };
            var buffer = new ReadOnlySpanReadBuffer(payload);
            shape(ref buffer, value);
            var expectedA = values.Length > 0 ? values[0] : SentinelA;
            var expectedB = values.Length > 1 ? values[1] : SentinelB;
            if (value.A != expectedA || value.B != expectedB)
            {
                throw new InvalidOperationException($"{name} field mismatch at arity {values.Length}: ({value.A}, {value.B}) expected ({expectedA}, {expectedB})");
            }
            if (buffer.BytesRemaining != 0)
            {
                throw new InvalidOperationException($"{name} left {buffer.BytesRemaining} bytes unconsumed at arity {values.Length}");
            }
        }
    }
}

public sealed class FusePair
{
    public int A;
    public int B;
}
