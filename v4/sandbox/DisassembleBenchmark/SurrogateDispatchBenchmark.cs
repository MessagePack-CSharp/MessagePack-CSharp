using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using SerializerFoundation;
using MessagePack;
using MessagePack.Formatters;

// Surrogate conversion dispatch: static abstract interface members (the original
// IMessagePackSurrogate shape, net10+ only) versus instance members on the struct surrogate
// invoked through default(TSurrogate) for the creation direction and on the deserialized
// value for the read direction (compiles on every TFM). Both arms are constrained calls on a
// struct type argument; the expectation was a codegen tie. Both arms are probe-local copies
// of the formatter so the file stays self-contained (the library shipped the Instance shape
// after this probe). The formatters are invoked directly over the fast buffer pair.
//
// MEASURED (Ryzen AI 9 HX 470, ShortRun, 9 runs, --jitdisasm on the formatter bodies):
//   Serialize:   StaticAbstract 12.9..13.4 ns | Instance 12.9..13.4 ns  (quiet-machine runs: tie)
//   Deserialize: StaticAbstract 19.5..20.6 ns | Instance 18.7..20.5 ns  (tie)
//   Outliers (Instance Serialize 16.9 / 25.0 ns, Instance Deserialize 27.8 ns) appeared only
//   in runs whose Error column blew up to 20..35 ns for EVERY arm, static included: laptop
//   noise after back-to-back runs, not codegen (the disassembly is identical across runs).
// VERDICT: NOT a tie in codegen. The formatter is instantiated over a CLASS target, so the
// instantiation is shared (TTarget = __Canon), and a static virtual call in shared generic
// code needs a runtime generic-dictionary lookup: the StaticAbstract Tier1 body carries
// `mov rax,[dict+0x28]; test rax,rax; je slowpath; call rax` plus a 24-byte struct copy for
// the argument in BOTH directions (Serialize 863 B, Deserialize 214 B with a
// CORINFO_HELP_RUNTIMEHANDLE_CLASS fallback). The Instance arm's constrained instance call
// resolves against the exact struct and inlines fully: ToSurrogate becomes three field
// loads off the target, ToTarget becomes NEWSFAST + three stores (Serialize 803 B,
// Deserialize 193 B, no indirect call). The instance shape is therefore strictly better
// for the common class-target case and equal for struct targets, on top of compiling
// downlevel. Wall-clock hides the difference behind the interface call to the surrogate's
// generated formatter, so this is an asm verdict, not a Mean verdict.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SurrogateDispatchBenchmark
{
    SurAbTargetStatic staticTarget = default!;
    SurAbTargetInstance instanceTarget = default!;
    byte[] payload = default!;
    IMessagePackFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, SurAbTargetStatic?> staticFormatter = default!;
    IMessagePackFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, SurAbTargetInstance?> instanceFormatter = default!;

    [GlobalSetup]
    public void Setup()
    {
        staticTarget = new SurAbTargetStatic(42, 123456789012L, "realm");
        instanceTarget = new SurAbTargetInstance(42, 123456789012L, "realm");

        var staticOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            new StaticSurrogateFormatterFactory<SurAbTargetStatic, SurAbStaticSurrogate>(),
            MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
            BuiltInFormatterFactory.Instance,
        ]));
        var instanceOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            new InstanceSurrogateFormatterFactory<SurAbTargetInstance, SurAbInstanceSurrogate>(),
            MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
            BuiltInFormatterFactory.Instance,
        ]));
        staticFormatter = staticOptions.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, SurAbTargetStatic?>();
        instanceFormatter = instanceOptions.Resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, SurAbTargetInstance?>();

        payload = MessagePackSerializer.Serialize(staticTarget, staticOptions);
        if (!MessagePackSerializer.Serialize(instanceTarget, instanceOptions).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: instance wire != static wire");
        if (!SerializeStaticAbstract().AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: direct static serialize");
        if (!SerializeInstance().AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: direct instance serialize");
        var s = DeserializeStaticAbstract();
        var i = DeserializeInstance();
        if (s.Value != 42 || s.Ticks != 123456789012L || s.Realm != "realm") throw new InvalidOperationException("verify failed: static deserialize");
        if (i.Value != 42 || i.Ticks != 123456789012L || i.Realm != "realm") throw new InvalidOperationException("verify failed: instance deserialize");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    [System.Runtime.CompilerServices.SkipLocalsInit]
    public byte[] SerializeStaticAbstract()
    {
        Span<byte> scratch = stackalloc byte[256];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            var state = new SerializeState(64);
            staticFormatter.Serialize(ref buffer, ref state, staticTarget);
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [BenchmarkCategory("Serialize"), Benchmark]
    [System.Runtime.CompilerServices.SkipLocalsInit]
    public byte[] SerializeInstance()
    {
        Span<byte> scratch = stackalloc byte[256];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            var state = new SerializeState(64);
            instanceFormatter.Serialize(ref buffer, ref state, instanceTarget);
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public SurAbTargetStatic DeserializeStaticAbstract()
    {
        var buffer = new ReadOnlySpanReadBuffer(payload);
        try
        {
            var state = new DeserializeState(64);
            SurAbTargetStatic? result = default;
            staticFormatter.Deserialize(ref buffer, ref state, ref result);
            return result!;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public SurAbTargetInstance DeserializeInstance()
    {
        var buffer = new ReadOnlySpanReadBuffer(payload);
        try
        {
            var state = new DeserializeState(64);
            SurAbTargetInstance? result = default;
            instanceFormatter.Deserialize(ref buffer, ref state, ref result);
            return result!;
        }
        finally
        {
            buffer.Dispose();
        }
    }
}

// ---- targets (constructor-owned, no serialization attributes) ----

public sealed class SurAbTargetStatic(int value, long ticks, string realm)
{
    public int Value { get; } = value;
    public long Ticks { get; } = ticks;
    public string Realm { get; } = realm;
}

public sealed class SurAbTargetInstance(int value, long ticks, string realm)
{
    public int Value { get; } = value;
    public long Ticks { get; } = ticks;
    public string Realm { get; } = realm;
}

// ---- StaticAbstract arm: the original interface shape (probe-local copy) ----

public interface IStaticSurrogate<TTarget, TSurrogate>
    where TSurrogate : struct, IStaticSurrogate<TTarget, TSurrogate>
{
    static abstract TSurrogate ToSurrogate(TTarget value);
    static abstract TTarget FromSurrogate(TSurrogate surrogate);
}

[MessagePackObject]
public readonly record struct SurAbStaticSurrogate([property: Key(0)] int Value, [property: Key(1)] long Ticks, [property: Key(2)] string Realm)
    : IStaticSurrogate<SurAbTargetStatic, SurAbStaticSurrogate>
{
    public static SurAbStaticSurrogate ToSurrogate(SurAbTargetStatic value) => new(value.Value, value.Ticks, value.Realm);
    public static SurAbTargetStatic FromSurrogate(SurAbStaticSurrogate surrogate) => new(surrogate.Value, surrogate.Ticks, surrogate.Realm);
}

public sealed class StaticSurrogateFormatter<TWriteBuffer, TReadBuffer, TTarget, TSurrogate> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TTarget?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where TSurrogate : struct, IStaticSurrogate<TTarget, TSurrogate>
{
    static bool TargetCanBeNull => default(TTarget) is null;

    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TSurrogate> surrogateFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        surrogateFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TSurrogate>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TTarget? value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }
        surrogateFormatter.Serialize(ref buffer, ref state, TSurrogate.ToSurrogate(value));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TTarget? value)
    {
        if (TargetCanBeNull && buffer.TryReadNil())
        {
            value = default;
            return;
        }
        TSurrogate surrogate = default;
        surrogateFormatter.Deserialize(ref buffer, ref state, ref surrogate);
        value = TSurrogate.FromSurrogate(surrogate);
    }
}

public sealed partial class StaticSurrogateFormatterFactory<TTarget, TSurrogate> : MessagePackFormatterFactory
    where TSurrogate : struct, IStaticSurrogate<TTarget, TSurrogate>
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        if (type == typeof(TTarget))
        {
            return new StaticSurrogateFormatter<TWriteBuffer, TReadBuffer, TTarget, TSurrogate>();
        }
        return null;
    }
}

// ---- Instance arm: proposed shape (asymmetric: creation on default(TSurrogate), read on this) ----

public interface IInstanceSurrogate<TTarget, TSurrogate>
    where TSurrogate : struct, IInstanceSurrogate<TTarget, TSurrogate>
{
    TSurrogate ToSurrogate(TTarget value);
    TTarget ToTarget();
}

[MessagePackObject]
public readonly record struct SurAbInstanceSurrogate([property: Key(0)] int Value, [property: Key(1)] long Ticks, [property: Key(2)] string Realm)
    : IInstanceSurrogate<SurAbTargetInstance, SurAbInstanceSurrogate>
{
    public SurAbInstanceSurrogate ToSurrogate(SurAbTargetInstance value) => new(value.Value, value.Ticks, value.Realm);
    public SurAbTargetInstance ToTarget() => new(Value, Ticks, Realm);
}

public sealed class InstanceSurrogateFormatter<TWriteBuffer, TReadBuffer, TTarget, TSurrogate> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TTarget?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where TSurrogate : struct, IInstanceSurrogate<TTarget, TSurrogate>
{
    static bool TargetCanBeNull => default(TTarget) is null;

    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TSurrogate> surrogateFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        surrogateFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TSurrogate>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TTarget? value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }
        surrogateFormatter.Serialize(ref buffer, ref state, default(TSurrogate).ToSurrogate(value));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TTarget? value)
    {
        if (TargetCanBeNull && buffer.TryReadNil())
        {
            value = default;
            return;
        }
        TSurrogate surrogate = default;
        surrogateFormatter.Deserialize(ref buffer, ref state, ref surrogate);
        value = surrogate.ToTarget();
    }
}

public sealed partial class InstanceSurrogateFormatterFactory<TTarget, TSurrogate> : MessagePackFormatterFactory
    where TSurrogate : struct, IInstanceSurrogate<TTarget, TSurrogate>
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        if (type == typeof(TTarget))
        {
            return new InstanceSurrogateFormatter<TWriteBuffer, TReadBuffer, TTarget, TSurrogate>();
        }
        return null;
    }
}
