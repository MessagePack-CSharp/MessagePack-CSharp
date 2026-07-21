using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using SerializerFoundation;
using UltraMessagePack;

// Round 4: probes 1-3 cleared stackalloc, ByRefLike generics, GVM, EH, and localloc+EH
// individually — yet EntryTry (probe 2) fails while its try/finally-free twin succeeds.
// The remaining delta is what EntryTry does INSIDE the try. Each candidate in isolation,
// always wrapped in try/finally:
//   RefStructFinally  - ref struct local (Span fields) alive across the EH region
//   GvmFinally        - generic virtual method call inside try
//   FormatterFinally  - interface call on a ByRefLike-instantiated interface inside try
public class DisasmProbe4Benchmark
{
    BenchPerson person = default!;
    static IMessagePackFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, BenchPerson> formatter = default!;

    [GlobalSetup]
    public void Setup()
    {
        DynamicFormatterFactory.Instance.RegisterFactory<BenchPerson>(new BenchPersonFormatterFactory());
        person = new BenchPerson { Id = 12345, Name = "abc", Score = 98.5 };
        formatter = UltraMessagePack.MessagePackSerializer.Default.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, BenchPerson>();
    }

    [Benchmark(Baseline = true)]
    public long RefStructFinally()
    {
        var buffer = new ArrayPoolListWriteBuffer(default);
        try
        {
            buffer.Advance(UltraMessagePack.MessagePackPrimitives.UnsafeWriteInt32(ref buffer.GetReference(UltraMessagePack.MessagePackPrimitives.MaxInt32Length), 42));
            return buffer.BytesWritten;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Benchmark]
    public int GvmFinally()
    {
        try
        {
            return ProbeGvm.Instance.M<string>();
        }
        finally
        {
            Blackhole(1);
        }
    }

    [Benchmark]
    public long FormatterFinally()
    {
        var buffer = new ArrayPoolListWriteBuffer(default);
        var state = new SerializeState();
        try
        {
            formatter.Serialize(ref buffer, ref state, ref person);
            return buffer.BytesWritten;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Blackhole(int x) { }
}

public interface IProbeGvm
{
    int M<T>();
}

public sealed class ProbeGvm : IProbeGvm
{
    public static readonly IProbeGvm Instance = new ProbeGvm();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int M<T>() => typeof(T).Name.Length;
}
