using System.Buffers;
using SerializerFoundation;

namespace UltraMessagePack.Tests;

public struct RoutedValue
{
    public int X;
}

// A formatter with the DOWNLEVEL generic shape: no `allows ref struct` on the buffer
// type parameters — exactly what a netstandard-compiled formatter library presents after
// assembly unification on net10. It can only close over the Compatible buffers.
// The nested int formatter exercises resolution INSIDE the compatibility graph.
sealed class RoutedValueFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, RoutedValue>
    where TWriteBuffer : struct, IWriteBuffer
    where TReadBuffer : struct, IReadBuffer
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, int> intFormatter = default!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        intFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, int>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, RoutedValue value)
    {
        intFormatter.Serialize(ref buffer, ref state, value.X);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref RoutedValue value)
    {
        var x = 0;
        intFormatter.Deserialize(ref buffer, ref state, ref x);
        value = new RoutedValue { X = x };
    }
}

// SIMULATION of a downlevel-compiled GENERATED factory: Type-based member only, and its
// dispatch knows only the Compatible buffer pairs — unknown pairs (the ref struct ones a
// downlevel compilation cannot even name) return null. This is the contract shape the
// resolver's fallback probe keys on.
#pragma warning disable UMP102 // simulating a factory compiled against the downlevel surface
sealed class CompatiblePairOnlyFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        if (valueType != typeof(RoutedValue))
        {
            return null;
        }
        if (writeBufferType == typeof(CompatibleArrayPoolListWriteBuffer) && readBufferType == typeof(CompatibleReadOnlySpanReadBuffer))
        {
            return new RoutedValueFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer>();
        }
        if (writeBufferType == typeof(CompatibleArrayPoolListWriteBuffer) && readBufferType == typeof(CompatibleReadOnlySequenceReadBuffer))
        {
            return new RoutedValueFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySequenceReadBuffer>();
        }
        if (writeBufferType == typeof(CompatibleBufferWriterWriteBuffer) && readBufferType == typeof(CompatibleReadOnlySpanReadBuffer))
        {
            return new RoutedValueFormatter<CompatibleBufferWriterWriteBuffer, CompatibleReadOnlySpanReadBuffer>();
        }
        return null;
    }
}
#pragma warning restore UMP102

public class CompatibilityFallbackRoutingTests
{
    static MessagePackSerializerOptions MakeOptions(out List<Type> fallbackTypes, bool throwOnLegacyFormatter = false)
    {
        var resolver = new MessagePackFormatterResolver(
            MessagePackFormatterFactory.Combine(
                new CompatiblePairOnlyFactory(),
                BuiltInFormatterFactory.Instance,
                GenericFormatterFactory.Instance),
            throwOnLegacyFormatter: throwOnLegacyFormatter);
        var captured = new List<Type>();
        resolver.CompatibilityFallback += captured.Add;
        fallbackTypes = captured;
        return new MessagePackSerializerOptions(resolver);
    }

    [Fact]
    public void DownlevelOnlyFormatterRoundTripsViaCompatibilityPath()
    {
        var options = MakeOptions(out var fallbackTypes);

        var payload = MessagePackSerializer.Serialize(new RoutedValue { X = 12345 }, options);
        var result = MessagePackSerializer.Deserialize<RoutedValue>(payload, options);

        Assert.Equal(12345, result.X);
        Assert.Contains(typeof(RoutedValue), fallbackTypes);
    }

    [Fact]
    public void TaintPropagatesFromNestedNodeToRoot()
    {
        // the ROOT (List<>) is fully fast-capable; only a child is downlevel-only. The
        // whole root graph must route, and the event reports the ROOT type.
        var options = MakeOptions(out var fallbackTypes);

        var value = new List<RoutedValue> { new() { X = 1 }, new() { X = -2 }, new() { X = 3 } };
        var payload = MessagePackSerializer.Serialize(value, options);
        var result = MessagePackSerializer.Deserialize<List<RoutedValue>>(payload, options);

        Assert.Equal(value.Select(v => v.X), result!.Select(v => v.X));
        Assert.Contains(typeof(List<RoutedValue>), fallbackTypes);
    }

    [Fact]
    public void UntaintedTypesStayOnTheFastPath()
    {
        var options = MakeOptions(out var fallbackTypes);

        // routes RoutedValue first so its decision is cached...
        MessagePackSerializer.Serialize(new RoutedValue { X = 1 }, options);
        // ...then an unrelated type must resolve clean: no new fallback notification
        var payload = MessagePackSerializer.Serialize(42, options);
        Assert.Equal(42, MessagePackSerializer.Deserialize<int>(payload, options));

        Assert.Equal([typeof(RoutedValue)], fallbackTypes);
    }

    [Fact]
    public void FallbackEventFiresOncePerRootType()
    {
        var options = MakeOptions(out var fallbackTypes);

        MessagePackSerializer.Serialize(new RoutedValue { X = 1 }, options);
        MessagePackSerializer.Serialize(new RoutedValue { X = 2 }, options);
        var payload = MessagePackSerializer.Serialize(new RoutedValue { X = 3 }, options);
        MessagePackSerializer.Deserialize<RoutedValue>(payload, options);

        Assert.Equal([typeof(RoutedValue)], fallbackTypes);
    }

    [Fact]
    public void BufferWriterEntryRoutesToo()
    {
        var options = MakeOptions(out _);

        var writer = new ArrayBufferWriter<byte>();
        MessagePackSerializer.Serialize(writer, new RoutedValue { X = 777 }, options);
        var result = MessagePackSerializer.Deserialize<RoutedValue>(writer.WrittenSpan, options);

        Assert.Equal(777, result.X);
    }

    [Fact]
    public void SequenceEntryRoutesToo()
    {
        var options = MakeOptions(out _);

        var payload = MessagePackSerializer.Serialize(new RoutedValue { X = 31337 }, options);
        var result = MessagePackSerializer.Deserialize<RoutedValue>(new ReadOnlySequence<byte>(payload), options);

        Assert.Equal(31337, result.X);
    }

#if NET9_0_OR_GREATER
    [Fact]
    public void ThrowOnLegacyFormatterRestoresTheThrow()
    {
        var options = MakeOptions(out var fallbackTypes, throwOnLegacyFormatter: true);

        var ex = Assert.Throws<InvalidOperationException>(
            () => MessagePackSerializer.Serialize(new RoutedValue { X = 1 }, options));
        Assert.Contains("net10.0", ex.Message);
        Assert.Empty(fallbackTypes);
    }
#endif
}
