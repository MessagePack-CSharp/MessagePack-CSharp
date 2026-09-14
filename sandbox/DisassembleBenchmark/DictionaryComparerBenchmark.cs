using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MessagePack;

// The cost of default-on collision-resistant comparers, measured at the seam that pays
// it: building a Dictionary of N keys (pre-sized, indexer insert — the shipping
// DictionaryFormatter shape minus the formatter machinery).
//
// Int keys are the interesting case: with comparer == null, Dictionary<int,V> runs the
// JIT-devirtualized EqualityComparer<int>.Default fast path; ANY non-null comparer
// demotes every hash/equals to interface calls, so the SipHash column pays both the hash
// and the lost devirtualization. Candidates:
//   Default       — comparer null (fast path; HashDoS-attackable identity hash)
//   SipHash       — HashFloodingResistantEqualityComparer.Get<int>() as shipped
//                   (BitwiseHashComparer: stackalloc span → Hash64 with loop + tail switch)
//   SipHashFixed4 — candidate specialization for fixed ≤8-byte keys: the whole value fits
//                   the length-tagged final block, so no span, no block loop, no tail
//                   switch — straight-line 4 SIPROUNDs
public class DictionaryComparerInt32Benchmark
{
    static readonly ulong k0 = 0x0706050403020100UL;
    static readonly ulong k1 = 0x0f0e0d0c0b0a0908UL;

    int[] keys = [];
    IEqualityComparer<int> sipHash = null!;
    SipHashFixed4Comparer fixed4 = null!;

    [Params(16, 1000, 100_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        keys = new int[N];
        for (var i = 0; i < N; i++)
        {
            keys[i] = rng.Next();
        }
        sipHash = HashFloodingResistantEqualityComparer.Get<int>()!;
        fixed4 = new SipHashFixed4Comparer(k0, k1);
        VerifyCandidates();
    }

    // the fixed-4 specialization must produce exactly Hash64(LE bytes, k0, k1) folded
    public static void VerifyCandidates()
    {
        var candidate = new SipHashFixed4Comparer(k0, k1);
        var bytes = new byte[4];
        int[] samples = [0, 1, -1, int.MinValue, int.MaxValue, 42, 0x12345678, -12345];
        foreach (var value in samples)
        {
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            var h = SipHash.Hash64(bytes, k0, k1);
            var expected = (int)(h ^ (h >> 32));
            if (candidate.GetHashCode(value) != expected)
            {
                throw new InvalidOperationException($"SipHashFixed4 mismatch for {value}");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public Dictionary<int, int> Default()
    {
        var d = new Dictionary<int, int>(N);
        var k = keys;
        for (var i = 0; i < k.Length; i++)
        {
            d[k[i]] = i;
        }
        return d;
    }

    [Benchmark]
    public Dictionary<int, int> SipHash_()
    {
        var d = new Dictionary<int, int>(N, sipHash);
        var k = keys;
        for (var i = 0; i < k.Length; i++)
        {
            d[k[i]] = i;
        }
        return d;
    }

    [Benchmark]
    public Dictionary<int, int> SipHashFixed4()
    {
        var d = new Dictionary<int, int>(N, fixed4);
        var k = keys;
        for (var i = 0; i < k.Length; i++)
        {
            d[k[i]] = i;
        }
        return d;
    }
}

// String keys: with comparer == null, .NET Core-era Dictionary<string,V> silently uses
// NonRandomizedStringEqualityComparer and only swaps to randomized Marvin when a bucket
// chain passes the collision threshold (~100) — that adaptive scheme IS the BCL's HashDoS
// defense, which is why string can be passed through on .NET 10. This measures what the
// pass-through saves versus handing every string to the SipHash comparer.
public class DictionaryComparerStringBenchmark
{
    string[] keys = [];
    IEqualityComparer<string> sipHash = null!;

    [Params(16, 1000, 100_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        keys = new string[N];
        for (var i = 0; i < N; i++)
        {
            // realistic short property/id-style keys, lengths ~7..16 chars
            keys[i] = $"key{i}_{rng.Next(1000):D3}";
        }
        // Get<string>() pass-throughs (null) on modern .NET as a result of this very
        // measurement; reference the comparer type directly to keep the A/B reproducible
        sipHash = StringHashComparer.Instance;
    }

    [Benchmark(Baseline = true)]
    public Dictionary<string, int> Default()
    {
        var d = new Dictionary<string, int>(N);
        var k = keys;
        for (var i = 0; i < k.Length; i++)
        {
            d[k[i]] = i;
        }
        return d;
    }

    [Benchmark]
    public Dictionary<string, int> SipHash_()
    {
        var d = new Dictionary<string, int>(N, sipHash);
        var k = keys;
        for (var i = 0; i < k.Length; i++)
        {
            d[k[i]] = i;
        }
        return d;
    }
}

// SipHash-1-3 of a 4-byte value where the value IS the final length-tagged block:
// b = 4<<56 | LE(value), so v3^=b → 1 round → v0^=b → v2^=0xff → 3 rounds, no memory.
sealed class SipHashFixed4Comparer : IEqualityComparer<int>
{
    readonly ulong k0;
    readonly ulong k1;

    public SipHashFixed4Comparer(ulong k0, ulong k1)
    {
        this.k0 = k0;
        this.k1 = k1;
    }

    public bool Equals(int x, int y) => x == y;

    public int GetHashCode(int value)
    {
        var v0 = 0x736f6d6570736575UL ^ k0;
        var v1 = 0x646f72616e646f6dUL ^ k1;
        var v2 = 0x6c7967656e657261UL ^ k0;
        var v3 = 0x7465646279746573UL ^ k1;
        var b = (4UL << 56) | (uint)value;

        v3 ^= b;
        Sipround(ref v0, ref v1, ref v2, ref v3);
        v0 ^= b;

        v2 ^= 0xff;
        Sipround(ref v0, ref v1, ref v2, ref v3);
        Sipround(ref v0, ref v1, ref v2, ref v3);
        Sipround(ref v0, ref v1, ref v2, ref v3);

        var h = v0 ^ v1 ^ v2 ^ v3;
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
}
