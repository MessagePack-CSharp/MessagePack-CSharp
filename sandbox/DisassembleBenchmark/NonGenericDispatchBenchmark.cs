using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MessagePack;

// Non-generic entry dispatch cost: how much does Serialize(Type, object) add over Serialize<T>?
// Candidates reproduce the bridge shape of MessagePackSerializer.NonGeneric.cs in the sandbox:
//   Current        - ConcurrentDictionary<Type,Entry> + ValidateValue (IsValueType/IsInstanceOfType) + virtual
//   LastHit        - single-entry (Type, Entry) pair checked by reference before the dictionary
//   IsTValidate    - validation moved into the entry as `value is T t` (one JIT type check, no RuntimeType calls)
//   LastHitIsT     - both
//   RefEq          - Current's dictionary with ReferenceEqualityComparer.Instance (RuntimeType is reference-identity
//                    anyway; the default comparer goes through the virtual Type.Equals/GetHashCode pair)
//   Lookup_*       - the dictionary TryGetValue alone, default comparer vs ReferenceEqualityComparer
// Measured for a reference type (BenchPerson, __Canon shared entry) and a boxed int.
//
// Concluded (2026-09-03, MediumRun): the dictionary lookup IS the overhead (~3.5ns over
// Serialize<T>; Poco 24.5 vs 20.9ns), ValidateValue is ~0.7ns of it. LastHit recovers
// almost all of it (21.6ns) and the always-miss alternating rows cost nothing extra
// (Alt_* 21.4 vs 21.5ns). REJECTED anyway as a benchmark artifact: the real non-generic
// demand is runtime-type dispatch where the type changes per call, and a static `last`
// written on every miss becomes a truly-shared cache line across server threads, costing
// far more than the 3ns it recovers. ConcurrentDictionary-per-call stays the product shape.
//
// RefEq round (2026-09-08, MediumRun, lookup-only rows): ReferenceEqualityComparer is NOT a free win for Type keys,
// the direction depends on the container. ConcurrentDictionary: default 2.44ns vs RefEq 2.94ns (slower; TryGetValue
// stays a shared __Canon callee where the default comparer gets devirtualized down to one virtual GetHashCode, the
// custom comparer goes through interface stubs). Plain Dictionary: default 2.44ns vs RefEq 1.92ns (faster; FindValue
// inlines into the caller and RefEq drops the ObjectEqualityComparer -> virtual Type.Equals hop). End-to-end rows
// (Poco_RefEq/Int_RefEq/Alt_RefEq) are within noise of Current either way. Product shape unchanged.
//
// Key-type round (same day, lookup-only, MediumRun): value-type keys take the containers' comparer-null fast path.
//   ConcurrentDictionary: Type 2.54 | RuntimeTypeHandle 1.64 | IntPtr (TypeHandle.Value) 1.08 ns
//   Dictionary:           Type 2.47 | RuntimeTypeHandle 1.87 | IntPtr 1.40 ns
// RuntimeTypeHandle keeps the RuntimeType reference (hash = object header hash, equality = reference), so it is as
// safe as a Type key; the raw pointer is faster still (arithmetic hash) but its identity is only safe while the
// cached value pins the type (collectible ALC reuse otherwise). Type -> handle conversion is included in the rows.
// APPLIED 2026-09-08: the product's hot Type-keyed caches (NonGeneric entries, ObjectFallback dispatchers, Typeless
// serializers, ReflectionUnion case slots) now key on RuntimeTypeHandle; the cold SourceGenerated registry keeps Type.
// *_Current rows below therefore measure the handle-keyed product shape from this date on.
// TypeKeyHashTable round (same day, MediumRun): the hot caches moved from ConcurrentDictionary<RuntimeTypeHandle> to the
// core's pointer-keyed open-addressing TypeKeyHashTable (see TypeKeyTableBenchmark). End to end: Poco_Current 21.67 vs
// Poco_Generic 19.63 (overhead 2.0 ns, was 3.5 with the Type key), Int_Current 14.00 vs Int_Generic 12.84.
public class NonGenericDispatchBenchmark
{
    BenchPerson person = default!;
    object personObj = default!;
    object intObj = 12345;
    MessagePackSerializerOptions options = default!;

    [GlobalSetup]
    public void Setup()
    {
        SourceGeneratedFormatterFactory.Instance.Register(typeof(BenchPerson), new BenchPersonFormatterFactory());
        person = new BenchPerson { Id = 12345, Name = "山岡士郎", Score = 98.5 };
        personObj = person;
        options = MessagePackSerializerOptions.Default;

        var expected = MessagePackSerializer.Serialize(person);
        foreach (var (name, actual) in new (string, byte[])[]
        {
            (nameof(Poco_Current), Poco_Current()),
            (nameof(Poco_LastHit), Poco_LastHit()),
            (nameof(Poco_IsTValidate), Poco_IsTValidate()),
            (nameof(Poco_LastHitIsT), Poco_LastHitIsT()),
            (nameof(Poco_RefEq), Poco_RefEq()),
        })
        {
            if (!actual.AsSpan().SequenceEqual(expected)) throw new InvalidOperationException($"verify failed: {name}");
        }
        var expectedInt = MessagePackSerializer.Serialize(12345);
        foreach (var (name, actual) in new (string, byte[])[]
        {
            (nameof(Int_Current), Int_Current()),
            (nameof(Int_LastHitIsT), Int_LastHitIsT()),
            (nameof(Int_RefEq), Int_RefEq()),
        })
        {
            if (!actual.AsSpan().SequenceEqual(expectedInt)) throw new InvalidOperationException($"verify failed: {name}");
        }
        if (Lookup_Default() is null || Lookup_RefEq() is null) throw new InvalidOperationException("verify failed: lookup");
    }

    [Benchmark(Baseline = true)]
    public byte[] Poco_Generic() => MessagePackSerializer.Serialize(person, options);

    [Benchmark]
    public byte[] Poco_Current() => MessagePackSerializer.Serialize(typeof(BenchPerson), personObj, options);

    [Benchmark]
    public byte[] Poco_LastHit() => LastHitBridge.Serialize(typeof(BenchPerson), personObj, options);

    [Benchmark]
    public byte[] Poco_IsTValidate() => IsTBridge.Serialize(typeof(BenchPerson), personObj, options);

    [Benchmark]
    public byte[] Poco_LastHitIsT() => LastHitIsTBridge.Serialize(typeof(BenchPerson), personObj, options);

    [Benchmark]
    public byte[] Poco_RefEq() => RefEqBridge.Serialize(typeof(BenchPerson), personObj, options);

    [Benchmark]
    public byte[] Int_Generic() => MessagePackSerializer.Serialize(12345, options);

    [Benchmark]
    public byte[] Int_Current() => MessagePackSerializer.Serialize(typeof(int), intObj, options);

    [Benchmark]
    public byte[] Int_LastHitIsT() => LastHitIsTBridge.Serialize(typeof(int), intObj, options);

    [Benchmark]
    public byte[] Int_RefEq() => RefEqBridge.Serialize(typeof(int), intObj, options);

    [Benchmark]
    public object? Lookup_Default() => RefEqBridge.LookupDefault(typeof(BenchPerson));

    [Benchmark]
    public object? Lookup_RefEq() => RefEqBridge.LookupRefEq(typeof(BenchPerson));

    [Benchmark]
    public object? Lookup_Dictionary_Default() => RefEqBridge.LookupDictionaryDefault(typeof(BenchPerson));

    [Benchmark]
    public object? Lookup_Dictionary_RefEq() => RefEqBridge.LookupDictionaryRefEq(typeof(BenchPerson));

    [Benchmark]
    public object? Lookup_Handle() => RefEqBridge.LookupHandle(typeof(BenchPerson));

    [Benchmark]
    public object? Lookup_Dictionary_Handle() => RefEqBridge.LookupDictionaryHandle(typeof(BenchPerson));

    [Benchmark]
    public object? Lookup_IntPtr() => RefEqBridge.LookupIntPtr(typeof(BenchPerson));

    [Benchmark]
    public object? Lookup_Dictionary_IntPtr() => RefEqBridge.LookupDictionaryIntPtr(typeof(BenchPerson));

    bool toggle;

    [Benchmark]
    public byte[] Alt_Current() => (toggle = !toggle)
        ? MessagePackSerializer.Serialize(typeof(BenchPerson), personObj, options)
        : MessagePackSerializer.Serialize(typeof(int), intObj, options);

    [Benchmark]
    public byte[] Alt_LastHitIsT() => (toggle = !toggle)
        ? LastHitIsTBridge.Serialize(typeof(BenchPerson), personObj, options)
        : LastHitIsTBridge.Serialize(typeof(int), intObj, options);

    [Benchmark]
    public byte[] Alt_RefEq() => (toggle = !toggle)
        ? RefEqBridge.Serialize(typeof(BenchPerson), personObj, options)
        : RefEqBridge.Serialize(typeof(int), intObj, options);
}

// Current's exact shape (ConcurrentDictionary + ValidateValue + virtual entry), only the comparer differs
static class RefEqBridge
{
    static readonly ConcurrentDictionary<Type, ValidatedEntry> entries = new(ReferenceEqualityComparer.Instance);
    static readonly ConcurrentDictionary<Type, ValidatedEntry> defaultEntries = new();

    public static byte[] Serialize(Type type, object? value, MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!entries.TryGetValue(type, out var entry)) entry = Create(entries, type);
        ValidateHelper.ValidateValue(type, value);
        return entry.Serialize(value, options);
    }

    // the plain Dictionary twins: the pattern most Type-keyed caches in the wild use (under a lock or read-only)
    static readonly Dictionary<Type, ValidatedEntry> plainDefault = new() { [typeof(BenchPerson)] = new ValidatedEntry<BenchPerson>(), [typeof(int)] = new ValidatedEntry<int>() };
    static readonly Dictionary<Type, ValidatedEntry> plainRefEq = new(ReferenceEqualityComparer.Instance) { [typeof(BenchPerson)] = new ValidatedEntry<BenchPerson>(), [typeof(int)] = new ValidatedEntry<int>() };

    public static ValidatedEntry? LookupDictionaryDefault(Type type) => plainDefault.TryGetValue(type, out var entry) ? entry : null;

    public static ValidatedEntry? LookupDictionaryRefEq(Type type) => plainRefEq.TryGetValue(type, out var entry) ? entry : null;

    // value-type keys: RuntimeTypeHandle (hash delegates to the RuntimeType object hash, equality is reference) and the
    // raw method-table pointer (hash is the pointer itself). Both take the comparer-null fast path of the containers.
    static readonly ConcurrentDictionary<RuntimeTypeHandle, ValidatedEntry> handleConcurrent = new() { [typeof(BenchPerson).TypeHandle] = new ValidatedEntry<BenchPerson>(), [typeof(int).TypeHandle] = new ValidatedEntry<int>() };
    static readonly Dictionary<RuntimeTypeHandle, ValidatedEntry> handlePlain = new() { [typeof(BenchPerson).TypeHandle] = new ValidatedEntry<BenchPerson>(), [typeof(int).TypeHandle] = new ValidatedEntry<int>() };
    static readonly ConcurrentDictionary<IntPtr, ValidatedEntry> pointerConcurrent = new() { [typeof(BenchPerson).TypeHandle.Value] = new ValidatedEntry<BenchPerson>(), [typeof(int).TypeHandle.Value] = new ValidatedEntry<int>() };
    static readonly Dictionary<IntPtr, ValidatedEntry> pointerPlain = new() { [typeof(BenchPerson).TypeHandle.Value] = new ValidatedEntry<BenchPerson>(), [typeof(int).TypeHandle.Value] = new ValidatedEntry<int>() };

    public static ValidatedEntry? LookupHandle(Type type) => handleConcurrent.TryGetValue(type.TypeHandle, out var entry) ? entry : null;

    public static ValidatedEntry? LookupDictionaryHandle(Type type) => handlePlain.TryGetValue(type.TypeHandle, out var entry) ? entry : null;

    public static ValidatedEntry? LookupIntPtr(Type type) => pointerConcurrent.TryGetValue(type.TypeHandle.Value, out var entry) ? entry : null;

    public static ValidatedEntry? LookupDictionaryIntPtr(Type type) => pointerPlain.TryGetValue(type.TypeHandle.Value, out var entry) ? entry : null;

    public static ValidatedEntry? LookupDefault(Type type)
    {
        if (!defaultEntries.TryGetValue(type, out var entry)) entry = Create(defaultEntries, type);
        return entry;
    }

    public static ValidatedEntry? LookupRefEq(Type type)
    {
        if (!entries.TryGetValue(type, out var entry)) entry = Create(entries, type);
        return entry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ValidatedEntry Create(ConcurrentDictionary<Type, ValidatedEntry> dictionary, Type type)
        => dictionary.GetOrAdd(type, (ValidatedEntry)Activator.CreateInstance(typeof(ValidatedEntry<>).MakeGenericType(type))!);
}

// ---- shared entry shapes ----

abstract class ValidatedEntry
{
    public abstract byte[] Serialize(object? value, MessagePackSerializerOptions options);
}

sealed class ValidatedEntry<T> : ValidatedEntry
{
    public override byte[] Serialize(object? value, MessagePackSerializerOptions options)
        => MessagePackSerializer.Serialize<T>((T)value!, options);
}

abstract class IsTEntry(Type type)
{
    public readonly Type Type = type;
    public abstract byte[] Serialize(object? value, MessagePackSerializerOptions options);
}

sealed class IsTEntry<T>() : IsTEntry(typeof(T))
{
    public override byte[] Serialize(object? value, MessagePackSerializerOptions options)
    {
        if (value is T t) return MessagePackSerializer.Serialize<T>(t, options);
        if (value is null && default(T) is null) return MessagePackSerializer.Serialize<T>(default!, options);
        return ThrowNotInstance(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte[] ThrowNotInstance(object? value)
    {
        if (value is null) throw new ArgumentException($"null is not a valid value for the non-nullable value type '{typeof(T)}'.", nameof(value));
        throw new ArgumentException($"The value of type '{value.GetType()}' is not an instance of the serialized type '{typeof(T)}'.", nameof(value));
    }
}

static class ValidateHelper
{
    public static void ValidateValue(Type type, object? value)
    {
        if (value is null)
        {
            if (type.IsValueType && Nullable.GetUnderlyingType(type) is null)
                throw new ArgumentException("null", nameof(value));
        }
        else if (!type.IsInstanceOfType(value))
        {
            throw new ArgumentException("not instance", nameof(value));
        }
    }
}

static class LastHitBridge
{
    sealed class Pair(Type type, ValidatedEntry entry) { public readonly Type Type = type; public readonly ValidatedEntry Entry = entry; }
    static readonly ConcurrentDictionary<Type, ValidatedEntry> entries = new();
    static Pair? last;

    public static byte[] Serialize(Type type, object? value, MessagePackSerializerOptions options)
    {
        var entry = Get(type);
        ValidateHelper.ValidateValue(type, value);
        return entry.Serialize(value, options);
    }

    static ValidatedEntry Get(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var p = last;
        if (p is not null && ReferenceEquals(p.Type, type)) return p.Entry;
        return GetSlow(type);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ValidatedEntry GetSlow(Type type)
    {
        if (!entries.TryGetValue(type, out var entry))
        {
            entry = entries.GetOrAdd(type, (ValidatedEntry)Activator.CreateInstance(typeof(ValidatedEntry<>).MakeGenericType(type))!);
        }
        last = new Pair(type, entry);
        return entry;
    }
}

static class IsTBridge
{
    static readonly ConcurrentDictionary<Type, IsTEntry> entries = new();

    public static byte[] Serialize(Type type, object? value, MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!entries.TryGetValue(type, out var entry)) entry = Create(type);
        return entry.Serialize(value, options);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IsTEntry Create(Type type)
        => entries.GetOrAdd(type, (IsTEntry)Activator.CreateInstance(typeof(IsTEntry<>).MakeGenericType(type))!);
}

static class LastHitIsTBridge
{
    static readonly ConcurrentDictionary<Type, IsTEntry> entries = new();
    static IsTEntry? last;

    public static byte[] Serialize(Type type, object? value, MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(type);
        var entry = last;
        if (entry is null || !ReferenceEquals(entry.Type, type)) entry = GetSlow(type);
        return entry.Serialize(value, options);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IsTEntry GetSlow(Type type)
    {
        if (!entries.TryGetValue(type, out var entry))
        {
            entry = entries.GetOrAdd(type, (IsTEntry)Activator.CreateInstance(typeof(IsTEntry<>).MakeGenericType(type))!);
        }
        last = entry;
        return entry;
    }
}
