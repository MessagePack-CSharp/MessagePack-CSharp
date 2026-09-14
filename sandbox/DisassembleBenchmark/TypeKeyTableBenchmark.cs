using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

// Type-keyed lookup tables under the serializer's actual constraints: keys are Types, entries are only ever added
// (rarely, so writes may lock), reads must be lock-free and are the hot path. Candidates:
//   ConcurrentHandle   - ConcurrentDictionary<RuntimeTypeHandle, T>, the product shape since 2026-09-08
//   HyperMapper        - the 2017 ThreadsafeTypeKeyHashTable: chained entries, Type.GetHashCode, reference equality,
//                        Volatile publish (neuecc/HyperMapper)
//   ChainPointer       - the same chain shape keyed by the method-table pointer (TypeHandle.Value), multiplicative
//                        hash, entry keeps the Type alive so the pointer cannot be reused under it
//   OpenAddressPointer - copy-on-write open addressing: one struct array of (pointer, Type, value), linear probing,
//                        every add publishes a new array so readers never see a slot in motion
// Two access patterns: the same type on every call (branch/cache friendly), and 16 types round-robin.
//
// MEASURED 2026-09-08 (MediumRun, ns): Same: ConcurrentHandle 1.03 | HyperMapper 1.29 | ChainPointer 0.11 |
// OpenAddressPointer 0.10 (the Same rows are throughput of independent iterations, not latency); Mixed:
// ConcurrentHandle 1.40 | HyperMapper 1.05 | ChainPointer 0.66 | OpenAddressPointer 0.62. The pointer key (no header
// hash read, no virtual GetHashCode) is the win, open addressing edges out the chain. OpenAddressPointer shipped as
// the core's TypeKeyHashTable and replaced ConcurrentDictionary<RuntimeTypeHandle,...> in all four hot caches.
//
// Review round (same day, Mixed, MediumRun): OpenAddressPointer 0.556 | PlainRead (no Volatile.Read) 0.551 | NoBounds
// (Unsafe.Add over GetArrayDataReference) 0.557 | SlimSlot (16-byte slot, Types in a side array) 0.599 +-0.06. All
// within noise on x64: the bounds check and the extra 8 bytes per slot are off the critical path. Applied only the
// plain read (correct per the memory model, saves ldar on ARM64); hash bit choice measured on 2552 real method
// tables: middle bits (>> 32 & mask) and top bits (>> (64 - log2 size)) give the same probe counts (mean 1.2-1.6).
//
// Review round 2 (2026-09-10, MediumRun, ns): TypeRef keys the slot on the Type reference (16-byte slot, pointer only hashed).
// Mixed: OpenAddressPointer 0.58 | TypeRef 0.66, but that harness lets the Type die right after the TypeHandle read in
// the pointer design while TypeRef must keep it live across the guarded-devirt fallback call (two extra callee-saved
// registers, the probe loop is identical). MixedLive, the product call shape (miss path consumes the Type):
// OpenAddressPointer 0.68 | SlimSlot 0.66 | TypeRef 0.66, same register set, within noise. TypeRef shipped: the nint key
// bought nothing the product call sites can use, and the slot loses 8 bytes plus the pointer-reuse argument.
public class TypeKeyTableBenchmark
{
    static readonly Type[] Types =
    [
        typeof(int), typeof(string), typeof(double), typeof(List<int>), typeof(Dictionary<string, int>), typeof(Guid),
        typeof(DateTime), typeof(byte[]), typeof(TypeKeyTableBenchmark), typeof(Payload), typeof(long), typeof(bool),
        typeof(decimal), typeof(int[]), typeof(string[]), typeof(object),
    ];

    sealed class Payload
    {
        public int Value;
    }

    readonly ConcurrentDictionary<RuntimeTypeHandle, Payload> concurrent = new();
    readonly HyperMapperTable<Payload> hyperMapper = new();
    readonly ChainPointerTable<Payload> chainPointer = new();
    readonly OpenAddressPointerTable<Payload> openAddress = new();
    readonly PlainReadTable<Payload> plainRead = new();
    readonly NoBoundsTable<Payload> noBounds = new();
    readonly SlimSlotTable<Payload> slimSlot = new();
    readonly TypeRefTable<Payload> typeRef = new();
    int cursor;

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < Types.Length; i++)
        {
            var payload = new Payload { Value = i + 1 };
            concurrent.TryAdd(Types[i].TypeHandle, payload);
            hyperMapper.TryAdd(Types[i], payload);
            chainPointer.TryAdd(Types[i], payload);
            openAddress.TryAdd(Types[i], payload);
            plainRead.TryAdd(Types[i], payload);
            noBounds.TryAdd(Types[i], payload);
            slimSlot.TryAdd(Types[i], payload);
            typeRef.TryAdd(Types[i], payload);
        }
        // every candidate answers every key identically
        for (var i = 0; i < Types.Length; i++)
        {
            var expected = i + 1;
            if (!concurrent.TryGetValue(Types[i].TypeHandle, out var a) || a.Value != expected) throw new InvalidOperationException("concurrent");
            if (!hyperMapper.TryGetValue(Types[i], out var b) || b.Value != expected) throw new InvalidOperationException("hyperMapper");
            if (!chainPointer.TryGetValue(Types[i], out var c) || c.Value != expected) throw new InvalidOperationException("chainPointer");
            if (!openAddress.TryGetValue(Types[i], out var d) || d.Value != expected) throw new InvalidOperationException("openAddress");
            if (!plainRead.TryGetValue(Types[i], out var e) || e.Value != expected) throw new InvalidOperationException("plainRead");
            if (!noBounds.TryGetValue(Types[i], out var f) || f.Value != expected) throw new InvalidOperationException("noBounds");
            if (!slimSlot.TryGetValue(Types[i], out var g) || g.Value != expected) throw new InvalidOperationException("slimSlot");
            if (!typeRef.TryGetValue(Types[i], out var h) || h.Value != expected) throw new InvalidOperationException("typeRef");
        }
        if (hyperMapper.TryGetValue(typeof(float), out _) || chainPointer.TryGetValue(typeof(float), out _) || openAddress.TryGetValue(typeof(float), out _) || typeRef.TryGetValue(typeof(float), out _)) throw new InvalidOperationException("miss");
    }

    Type Next() => Types[(cursor = (cursor + 1) & 15)];

    [Benchmark(Baseline = true)]
    public int Same_ConcurrentHandle() => concurrent.TryGetValue(typeof(Payload).TypeHandle, out var p) ? p.Value : 0;

    [Benchmark]
    public int Same_HyperMapper() => hyperMapper.TryGetValue(typeof(Payload), out var p) ? p.Value : 0;

    [Benchmark]
    public int Same_ChainPointer() => chainPointer.TryGetValue(typeof(Payload), out var p) ? p.Value : 0;

    [Benchmark]
    public int Same_OpenAddressPointer() => openAddress.TryGetValue(typeof(Payload), out var p) ? p.Value : 0;

    [Benchmark]
    public int Same_TypeRef() => typeRef.TryGetValue(typeof(Payload), out var p) ? p.Value : 0;

    [Benchmark]
    public int Mixed_ConcurrentHandle() => concurrent.TryGetValue(Next().TypeHandle, out var p) ? p.Value : 0;

    [Benchmark]
    public int Mixed_HyperMapper() => hyperMapper.TryGetValue(Next(), out var p) ? p.Value : 0;

    [Benchmark]
    public int Mixed_ChainPointer() => chainPointer.TryGetValue(Next(), out var p) ? p.Value : 0;

    [Benchmark]
    public int Mixed_OpenAddressPointer() => openAddress.TryGetValue(Next(), out var p) ? p.Value : 0;

    [Benchmark]
    public int Mixed_PlainRead() => plainRead.TryGetValue(Next(), out var p) ? p.Value : 0;

    [Benchmark]
    public int Mixed_NoBounds() => noBounds.TryGetValue(Next(), out var p) ? p.Value : 0;

    [Benchmark]
    public int Mixed_SlimSlot() => slimSlot.TryGetValue(Next(), out var p) ? p.Value : 0;

    [Benchmark]
    public int Mixed_TypeRef() => typeRef.TryGetValue(Next(), out var p) ? p.Value : 0;

    // product-shaped call site: the miss path consumes the Type (creates the entry), so the reference stays live across
    // the lookup in every design; this removes the harness-only advantage of a key that lets the Type die early
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Miss(Type type) => type.GetHashCode();

    [Benchmark]
    public int MixedLive_OpenAddressPointer() { var t = Next(); return openAddress.TryGetValue(t, out var p) ? p.Value : Miss(t); }

    [Benchmark]
    public int MixedLive_SlimSlot() { var t = Next(); return slimSlot.TryGetValue(t, out var p) ? p.Value : Miss(t); }

    [Benchmark]
    public int MixedLive_TypeRef() { var t = Next(); return typeRef.TryGetValue(t, out var p) ? p.Value : Miss(t); }
}

// review round: (1) plain read of the array reference instead of Volatile.Read (the memory model orders the
// data-dependent loads through a freshly read reference), (2) bounds check removed with a ref into the array,
// (3) a 16-byte slot with the pinning Type references kept in a side array
sealed class PlainReadTable<TValue> where TValue : class
{
    struct Slot { public nint Key; public Type? Type; public TValue? Value; }
    Slot[] slots = new Slot[32];
    int count;
    readonly object writerLock = new();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Index(nint key, int mask) => (int)((ulong)key * 0x9E3779B97F4A7C15UL >> 32) & mask;
    public bool TryGetValue(Type type, out TValue value)
    {
        var key = type.TypeHandle.Value;
        var table = slots;
        var mask = table.Length - 1;
        for (var i = Index(key, mask); ; i = (i + 1) & mask)
        {
            ref var slot = ref table[i];
            if (slot.Key == key) { value = slot.Value!; return true; }
            if (slot.Key == 0) { value = default!; return false; }
        }
    }
    public bool TryAdd(Type type, TValue value)
    {
        lock (writerLock)
        {
            if (TryGetValue(type, out _)) return false;
            var next = (count + 1) * 2 > slots.Length ? new Slot[slots.Length * 2] : new Slot[slots.Length];
            if (next.Length != slots.Length) { foreach (var s in slots) if (s.Key != 0) Insert(next, s); } else slots.AsSpan().CopyTo(next);
            Insert(next, new Slot { Key = type.TypeHandle.Value, Type = type, Value = value });
            count++;
            Volatile.Write(ref slots, next);
            return true;
        }
    }
    static void Insert(Slot[] table, Slot slot)
    {
        var mask = table.Length - 1;
        for (var i = Index(slot.Key, mask); ; i = (i + 1) & mask) if (table[i].Key == 0) { table[i] = slot; return; }
    }
}

sealed class NoBoundsTable<TValue> where TValue : class
{
    struct Slot { public nint Key; public Type? Type; public TValue? Value; }
    Slot[] slots = new Slot[32];
    int count;
    readonly object writerLock = new();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Index(nint key, int mask) => (int)((ulong)key * 0x9E3779B97F4A7C15UL >> 32) & mask;
    public bool TryGetValue(Type type, out TValue value)
    {
        var key = type.TypeHandle.Value;
        var table = slots;
        var mask = table.Length - 1;
        ref var first = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(table);
        for (var i = Index(key, mask); ; i = (i + 1) & mask)
        {
            ref var slot = ref Unsafe.Add(ref first, i);
            if (slot.Key == key) { value = slot.Value!; return true; }
            if (slot.Key == 0) { value = default!; return false; }
        }
    }
    public bool TryAdd(Type type, TValue value)
    {
        lock (writerLock)
        {
            if (TryGetValue(type, out _)) return false;
            var next = (count + 1) * 2 > slots.Length ? new Slot[slots.Length * 2] : new Slot[slots.Length];
            if (next.Length != slots.Length) { foreach (var s in slots) if (s.Key != 0) Insert(next, s); } else slots.AsSpan().CopyTo(next);
            Insert(next, new Slot { Key = type.TypeHandle.Value, Type = type, Value = value });
            count++;
            Volatile.Write(ref slots, next);
            return true;
        }
    }
    static void Insert(Slot[] table, Slot slot)
    {
        var mask = table.Length - 1;
        for (var i = Index(slot.Key, mask); ; i = (i + 1) & mask) if (table[i].Key == 0) { table[i] = slot; return; }
    }
}

sealed class SlimSlotTable<TValue> where TValue : class
{
    struct Slot { public nint Key; public TValue? Value; }
    Slot[] slots = new Slot[32];
    Type?[] pinned = new Type?[32];
    int count;
    readonly object writerLock = new();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Index(nint key, int mask) => (int)((ulong)key * 0x9E3779B97F4A7C15UL >> 32) & mask;
    public bool TryGetValue(Type type, out TValue value)
    {
        var key = type.TypeHandle.Value;
        var table = slots;
        var mask = table.Length - 1;
        ref var first = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(table);
        for (var i = Index(key, mask); ; i = (i + 1) & mask)
        {
            ref var slot = ref Unsafe.Add(ref first, i);
            if (slot.Key == key) { value = slot.Value!; return true; }
            if (slot.Key == 0) { value = default!; return false; }
        }
    }
    public bool TryAdd(Type type, TValue value)
    {
        lock (writerLock)
        {
            if (TryGetValue(type, out _)) return false;
            var next = (count + 1) * 2 > slots.Length ? new Slot[slots.Length * 2] : new Slot[slots.Length];
            if (next.Length != slots.Length) { foreach (var s in slots) if (s.Key != 0) Insert(next, s); } else slots.AsSpan().CopyTo(next);
            Insert(next, new Slot { Key = type.TypeHandle.Value, Value = value });
            if (count == pinned.Length) Array.Resize(ref pinned, pinned.Length * 2);
            pinned[count] = type;
            count++;
            Volatile.Write(ref slots, next);
            return true;
        }
    }
    static void Insert(Slot[] table, Slot slot)
    {
        var mask = table.Length - 1;
        for (var i = Index(slot.Key, mask); ; i = (i + 1) & mask) if (table[i].Key == 0) { table[i] = slot; return; }
    }
}

// the 2017 shape, trimmed to what the benchmark needs (no factory overloads)
sealed class HyperMapperTable<TValue> where TValue : class
{
    sealed class Entry
    {
        public Type Key = default!;
        public TValue Value = default!;
        public int Hash;
        public Entry? Next;
    }

    Entry?[] buckets = new Entry?[16];
    int size;
    readonly object writerLock = new();

    public bool TryGetValue(Type key, out TValue value)
    {
        var table = buckets;
        var hash = key.GetHashCode();
        var entry = table[hash & (table.Length - 1)];
        while (entry != null)
        {
            if (entry.Key == key)
            {
                value = entry.Value;
                return true;
            }
            entry = entry.Next;
        }
        value = default!;
        return false;
    }

    public bool TryAdd(Type key, TValue value)
    {
        lock (writerLock)
        {
            if (TryGetValue(key, out _)) return false;
            if ((size + 1) * 4 > buckets.Length * 3)
            {
                var grown = new Entry?[buckets.Length * 2];
                foreach (var head in buckets)
                {
                    for (var e = head; e != null; e = e.Next)
                    {
                        Insert(grown, new Entry { Key = e.Key, Value = e.Value, Hash = e.Hash });
                    }
                }
                Insert(grown, new Entry { Key = key, Value = value, Hash = key.GetHashCode() });
                Volatile.Write(ref buckets, grown);
            }
            else
            {
                Insert(buckets, new Entry { Key = key, Value = value, Hash = key.GetHashCode() });
            }
            size++;
            return true;
        }
    }

    static void Insert(Entry?[] table, Entry entry)
    {
        ref var head = ref table[entry.Hash & (table.Length - 1)];
        if (head == null)
        {
            Volatile.Write(ref head, entry);
            return;
        }
        var last = head;
        while (last.Next != null) last = last.Next;
        Volatile.Write(ref last.Next, entry);
    }
}

// chain shape keyed by the method-table pointer; the entry's Type reference pins the type (and its loader allocator),
// so the pointer stays unique for as long as the entry exists
sealed class ChainPointerTable<TValue> where TValue : class
{
    sealed class Entry
    {
        public nint Key;
        public Type Type = default!;
        public TValue Value = default!;
        public Entry? Next;
    }

    Entry?[] buckets = new Entry?[16];
    int size;
    readonly object writerLock = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Index(nint key, int mask) => (int)((ulong)key * 0x9E3779B97F4A7C15UL >> 32) & mask;

    public bool TryGetValue(Type type, out TValue value)
    {
        var key = type.TypeHandle.Value;
        var table = buckets;
        var entry = table[Index(key, table.Length - 1)];
        while (entry != null)
        {
            if (entry.Key == key)
            {
                value = entry.Value;
                return true;
            }
            entry = entry.Next;
        }
        value = default!;
        return false;
    }

    public bool TryAdd(Type type, TValue value)
    {
        lock (writerLock)
        {
            if (TryGetValue(type, out _)) return false;
            var entry = new Entry { Key = type.TypeHandle.Value, Type = type, Value = value };
            if ((size + 1) * 4 > buckets.Length * 3)
            {
                var grown = new Entry?[buckets.Length * 2];
                foreach (var head in buckets)
                {
                    for (var e = head; e != null; e = e.Next)
                    {
                        Insert(grown, new Entry { Key = e.Key, Type = e.Type, Value = e.Value });
                    }
                }
                Insert(grown, entry);
                Volatile.Write(ref buckets, grown);
            }
            else
            {
                Insert(buckets, entry);
            }
            size++;
            return true;
        }
    }

    static void Insert(Entry?[] table, Entry entry)
    {
        ref var head = ref table[Index(entry.Key, table.Length - 1)];
        if (head == null)
        {
            Volatile.Write(ref head, entry);
            return;
        }
        var last = head;
        while (last.Next != null) last = last.Next;
        Volatile.Write(ref last.Next, entry);
    }
}

// copy-on-write open addressing over one struct array: a lookup is the array load, one index computation and a
// probe that usually ends on the first slot; adds are rare enough to clone the array every time
sealed class OpenAddressPointerTable<TValue> where TValue : class
{
    struct Slot
    {
        public nint Key;
        public Type? Type; // pins the type for the pointer's lifetime
        public TValue? Value;
    }

    Slot[] slots = new Slot[32];
    int count;
    readonly object writerLock = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Index(nint key, int mask) => (int)((ulong)key * 0x9E3779B97F4A7C15UL >> 32) & mask;

    public bool TryGetValue(Type type, out TValue value)
    {
        var key = type.TypeHandle.Value;
        var table = slots;
        var mask = table.Length - 1;
        for (var i = Index(key, mask); ; i = (i + 1) & mask)
        {
            ref var slot = ref table[i];
            if (slot.Key == key)
            {
                value = slot.Value!;
                return true;
            }
            if (slot.Key == 0)
            {
                value = default!;
                return false;
            }
        }
    }

    public bool TryAdd(Type type, TValue value)
    {
        lock (writerLock)
        {
            if (TryGetValue(type, out _)) return false;
            // stay under half full so probes stay short
            var next = (count + 1) * 2 > slots.Length ? new Slot[slots.Length * 2] : new Slot[slots.Length];
            if (next.Length != slots.Length)
            {
                foreach (var slot in slots)
                {
                    if (slot.Key != 0) Insert(next, slot);
                }
            }
            else
            {
                slots.AsSpan().CopyTo(next);
            }
            Insert(next, new Slot { Key = type.TypeHandle.Value, Type = type, Value = value });
            count++;
            Volatile.Write(ref slots, next);
            return true;
        }
    }

    static void Insert(Slot[] table, Slot slot)
    {
        var mask = table.Length - 1;
        for (var i = Index(slot.Key, mask); ; i = (i + 1) & mask)
        {
            if (table[i].Key == 0)
            {
                table[i] = slot;
                return;
            }
        }
    }
}

// review round 2 (2026-09-10): the slot drops the pointer and keys on the Type reference itself (16-byte slot, no side
// array). The hash still comes from TypeHandle.Value, the compare is ReferenceEquals (Type.operator== would add an
// `is RuntimeType` probe on the miss path), the empty sentinel is null, and adds are in place like the shipped table:
// Value first, Type last with a release write, readers acquire the Type.
sealed class TypeRefTable<TValue> where TValue : class
{
    struct Slot { public Type? Type; public TValue? Value; }
    Slot[] slots = new Slot[32];
    int count;
    readonly object writerLock = new();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Index(nint key, int mask) => (int)((ulong)key * 0x9E3779B97F4A7C15UL >> 32) & mask;
    public bool TryGetValue(Type type, out TValue value)
    {
        var key = type.TypeHandle.Value;
        var table = slots;
        var mask = table.Length - 1;
        for (var i = Index(key, mask); ; i = (i + 1) & mask)
        {
            ref var slot = ref table[i];
            var slotType = Volatile.Read(ref slot.Type);
            if (ReferenceEquals(slotType, type)) { value = slot.Value!; return true; }
            if (slotType is null) { value = default!; return false; }
        }
    }
    public bool TryAdd(Type type, TValue value)
    {
        lock (writerLock)
        {
            if (TryGetValue(type, out _)) return false;
            var current = slots;
            if ((count + 1) * 2 > current.Length)
            {
                var next = new Slot[current.Length * 2];
                foreach (var s in current) if (s.Type is not null) Insert(next, s.Type, s.Value!);
                Insert(next, type, value);
                count++;
                Volatile.Write(ref slots, next);
            }
            else
            {
                Insert(current, type, value);
                count++;
            }
            return true;
        }
    }
    static void Insert(Slot[] table, Type type, TValue value)
    {
        var mask = table.Length - 1;
        for (var i = Index(type.TypeHandle.Value, mask); ; i = (i + 1) & mask)
        {
            ref var slot = ref table[i];
            if (slot.Type is null)
            {
                slot.Value = value;
                Volatile.Write(ref slot.Type, type);
                return;
            }
        }
    }
}
