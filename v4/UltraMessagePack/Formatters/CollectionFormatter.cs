using SerializerFoundation;

namespace UltraMessagePack.Formatters;

public sealed partial class ArrayFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T[]?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        buffer.WriteArrayHeader(value.Length);

        var f = formatter;

        // TODO: remove unneeds ref T accesss.

        // remove covariance check
        ref var head = ref MemoryMarshal.GetArrayDataReference(value);
        for (int i = 0; i < value.Length; i++)
        {
            f.Serialize(ref buffer, ref state, Unsafe.Add(ref head, i));
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        // TODO: check "count" validation.
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

public sealed partial class ArrayFormatterFactory<T> : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return new ArrayFormatter<TWriteBuffer, TReadBuffer, T>();
    }
}




public sealed partial class ListFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, List<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, List<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        buffer.WriteArrayHeader(value.Count);

        var f = formatter;
#if NET
        var span = CollectionsMarshal.AsSpan(value);
        for (int i = 0; i < span.Length; i++)
        {
            f.Serialize(ref buffer, ref state, span[i]);
        }
#else
        // no CollectionsMarshal downlevel: the indexer bounds check per element is the
        // accepted correctness-tier cost
        for (int i = 0; i < value.Count; i++)
        {
            f.Serialize(ref buffer, ref state, value[i]);
        }
#endif
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
        var f = formatter;
#if NET
        CollectionsMarshal.SetCount(result, count);
        var span = CollectionsMarshal.AsSpan(result);
        for (int i = 0; i < span.Length; i++)
        {
            f.Deserialize(ref buffer, ref state, ref span[i]);
        }
#else
        // populate semantics without SetCount/AsSpan: overwrite the reused prefix via the
        // indexer, Add the growth, truncate the excess
        if (result.Count > count)
        {
            result.RemoveRange(count, result.Count - count);
        }
        for (int i = 0; i < count; i++)
        {
            var item = i < result.Count ? result[i] : default(T)!;
            f.Deserialize(ref buffer, ref state, ref item);
            if (i < result.Count)
            {
                result[i] = item;
            }
            else
            {
                result.Add(item);
            }
        }
#endif
        value = result;
    }
}

public sealed partial class ListFormatterFactory<T> : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return new ListFormatter<TWriteBuffer, TReadBuffer, T>();
    }
}

public sealed class DictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Dictionary<TKey, TValue>?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TKey : notnull
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TKey> keyFormatter = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TValue> valueFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        keyFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TKey>();
        valueFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TValue>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Dictionary<TKey, TValue>? value)
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
            kf.Serialize(ref buffer, ref state, kv.Key);
            vf.Serialize(ref buffer, ref state, kv.Value);
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

public sealed partial class DictionaryFormatterFactory<TKey, TValue> : IMessagePackFormatterFactory
    where TKey : notnull
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return new DictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>();
    }
}