using System.Collections;

namespace MessagePack.Formatters;

// The non-generic collection, elements route through GetFormatter<object>.

public sealed partial class NonGenericInterfaceEnumerableFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IEnumerable?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> formatter = null!;
    IMessagePackFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, object?> staged = null!; // for sources without a count, see StagedEnumerable

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, object?>();
        staged = resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, object?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IEnumerable? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        if (value is ICollection collection)
        {
            buffer.WriteArrayHeader(collection.Count);
            foreach (var item in collection)
            {
                formatter.Serialize(ref buffer, ref state, item);
            }
        }
        else
        {
            // no count up front: each element is encoded as it is enumerated, then the header and the bytes
            StagedEnumerable.Serialize(ref buffer, ref state, staged, value);
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
            var capacity = ReadBufferExtensions.PresizeHashCapacity<KeyValuePair<object, object?>>(count, buffer.BytesRemaining);
            dictionary = comparer == null
                ? new Dictionary<object, object?>(capacity)
                : new Dictionary<object, object?>(capacity, comparer);
        }
        if (dictionary is SortedList sortedList)
        {
            NonGenericCollectionFormatterHelper.ReadIntoSortedList(formatter, ref buffer, ref state, count, sortedList);
            state.Exit();
            value = dictionary;
            return;
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
        if (dictionary is SortedList sortedList)
        {
            NonGenericCollectionFormatterHelper.ReadIntoSortedList(formatter, ref buffer, ref state, count, sortedList);
            state.Exit();
            value = dictionary;
            return;
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

    // System.Collections.SortedList (the type, a class derived from it, an IDictionary that holds one) inserts by binary
    // search plus a shift of everything after the slot: keys in descending order cost O(n) each (200,000 took 5 seconds).
    // So the pairs are read first, sorted, and added in the order the list keeps them, where each Add shifts at most one
    // element. The list does not expose its comparer: the sort uses the default one, and the first two Adds (the
    // smallest and the largest) show whether the list runs the same way or reversed. A list on a comparer unrelated to
    // the default order gets no help from this, and keys the default comparer cannot compare (mixed runtime types) are
    // added in payload order, where the list itself refuses them.
    internal static void ReadIntoSortedList<TWriteBuffer, TReadBuffer>(IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> formatter, ref TReadBuffer buffer, ref DeserializeState state, int count, SortedList list)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var capacity = ReadBufferExtensions.PresizeCapacity<KeyValuePair<object, object?>>(count, buffer.BytesRemaining);
        var keys = new object?[capacity];
        var values = new object?[capacity];
        for (int i = 0; i < count; i++)
        {
            if (i == keys.Length)
            {
                // the header claimed more than its bytes justify (see PresizeCapacity): grow as the bytes arrive
                var grown = (int)Math.Min(Math.Max(4L, keys.Length * 2L), count);
                Array.Resize(ref keys, grown);
                Array.Resize(ref values, grown);
            }
            formatter.Deserialize(ref buffer, ref state, ref keys[i]);
            formatter.Deserialize(ref buffer, ref state, ref values[i]);
        }

        if (count > 2)
        {
            // on copies: a comparer failure part-way leaves the originals in payload order
            var sortedKeys = (object?[])keys.Clone();
            var sortedValues = (object?[])values.Clone();
            try
            {
                Array.Sort(sortedKeys, sortedValues, 0, count, Comparer.Default);
                keys = sortedKeys;
                values = sortedValues;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                // not comparable by the default comparer
            }
        }

        if (count == 0)
        {
            return;
        }
        Add(list, keys[0], values[0]);
        if (count == 1)
        {
            return;
        }
        Add(list, keys[count - 1], values[count - 1]);
        if (ReferenceEquals(list.GetKey(0), keys[0]))
        {
            for (int i = 1; i < count - 1; i++)
            {
                Add(list, keys[i], values[i]);
            }
        }
        else
        {
            for (int i = count - 2; i >= 1; i--)
            {
                Add(list, keys[i], values[i]);
            }
        }
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
