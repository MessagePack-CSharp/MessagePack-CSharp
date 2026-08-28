using System.Collections.Frozen;

namespace MessagePack.Formatters;

public sealed class FrozenDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, FrozenDictionary<TKey, TValue>?>
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
    IEqualityComparer<TKey>? comparer;

    public FrozenDictionaryFormatter(IEqualityComparer<TKey>? comparer)
    {
        this.comparer = comparer;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        keyFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TKey>();
        valueFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TValue>();
        if (comparer == null && resolver.HashFloodingResistant)
        {
            comparer = HashFloodingResistantEqualityComparer.Get<TKey>();
        }
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, FrozenDictionary<TKey, TValue>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        buffer.WriteMapHeader(value.Count);

        foreach (var kv in value)
        {
            kf.Serialize(ref buffer, ref state, kv.Key);
            vf.Serialize(ref buffer, ref state, kv.Value);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref FrozenDictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();

        // stage in a Dictionary (bomb-guarded count) and freeze once
        var staging = new Dictionary<TKey, TValue>(count, comparer);

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            staging[k] = v;
        }
        // a duplicate key collapsed into one slot: reject (cheapest possible detection)
        if (staging.Count != count)
        {
            MessagePackSerializationException.ThrowDuplicateMapKey();
        }

        value = staging.ToFrozenDictionary(comparer);
        state.Exit();
    }
}

public sealed partial class FrozenDictionaryFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
    where TKey : notnull
{
    readonly IEqualityComparer<TKey>? comparer;

    public FrozenDictionaryFormatterFactory()
    {
        this.comparer = null;
    }

    public FrozenDictionaryFormatterFactory(IEqualityComparer<TKey>? comparer)
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
        if (type == typeof(FrozenDictionary<TKey, TValue>))
        {
            return new FrozenDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>(comparer);
        }
        return null;
    }
}

public sealed partial class FrozenSetFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, FrozenSet<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;
    IEqualityComparer<T>? comparer;

    public FrozenSetFormatter(IEqualityComparer<T>? comparer)
    {
        this.comparer = comparer;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
        if (comparer == null && resolver.HashFloodingResistant)
        {
            comparer = HashFloodingResistantEqualityComparer.Get<T>();
        }
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, FrozenSet<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref FrozenSet<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

#if NETSTANDARD2_0
        var staging = new HashSet<T>(comparer); // no capacity ctor on ns2.0
#else
        var staging = new HashSet<T>(count, comparer);
#endif

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            staging.Add(item);
        }

        value = staging.ToFrozenSet(comparer);
        state.Exit();
    }
}

public sealed partial class FrozenSetFormatterFactory<T> : MessagePackFormatterFactory
{
    readonly IEqualityComparer<T>? comparer;

    public FrozenSetFormatterFactory()
    {
        comparer = null;
    }

    public FrozenSetFormatterFactory(IEqualityComparer<T>? comparer)
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
        if (type == typeof(FrozenSet<T>))
        {
            return new FrozenSetFormatter<TWriteBuffer, TReadBuffer, T>(comparer);
        }
        return null;
    }
}
