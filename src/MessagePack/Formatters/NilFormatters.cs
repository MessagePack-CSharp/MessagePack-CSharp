namespace MessagePack.Formatters;

/// <summary>Serializes <see cref="Nil"/> as the nil token.</summary>
public sealed partial class NilFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Nil>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Nil value)
    {
        buffer.WriteNil();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Nil value)
    {
        if (!buffer.TryReadNil())
        {
            throw new MessagePackSerializationException("Can't parse to Nil, the msgpack code is not nil.");
        }
        value = Nil.Default;
    }
}

// NullableNil is same as Nil.
public sealed partial class NullableNilFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Nil?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Nil? value)
    {
        buffer.WriteNil(); // null and Nil are indistinguishable on the wire by design
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Nil? value)
    {
        if (!buffer.TryReadNil())
        {
            throw new MessagePackSerializationException("Can't parse to Nil, the msgpack code is not nil.");
        }
        value = Nil.Default;
    }
}
