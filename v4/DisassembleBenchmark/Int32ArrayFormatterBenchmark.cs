using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using SerializerFoundation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using UltraMessagePack;

// The revived SIMD int[] formatter (Int32ArrayFormatter: one Vector128 compare
// classifies 16 elements as fixint, one vpmovsxbd/vpmovdb converts them) vs the generic
// per-element ArrayFormatter<int> vs MessagePack-CSharp vs Nerdbank.MessagePack. The
// scalar comparison serializer is built with the new factory-chain semantics: putting
// GenericFormatterFactory FIRST makes it claim int[] before the primitive factory's
// SIMD variant.
//   Small - all fixint [-32, 128): the SIMD fast path end to end
//   Mixed - 8-class random: exercises the scalar-chunk fallback + SIMD resume
//   Large - all wide classes: SIMD never engages (pure overhead check)
//
// MEASURED (Zen 5 AVX-512, ShortRun, ns/element, N=1000):
//   Deserialize: Small SIMD 0.13 vs Scalar 1.03 (7.7x) vs Mpcs 1.85 (13.8x)
//                Mixed 2.54 / 2.67 / 3.85    Large 3.20 / 3.33 / 2.75
//   Serialize:   Small SIMD 0.14 vs Scalar 0.96 (6.9x) vs Mpcs 0.48 (3.4x)
//                Mixed 1.26 / 1.43 / 1.50    Large 1.55 / 1.73 / 0.89
// VERDICT: adopted — the SIMD formatter beats our own scalar in EVERY cell and hits
// memcpy-class throughput on fixint runs (~8 elements/ns; 16-wide vpmovdb vs Mpcs's
// 4-wide Vector128 shuffle explains the 3.5x serialize / 13.8x deserialize lead on
// Small). IMPORTANT correction (initially misattributed): Mpcs's Int32ArrayFormatter
// is ALSO SIMD on serialize — LittleEndianSerialize classifies 4 elements per iteration
// with six vector compares summed into a per-lane "kinds" vector, takes a shuffle+narrow
// fast lane when all four are fixint, and otherwise emits per element from the
// BE-pre-shuffled vector via a switch. That vectorized CLASSIFICATION never disengages
// on wide values, which is exactly why Mpcs leads our fixint-only SIMD on Mixed/Large
// serialize — an acceleration-coverage difference, not a cascade-speculation effect.
//
// MEASURED round 2 (non-SIMD hardware simulated via DOTNET_EnableHWIntrinsic=0, which
// also drops Mpcs to its scalar whole-array-batched fallback): our serialize bulk/tail
// was a per-element WriteInt32 loop whose real cost was the byref buffer-field traffic —
// asm showed index/capacity/pointer reloaded from memory per element (large structs
// passed by ref don't get promoted). Rewritten as batched scalar chunks (fields touched
// once per 16, inner loop is pure register arithmetic):
//   Small 0.75 -> 0.65 ns/elem, beating Mpcs's scalar fallback (0.84); SIMD rows
//   unchanged. Mixed 1.70 vs 0.99 / Large 2.1 vs 0.8 still trail because Mpcs's scalar
//   fallback pairs with the vectorized-classification design above; closing those cells
//   means adopting multi-class vector emit (kinds-style), a recorded follow-up round.
//
// MEASURED round 3 (kinds-style vector emit, two variants): rebuilt serialize around a
// region reservation (up to 4096 elem * 5 B reserved up front, offset accumulated in a
// register, one Advance per region) and ported Mpcs's kinds classification for mixed
// quads. (a) switch emit: Mixed 1.77 / Large 1.81, still behind Mpcs 1.63 / 1.26 in-run.
// (b) branchless emit (kind -> code|shift|len table, every lane one 8B store, next store
// overwrites the tail): Mixed 1.96 / Large 1.99 — NOT better, because at N=1000 the
// repeated array lets Zen 5 memorize the switch's branch sequence (CLAUDE.md pitfall),
// so there were no mispredicts to remove and the extra table/shift uops were pure cost.
// Also learned (fetched the actual sources): Nerdbank's int[] serialize is NOT SIMD at
// all — whole-array reservation + plain scalar switch — yet led Mixed/Large at N=1000.
//
// MEASURED round 4 (decider: kinds vs scalar chain, N=1000 AND N=100_000, per-elem ns):
//                        kinds-branchless  scalar-region(hybrid)   Mpcs    Nerdbank
//   1000    Small            0.13               0.11               0.48      0.71
//   1000    Mixed            2.03               1.51               1.68      1.11
//   1000    Large            1.94               2.05               1.60      1.06
//   100000  Small            0.35               0.34               1.01      1.49
//   100000  Mixed            3.59               3.44              11.61      7.05
//   100000  Large            2.53               2.44               6.98      4.87
// At N=100_000 the predictor can no longer memorize: Mpcs's and Nerdbank's emit switches
// collapse on mispredicts (Mpcs Mixed 1.68 -> 11.6 ns/elem) while our guard-dominated
// scalar classify chain holds, and the scalar-region shape beats the kinds variants in
// every cell it doesn't tie. VERDICT: adopted scalar-region + 16-wide fixint superlane
// into the library (kinds classification deleted); vectorized multi-class classification
// does not pay in either predictability regime. SerializeSimd and SerializeHybrid are the
// same algorithm since adoption (the bench-local RegionScalarInt32ArrayFormatter below is
// the surviving A/B artifact). Closing run, every cell won at N=100_000 vs both
// libraries — Serialize Small 0.36 vs 1.35/1.51, Mixed 3.84 vs 11.2/7.8, Large 2.87 vs
// 7.4/4.7; Deserialize Small 0.94 vs 5.0/9.6, Mixed 5.31 vs 12.8/19.1, Large 3.59 vs
// 8.0/12.5. At N=1000 (memorized predictor) Nerdbank's minimal-uop scalar switch still
// leads serialize Mixed/Large by ~20-45%: that regime rewards fewer uops, not fewer
// branches, and is the remaining known gap — accepted, since it evaporates the moment
// data stops repeating.
//
// MEASURED round 5 (wide superlane): realistic arrays are magnitude-homogeneous, so the
// complement of the all-fixint run is the all-wide run (every element a 5-byte token) —
// and fixed element size makes it vectorizable with no predictor dependence. If all 16
// lanes are outside [-32768, 65535]: code bytes 0xd2/0xce picked branchlessly by sign
// mask, payloads byte-reversed with one shuffle, and two vpermi2b (Avx512Vbmi) weave
// codes + payloads into the 80-byte token run (two 64B stores overlapping by 16).
// Serialize Large ns/elem, in-run:      Ultra   no-wide-lane   Mpcs   Nerdbank
//   1000  (memorized predictor)          0.45       2.51       1.47     1.17
//   100000 (real prediction)             0.84       2.77       7.88     4.23
// Mixed/Small unchanged within noise (the extra probe only runs when the fixint probe
// fails and costs one add+compare per 16). VERDICT: adopted. Every serialize cell now
// beats both libraries in BOTH regimes; the N=1000 Mixed cell (Nb 1.32 vs our 1.67) is
// the sole remaining Nerdbank lead — true random class interleave with a memorized
// predictor — accepted as benchmark-only. Deserialize wide superlane (5B fixed stride
// decode, the symmetric trick) is a possible future round.
//
// MEASURED round 6 (deserialize wide superlane): the symmetric decode — if the next 80
// window bytes are 16 tokens of [0xd2|0xce][4B BE], one vpermi2b gathers payloads
// byte-swapped straight into LE dwords and a second replicates each code across its
// dword for vectorized validation (all lanes int32/uint32, and no uint32 payload above
// int.MaxValue — those fall to the scalar reader, preserving exception behavior and
// non-minimal-encoding acceptance, both covered by tests).
// Deserialize Large ns/elem, in-run:    Ultra    Mpcs   Nerdbank      (prior Ultra)
//   1000  (memorized predictor)          0.29     3.37     7.54           3.35
//   100000 (real prediction)             0.86     7.59    13.47           3.59
// Mixed/Small unchanged within noise. VERDICT: adopted. Final standings across all 24
// cells (2 directions x 3 dists x 2 N): every cell beats both libraries except serialize
// Mixed at N=1000 (Nerdbank's minimal-uop scalar switch under a memorized predictor,
// benchmark-only) and deserialize Mixed at N=100k where our own generic per-element
// formatter edges the specialized one by ~7% (probe overhead on never-hitting data;
// within the equivalence band both runs it was observed).
//
// MEASURED round 7 (tier hardening: guards, AVX2 tier, non-VBMI wide lanes):
// (a) every 512-bit site now requires Vector512.IsHardwareAccelerated too — on
// Skylake-X-class CPUs Avx512F.IsSupported is true but the runtime reports
// IsHardwareAccelerated=false because 512-bit uops force downclocking; both are JIT
// constants so the guard is free. (b) New Vector256 tier (current consumer Intel is
// AVX2-only): fixint superlane via two 256-bit probes + portable Narrow on serialize,
// vpmovsxbw/vpmovsxwd on deserialize. (c) Wide superlanes for every tier below
// AVX-512VBMI: the vpermi2b weave is impractical under lane-local vpshufb, but the
// actual win is that a verified all-wide run makes the stride CONSTANT (every offset =
// base + 5t), which removes the classify chain and the offset serial dependency — so
// EmitWide16/TryDecodeWide16 do plain scalar constant-stride stores/loads at full ILP.
// AVX2-simulated (DOTNET_EnableAVX512F=0), ns/elem, in-run vs Mpcs (still running its
// Vector128 kinds SIMD) and Nerdbank:
//   Serialize   1000: Small 0.14 / 0.51 / 0.74   Large 0.36 / 1.39 / 1.08
//             100000: Small 0.33 / 0.89 / 1.13   Large 0.75 / 6.17 / 4.06
//   Deserialize 1000: Small 0.16 / 2.37 / 7.37   Large 0.31 / 3.03 / 6.95
//             100000: Small 1.07 / 4.89 / 8.91   Large 0.92 / 8.32 / 13.1
// [CORRECTED IN ROUND 8: the "constant-stride matches vpermi2b" reading here compared
// ACROSS runs — the documented cross-run pitfall — and was wrong.] Tests pass under all
// four tiers (full / AVX2-only / V128-only / DOTNET_EnableHWIntrinsic=0). VERDICT:
// guards + V256 tier + non-VBMI wide lanes adopted.
//
// MEASURED round 8 (vpermi2b deletion attempt, reverted): deleting the vpermi2b paths
// on the "measured identical" premise regressed Ser Large to 5.27 ns/elem. Asm showed
// EmitWide16's `v < 0 ? : ` ternary had compiled to a BRANCH (test/jl) — the JIT picks
// branch vs cmov at PGO's discretion, and the branch flavor eats a ~50% mispredict per
// element on random signs; the earlier AVX2-sim run had simply won the cmov lottery.
// PITFALL: never leave a hot sign-select to the ternary — the sign-mask xor
// (0xce ^ ((v >> 31) & 0x1c), asm: sar/and/xor) is deterministic branchless. That fix
// plus a 2-code pre-gate in TryDecodeWide16 (a failed probe on mixed data must be cheap;
// full-fused probing taxed Mixed deser 1.5x, a full 16-code pre-pass taxed all-wide 40%)
// recovered the scalar lanes to 1.2-1.7 ns/elem. The TRUE in-run A/B (library=vpermi2b,
// bench-local variant=identical code with scalar wide lanes) then settled it:
//   ns/elem            vpermi2b   constant-stride scalar
//   Ser  Large 1000      0.37        1.22      (3.3x)
//   Ser  Large 100000    0.85        1.66      (2.0x)
//   Deser Large 1000     0.27        1.48      (5.4x)
//   Deser Large 100000   0.74        1.61      (2.2x)
//   Small/Mixed: equal within noise (wide lanes not engaged).
// VERDICT: vpermi2b restored for the VBMI tier — the shuffle IS worth 2-5x on wide runs;
// the branchless scalar helpers remain the fallback for every other tier (and are now
// deterministic instead of PGO-dependent).
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Int32ArrayFormatterBenchmark
{
    // N=1000 repeats the same small array: Zen 5 memorizes even random branch sequences
    // (CLAUDE.md pitfall), so scalar classify chains run mispredict-free. N=100_000 defeats
    // the memorization and shows the unpredictable-data behavior. Means are per ARRAY
    // (no OperationsPerInvoke); divide by N for per-element.
    [Params(1000, 100_000)]
    public int N = 1000;

    [Params("Small", "Mixed", "Large")]
    public string Dist = "Small";

    int[] data = default!;
    byte[] payload = default!;
    MessagePackSerializer scalar = default!;
    MessagePackSerializer hybrid = default!;
    readonly Nerdbank.MessagePack.MessagePackSerializer nb = new();

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        data = new int[N];
        for (int i = 0; i < N; i++)
        {
            data[i] = Dist switch
            {
                "Small" => rand.Next(-32, 128),
                "Large" => rand.Next(2) == 0 ? rand.Next(65536, int.MaxValue) : rand.Next(int.MinValue, -32768),
                _ => rand.Next(8) switch
                {
                    0 => rand.Next(0, 128),
                    1 => rand.Next(-32, 0),
                    2 => rand.Next(128, 256),
                    3 => rand.Next(-128, -32),
                    4 => rand.Next(256, 65536),
                    5 => rand.Next(-32768, -128),
                    6 => rand.Next(65536, int.MaxValue),
                    _ => rand.Next(int.MinValue, -32768),
                },
            };
        }

        // generic per-element path: GenericFormatterFactory first claims int[]
        scalar = new MessagePackSerializer(GenericFormatterFactory.Instance, PrimitiveFormatterFactory.Instance);
        // A/B variant: region + fixint superlane, but mixed 16s emit via the scalar classify chain
        hybrid = new MessagePackSerializer(new RegionScalarInt32ArrayFormatterFactory(), PrimitiveFormatterFactory.Instance, GenericFormatterFactory.Instance);

        payload = MessagePackSerializer.Default.Serialize(data);
        var oracle = MessagePack.MessagePackSerializer.Serialize(data);
        if (!payload.AsSpan().SequenceEqual(oracle)) throw new InvalidOperationException("verify failed: simd bytes vs oracle");
        if (!scalar.Serialize(data).AsSpan().SequenceEqual(oracle)) throw new InvalidOperationException("verify failed: scalar bytes vs oracle");
        if (!hybrid.Serialize(data).AsSpan().SequenceEqual(oracle)) throw new InvalidOperationException("verify failed: hybrid bytes vs oracle");
        if (!hybrid.Deserialize<int[]>(payload)!.AsSpan().SequenceEqual(data)) throw new InvalidOperationException("verify failed: hybrid roundtrip");
        if (!MessagePackSerializer.Default.Deserialize<int[]>(payload)!.AsSpan().SequenceEqual(data)) throw new InvalidOperationException("verify failed: simd roundtrip");
        if (!scalar.Deserialize<int[]>(payload)!.AsSpan().SequenceEqual(data)) throw new InvalidOperationException("verify failed: scalar roundtrip");
        if (!nb.Deserialize<int[], NbInt32ArrayWitness>(nb.Serialize<int[], NbInt32ArrayWitness>(data))!.AsSpan().SequenceEqual(data)) throw new InvalidOperationException("verify failed: nerdbank roundtrip");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeSimd() => MessagePackSerializer.Default.Serialize(data);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeScalar() => scalar.Serialize(data);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeMpcs() => MessagePack.MessagePackSerializer.Serialize(data);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeHybridScalarRegion() => hybrid.Serialize(data);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeNerdbank() => nb.Serialize<int[], NbInt32ArrayWitness>(data);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public int[] DeserializeSimd() => MessagePackSerializer.Default.Deserialize<int[]>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public int[] DeserializeScalar() => scalar.Deserialize<int[]>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public int[] DeserializeHybridScalarRegion() => hybrid.Deserialize<int[]>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public int[] DeserializeMpcs() => MessagePack.MessagePackSerializer.Deserialize<int[]>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public int[] DeserializeNerdbank() => nb.Deserialize<int[], NbInt32ArrayWitness>(payload)!;
}

// PolyType witness so Nerdbank.MessagePack can serialize int[] without a user type
[PolyType.GenerateShapeFor<int[]>]
public partial class NbInt32ArrayWitness;

// A/B candidate: identical to the library formatter EXCEPT the wide superlanes use the
// constant-stride scalar helpers (EmitWide16 / TryDecodeWide16) even on VBMI hardware,
// so one run directly compares vpermi2b weave vs constant-stride scalar in-run.
public sealed class RegionScalarInt32ArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int[]?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    const int RegionElements = 4096;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref int[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        int length = value.Length;
        buffer.WriteArrayHeader(length);
        ref int src = ref MemoryMarshal.GetArrayDataReference(value);
        int i = 0;
        while (i < length)
        {
            int regionEnd = i + Math.Min(length - i, RegionElements);
            ref byte d = ref buffer.GetReference((regionEnd - i) * UltraMessagePack.MessagePackPrimitives.MaxInt32Length);
            int written = 0;
            if (Avx512F.IsSupported)
            {
                while (i + 16 <= regionEnd)
                {
                    var v = Vector512.LoadUnsafe(ref src, (nuint)i);
                    var biased = (v + Vector512.Create(32)).AsUInt32();
                    if (Vector512.LessThanOrEqualAll(biased, Vector512.Create(159u)))
                    {
                        Avx512F.ConvertToVector128SByte(v).AsByte().StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    else if (Vector512.GreaterThanOrEqualAll((v + Vector512.Create(32768)).AsUInt32(), Vector512.Create(98304u)))
                    {
                        EmitWide16(ref d, written, ref src, i);
                        written += 80;
                        i += 16;
                    }
                    else
                    {
                        int end = i + 16;
                        for (; i < end; i++)
                        {
                            written += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
            for (; i < regionEnd; i++)
            {
                written += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
            }
            buffer.Advance(written);
        }
    }

    static void EmitWide16(ref byte d, int written, ref int src, int i)
    {
        for (int t = 0; t < 16; t++)
        {
            int v = Unsafe.Add(ref src, i + t);
            Unsafe.Add(ref d, written + (t * 5)) = (byte)(0xce ^ ((v >> 31) & 0x1c));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + (t * 5) + 1), System.Buffers.Binary.BinaryPrimitives.ReverseEndianness((uint)v));
        }
    }

    static bool TryDecodeWide16(ref byte w0, ref int dst, int i)
    {
        if (!IsWideCode(Unsafe.Add(ref w0, 0)) || !IsWideCode(Unsafe.Add(ref w0, 5)))
        {
            return false;
        }
        bool ok = true;
        bool uintOverflow = false;
        for (int t = 0; t < 16; t++)
        {
            byte c = Unsafe.Add(ref w0, t * 5);
            int v = (int)System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref w0, (t * 5) + 1)));
            Unsafe.Add(ref dst, i + t) = v;
            ok &= (c == 0xd2) | (c == 0xce);
            uintOverflow |= (c == 0xce) & (v < 0);
        }
        return ok && !uintOverflow;

        static bool IsWideCode(byte c) => (c == 0xd2) | (c == 0xce);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        int count = buffer.ReadArrayHeader();
        var result = (value != null && value.Length == count) ? value : GC.AllocateUninitializedArray<int>(count);
        ref int dst = ref MemoryMarshal.GetArrayDataReference(result);
        int i = 0;
        if (Vector128.IsHardwareAccelerated)
        {
            while (i + 16 <= count)
            {
                var window = buffer.GetSpan();
                if (window.Length >= 16)
                {
                    var codes = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    var biased = codes + Vector128.Create((byte)0x20);
                    if (Vector128.LessThanOrEqualAll(biased, Vector128.Create((byte)0x9f)))
                    {
                        if (Avx512F.IsSupported)
                        {
                            Avx512F.ConvertToVector512Int32(codes.AsSByte()).StoreUnsafe(ref dst, (nuint)i);
                        }
                        else
                        {
                            var (lo, hi) = Vector128.Widen(codes.AsSByte());
                            var (i0, i1) = Vector128.Widen(lo);
                            var (i2, i3) = Vector128.Widen(hi);
                            i0.StoreUnsafe(ref dst, (nuint)i);
                            i1.StoreUnsafe(ref dst, (nuint)(i + 4));
                            i2.StoreUnsafe(ref dst, (nuint)(i + 8));
                            i3.StoreUnsafe(ref dst, (nuint)(i + 12));
                        }
                        buffer.Advance(16);
                        i += 16;
                        continue;
                    }

                    if (window.Length >= 80
                        && TryDecodeWide16(ref MemoryMarshal.GetReference(window), ref dst, i))
                    {
                        buffer.Advance(80);
                        i += 16;
                        continue;
                    }
                }

                int end = i + 16;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadInt32();
                }
            }
        }
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadInt32();
        }
        value = result;
    }
}

public sealed class RegionScalarInt32ArrayFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return type == typeof(int[]) ? new RegionScalarInt32ArrayFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}
