// bool / sbyte codecs: is the 64/32/16 width cascade worth it over the Vector128-only loop?
//
// Both codecs are elementwise (bool: compare-to-zero then mask-add between the adjacent
// codes 0xc2/0xc3; sbyte: all-fixint runs store verbatim behind a biased range gate), so
// wider registers cut only the iteration count. The open question is whether that pays
// once the loop is already trivially cheap, and on the decode side how much the per-chunk
// window bookkeeping amortization contributes. Candidates are direct statics over
// contiguous buffers; sbyte data is all-fixint (the superlane case; mixed data takes the
// scalar path identically in both variants).
//
// RESULTS (Zen 5, ShortRun, ns/element, N=1000 / N=100000):
//   bool  ser   128 0.022/0.018   cascade 0.011/0.010   (1.9x)
//   bool  deser 128 0.038/0.039   cascade 0.015/0.014   (2.5-2.8x)
//   sbyte ser   128 0.030/0.025   cascade 0.012/0.009   (2.6-2.8x)
//   sbyte deser 128 0.030/0.024   cascade 0.009/0.013   (1.9-3.3x)
// VERDICT: cascade adopted in Boolean/SByteElementCodec, both directions. The elementwise
// widening prediction held: wider registers cut iteration count with no downside, and the
// decode side additionally amortizes per-chunk bookkeeping. Correctness across tiers is
// pinned by the test suite under DOTNET_EnableAVX512F=0 / EnableAVX2=0 / EnableSSSE3=0.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class BoolSByteWidthBenchmark
{
    [Params(1000, 100_000)]
    public int N = 1000;

    bool[] bools = default!;
    sbyte[] sbytes = default!;
    byte[] bbuf = default!;
    byte[] sbuf = default!;
    bool[] bdst = default!;
    sbyte[] sdst = default!;
    byte[] bElems = default!;
    byte[] sElems = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        bools = new bool[N];
        sbytes = new sbyte[N];
        for (int i = 0; i < N; i++)
        {
            bools[i] = rand.Next(2) == 0;
            sbytes[i] = (sbyte)rand.Next(-32, 128); // fixint range only
        }
        bbuf = new byte[N + 64];
        sbuf = new byte[2 * N + 64];
        bdst = new bool[N];
        sdst = new sbyte[N];

        bElems = new byte[N];
        for (int i = 0; i < N; i++)
        {
            bElems[i] = bools[i] ? (byte)0xc3 : (byte)0xc2;
        }
        sElems = new byte[N];
        for (int i = 0; i < N; i++)
        {
            sElems[i] = unchecked((byte)sbytes[i]); // fixint token IS the value byte
        }

        if (!BoolSByteWidthVerify.VerifyAll(bools, sbytes)) throw new InvalidOperationException("verify failed: width candidates");
    }

    [BenchmarkCategory("BoolSer"), Benchmark(Baseline = true)]
    public int BoolSer128() => BoolSByteWidthCandidates.WriteBool128(ref MemoryMarshal.GetArrayDataReference(bbuf), ref MemoryMarshal.GetArrayDataReference(bools), N);

    [BenchmarkCategory("BoolSer"), Benchmark]
    public int BoolSerCascade() => BoolSByteWidthCandidates.WriteBoolCascade(ref MemoryMarshal.GetArrayDataReference(bbuf), ref MemoryMarshal.GetArrayDataReference(bools), N);

    [BenchmarkCategory("BoolDeser"), Benchmark(Baseline = true)]
    public bool BoolDeser128() => BoolSByteWidthCandidates.ReadBool128(ref MemoryMarshal.GetArrayDataReference(bElems), ref MemoryMarshal.GetArrayDataReference(bdst), N);

    [BenchmarkCategory("BoolDeser"), Benchmark]
    public bool BoolDeserCascade() => BoolSByteWidthCandidates.ReadBoolCascade(ref MemoryMarshal.GetArrayDataReference(bElems), ref MemoryMarshal.GetArrayDataReference(bdst), N);

    [BenchmarkCategory("SByteSer"), Benchmark(Baseline = true)]
    public int SByteSer128() => BoolSByteWidthCandidates.WriteSByte128(ref MemoryMarshal.GetArrayDataReference(sbuf), ref MemoryMarshal.GetArrayDataReference(sbytes), N);

    [BenchmarkCategory("SByteSer"), Benchmark]
    public int SByteSerCascade() => BoolSByteWidthCandidates.WriteSByteCascade(ref MemoryMarshal.GetArrayDataReference(sbuf), ref MemoryMarshal.GetArrayDataReference(sbytes), N);

    [BenchmarkCategory("SByteDeser"), Benchmark(Baseline = true)]
    public bool SByteDeser128() => BoolSByteWidthCandidates.ReadSByte128(ref MemoryMarshal.GetArrayDataReference(sElems), ref MemoryMarshal.GetArrayDataReference(sdst), N);

    [BenchmarkCategory("SByteDeser"), Benchmark]
    public bool SByteDeserCascade() => BoolSByteWidthCandidates.ReadSByteCascade(ref MemoryMarshal.GetArrayDataReference(sElems), ref MemoryMarshal.GetArrayDataReference(sdst), N);
}

public static class BoolSByteWidthCandidates
{
    // ---- bool serialize ----

    public static int WriteBool128(ref byte d, ref bool src, int length)
    {
        int i = 0;
        for (; i + 16 <= length; i += 16)
        {
            var v = Vector128.LoadUnsafe(ref Unsafe.As<bool, byte>(ref src), (nuint)i);
            (Vector128.Min(v, Vector128.Create((byte)1)) + Vector128.Create((byte)0xc2)).StoreUnsafe(ref Unsafe.Add(ref d, i));
        }
        for (; i < length; i++)
        {
            Unsafe.Add(ref d, i) = Unsafe.Add(ref src, i) ? (byte)0xc3 : (byte)0xc2;
        }
        return length;
    }

    public static int WriteBoolCascade(ref byte d, ref bool src, int length)
    {
        int i = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            for (; i + 64 <= length; i += 64)
            {
                var v = Vector512.LoadUnsafe(ref Unsafe.As<bool, byte>(ref src), (nuint)i);
                (Vector512.Min(v, Vector512.Create((byte)1)) + Vector512.Create((byte)0xc2)).StoreUnsafe(ref Unsafe.Add(ref d, i));
            }
        }
        if (Vector256.IsHardwareAccelerated)
        {
            for (; i + 32 <= length; i += 32)
            {
                var v = Vector256.LoadUnsafe(ref Unsafe.As<bool, byte>(ref src), (nuint)i);
                (Vector256.Min(v, Vector256.Create((byte)1)) + Vector256.Create((byte)0xc2)).StoreUnsafe(ref Unsafe.Add(ref d, i));
            }
        }
        for (; i + 16 <= length; i += 16)
        {
            var v = Vector128.LoadUnsafe(ref Unsafe.As<bool, byte>(ref src), (nuint)i);
            (Vector128.Min(v, Vector128.Create((byte)1)) + Vector128.Create((byte)0xc2)).StoreUnsafe(ref Unsafe.Add(ref d, i));
        }
        for (; i < length; i++)
        {
            Unsafe.Add(ref d, i) = Unsafe.Add(ref src, i) ? (byte)0xc3 : (byte)0xc2;
        }
        return length;
    }

    // ---- bool deserialize ----

    public static bool ReadBool128(ref byte s, ref bool dst, int count)
    {
        bool ok = true;
        int i = 0;
        for (; i + 16 <= count; i += 16)
        {
            var v = Vector128.LoadUnsafe(ref Unsafe.Add(ref s, i));
            ok &= Vector128.EqualsAll(v & Vector128.Create((byte)0xFE), Vector128.Create((byte)0xc2));
            (v & Vector128.Create((byte)1)).StoreUnsafe(ref Unsafe.As<bool, byte>(ref dst), (nuint)i);
        }
        for (; i < count; i++)
        {
            byte b = Unsafe.Add(ref s, i);
            ok &= (b & 0xFE) == 0xc2;
            Unsafe.Add(ref dst, i) = (b & 1) != 0;
        }
        return ok;
    }

    public static bool ReadBoolCascade(ref byte s, ref bool dst, int count)
    {
        bool ok = true;
        int i = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            for (; i + 64 <= count; i += 64)
            {
                var v = Vector512.LoadUnsafe(ref Unsafe.Add(ref s, i));
                ok &= Vector512.EqualsAll(v & Vector512.Create((byte)0xFE), Vector512.Create((byte)0xc2));
                (v & Vector512.Create((byte)1)).StoreUnsafe(ref Unsafe.As<bool, byte>(ref dst), (nuint)i);
            }
        }
        if (Vector256.IsHardwareAccelerated)
        {
            for (; i + 32 <= count; i += 32)
            {
                var v = Vector256.LoadUnsafe(ref Unsafe.Add(ref s, i));
                ok &= Vector256.EqualsAll(v & Vector256.Create((byte)0xFE), Vector256.Create((byte)0xc2));
                (v & Vector256.Create((byte)1)).StoreUnsafe(ref Unsafe.As<bool, byte>(ref dst), (nuint)i);
            }
        }
        for (; i + 16 <= count; i += 16)
        {
            var v = Vector128.LoadUnsafe(ref Unsafe.Add(ref s, i));
            ok &= Vector128.EqualsAll(v & Vector128.Create((byte)0xFE), Vector128.Create((byte)0xc2));
            (v & Vector128.Create((byte)1)).StoreUnsafe(ref Unsafe.As<bool, byte>(ref dst), (nuint)i);
        }
        for (; i < count; i++)
        {
            byte b = Unsafe.Add(ref s, i);
            ok &= (b & 0xFE) == 0xc2;
            Unsafe.Add(ref dst, i) = (b & 1) != 0;
        }
        return ok;
    }

    // ---- sbyte serialize (all-fixint superlane path) ----

    public static int WriteSByte128(ref byte d, ref sbyte src, int length)
    {
        int i = 0;
        int o = 0;
        for (; i + 16 <= length; i += 16)
        {
            var v = Vector128.LoadUnsafe(ref Unsafe.As<sbyte, byte>(ref src), (nuint)i);
            if (!Vector128.LessThanOrEqualAll(v + Vector128.Create((byte)0x20), Vector128.Create((byte)0x9f)))
            {
                break;
            }
            v.StoreUnsafe(ref Unsafe.Add(ref d, o));
            o += 16;
        }
        for (; i < length; i++)
        {
            o += MessagePack.MessagePackPrimitives.UnsafeWriteSByte(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    public static int WriteSByteCascade(ref byte d, ref sbyte src, int length)
    {
        int i = 0;
        int o = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            for (; i + 64 <= length; i += 64)
            {
                var v = Vector512.LoadUnsafe(ref Unsafe.As<sbyte, byte>(ref src), (nuint)i);
                if (!Vector512.LessThanOrEqualAll(v + Vector512.Create((byte)0x20), Vector512.Create((byte)0x9f)))
                {
                    break;
                }
                v.StoreUnsafe(ref Unsafe.Add(ref d, o));
                o += 64;
            }
        }
        if (Vector256.IsHardwareAccelerated)
        {
            for (; i + 32 <= length; i += 32)
            {
                var v = Vector256.LoadUnsafe(ref Unsafe.As<sbyte, byte>(ref src), (nuint)i);
                if (!Vector256.LessThanOrEqualAll(v + Vector256.Create((byte)0x20), Vector256.Create((byte)0x9f)))
                {
                    break;
                }
                v.StoreUnsafe(ref Unsafe.Add(ref d, o));
                o += 32;
            }
        }
        for (; i + 16 <= length; i += 16)
        {
            var v = Vector128.LoadUnsafe(ref Unsafe.As<sbyte, byte>(ref src), (nuint)i);
            if (!Vector128.LessThanOrEqualAll(v + Vector128.Create((byte)0x20), Vector128.Create((byte)0x9f)))
            {
                break;
            }
            v.StoreUnsafe(ref Unsafe.Add(ref d, o));
            o += 16;
        }
        for (; i < length; i++)
        {
            o += MessagePack.MessagePackPrimitives.UnsafeWriteSByte(ref Unsafe.Add(ref d, o), Unsafe.Add(ref src, i));
        }
        return o;
    }

    // ---- sbyte deserialize (all-fixint) ----

    public static bool ReadSByte128(ref byte s, ref sbyte dst, int count)
    {
        int i = 0;
        for (; i + 16 <= count; i += 16)
        {
            var v = Vector128.LoadUnsafe(ref Unsafe.Add(ref s, i));
            if (!Vector128.LessThanOrEqualAll(v + Vector128.Create((byte)0x20), Vector128.Create((byte)0x9f)))
            {
                return false;
            }
            v.StoreUnsafe(ref Unsafe.As<sbyte, byte>(ref dst), (nuint)i);
        }
        for (; i < count; i++)
        {
            byte b = Unsafe.Add(ref s, i);
            if ((byte)(b + 0x20) > 0x9f)
            {
                return false;
            }
            Unsafe.Add(ref dst, i) = unchecked((sbyte)b);
        }
        return true;
    }

    public static bool ReadSByteCascade(ref byte s, ref sbyte dst, int count)
    {
        int i = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            for (; i + 64 <= count; i += 64)
            {
                var v = Vector512.LoadUnsafe(ref Unsafe.Add(ref s, i));
                if (!Vector512.LessThanOrEqualAll(v + Vector512.Create((byte)0x20), Vector512.Create((byte)0x9f)))
                {
                    return false;
                }
                v.StoreUnsafe(ref Unsafe.As<sbyte, byte>(ref dst), (nuint)i);
            }
        }
        if (Vector256.IsHardwareAccelerated)
        {
            for (; i + 32 <= count; i += 32)
            {
                var v = Vector256.LoadUnsafe(ref Unsafe.Add(ref s, i));
                if (!Vector256.LessThanOrEqualAll(v + Vector256.Create((byte)0x20), Vector256.Create((byte)0x9f)))
                {
                    return false;
                }
                v.StoreUnsafe(ref Unsafe.As<sbyte, byte>(ref dst), (nuint)i);
            }
        }
        for (; i + 16 <= count; i += 16)
        {
            var v = Vector128.LoadUnsafe(ref Unsafe.Add(ref s, i));
            if (!Vector128.LessThanOrEqualAll(v + Vector128.Create((byte)0x20), Vector128.Create((byte)0x9f)))
            {
                return false;
            }
            v.StoreUnsafe(ref Unsafe.As<sbyte, byte>(ref dst), (nuint)i);
        }
        for (; i < count; i++)
        {
            byte b = Unsafe.Add(ref s, i);
            if ((byte)(b + 0x20) > 0x9f)
            {
                return false;
            }
            Unsafe.Add(ref dst, i) = unchecked((sbyte)b);
        }
        return true;
    }
}

// shared by GlobalSetup and Program.cs --verify
public static class BoolSByteWidthVerify
{
    public static bool VerifyAll(bool[] bools, sbyte[] sbytes)
    {
        var ok = true;
        int n = bools.Length;

        var bExpected = new byte[n + 64];
        for (int i = 0; i < n; i++)
        {
            bExpected[i] = bools[i] ? (byte)0xc3 : (byte)0xc2;
        }
        foreach (var (name, run) in new (string, Func<byte[], int>)[]
        {
            ("WriteBool128", buf => BoolSByteWidthCandidates.WriteBool128(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(bools), n)),
            ("WriteBoolCascade", buf => BoolSByteWidthCandidates.WriteBoolCascade(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(bools), n)),
        })
        {
            var actual = new byte[n + 64];
            if (run(actual) != n || !actual.AsSpan(0, n).SequenceEqual(bExpected.AsSpan(0, n)))
            {
                ok = false;
                Console.WriteLine($"NG BoolWidth {name} n={n}");
            }
        }
        foreach (var (name, run) in new (string, Func<bool[], bool>)[]
        {
            ("ReadBool128", dst => BoolSByteWidthCandidates.ReadBool128(ref MemoryMarshal.GetArrayDataReference(bExpected), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            ("ReadBoolCascade", dst => BoolSByteWidthCandidates.ReadBoolCascade(ref MemoryMarshal.GetArrayDataReference(bExpected), ref MemoryMarshal.GetArrayDataReference(dst), n)),
        })
        {
            var dst = new bool[n];
            if (!run(dst) || !dst.AsSpan().SequenceEqual(bools))
            {
                ok = false;
                Console.WriteLine($"NG BoolWidth {name} n={n}");
            }
        }

        var sExpected = new byte[2 * n + 64];
        int sLen = 0;
        for (int i = 0; i < n; i++)
        {
            sLen += MessagePack.MessagePackPrimitives.UnsafeWriteSByte(ref sExpected[sLen], sbytes[i]);
        }
        foreach (var (name, run) in new (string, Func<byte[], int>)[]
        {
            ("WriteSByte128", buf => BoolSByteWidthCandidates.WriteSByte128(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(sbytes), n)),
            ("WriteSByteCascade", buf => BoolSByteWidthCandidates.WriteSByteCascade(ref MemoryMarshal.GetArrayDataReference(buf), ref MemoryMarshal.GetArrayDataReference(sbytes), n)),
        })
        {
            var actual = new byte[2 * n + 64];
            if (run(actual) != sLen || !actual.AsSpan(0, sLen).SequenceEqual(sExpected.AsSpan(0, sLen)))
            {
                ok = false;
                Console.WriteLine($"NG BoolWidth {name} n={n}");
            }
        }
        if (sLen == n) // all-fixint payload: the decode candidates require it
        {
            foreach (var (name, run) in new (string, Func<sbyte[], bool>)[]
            {
                ("ReadSByte128", dst => BoolSByteWidthCandidates.ReadSByte128(ref MemoryMarshal.GetArrayDataReference(sExpected), ref MemoryMarshal.GetArrayDataReference(dst), n)),
                ("ReadSByteCascade", dst => BoolSByteWidthCandidates.ReadSByteCascade(ref MemoryMarshal.GetArrayDataReference(sExpected), ref MemoryMarshal.GetArrayDataReference(dst), n)),
            })
            {
                var dst = new sbyte[n];
                if (!run(dst) || !dst.AsSpan().SequenceEqual(sbytes))
                {
                    ok = false;
                    Console.WriteLine($"NG BoolWidth {name} n={n}");
                }
            }
        }
        return ok;
    }
}
