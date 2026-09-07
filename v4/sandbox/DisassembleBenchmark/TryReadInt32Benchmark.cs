using BenchmarkDotNet.Attributes;

public class TryReadInt32Benchmark
{
    // same rationale as TryWriteInt32Benchmark: large enough that the branch
    // predictor cannot memorize the repeating code-byte sequence
    const int Count = 100_000;

    byte[] buffer = default!;

    [Params("Small", "Mixed", "Large")]
    public string Distribution = "Mixed";

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        buffer = new byte[Count * 5];
        int offset = 0;
        for (int i = 0; i < Count; i++)
        {
            int v = Distribution switch
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
            MessagePackPrimitives.TryWriteInt32Cascade(buffer.AsSpan(offset), v, out var written);
            offset += written;
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int Cascade()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int i = 0; i < Count; i++)
        {
            MessagePackPrimitives.TryReadInt32Cascade(buf.Slice(offset), out var v, out var consumed);
            sum += v;
            offset += consumed;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Branchless()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int i = 0; i < Count; i++)
        {
            MessagePackPrimitives.TryReadInt32Branchless(buf.Slice(offset), out var v, out var consumed);
            sum += v;
            offset += consumed;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Branchless2()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int i = 0; i < Count; i++)
        {
            MessagePackPrimitives.TryReadInt32Branchless2(buf.Slice(offset), out var v, out var consumed);
            sum += v;
            offset += consumed;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Branchless3()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int i = 0; i < Count; i++)
        {
            MessagePackPrimitives.TryReadInt32Branchless3(buf.Slice(offset), out var v, out var consumed);
            sum += v;
            offset += consumed;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Hybrid()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int i = 0; i < Count; i++)
        {
            MessagePackPrimitives.TryReadInt32Hybrid(buf.Slice(offset), out var v, out var consumed);
            sum += v;
            offset += consumed;
        }
        return sum;
    }

    // Round 4: DecodeResult enum contract; the loop checks result == Success the way real
    // callers would, so the category encoding cost (if any) is included
    [Benchmark(OperationsPerInvoke = Count)]
    public int Dr()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int i = 0; i < Count; i++)
        {
            if (MessagePackPrimitives.TryReadInt32Dr(buf.Slice(offset), out var v, out var consumed) == MessagePackPrimitives.DecodeResult.Success)
            {
                sum += v;
            }
            offset += consumed;
        }
        return sum;
    }
}
