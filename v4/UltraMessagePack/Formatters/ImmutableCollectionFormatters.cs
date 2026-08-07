// Immutable collections cannot be populated so Deserialize always materializes fresh (the incoming value is ignored).

using System.Collections.Immutable;

namespace UltraMessagePack.Formatters;

#region ImmutableArray, ImmutableList, ImmutableHashSet, ImmutableSortedSet, ImmutableQueue, ImmutableStack

public sealed partial class ImmutableArrayFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ImmutableArray<T>>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ImmutableArray<T> value)
    {
        // default(ImmutableArray<T>) round-trips as nil, matching v3
        if (value.IsDefault)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            f.Serialize(ref buffer, ref state, value[i]);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ImmutableArray<T> value)
    {
        if (buffer.TryReadNil())
        {
            value = default;
            return;
        }

        var count = buffer.ReadArrayHeader();

        if (count == 0)
        {
            value = ImmutableArray<T>.Empty;
            return;
        }

        var array = new T[count];

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            f.Deserialize(ref buffer, ref state, ref array[i]);
        }

#if NET
        value = ImmutableCollectionsMarshal.AsImmutableArray(array); // zero-copy: array never escapes
#else
        value = ImmutableArray.Create(array);
#endif
        state.Exit();
    }
}

public sealed partial class ImmutableArrayFormatterFactory<T> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(ImmutableArray<T>))
        {
            return new ImmutableArrayFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class ImmutableListFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ImmutableList<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ImmutableList<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ImmutableList<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();
        var builder = ImmutableList.CreateBuilder<T>();

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            builder.Add(item);
        }

        value = builder.ToImmutable();
        state.Exit();
    }
}

public sealed partial class ImmutableListFormatterFactory<T> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(ImmutableList<T>))
        {
            return new ImmutableListFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class ImmutableHashSetFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ImmutableHashSet<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;
    IEqualityComparer<T>? comparer;

    public ImmutableHashSetFormatter(IEqualityComparer<T>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ImmutableHashSet<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ImmutableHashSet<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();
        var builder = ImmutableHashSet.CreateBuilder<T>(comparer);

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            builder.Add(item);
        }

        value = builder.ToImmutable();
        state.Exit();
    }
}

public sealed partial class ImmutableHashSetFormatterFactory<T> : MessagePackFormatterFactory
{
    readonly IEqualityComparer<T>? comparer;

    public ImmutableHashSetFormatterFactory()
    {
        comparer = null;
    }

    public ImmutableHashSetFormatterFactory(IEqualityComparer<T>? comparer)
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
        if (type == typeof(ImmutableHashSet<T>))
        {
            return new ImmutableHashSetFormatter<TWriteBuffer, TReadBuffer, T>(comparer);
        }
        return null;
    }
}

public sealed partial class ImmutableSortedSetFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ImmutableSortedSet<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ImmutableSortedSet<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ImmutableSortedSet<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();
        var builder = ImmutableSortedSet.CreateBuilder<T>();

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            builder.Add(item);
        }

        value = builder.ToImmutable();
        state.Exit();
    }
}

public sealed partial class ImmutableSortedSetFormatterFactory<T> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(ImmutableSortedSet<T>))
        {
            return new ImmutableSortedSetFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class ImmutableQueueFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ImmutableQueue<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ImmutableQueue<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        // ImmutableQueue exposes no Count: first pass counts, second writes.
        buffer.WriteArrayHeader(value.Count());

        foreach (var item in value) // enumeration order = dequeue order
        {
            f.Serialize(ref buffer, ref state, item);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ImmutableQueue<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();
        var temp = new T[count];

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            f.Deserialize(ref buffer, ref state, ref temp[i]);
        }

        value = ImmutableQueue.CreateRange(temp); // enqueues in order: dequeue order = stream order
        state.Exit();
    }
}

public sealed partial class ImmutableQueueFormatterFactory<T> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(ImmutableQueue<T>))
        {
            return new ImmutableQueueFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class ImmutableStackFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ImmutableStack<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ImmutableStack<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        // ImmutableStack exposes no Count: first pass counts, second writes
        buffer.WriteArrayHeader(value.Count());

        foreach (var item in value) // enumeration order = top first (v3 wire order)
        {
            f.Serialize(ref buffer, ref state, item);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ImmutableStack<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        var f = formatter;
        state.Enter();

        // stream order is top-first, but a stack is rebuilt bottom-first: fill a temp
        // array REVERSED, then CreateRange pushes in temp order (see StackFormatter)
        var temp = new T[count];
        for (int i = 0; i < count; i++)
        {
            f.Deserialize(ref buffer, ref state, ref temp[count - 1 - i]);
        }

        value = ImmutableStack.CreateRange(temp); // temp[^1] (= stream head) ends on top
        state.Exit();
    }
}

public sealed partial class ImmutableStackFormatterFactory<T> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(ImmutableStack<T>))
        {
            return new ImmutableStackFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

#endregion

#region ImmutableDictionary, ImmutableSortedDictionary

public sealed class ImmutableDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ImmutableDictionary<TKey, TValue>?>
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

    public ImmutableDictionaryFormatter(IEqualityComparer<TKey>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ImmutableDictionary<TKey, TValue>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ImmutableDictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();
        var builder = comparer == null
            ? ImmutableDictionary.CreateBuilder<TKey, TValue>()
            : ImmutableDictionary.CreateBuilder<TKey, TValue>(comparer);

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            builder[k] = v; // last-win, matching DictionaryFormatter
        }

        value = builder.ToImmutable();
        state.Exit();
    }
}

public sealed partial class ImmutableDictionaryFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
    where TKey : notnull
{
    readonly IEqualityComparer<TKey>? comparer;

    public ImmutableDictionaryFormatterFactory()
    {
        this.comparer = null;
    }

    public ImmutableDictionaryFormatterFactory(IEqualityComparer<TKey>? comparer)
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
        if (type == typeof(ImmutableDictionary<TKey, TValue>))
        {
            return new ImmutableDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>(comparer);
        }
        return null;
    }
}

public sealed class ImmutableSortedDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ImmutableSortedDictionary<TKey, TValue>?>
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ImmutableSortedDictionary<TKey, TValue>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ImmutableSortedDictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();
        var builder = ImmutableSortedDictionary.CreateBuilder<TKey, TValue>();

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            builder[k] = v;
        }

        value = builder.ToImmutable();
        state.Exit();
    }
}

public sealed partial class ImmutableSortedDictionaryFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
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
        if (type == typeof(ImmutableSortedDictionary<TKey, TValue>))
        {
            return new ImmutableSortedDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>();
        }
        return null;
    }
}

#endregion

#region IImmutableList, IImmutableSet, IImmutableQueue, IImmutableStack, IImmutableDictionary

public sealed partial class InterfaceImmutableListFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IImmutableList<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IImmutableList<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IImmutableList<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();
        var builder = ImmutableList.CreateBuilder<T>();

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            builder.Add(item);
        }

        value = builder.ToImmutable();
        state.Exit();
    }
}

public sealed partial class InterfaceImmutableListFormatterFactory<T> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(IImmutableList<T>))
        {
            return new InterfaceImmutableListFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class InterfaceImmutableSetFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IImmutableSet<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;
    IEqualityComparer<T>? comparer;

    public InterfaceImmutableSetFormatter(IEqualityComparer<T>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IImmutableSet<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IImmutableSet<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();
        var builder = ImmutableHashSet.CreateBuilder<T>(comparer);

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            builder.Add(item);
        }

        value = builder.ToImmutable();
        state.Exit();
    }
}

public sealed partial class InterfaceImmutableSetFormatterFactory<T> : MessagePackFormatterFactory
{
    readonly IEqualityComparer<T>? comparer;

    public InterfaceImmutableSetFormatterFactory()
    {
        comparer = null;
    }

    public InterfaceImmutableSetFormatterFactory(IEqualityComparer<T>? comparer)
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
        if (type == typeof(IImmutableSet<T>))
        {
            return new InterfaceImmutableSetFormatter<TWriteBuffer, TReadBuffer, T>(comparer);
        }
        return null;
    }
}

public sealed partial class InterfaceImmutableQueueFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IImmutableQueue<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IImmutableQueue<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(value.Count());

        foreach (var item in value)
        {
            f.Serialize(ref buffer, ref state, item);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IImmutableQueue<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();
        var temp = new T[count];

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            f.Deserialize(ref buffer, ref state, ref temp[i]);
        }

        value = ImmutableQueue.CreateRange(temp);
        state.Exit();
    }
}

public sealed partial class InterfaceImmutableQueueFormatterFactory<T> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(IImmutableQueue<T>))
        {
            return new InterfaceImmutableQueueFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class InterfaceImmutableStackFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IImmutableStack<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IImmutableStack<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(value.Count());

        foreach (var item in value) // top first
        {
            f.Serialize(ref buffer, ref state, item);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IImmutableStack<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        var f = formatter;
        state.Enter();

        var temp = new T[count];
        for (int i = 0; i < count; i++)
        {
            f.Deserialize(ref buffer, ref state, ref temp[count - 1 - i]);
        }

        value = ImmutableStack.CreateRange(temp);
        state.Exit();
    }
}

public sealed partial class InterfaceImmutableStackFormatterFactory<T> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(IImmutableStack<T>))
        {
            return new InterfaceImmutableStackFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed class InterfaceImmutableDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IImmutableDictionary<TKey, TValue>?>
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

    public InterfaceImmutableDictionaryFormatter(IEqualityComparer<TKey>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IImmutableDictionary<TKey, TValue>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IImmutableDictionary<TKey, TValue>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();
        var builder = comparer == null
            ? ImmutableDictionary.CreateBuilder<TKey, TValue>()
            : ImmutableDictionary.CreateBuilder<TKey, TValue>(comparer);

        var kf = keyFormatter;
        var vf = valueFormatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            TKey k = default!;
            TValue v = default!;
            kf.Deserialize(ref buffer, ref state, ref k);
            vf.Deserialize(ref buffer, ref state, ref v);
            builder[k] = v;
        }

        value = builder.ToImmutable();
        state.Exit();
    }
}

public sealed partial class InterfaceImmutableDictionaryFormatterFactory<TKey, TValue> : MessagePackFormatterFactory
    where TKey : notnull
{
    readonly IEqualityComparer<TKey>? comparer;

    public InterfaceImmutableDictionaryFormatterFactory()
    {
        this.comparer = null;
    }

    public InterfaceImmutableDictionaryFormatterFactory(IEqualityComparer<TKey>? comparer)
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
        if (type == typeof(IImmutableDictionary<TKey, TValue>))
        {
            return new InterfaceImmutableDictionaryFormatter<TWriteBuffer, TReadBuffer, TKey, TValue>(comparer);
        }
        return null;
    }
}

#endregion
