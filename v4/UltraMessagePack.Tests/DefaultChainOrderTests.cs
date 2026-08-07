using SerializerFoundation;
using UltraMessagePack.Formatters;

namespace UltraMessagePack.Tests;

// Chain order is Registry → Primitive → Generic: user registrations override every
// built-in tier (the v3 user-formatters-first composition convention).
public class DefaultChainOrderTests
{
    [Fact]
    public void Registry_OverridesPrimitiveTier()
    {
        // a PRIVATE registry keeps the global default-chain state untouched (registering
        // an int override in FormatterRegistry.Instance would poison every other test)
        var registry = new SourceGeneratedFormatterFactory();
        registry.RegisterFactory<int>(new NegatingIntFormatterFactory());
        var options = new MessagePackSerializerOptions(
            [registry, BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]);

        // the negating formatter wins over the primitive one: 5 goes to the wire as -5
        Assert.Equal(new byte[] { 0xFB }, MessagePackSerializer.Serialize(5, options));
        Assert.Equal(5, MessagePackSerializer.Deserialize<int>(new byte[] { 0xFB }, options));
    }

    [Fact]
    public void DefaultChain_RegistryBeatsGenericTier()
    {
        // regression guard: with Registry LAST this registration was silently dead —
        // GenericFormatterFactory claimed the enum first with int encoding
        SourceGeneratedFormatterFactory.Instance.RegisterFactory<OrderTestEnum>(
            new EnumAsStringFormatterFactory<OrderTestEnum>());
        var options = new MessagePackSerializerOptions([MessagePackFormatterFactory.Default]); // fresh resolver over the default chain

        var bytes = MessagePackSerializer.Serialize(OrderTestEnum.Two, options);
        Assert.Equal(0xA3, bytes[0]); // fixstr "Two", not fixint 1
        Assert.Equal(OrderTestEnum.Two, MessagePackSerializer.Deserialize<OrderTestEnum>(bytes, options));
    }

    // registered only by this class; safe in the process-global registry
    public enum OrderTestEnum { One, Two }

    [Fact]
    public void LeafFactories_InChain_DeclineForeignTypes()
    {
        // chain discipline: every public closed leaf factory checks the requested type,
        // so direct chain placement cannot hijack unrelated resolutions
        var options = new MessagePackSerializerOptions(
        [
            new NullableFormatterFactory<int>(),
            new EnumFormatterFactory<OrderTestEnum>(),
            new ArrayFormatterFactory<double>(),
            new ListFormatterFactory<double>(),
            new DictionaryFormatterFactory<int, int>(),
            MessagePackFormatterFactory.Default,
        ]);

        // foreign types pass through to the default chain
        Assert.Equal(new byte[] { 123 }, MessagePackSerializer.Serialize(123, options));
        Assert.Equal("x", MessagePackSerializer.Deserialize<string>(MessagePackSerializer.Serialize("x", options), options));

        // each factory still serves exactly its own type
        Assert.Equal((int?)7, MessagePackSerializer.Deserialize<int?>(MessagePackSerializer.Serialize((int?)7, options), options));
        Assert.Equal(OrderTestEnum.Two, MessagePackSerializer.Deserialize<OrderTestEnum>(MessagePackSerializer.Serialize(OrderTestEnum.Two, options), options));
        Assert.Equal(new[] { 1.5, 2.5 }, MessagePackSerializer.Deserialize<double[]>(MessagePackSerializer.Serialize(new[] { 1.5, 2.5 }, options), options));
        Assert.Equal(new List<double> { 1.5 }, MessagePackSerializer.Deserialize<List<double>>(MessagePackSerializer.Serialize(new List<double> { 1.5 }, options), options));
        var dict = new Dictionary<int, int> { [1] = 2 };
        Assert.Equal(dict, MessagePackSerializer.Deserialize<Dictionary<int, int>>(MessagePackSerializer.Serialize(dict, options), options));
    }
}

public sealed class NegatingIntFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value)
    {
        buffer.WriteInt32(-value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value)
    {
        value = -buffer.ReadInt32();
    }
}

public sealed partial class NegatingIntFormatterFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return new NegatingIntFormatter<TWriteBuffer, TReadBuffer>();
    }
}
