using MessagePack;
using MessagePack.Formatters;
using SerializerFoundation;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// an enum whose type-level [MessagePackFormatter] names a factory with the DOWNLEVEL shape (the Type-based member
// only, serving the Compatible pairs): a netstandard-built formatter library attached by attribute
[MessagePackFormatter(typeof(CompatibleOnlyRoutedModeFactory))]
public enum RoutedMode
{
    A,
    B,
}

#pragma warning disable MsgPack102 // simulating a factory compiled against the downlevel surface
public sealed class CompatibleOnlyRoutedModeFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        if (valueType != typeof(RoutedMode))
        {
            return null;
        }
        if (writeBufferType == typeof(CompatibleArrayPoolListWriteBuffer) && readBufferType == typeof(CompatibleReadOnlySpanReadBuffer))
        {
            return new RoutedModeStringFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer>();
        }
        if (writeBufferType == typeof(CompatibleArrayPoolListWriteBuffer) && readBufferType == typeof(CompatibleReadOnlySequenceReadBuffer))
        {
            return new RoutedModeStringFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySequenceReadBuffer>();
        }
        if (writeBufferType == typeof(CompatibleBufferWriterWriteBuffer) && readBufferType == typeof(CompatibleReadOnlySpanReadBuffer))
        {
            return new RoutedModeStringFormatter<CompatibleBufferWriterWriteBuffer, CompatibleReadOnlySpanReadBuffer>();
        }
        return null;
    }
}
#pragma warning restore MsgPack102

#pragma warning disable MsgPack104 // the string codec is the point of this formatter
public sealed class RoutedModeStringFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, RoutedMode>
    where TWriteBuffer : struct, IWriteBuffer
    where TReadBuffer : struct, IReadBuffer
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, RoutedMode value) => buffer.WriteString(value.ToString());

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref RoutedMode value) => value = Enum.Parse<RoutedMode>(buffer.ReadString()!);
}
#pragma warning restore MsgPack104

// the same downlevel shape named by a MEMBER attribute: the formatter it creates must take the same reroute
public enum RoutedMode2
{
    A,
    B,
}

#pragma warning disable MsgPack102 // simulating a factory compiled against the downlevel surface
public sealed class CompatibleOnlyMemberFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        if (valueType != typeof(RoutedMode2))
        {
            return null;
        }
        if (writeBufferType == typeof(CompatibleArrayPoolListWriteBuffer) && readBufferType == typeof(CompatibleReadOnlySpanReadBuffer))
        {
            return new EnumStringFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, RoutedMode2>();
        }
        if (writeBufferType == typeof(CompatibleArrayPoolListWriteBuffer) && readBufferType == typeof(CompatibleReadOnlySequenceReadBuffer))
        {
            return new EnumStringFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySequenceReadBuffer, RoutedMode2>();
        }
        if (writeBufferType == typeof(CompatibleBufferWriterWriteBuffer) && readBufferType == typeof(CompatibleReadOnlySpanReadBuffer))
        {
            return new EnumStringFormatter<CompatibleBufferWriterWriteBuffer, CompatibleReadOnlySpanReadBuffer, RoutedMode2>();
        }
        return null;
    }
}
#pragma warning restore MsgPack102

#pragma warning disable MsgPack104 // the string codec is the point of this formatter
public sealed class EnumStringFormatter<TWriteBuffer, TReadBuffer, TEnum> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TEnum>
    where TWriteBuffer : struct, IWriteBuffer
    where TReadBuffer : struct, IReadBuffer
    where TEnum : struct, Enum
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TEnum value) => buffer.WriteString(value.ToString());

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TEnum value) => value = Enum.Parse<TEnum>(buffer.ReadString()!);
}
#pragma warning restore MsgPack104

[MessagePackObject]
public class RoutedHolder
{
    [Key(0)]
    [MessagePackFormatter(typeof(CompatibleOnlyMemberFactory))]
    public RoutedMode2 Mode { get; set; }

    [Key(1)] public int Other { get; set; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class RoutedHolderReflection
{
    [Key(0)]
    [MessagePackFormatter(typeof(CompatibleOnlyMemberFactory))]
    public RoutedMode2 Mode { get; set; }

    [Key(1)] public int Other { get; set; }
}

public class ReviewRound18RegressionTests
{
    // a member-level [MessagePackFormatter] factory that serves the member only over the Compatible buffers: the
    // resolver reroutes the owning graph there (generated and reflection formatters alike), so the member keeps the
    // factory's string wire instead of the initialization failing
    [Fact]
    public void MemberLevelAttributedCompatibleOnlyFactory_ReroutesTheGraph()
    {
        byte[] expected = [0x92, 0xA1, (byte)'B', 0x03]; // [ "B", 3 ]
        var bytes = V4.Serialize(new RoutedHolder { Mode = RoutedMode2.B, Other = 3 });
        Assert.Equal(expected, bytes);
        var back = V4.Deserialize<RoutedHolder>(bytes)!;
        Assert.Equal(RoutedMode2.B, back.Mode);
        Assert.Equal(3, back.Other);
        Assert.Equal(RoutedMode2.B, V4.Deserialize<RoutedHolder>(new System.Buffers.ReadOnlySequence<byte>(bytes))!.Mode);

        var reflected = V4.Serialize(new RoutedHolderReflection { Mode = RoutedMode2.B, Other = 3 });
        Assert.Equal(expected, reflected);
        Assert.Equal(RoutedMode2.B, V4.Deserialize<RoutedHolderReflection>(reflected)!.Mode);

        // nested in another type: the reroute covers the whole graph
        Assert.Equal([0x91, .. expected], V4.Serialize(new[] { new RoutedHolder { Mode = RoutedMode2.B, Other = 3 } }));

        // opting out of the fallback is loud, and names the factory
        var strict = new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.Default) { ThrowOnLegacyFormatter = true });
        var error = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(new RoutedHolder(), strict));
        Assert.Contains(nameof(CompatibleOnlyMemberFactory), error.Message);
    }

    // the resolver's Compatible-pair probe must reach the attributed factory through the generated factory: the enum
    // keeps its string wire instead of silently taking the built-in integer formatter
    [Fact]
    public void TypeLevelAttributedCompatibleOnlyFactory_KeepsItsWire()
    {
        var bytes = V4.Serialize(RoutedMode.B);
        Assert.Equal(V4.Serialize("B"), bytes);
        Assert.Equal(RoutedMode.B, V4.Deserialize<RoutedMode>(bytes));
        Assert.Equal(RoutedMode.B, V4.Deserialize<RoutedMode>(new System.Buffers.ReadOnlySequence<byte>(bytes)));
        Assert.Equal(bytes, V4.Serialize(new[] { RoutedMode.B })[1..]); // as an element too
    }

    // a Type value is a System.RuntimeType at runtime: the runtime-type paths serialize it as System.Type
    [Fact]
    public void TypeValue_SerializesAsType_FromRuntimeTypePaths()
    {
#pragma warning disable CS0618
        var factory = MessagePackFormatterFactory.Combine(new TypeFormatterFactory(), MessagePackFormatterFactory.Default);
#pragma warning restore CS0618
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(factory));
        var typed = V4.Serialize<Type?>(typeof(int), options);
        Assert.Equal(typed, V4.Serialize<object>(typeof(int), options));
        Assert.Equal(typed, V4.Serialize(typeof(int).GetType(), typeof(int), options));
        Assert.Equal(typeof(List<string>), V4.Deserialize<Type?[]>(V4.Serialize(new object[] { typeof(List<string>) }, options), options)![0]);

        var typeless = new MessagePackSerializerOptions(new MessagePackFormatterResolver(factory.WithTypeless(TypelessTypeLoader.AllowedTypes(typeof(Type)))));
        Assert.Equal(typeof(int), V4.Deserialize<object>(V4.Serialize<object>(typeof(int), typeless), typeless));
    }
}
