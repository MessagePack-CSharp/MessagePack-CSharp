using SerializerFoundation;

namespace MessagePack.Tests;

public struct BridgeValue
{
    public int X;
}

// the Round's partial style: constraints come from BufferConstraintsGenerator
public sealed partial class BridgeValueFormatter<TWriteBuffer, TReadBuffer>
    : IMessagePackFormatter<TWriteBuffer, TReadBuffer, BridgeValue>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, BridgeValue value)
    {
        buffer.WriteInt32(value.X);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref BridgeValue value)
    {
        value = new BridgeValue { X = buffer.ReadInt32() };
    }
}

// SIMULATION of a downlevel-compiled factory: implements ONLY the Type-based member.
// That this even compiles on net10.0 is the first assertion — the generic interface
// member is satisfied by its default-implementation bridge. MsgPack102 rightly flags this
// shape for NEW net10 code, which is exactly what this type simulates not being.
#pragma warning disable MsgPack102
sealed class TypeBasedOnlyFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
        => valueType == typeof(BridgeValue)
            ? Activator.CreateInstance(typeof(BridgeValueFormatter<,>).MakeGenericType(writeBufferType, readBufferType))
            : null;
}
#pragma warning restore MsgPack102

// End-to-end proof of the two-tier factory interface: generic resolution routes through
// the default-implementation bridge into the Type-based implementation.
public class FactoryBridgeTest
{
    [Fact]
    public unsafe void BridgeResolvesThroughFallbackPair()
    {
        var resolver = new MessagePackFormatterResolver(new TypeBasedOnlyFactory());

        // generic resolution → interface default implementation → Type-based factory
        var formatter = resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, BridgeValue>();
        Assert.IsType<BridgeValueFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer>>(formatter);

        // and the produced formatter actually works over those buffers
        var writeBuffer = new CompatibleArrayPoolListWriteBuffer();
        try
        {
            var serializeState = new SerializeState();
            formatter.Serialize(ref writeBuffer, ref serializeState, new BridgeValue { X = 12345 });
            var payload = writeBuffer.ToArray();

            fixed (byte* pointer = payload)
            {
                var readBuffer = new CompatibleReadOnlySpanReadBuffer(pointer, payload.Length);
                var deserializeState = new DeserializeState(maxDepth: 0, payload.Length);
                var result = default(BridgeValue);
                formatter.Deserialize(ref readBuffer, ref deserializeState, ref result);
                Assert.Equal(12345, result.X);
            }
        }
        finally
        {
            writeBuffer.Dispose();
        }
    }

    [Fact]
    public void BridgeCarriesTheWholeSerializerEntry()
    {
        // the serializer's net10 entries resolve with REF STRUCT buffer types; the bridge
        // hands those to the Type-based factory, whose MakeGenericType closes the
        // allows-ref-struct formatter over them (runtime support verified by this test)
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver([new TypeBasedOnlyFactory()]));
        var payload = MessagePackSerializer.Serialize(new BridgeValue { X = -7 }, options);
        var result = MessagePackSerializer.Deserialize<BridgeValue>(payload, options);
        Assert.Equal(-7, result.X);
    }
}

// A partial type whose LEADING declaration carries no base list. The generators' syntax predicates only admit
// declarations with a base list, so "the first declaration in source order" was never the one that reached them,
// and neither the Type-based bridge nor the buffer constraints were generated: a compile error, which is why these
// two types compiling at all is the assertion.
public sealed partial class LeadingBarePartialFactory;

public sealed partial class LeadingBarePartialFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        => type == typeof(BridgeValue) ? new LeadingBarePartialFormatter<TWriteBuffer, TReadBuffer>() : null;
}

public sealed partial class LeadingBarePartialFormatter<TWriteBuffer, TReadBuffer>;

public sealed partial class LeadingBarePartialFormatter<TWriteBuffer, TReadBuffer>
    : IMessagePackFormatter<TWriteBuffer, TReadBuffer, BridgeValue>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, BridgeValue value)
    {
        buffer.WriteInt32(value.X);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref BridgeValue value)
    {
        value = new BridgeValue { X = buffer.ReadInt32() };
    }
}

public class LeadingBarePartialDeclarationTest
{
    [Fact]
    public void BridgeAndConstraintsAreGeneratedForTheDeclarationWithTheBaseList()
    {
        // (on net9+ the generated Type-based bridge deliberately answers null, so the generic overload is what the
        // resolver takes; the bridge's existence is proven by the compile, its dispatch by the downlevel test projects)
        var resolver = new MessagePackFormatterResolver(new LeadingBarePartialFactory());
        Assert.IsType<LeadingBarePartialFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer>>(
            resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, BridgeValue>());

        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(new LeadingBarePartialFactory()));
        Assert.Equal(99, MessagePackSerializer.Deserialize<BridgeValue>(MessagePackSerializer.Serialize(new BridgeValue { X = 99 }, options), options).X);
    }
}

// Outer.Factory and Outer<T>.Factory share a simple name: the bridge's hint name must carry the containing arity or
// the second AddSource fails (CS8785); and a type legally named by a keyword must be emitted escaped (`@event`).
// Compiling is the assertion.
public partial class BridgeOuter
{
    public sealed partial class Factory : MessagePackFormatterFactory
    {
        public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type) => null;
    }
}

public partial class BridgeOuter<T>
{
    public sealed partial class Factory : MessagePackFormatterFactory
    {
        public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type) => null;
    }

    public sealed partial class Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T?>
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T? value) => buffer.WriteNil();

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T? value) => buffer.Skip();
    }
}

public sealed partial class @event : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type) => null;
}

public class BridgeNamingTest
{
    [Fact]
    public void SameSimpleNameUnderDifferentArities_AndKeywordNames_Generate()
    {
        Assert.Null(new BridgeOuter.Factory().CreateFormatter(typeof(CompatibleArrayPoolListWriteBuffer), typeof(CompatibleReadOnlySpanReadBuffer), typeof(int)));
        Assert.Null(new BridgeOuter<string>.Factory().CreateFormatter(typeof(CompatibleArrayPoolListWriteBuffer), typeof(CompatibleReadOnlySpanReadBuffer), typeof(int)));
        Assert.Null(new @event().CreateFormatter(typeof(CompatibleArrayPoolListWriteBuffer), typeof(CompatibleReadOnlySpanReadBuffer), typeof(int)));
        Assert.NotNull(new BridgeOuter<string>.Formatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer>());
    }
}

// SIMULATION of a factory from a library compiled against a netstandard build of MessagePack: it serves int (as a
// string, so the wire tells which factory won) but only over the Compatible (non-ref-struct) buffer pairs.
public sealed partial class StringifyingIntFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value) => buffer.WriteString(value.ToString());

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value) => value = int.Parse(buffer.ReadString()!);
}

#pragma warning disable MsgPack102
sealed class CompatOnlyIntFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
        => valueType == typeof(int) && !writeBufferType.IsByRefLike && !readBufferType.IsByRefLike
            ? Activator.CreateInstance(typeof(StringifyingIntFormatter<,>).MakeGenericType(writeBufferType, readBufferType))
            : null;
}
#pragma warning restore MsgPack102

public class CompatOnlyFactoryInChainTest
{
    // a compat-only factory placed before the built-ins must win the type it serves: the chain used to skip its null
    // for the ref struct pair and let BuiltIn take int with the integer wire, so the same type serialized two ways
    // depending on what followed in the chain (and ThrowOnLegacyFormatter never saw the legacy formatter)
    [Fact]
    public void CompatOnlyFactory_KeepsItsPlaceInTheChain()
    {
        var alone = new MessagePackSerializerOptions(new MessagePackFormatterResolver(new CompatOnlyIntFactory()));
        var chained = new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.Combine(new CompatOnlyIntFactory(), MessagePackFormatterFactory.Default)));

        var expected = new byte[] { 0xA2, (byte)'4', (byte)'2' }; // "42"
        Assert.Equal(expected, MessagePackSerializer.Serialize(42, alone));
        Assert.Equal(expected, MessagePackSerializer.Serialize(42, chained));
        Assert.Equal(42, MessagePackSerializer.Deserialize<int>(expected, chained));

        // a type the compat-only factory does not serve still falls through to the rest of the chain
        Assert.Equal(new byte[] { 0xA1, (byte)'x' }, MessagePackSerializer.Serialize("x", chained));

        var strict = new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.Combine(new CompatOnlyIntFactory(), MessagePackFormatterFactory.Default)) { ThrowOnLegacyFormatter = true });
        Assert.Throws<InvalidOperationException>(() => MessagePackSerializer.Serialize(42, strict));
    }
}
