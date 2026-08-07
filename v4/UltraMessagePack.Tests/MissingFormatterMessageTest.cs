using SerializerFoundation;

namespace UltraMessagePack.Tests;

public struct MissingProbeValue
{
}

// probe target: only ever constructed and discarded by the resolver's fallback-pair probe
public sealed partial class MissingProbeValueFormatter<TWriteBuffer, TReadBuffer>
    : IMessagePackFormatter<TWriteBuffer, TReadBuffer, MissingProbeValue>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, MissingProbeValue value)
    {
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref MissingProbeValue value)
    {
    }
}

#pragma warning disable UMP102
// a factory that serves nothing: every resolution lands on MissingMessagePackFormatter
sealed class NullFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType) => null;
}

// SIMULATION of a downlevel-compiled generated factory: serves the type, but only for
// the fallback pairs its generated Type-based dispatch knows; unknown pairs return null
sealed class FallbackOnlyFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        if (valueType == typeof(MissingProbeValue) &&
            writeBufferType == typeof(CompatibleArrayPoolListWriteBuffer) &&
            readBufferType == typeof(CompatibleReadOnlySpanReadBuffer))
        {
            return new MissingProbeValueFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer>();
        }
        return null;
    }
}
#pragma warning restore UMP102

// The missing-formatter exception must identify the buffer pair, and must distinguish
// "type not registered" from "type registered, but the requesting ref struct pair is
// unservable" (= a downlevel-compiled library) — the resolver probes a fallback pair
// when a ref struct pair resolution comes back null, and only a positive probe adds
// the downlevel hint.
public class MissingFormatterMessageTest
{
    [Fact]
    public void ServableDownlevelGetsPairAndHint()
    {
        var resolver = new MessagePackFormatterResolver(new FallbackOnlyFactory());
        var formatter = resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, MissingProbeValue>();

        var message = CaptureSerializeMessage(formatter);
        Assert.Contains(nameof(MissingProbeValue), message);
        Assert.Contains(nameof(FallbackOnlyFactory), message);
        Assert.Contains(nameof(ArrayPoolListWriteBuffer), message);
        Assert.Contains(nameof(ReadOnlySpanReadBuffer), message);
        Assert.Contains("net10.0", message);
    }

    [Fact]
    public void NotRegisteredGetsPairWithoutHint()
    {
        var resolver = new MessagePackFormatterResolver(new NullFactory());
        var formatter = resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, MissingProbeValue>();

        var message = CaptureSerializeMessage(formatter);
        Assert.Contains(nameof(ArrayPoolListWriteBuffer), message);
        Assert.Contains(nameof(ReadOnlySpanReadBuffer), message);
        Assert.DoesNotContain("net10.0", message);
    }

    [Fact]
    public void FallbackPairMissingGetsPairWithoutHint()
    {
        // non-ref-struct pair: nothing to probe (the request itself was a fallback pair)
        var resolver = new MessagePackFormatterResolver(new NullFactory());
        var formatter = resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, MissingProbeValue>();

        InvalidOperationException? caught = null;
        var buffer = new CompatibleArrayPoolListWriteBuffer();
        try
        {
            var state = new SerializeState();
            formatter.Serialize(ref buffer, ref state, default);
        }
        catch (InvalidOperationException e)
        {
            caught = e;
        }
        finally
        {
            buffer.Dispose();
        }

        Assert.NotNull(caught);
        Assert.Contains(nameof(CompatibleArrayPoolListWriteBuffer), caught.Message);
        Assert.Contains(nameof(CompatibleReadOnlySpanReadBuffer), caught.Message);
        Assert.DoesNotContain("net10.0", caught.Message);
    }

    static string CaptureSerializeMessage(IMessagePackFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, MissingProbeValue> formatter)
    {
        InvalidOperationException? caught = null;
        var buffer = default(ArrayPoolListWriteBuffer);
        var state = new SerializeState();
        try
        {
            formatter.Serialize(ref buffer, ref state, default);
        }
        catch (InvalidOperationException e)
        {
            caught = e;
        }
        Assert.NotNull(caught);
        return caught.Message;
    }
}
