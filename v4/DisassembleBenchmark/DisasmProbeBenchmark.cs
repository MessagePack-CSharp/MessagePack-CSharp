using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using SerializerFoundation;

// Isolates what makes BDN's DisassemblyDiagnoser silently drop a whole benchmark
// (Code Size = NA, method absent from asm.md, no error anywhere). Observed pattern:
// all Ultra benchmarks and MessagePack-CSharp Deserialize fail; MessagePack-CSharp
// Serialize and all Nerdbank benchmarks succeed. Each probe below adds exactly one
// suspect construct to an otherwise trivial NoInlining call chain.
public class DisasmProbeBenchmark
{
    [Benchmark(Baseline = true)]
    public int Plain() => Probe.Plain(1);

    [Benchmark]
    public int WithStackAlloc() => Probe.WithStackAlloc(1);

    [Benchmark]
    public int WithRefStructGeneric() => Probe.WithRefStructGeneric(1);

    [Benchmark]
    public int WithGvm() => Probe.WithGvm(1);
}

public static class Probe
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Plain(int x) => x + 1;

    // Ultra Serialize<T> shape: stackalloc scratch (SkipLocalsInit omitted — this project
    // doesn't enable AllowUnsafeBlocks, and the suspect construct is the localloc itself)
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int WithStackAlloc(int x)
    {
        Span<byte> s = stackalloc byte[1024];
        s[0] = (byte)x;
        return s[0] + s[x];
    }

    // Ultra formatter shape: generic method instantiated over a ByRefLike type
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int WithRefStructGeneric(int x)
    {
        var buffer = new ReadOnlySpanReadBuffer([1, 2, 3, 4]);
        return UseBuffer(ref buffer) + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int UseBuffer<T>(ref T buffer) where T : struct, IReadBuffer, allows ref struct
        => (int)buffer.BytesRemaining;

    // resolver shape shared by Ultra (pre-bypass) and MessagePack-CSharp
    // (IFormatterResolver.GetFormatter<T>): a generic virtual method call
    interface IGvm
    {
        int M<T>();
    }

    sealed class Gvm : IGvm
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int M<T>() => typeof(T).Name.Length;
    }

    static readonly IGvm gvm = new Gvm();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int WithGvm(int x) => gvm.M<string>() + x;
}
