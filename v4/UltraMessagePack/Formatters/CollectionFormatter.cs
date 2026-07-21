using SerializerFoundation;

namespace UltraMessagePack.Formatters;

public sealed class ArrayFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T[]?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref T[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        buffer.WriteArrayHeader(value.Length);

        var f = formatter;
        // remove covariance check
        ref var head = ref MemoryMarshal.GetArrayDataReference(value);
        for (int i = 0; i < value.Length; i++)
        {
            f.Serialize(ref buffer, ref state, ref Unsafe.Add(ref head, i));
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();
        // Populate contract: reuse the incoming array only when the length matches exactly
        // (its elements then act as populate targets); otherwise allocate fresh
        var result = (value != null && value.Length == count) ? value : new T[count];

        var f = formatter;
        ref var head = ref MemoryMarshal.GetArrayDataReference(result);
        for (int i = 0; i < count; i++)
        {
            f.Deserialize(ref buffer, ref state, ref Unsafe.Add(ref head, i));
        }
        value = result;
    }
}

public sealed class ArrayFormatterFactory<T> : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return new ArrayFormatter<TWriteBuffer, TReadBuffer, T>();
    }
}




public sealed class ListFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, List<T>?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref List<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        buffer.WriteArrayHeader(value.Count);

        var f = formatter;
        var span = CollectionsMarshal.AsSpan(value);
        for (int i = 0; i < span.Length; i++)
        {
            f.Serialize(ref buffer, ref state, ref span[i]);
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref List<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();
        var result = value ?? new List<T>(count);
        CollectionsMarshal.SetCount(result, count);

        var f = formatter;
        var span = CollectionsMarshal.AsSpan(result);
        for (int i = 0; i < span.Length; i++)
        {
            f.Deserialize(ref buffer, ref state, ref span[i]);
        }
        value = result;
    }
}

public sealed class ListFormatterFactory<T> : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return new ListFormatter<TWriteBuffer, TReadBuffer, T>();
    }
}

public sealed class DictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Dictionary<TKey, TValue>?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where TKey : notnull
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TKey> keyFormatter = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TValue> valueFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        keyFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TKey>();
        valueFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TValue>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref Dictionary<TKey, TValue>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        buffer.WriteMapHeader(value.Count);

        var kf = keyFormatter;
        var vf = valueFormatter;
        foreach (var kv in value)
        {
            var k = kv.Key;
            var v = kv.Value;
            kf.Serialize(ref buffer, ref state, ref k);
            vf.Serialize(ref buffer, ref state, ref v);
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Dictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();
        Dictionary<TKey, TValue> result;
        if (value != null)
        {
            result = value;
            result.Clear();
        }
        else
        {
            result = new Dictionary<TKey, TValue>(count);
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            result[k] = v;
        }
        value = result;
    }
}

public sealed class DictionaryFormatterFactory<TKey, TValue> : IMessagePackFormatterFactory
    where TKey : notnull
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return new DictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>();
    }
}