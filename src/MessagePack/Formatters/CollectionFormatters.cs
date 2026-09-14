// Contract for Populate on collections (Deserialize(ref T value))
// If elements can be accessed by indexer, populate them through the indexer
// If the collection supports Clear, call Clear first and then Add
// If Clear is not supported, populate the existing instance only when Count matches
// Do not add extra elements, behave the same as a fresh deserialization

// These formatters will be created by GenericFormatterFactory or Source Generated closed-generic formatter factory

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace MessagePack.Formatters;

#region Array, List, LinkedList, Queue, Stack, HashSet, ReadOnlyCollection, ObservableCollection, ReadOnlyObservableCollection

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

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            f.Serialize(ref buffer, ref state, value[i]);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        // ReadArrayHeader validates the claimed count against BytesRemaining.
        var count = buffer.ReadArrayHeader();

        // empty fast path
        if (count == 0)
        {
            value = [];
            return;
        }

        // Populate contract: reuse the incoming array only when the length matches exactly, otherwise allocate fresh.
        // A covariant incoming array (U[] behind T[]) is rejected by AsSpan below — spec'd, rare case.
        var result = (value != null && value.Length == count)
            ? value
            : new T[count]; // don't use GC.AllocateUninitializedArray<T>(count) because it would be a returning uninitialized memory via `ref span[i]`

        var f = formatter;
        state.Enter(); // enter depth-check

        // AsSpan pays the array covariance check once
        // a win only for generic T[], non-generic exact-typed loops should index directly
        var span = result.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            f.Deserialize(ref buffer, ref state, ref span[i]);
        }

        value = result;
        state.Exit(); // exit depth-check(does not need try-finally)
    }
}

public sealed partial class ArrayFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(T[]))
        {
            return new ArrayFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
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

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(value.Count);

#if NET9_0_OR_GREATER
        var span = CollectionsMarshal.AsSpan(value);
        for (int i = 0; i < span.Length; i++)
        {
            f.Serialize(ref buffer, ref state, span[i]);
        }
#else
        // no CollectionsMarshal downlevel: the indexer bounds check per element is the accepted correctness-tier cost
        var count = value.Count;
        for (int i = 0; i < count; i++)
        {
            f.Serialize(ref buffer, ref state, value[i]);
        }
#endif

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref List<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        // ReadArrayHeader validates the claimed count against BytesRemaining.
        var count = buffer.ReadArrayHeader();

        var f = formatter;
        state.Enter();

#if NET9_0_OR_GREATER
        var result = value ?? new List<T>(count);
        CollectionsMarshal.SetCount(result, count);
        var span = CollectionsMarshal.AsSpan(result);
        for (int i = 0; i < span.Length; i++)
        {
            f.Deserialize(ref buffer, ref state, ref span[i]);
        }
#else
        List<T> result;
        if (value == null)
        {
            result = new List<T>(count); // count is bomb-guarded by ReadArrayHeader
            for (int i = 0; i < count; i++)
            {
                T item = default!;
                f.Deserialize(ref buffer, ref state, ref item);
                result.Add(item);
            }
        }
        else
        {
            // populate semantics without SetCount/AsSpan: overwrite the reused prefix via the
            // indexer, Add the growth, truncate the excess
            result = value;
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
        }
#endif

        value = result;
        state.Exit();
    }
}

public sealed partial class ListFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(List<T>))
        {
            return new ListFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class LinkedListFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, LinkedList<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, LinkedList<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(value.Count);

        for (var node = value.First; node != null; node = node.Next)
        {
            f.Serialize(ref buffer, ref state, node.Value);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref LinkedList<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        LinkedList<T> result;
        if (value != null)
        {
            result = value;
            result.Clear();
        }
        else
        {
            result = new LinkedList<T>();
        }

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            result.AddLast(item);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class LinkedListFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(LinkedList<T>))
        {
            return new LinkedListFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class QueueFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Queue<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Queue<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(value.Count);

        foreach (var item in value) // enumeration order = dequeue order, roundtrip-stable
        {
            f.Serialize(ref buffer, ref state, item);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Queue<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        Queue<T> result;
        if (value != null)
        {
            result = value;
            result.Clear();
        }
        else
        {
            result = new Queue<T>(count);
        }

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            result.Enqueue(item);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class QueueFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(Queue<T>))
        {
            return new QueueFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class StackFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Stack<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Stack<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(value.Count);

        foreach (var item in value) // enumeration order = top first (v3 wire order)
        {
            f.Serialize(ref buffer, ref state, item);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Stack<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        var f = formatter;
        state.Enter();

        // stream order is top-first, but a stack is rebuilt bottom-first, so the whole
        // payload must be buffered: fill a temp array REVERSED, then push in temp order
        // Since this is a low priority issue and the performance benefit is limited unless the Stack is very large,
        // we will not use ArrayPool.
        var temp = new T[count];
        for (int i = 0; i < count; i++)
        {
            f.Deserialize(ref buffer, ref state, ref temp[count - 1 - i]);
        }

        Stack<T> result;
        if (value != null)
        {
            result = value;
            result.Clear();
            foreach (var item in temp)
            {
                result.Push(item);
            }
        }
        else
        {
            result = new Stack<T>(temp); // the IEnumerable ctor pushes in order: temp[^1] (= stream head) ends on top
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class StackFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(Stack<T>))
        {
            return new StackFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class HashSetFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, HashSet<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;
    IEqualityComparer<T>? comparer;

    public HashSetFormatter(IEqualityComparer<T>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, HashSet<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref HashSet<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        HashSet<T> result;
        if (value != null)
        {
            // reuse keeps the instance's comparer — a fresh set could only get the default one
            result = value;
            result.Clear();
        }
        else
        {
#if NETSTANDARD2_0
            result = new HashSet<T>(comparer); // no capacity ctor on ns2.0
#else
            result = new HashSet<T>(count, comparer);
#endif
        }

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            result.Add(item);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class HashSetFormatterFactory<T> : MessagePackFormatterFactory
{
    readonly IEqualityComparer<T>? comparer;

    public HashSetFormatterFactory()
    {
        comparer = null;
    }

    public HashSetFormatterFactory(IEqualityComparer<T>? comparer)
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
        if (type == typeof(HashSet<T>))
        {
            return new HashSetFormatter<TWriteBuffer, TReadBuffer, T>(comparer);
        }
        return null;
    }
}

public sealed partial class SortedSetFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, SortedSet<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;
    readonly IComparer<T>? comparer;

    public SortedSetFormatter(IComparer<T>? comparer)
    {
        this.comparer = comparer;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        // comparison-based (red-black tree), not hash-based: no flooding comparer here
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, SortedSet<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref SortedSet<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        SortedSet<T> result;
        if (value != null)
        {
            // reuse keeps the instance's comparer — a fresh set could only get the default one
            result = value;
            result.Clear();
        }
        else
        {
            result = new SortedSet<T>(comparer);
        }

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            result.Add(item);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class SortedSetFormatterFactory<T> : MessagePackFormatterFactory
{
    readonly IComparer<T>? comparer;

    public SortedSetFormatterFactory()
    {
        comparer = null;
    }

    public SortedSetFormatterFactory(IComparer<T>? comparer)
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
        if (type == typeof(SortedSet<T>))
        {
            return new SortedSetFormatter<TWriteBuffer, TReadBuffer, T>(comparer);
        }
        return null;
    }
}

#if NET9_0_OR_GREATER

public sealed partial class ReadOnlySetFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ReadOnlySet<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;
    IEqualityComparer<T>? comparer;

    public ReadOnlySetFormatter(IEqualityComparer<T>? comparer)
    {
        this.comparer = comparer;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
        if (comparer == null && resolver.HashFloodingResistant)
        {
            comparer = HashFloodingResistantEqualityComparer.Get<T>(); // the wrapper is hash-backed
        }
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ReadOnlySet<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ReadOnlySet<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        // no populate-reuse: the wrapper is read-only, so a fresh backing set every time
        var count = buffer.ReadArrayHeader();
        var set = new HashSet<T>(count, comparer);

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            set.Add(item);
        }

        value = new ReadOnlySet<T>(set);
        state.Exit();
    }
}

public sealed partial class ReadOnlySetFormatterFactory<T> : MessagePackFormatterFactory
{
    readonly IEqualityComparer<T>? comparer;

    public ReadOnlySetFormatterFactory()
    {
        comparer = null;
    }

    public ReadOnlySetFormatterFactory(IEqualityComparer<T>? comparer)
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
        if (type == typeof(ReadOnlySet<T>))
        {
            return new ReadOnlySetFormatter<TWriteBuffer, TReadBuffer, T>(comparer);
        }
        return null;
    }
}

#endif

public sealed partial class ReadOnlyCollectionFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ReadOnlyCollection<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ReadOnlyCollection<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        var count = value.Count;
        buffer.WriteArrayHeader(count);

        for (int i = 0; i < count; i++) // indexer loop: the wrapper's enumerator allocates
        {
            f.Serialize(ref buffer, ref state, value[i]);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ReadOnlyCollection<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        var f = formatter;
        state.Enter();

        // immutable wrapper: populate cannot reuse — fill a fresh array and wrap it once
        var array = new T[count];
        var span = array.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            f.Deserialize(ref buffer, ref state, ref span[i]);
        }

        value = new ReadOnlyCollection<T>(array);
        state.Exit();
    }
}

public sealed partial class ReadOnlyCollectionFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(ReadOnlyCollection<T>))
        {
            return new ReadOnlyCollectionFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class ObservableCollectionFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ObservableCollection<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ObservableCollection<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        var count = value.Count;
        buffer.WriteArrayHeader(count);

        for (int i = 0; i < count; i++)
        {
            f.Serialize(ref buffer, ref state, value[i]);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ObservableCollection<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        // populate reuse keeps the instance's event subscriptions alive — Clear and
        // per-Add change notifications firing IS the point of the type
        ObservableCollection<T> result;
        if (value != null)
        {
            result = value;
            result.Clear();
        }
        else
        {
            result = new ObservableCollection<T>();
        }

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            result.Add(item);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class ObservableCollectionFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(ObservableCollection<T>))
        {
            return new ObservableCollectionFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class ReadOnlyObservableCollectionFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ReadOnlyObservableCollection<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ReadOnlyObservableCollection<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        var count = value.Count;
        buffer.WriteArrayHeader(count);

        for (int i = 0; i < count; i++)
        {
            f.Serialize(ref buffer, ref state, value[i]);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ReadOnlyObservableCollection<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        var f = formatter;
        state.Enter();

        // immutable wrapper: build the inner collection, wrap once
        var inner = new ObservableCollection<T>();
        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            inner.Add(item);
        }

        value = new ReadOnlyObservableCollection<T>(inner);
        state.Exit();
    }
}

public sealed partial class ReadOnlyObservableCollectionFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(ReadOnlyObservableCollection<T>))
        {
            return new ReadOnlyObservableCollectionFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

#endregion

#region ArraySegment, Memory, ReadOnlyMemory

public sealed partial class ArraySegmentFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ArraySegment<T>>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ArraySegment<T> value)
    {
        if (value.Array == null) // default segment = nil (v3 wire behavior)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(value.Count);

        // plain indexing, no AsSpan: a covariant backing array must serialize (see ArrayFormatter)
        var array = value.Array;
        var end = value.Offset + value.Count;
        for (int i = value.Offset; i < end; i++)
        {
            f.Serialize(ref buffer, ref state, array[i]);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ArraySegment<T> value)
    {
        if (buffer.TryReadNil())
        {
            value = default;
            return;
        }

        var count = buffer.ReadArrayHeader();

        // Populate contract: overwrite the incoming view's backing store in place only on
        // an exact length match, otherwise a fresh zero-offset segment
        T[] array;
        int offset;
        if (value.Array != null && value.Count == count)
        {
            array = value.Array;
            offset = value.Offset;
        }
        else
        {
            array = new T[count];
            offset = 0;
        }

        var f = formatter;
        state.Enter();

        // AsSpan pays the covariance check once (throws on covariant reuse — spec'd, same as ArrayFormatter populate)
        var span = array.AsSpan(offset, count);
        for (int i = 0; i < span.Length; i++)
        {
            f.Deserialize(ref buffer, ref state, ref span[i]);
        }

        value = new ArraySegment<T>(array, offset, count);
        state.Exit();
    }
}

public sealed partial class ArraySegmentFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(ArraySegment<T>))
        {
            return new ArraySegmentFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class MemoryFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Memory<T>>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Memory<T> value)
    {
        // Memory has no null: default serializes as an empty array, never nil
        var f = formatter;
        state.Enter();

        var span = value.Span;
        buffer.WriteArrayHeader(span.Length);

        for (int i = 0; i < span.Length; i++)
        {
            f.Serialize(ref buffer, ref state, span[i]);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Memory<T> value)
    {
        if (buffer.TryReadNil()) // nil is accepted for symmetry with the nullable wrappers
        {
            value = default;
            return;
        }

        var count = buffer.ReadArrayHeader();

        // exact-length reuse writes through to the caller's backing store (array or
        // MemoryManager) — the ArrayFormatter populate rule applied to a view
        var result = value.Length == count ? value : new T[count];

        var f = formatter;
        state.Enter();

        var span = result.Span;
        for (int i = 0; i < span.Length; i++)
        {
            f.Deserialize(ref buffer, ref state, ref span[i]);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class MemoryFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(Memory<T>))
        {
            return new MemoryFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class ReadOnlyMemoryFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ReadOnlyMemory<T>>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ReadOnlyMemory<T> value)
    {
        var f = formatter;
        state.Enter();

        var span = value.Span;
        buffer.WriteArrayHeader(span.Length);

        for (int i = 0; i < span.Length; i++)
        {
            f.Serialize(ref buffer, ref state, span[i]);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ReadOnlyMemory<T> value)
    {
        if (buffer.TryReadNil())
        {
            value = default;
            return;
        }

        var count = buffer.ReadArrayHeader();

        var f = formatter;
        state.Enter();

        // read-only view: no write-through possible, always a fresh backing array
        var array = new T[count];
        var span = array.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            f.Deserialize(ref buffer, ref state, ref span[i]);
        }

        value = array;
        state.Exit();
    }
}

public sealed partial class ReadOnlyMemoryFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(ReadOnlyMemory<T>))
        {
            return new ReadOnlyMemoryFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

#endregion

#region ConcurrentQueue, ConcurrentStack, ConcurrentBag

public sealed partial class ConcurrentQueueFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ConcurrentQueue<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ConcurrentQueue<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ConcurrentQueue<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        var f = formatter;
        state.Enter();

        var result = new ConcurrentQueue<T>();
        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            result.Enqueue(item);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class ConcurrentQueueFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(ConcurrentQueue<T>))
        {
            return new ConcurrentQueueFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class ConcurrentStackFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ConcurrentStack<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ConcurrentStack<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(value.Count);

        foreach (var item in value) // enumeration order = top first, same as Stack<T>
        {
            f.Serialize(ref buffer, ref state, item);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ConcurrentStack<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        var f = formatter;
        state.Enter();

        // same reversed-temp shape as StackFormatter: the IEnumerable ctor pushes in
        // order, so temp[^1] (= stream head) ends on top
        var temp = new T[count];
        for (int i = 0; i < count; i++)
        {
            f.Deserialize(ref buffer, ref state, ref temp[count - 1 - i]);
        }

        value = new ConcurrentStack<T>(temp);
        state.Exit();
    }
}

public sealed partial class ConcurrentStackFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(ConcurrentStack<T>))
        {
            return new ConcurrentStackFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class ConcurrentBagFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ConcurrentBag<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ConcurrentBag<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(value.Count);

        foreach (var item in value) // a bag is unordered; enumeration order is whatever the snapshot yields
        {
            f.Serialize(ref buffer, ref state, item);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ConcurrentBag<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        var f = formatter;
        state.Enter();

        var result = new ConcurrentBag<T>();
        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            result.Add(item);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class ConcurrentBagFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(ConcurrentBag<T>))
        {
            return new ConcurrentBagFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

#endregion

#region IEnumerable<T>, ICollection<T>, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ISet<T>, IReadOnlySet<T> 

public sealed partial class InterfaceEnumerableFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IEnumerable<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IEnumerable<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        if (value.TryGetNonEnumeratedCount(out var count))
        {
            buffer.WriteArrayHeader(count);
            foreach (var item in value)
            {
                f.Serialize(ref buffer, ref state, item);
            }
        }
        else
        {
            // TODO: Optimize use of ArrayPool<T> or others
            var buffered = value.ToArray();
            buffer.WriteArrayHeader(buffered.Length);
            foreach (var item in buffered)
            {
                f.Serialize(ref buffer, ref state, item);
            }
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IEnumerable<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        if (count == 0)
        {
            value = Array.Empty<T>();
            return;
        }

        var f = formatter;
        state.Enter();

        var result = new T[count];
        var span = result.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            f.Deserialize(ref buffer, ref state, ref span[i]);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class InterfaceEnumerableFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(IEnumerable<T>))
        {
            return new InterfaceEnumerableFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class InterfaceCollectionFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ICollection<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ICollection<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ICollection<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        ICollection<T> result;
        if (value != null && !value.IsReadOnly)
        {
            result = value;
            result.Clear();
        }
        else
        {
            result = new List<T>(count); // count is bomb-guarded by ReadArrayHeader
        }

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            result.Add(item);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class InterfaceCollectionFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(ICollection<T>))
        {
            return new InterfaceCollectionFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class InterfaceListFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IList<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IList<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        var count = value.Count;
        buffer.WriteArrayHeader(count);

        for (int i = 0; i < count; i++) // indexer loop: no enumerator allocation
        {
            f.Serialize(ref buffer, ref state, value[i]);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IList<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        IList<T> result;
        if (value != null && !value.IsReadOnly)
        {
            result = value;
            result.Clear();
        }
        else
        {
            result = new List<T>(count);
        }

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            result.Add(item);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class InterfaceListFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(IList<T>))
        {
            return new InterfaceListFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class InterfaceReadOnlyCollectionFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IReadOnlyCollection<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IReadOnlyCollection<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IReadOnlyCollection<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        if (count == 0)
        {
            value = Array.Empty<T>();
            return;
        }

        var f = formatter;
        state.Enter();

        var result = new T[count];
        var span = result.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            f.Deserialize(ref buffer, ref state, ref span[i]);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class InterfaceReadOnlyCollectionFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(IReadOnlyCollection<T>))
        {
            return new InterfaceReadOnlyCollectionFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class InterfaceReadOnlyListFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IReadOnlyList<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IReadOnlyList<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();

        var count = value.Count;
        buffer.WriteArrayHeader(count);

        for (int i = 0; i < count; i++)
        {
            f.Serialize(ref buffer, ref state, value[i]);
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IReadOnlyList<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        if (count == 0)
        {
            value = Array.Empty<T>();
            return;
        }

        var f = formatter;
        state.Enter();

        var result = new T[count];
        var span = result.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            f.Deserialize(ref buffer, ref state, ref span[i]);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class InterfaceReadOnlyListFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(IReadOnlyList<T>))
        {
            return new InterfaceReadOnlyListFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class InterfaceSetFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ISet<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;
    IEqualityComparer<T>? comparer;

    public InterfaceSetFormatter(IEqualityComparer<T>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ISet<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ISet<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        ISet<T> result;
        if (value != null && !value.IsReadOnly)
        {
            result = value; // reuse keeps the instance's comparer
            result.Clear();
        }
        else
        {
#if NETSTANDARD2_0
            result = new HashSet<T>(comparer); // no capacity ctor on ns2.0
#else
            result = new HashSet<T>(count, comparer);
#endif
        }

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            result.Add(item);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class InterfaceSetFormatterFactory<T> : MessagePackFormatterFactory
{
    readonly IEqualityComparer<T>? comparer;

    public InterfaceSetFormatterFactory()
    {
        comparer = null;
    }

    public InterfaceSetFormatterFactory(IEqualityComparer<T>? comparer)
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
        if (type == typeof(ISet<T>))
        {
            return new InterfaceSetFormatter<TWriteBuffer, TReadBuffer, T>(comparer);
        }
        return null;
    }
}

#if NET9_0_OR_GREATER

public sealed partial class InterfaceReadOnlySetFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IReadOnlySet<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;
    IEqualityComparer<T>? comparer;

    public InterfaceReadOnlySetFormatter(IEqualityComparer<T>? comparer)
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IReadOnlySet<T>? value)
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IReadOnlySet<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        var f = formatter;
        state.Enter();

        var result = new HashSet<T>(count, comparer);
        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            result.Add(item);
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class InterfaceReadOnlySetFormatterFactory<T> : MessagePackFormatterFactory
{
    readonly IEqualityComparer<T>? comparer;

    public InterfaceReadOnlySetFormatterFactory()
    {
        comparer = null;
    }

    public InterfaceReadOnlySetFormatterFactory(IEqualityComparer<T>? comparer)
    {
        this.comparer = comparer;
    }

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        if (type == typeof(IReadOnlySet<T>))
        {
            return new InterfaceReadOnlySetFormatter<TWriteBuffer, TReadBuffer, T>(comparer);
        }
        return null;
    }
}

#endif

#endregion

#region ReadOnlySequence

public sealed partial class ReadOnlySequenceFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ReadOnlySequence<T>>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ReadOnlySequence<T> value)
    {
        var f = formatter;
        state.Enter();

        // Length is long; the msgpack array header (and every materializable payload) is int-bounded
        buffer.WriteArrayHeader(checked((int)value.Length));

        foreach (var segment in value)
        {
            var span = segment.Span;
            for (int i = 0; i < span.Length; i++)
            {
                f.Serialize(ref buffer, ref state, span[i]);
            }
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ReadOnlySequence<T> value)
    {
        if (buffer.TryReadNil())
        {
            value = default;
            return;
        }

        var count = buffer.ReadArrayHeader();

        if (count == 0)
        {
            value = default;
            return;
        }

        var array = new T[count];

        var f = formatter;
        state.Enter();

        for (int i = 0; i < count; i++)
        {
            f.Deserialize(ref buffer, ref state, ref array[i]);
        }

        value = new ReadOnlySequence<T>(array);
        state.Exit();
    }
}

public sealed partial class ReadOnlySequenceFormatterFactory<T> : MessagePackFormatterFactory
{
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
        if (type == typeof(ReadOnlySequence<T>))
        {
            return new ReadOnlySequenceFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

#endregion
