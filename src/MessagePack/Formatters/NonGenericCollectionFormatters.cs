using System.Collections;

namespace MessagePack.Formatters;

// The non-generic collection, elements route through GetFormatter<object>.

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
        // populate: a mutable, growable instance (an ArrayList) is refilled in place, as the concrete-type formatters
        // do; a read-only or fixed-size one (an array behind the view) is replaced
        if (value is { IsReadOnly: false, IsFixedSize: false } existing)
        {
            if (buffer.TryReadNil())
            {
                value = null;
                return;
            }
            var count = buffer.ReadArrayHeader(ref state);
            state.Enter();
            existing.Clear();
            for (int i = 0; i < count; i++)
            {
                object? element = null;
                formatter.Deserialize(ref buffer, ref state, ref element);
                existing.Add(element);
            }
            state.Exit();
            return;
        }
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
        var count = buffer.ReadMapHeader(ref state);
        state.Enter();

        // populate: a mutable instance (a Hashtable, with its comparer) is refilled in place; a read-only one is
        // replaced. (A flooding-resistant resolver replaces an instance on the default comparer only through the
        // generic Dictionary<,> formatter; the non-generic view keeps whatever comparer the instance has.)
        IDictionary dictionary;
        if (value is { IsReadOnly: false, IsFixedSize: false } existing)
        {
            existing.Clear();
            dictionary = existing;
        }
        else
        {
            dictionary = comparer == null
                ? new Dictionary<object, object?>(count)
                : new Dictionary<object, object?>(count, comparer);
        }
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
        var count = buffer.ReadArrayHeader(ref state);
        // Populate contract: a mutable, growable instance is refilled in place
        T list;
        if (value is { IsReadOnly: false, IsFixedSize: false })
        {
            list = value;
            list.Clear();
        }
        else
        {
            list = new T();
        }
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
        var count = buffer.ReadMapHeader(ref state);
        // Populate contract: a mutable, growable instance is refilled in place; the concrete type owns its comparer
        // either way (e.g. Hashtable)
        T dictionary;
        if (value is { IsReadOnly: false, IsFixedSize: false })
        {
            dictionary = value;
            dictionary.Clear();
        }
        else
        {
            dictionary = new T();
        }
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
        var count = buffer.ReadArrayHeader(ref state);
        if (count == 0)
        {
            return Array.Empty<object?>();
        }
        state.Enter();

        var array = new object?[count];
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
