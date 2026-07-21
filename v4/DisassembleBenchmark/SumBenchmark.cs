using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;

public class SumBenchmark
{
    int[] data = default!;

    [Params(1000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        data = new int[N];
        for (int i = 0; i < N; i++) data[i] = rand.Next(0, 100);
    }

    [Benchmark(Baseline = true)]
    public int ForLoop()
    {
        var d = data;
        int sum = 0;
        for (int i = 0; i < d.Length; i++)
        {
            sum += d[i];
        }
        return sum;
    }

    [Benchmark]
    public int ForEachLoop()
    {
        int sum = 0;
        foreach (var x in data)
        {
            sum += x;
        }
        return sum;
    }

    [Benchmark]
    public int LinqSum() => data.Sum();

    [Benchmark]
    public int VectorSimd()
    {
        var d = data.AsSpan();
        var acc = Vector<int>.Zero;
        int i = 0;
        for (; i <= d.Length - Vector<int>.Count; i += Vector<int>.Count)
        {
            acc += new Vector<int>(d.Slice(i));
        }
        int sum = Vector.Sum(acc);
        for (; i < d.Length; i++)
        {
            sum += d[i];
        }
        return sum;
    }

    [Benchmark]
    public int TensorPrimitivesSum() => TensorPrimitives.Sum<int>(data);

    // Round 2: eliminate the per-iteration Slice bounds checks seen in VectorSimd's asm
    [Benchmark]
    public int VectorRef()
    {
        ref int p = ref MemoryMarshal.GetArrayDataReference(data);
        int length = data.Length;
        var acc = Vector<int>.Zero;
        int i = 0;
        for (; i <= length - Vector<int>.Count; i += Vector<int>.Count)
        {
            acc += Vector.LoadUnsafe(ref p, (nuint)i);
        }
        int sum = Vector.Sum(acc);
        for (; i < length; i++)
        {
            sum += Unsafe.Add(ref p, i);
        }
        return sum;
    }

    // Round 2: match TensorPrimitives' shape — zmm registers + multiple accumulators
    [Benchmark]
    public int Vector512Unrolled()
    {
        ref int p = ref MemoryMarshal.GetArrayDataReference(data);
        int length = data.Length;
        int i = 0;
        int sum = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            var acc0 = Vector512<int>.Zero;
            var acc1 = Vector512<int>.Zero;
            var acc2 = Vector512<int>.Zero;
            var acc3 = Vector512<int>.Zero;
            for (; i <= length - Vector512<int>.Count * 4; i += Vector512<int>.Count * 4)
            {
                acc0 += Vector512.LoadUnsafe(ref p, (nuint)i);
                acc1 += Vector512.LoadUnsafe(ref p, (nuint)(i + Vector512<int>.Count));
                acc2 += Vector512.LoadUnsafe(ref p, (nuint)(i + Vector512<int>.Count * 2));
                acc3 += Vector512.LoadUnsafe(ref p, (nuint)(i + Vector512<int>.Count * 3));
            }
            for (; i <= length - Vector512<int>.Count; i += Vector512<int>.Count)
            {
                acc0 += Vector512.LoadUnsafe(ref p, (nuint)i);
            }
            sum = Vector512.Sum((acc0 + acc1) + (acc2 + acc3));
        }
        for (; i < length; i++)
        {
            sum += Unsafe.Add(ref p, i);
        }
        return sum;
    }

    // Round 3: nuint indexing (kills movsxd) + masked vector tail (kills scalar remainder loop)
    [Benchmark]
    public int Vector512Final()
    {
        ref int p = ref MemoryMarshal.GetArrayDataReference(data);
        nuint length = (nuint)data.Length;
        nuint vc = (nuint)Vector512<int>.Count;

        if (!Vector512.IsHardwareAccelerated || length < vc)
        {
            int s = 0;
            for (nuint j = 0; j < length; j++)
            {
                s += Unsafe.Add(ref p, j);
            }
            return s;
        }

        var acc0 = Vector512<int>.Zero;
        var acc1 = Vector512<int>.Zero;
        var acc2 = Vector512<int>.Zero;
        var acc3 = Vector512<int>.Zero;
        nuint i = 0;
        if (length >= vc * 4)
        {
            nuint lastBlock = length - vc * 4;
            for (; i <= lastBlock; i += vc * 4)
            {
                acc0 += Vector512.LoadUnsafe(ref p, i);
                acc1 += Vector512.LoadUnsafe(ref p, i + vc);
                acc2 += Vector512.LoadUnsafe(ref p, i + vc * 2);
                acc3 += Vector512.LoadUnsafe(ref p, i + vc * 3);
            }
        }
        for (nuint lastVector = length - vc; i <= lastVector; i += vc)
        {
            acc0 += Vector512.LoadUnsafe(ref p, i);
        }
        if (i < length)
        {
            // overlapping load of the final vector, masking out already-summed lanes
            nuint offset = length - vc;
            var lane = Vector512<int>.Indices + Vector512.Create((int)offset);
            var mask = Vector512.GreaterThanOrEqual(lane, Vector512.Create((int)i));
            acc1 += mask & Vector512.LoadUnsafe(ref p, offset);
        }
        return Vector512.Sum((acc0 + acc1) + (acc2 + acc3));
    }
}
