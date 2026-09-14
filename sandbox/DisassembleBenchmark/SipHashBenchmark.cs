using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using MessagePack;

// SipHash port shapes. Internal/SipHash.cs is a 1:1 port of siphash.c, which keeps the C
// macros' byte-by-byte U8TO64_LE loads: a C compiler fuses that idiom into one mov, but
// RyuJIT performs no such load combining, so the port pays ~15 instructions per 8-byte
// block where C pays one. The candidates isolate the two transforms v3-style C# applies:
//   Reference_1to1    — the shipping comparer path: 1:1 port + stackalloc byte[8] out
//                       buffer + read-back + fold
//   BlockRead         — one unaligned ulong load per block, ulong returned directly
//                       (no out-buffer roundtrip); round loops kept as for(;;) to see
//                       whether the JIT unrolls constant 2/4 trip counts on its own
//   BlockReadUnrolled — BlockRead + every SIPROUND written out straight-line (v3 shape)
public class SipHashBenchmark
{
    static readonly byte[] key = [.. Enumerable.Range(0, 16).Select(i => (byte)i)];
    static readonly ulong k0 = BinaryPrimitives.ReadUInt64LittleEndian(key);
    static readonly ulong k1 = BinaryPrimitives.ReadUInt64LittleEndian(key.AsSpan(8));

    byte[] data = [];

    // 4 = int key, 16/32 = short string keys (UTF-16 bytes), 1024 = throughput view
    [Params(4, 16, 32, 1024)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        data = new byte[N];
        new Random(42).NextBytes(data);
        VerifyCandidates();
    }

    // every candidate must match the 1:1 reference bit-for-bit; 0..129 covers each tail
    // residue (0..7) across zero, one, and many whole blocks
    public static void VerifyCandidates()
    {
        var rng = new Random(1234);
        var k = new byte[16];
        rng.NextBytes(k);
        var kk0 = BinaryPrimitives.ReadUInt64LittleEndian(k);
        var kk1 = BinaryPrimitives.ReadUInt64LittleEndian(k.AsSpan(8));
        Span<byte> out8 = stackalloc byte[8];
        for (var len = 0; len <= 129; len++)
        {
            var input = new byte[len];
            rng.NextBytes(input);
            SipHash.Siphash(input, k, out8);
            var expected = BinaryPrimitives.ReadUInt64LittleEndian(out8);
            if (HashBlockRead(input, kk0, kk1) != expected)
            {
                throw new InvalidOperationException($"BlockRead mismatch at len={len}");
            }
            if (HashBlockReadUnrolled(input, kk0, kk1) != expected)
            {
                throw new InvalidOperationException($"BlockReadUnrolled mismatch at len={len}");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public int Reference_1to1()
    {
        Span<byte> out8 = stackalloc byte[8];
        SipHash.Siphash(data, key, out8);
        var h = BinaryPrimitives.ReadUInt64LittleEndian(out8);
        return (int)(h ^ (h >> 32));
    }

    [Benchmark]
    public int BlockRead()
    {
        var h = HashBlockRead(data, k0, k1);
        return (int)(h ^ (h >> 32));
    }

    [Benchmark]
    public int BlockReadUnrolled()
    {
        var h = HashBlockReadUnrolled(data, k0, k1);
        return (int)(h ^ (h >> 32));
    }

    // SipHash-1-3 (Rust std's HashMap default): same BlockRead shape, 1 compression /
    // 3 finalization rounds. Not covered by VerifyCandidates (different function than the
    // 2-4 oracle); its library twin SipHash.Hash64 pins against Rust's sip13 vectors in
    // SipHashTests. Measured ~2x over BlockRead at key sizes → adopted as the shipping
    // round count on 2026-08-06.
    [Benchmark]
    public int BlockRead13()
    {
        var h = HashBlockRead13(data, k0, k1);
        return (int)(h ^ (h >> 32));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Sipround(ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3)
    {
        v0 += v1;
        v1 = BitOperations.RotateLeft(v1, 13);
        v1 ^= v0;
        v0 = BitOperations.RotateLeft(v0, 32);
        v2 += v3;
        v3 = BitOperations.RotateLeft(v3, 16);
        v3 ^= v2;
        v0 += v3;
        v3 = BitOperations.RotateLeft(v3, 21);
        v3 ^= v0;
        v2 += v1;
        v1 = BitOperations.RotateLeft(v1, 17);
        v1 ^= v2;
        v2 = BitOperations.RotateLeft(v2, 32);
    }

    static ulong HashBlockRead(ReadOnlySpan<byte> input, ulong k0, ulong k1)
    {
        var v0 = 0x736f6d6570736575UL ^ k0;
        var v1 = 0x646f72616e646f6dUL ^ k1;
        var v2 = 0x6c7967656e657261UL ^ k0;
        var v3 = 0x7465646279746573UL ^ k1;

        var inlen = input.Length;
        var end = inlen & ~7;
        var b = (ulong)inlen << 56;

        ref var r0 = ref MemoryMarshal.GetReference(input);
        for (var i = 0; i < end; i += 8)
        {
            var m = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref r0, i));
            if (!BitConverter.IsLittleEndian)
            {
                m = BinaryPrimitives.ReverseEndianness(m);
            }
            v3 ^= m;
            for (var r = 0; r < 2; r++)
            {
                Sipround(ref v0, ref v1, ref v2, ref v3);
            }
            v0 ^= m;
        }

        switch (inlen & 7)
        {
            case 7: b |= (ulong)Unsafe.Add(ref r0, end + 6) << 48; goto case 6;
            case 6: b |= (ulong)Unsafe.Add(ref r0, end + 5) << 40; goto case 5;
            case 5: b |= (ulong)Unsafe.Add(ref r0, end + 4) << 32; goto case 4;
            case 4: b |= (ulong)Unsafe.Add(ref r0, end + 3) << 24; goto case 3;
            case 3: b |= (ulong)Unsafe.Add(ref r0, end + 2) << 16; goto case 2;
            case 2: b |= (ulong)Unsafe.Add(ref r0, end + 1) << 8; goto case 1;
            case 1: b |= Unsafe.Add(ref r0, end); break;
            case 0: break;
        }

        v3 ^= b;
        for (var r = 0; r < 2; r++)
        {
            Sipround(ref v0, ref v1, ref v2, ref v3);
        }
        v0 ^= b;

        v2 ^= 0xff;
        for (var r = 0; r < 4; r++)
        {
            Sipround(ref v0, ref v1, ref v2, ref v3);
        }
        return v0 ^ v1 ^ v2 ^ v3;
    }

    static ulong HashBlockRead13(ReadOnlySpan<byte> input, ulong k0, ulong k1)
    {
        var v0 = 0x736f6d6570736575UL ^ k0;
        var v1 = 0x646f72616e646f6dUL ^ k1;
        var v2 = 0x6c7967656e657261UL ^ k0;
        var v3 = 0x7465646279746573UL ^ k1;

        var inlen = input.Length;
        var end = inlen & ~7;
        var b = (ulong)inlen << 56;

        ref var r0 = ref MemoryMarshal.GetReference(input);
        for (var i = 0; i < end; i += 8)
        {
            var m = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref r0, i));
            if (!BitConverter.IsLittleEndian)
            {
                m = BinaryPrimitives.ReverseEndianness(m);
            }
            v3 ^= m;
            Sipround(ref v0, ref v1, ref v2, ref v3);
            v0 ^= m;
        }

        switch (inlen & 7)
        {
            case 7: b |= (ulong)Unsafe.Add(ref r0, end + 6) << 48; goto case 6;
            case 6: b |= (ulong)Unsafe.Add(ref r0, end + 5) << 40; goto case 5;
            case 5: b |= (ulong)Unsafe.Add(ref r0, end + 4) << 32; goto case 4;
            case 4: b |= (ulong)Unsafe.Add(ref r0, end + 3) << 24; goto case 3;
            case 3: b |= (ulong)Unsafe.Add(ref r0, end + 2) << 16; goto case 2;
            case 2: b |= (ulong)Unsafe.Add(ref r0, end + 1) << 8; goto case 1;
            case 1: b |= Unsafe.Add(ref r0, end); break;
            case 0: break;
        }

        v3 ^= b;
        Sipround(ref v0, ref v1, ref v2, ref v3);
        v0 ^= b;

        v2 ^= 0xff;
        Sipround(ref v0, ref v1, ref v2, ref v3);
        Sipround(ref v0, ref v1, ref v2, ref v3);
        Sipround(ref v0, ref v1, ref v2, ref v3);
        return v0 ^ v1 ^ v2 ^ v3;
    }

    static ulong HashBlockReadUnrolled(ReadOnlySpan<byte> input, ulong k0, ulong k1)
    {
        var v0 = 0x736f6d6570736575UL ^ k0;
        var v1 = 0x646f72616e646f6dUL ^ k1;
        var v2 = 0x6c7967656e657261UL ^ k0;
        var v3 = 0x7465646279746573UL ^ k1;

        var inlen = input.Length;
        var end = inlen & ~7;
        var b = (ulong)inlen << 56;

        ref var r0 = ref MemoryMarshal.GetReference(input);
        for (var i = 0; i < end; i += 8)
        {
            var m = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref r0, i));
            if (!BitConverter.IsLittleEndian)
            {
                m = BinaryPrimitives.ReverseEndianness(m);
            }
            v3 ^= m;
            Sipround(ref v0, ref v1, ref v2, ref v3);
            Sipround(ref v0, ref v1, ref v2, ref v3);
            v0 ^= m;
        }

        switch (inlen & 7)
        {
            case 7: b |= (ulong)Unsafe.Add(ref r0, end + 6) << 48; goto case 6;
            case 6: b |= (ulong)Unsafe.Add(ref r0, end + 5) << 40; goto case 5;
            case 5: b |= (ulong)Unsafe.Add(ref r0, end + 4) << 32; goto case 4;
            case 4: b |= (ulong)Unsafe.Add(ref r0, end + 3) << 24; goto case 3;
            case 3: b |= (ulong)Unsafe.Add(ref r0, end + 2) << 16; goto case 2;
            case 2: b |= (ulong)Unsafe.Add(ref r0, end + 1) << 8; goto case 1;
            case 1: b |= Unsafe.Add(ref r0, end); break;
            case 0: break;
        }

        v3 ^= b;
        Sipround(ref v0, ref v1, ref v2, ref v3);
        Sipround(ref v0, ref v1, ref v2, ref v3);
        v0 ^= b;

        v2 ^= 0xff;
        Sipround(ref v0, ref v1, ref v2, ref v3);
        Sipround(ref v0, ref v1, ref v2, ref v3);
        Sipround(ref v0, ref v1, ref v2, ref v3);
        Sipround(ref v0, ref v1, ref v2, ref v3);
        return v0 ^ v1 ^ v2 ^ v3;
    }
}
