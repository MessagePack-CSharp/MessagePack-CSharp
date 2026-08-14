// int32 wide-superlane 256/128 tiers: weave vs the constant-stride scalar emit/decode.
//
// Background. The int32 codec's all-wide superlane (16 verified 5-byte tokens) runs a
// vpermi2b weave on AVX-512VBMI but falls back to scalar EmitWide16 / TryDecodeWide16
// everywhere else, which is most consumer hardware. The float round proved the token
// shape [code][4B BE] weaves fine at 256/128 (vpshufb in-lane, vpermq for lane
// arrangement); int32 differs only in the code byte, which is not a constant: 0xce
// (uint32) for non-negative, 0xd2 (int32) for negative, selected branchlessly as
// 0xce ^ (sign & 0x1c). Serialize adds one extra vpshufb that scatters the computed
// code bytes into the code slots (float ORs a constant vector instead). Decode adds a
// second Equals for the 0xd2 alternative plus the uint-overflow test: a 0xce token
// whose payload has its MSB set must fall to the scalar reader to throw, checked for
// all six tokens at once by shifting the ce code-position mask onto the first payload
// byte's movemask bit.
//
// The superlane is probed in 16-element blocks, and 16 does not divide by the 6-token
// 256 window. Serialize candidates try both remedies: x3 emits overlapping windows
// (t0-5, t6-11, t10-15; the middle store's garbage tail is rewritten, the last store
// overhangs the 80-byte run by 2 bytes, which costs the codec +2 bytes of region
// reservation slack), x2S4 emits two windows plus a 4-token scalar tail (no overhang).
// The 128 candidate emits five 3-token windows plus one scalar token (exact 80 bytes).
// Decode is not block-bound (the outer loop has no alignment), so decode candidates
// just consume 6 (256) or 3 (128) tokens per iteration like the float decode weaves.
//
// Environment correction (found in the LadderWideWeave round): the run below was
// labeled DOTNET_EnableAVX512F=0 but BenchmarkDotNet children do not inherit DOTNET_*
// knobs, so it actually ran with full AVX-512 enabled. The comparison stands: every
// candidate calls its tier's intrinsics directly, so the rows measure the AVX2/SSSE3
// code paths as such.
//
// RESULTS (Zen 5, ShortRun, ns/element, N=1000 / N=100000):
//   ser   scalar 0.91/0.69   W256x3 0.100/0.099   W256x2S4 0.208/0.193   W128x5 0.253/0.202
//   deser scalar 1.12/1.12   W256 0.238/0.254     W128 0.401/0.416
// VERDICT: W256x3 (7-9x) adopted as the AVX2 serialize tier; W256x2S4 rejected, the
// 4-token scalar tail alone doubles the block cost while the x3 overhang costs only +2
// bytes of region reservation slack. W128x5 (3.6x) adopted for SSSE3. Both decode
// weaves adopted (4.4-4.7x / 2.7x); NEON keeps the constant-stride scalar paths, whose
// movemask-free gates were never measured there. UInt32ElementCodec gets the same tiers
// with a constant 0xce code vector (no sign select, 0xd2 goes to the scalar reader).

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Int32WideWeaveBenchmark
{
    [Params(1000, 100_000)]
    public int N = 1000;

    int[] data = default!;
    byte[] buf = default!;
    byte[] elems = default!;
    int[] dst = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        data = new int[N];
        for (int i = 0; i < N; i++)
        {
            // all-wide, random sign: |v| > 65536 so every element needs the 5-byte token
            int magnitude = rand.Next(65537, int.MaxValue);
            data[i] = rand.Next(2) == 0 ? magnitude : -magnitude;
        }
        buf = new byte[5 * N + 64];
        dst = new int[N];
        elems = new byte[5 * N + 64];
        Int32WideWeaveCandidates.WriteWideScalar(ref MemoryMarshal.GetArrayDataReference(elems), ref MemoryMarshal.GetArrayDataReference(data), N);

        if (!Int32WideWeaveVerify.VerifyAll(data)) throw new InvalidOperationException("verify failed: int32 wide weave candidates");
    }

    // ---- serialize ----

    [Benchmark(Baseline = true), BenchmarkCategory("Int32WideSer")]
    public int WideScalar() => Int32WideWeaveCandidates.WriteWideScalar(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(data), N);

    [Benchmark, BenchmarkCategory("Int32WideSer")]
    public int WideW256x3() => Int32WideWeaveCandidates.WriteWide256x3(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(data), N);

    [Benchmark, BenchmarkCategory("Int32WideSer")]
    public int WideW256x2S4() => Int32WideWeaveCandidates.WriteWide256x2S4(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(data), N);

    [Benchmark, BenchmarkCategory("Int32WideSer")]
    public int WideW128x5() => Int32WideWeaveCandidates.WriteWide128x5(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(data), N);

    // ---- deserialize ----

    [Benchmark(Baseline = true), BenchmarkCategory("Int32WideDeser")]
    public bool WideDecodeScalar() => Int32WideWeaveCandidates.ReadWideScalar(ref MemoryMarshal.GetArrayDataReference(elems), ref MemoryMarshal.GetArrayDataReference(dst), N);

    [Benchmark, BenchmarkCategory("Int32WideDeser")]
    public bool WideDecodeW256() => Int32WideWeaveCandidates.ReadWide256(ref MemoryMarshal.GetArrayDataReference(elems), ref MemoryMarshal.GetArrayDataReference(dst), N);

    [Benchmark, BenchmarkCategory("Int32WideDeser")]
    public bool WideDecodeW128() => Int32WideWeaveCandidates.ReadWide128(ref MemoryMarshal.GetArrayDataReference(elems), ref MemoryMarshal.GetArrayDataReference(dst), N);
}

static class Int32WideBenchTables
{
    const byte B = byte.MaxValue;

    // Code scatter for the 256 serialize weave. Input = the code dwords computed on the
    // SAME vpermq-arranged vector as the payload shuffle (lane 0 = elements 0..3,
    // lane 1 = elements 2..5, each dword 0x000000cc with the code in the low byte, i.e.
    // at in-lane byte 4k). Output puts token t's code at position 5t: lane 0 serves
    // tokens 0-3 (positions 0/5/10/15 from bytes 0/4/8/12), lane 1 serves tokens 4-5
    // (positions 20/25 = in-lane 4/9 from bytes 8/12, elements 4 and 5).
    public static readonly Vector256<byte> Code256 = Vector256.Create(
        0, B, B, B, B, 4, B, B, B, B, 8, B, B, B, B, 12,
        B, B, B, B, 8, B, B, B, B, 12, B, B, B, B, B, B);

    // 128 variant, no permute: dwords are elements 0..3, tokens 0-2 at positions 0/5/10.
    public static readonly Vector128<byte> Code128 = Vector128.Create(
        0, B, B, B, B, 4, B, B, B, B, 8, B, B, B, B, B);
}

public static class Int32WideWeaveCandidates
{
    // ---- serialize: the shipping non-VBMI path (constant-stride scalar) and the weaves ----

    public static int WriteWideScalar(ref byte d, ref int src, int length)
    {
        int i = 0;
        int o = 0;
        for (; length - i >= 16; i += 16, o += 80)
        {
            EmitWide16(ref d, o, ref src, i);
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    static void EmitWide16(ref byte d, int written, ref int src, int i)
    {
        for (int t = 0; t < 16; t++)
        {
            int v = Unsafe.Add(ref src, i + t);
            Unsafe.Add(ref d, written + (t * 5)) = (byte)(0xce ^ ((v >> 31) & 0x1c));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + (t * 5) + 1), BinaryPrimitives.ReverseEndianness((uint)v));
        }
    }

    static void EmitWide4(ref byte d, int written, ref int src, int i)
    {
        for (int t = 0; t < 4; t++)
        {
            int v = Unsafe.Add(ref src, i + t);
            Unsafe.Add(ref d, written + (t * 5)) = (byte)(0xce ^ ((v >> 31) & 0x1c));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + (t * 5) + 1), BinaryPrimitives.ReverseEndianness((uint)v));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector256<byte> Weave6(Vector256<int> permuted)
    {
        var codes = (Vector256.Create(0x000000ce) ^ (Vector256.ShiftRightArithmetic(permuted, 31) & Vector256.Create(0x0000001c))).AsByte();
        return Avx2.Shuffle(permuted.AsByte(), WeaveBenchTables.FloatIndices256) | Avx2.Shuffle(codes, Int32WideBenchTables.Code256);
    }

    // three overlapping 6-token windows per 16-element block: t0-5 at o, t6-11 at o+30
    // (its 2 garbage bytes rewritten by the next store), t10-15 at o+50 via a shifted
    // vpermq (lane 0 = elements 10-13, lane 1 = 12-15, so the same shuffle tables apply;
    // tokens 10-11 are written twice with identical bytes). The last store overhangs the
    // 80-byte run by 2 bytes.
    public static int WriteWide256x3(ref byte d, ref int src, int length)
    {
        int i = 0;
        int o = 0;
        if (Avx2.IsSupported)
        {
            for (; length - i >= 16; i += 16, o += 80)
            {
                var p0 = Avx2.Permute4x64(Vector256.LoadUnsafe(ref src, (nuint)i).AsUInt64(), 0b10_01_01_00).AsInt32();
                var p1 = Avx2.Permute4x64(Vector256.LoadUnsafe(ref src, (nuint)(i + 4)).AsUInt64(), 0b11_10_10_01).AsInt32();
                var p2 = Avx2.Permute4x64(Vector256.LoadUnsafe(ref src, (nuint)(i + 8)).AsUInt64(), 0b11_10_10_01).AsInt32();
                Weave6(p0).StoreUnsafe(ref Unsafe.Add(ref d, o));
                Weave6(p1).StoreUnsafe(ref Unsafe.Add(ref d, o + 30));
                Weave6(p2).StoreUnsafe(ref Unsafe.Add(ref d, o + 50));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    // two windows plus a 4-token scalar tail: no output overhang, but 4 of 16 elements
    // stay scalar
    public static int WriteWide256x2S4(ref byte d, ref int src, int length)
    {
        int i = 0;
        int o = 0;
        if (Avx2.IsSupported)
        {
            for (; length - i >= 16; i += 16, o += 80)
            {
                var p0 = Avx2.Permute4x64(Vector256.LoadUnsafe(ref src, (nuint)i).AsUInt64(), 0b10_01_01_00).AsInt32();
                var p1 = Avx2.Permute4x64(Vector256.LoadUnsafe(ref src, (nuint)(i + 4)).AsUInt64(), 0b11_10_10_01).AsInt32();
                Weave6(p0).StoreUnsafe(ref Unsafe.Add(ref d, o));
                Weave6(p1).StoreUnsafe(ref Unsafe.Add(ref d, o + 30));
                EmitWide4(ref d, o + 60, ref src, i + 12);
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    // five 3-token windows plus one scalar token: stores at o..o+60 each overhang 1 byte
    // into the next window's code slot (rewritten), the fifth store's garbage byte at
    // o+75 is rewritten by token 15's scalar code, exact 80 bytes total
    public static int WriteWide128x5(ref byte d, ref int src, int length)
    {
        int i = 0;
        int o = 0;
        if (Ssse3.IsSupported)
        {
            for (; length - i >= 16; i += 16, o += 80)
            {
                for (int t = 0; t < 5; t++)
                {
                    var v = Vector128.LoadUnsafe(ref src, (nuint)(i + (t * 3)));
                    var codes = (Vector128.Create(0x000000ce) ^ (Vector128.ShiftRightArithmetic(v, 31) & Vector128.Create(0x0000001c))).AsByte();
                    (Ssse3.Shuffle(v.AsByte(), WeaveBenchTables.FloatIndices128) | Ssse3.Shuffle(codes, Int32WideBenchTables.Code128))
                        .StoreUnsafe(ref Unsafe.Add(ref d, o + (t * 15)));
                }
                int v15 = Unsafe.Add(ref src, i + 15);
                Unsafe.Add(ref d, o + 75) = (byte)(0xce ^ ((v15 >> 31) & 0x1c));
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, o + 76), BinaryPrimitives.ReverseEndianness((uint)v15));
            }
        }
        for (; i < length; i++)
        {
            o += UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    // ---- deserialize: the shipping non-VBMI path and the weaves ----

    public static bool ReadWideScalar(ref byte s, ref int dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        for (; count - i >= 16; i += 16, o += 80)
        {
            ok &= TryDecodeWide16(ref Unsafe.Add(ref s, o), ref dst, i);
        }
        for (; i < count; i++, o += 5)
        {
            ok &= DecodeOne(ref s, o, ref dst, i);
        }
        return ok;
    }

    static bool TryDecodeWide16(ref byte w0, ref int dst, int i)
    {
        bool ok = true;
        bool uintOverflow = false;
        for (int t = 0; t < 16; t++)
        {
            byte c = Unsafe.Add(ref w0, t * 5);
            int v = (int)BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref w0, (t * 5) + 1)));
            Unsafe.Add(ref dst, i + t) = v;
            ok &= (c == 0xd2) | (c == 0xce);
            uintOverflow |= (c == 0xce) & (v < 0);
        }
        return ok && !uintOverflow;
    }

    static bool DecodeOne(ref byte s, int o, ref int dst, int i)
    {
        byte c = Unsafe.Add(ref s, o);
        int v = (int)BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, o + 1)));
        Unsafe.Add(ref dst, i) = v;
        return ((c == 0xd2) | (c == 0xce)) & !((c == 0xce) & (v < 0));
    }

    // 6 tokens per 32B window: code validity is (ce|d2) at the six code positions; the
    // uint-overflow test shifts the ce positions onto the following payload byte's
    // movemask bit (a 0xce token whose payload MSB is set must go scalar to throw)
    public static bool ReadWide256(ref byte s, ref int dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Avx2.IsSupported)
        {
            for (; count - i >= 8; i += 6, o += 30)
            {
                var w = Vector256.LoadUnsafe(ref Unsafe.Add(ref s, o));
                uint ce = Vector256.Equals(w, Vector256.Create((byte)0xce)).ExtractMostSignificantBits();
                uint d2 = Vector256.Equals(w, Vector256.Create((byte)0xd2)).ExtractMostSignificantBits();
                uint msb = w.ExtractMostSignificantBits();
                ok &= ((ce | d2) & 0x2108421u) == 0x2108421u;
                ok &= (((ce & 0x2108421u) << 1) & msb) == 0;
                var shuffled = Avx2.Shuffle(w, WeaveBenchTables.FloatDecShuffle256);
                shuffled.GetLower().StoreUnsafe(ref Unsafe.As<int, byte>(ref dst), (nuint)(i * 4));
                shuffled.GetUpper().StoreUnsafe(ref Unsafe.As<int, byte>(ref dst), (nuint)((i * 4) + 12));
            }
        }
        for (; i < count; i++, o += 5)
        {
            ok &= DecodeOne(ref s, o, ref dst, i);
        }
        return ok;
    }

    public static bool ReadWide128(ref byte s, ref int dst, int count)
    {
        bool ok = true;
        int i = 0;
        int o = 0;
        if (Ssse3.IsSupported)
        {
            for (; count - i >= 4; i += 3, o += 15)
            {
                var w = Vector128.LoadUnsafe(ref Unsafe.Add(ref s, o));
                uint ce = Vector128.Equals(w, Vector128.Create((byte)0xce)).ExtractMostSignificantBits();
                uint d2 = Vector128.Equals(w, Vector128.Create((byte)0xd2)).ExtractMostSignificantBits();
                uint msb = w.ExtractMostSignificantBits();
                ok &= ((ce | d2) & 0x421u) == 0x421u;
                ok &= (((ce & 0x421u) << 1) & msb) == 0;
                Ssse3.Shuffle(w, WeaveBenchTables.FloatDecShuffle128).StoreUnsafe(ref Unsafe.As<int, byte>(ref dst), (nuint)(i * 4));
            }
        }
        for (; i < count; i++, o += 5)
        {
            ok &= DecodeOne(ref s, o, ref dst, i);
        }
        return ok;
    }
}

public static class Int32WideWeaveVerify
{
    public static bool VerifyAll(int[] data)
    {
        var ok = true;
        int n = data.Length;

        var expected = new byte[(5 * n) + 64];
        expected.AsSpan().Fill(0xAA);
        int expLen = Int32WideWeaveCandidates.WriteWideScalar(ref MemoryMarshal.GetArrayDataReference(expected), ref MemoryMarshal.GetArrayDataReference(data), n);
        if (expLen != 5 * n)
        {
            Console.WriteLine($"NG Int32WideWeave n={n}: test data is not all-wide (len {expLen})");
            return false;
        }

        var writers = new (string Name, Func<byte[], int> Fn)[]
        {
            ("WriteWide256x3", buf => Int32WideWeaveCandidates.WriteWide256x3(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(data), n)),
            ("WriteWide256x2S4", buf => Int32WideWeaveCandidates.WriteWide256x2S4(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(data), n)),
            ("WriteWide128x5", buf => Int32WideWeaveCandidates.WriteWide128x5(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(data), n)),
        };
        foreach (var (name, fn) in writers)
        {
            var buf = new byte[(5 * n) + 64];
            buf.AsSpan().Fill(0x55);
            int len = fn(buf);
            if (len != expLen || !buf.AsSpan(0, expLen).SequenceEqual(expected.AsSpan(0, expLen)))
            {
                Console.WriteLine($"NG Int32WideWeave n={n} {name}: wire mismatch");
                ok = false;
            }
        }

        var readers = new (string Name, Func<byte[], int[], bool> Fn)[]
        {
            ("ReadWideScalar", (wire, dst) => Int32WideWeaveCandidates.ReadWideScalar(ref MemoryMarshal.GetArrayDataReference(wire), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadWide256", (wire, dst) => Int32WideWeaveCandidates.ReadWide256(ref MemoryMarshal.GetArrayDataReference(wire), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadWide128", (wire, dst) => Int32WideWeaveCandidates.ReadWide128(ref MemoryMarshal.GetArrayDataReference(wire), ref MemoryMarshal.GetArrayDataReference(dst), n)),
        };
        foreach (var (name, fn) in readers)
        {
            var dst = new int[n];
            Array.Fill(dst, unchecked((int)0xDEADBEEF));
            if (!fn(expected, dst) || !dst.AsSpan().SequenceEqual(data))
            {
                Console.WriteLine($"NG Int32WideWeave n={n} {name}: decode mismatch");
                ok = false;
            }
        }

        if (n >= 1)
        {
            // a corrupted code byte and an out-of-int-range uint32 token must both fail
            // the gate (the codec then falls to the scalar reader, which throws)
            var corrupted = (byte[])expected.Clone();
            corrupted[0] = 0xca;
            var overflow = (byte[])expected.Clone();
            overflow[0] = 0xce;
            overflow[1] = 0x80;
            foreach (var (name, fn) in readers)
            {
                var dst = new int[n];
                if (fn(corrupted, dst))
                {
                    Console.WriteLine($"NG Int32WideWeave n={n} {name}: accepted corrupted code");
                    ok = false;
                }
                if (fn(overflow, dst))
                {
                    Console.WriteLine($"NG Int32WideWeave n={n} {name}: accepted uint32 overflow");
                    ok = false;
                }
            }
        }
        return ok;
    }
}
