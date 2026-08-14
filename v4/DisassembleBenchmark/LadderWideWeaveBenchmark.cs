// int16/int64 wide-superlane weave tiers (512/256/128) vs the constant-stride scalar
// emit/decode. Companion to Int32WideWeaveBenchmark; closes the last two token shapes.
//
// Background. After the int32 round, the remaining all-scalar wide superlanes are the
// 3-byte ([code][2B BE], codes 0xcd/0xd1) and 9-byte ([code][8B BE], codes 0xcf/0xd3)
// tokens, which are scalar at EVERY tier including AVX-512VBMI. The 9-byte shape is
// float64's with a sign-selected code, so the double weave/gather tables apply directly.
// The 3-byte shape has no float analog: a 16-short block becomes a 48-byte run, and the
// candidates below split it as one 64B vpermi2b (512, stored as 32+16 exact), one
// 10-token vpermq+vpshufb window plus a 5-token half window plus one scalar token (256),
// or three 5-token windows plus one scalar token (128).
//
// Layout notes. Short 256: vpermq 0b10_01_01_00 arranges lanes [e0-7][e4-11], so tokens
// 0-5 shuffle from lane 0 (token 5's code rides at byte 15) and tokens 5-9's payloads
// plus 6-9's codes from lane 1. The 5-token half windows read elements 10-14 from a
// 128-bit load at element 8 with tables shifted by two words (a load at element 10 would
// read past the block). Long 256 mirrors the double 256 weave with a code-scatter
// shuffle replacing the constant OR; the block tail is the exact-18B double-128 pair
// with both codes and the last payload byte scalar. Decode gates extend the int32
// movemask recipe: code positions every 3 (mask 0x9249249 for 10 tokens, 0x1249 for 5)
// or every 9 (0x40201 for 3), unsigned-overflow via the code mask shifted onto the
// first payload byte's movemask bit. The long 512 decode reuses the double gather
// tables outright and masks validation to lanes 0-6 (lane 7 is the garbage lane).
//
// Environment note discovered during this round: BenchmarkDotNet child processes do NOT
// inherit DOTNET_* runtime knobs, so DOTNET_EnableAVX512F=0 never reaches the measured
// process (verified: the child log prints the full AVX-512 feature set). Tier forcing
// works for dotnet test and --verify, which run in-process. Measurements therefore rely
// on candidates calling their tier's intrinsics directly; both runs below are the same
// full-AVX-512 environment and agreed within ShortRun noise.
//
// RESULTS (Zen 5, ShortRun, ns/element, N=1000 / N=100000):
//   short ser  scalar 0.82/0.67   W512 0.046/0.039   W256 0.090/0.084   W128 0.098/0.090
//   short dec  scalar 1.17/1.19   W512 0.081/0.084   W256 0.137/0.138   W128 0.226/0.231
//   long  ser  scalar 0.64/0.80   W512 0.113/0.146   W512Overlap 0.099/0.126
//              W256 0.201/0.235   W128 0.568/0.585
//   long  dec  scalar 1.10/1.11   W512 0.222/0.217   W256 0.369/0.378   W128 1.10/1.11
// VERDICT: every short tier adopted (512 17x, 256 ~9x, 128 ~8x serialize; 14x/8.6x/5.2x
// decode) into Int16/UInt16ElementCodec (the VBMI weave lives inside the 256 probe tier,
// which is the widest short probe). Long: the Overlap 512 variant beats the scalar-tail
// one (~12%) and is adopted with +1 byte of region reservation slack; 256 adopted
// (3.2x); decode 512/256 adopted (5x/3x). Long W128 serialize measured 1.13-1.36x but
// has no 128 probe tier to live in (absent by design) and stays unadopted; long W128
// decode is a WASH (the per-token scalar gate eats the byte-reverse gain), rejected.
// UInt16/UInt64 get the same tiers with constant-code fills and single-pattern gates.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class LadderWideWeaveBenchmark
{
    [Params(1000, 100_000)]
    public int N = 1000;

    short[] sdata = default!;
    long[] ldata = default!;
    byte[] sbuf = default!;
    byte[] lbuf = default!;
    byte[] sElems = default!;
    byte[] lElems = default!;
    short[] sdst = default!;
    long[] ldst = default!;

    public static (short[] Shorts, long[] Longs) MakeWideData(int n, int seed)
    {
        var rand = new Random(seed);
        var shorts = new short[n];
        var longs = new long[n];
        for (int i = 0; i < n; i++)
        {
            // all-wide, random sign: short outside [-128, 255], long outside [int.Min, uint.Max]
            int sm = rand.Next(256, 32768);
            shorts[i] = (short)(rand.Next(2) == 0 ? sm : -sm);
            long lm = rand.NextInt64(4294967296L, long.MaxValue);
            longs[i] = rand.Next(2) == 0 ? lm : -lm;
        }
        if (n >= 2)
        {
            shorts[0] = short.MaxValue;
            shorts[n - 1] = short.MinValue;
            longs[0] = long.MaxValue;
            longs[n - 1] = long.MinValue;
        }
        return (shorts, longs);
    }

    [GlobalSetup]
    public void Setup()
    {
        (sdata, ldata) = MakeWideData(N, 42);
        sbuf = new byte[3 * N + 64];
        lbuf = new byte[9 * N + 64];
        sdst = new short[N];
        ldst = new long[N];
        sElems = new byte[3 * N + 64];
        lElems = new byte[9 * N + 64];
        LadderWideWeaveCandidates.WriteShortWideScalar(ref MemoryMarshal.GetArrayDataReference(sElems), ref MemoryMarshal.GetArrayDataReference(sdata), N);
        LadderWideWeaveCandidates.WriteLongWideScalar(ref MemoryMarshal.GetArrayDataReference(lElems), ref MemoryMarshal.GetArrayDataReference(ldata), N);

        if (!LadderWideWeaveVerify.VerifyAll(sdata, ldata)) throw new InvalidOperationException("verify failed: ladder wide weave candidates");
    }

    // ---- short serialize ----

    [Benchmark(Baseline = true), BenchmarkCategory("ShortWideSer")]
    public int ShortWideScalar() => LadderWideWeaveCandidates.WriteShortWideScalar(ref MemoryMarshal.GetArrayDataReference(sbuf), ref MemoryMarshal.GetArrayDataReference(sdata), N);

    [Benchmark, BenchmarkCategory("ShortWideSer")]
    public int ShortWideW512() => LadderWideWeaveCandidates.WriteShortWide512(ref MemoryMarshal.GetArrayDataReference(sbuf), ref MemoryMarshal.GetArrayDataReference(sdata), N);

    [Benchmark, BenchmarkCategory("ShortWideSer")]
    public int ShortWideW256() => LadderWideWeaveCandidates.WriteShortWide256(ref MemoryMarshal.GetArrayDataReference(sbuf), ref MemoryMarshal.GetArrayDataReference(sdata), N);

    [Benchmark, BenchmarkCategory("ShortWideSer")]
    public int ShortWideW128() => LadderWideWeaveCandidates.WriteShortWide128(ref MemoryMarshal.GetArrayDataReference(sbuf), ref MemoryMarshal.GetArrayDataReference(sdata), N);

    // ---- short deserialize ----

    [Benchmark(Baseline = true), BenchmarkCategory("ShortWideDeser")]
    public bool ShortWideDecScalar() => LadderWideWeaveCandidates.ReadShortWideScalar(ref MemoryMarshal.GetArrayDataReference(sElems), ref MemoryMarshal.GetArrayDataReference(sdst), N);

    [Benchmark, BenchmarkCategory("ShortWideDeser")]
    public bool ShortWideDecW512() => LadderWideWeaveCandidates.ReadShortWide512(ref MemoryMarshal.GetArrayDataReference(sElems), ref MemoryMarshal.GetArrayDataReference(sdst), N);

    [Benchmark, BenchmarkCategory("ShortWideDeser")]
    public bool ShortWideDecW256() => LadderWideWeaveCandidates.ReadShortWide256(ref MemoryMarshal.GetArrayDataReference(sElems), ref MemoryMarshal.GetArrayDataReference(sdst), N);

    [Benchmark, BenchmarkCategory("ShortWideDeser")]
    public bool ShortWideDecW128() => LadderWideWeaveCandidates.ReadShortWide128(ref MemoryMarshal.GetArrayDataReference(sElems), ref MemoryMarshal.GetArrayDataReference(sdst), N);

    // ---- long serialize ----

    [Benchmark(Baseline = true), BenchmarkCategory("LongWideSer")]
    public int LongWideScalar() => LadderWideWeaveCandidates.WriteLongWideScalar(ref MemoryMarshal.GetArrayDataReference(lbuf), ref MemoryMarshal.GetArrayDataReference(ldata), N);

    [Benchmark, BenchmarkCategory("LongWideSer")]
    public int LongWideW512() => LadderWideWeaveCandidates.WriteLongWide512(ref MemoryMarshal.GetArrayDataReference(lbuf), ref MemoryMarshal.GetArrayDataReference(ldata), N);

    [Benchmark, BenchmarkCategory("LongWideSer")]
    public int LongWideW512Overlap() => LadderWideWeaveCandidates.WriteLongWide512Overlap(ref MemoryMarshal.GetArrayDataReference(lbuf), ref MemoryMarshal.GetArrayDataReference(ldata), N);

    [Benchmark, BenchmarkCategory("LongWideSer")]
    public int LongWideW256() => LadderWideWeaveCandidates.WriteLongWide256(ref MemoryMarshal.GetArrayDataReference(lbuf), ref MemoryMarshal.GetArrayDataReference(ldata), N);

    [Benchmark, BenchmarkCategory("LongWideSer")]
    public int LongWideW128() => LadderWideWeaveCandidates.WriteLongWide128(ref MemoryMarshal.GetArrayDataReference(lbuf), ref MemoryMarshal.GetArrayDataReference(ldata), N);

    // ---- long deserialize ----

    [Benchmark(Baseline = true), BenchmarkCategory("LongWideDeser")]
    public bool LongWideDecScalar() => LadderWideWeaveCandidates.ReadLongWideScalar(ref MemoryMarshal.GetArrayDataReference(lElems), ref MemoryMarshal.GetArrayDataReference(ldst), N);

    [Benchmark, BenchmarkCategory("LongWideDeser")]
    public bool LongWideDecW512() => LadderWideWeaveCandidates.ReadLongWide512(ref MemoryMarshal.GetArrayDataReference(lElems), ref MemoryMarshal.GetArrayDataReference(ldst), N);

    [Benchmark, BenchmarkCategory("LongWideDeser")]
    public bool LongWideDecW256() => LadderWideWeaveCandidates.ReadLongWide256(ref MemoryMarshal.GetArrayDataReference(lElems), ref MemoryMarshal.GetArrayDataReference(ldst), N);

    [Benchmark, BenchmarkCategory("LongWideDeser")]
    public bool LongWideDecW128() => LadderWideWeaveCandidates.ReadLongWide128(ref MemoryMarshal.GetArrayDataReference(lElems), ref MemoryMarshal.GetArrayDataReference(ldst), N);
}

static class LadderWideBenchTables
{
    const byte B = byte.MaxValue; // pshufb-zeroed slot
    const byte Z = 0x80;          // pshufb-zeroed slot (decode tables)

    // ---- short serialize ----

    // 512: one vpermi2b builds the whole 48-byte run. Source 1 = 16 shorts (LE words at
    // bytes 2t..2t+1), source 2 = the sign-selected code words (low byte at 2t, index
    // 64 + 2t). Token t = [64+2t, 2t+1, 2t]; bytes 48-63 are garbage (stored split as
    // 32 + 16 so nothing past the run is written).
    public static readonly Vector512<byte> ShortIdx512 = Vector512.Create(
        (byte)64, 1, 0, 66, 3, 2, 68, 5, 4, 70, 7, 6,
        72, 9, 8, 74, 11, 10, 76, 13, 12, 78, 15, 14,
        80, 17, 16, 82, 19, 18, 84, 21, 20, 86, 23, 22,
        88, 25, 24, 90, 27, 26, 92, 29, 28, 94, 31, 30,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    // 256 (input vpermq'd to lanes [e0-7][e4-11]): payload bytes of tokens 0-4 from
    // lane 0, token 5's payload plus 6-9's from lane 1; code scatter fills positions 3t
    // (token 5's code rides at byte 15 from lane 0, element 5's low byte at 10).
    public static readonly Vector256<byte> ShortPay256 = Vector256.Create(
        B, 1, 0, B, 3, 2, B, 5, 4, B, 7, 6, B, 9, 8, B,
        3, 2, B, 5, 4, B, 7, 6, B, 9, 8, B, 11, 10, B, B);
    public static readonly Vector256<byte> ShortCode256 = Vector256.Create(
        0, B, B, 2, B, B, 4, B, B, 6, B, B, 8, B, B, 10,
        B, B, 4, B, B, 6, B, B, 8, B, B, 10, B, B, B, B);

    // 128: 5 tokens from elements 0-4 of an 8-short load (Lo), or elements 2-6 (Hi,
    // used with a load at element 8 to reach elements 10-14 without reading past the
    // block). The 16th byte of each store is the next window's code slot (rewritten).
    public static readonly Vector128<byte> ShortPay128 = Vector128.Create(
        B, 1, 0, B, 3, 2, B, 5, 4, B, 7, 6, B, 9, 8, B);
    public static readonly Vector128<byte> ShortCode128 = Vector128.Create(
        0, B, B, 2, B, B, 4, B, B, 6, B, B, 8, B, B, B);
    public static readonly Vector128<byte> ShortPayHi128 = Vector128.Create(
        B, 5, 4, B, 7, 6, B, 9, 8, B, 11, 10, B, 13, 12, B);
    public static readonly Vector128<byte> ShortCodeHi128 = Vector128.Create(
        4, B, B, 6, B, B, 8, B, B, 10, B, B, 12, B, B, B);

    // ---- short decode ----

    // 512: single-source gathers over a 64B window; word t = window bytes [3t+2, 3t+1]
    // (values) or [3t, 3t] (codes). Only the lower 32 output bytes are used.
    public static readonly Vector512<byte> ShortDecVals512 = Vector512.Create(
        (byte)2, 1, 5, 4, 8, 7, 11, 10, 14, 13, 17, 16, 20, 19, 23, 22,
        26, 25, 29, 28, 32, 31, 35, 34, 38, 37, 41, 40, 44, 43, 47, 46,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public static readonly Vector512<byte> ShortDecCodes512 = Vector512.Create(
        (byte)0, 0, 3, 3, 6, 6, 9, 9, 12, 12, 15, 15, 18, 18, 21, 21,
        24, 24, 27, 27, 30, 30, 33, 33, 36, 36, 39, 39, 42, 42, 45, 45,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    // 256: 10 tokens per 30B window; lane 0 decodes tokens 0-4, lane 1 tokens 5-9
    // (token 5's payload sits at window bytes 16-17, fully inside lane 1). The halves
    // store 10B apart; garbage tails heal under the next store or the scalar tail.
    public static readonly Vector256<byte> ShortDec256 = Vector256.Create(
        (byte)2, 1, 5, 4, 8, 7, 11, 10, 14, 13, Z, Z, Z, Z, Z, Z,
        1, 0, 4, 3, 7, 6, 10, 9, 13, 12, Z, Z, Z, Z, Z, Z);
    public static readonly Vector128<byte> ShortDec128 = Vector128.Create(
        (byte)2, 1, 5, 4, 8, 7, 11, 10, 14, 13, Z, Z, Z, Z, Z, Z);

    // ---- long serialize ----

    // 512: one vpermi2b builds tokens 0-6 (63 bytes; byte 63 is garbage, healed by
    // token 7's scalar code). Source 2 = sign-selected code qwords (low byte at 8t).
    public static readonly Vector512<byte> LongIdx512 = Vector512.Create(
        (byte)64, 7, 6, 5, 4, 3, 2, 1, 0,
        72, 15, 14, 13, 12, 11, 10, 9, 8,
        80, 23, 22, 21, 20, 19, 18, 17, 16,
        88, 31, 30, 29, 28, 27, 26, 25, 24,
        96, 39, 38, 37, 36, 35, 34, 33, 32,
        104, 47, 46, 45, 44, 43, 42, 41, 40,
        112, 55, 54, 53, 52, 51, 50, 49, 48,
        0);
    // overlap variant: tokens 1-7 from the same source, stored at offset +9 (rewrites
    // tokens 1-6 with identical bytes; 1 byte of overhang past the 72-byte run)
    public static readonly Vector512<byte> LongIdxShift512 = Vector512.Create(
        (byte)72, 15, 14, 13, 12, 11, 10, 9, 8,
        80, 23, 22, 21, 20, 19, 18, 17, 16,
        88, 31, 30, 29, 28, 27, 26, 25, 24,
        96, 39, 38, 37, 36, 35, 34, 33, 32,
        104, 47, 46, 45, 44, 43, 42, 41, 40,
        112, 55, 54, 53, 52, 51, 50, 49, 48,
        120, 63, 62, 61, 60, 59, 58, 57, 56,
        0);

    // 256: the double weave layout (vpermq lanes [e0,e1][e1,e2]) with a code scatter
    // instead of the constant OR; positions 0 and 9 from lane 0 (elements 0-1), position
    // 18 from lane 1 (element 2 at in-lane qword 1).
    public static readonly Vector256<byte> LongCode256 = Vector256.Create(
        0, B, B, B, B, B, B, B, B, 8, B, B, B, B, B, B,
        B, B, 8, B, B, B, B, B, B, B, B, B, B, B, B, B);
}

public static class LadderWideWeaveCandidates
{
    // ==== short serialize ====

    public static int WriteShortWideScalar(ref byte d, ref short src, int length)
    {
        int i = 0;
        int o = 0;
        for (; length - i >= 16; i += 16, o += 48)
        {
            EmitShortWide16(ref d, o, ref src, i);
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt16(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    static void EmitShortWide16(ref byte d, int written, ref short src, int i)
    {
        for (int t = 0; t < 16; t++)
        {
            short val = Unsafe.Add(ref src, i + t);
            Unsafe.Add(ref d, written + (t * 3)) = (byte)(0xcd ^ ((val >> 15) & 0x1c));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + (t * 3) + 1), BinaryPrimitives.ReverseEndianness((ushort)val));
        }
    }

    public static int WriteShortWide512(ref byte d, ref short src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            for (; length - i >= 16; i += 16, o += 48)
            {
                var v = Vector256.LoadUnsafe(ref src, (nuint)i);
                var codes = Vector256.Create((short)0x00cd) ^ (Vector256.ShiftRightArithmetic(v, 15) & Vector256.Create((short)0x001c));
                var r = Avx512Vbmi.PermuteVar64x8x2(v.AsByte().ToVector512(), LadderWideBenchTables.ShortIdx512, codes.AsByte().ToVector512());
                r.GetLower().StoreUnsafe(ref Unsafe.Add(ref d, o));
                r.GetUpper().GetLower().StoreUnsafe(ref Unsafe.Add(ref d, o + 32));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt16(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteShortWide256(ref byte d, ref short src, int length)
    {
        int i = 0;
        int o = 0;
        if (Avx2.IsSupported)
        {
            for (; length - i >= 16; i += 16, o += 48)
            {
                // tokens 0-9 from one vpermq'd window
                var p = Avx2.Permute4x64(Vector256.LoadUnsafe(ref src, (nuint)i).AsUInt64(), 0b10_01_01_00).AsInt16();
                var cp = (Vector256.Create((short)0x00cd) ^ (Vector256.ShiftRightArithmetic(p, 15) & Vector256.Create((short)0x001c))).AsByte();
                (Avx2.Shuffle(p.AsByte(), LadderWideBenchTables.ShortPay256) | Avx2.Shuffle(cp, LadderWideBenchTables.ShortCode256))
                    .StoreUnsafe(ref Unsafe.Add(ref d, o));
                // tokens 10-14 from a half window at element 8 (elements 10-14)
                var h = Vector128.LoadUnsafe(ref src, (nuint)(i + 8));
                var ch = (Vector128.Create((short)0x00cd) ^ (Vector128.ShiftRightArithmetic(h, 15) & Vector128.Create((short)0x001c))).AsByte();
                (Ssse3.Shuffle(h.AsByte(), LadderWideBenchTables.ShortPayHi128) | Ssse3.Shuffle(ch, LadderWideBenchTables.ShortCodeHi128))
                    .StoreUnsafe(ref Unsafe.Add(ref d, o + 30));
                // token 15 scalar (heals byte o+45)
                short v15 = Unsafe.Add(ref src, i + 15);
                Unsafe.Add(ref d, o + 45) = (byte)(0xcd ^ ((v15 >> 15) & 0x1c));
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, o + 46), BinaryPrimitives.ReverseEndianness((ushort)v15));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt16(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteShortWide128(ref byte d, ref short src, int length)
    {
        int i = 0;
        int o = 0;
        if (Ssse3.IsSupported)
        {
            for (; length - i >= 16; i += 16, o += 48)
            {
                var a = Vector128.LoadUnsafe(ref src, (nuint)i);
                var b = Vector128.LoadUnsafe(ref src, (nuint)(i + 5));
                var c = Vector128.LoadUnsafe(ref src, (nuint)(i + 8));
                var ca = (Vector128.Create((short)0x00cd) ^ (Vector128.ShiftRightArithmetic(a, 15) & Vector128.Create((short)0x001c))).AsByte();
                var cb = (Vector128.Create((short)0x00cd) ^ (Vector128.ShiftRightArithmetic(b, 15) & Vector128.Create((short)0x001c))).AsByte();
                var cc = (Vector128.Create((short)0x00cd) ^ (Vector128.ShiftRightArithmetic(c, 15) & Vector128.Create((short)0x001c))).AsByte();
                (Ssse3.Shuffle(a.AsByte(), LadderWideBenchTables.ShortPay128) | Ssse3.Shuffle(ca, LadderWideBenchTables.ShortCode128))
                    .StoreUnsafe(ref Unsafe.Add(ref d, o));
                (Ssse3.Shuffle(b.AsByte(), LadderWideBenchTables.ShortPay128) | Ssse3.Shuffle(cb, LadderWideBenchTables.ShortCode128))
                    .StoreUnsafe(ref Unsafe.Add(ref d, o + 15));
                (Ssse3.Shuffle(c.AsByte(), LadderWideBenchTables.ShortPayHi128) | Ssse3.Shuffle(cc, LadderWideBenchTables.ShortCodeHi128))
                    .StoreUnsafe(ref Unsafe.Add(ref d, o + 30));
                short v15 = Unsafe.Add(ref src, i + 15);
                Unsafe.Add(ref d, o + 45) = (byte)(0xcd ^ ((v15 >> 15) & 0x1c));
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, o + 46), BinaryPrimitives.ReverseEndianness((ushort)v15));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt16(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    // ==== short decode ====

    public static bool ReadShortWideScalar(ref byte s, ref short dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        for (; count - i >= 16; i += 16, o += 48)
        {
            ok &= TryDecodeShortWide16(ref Unsafe.Add(ref s, o), ref dst, i);
        }
        for (; i < count; i++, o += 3)
        {
            ok &= DecodeOneShort(ref s, o, ref dst, i);
        }
        return ok;
    }

    static bool TryDecodeShortWide16(ref byte w0, ref short dst, int i)
    {
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
    }

    static bool DecodeOneShort(ref byte s, int o, ref short dst, int i)
    {
        byte c = Unsafe.Add(ref s, o);
        ushort raw = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref s, o + 1)));
        Unsafe.Add(ref dst, i) = unchecked((short)raw);
        return ((c == 0xd1) | (c == 0xcd)) & !((c == 0xcd) & (raw > 0x7FFF));
    }

    public static bool ReadShortWide512(ref byte s, ref short dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            // needs 64 readable bytes per 48 consumed; the bench buffer has slack, the
            // codec gates on window.Length >= 64
            for (; count - i >= 16 && (3 * count + 16) - o >= 64; i += 16, o += 48)
            {
                var w = Vector512.LoadUnsafe(ref Unsafe.Add(ref s, o));
                var vals = Avx512Vbmi.PermuteVar64x8(w, LadderWideBenchTables.ShortDecVals512).GetLower().AsInt16();
                var codes = Avx512Vbmi.PermuteVar64x8(w, LadderWideBenchTables.ShortDecCodes512).GetLower().AsUInt16();
                var isCd = Vector256.Equals(codes, Vector256.Create((ushort)0xcdcd));
                var isD1 = Vector256.Equals(codes, Vector256.Create((ushort)0xd1d1));
                ok &= Vector256.EqualsAll(isCd | isD1, Vector256<ushort>.AllBitsSet);
                ok &= Vector256.GreaterThanOrEqualAll(isCd.AsInt16() & vals, Vector256<short>.Zero);
                vals.StoreUnsafe(ref dst, (nuint)i);
            }
        }
        for (; i < count; i++, o += 3)
        {
            ok &= DecodeOneShort(ref s, o, ref dst, i);
        }
        return ok;
    }

    public static bool ReadShortWide256(ref byte s, ref short dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Avx2.IsSupported)
        {
            for (; count - i >= 13; i += 10, o += 30)
            {
                var w = Vector256.LoadUnsafe(ref Unsafe.Add(ref s, o));
                uint cd = Vector256.Equals(w, Vector256.Create((byte)0xcd)).ExtractMostSignificantBits();
                uint d1 = Vector256.Equals(w, Vector256.Create((byte)0xd1)).ExtractMostSignificantBits();
                uint msb = w.ExtractMostSignificantBits();
                ok &= ((cd | d1) & 0x9249249u) == 0x9249249u;
                ok &= (((cd & 0x9249249u) << 1) & msb) == 0;
                var shuffled = Avx2.Shuffle(w, LadderWideBenchTables.ShortDec256);
                shuffled.GetLower().StoreUnsafe(ref Unsafe.As<short, byte>(ref dst), (nuint)(i * 2));
                shuffled.GetUpper().StoreUnsafe(ref Unsafe.As<short, byte>(ref dst), (nuint)((i * 2) + 10));
            }
        }
        for (; i < count; i++, o += 3)
        {
            ok &= DecodeOneShort(ref s, o, ref dst, i);
        }
        return ok;
    }

    public static bool ReadShortWide128(ref byte s, ref short dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Ssse3.IsSupported)
        {
            for (; count - i >= 8; i += 5, o += 15)
            {
                var w = Vector128.LoadUnsafe(ref Unsafe.Add(ref s, o));
                uint cd = Vector128.Equals(w, Vector128.Create((byte)0xcd)).ExtractMostSignificantBits();
                uint d1 = Vector128.Equals(w, Vector128.Create((byte)0xd1)).ExtractMostSignificantBits();
                uint msb = w.ExtractMostSignificantBits();
                ok &= ((cd | d1) & 0x1249u) == 0x1249u;
                ok &= (((cd & 0x1249u) << 1) & msb) == 0;
                Ssse3.Shuffle(w, LadderWideBenchTables.ShortDec128).StoreUnsafe(ref Unsafe.As<short, byte>(ref dst), (nuint)(i * 2));
            }
        }
        for (; i < count; i++, o += 3)
        {
            ok &= DecodeOneShort(ref s, o, ref dst, i);
        }
        return ok;
    }

    // ==== long serialize ====

    public static int WriteLongWideScalar(ref byte d, ref long src, int length)
    {
        int i = 0;
        int o = 0;
        for (; length - i >= 8; i += 8, o += 72)
        {
            EmitLongWide8(ref d, o, ref src, i);
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt64(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    static void EmitLongWide8(ref byte d, int written, ref long src, int i)
    {
        for (int t = 0; t < 8; t++)
        {
            long val = Unsafe.Add(ref src, i + t);
            Unsafe.Add(ref d, written + (t * 9)) = (byte)(0xcf ^ (int)((val >> 63) & 0x1c));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + (t * 9) + 1), BinaryPrimitives.ReverseEndianness((ulong)val));
        }
    }

    public static int WriteLongWide512(ref byte d, ref long src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            for (; length - i >= 8; i += 8, o += 72)
            {
                var v = Vector512.LoadUnsafe(ref src, (nuint)i);
                var codes = (Vector512.Create(0x00000000000000cfL) ^ (Vector512.ShiftRightArithmetic(v, 63) & Vector512.Create(0x000000000000001cL))).AsByte();
                Avx512Vbmi.PermuteVar64x8x2(v.AsByte(), LadderWideBenchTables.LongIdx512, codes)
                    .StoreUnsafe(ref Unsafe.Add(ref d, o));
                // token 7 scalar; its code heals the store's garbage byte at o+63
                long v7 = Unsafe.Add(ref src, i + 7);
                Unsafe.Add(ref d, o + 63) = (byte)(0xcf ^ (int)((v7 >> 63) & 0x1c));
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, o + 64), BinaryPrimitives.ReverseEndianness((ulong)v7));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt64(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteLongWide512Overlap(ref byte d, ref long src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            for (; length - i >= 8; i += 8, o += 72)
            {
                var v = Vector512.LoadUnsafe(ref src, (nuint)i);
                var codes = (Vector512.Create(0x00000000000000cfL) ^ (Vector512.ShiftRightArithmetic(v, 63) & Vector512.Create(0x000000000000001cL))).AsByte();
                Avx512Vbmi.PermuteVar64x8x2(v.AsByte(), LadderWideBenchTables.LongIdx512, codes)
                    .StoreUnsafe(ref Unsafe.Add(ref d, o));
                Avx512Vbmi.PermuteVar64x8x2(v.AsByte(), LadderWideBenchTables.LongIdxShift512, codes)
                    .StoreUnsafe(ref Unsafe.Add(ref d, o + 9));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt64(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteLongWide256(ref byte d, ref long src, int length)
    {
        int i = 0;
        int o = 0;
        if (Avx2.IsSupported)
        {
            for (; length - i >= 8; i += 8, o += 72)
            {
                Weave3Long(ref d, o, Vector256.LoadUnsafe(ref src, (nuint)i));
                Weave3Long(ref d, o + 27, Vector256.LoadUnsafe(ref src, (nuint)(i + 3)));
                // tokens 6-7 as the exact-18B pair; the vector heals bytes o+54..o+58
                var pair = Vector128.LoadUnsafe(ref src, (nuint)(i + 6)).AsByte();
                Ssse3.Shuffle(pair, FloatWeaveBenchTablesBridge.DoubleIndices128).StoreUnsafe(ref Unsafe.Add(ref d, o + 55));
                long v6 = Unsafe.Add(ref src, i + 6);
                long v7 = Unsafe.Add(ref src, i + 7);
                Unsafe.Add(ref d, o + 54) = (byte)(0xcf ^ (int)((v6 >> 63) & 0x1c));
                Unsafe.Add(ref d, o + 63) = (byte)(0xcf ^ (int)((v7 >> 63) & 0x1c));
                Unsafe.Add(ref d, o + 71) = (byte)v7;
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt64(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Weave3Long(ref byte d, int at, Vector256<long> loaded)
    {
        var p = Avx2.Permute4x64(loaded.AsUInt64(), 0b10_01_01_00).AsInt64();
        var cp = (Vector256.Create(0x00000000000000cfL) ^ (Vector256.LessThan(p, Vector256<long>.Zero) & Vector256.Create(0x000000000000001cL))).AsByte();
        (Avx2.Shuffle(p.AsByte(), FloatWeaveBenchTablesBridge.DoubleIndices256) | Avx2.Shuffle(cp, LadderWideBenchTables.LongCode256))
            .StoreUnsafe(ref Unsafe.Add(ref d, at));
    }

    public static int WriteLongWide128(ref byte d, ref long src, int length)
    {
        int i = 0;
        int o = 0;
        if (Ssse3.IsSupported)
        {
            for (; length - i >= 8; i += 8, o += 72)
            {
                for (int p = 0; p < 4; p++)
                {
                    int at = o + (p * 18);
                    var pair = Vector128.LoadUnsafe(ref src, (nuint)(i + (p * 2))).AsByte();
                    Ssse3.Shuffle(pair, FloatWeaveBenchTablesBridge.DoubleIndices128).StoreUnsafe(ref Unsafe.Add(ref d, at + 1));
                    long v0 = Unsafe.Add(ref src, i + (p * 2));
                    long v1 = Unsafe.Add(ref src, i + (p * 2) + 1);
                    Unsafe.Add(ref d, at) = (byte)(0xcf ^ (int)((v0 >> 63) & 0x1c));
                    Unsafe.Add(ref d, at + 9) = (byte)(0xcf ^ (int)((v1 >> 63) & 0x1c));
                    Unsafe.Add(ref d, at + 17) = (byte)v1;
                }
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt64(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    // ==== long decode ====

    public static bool ReadLongWideScalar(ref byte s, ref long dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        for (; count - i >= 8; i += 8, o += 72)
        {
            ok &= TryDecodeLongWide8(ref Unsafe.Add(ref s, o), ref dst, i);
        }
        for (; i < count; i++, o += 9)
        {
            ok &= DecodeOneLong(ref s, o, ref dst, i);
        }
        return ok;
    }

    static bool TryDecodeLongWide8(ref byte w0, ref long dst, int i)
    {
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
    }

    static bool DecodeOneLong(ref byte s, int o, ref long dst, int i)
    {
        byte c = Unsafe.Add(ref s, o);
        ulong raw = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s, o + 1)));
        Unsafe.Add(ref dst, i) = unchecked((long)raw);
        return ((c == 0xd3) | (c == 0xcf)) & !((c == 0xcf) & ((long)raw < 0));
    }

    public static bool ReadLongWide512(ref byte s, ref long dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            // the double gather tables apply as-is; validation masks to lanes 0-6 (lane
            // 7 is the garbage lane, its dst slot is re-decoded by the next iteration)
            for (; count - i >= 8 && (9 * count + 16) - o >= 64; i += 7, o += 63)
            {
                var w = Vector512.LoadUnsafe(ref Unsafe.Add(ref s, o));
                var vals = Avx512Vbmi.PermuteVar64x8(w, FloatWeaveBenchTablesBridge.DoubleDecodeValues).AsInt64();
                var codes = Avx512Vbmi.PermuteVar64x8(w, FloatWeaveBenchTablesBridge.DoubleDecodeCodes).AsInt64();
                ulong cf = Vector512.Equals(codes, Vector512.Create(unchecked((long)0xCFCFCFCFCFCFCFCF))).ExtractMostSignificantBits();
                ulong d3 = Vector512.Equals(codes, Vector512.Create(unchecked((long)0xD3D3D3D3D3D3D3D3))).ExtractMostSignificantBits();
                ulong neg = vals.ExtractMostSignificantBits();
                ok &= ((cf | d3) & 0x7FUL) == 0x7FUL;
                ok &= (cf & neg & 0x7FUL) == 0;
                vals.StoreUnsafe(ref dst, (nuint)i);
            }
        }
        for (; i < count; i++, o += 9)
        {
            ok &= DecodeOneLong(ref s, o, ref dst, i);
        }
        return ok;
    }

    public static bool ReadLongWide256(ref byte s, ref long dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Avx2.IsSupported)
        {
            for (; count - i >= 4; i += 3, o += 27)
            {
                var w = Vector256.LoadUnsafe(ref Unsafe.Add(ref s, o));
                uint cf = Vector256.Equals(w, Vector256.Create((byte)0xcf)).ExtractMostSignificantBits();
                uint d3 = Vector256.Equals(w, Vector256.Create((byte)0xd3)).ExtractMostSignificantBits();
                uint msb = w.ExtractMostSignificantBits();
                ok &= ((cf | d3) & 0x40201u) == 0x40201u;
                ok &= (((cf & 0x40201u) << 1) & msb) == 0;
                var vB = Avx2.Permute4x64(w.AsUInt64(), 0b00_00_10_01).AsByte();
                (Avx2.Shuffle(w, FloatWeaveBenchTablesBridge.DoubleDecShuffleA256) | Avx2.Shuffle(vB, FloatWeaveBenchTablesBridge.DoubleDecShuffleB256))
                    .StoreUnsafe(ref Unsafe.As<long, byte>(ref dst), (nuint)(i * 8));
            }
        }
        for (; i < count; i++, o += 9)
        {
            ok &= DecodeOneLong(ref s, o, ref dst, i);
        }
        return ok;
    }

    public static bool ReadLongWide128(ref byte s, ref long dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Ssse3.IsSupported)
        {
            for (; count - i >= 2; i += 1, o += 9)
            {
                byte c = Unsafe.Add(ref s, o);
                ok &= ((c == 0xd3) | (c == 0xcf)) & !((c == 0xcf) & (Unsafe.Add(ref s, o + 1) >= 0x80));
                var w = Vector128.LoadUnsafe(ref Unsafe.Add(ref s, o));
                Ssse3.Shuffle(w, FloatWeaveBenchTablesBridge.DoubleDecShuffle128).StoreUnsafe(ref Unsafe.As<long, byte>(ref dst), (nuint)(i * 8));
            }
        }
        for (; i < count; i++, o += 9)
        {
            ok &= DecodeOneLong(ref s, o, ref dst, i);
        }
        return ok;
    }
}

// the double tables live in WeaveBenchTables (FloatWeaveBenchmark.cs); re-exposed here
// under a bridge name so this file reads standalone
static class FloatWeaveBenchTablesBridge
{
    public static Vector256<byte> DoubleIndices256 => WeaveBenchTables.DoubleIndices256;
    public static Vector128<byte> DoubleIndices128 => WeaveBenchTables.DoubleIndices128;
    public static Vector256<byte> DoubleDecShuffleA256 => WeaveBenchTables.DoubleDecShuffleA256;
    public static Vector256<byte> DoubleDecShuffleB256 => WeaveBenchTables.DoubleDecShuffleB256;
    public static Vector128<byte> DoubleDecShuffle128 => WeaveBenchTables.DoubleDecShuffle128;
    public static Vector512<byte> DoubleDecodeValues => WeaveBenchTables.DoubleDecodeValues;
    public static Vector512<byte> DoubleDecodeCodes => WeaveBenchTables.DoubleDecodeCodes;
}

public static class LadderWideWeaveVerify
{
    public static bool VerifyAll(short[] sdata, long[] ldata)
    {
        var ok = true;
        int n = sdata.Length;

        // ---- short ----
        var sExpected = new byte[(3 * n) + 64];
        sExpected.AsSpan().Fill(0xAA);
        int sLen = LadderWideWeaveCandidates.WriteShortWideScalar(ref MemoryMarshal.GetArrayDataReference(sExpected), ref MemoryMarshal.GetArrayDataReference(sdata), n);
        if (sLen != 3 * n)
        {
            Console.WriteLine($"NG LadderWideWeave n={n}: short test data is not all-wide");
            return false;
        }
        var sWriters = new (string Name, Func<byte[], int> Fn)[]
        {
            ("WriteShortWide512", buf => LadderWideWeaveCandidates.WriteShortWide512(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(sdata), n)),
            ("WriteShortWide256", buf => LadderWideWeaveCandidates.WriteShortWide256(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(sdata), n)),
            ("WriteShortWide128", buf => LadderWideWeaveCandidates.WriteShortWide128(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(sdata), n)),
        };
        foreach (var (name, fn) in sWriters)
        {
            var buf = new byte[(3 * n) + 64];
            buf.AsSpan().Fill(0x55);
            int len = fn(buf);
            if (len != sLen || !buf.AsSpan(0, sLen).SequenceEqual(sExpected.AsSpan(0, sLen)))
            {
                Console.WriteLine($"NG LadderWideWeave n={n} {name}: wire mismatch");
                ok = false;
            }
        }
        var sReaders = new (string Name, Func<byte[], short[], bool> Fn)[]
        {
            ("ReadShortWideScalar", (wire, dst) => LadderWideWeaveCandidates.ReadShortWideScalar(ref MemoryMarshal.GetArrayDataReference(wire), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadShortWide512", (wire, dst) => LadderWideWeaveCandidates.ReadShortWide512(ref MemoryMarshal.GetArrayDataReference(wire), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadShortWide256", (wire, dst) => LadderWideWeaveCandidates.ReadShortWide256(ref MemoryMarshal.GetArrayDataReference(wire), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadShortWide128", (wire, dst) => LadderWideWeaveCandidates.ReadShortWide128(ref MemoryMarshal.GetArrayDataReference(wire), ref MemoryMarshal.GetArrayDataReference(dst), n)),
        };
        foreach (var (name, fn) in sReaders)
        {
            var dst = new short[n];
            Array.Fill(dst, unchecked((short)0xBEEF));
            if (!fn(sExpected, dst) || !dst.AsSpan().SequenceEqual(sdata))
            {
                Console.WriteLine($"NG LadderWideWeave n={n} {name}: short decode mismatch");
                ok = false;
            }
        }
        if (n >= 1)
        {
            var corrupted = (byte[])sExpected.Clone();
            corrupted[0] = 0xca;
            var overflow = (byte[])sExpected.Clone();
            overflow[0] = 0xcd;
            overflow[1] = 0x80;
            foreach (var (name, fn) in sReaders)
            {
                var dst = new short[n];
                if (fn(corrupted, dst))
                {
                    Console.WriteLine($"NG LadderWideWeave n={n} {name}: accepted corrupted code");
                    ok = false;
                }
                if (fn(overflow, dst))
                {
                    Console.WriteLine($"NG LadderWideWeave n={n} {name}: accepted uint16 overflow");
                    ok = false;
                }
            }
        }

        // ---- long ----
        var lExpected = new byte[(9 * n) + 64];
        lExpected.AsSpan().Fill(0xAA);
        int lLen = LadderWideWeaveCandidates.WriteLongWideScalar(ref MemoryMarshal.GetArrayDataReference(lExpected), ref MemoryMarshal.GetArrayDataReference(ldata), n);
        if (lLen != 9 * n)
        {
            Console.WriteLine($"NG LadderWideWeave n={n}: long test data is not all-wide");
            return false;
        }
        var lWriters = new (string Name, Func<byte[], int> Fn)[]
        {
            ("WriteLongWide512", buf => LadderWideWeaveCandidates.WriteLongWide512(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(ldata), n)),
            ("WriteLongWide512Overlap", buf => LadderWideWeaveCandidates.WriteLongWide512Overlap(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(ldata), n)),
            ("WriteLongWide256", buf => LadderWideWeaveCandidates.WriteLongWide256(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(ldata), n)),
            ("WriteLongWide128", buf => LadderWideWeaveCandidates.WriteLongWide128(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(ldata), n)),
        };
        foreach (var (name, fn) in lWriters)
        {
            var buf = new byte[(9 * n) + 64];
            buf.AsSpan().Fill(0x55);
            int len = fn(buf);
            if (len != lLen || !buf.AsSpan(0, lLen).SequenceEqual(lExpected.AsSpan(0, lLen)))
            {
                Console.WriteLine($"NG LadderWideWeave n={n} {name}: long wire mismatch");
                ok = false;
            }
        }
        var lReaders = new (string Name, Func<byte[], long[], bool> Fn)[]
        {
            ("ReadLongWideScalar", (wire, dst) => LadderWideWeaveCandidates.ReadLongWideScalar(ref MemoryMarshal.GetArrayDataReference(wire), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadLongWide512", (wire, dst) => LadderWideWeaveCandidates.ReadLongWide512(ref MemoryMarshal.GetArrayDataReference(wire), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadLongWide256", (wire, dst) => LadderWideWeaveCandidates.ReadLongWide256(ref MemoryMarshal.GetArrayDataReference(wire), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadLongWide128", (wire, dst) => LadderWideWeaveCandidates.ReadLongWide128(ref MemoryMarshal.GetArrayDataReference(wire), ref MemoryMarshal.GetArrayDataReference(dst), n)),
        };
        foreach (var (name, fn) in lReaders)
        {
            var dst = new long[n];
            Array.Fill(dst, unchecked((long)0xDEADBEEFDEADBEEF));
            if (!fn(lExpected, dst) || !dst.AsSpan().SequenceEqual(ldata))
            {
                Console.WriteLine($"NG LadderWideWeave n={n} {name}: long decode mismatch");
                ok = false;
            }
        }
        if (n >= 1)
        {
            var corrupted = (byte[])lExpected.Clone();
            corrupted[0] = 0xca;
            var overflow = (byte[])lExpected.Clone();
            overflow[0] = 0xcf;
            overflow[1] = 0x80;
            foreach (var (name, fn) in lReaders)
            {
                var dst = new long[n];
                if (fn(corrupted, dst))
                {
                    Console.WriteLine($"NG LadderWideWeave n={n} {name}: accepted corrupted code");
                    ok = false;
                }
                if (fn(overflow, dst))
                {
                    Console.WriteLine($"NG LadderWideWeave n={n} {name}: accepted uint64 overflow");
                    ok = false;
                }
            }
        }
        return ok;
    }
}
