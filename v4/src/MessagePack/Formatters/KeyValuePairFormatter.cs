namespace MessagePack.Formatters;

/// <summary>Serializes <see cref="KeyValuePair{TKey, TValue}"/> as [key, value].</summary>
public sealed partial class KeyValuePairFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, KeyValuePair<TKey, TValue>>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TKey> keyFormatter = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TValue> valueFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        keyFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TKey>();
        valueFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TValue>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, KeyValuePair<TKey, TValue> value)
    {
        state.Enter();
        buffer.WriteFixArrayHeader(2);
        keyFormatter.Serialize(ref buffer, ref state, value.Key);
        valueFormatter.Serialize(ref buffer, ref state, value.Value);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref KeyValuePair<TKey, TValue> value)
    {
        if (buffer.ReadArrayHeader() != 2)
        {
            throw new MessagePackSerializationException("Invalid KeyValuePair format.");
        }
        state.Enter();
        TKey key = default!;
        TValue val = default!;
        keyFormatter.Deserialize(ref buffer, ref state, ref key);
        valueFormatter.Deserialize(ref buffer, ref state, ref val);
        value = new KeyValuePair<TKey, TValue>(key, val);
        state.Exit();
    }
}

// one method, two signatures: net9+ overrides the base virtual (constraints inherited);
// downlevel has no base member, so the constraints are spelled out
public sealed partial class KeyValuePairFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(KeyValuePair<TKey, TValue>) ? new KeyValuePairFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>() : null;
    }
}
