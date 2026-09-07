using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

// WriteBoolean: ternary select vs branchless OR of the reinterpreted bool byte.
// Random exposes the ternary to worst-case branch prediction (if the JIT emits a branch);
// AllTrue is the perfectly-predictable control.
public class WriteBooleanBenchmark
{
    const int Count = 100_000;

    bool[] values = default!;
    byte[] buffer = default!;

    [Params("Random", "AllTrue")]
    public string Distribution = "Random";

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        values = new bool[Count];
        buffer = new byte[Count];
        for (int i = 0; i < Count; i++)
        {
            values[i] = Distribution == "AllTrue" || rand.Next(2) == 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WriteTernary(ref byte destination, bool value)
    {
        destination = value ? (byte)195 : (byte)194;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WriteOr(ref byte destination, bool value)
    {
        destination = (byte)(194 | Unsafe.As<bool, byte>(ref value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WriteNormalizedOr(ref byte destination, bool value)
    {
        // defends against non-normalized bools: 0 -> 0, 1..255 -> 1
        byte raw = Unsafe.As<bool, byte>(ref value);
        destination = (byte)(194 | ((uint)-raw >> 31));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public byte Ternary()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        for (int i = 0; i < vals.Length; i++)
        {
            WriteTernary(ref Unsafe.Add(ref d, i), vals[i]);
        }
        return buffer[0];
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public byte UnsafeOr()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        for (int i = 0; i < vals.Length; i++)
        {
            WriteOr(ref Unsafe.Add(ref d, i), vals[i]);
        }
        return buffer[0];
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public byte NormalizedOr()
    {
        var vals = values;
        ref byte d = ref MemoryMarshal.GetArrayDataReference(buffer);
        for (int i = 0; i < vals.Length; i++)
        {
            WriteNormalizedOr(ref Unsafe.Add(ref d, i), vals[i]);
        }
        return buffer[0];
    }
}
