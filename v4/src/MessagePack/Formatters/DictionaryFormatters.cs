using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace MessagePack.Formatters;

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
    IEqualityComparer<TKey>? comparer;

    public DictionaryFormatter(IEqualityComparer<TKey>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Dictionary<TKey, TValue>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Dictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        // ReadMapHeader validates the claimed count against BytesRemaining.
        var count = buffer.ReadMapHeader();

        // Populate contract
        Dictionary<TKey, TValue> result;
        if (value != null)
        {
            // reuse keeps the instance's comparer
            result = value;
            result.Clear();
#if !NETSTANDARD2_0
            result.EnsureCapacity(count);
#endif
        }
        else
        {
            result = new Dictionary<TKey, TValue>(count, comparer);
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        // Duplicate map keys are rejected as data errors.
        // JSON RFC 8259: "names SHOULD be unique", MsgPack Spec: "keys SHOULD be unique"
        // so a duplicate is a bug or an attacker desynchronizing validators.
#if NET
        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(result, k, out var exists);
            if (exists)
            {
                MessagePackSerializationException.ThrowDuplicateMapKey();
            }
            slot = default!; // freshly added, so this only settles the nullable flow
            vf.Deserialize(ref buffer, ref state, ref slot);
        }
#else
        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            result[k] = v;
        }
        // a duplicate key collapsed into one slot: reject (cheapest possible detection)
        if (result.Count != count)
        {
            MessagePackSerializationException.ThrowDuplicateMapKey();
        }
#endif

        value = result;
        state.Exit();
    }
}

public sealed partial class DictionaryFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
    where TKey : notnull
{
    readonly IEqualityComparer<TKey>? comparer;

    public DictionaryFormatterFactory()
    {
        this.comparer = null;
    }

    public DictionaryFormatterFactory(IEqualityComparer<TKey>? comparer)
    {
        this.comparer = comparer;
    }

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
        if (type == typeof(Dictionary<TKey, TValue>))
        {
            return new DictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>(comparer);
        }
        return null;
    }
}

public sealed class InterfaceDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IDictionary<TKey, TValue>?>
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

    public InterfaceDictionaryFormatter(IEqualityComparer<TKey>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IDictionary<TKey, TValue>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IDictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();

        IDictionary<TKey, TValue> result;
        if (value != null && !value.IsReadOnly)
        {
            result = value; // reuse keeps the instance's comparer
            result.Clear();
        }
        else
        {
            result = new Dictionary<TKey, TValue>(count, comparer);
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            result[k] = v;
        }
        // a duplicate key collapsed into one slot: reject (cheapest possible detection)
        if (result.Count != count)
        {
            MessagePackSerializationException.ThrowDuplicateMapKey();
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class InterfaceDictionaryFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
    where TKey : notnull
{
    readonly IEqualityComparer<TKey>? comparer;

    public InterfaceDictionaryFormatterFactory()
    {
        this.comparer = null;
    }

    public InterfaceDictionaryFormatterFactory(IEqualityComparer<TKey>? comparer)
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
        if (type == typeof(IDictionary<TKey, TValue>))
        {
            return new InterfaceDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>(comparer);
        }
        return null;
    }
}

public sealed class InterfaceReadOnlyDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IReadOnlyDictionary<TKey, TValue>?>
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

    public InterfaceReadOnlyDictionaryFormatter(IEqualityComparer<TKey>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IReadOnlyDictionary<TKey, TValue>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IReadOnlyDictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();

        // the interface is read-only: always materialize fresh
        var result = new Dictionary<TKey, TValue>(count, comparer);

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            result[k] = v;
        }
        // a duplicate key collapsed into one slot: reject (cheapest possible detection)
        if (result.Count != count)
        {
            MessagePackSerializationException.ThrowDuplicateMapKey();
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class InterfaceReadOnlyDictionaryFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
    where TKey : notnull
{
    readonly IEqualityComparer<TKey>? comparer;

    public InterfaceReadOnlyDictionaryFormatterFactory()
    {
        this.comparer = null;
    }

    public InterfaceReadOnlyDictionaryFormatterFactory(IEqualityComparer<TKey>? comparer)
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
        if (type == typeof(IReadOnlyDictionary<TKey, TValue>))
        {
            return new InterfaceReadOnlyDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>(comparer);
        }
        return null;
    }
}

public sealed class ReadOnlyDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ReadOnlyDictionary<TKey, TValue>?>
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

    public ReadOnlyDictionaryFormatter(IEqualityComparer<TKey>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ReadOnlyDictionary<TKey, TValue>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ReadOnlyDictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();

        // the wrapper is immutable: always materialize a fresh inner dictionary
        var inner = new Dictionary<TKey, TValue>(count, comparer);

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            inner[k] = v;
        }
        // a duplicate key collapsed into one slot: reject (cheapest possible detection)
        if (inner.Count != count)
        {
            MessagePackSerializationException.ThrowDuplicateMapKey();
        }

        value = new ReadOnlyDictionary<TKey, TValue>(inner);
        state.Exit();
    }
}

public sealed partial class ReadOnlyDictionaryFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
    where TKey : notnull
{
    readonly IEqualityComparer<TKey>? comparer;

    public ReadOnlyDictionaryFormatterFactory()
    {
        this.comparer = null;
    }

    public ReadOnlyDictionaryFormatterFactory(IEqualityComparer<TKey>? comparer)
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
        if (type == typeof(ReadOnlyDictionary<TKey, TValue>))
        {
            return new ReadOnlyDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>(comparer);
        }
        return null;
    }
}

// SortedList / SortedDictionary are comparison-based (red-black tree / sorted arrays):
// hash flooding does not apply, so there is no comparer injection here.
public sealed class SortedListFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, SortedList<TKey, TValue>?>
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, SortedList<TKey, TValue>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref SortedList<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();

        SortedList<TKey, TValue> result;
        if (value != null)
        {
            result = value;
            result.Clear();
        }
        else
        {
            result = new SortedList<TKey, TValue>(count);
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            result[k] = v;
        }
        // a duplicate key collapsed into one slot: reject (cheapest possible detection)
        if (result.Count != count)
        {
            MessagePackSerializationException.ThrowDuplicateMapKey();
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class SortedListFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
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
        if (type == typeof(SortedList<TKey, TValue>))
        {
            return new SortedListFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>();
        }
        return null;
    }
}

public sealed class SortedDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, SortedDictionary<TKey, TValue>?>
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, SortedDictionary<TKey, TValue>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref SortedDictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();

        SortedDictionary<TKey, TValue> result;
        if (value != null)
        {
            result = value; // reuse keeps the instance's comparer
            result.Clear();
        }
        else
        {
            result = new SortedDictionary<TKey, TValue>();
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            result[k] = v;
        }
        // a duplicate key collapsed into one slot: reject (cheapest possible detection)
        if (result.Count != count)
        {
            MessagePackSerializationException.ThrowDuplicateMapKey();
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class SortedDictionaryFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
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
        if (type == typeof(SortedDictionary<TKey, TValue>))
        {
            return new SortedDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>();
        }
        return null;
    }
}

public sealed class ConcurrentDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ConcurrentDictionary<TKey, TValue>?>
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

    public ConcurrentDictionaryFormatter(IEqualityComparer<TKey>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ConcurrentDictionary<TKey, TValue>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        // Count and the enumeration are only consistent while nobody mutates the
        // dictionary concurrently — same contract as v3
        buffer.WriteMapHeader(value.Count);

        foreach (var kv in value)
        {
            kf.Serialize(ref buffer, ref state, kv.Key);
            vf.Serialize(ref buffer, ref state, kv.Value);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ConcurrentDictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();

        ConcurrentDictionary<TKey, TValue> result;
        if (value != null)
        {
            result = value; // reuse keeps the instance's comparer
            result.Clear();
        }
        else
        {
            // .NET Framework's (IEqualityComparer) ctor throws on null — branch instead
            result = comparer == null
                ? new ConcurrentDictionary<TKey, TValue>()
                : new ConcurrentDictionary<TKey, TValue>(comparer);
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            result[k] = v;
        }
        // a duplicate key collapsed into one slot: reject (cheapest possible detection)
        if (result.Count != count)
        {
            MessagePackSerializationException.ThrowDuplicateMapKey();
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class ConcurrentDictionaryFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
    where TKey : notnull
{
    readonly IEqualityComparer<TKey>? comparer;

    public ConcurrentDictionaryFormatterFactory()
    {
        this.comparer = null;
    }

    public ConcurrentDictionaryFormatterFactory(IEqualityComparer<TKey>? comparer)
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
        if (type == typeof(ConcurrentDictionary<TKey, TValue>))
        {
            return new ConcurrentDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>(comparer);
        }
        return null;
    }
}

#if NET9_0_OR_GREATER

public sealed class OrderedDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, OrderedDictionary<TKey, TValue>?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where TKey : notnull
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TKey> keyFormatter = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TValue> valueFormatter = null!;
    IEqualityComparer<TKey>? comparer;

    public OrderedDictionaryFormatter(IEqualityComparer<TKey>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, OrderedDictionary<TKey, TValue>? value)
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

        foreach (var kv in value) // enumeration order = insertion order, roundtrip-stable
        {
            kf.Serialize(ref buffer, ref state, kv.Key);
            vf.Serialize(ref buffer, ref state, kv.Value);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref OrderedDictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();

        OrderedDictionary<TKey, TValue> result;
        if (value != null)
        {
            result = value; // reuse keeps the instance's comparer
            result.Clear();
            result.EnsureCapacity(count);
        }
        else
        {
            result = new OrderedDictionary<TKey, TValue>(count, comparer);
        }

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            result[k] = v;
        }
        // a duplicate key collapsed into one slot: reject (cheapest possible detection)
        if (result.Count != count)
        {
            MessagePackSerializationException.ThrowDuplicateMapKey();
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class OrderedDictionaryFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
    where TKey : notnull
{
    readonly IEqualityComparer<TKey>? comparer;

    public OrderedDictionaryFormatterFactory()
    {
        this.comparer = null;
    }

    public OrderedDictionaryFormatterFactory(IEqualityComparer<TKey>? comparer)
    {
        this.comparer = comparer;
    }

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        if (type == typeof(OrderedDictionary<TKey, TValue>))
        {
            return new OrderedDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>(comparer);
        }
        return null;
    }
}

#endif

#if NET

// PriorityQueue is not a dictionary: elements may repeat, so the map format is out.
// Wire format is an array of [element, priority] pairs.
// UnorderedItems enumerates in internal heap order: the wire order is unspecified but the content round-trips.
// The priority ordering uses IComparer(comparison-based), so hash-flooding resistance does not apply.
public sealed class PriorityQueueFormatter<TWriteBuffer, TReadBuffer, TElement, TPriority> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, PriorityQueue<TElement, TPriority>?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TElement> elementFormatter = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TPriority> priorityFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        elementFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TElement>();
        priorityFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TPriority>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, PriorityQueue<TElement, TPriority>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var ef = elementFormatter;
        var pf = priorityFormatter;
        state.Enter();

        var items = value.UnorderedItems;
        buffer.WriteArrayHeader(items.Count);

        foreach (var (element, priority) in items)
        {
            buffer.WriteArrayHeader(2);
            ef.Serialize(ref buffer, ref state, element);
            pf.Serialize(ref buffer, ref state, priority);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref PriorityQueue<TElement, TPriority>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        PriorityQueue<TElement, TPriority> result;
        if (value != null)
        {
            result = value; // reuse keeps the instance's comparer
            result.Clear();
            result.EnsureCapacity(count);
        }
        else
        {
            result = new PriorityQueue<TElement, TPriority>(count);
        }

        var ef = elementFormatter;
        var pf = priorityFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            var pairLength = buffer.ReadArrayHeader();
            if (pairLength != 2)
            {
                throw new MessagePackSerializationException($"Invalid PriorityQueue entry: expected [element, priority] pair (fixarray2), got array of length {pairLength}.");
            }

            TElement element = default!;
            TPriority priority = default!;
            ef.Deserialize(ref buffer, ref state, ref element);
            pf.Deserialize(ref buffer, ref state, ref priority);

            // Deserialize enqueues per item, NOT v3's temp-buffer + ctor bulk heapify
            result.Enqueue(element, priority);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class PriorityQueueFormatterFactory<TElement, TPriority> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(PriorityQueue<TElement, TPriority>))
        {
            return new PriorityQueueFormatter<TWriteBuffer, TReadBuffer, TElement, TPriority>();
        }
        return null;
    }
}

#endif