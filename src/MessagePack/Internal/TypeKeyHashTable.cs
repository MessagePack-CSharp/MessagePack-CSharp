using System.Diagnostics.CodeAnalysis;

namespace MessagePack;

// A thread-safe dictionary keyed by Type, used for NonGenerics -> Generics lookup.
// Because the usage conditions are limited, we can provide a custom implementation that is faster than ConcurrentDictionary<Type,>.
// There is no Remove, only Get and Add. Get is called frequently, and Add settles down after a certain amount.
// Under these conditions, a design where Get is lock-free and only Add takes a lock works well.
// The hash value is computed by applying Fibonacci hashing to the method table pointer held by the Type, and open addressing is used,
// so lookups are about 2 to 3 times faster than a normal ConcurrentDictionary.
// The slot compares the Type reference itself (a RuntimeType is unique per type, so reference identity is the equality),
// the pointer is only hashed and never stored: the entry keeps its Type alive, so the method table cannot move or be reused under it.
// Note that the safety of this technique is not a concern, since the same approach has been used inside the runtime in CoreCLR itself,
// for example in System.Runtime.CompilerServices.CastCache and System.Runtime.CompilerServices.GenericCache.
// COM types are out of scope.

sealed class TypeKeyHashTable<TValue> where TValue : class
{
    struct Entry
    {
        public Type? Type; // null marks an empty slot
        public TValue? Value;
    }

    Entry[] entries;
    int count;
    readonly object writerLock = new();

    public TypeKeyHashTable(int capacity = 32)
    {
        const int MaxCapacity = 1 << 29;

        if (capacity < 0 || capacity > MaxCapacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                $"Capacity must be between 0 and {MaxCapacity}.");
        }

        var requiredSize = capacity * 2;
        var size = 8;
        while (size < requiredSize)
        {
            size <<= 1;
        }

        entries = new Entry[size];
    }

    // Fibonacci hashing(0x9E3779B97F4A7C15UL is known magic number)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int GetIndex(nint key, int mask) => unchecked((int)(((ulong)key * 0x9E3779B97F4A7C15UL) >> 32)) & mask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(Type type, [NotNullWhen(true)] out TValue? value)
    {
        var key = type.TypeHandle.Value;

        // the array reference itself needs no Volatile.Read: the writer publishes a grown array with a release write, and
        // the .NET memory model orders the reads that depend on a freshly loaded reference after that load.
        // The Type does: an add writes Value into a free slot of the live array and then release-writes the Type,
        // so a reader that sees the Type must not have read the Value earlier.
        var table = entries;
        var mask = table.Length - 1;
        for (var i = GetIndex(key, mask); ; i = (i + 1) & mask)
        {
            ref var slot = ref table[i];
            var slotType = Volatile.Read(ref slot.Type);
            if (ReferenceEquals(slotType, type)) // ReferenceEquals rather than Type.operator==: the operator's miss path adds an `is RuntimeType` probe.
            {
                value = slot.Value!;
                return true;
            }
            if (slotType is null)
            {
                value = null;
                return false;
            }
        }
    }

    /// <summary>Adds the entry unless the type already has one.</summary>
    public bool TryAdd(Type type, TValue value)
    {
        lock (writerLock)
        {
            if (TryGetValue(type, out _))
            {
                return false;
            }
            Publish(type, value);
            return true;
        }
    }

    /// <summary>Returns the entry the type ends up with: the existing one, or <paramref name="value"/> once added.</summary>
    public TValue GetOrAdd(Type type, TValue value)
    {
        lock (writerLock)
        {
            if (TryGetValue(type, out var existing))
            {
                return existing;
            }
            Publish(type, value);
            return value;
        }
    }

    // under the writer lock: the entry goes into a free slot of the live array (the filled slot's neighbour stays empty
    // under the half-full rule, so other keys' probes still terminate), or into a grown array that is published whole
    void Publish(Type type, TValue value)
    {
        var current = entries;
        if ((count + 1) * 2 > current.Length) // Load factor > 0.5, grow
        {
            var next = current.Length < (1 << 30)
                ? new Entry[current.Length * 2]
                : throw new InvalidOperationException("Maximum capacity reached.");
            foreach (var entry in current)
            {
                if (entry.Type is not null)
                {
                    Insert(next, entry.Type, entry.Value!);
                }
            }
            Insert(next, type, value);
            count++;
            Volatile.Write(ref entries, next);
        }
        else
        {
            Insert(current, type, value);
            count++;
        }
    }

    static void Insert(Entry[] table, Type type, TValue value)
    {
        var mask = table.Length - 1;
        for (var i = GetIndex(type.TypeHandle.Value, mask); ; i = (i + 1) & mask)
        {
            ref var slot = ref table[i];
            if (slot.Type is null)
            {
                // Value first, Type last with a release write: a reader that acquires the Type sees a complete entry
                slot.Value = value;
                Volatile.Write(ref slot.Type, type);
                return;
            }
        }
    }
}
