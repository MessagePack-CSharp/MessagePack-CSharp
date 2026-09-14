using SerializerFoundation;

namespace MessagePack.Formatters;

public sealed class NullableFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where T : struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T? value)
    {
        if (value is T inner)
        {
            formatter.Serialize(ref buffer, ref state, inner);
        }
        else
        {
            buffer.WriteNil();
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        T inner = default;
        formatter.Deserialize(ref buffer, ref state, ref inner);
        value = inner;
    }
}

public sealed partial class NullableFormatterFactory<T> : MessagePackFormatterFactory
    where T : struct
{
    // one method, two signatures: net9+ overrides the base virtual (constraints
    // inherited); downlevel has no base member, so the constraints are spelled out
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(T?))
        {
            return new NullableFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}
