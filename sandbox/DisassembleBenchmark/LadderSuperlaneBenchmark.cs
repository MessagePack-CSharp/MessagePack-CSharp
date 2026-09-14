// short/long array codecs: do the int32-style homogeneous superlanes pay for the other
// ladder widths, or does region-scalar stay the right call?
//
// The int32 codec ships two superlanes (all-fixint: 16 tokens = 16 bytes; all-wide:
// 16 x 5B constant stride) with mixed 16s falling to the scalar classify chain. The open
// question per remaining ladder type is whether the same gates pay when the wide token is
// 3B (short) or 9B (long) and the ladder has more intermediate classes that the gates
// simply skip (a run of int8-class shorts takes the scalar chain in both variants).
//
// Candidates are direct statics over contiguous buffers, Zen 5 paths only (adoption adds
// tiers). Sign-select code bytes use the mask-xor trick throughout: 0xcd ^ 0x1c = 0xd1
// and 0xcf ^ 0x1c = 0xd3, the same adjacency exploited by the int32 wide lane.
// Distributions: "fixint" (all 1B tokens), "wide" (all 3B/9B tokens), "mixed" (all
// ladder classes at random, the gates' worst case).
//
// RESULTS (Zen 5, ShortRun, ratio vs region-scalar, N=1000 / N=100000):
//   round 1, unguarded gates (the shape kept in this file):
//     int16 ser   fixint 0.11/0.07   wide 0.50/0.49   mixed 1.02/1.27
//     int16 deser fixint 0.07/0.05   wide 0.46/0.46   mixed 1.10/1.30
//     int64 ser   fixint 0.15/0.16   wide 0.59/0.51   mixed 1.04/0.79
//     int64 deser fixint 0.11/0.12   wide 0.44/0.45   mixed 1.10/1.37
//   round 2, first-element/first-code class probes guarding the gates: homogeneous
//   unchanged, mixed WORSE (int16 1.42 both directions, int64 ser 1.10, deser 1.29).
//   The probe is a data-dependent branch that mispredicts ~alternately on random
//   classes (13-20 cycles each, the ternary-branch lesson), while the unguarded
//   all-16 gates are predictably-false branches whose cost is just the vector probe.
//
// VERDICT: homogeneous runs win decisively (2-20x) and real arrays are magnitude-
// homogeneous, but the mixed regression at large N (up to 1.37x) exceeds the 10%
// equivalence bar, so this was a judgment call rather than an automatic adoption.
// Guarding is refuted either way. DECISION (neuecc, 2026-08-12): accept the int32-style
// trade and adopt the unguarded superlanes. Implemented in Int16/UInt16/UInt32/Int64/
// UInt64ElementCodec, both directions; the unsigned variants use constant wide codes
// (0xcd/0xce/0xcf, no sign select, no overflow flag) and uint32 reuses the shared
// WideLaneTables weave/gather with a code-filled second source.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class LadderSuperlaneBenchmark
{
    [Params(1000, 100_000)]
    public int N = 1000;

    [Params("fixint", "wide", "mixed")]
    public string Dist = "mixed";

    short[] shorts = default!;
    long[] longs = default!;
    byte[] sbuf = default!;
    byte[] lbuf = default!;
    byte[] sPayload = default!;
    byte[] lPayload = default!;
    int sPayloadLen;
    int lPayloadLen;
    short[] sdst = default!;
    long[] ldst = default!;

    public static (short[] Shorts, long[] Longs) MakeData(int n, string dist, int seed)
    {
        var rand = new Random(seed);
        var shorts = new short[n];
        var longs = new long[n];
        for (int i = 0; i < n; i++)
        {
            switch (dist)
            {
                case "fixint":
                    shorts[i] = (short)rand.Next(-32, 128);
                    longs[i] = rand.Next(-32, 128);
                    break;
                case "wide":
                    // short: outside [-128, 255] so every token is 3B; long: outside
                    // [int.Min, uint.Max] so every token is 9B; signs mixed
                    shorts[i] = (short)(rand.Next(2) == 0 ? rand.Next(256, 32768) : -rand.Next(256, 32769));
                    longs[i] = rand.Next(2) == 0
                        ? rand.NextInt64(4294967296L, long.MaxValue)
                        : -rand.NextInt64(2147483649L, long.MaxValue);
                    break;
                default:
                    shorts[i] = rand.Next(6) switch
                    {
                        0 => (short)rand.Next(-32, 128),
                        1 => (short)rand.Next(128, 256),
                        2 => (short)rand.Next(-128, -32),
                        3 => (short)rand.Next(256, 32768),
                        4 => (short)rand.Next(-32768, -128),
                        _ => (short)rand.Next(short.MinValue, short.MaxValue),
                    };
                    longs[i] = rand.Next(8) switch
                    {
                        0 => rand.Next(-32, 128),
                        1 => rand.Next(-256, 256),
                        2 => rand.Next(-65536, 65536),
                        3 => rand.Next(int.MinValue, int.MaxValue),
                        4 => (uint)rand.Next(),
                        5 => rand.NextInt64(4294967296L, long.MaxValue),
                        _ => -rand.NextInt64(2147483649L, long.MaxValue),
                    };
                    break;
            }
        }
        return (shorts, longs);
    }

    [GlobalSetup]
    public void Setup()
    {
        (shorts, longs) = MakeData(N, Dist, 42);
        sbuf = new byte[3 * N + 64];
        lbuf = new byte[9 * N + 64];
        sdst = new short[N];
        ldst = new long[N];

        sPayload = new byte[3 * N + 64];
        sPayloadLen = LadderSuperlaneCandidates.WriteInt16Scalar(ref MemoryMarshal.GetArrayDataReference(sPayload), ref MemoryMarshal.GetArrayDataReference(shorts), N);
        lPayload = new byte[9 * N + 64];
        lPayloadLen = LadderSuperlaneCandidates.WriteInt64Scalar(ref MemoryMarshal.GetArrayDataReference(lPayload), ref MemoryMarshal.GetArrayDataReference(longs), N);

        if (!LadderSuperlaneVerify.VerifyAll(shorts, longs)) throw new InvalidOperationException("verify failed: ladder superlane candidates");
    }

    [BenchmarkCategory("Int16Ser"), Benchmark(Baseline = true)]
    public int Int16SerScalar() => LadderSuperlaneCandidates.WriteInt16Scalar(ref MemoryMarshal.GetArrayDataReference(sbuf), ref MemoryMarshal.GetArrayDataReference(shorts), N);

    [BenchmarkCategory("Int16Ser"), Benchmark]
    public int Int16SerSuper() => LadderSuperlaneCandidates.WriteInt16Superlane(ref MemoryMarshal.GetArrayDataReference(sbuf), ref MemoryMarshal.GetArrayDataReference(shorts), N);

    [BenchmarkCategory("Int16Deser"), Benchmark(Baseline = true)]
    public bool Int16DeserScalar() => LadderSuperlaneCandidates.ReadInt16Scalar(sPayload.AsSpan(0, sPayloadLen), sdst.AsSpan(0, N));

    [BenchmarkCategory("Int16Deser"), Benchmark]
    public bool Int16DeserSuper() => LadderSuperlaneCandidates.ReadInt16Superlane(sPayload.AsSpan(0, sPayloadLen), sdst.AsSpan(0, N));

    [BenchmarkCategory("Int64Ser"), Benchmark(Baseline = true)]
    public int Int64SerScalar() => LadderSuperlaneCandidates.WriteInt64Scalar(ref MemoryMarshal.GetArrayDataReference(lbuf), ref MemoryMarshal.GetArrayDataReference(longs), N);

    [BenchmarkCategory("Int64Ser"), Benchmark]
    public int Int64SerSuper() => LadderSuperlaneCandidates.WriteInt64Superlane(ref MemoryMarshal.GetArrayDataReference(lbuf), ref MemoryMarshal.GetArrayDataReference(longs), N);

    [BenchmarkCategory("Int64Deser"), Benchmark(Baseline = true)]
    public bool Int64DeserScalar() => LadderSuperlaneCandidates.ReadInt64Scalar(lPayload.AsSpan(0, lPayloadLen), ldst.AsSpan(0, N));

    [BenchmarkCategory("Int64Deser"), Benchmark]
    public bool Int64DeserSuper() => LadderSuperlaneCandidates.ReadInt64Superlane(lPayload.AsSpan(0, lPayloadLen), ldst.AsSpan(0, N));
}

public static class LadderSuperlaneCandidates
{
    // ---- int16 ----

    public static int WriteInt16Scalar(ref byte d, ref short src, int length)
    {
        int o = 0;
        for (int i = 0; i < length; i++)
        {
            o += MessagePack.MessagePackPrimitives.UnsafeWriteInt16(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteInt16Superlane(ref byte d, ref short src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector256.IsHardwareAccelerated)
        {
            while (i + 16 <= length)
            {
                var v = Vector256.LoadUnsafe(ref src, (nuint)i);
                // all 16 in fixint range [-32, 127]?
                if (Vector256.LessThanOrEqualAll((v + Vector256.Create((short)32)).AsUInt16(), Vector256.Create((ushort)159)))
                {
                    // truncating narrow agrees with the values (verified in range)
                    Vector128.Narrow(v.GetLower(), v.GetUpper()).AsByte().StoreUnsafe(ref Unsafe.Add(ref d, o));
                    o += 16;
                    i += 16;
                    continue;
                }
                // all 16 outside [-128, 255], i.e. every token is 3B (int16/uint16)?
                if (Vector256.GreaterThanOrEqualAll((v + Vector256.Create((short)128)).AsUInt16(), Vector256.Create((ushort)384)))
                {
                    for (int t = 0; t < 16; t++)
                    {
                        short val = Unsafe.Add(ref src, i + t);
                        Unsafe.Add(ref d, o + (t * 3)) = (byte)(0xcd ^ ((val >> 15) & 0x1c));
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, o + (t * 3) + 1), BinaryPrimitives.ReverseEndianness((ushort)val));
                    }
                    o += 48;
                    i += 16;
                    continue;
                }
                int end = i + 16;
                for (; i < end; i++)
                {
                    o += MessagePack.MessagePackPrimitives.UnsafeWriteInt16(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
                }
            }
        }
        for (; i < length; i++)
        {
            o += MessagePack.MessagePackPrimitives.UnsafeWriteInt16(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static bool ReadInt16Scalar(ReadOnlySpan<byte> s, Span<short> dst)
    {
        int o = 0;
        for (int i = 0; i < dst.Length; i++)
        {
            if (MessagePack.MessagePackPrimitives.TryReadInt16(s.Slice(o), out dst[i], out var size) != MessagePack.DecodeResult.Success)
            {
                return false;
            }
            o += size;
        }
        return true;
    }

    public static bool ReadInt16Superlane(ReadOnlySpan<byte> s, Span<short> dst)
    {
        int i = 0;
        int o = 0;
        int count = dst.Length;
        ref byte sr = ref MemoryMarshal.GetReference(s);
        ref short dr = ref MemoryMarshal.GetReference(dst);
        if (Vector128.IsHardwareAccelerated)
        {
            while (i + 16 <= count)
            {
                if (s.Length - o >= 16)
                {
                    var codes = Vector128.LoadUnsafe(ref Unsafe.Add(ref sr, o));
                    if (Vector128.LessThanOrEqualAll(codes + Vector128.Create((byte)0x20), Vector128.Create((byte)0x9f)))
                    {
                        // sign-extend 16 fixint bytes to 16 shorts
                        var (lo, hi) = Vector128.Widen(codes.AsSByte());
                        lo.StoreUnsafe(ref dr, (nuint)i);
                        hi.StoreUnsafe(ref dr, (nuint)(i + 8));
                        o += 16;
                        i += 16;
                        continue;
                    }
                }
                if (s.Length - o >= 48 && TryDecodeWide16Int16(ref Unsafe.Add(ref sr, o), ref dr, i))
                {
                    o += 48;
                    i += 16;
                    continue;
                }
                int end = i + 16;
                for (; i < end; i++)
                {
                    if (MessagePack.MessagePackPrimitives.TryReadInt16(s.Slice(o), out Unsafe.Add(ref dr, i), out var size) != MessagePack.DecodeResult.Success)
                    {
                        return false;
                    }
                    o += size;
                }
            }
        }
        for (; i < count; i++)
        {
            if (MessagePack.MessagePackPrimitives.TryReadInt16(s.Slice(o), out Unsafe.Add(ref dr, i), out var size) != MessagePack.DecodeResult.Success)
            {
                return false;
            }
            o += size;
        }
        return true;
    }

    // constant-stride decode of 16 x [0xd1|0xcd][2B BE]; a uint16 above short.MaxValue
    // must reach the scalar reader to throw, so it fails the gate here
    static bool TryDecodeWide16Int16(ref byte w0, ref short dst, int i)
    {
        if (!IsWide(Unsafe.Add(ref w0, 0)) || !IsWide(Unsafe.Add(ref w0, 3)))
        {
            return false;
        }
        bool ok = true;
        bool overflow = false;
        for (int t = 0; t < 16; t++)
        {
            byte c = Unsafe.Add(ref w0, t * 3);
            ushort raw = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref w0, (t * 3) + 1)));
            Unsafe.Add(ref dst, i + t) = unchecked((short)raw);
            ok &= (c == 0xd1) | (c == 0xcd);
            overflow |= (c == 0xcd) & (raw > 0x7FFF);
        }
        return ok && !overflow;

        static bool IsWide(byte c) => (c == 0xd1) | (c == 0xcd);
    }

    // ---- int64 ----

    public static int WriteInt64Scalar(ref byte d, ref long src, int length)
    {
        int o = 0;
        for (int i = 0; i < length; i++)
        {
            o += MessagePack.MessagePackPrimitives.UnsafeWriteInt64(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteInt64Superlane(ref byte d, ref long src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512F.IsSupported)
        {
            while (i + 8 <= length)
            {
                var v = Vector512.LoadUnsafe(ref src, (nuint)i);
                if (Vector512.LessThanOrEqualAll((v + Vector512.Create(32L)).AsUInt64(), Vector512.Create(159UL)))
                {
                    // truncating narrow 8 longs -> 8 bytes (vpmovqb)
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, o), Avx512F.ConvertToVector128SByte(v).AsUInt64().ToScalar());
                    o += 8;
                    i += 8;
                    continue;
                }
                // all 8 outside [int.Min, uint.Max], i.e. every token is 9B (int64/uint64)?
                if (Vector512.GreaterThanOrEqualAll((v + Vector512.Create(2147483648L)).AsUInt64(), Vector512.Create(6442450944UL)))
                {
                    for (int t = 0; t < 8; t++)
                    {
                        long val = Unsafe.Add(ref src, i + t);
                        Unsafe.Add(ref d, o + (t * 9)) = (byte)(0xcf ^ (int)((val >> 63) & 0x1c));
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, o + (t * 9) + 1), BinaryPrimitives.ReverseEndianness((ulong)val));
                    }
                    o += 72;
                    i += 8;
                    continue;
                }
                int end = i + 8;
                for (; i < end; i++)
                {
                    o += MessagePack.MessagePackPrimitives.UnsafeWriteInt64(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
                }
            }
        }
        for (; i < length; i++)
        {
            o += MessagePack.MessagePackPrimitives.UnsafeWriteInt64(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static bool ReadInt64Scalar(ReadOnlySpan<byte> s, Span<long> dst)
    {
        int o = 0;
        for (int i = 0; i < dst.Length; i++)
        {
            if (MessagePack.MessagePackPrimitives.TryReadInt64(s.Slice(o), out dst[i], out var size) != MessagePack.DecodeResult.Success)
            {
                return false;
            }
            o += size;
        }
        return true;
    }

    public static bool ReadInt64Superlane(ReadOnlySpan<byte> s, Span<long> dst)
    {
        int i = 0;
        int o = 0;
        int count = dst.Length;
        ref byte sr = ref MemoryMarshal.GetReference(s);
        ref long dr = ref MemoryMarshal.GetReference(dst);
        if (Vector512.IsHardwareAccelerated && Avx512F.IsSupported)
        {
            while (i + 8 <= count)
            {
                if (s.Length - o >= 16)
                {
                    var codes = Vector128.LoadUnsafe(ref Unsafe.Add(ref sr, o));
                    // only the lower 8 lanes matter (8 tokens); the load needs 16B of window
                    if ((Vector128.LessThanOrEqual(codes + Vector128.Create((byte)0x20), Vector128.Create((byte)0x9f)).ExtractMostSignificantBits() & 0xFF) == 0xFF)
                    {
                        // sign-extend 8 fixint bytes to 8 longs (vpmovsxbq)
                        Avx512F.ConvertToVector512Int64(codes.AsSByte()).StoreUnsafe(ref dr, (nuint)i);
                        o += 8;
                        i += 8;
                        continue;
                    }
                }
                if (s.Length - o >= 72 && TryDecodeWide8Int64(ref Unsafe.Add(ref sr, o), ref dr, i))
                {
                    o += 72;
                    i += 8;
                    continue;
                }
                int end = i + 8;
                for (; i < end; i++)
                {
                    if (MessagePack.MessagePackPrimitives.TryReadInt64(s.Slice(o), out Unsafe.Add(ref dr, i), out var size) != MessagePack.DecodeResult.Success)
                    {
                        return false;
                    }
                    o += size;
                }
            }
        }
        for (; i < count; i++)
        {
            if (MessagePack.MessagePackPrimitives.TryReadInt64(s.Slice(o), out Unsafe.Add(ref dr, i), out var size) != MessagePack.DecodeResult.Success)
            {
                return false;
            }
            o += size;
        }
        return true;
    }

    // constant-stride decode of 8 x [0xd3|0xcf][8B BE]; a uint64 above long.MaxValue
    // must reach the scalar reader to throw, so it fails the gate here
    static bool TryDecodeWide8Int64(ref byte w0, ref long dst, int i)
    {
        if (!IsWide(Unsafe.Add(ref w0, 0)) || !IsWide(Unsafe.Add(ref w0, 9)))
        {
            return false;
        }
        bool ok = true;
        bool overflow = false;
        for (int t = 0; t < 8; t++)
        {
            byte c = Unsafe.Add(ref w0, t * 9);
            ulong raw = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref w0, (t * 9) + 1)));
            Unsafe.Add(ref dst, i + t) = unchecked((long)raw);
            ok &= (c == 0xd3) | (c == 0xcf);
            overflow |= (c == 0xcf) & ((long)raw < 0);
        }
        return ok && !overflow;

        static bool IsWide(byte c) => (c == 0xd3) | (c == 0xcf);
    }
}

// shared by GlobalSetup and Program.cs --verify
public static class LadderSuperlaneVerify
{
    public static bool VerifyAll(short[] shorts, long[] longs)
    {
        var ok = true;
        int n = shorts.Length;

        var sExpected = new byte[3 * n + 64];
        var sLen = LadderSuperlaneCandidates.WriteInt16Scalar(ref MemoryMarshal.GetArrayDataReference(sExpected), ref MemoryMarshal.GetArrayDataReference(shorts), n);
        var sActual = new byte[3 * n + 64];
        var sActLen = LadderSuperlaneCandidates.WriteInt16Superlane(ref MemoryMarshal.GetArrayDataReference(sActual), ref MemoryMarshal.GetArrayDataReference(shorts), n);
        if (sActLen != sLen || !sActual.AsSpan(0, sLen).SequenceEqual(sExpected.AsSpan(0, sLen)))
        {
            ok = false;
            Console.WriteLine($"NG LadderSuperlane Int16Ser n={n}");
        }
        foreach (var (name, run) in new (string, Func<short[], bool>)[]
        {
            ("Int16DeserScalar", dst => LadderSuperlaneCandidates.ReadInt16Scalar(sExpected.AsSpan(0, sLen), dst)),
            ("Int16DeserSuper", dst => LadderSuperlaneCandidates.ReadInt16Superlane(sExpected.AsSpan(0, sLen), dst)),
        })
        {
            var dst = new short[n];
            if (!run(dst) || !dst.AsSpan().SequenceEqual(shorts))
            {
                ok = false;
                Console.WriteLine($"NG LadderSuperlane {name} n={n}");
            }
        }

        var lExpected = new byte[9 * n + 64];
        var lLen = LadderSuperlaneCandidates.WriteInt64Scalar(ref MemoryMarshal.GetArrayDataReference(lExpected), ref MemoryMarshal.GetArrayDataReference(longs), n);
        var lActual = new byte[9 * n + 64];
        var lActLen = LadderSuperlaneCandidates.WriteInt64Superlane(ref MemoryMarshal.GetArrayDataReference(lActual), ref MemoryMarshal.GetArrayDataReference(longs), n);
        if (lActLen != lLen || !lActual.AsSpan(0, lLen).SequenceEqual(lExpected.AsSpan(0, lLen)))
        {
            ok = false;
            Console.WriteLine($"NG LadderSuperlane Int64Ser n={n}");
        }
        foreach (var (name, run) in new (string, Func<long[], bool>)[]
        {
            ("Int64DeserScalar", dst => LadderSuperlaneCandidates.ReadInt64Scalar(lExpected.AsSpan(0, lLen), dst)),
            ("Int64DeserSuper", dst => LadderSuperlaneCandidates.ReadInt64Superlane(lExpected.AsSpan(0, lLen), dst)),
        })
        {
            var dst = new long[n];
            if (!run(dst) || !dst.AsSpan().SequenceEqual(longs))
            {
                ok = false;
                Console.WriteLine($"NG LadderSuperlane {name} n={n}");
            }
        }

        // decode gates must reject what the scalar reader rejects: a uint16/uint64 wide
        // token whose value does not fit the signed target
        if (n >= 16)
        {
            var sBad = new byte[3 * 16 + 16];
            for (int t = 0; t < 16; t++)
            {
                sBad[t * 3] = 0xcd;
                BinaryPrimitives.WriteUInt16BigEndian(sBad.AsSpan((t * 3) + 1), 40000); // > short.MaxValue
            }
            var dst16 = new short[16];
            if (LadderSuperlaneCandidates.ReadInt16Superlane(sBad.AsSpan(0, 48), dst16))
            {
                ok = false;
                Console.WriteLine("NG LadderSuperlane Int16 overflow accepted");
            }
            var lBad = new byte[9 * 8 + 16];
            for (int t = 0; t < 8; t++)
            {
                lBad[t * 9] = 0xcf;
                BinaryPrimitives.WriteUInt64BigEndian(lBad.AsSpan((t * 9) + 1), ulong.MaxValue);
            }
            var dst8 = new long[8];
            if (LadderSuperlaneCandidates.ReadInt64Superlane(lBad.AsSpan(0, 72), dst8))
            {
                ok = false;
                Console.WriteLine("NG LadderSuperlane Int64 overflow accepted");
            }
        }
        return ok;
    }
}
