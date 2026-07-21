using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

// Round 3: probe 2 showed try/finally flips a disassemblable method (EntryNoTry, 1962B)
// into a silently dropped one (EntryTry, NA) — but both contained stackalloc. This round
// separates the two ingredients: EH alone, localloc alone (already cleared in round 1,
// repeated as in-class control), and each EH flavor combined with localloc.
public class DisasmProbe3Benchmark
{
    [Benchmark(Baseline = true)]
    public int EhOnly() => Probe3.EhOnly(1);

    [Benchmark]
    public int LocallocOnly() => Probe3.LocallocOnly(1);

    [Benchmark]
    public int LocallocFinally() => Probe3.LocallocFinally(1);

    [Benchmark]
    public int LocallocCatch() => Probe3.LocallocCatch(1);
}

public static class Probe3
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int EhOnly(int x)
    {
        try
        {
            return Work(x);
        }
        finally
        {
            Blackhole(x);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int LocallocOnly(int x)
    {
        Span<byte> s = stackalloc byte[1024];
        s[0] = (byte)x;
        return s[0] + s[x];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int LocallocFinally(int x)
    {
        Span<byte> s = stackalloc byte[1024];
        s[0] = (byte)x;
        try
        {
            return Work(s[0]);
        }
        finally
        {
            Blackhole(x);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int LocallocCatch(int x)
    {
        Span<byte> s = stackalloc byte[1024];
        s[0] = (byte)x;
        try
        {
            return Work(s[0]);
        }
        catch (Exception)
        {
            return -1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Work(int x) => x + 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Blackhole(int x) { }
}
