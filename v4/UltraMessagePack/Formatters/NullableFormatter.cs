using SerializerFoundation;

namespace UltraMessagePack.Formatters;

public sealed class NullableFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where T : struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref T? value)
    {
        if (value is T inner)
        {
            formatter.Serialize(ref buffer, ref state, ref inner);
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

public sealed class NullableFormatterFactory<T> : IMessagePackFormatterFactory
    where T : struct
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return new NullableFormatter<TWriteBuffer, TReadBuffer, T>();
    }
}
