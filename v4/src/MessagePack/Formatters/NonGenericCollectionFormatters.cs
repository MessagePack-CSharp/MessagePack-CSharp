using System.Collections;

namespace MessagePack.Formatters;

// The non-generic collection, elements route through GetFormatter<object> - PrimitiveObjectFormatter by default.

public sealed partial class NonGenericInterfaceEnumerableFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IEnumerable?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, object?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IEnumerable? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        
        // the count is unknown upfront: collect the item REFERENCES first.
        var items = new List<object?>();
        foreach (var item in value)
        {
            items.Add(item);
        }
        state.Enter();
        buffer.WriteArrayHeader(items.Count);
        foreach (var item in items)
        {
            formatter.Serialize(ref buffer, ref state, item);
        }
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IEnumerable? value)
    {
        value = NonGenericCollectionFormatterHelper.DeserializeObjectArray(formatter, ref buffer, ref state);
    }
}

public sealed partial class NonGenericInterfaceCollectionFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ICollection?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, object?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ICollection? value)
    {
        NonGenericCollectionFormatterHelper.SerializeCollection(formatter, ref buffer, ref state, value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ICollection? value)
    {
        value = NonGenericCollectionFormatterHelper.DeserializeObjectArray(formatter, ref buffer, ref state);
    }
}

public sealed partial class NonGenericInterfaceListFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IList?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, object?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IList? value)
    {
        NonGenericCollectionFormatterHelper.SerializeCollection(formatter, ref buffer, ref state, value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IList? value)
    {
        value = NonGenericCollectionFormatterHelper.DeserializeObjectArray(formatter, ref buffer, ref state);
    }
}

public sealed partial class NonGenericInterfaceDictionaryFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IDictionary?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> formatter = null!;
    IEqualityComparer<object>? comparer;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, object?>();
        if (resolver.HashFloodingResistant)
        {
            comparer = HashFloodingResistantEqualityComparer.Get<object>();
        }
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IDictionary? value)
    {
        NonGenericCollectionFormatterHelper.SerializeDictionary(formatter, ref buffer, ref state, value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IDictionary? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadMapHeader();
        var dictionary = comparer == null
            ? new Dictionary<object, object?>(count)
            : new Dictionary<object, object?>(count, comparer);
        state.Enter();
        for (int i = 0; i < count; i++)
        {
            object? key = null;
            object? val = null;
            formatter.Deserialize(ref buffer, ref state, ref key);
            formatter.Deserialize(ref buffer, ref state, ref val);
            NonGenericCollectionFormatterHelper.Add(dictionary, key, val);
        }
        state.Exit();
        value = dictionary;
    }
}

public sealed class NonGenericListFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where T : class, IList, new()
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, object?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T? value)
    {
        NonGenericCollectionFormatterHelper.SerializeCollection(formatter, ref buffer, ref state, value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadArrayHeader();
        var list = new T();
        state.Enter();
        for (int i = 0; i < count; i++)
        {
            object? item = null;
            formatter.Deserialize(ref buffer, ref state, ref item);
            list.Add(item);
        }
        state.Exit();
        value = list;
    }
}

public sealed class NonGenericDictionaryFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where T : class, IDictionary, new()
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, object?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T? value)
    {
        NonGenericCollectionFormatterHelper.SerializeDictionary(formatter, ref buffer, ref state, value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadMapHeader();
        var dictionary = new T(); // the concrete type owns its comparer (e.g. Hashtable)
        state.Enter();
        for (int i = 0; i < count; i++)
        {
            object? key = null;
            object? val = null;
            formatter.Deserialize(ref buffer, ref state, ref key);
            formatter.Deserialize(ref buffer, ref state, ref val);
            NonGenericCollectionFormatterHelper.Add(dictionary, key, val);
        }
        state.Exit();
        value = dictionary;
    }
}

// one method, two signatures: net9+ overrides the base virtual (constraints inherited);
// downlevel has no base member, so the constraints are spelled out
public sealed partial class NonGenericListFormatterFactory<T> : MessagePackFormatterFactory
    where T : class, IList, new()
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(T) ? new NonGenericListFormatter<TWriteBuffer, TReadBuffer, T>() : null;
    }
}

public sealed partial class NonGenericDictionaryFormatterFactory<T> : MessagePackFormatterFactory
    where T : class, IDictionary, new()
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(T) ? new NonGenericDictionaryFormatter<TWriteBuffer, TReadBuffer, T>() : null;
    }
}

file static class NonGenericCollectionFormatterHelper
{
    internal static void SerializeCollection<TWriteBuffer, TReadBuffer>(
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> formatter,
        ref TWriteBuffer buffer, ref SerializeState state, ICollection? value)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteArrayHeader(value.Count);
        foreach (var item in value)
        {
            formatter.Serialize(ref buffer, ref state, item);
        }
        state.Exit();
    }

    internal static void SerializeDictionary<TWriteBuffer, TReadBuffer>(
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> formatter,
        ref TWriteBuffer buffer, ref SerializeState state, IDictionary? value)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteMapHeader(value.Count);
        foreach (DictionaryEntry entry in value)
        {
            formatter.Serialize(ref buffer, ref state, entry.Key);
            formatter.Serialize(ref buffer, ref state, entry.Value);
        }
        state.Exit();
    }

    internal static object?[]? DeserializeObjectArray<TWriteBuffer, TReadBuffer>(
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> formatter,
        ref TReadBuffer buffer, ref DeserializeState state)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (buffer.TryReadNil())
        {
            return null;
        }
        var count = buffer.ReadArrayHeader();
        if (count == 0)
        {
            return Array.Empty<object?>();
        }
        var array = new object?[count];
        state.Enter();
        for (int i = 0; i < array.Length; i++)
        {
            formatter.Deserialize(ref buffer, ref state, ref array[i]);
        }
        state.Exit();
        return array;
    }

    internal static void Add(IDictionary dictionary, object? key, object? value)
    {
        try
        {
            dictionary.Add(key!, value); // nil or duplicate keys are data errors
        }
        catch (ArgumentException ex)
        {
            throw new MessagePackSerializationException("Invalid map key in payload (nil or duplicate).", ex);
        }
    }
}
