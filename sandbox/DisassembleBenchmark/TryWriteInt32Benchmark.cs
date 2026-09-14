using BenchmarkDotNet.Attributes;

public class TryWriteInt32Benchmark
{
    // large enough that the branch predictor cannot memorize the repeating sequence
    // (with 1000 values, Zen 5 learned the whole pattern: BranchMispredictions/Op was 0 even for Mixed)
    const int Count = 100_000;

    int[] values = default!;
    byte[] buffer = default!;

    // Small: all fixint (perfectly predictable branch)
    // Mixed: format class chosen at random per element (worst case for branch prediction)
    // Large: all 5-byte int32/uint32
    [Params("Small", "Mixed", "Large")]
    public string Distribution = "Mixed";

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        values = new int[Count];
        buffer = new byte[Count * 5];
        for (int i = 0; i < Count; i++)
        {
            values[i] = Distribution switch
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
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int Cascade()
    {
        var vals = values;
        var buf = buffer.AsSpan();
        int offset = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            MessagePackPrimitives.TryWriteInt32Cascade(buf.Slice(offset), vals[i], out var written);
            offset += written;
        }
        return offset;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int CascadeUnsafe()
    {
        var vals = values;
        var buf = buffer.AsSpan();
        int offset = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            MessagePackPrimitives.TryWriteInt32CascadeUnsafe(buf.Slice(offset), vals[i], out var written);
            offset += written;
        }
        return offset;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int FixintFirst()
    {
        var vals = values;
        var buf = buffer.AsSpan();
        int offset = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            MessagePackPrimitives.TryWriteInt32FixintFirst(buf.Slice(offset), vals[i], out var written);
            offset += written;
        }
        return offset;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int BranchlessTable()
    {
        var vals = values;
        var buf = buffer.AsSpan();
        int offset = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            MessagePackPrimitives.TryWriteInt32BranchlessTable(buf.Slice(offset), vals[i], out var written);
            offset += written;
        }
        return offset;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Branchless2()
    {
        var vals = values;
        var buf = buffer.AsSpan();
        int offset = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            MessagePackPrimitives.TryWriteInt32Branchless2(buf.Slice(offset), vals[i], out var written);
            offset += written;
        }
        return offset;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Hybrid()
    {
        var vals = values;
        var buf = buffer.AsSpan();
        int offset = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            MessagePackPrimitives.TryWriteInt32Hybrid(buf.Slice(offset), vals[i], out var written);
            offset += written;
        }
        return offset;
    }
}
