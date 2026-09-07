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
// Measured for a reference type (BenchPerson, __Canon shared entry) and a boxed int.
//
// Concluded (2026-09-03, MediumRun): the dictionary lookup IS the overhead (~3.5ns over
// Serialize<T>; Poco 24.5 vs 20.9ns), ValidateValue is ~0.7ns of it. LastHit recovers
// almost all of it (21.6ns) and the always-miss alternating rows cost nothing extra
// (Alt_* 21.4 vs 21.5ns). REJECTED anyway as a benchmark artifact: the real non-generic
// demand is runtime-type dispatch where the type changes per call, and a static `last`
// written on every miss becomes a truly-shared cache line across server threads, costing
// far more than the 3ns it recovers. ConcurrentDictionary-per-call stays the product shape.
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
        })
        {
            if (!actual.AsSpan().SequenceEqual(expected)) throw new InvalidOperationException($"verify failed: {name}");
        }
        var expectedInt = MessagePackSerializer.Serialize(12345);
        foreach (var (name, actual) in new (string, byte[])[]
        {
            (nameof(Int_Current), Int_Current()),
            (nameof(Int_LastHitIsT), Int_LastHitIsT()),
        })
        {
            if (!actual.AsSpan().SequenceEqual(expectedInt)) throw new InvalidOperationException($"verify failed: {name}");
        }
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
    public byte[] Int_Generic() => MessagePackSerializer.Serialize(12345, options);

    [Benchmark]
    public byte[] Int_Current() => MessagePackSerializer.Serialize(typeof(int), intObj, options);

    [Benchmark]
    public byte[] Int_LastHitIsT() => LastHitIsTBridge.Serialize(typeof(int), intObj, options);

    bool toggle;

    [Benchmark]
    public byte[] Alt_Current() => (toggle = !toggle)
        ? MessagePackSerializer.Serialize(typeof(BenchPerson), personObj, options)
        : MessagePackSerializer.Serialize(typeof(int), intObj, options);

    [Benchmark]
    public byte[] Alt_LastHitIsT() => (toggle = !toggle)
        ? LastHitIsTBridge.Serialize(typeof(BenchPerson), personObj, options)
        : LastHitIsTBridge.Serialize(typeof(int), intObj, options);
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
