using System.Diagnostics.CodeAnalysis;

namespace MessagePack.Formatters;

public sealed class GenericCollectionFormatter<TWriteBuffer, TReadBuffer, TElement, TCollection> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TCollection?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TCollection : class, ICollection<TElement>, new()
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TElement> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TElement>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TCollection? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        var f = formatter;
        state.Enter();
        buffer.WriteArrayHeader(value.Count);
        foreach (var item in value)
        {
            f.Serialize(ref buffer, ref state, item);
        }
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TCollection? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadArrayHeader();
        var result = new TCollection();
        var f = formatter;
        state.Enter();
        for (int i = 0; i < count; i++)
        {
            TElement item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            result.Add(item);
        }
        state.Exit();
        value = result;
    }
}

public sealed class GenericDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue, TDictionary> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TDictionary?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TDictionary : class, IDictionary<TKey, TValue>, new()
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TKey> keyFormatter = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TValue> valueFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        keyFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TKey>();
        valueFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TValue>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TDictionary? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteMapHeader(value.Count);
        foreach (var kvp in value)
        {
            keyFormatter.Serialize(ref buffer, ref state, kvp.Key);
            valueFormatter.Serialize(ref buffer, ref state, kvp.Value);
        }
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TDictionary? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadMapHeader();
        var result = new TDictionary(); // the concrete type owns its comparer
        state.Enter();
        for (int i = 0; i < count; i++)
        {
            TKey key = default!;
            TValue val = default!;
            keyFormatter.Deserialize(ref buffer, ref state, ref key);
            valueFormatter.Deserialize(ref buffer, ref state, ref val);
            try
            {
                result.Add(key, val); // nil or duplicate keys are data errors
            }
            catch (ArgumentException ex)
            {
                throw new MessagePackSerializationException("Invalid map key in payload (nil or duplicate).", ex);
            }
        }
        state.Exit();
        value = result;
    }
}

// one method, two signatures: net9+ overrides the base virtual (constraints inherited);
// downlevel has no base member, so the constraints are spelled out
public sealed partial class GenericCollectionFormatterFactory<TElement, TCollection> : MessagePackFormatterFactory
    where TCollection : class, ICollection<TElement>, new()
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(TCollection) ? new GenericCollectionFormatter<TWriteBuffer, TReadBuffer, TElement, TCollection>() : null;
    }
}

public sealed partial class GenericDictionaryFormatterFactory<TKey, TValue, TDictionary> : MessagePackFormatterFactory
    where TDictionary : class, IDictionary<TKey, TValue>, new()
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(TDictionary) ? new GenericDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue, TDictionary>() : null;
    }
}

public sealed class GenericEnumerableFormatter<TWriteBuffer, TReadBuffer, TElement, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCollection> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TCollection?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TCollection : IEnumerable<TElement>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TElement> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TElement>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TCollection? value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }
        // the header needs the count up front: collection views report it, anything else
        // buffers the elements once (v3 buffered the encoded bytes instead; same wire)
        IReadOnlyCollection<TElement> counted = value switch
        {
            ICollection<TElement> collection => new CollectionCountAdapter(collection),
            IReadOnlyCollection<TElement> readOnly => readOnly,
            _ => new List<TElement>(value),
        };
        var f = formatter;
        state.Enter();
        buffer.WriteArrayHeader(counted.Count);
        foreach (var item in counted)
        {
            f.Serialize(ref buffer, ref state, item);
        }
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TCollection? value)
    {
        if (buffer.TryReadNil())
        {
            value = default;
            return;
        }
        var count = buffer.ReadArrayHeader();
        var items = new TElement[count];
        var f = formatter;
        state.Enter();
        for (int i = 0; i < count; i++)
        {
            f.Deserialize(ref buffer, ref state, ref items[i]);
        }
        state.Exit();
        value = (TCollection)Activator.CreateInstance(typeof(TCollection), items)!;
    }

    sealed class CollectionCountAdapter(ICollection<TElement> collection) : IReadOnlyCollection<TElement>
    {
        public int Count => collection.Count;

        public IEnumerator<TElement> GetEnumerator() => collection.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => collection.GetEnumerator();
    }
}

public sealed class GenericReadOnlyDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDictionary> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TDictionary?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TKey> keyFormatter = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TValue> valueFormatter = null!;
    IEqualityComparer<TKey>? comparer;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        keyFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TKey>();
        valueFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TValue>();
        if (resolver.HashFloodingResistant)
        {
            comparer = HashFloodingResistantEqualityComparer.Get<TKey>();
        }
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TDictionary? value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteMapHeader(value.Count);
        foreach (var kvp in value)
        {
            keyFormatter.Serialize(ref buffer, ref state, kvp.Key);
            valueFormatter.Serialize(ref buffer, ref state, kvp.Value);
        }
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TDictionary? value)
    {
        if (buffer.TryReadNil())
        {
            value = default;
            return;
        }
        var count = buffer.ReadMapHeader();
        var intermediate = new Dictionary<TKey, TValue>(comparer);
        state.Enter();
        for (int i = 0; i < count; i++)
        {
            TKey key = default!;
            TValue val = default!;
            keyFormatter.Deserialize(ref buffer, ref state, ref key);
            valueFormatter.Deserialize(ref buffer, ref state, ref val);
            try
            {
                intermediate.Add(key, val); // nil or duplicate keys are data errors
            }
            catch (ArgumentException ex)
            {
                throw new MessagePackSerializationException("Invalid map key in payload (nil or duplicate).", ex);
            }
        }
        state.Exit();
        value = (TDictionary)Activator.CreateInstance(typeof(TDictionary), intermediate)!;
    }
}

public sealed partial class GenericEnumerableFormatterFactory<TElement, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCollection> : MessagePackFormatterFactory
    where TCollection : IEnumerable<TElement>
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(TCollection) ? new GenericEnumerableFormatter<TWriteBuffer, TReadBuffer, TElement, TCollection>() : null;
    }
}

public sealed partial class GenericReadOnlyDictionaryFormatterFactory<TKey, TValue, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDictionary> : MessagePackFormatterFactory
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(TDictionary) ? new GenericReadOnlyDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue, TDictionary>() : null;
    }
}
