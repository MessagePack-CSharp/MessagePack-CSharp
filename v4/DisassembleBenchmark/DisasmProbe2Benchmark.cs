using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using SerializerFoundation;
using UltraMessagePack;

// Second-round bisection for the silent DisassemblyDiagnoser failures (Code Size = NA):
// round 1 (DisasmProbeBenchmark) cleared stackalloc, ByRefLike generic methods and GVM
// calls individually — all disassembled fine. This round replays the REAL failing call
// (MessagePackSerializer.Default.Serialize) and peels it apart: entry body with and without
// try/finally, and the bare formatter call. Known-failing/known-working MessagePack-CSharp
// calls are included as in-class controls.
public class DisasmProbe2Benchmark
{
    BenchPerson person = default!;
    byte[] serialized = default!;
    static IMessagePackFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, BenchPerson> formatter = default!;

    [GlobalSetup]
    public void Setup()
    {
        DynamicFormatterFactory.Instance.RegisterFactory<BenchPerson>(new BenchPersonFormatterFactory());
        person = new BenchPerson { Id = 12345, Name = "abc", Score = 98.5 };
        serialized = MessagePack.MessagePackSerializer.Serialize(person);
        formatter = UltraMessagePack.MessagePackSerializer.Default.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, BenchPerson>();
    }

    // known-failing full entry (byte[] path, no byref args at this level)
    [Benchmark(Baseline = true)]
    public byte[] UltraFull() => MessagePackSerializer.Default.Serialize(person);

    // entry body replicated WITHOUT try/finally
    [Benchmark]
    public byte[] EntryNoTry() => SerializeNoTry(person);

    // entry body replicated WITH try/finally
    [Benchmark]
    public byte[] EntryTry() => SerializeTry(person);

    // no entry at all: cached formatter, direct call
    [Benchmark]
    public long FormatterOnly()
    {
        var buffer = new ArrayPoolListWriteBuffer(default);
        var state = new SerializeState();
        formatter.Serialize(ref buffer, ref state, ref person);
        var written = buffer.BytesWritten;
        buffer.Dispose();
        return written;
    }

    // in-class controls: known-failing / known-working MessagePack-CSharp calls
    [Benchmark]
    public BenchPerson CsharpDeserialize() => MessagePack.MessagePackSerializer.Deserialize<BenchPerson>(serialized)!;

    [Benchmark]
    public byte[] CsharpSerialize() => MessagePack.MessagePackSerializer.Serialize(person);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte[] SerializeNoTry(BenchPerson value)
    {
        Span<byte> scratch = stackalloc byte[1024];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        var state = new SerializeState();
        UltraMessagePack.MessagePackSerializer.Default.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, BenchPerson>().Serialize(ref buffer, ref state, ref value);
        var result = buffer.ToArray();
        buffer.Dispose();
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte[] SerializeTry(BenchPerson value)
    {
        Span<byte> scratch = stackalloc byte[1024];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            var state = new SerializeState();
            UltraMessagePack.MessagePackSerializer.Default.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, BenchPerson>().Serialize(ref buffer, ref state, ref value);
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }
}
