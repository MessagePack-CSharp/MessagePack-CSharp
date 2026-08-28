using System.Collections;

namespace MessagePack.Formatters;

// IGrouping = [key, [elements]] (fixarray2), ILookup = array of groupings.
// ILookup guarantees enumeration in first-encounter key order (the LINQ GroupBy/ToLookup contract)
// We keep it explicitly, because Dictionary's enumeration order is not guaranteed

sealed class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
{
    readonly TKey key;
    
    readonly IEnumerable<TElement> elements;

    // threads ILookup's first-encounter enumeration order through the nodes themselves
    internal Grouping<TKey, TElement>? nextInAddOrder;

    public Grouping(TKey key, IEnumerable<TElement> elements)
    {
        this.key = key;
        this.elements = elements;
    }

    public TKey Key => key;

    public IEnumerator<TElement> GetEnumerator() => elements.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();
}

sealed class Lookup<TKey, TElement> : ILookup<TKey, TElement>
    where TKey : notnull
{
    readonly Dictionary<TKey, Grouping<TKey, TElement>> map;
    readonly Grouping<TKey, TElement>? first;

    public Lookup(Dictionary<TKey, Grouping<TKey, TElement>> map, Grouping<TKey, TElement>? first)
    {
        this.map = map;
        this.first = first;
    }

    public IEnumerable<TElement> this[TKey key] => map.TryGetValue(key, out var grouping) ? grouping : [];

    public int Count => map.Count;

    public bool Contains(TKey key) => map.ContainsKey(key);

    public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
    {
        for (var grouping = first; grouping != null; grouping = grouping.nextInAddOrder)
        {
            yield return grouping;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class InterfaceGroupingFormatter<TWriteBuffer, TReadBuffer, TKey, TElement> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IGrouping<TKey, TElement>?>
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
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, IEnumerable<TElement>> elementsFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        keyFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TKey>();
        elementsFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, IEnumerable<TElement>>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IGrouping<TKey, TElement>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        state.Enter();

        buffer.WriteArrayHeader(2);
        keyFormatter.Serialize(ref buffer, ref state, value.Key);
        elementsFormatter.Serialize(ref buffer, ref state, value);

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IGrouping<TKey, TElement>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var header = buffer.ReadArrayHeader();
        if (header != 2)
        {
            throw new MessagePackSerializationException($"Invalid Grouping format: expected a 2-element array, got {header}.");
        }

        state.Enter();

        TKey key = default!;
        IEnumerable<TElement>? elements = null;
        keyFormatter.Deserialize(ref buffer, ref state, ref key);
        elementsFormatter.Deserialize(ref buffer, ref state, ref elements!);

        value = new Grouping<TKey, TElement>(key, elements ?? []);
        state.Exit();
    }
}

public sealed partial class InterfaceGroupingFormatterFactory<TKey, TElement> : MessagePackFormatterFactory
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
        if (type == typeof(IGrouping<TKey, TElement>))
        {
            return new InterfaceGroupingFormatter<TWriteBuffer, TReadBuffer, TKey, TElement>();
        }
        return null;
    }
}

public sealed class InterfaceLookupFormatter<TWriteBuffer, TReadBuffer, TKey, TElement> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ILookup<TKey, TElement>?>
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
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, IGrouping<TKey, TElement>?> groupingFormatter = null!;
    IEqualityComparer<TKey>? comparer;

    public InterfaceLookupFormatter(IEqualityComparer<TKey>? comparer)
    {
        this.comparer = comparer;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        groupingFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, IGrouping<TKey, TElement>?>();
        if (comparer == null && resolver.HashFloodingResistant)
        {
            comparer = HashFloodingResistantEqualityComparer.Get<TKey>();
        }
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ILookup<TKey, TElement>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var gf = groupingFormatter;
        state.Enter();

        buffer.WriteArrayHeader(value.Count);

        foreach (var grouping in value)
        {
            gf.Serialize(ref buffer, ref state, grouping);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ILookup<TKey, TElement>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();
        var map = new Dictionary<TKey, Grouping<TKey, TElement>>(count, comparer);
        Grouping<TKey, TElement>? first = null;
        Grouping<TKey, TElement>? last = null;

        var gf = groupingFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            IGrouping<TKey, TElement>? deserialized = null;
            gf.Deserialize(ref buffer, ref state, ref deserialized);
            if (deserialized == null)
            {
                throw new MessagePackSerializationException("Invalid Lookup format: a grouping element was nil.");
            }

            // the resolver normally hands back our own Grouping; a user-overridden
            // IGrouping formatter producing another implementation gets rewrapped
            var grouping = deserialized as Grouping<TKey, TElement>
                ?? new Grouping<TKey, TElement>(deserialized.Key, deserialized);

            // a duplicate group key is a data error, the same rule as the dictionary family
            // (a well-formed ILookup payload has unique group keys by construction)
            if (map.TryGetValue(grouping.Key, out _))
            {
                MessagePackSerializationException.ThrowDuplicateMapKey();
            }
            map[grouping.Key] = grouping;
            if (last == null)
            {
                first = grouping;
            }
            else
            {
                last.nextInAddOrder = grouping;
            }
            last = grouping;
        }

        value = new Lookup<TKey, TElement>(map, first);
        state.Exit();
    }
}

public sealed partial class InterfaceLookupFormatterFactory<TKey, TElement> : MessagePackFormatterFactory
    where TKey : notnull
{
    readonly IEqualityComparer<TKey>? comparer;

    public InterfaceLookupFormatterFactory()
    {
        this.comparer = null;
    }

    public InterfaceLookupFormatterFactory(IEqualityComparer<TKey>? comparer)
    {
        this.comparer = comparer;
    }

#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(ILookup<TKey, TElement>))
        {
            return new InterfaceLookupFormatter<TWriteBuffer, TReadBuffer, TKey, TElement>(comparer);
        }
        return null;
    }
}

