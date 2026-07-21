using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using MessagePack;
using SerializerFoundation;
using System.Runtime.CompilerServices;
using UltraMessagePack;
using UltraSerializer = UltraMessagePack.MessagePackSerializer;

// Round 11: what does nested formatter dispatch really cost, and does the resolver design
// hold up for POCO-as-array graphs? NestOuter{Id, M:NestMiddle{X, I:NestInner{A,B}}} —
// two nested formatter hops per object. Four composition strategies:
//   IfaceField     - current ArrayFormatter style: Initialize resolves the inner formatter
//                    once into an interface field; nested call is an interface call whose
//                    devirtualization depends on PGO/GDV luck (round 6 finding)
//   PerCall        - no field: resolver.GetFormatter per call (the formatterTable read);
//                    answers whether Initialize-time resolution is even needed now
//   DirectConcrete - the source-gen-knows-the-type option: concrete sealed formatter
//                    fields, deterministic direct calls/inlining; trades away per-type
//                    resolver substitution for nested types
//   Flat           - the outer formatter writes the whole tree itself: the ceiling
// Also exercises the resolver's recursive construction machinery (Initialize -> GetFormatter
// for nested types) for a 3-type graph.
//
// Measured (2 runs + DOTNET_TieredPGO=0):
//   default runtime: all four within the GDV-luck noise band; the only consistent signal
//     is PerCall serialize +13..15% (two formatterTable reads per object). IfaceField's
//     interface calls devirtualize+inline via GDV at monomorphic callsites.
//   TieredPGO=0 (no GDV): IfaceField/PerCall pay ~20-25% (Deserialize 42 vs Direct/Flat 32,
//     Serialize 32 vs Direct 25.5) — the true cost of nested interface dispatch when the
//     lottery misses.
// Design conclusions: keep Initialize-time resolution into a formatter field (current
// style); per-call resolution is strictly worse. DirectConcrete is the deterministic
// insurance against GDV misses (source-gen option, trades away nested-type substitution).
// Flat gains nothing over Direct — full flattening is unnecessary codegen bloat.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DisasmProbe11Benchmark
{
    NestOuter outer = default!;
    byte[] payload = default!;
    UltraSerializer ifaceField = default!;
    UltraSerializer perCall = default!;
    UltraSerializer direct = default!;
    UltraSerializer flat = default!;

    [GlobalSetup]
    public void Setup()
    {
        outer = new NestOuter { Id = 42, M = new NestMiddle { X = -1000, I = new NestInner { A = 1, B = 123456 } } };
        payload = MessagePack.MessagePackSerializer.Serialize(outer);

        ifaceField = new UltraSerializer(new UltraMessagePack.MessagePackFormatterResolver(new MapFactoryResolver(
            (typeof(NestOuter), new OuterIfaceFieldFormatterFactory()),
            (typeof(NestMiddle), new MiddleIfaceFieldFormatterFactory()),
            (typeof(NestInner), new InnerFormatterFactory()))));
        perCall = new UltraSerializer(new UltraMessagePack.MessagePackFormatterResolver(new MapFactoryResolver(
            (typeof(NestOuter), new OuterPerCallFormatterFactory()),
            (typeof(NestMiddle), new MiddlePerCallFormatterFactory()),
            (typeof(NestInner), new InnerFormatterFactory()))));
        direct = new UltraSerializer(new UltraMessagePack.MessagePackFormatterResolver(new MapFactoryResolver(
            (typeof(NestOuter), new OuterDirectFormatterFactory()))));
        flat = new UltraSerializer(new UltraMessagePack.MessagePackFormatterResolver(new MapFactoryResolver(
            (typeof(NestOuter), new OuterFlatFormatterFactory()))));

        foreach (var (name, s) in new (string, UltraSerializer)[]
        {
            (nameof(ifaceField), ifaceField), (nameof(perCall), perCall), (nameof(direct), direct), (nameof(flat), flat),
        })
        {
            var bytes = s.Serialize(outer);
            if (!bytes.AsSpan().SequenceEqual(payload)) throw new InvalidOperationException($"verify failed: serialize {name}");
            var back = s.Deserialize<NestOuter>(payload)!;
            if (back.Id != outer.Id || back.M!.X != outer.M!.X || back.M.I!.A != outer.M.I!.A || back.M.I.B != outer.M.I.B)
                throw new InvalidOperationException($"verify failed: deserialize {name}");
        }
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeIfaceField() => ifaceField.Serialize(outer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializePerCall() => perCall.Serialize(outer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeDirect() => direct.Serialize(outer);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeFlat() => flat.Serialize(outer);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public NestOuter DeserializeIfaceField() => ifaceField.Deserialize<NestOuter>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public NestOuter DeserializePerCall() => perCall.Deserialize<NestOuter>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public NestOuter DeserializeDirect() => direct.Deserialize<NestOuter>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public NestOuter DeserializeFlat() => flat.Deserialize<NestOuter>(payload)!;
}

[MessagePackObject]
public class NestOuter
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public NestMiddle? M { get; set; }
}

[MessagePackObject]
public class NestMiddle
{
    [Key(0)] public int X { get; set; }
    [Key(1)] public NestInner? I { get; set; }
}

[MessagePackObject]
public class NestInner
{
    [Key(0)] public int A { get; set; }
    [Key(1)] public int B { get; set; }
}

// factory mapping several types to their per-type factories (probe-local; array scan is
// fine, resolution happens once per instantiation)
public sealed class MapFactoryResolver : UltraMessagePack.IMessagePackFormatterFactory
{
    readonly (Type type, UltraMessagePack.IMessagePackFormatterFactory factory)[] factories;

    public MapFactoryResolver(params (Type, UltraMessagePack.IMessagePackFormatterFactory)[] factories) => this.factories = factories;

    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer, allows ref struct
        where TReadBuffer : struct, SerializerFoundation.IReadBuffer, allows ref struct
    {
        foreach (var (t, f) in factories)
        {
            if (t == type) return f.CreateFormatter<TWriteBuffer, TReadBuffer>(type);
        }
        return null;
    }
}

static class NestThrows
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InvalidCount(int count) => throw new UltraMessagePack.MessagePackSerializationException($"Invalid array count: {count}");
}

// ---- leaf ----

public sealed class InnerFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NestInner>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref NestInner value)
    {
        buffer.WriteArrayHeader(2);
        buffer.WriteInt32(value.A);
        buffer.WriteInt32(value.B);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NestInner value)
    {
        value ??= new NestInner();
        var count = buffer.ReadArrayHeader();
        if (count != 2) NestThrows.InvalidCount(count);
        value.A = buffer.ReadInt32();
        value.B = buffer.ReadInt32();
    }
}

public sealed class InnerFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
        => new InnerFormatter<TWriteBuffer, TReadBuffer>();
}

// ---- variant: interface field (current ArrayFormatter style) ----

public sealed class MiddleIfaceFieldFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NestMiddle>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, NestInner> inner = default!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        inner = resolver.GetFormatter<TWriteBuffer, TReadBuffer, NestInner>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref NestMiddle value)
    {
        buffer.WriteArrayHeader(2);
        buffer.WriteInt32(value.X);
        var i = value.I!;
        inner.Serialize(ref buffer, ref state, ref i);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NestMiddle value)
    {
        value ??= new NestMiddle();
        var count = buffer.ReadArrayHeader();
        if (count != 2) NestThrows.InvalidCount(count);
        value.X = buffer.ReadInt32();
        var i = value.I;
        inner.Deserialize(ref buffer, ref state, ref i!);
        value.I = i;
    }
}

public sealed class MiddleIfaceFieldFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
        => new MiddleIfaceFieldFormatter<TWriteBuffer, TReadBuffer>();
}

public sealed class OuterIfaceFieldFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NestOuter>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, NestMiddle> middle = default!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        middle = resolver.GetFormatter<TWriteBuffer, TReadBuffer, NestMiddle>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref NestOuter value)
    {
        buffer.WriteArrayHeader(2);
        buffer.WriteInt32(value.Id);
        var m = value.M!;
        middle.Serialize(ref buffer, ref state, ref m);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NestOuter value)
    {
        value ??= new NestOuter();
        var count = buffer.ReadArrayHeader();
        if (count != 2) NestThrows.InvalidCount(count);
        value.Id = buffer.ReadInt32();
        var m = value.M;
        middle.Deserialize(ref buffer, ref state, ref m!);
        value.M = m;
    }
}

public sealed class OuterIfaceFieldFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
        => new OuterIfaceFieldFormatter<TWriteBuffer, TReadBuffer>();
}

// ---- variant: per-call resolver (formatterTable read each time, no Initialize state) ----

public sealed class MiddlePerCallFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NestMiddle>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    MessagePackFormatterResolver resolver = default!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        this.resolver = resolver;
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref NestMiddle value)
    {
        buffer.WriteArrayHeader(2);
        buffer.WriteInt32(value.X);
        var i = value.I!;
        resolver.GetFormatter<TWriteBuffer, TReadBuffer, NestInner>().Serialize(ref buffer, ref state, ref i);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NestMiddle value)
    {
        value ??= new NestMiddle();
        var count = buffer.ReadArrayHeader();
        if (count != 2) NestThrows.InvalidCount(count);
        value.X = buffer.ReadInt32();
        var i = value.I;
        resolver.GetFormatter<TWriteBuffer, TReadBuffer, NestInner>().Deserialize(ref buffer, ref state, ref i!);
        value.I = i;
    }
}

public sealed class MiddlePerCallFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
        => new MiddlePerCallFormatter<TWriteBuffer, TReadBuffer>();
}

public sealed class OuterPerCallFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NestOuter>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    MessagePackFormatterResolver resolver = default!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        this.resolver = resolver;
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref NestOuter value)
    {
        buffer.WriteArrayHeader(2);
        buffer.WriteInt32(value.Id);
        var m = value.M!;
        resolver.GetFormatter<TWriteBuffer, TReadBuffer, NestMiddle>().Serialize(ref buffer, ref state, ref m);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NestOuter value)
    {
        value ??= new NestOuter();
        var count = buffer.ReadArrayHeader();
        if (count != 2) NestThrows.InvalidCount(count);
        value.Id = buffer.ReadInt32();
        var m = value.M;
        resolver.GetFormatter<TWriteBuffer, TReadBuffer, NestMiddle>().Deserialize(ref buffer, ref state, ref m!);
        value.M = m;
    }
}

public sealed class OuterPerCallFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
        => new OuterPerCallFormatter<TWriteBuffer, TReadBuffer>();
}

// ---- variant: direct concrete references (source-gen knows the nested formatter types) ----

public sealed class MiddleDirectFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NestMiddle>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    readonly InnerFormatter<TWriteBuffer, TReadBuffer> inner = new();

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref NestMiddle value)
    {
        buffer.WriteArrayHeader(2);
        buffer.WriteInt32(value.X);
        var i = value.I!;
        inner.Serialize(ref buffer, ref state, ref i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NestMiddle value)
    {
        value ??= new NestMiddle();
        var count = buffer.ReadArrayHeader();
        if (count != 2) NestThrows.InvalidCount(count);
        value.X = buffer.ReadInt32();
        var i = value.I;
        inner.Deserialize(ref buffer, ref state, ref i!);
        value.I = i;
    }
}

public sealed class OuterDirectFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NestOuter>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    readonly MiddleDirectFormatter<TWriteBuffer, TReadBuffer> middle = new();

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref NestOuter value)
    {
        buffer.WriteArrayHeader(2);
        buffer.WriteInt32(value.Id);
        var m = value.M!;
        middle.Serialize(ref buffer, ref state, ref m);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NestOuter value)
    {
        value ??= new NestOuter();
        var count = buffer.ReadArrayHeader();
        if (count != 2) NestThrows.InvalidCount(count);
        value.Id = buffer.ReadInt32();
        var m = value.M;
        middle.Deserialize(ref buffer, ref state, ref m!);
        value.M = m;
    }
}

public sealed class OuterDirectFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
        => new OuterDirectFormatter<TWriteBuffer, TReadBuffer>();
}

// ---- variant: fully flattened (the ceiling) ----

public sealed class OuterFlatFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NestOuter>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref NestOuter value)
    {
        var m = value.M!;
        var i = m.I!;
        buffer.WriteArrayHeader(2);
        buffer.WriteInt32(value.Id);
        buffer.WriteArrayHeader(2);
        buffer.WriteInt32(m.X);
        buffer.WriteArrayHeader(2);
        buffer.WriteInt32(i.A);
        buffer.WriteInt32(i.B);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NestOuter value)
    {
        value ??= new NestOuter();
        var m = value.M ??= new NestMiddle();
        var i = m.I ??= new NestInner();
        var count = buffer.ReadArrayHeader();
        if (count != 2) NestThrows.InvalidCount(count);
        value.Id = buffer.ReadInt32();
        count = buffer.ReadArrayHeader();
        if (count != 2) NestThrows.InvalidCount(count);
        m.X = buffer.ReadInt32();
        count = buffer.ReadArrayHeader();
        if (count != 2) NestThrows.InvalidCount(count);
        i.A = buffer.ReadInt32();
        i.B = buffer.ReadInt32();
    }
}

public sealed class OuterFlatFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
        => new OuterFlatFormatter<TWriteBuffer, TReadBuffer>();
}
