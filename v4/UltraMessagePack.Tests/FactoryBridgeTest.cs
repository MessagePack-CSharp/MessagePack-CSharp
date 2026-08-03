using SerializerFoundation;

namespace UltraMessagePack.Tests;

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
// member is satisfied by its default-implementation bridge. UMP102 rightly flags this
// shape for NEW net10 code, which is exactly what this type simulates not being.
#pragma warning disable UMP102
sealed class TypeBasedOnlyFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
        => valueType == typeof(BridgeValue)
            ? Activator.CreateInstance(typeof(BridgeValueFormatter<,>).MakeGenericType(writeBufferType, readBufferType))
            : null;
}
#pragma warning restore UMP102

// End-to-end proof of the two-tier factory interface: generic resolution routes through
// the default-implementation bridge into the Type-based implementation.
public class FactoryBridgeTest
{
    [Fact]
    public unsafe void BridgeResolvesThroughFallbackPair()
    {
        var resolver = new MessagePackFormatterResolver(new TypeBasedOnlyFactory());

        // generic resolution → interface default implementation → Type-based factory
        var formatter = resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, UnsafeReadOnlySpanReadBuffer, BridgeValue>();
        Assert.IsType<BridgeValueFormatter<CompatibleArrayPoolListWriteBuffer, UnsafeReadOnlySpanReadBuffer>>(formatter);

        // and the produced formatter actually works over those buffers
        var writeBuffer = new CompatibleArrayPoolListWriteBuffer();
        try
        {
            var serializeState = new SerializeState();
            formatter.Serialize(ref writeBuffer, ref serializeState, new BridgeValue { X = 12345 });
            var payload = writeBuffer.ToArray();

            fixed (byte* pointer = payload)
            {
                var readBuffer = new UnsafeReadOnlySpanReadBuffer(pointer, payload.Length);
                var deserializeState = new DeserializeState();
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
        var options = new MessagePackSerializerOptions(new TypeBasedOnlyFactory());
        var payload = MessagePackSerializer.Serialize(new BridgeValue { X = -7 }, options);
        var result = MessagePackSerializer.Deserialize<BridgeValue>(payload, options);
        Assert.Equal(-7, result.X);
    }
}
