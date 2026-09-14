extern alias V3;
using MessagePack;
using MessagePack.Formatters;
using SerializerFoundation;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// [MessagePackFormatter] compatibility with v3 annotation assemblies: the attribute is
// matched by NAME (v3's FormatterType property read alongside v4's FactoryType). The
// member-level form is honored at runtime by the reflection object formatter's slots;
// the TYPE-level form has NO runtime tier (the source generator compiles it, and the only
// population that would need one, pre-built v3 DLLs, names v3-compiled formatters that
// are bound to v3's writer/reader and cannot run on v4). A type reaching the reflection
// tier with the annotation gets a targeted error instead of a loader maze or a silent
// reflection map. The types here carry the v3 MessagePack.Annotations attribute.
public class V3FormatterAttributeTests
{
    // SourceGenerated deliberately absent: the generator compiles type-level
    // [MessagePackFormatter] for this assembly's own types, and these tests pin what
    // happens to a type the generator never registered
    static readonly MessagePackSerializerOptions WithoutSourceGenerated = new(new MessagePackFormatterResolver(
        MessagePackFormatterFactory.Combine(
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true))));

    [Fact]
    public void TypeLevel_NotCompiledByGenerator_FailsWithTargetedMessage()
    {
        // a v4-shaped target the generator never registered: the reflection tier names the
        // missing generator run rather than a bare "no formatter" miss or a reflection map
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(new V3AttrPoint { X = 7 }, WithoutSourceGenerated));
        Assert.Contains("source generator", ex.Message);
        Assert.Contains(nameof(V3AttrPointFactory), ex.Message);
    }

    [Fact]
    public void TypeLevel_V3CompiledFormatterType_FailsWithTargetedMessage()
    {
        // the pre-built v3 DLL shape: a type-level attribute naming a formatter that
        // implements v3's IMessagePackFormatter<T>. Declared inline because the generator's
        // MsgPack012 (an error, not suppressible) rightly refuses to compile such a type in
        // this assembly; the reflection tier hands the attribute to this same diagnostic
        var attribute = new V3::MessagePack.MessagePackFormatterAttribute(typeof(V3CompiledIntFormatter));
        var ex = AttributeFormatterActivator.TypeLevelNotCompiled(attribute, typeof(int));
        Assert.Contains("cannot run on v4", ex.Message);
    }

    [Fact]
    public void MemberLevel_V3Attribute_ServedByReflectionSlots()
    {
        var contractless = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [MessagePackFormatterFactory.Default.WithContractless()]));
        var bytes = V4.Serialize(new V3AttrHolder { Point = new V3AttrPoint { X = 9 } }, contractless);
        Assert.Equal(9, V4.Deserialize<V3AttrHolder>(bytes, contractless)!.Point!.X);
        // map { "Point": 0x09 }: the member slot honored the v3-named attribute
        Assert.Equal((byte)0x09, bytes[^1]);
    }

    [Fact]
    public void MemberLevel_V3CompiledFormatterType_FailsWithTargetedMessage()
    {
        // a formatter compiled against v3 implements v3's IMessagePackFormatter<T> and can
        // never run on v4; the member-slot activator names the problem instead of a loader exception
        var attribute = new V3::MessagePack.MessagePackFormatterAttribute(typeof(V3CompiledIntFormatter));
        var ex = Assert.Throws<MessagePackSerializationException>(() =>
            AttributeFormatterActivator.Create(attribute, typeof(int), typeof(ArrayPoolListWriteBuffer), typeof(ReadOnlySpanReadBuffer), "test"));
        Assert.Contains("cannot run on v4", ex.Message);
    }

    [Fact]
    public void SourceGenerator_AlsoCompilesTheV3NamedAttribute()
    {
        // the generator matches the attribute by name too, so a RECOMPILED assembly gets
        // the registration - even on the AOT chains
        var bytes = V4.Serialize(new V3AttrPoint { X = 7 }, MessagePackSerializerOptions.DefaultAot);
        Assert.Equal(new byte[] { 0x07 }, bytes);
    }
}

[V3::MessagePack.MessagePackFormatter(typeof(V3AttrPointFactory))]
public class V3AttrPoint
{
    public int X { get; set; }
}

public class V3AttrHolder
{
    public V3AttrPoint? Point { get; set; }
}

public sealed partial class V3AttrPointFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return type == typeof(V3AttrPoint) ? new V3AttrPointFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}

public sealed class V3AttrPointFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, V3AttrPoint?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, V3AttrPoint? value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteInt32(value.X);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref V3AttrPoint? value)
    {
        value = buffer.TryReadNil() ? null : new V3AttrPoint { X = buffer.ReadInt32() };
    }
}

// the shape a v3-compiled custom formatter has: v3's single-generic interface over v3's
// writer/reader structs (the oracle assembly stands in for a real v3-built DLL)
public class V3CompiledIntFormatter : V3::MessagePack.Formatters.IMessagePackFormatter<int>
{
    public void Serialize(ref V3::MessagePack.MessagePackWriter writer, int value, V3::MessagePack.MessagePackSerializerOptions options) => writer.WriteInt32(value);

    public int Deserialize(ref V3::MessagePack.MessagePackReader reader, V3::MessagePack.MessagePackSerializerOptions options) => reader.ReadInt32();
}
