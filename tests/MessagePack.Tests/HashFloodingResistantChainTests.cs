using MessagePack;
using Xunit;

namespace MessagePack.Tests;

// The secure-by-default wiring: hash-collection formatters pick up flooding-resistant
// comparers in Initialize when the resolver's HashFloodingResistant flag (default true)
// is set. Deserialized hash collections must come back with the flooding-resistant
// comparer injected (or the default comparer where the policy passes through); a
// resolver constructed with hashFloodingResistant: false keeps default comparers.
public class HashFloodingResistantChainTests
{
    static readonly MessagePackSerializerOptions trusted = new(
        new MessagePackFormatterResolver(MessagePackFormatterFactory.Default) { HashFloodingResistant = false });

    static T Roundtrip<T>(T value, MessagePackSerializerOptions? options = null)
    {
        var bytes = options == null
            ? MessagePackSerializer.Serialize(value)
            : MessagePackSerializer.Serialize(value, options);
        return (options == null
            ? MessagePackSerializer.Deserialize<T>(bytes)
            : MessagePackSerializer.Deserialize<T>(bytes, options))!;
    }

    [Fact]
    public void Default_InjectsFloodingResistantComparer_Dictionary()
    {
        var back = Roundtrip(new Dictionary<int, int> { [1] = 10, [2] = 20 });
        Assert.Same(HashFloodingResistantEqualityComparer.Get<int>(), back.Comparer);
        Assert.Equal(2, back.Count);
        Assert.Equal(10, back[1]);
        Assert.Equal(20, back[2]);
    }

    [Fact]
    public void Default_InjectsFloodingResistantComparer_EnumKey()
    {
        var back = Roundtrip(new Dictionary<DayOfWeek, int> { [DayOfWeek.Monday] = 1 });
        Assert.Same(HashFloodingResistantEqualityComparer.Get<DayOfWeek>(), back.Comparer);
        Assert.Equal(1, back[DayOfWeek.Monday]);
    }

    // Nullable<T> hashes as T does, so a nullable key was exactly as floodable as its underlying type while Get<T?>()
    // answered null and the collection kept its default comparer
    [Fact]
    public void Default_InjectsFloodingResistantComparer_NullableKeys()
    {
#pragma warning disable CS8714 // Dictionary<TKey,> asks for notnull; a nullable value type is a legal (warned) key
        var dict = Roundtrip(new Dictionary<long?, int> { [1] = 10, [2] = 20 });
        Assert.Same(HashFloodingResistantEqualityComparer.Get<long?>(), dict.Comparer);
        Assert.Equal(10, dict[1]);
        Assert.Equal(20, dict[2]);
#pragma warning restore CS8714

        var set = Roundtrip(new HashSet<int?> { 1, null });
        Assert.Same(HashFloodingResistantEqualityComparer.Get<int?>(), set.Comparer);
        Assert.Contains(1, set);
        Assert.Contains(null, set);

        // a nullable enum is not covered (no AOT-safe way to reach TEnum), so the set keeps its default comparer
        var enumSet = Roundtrip(new HashSet<DayOfWeek?> { DayOfWeek.Monday, null });
        Assert.Null(HashFloodingResistantEqualityComparer.Get<DayOfWeek?>());
        Assert.Same(EqualityComparer<DayOfWeek?>.Default, enumSet.Comparer);
        Assert.Contains(DayOfWeek.Monday, enumSet);
        Assert.Contains(null, enumSet);

        Assert.Null(HashFloodingResistantEqualityComparer.Get<KeyValuePair<int, int>?>()); // not a covered underlying type
    }

    // the common shape `= new()` member: deserializing into a model reused the instance and with it the default
    // comparer, so HashFloodingResistant never reached a dictionary or set that was initialized by its owner
    [MessagePackObject]
    public class PreinitializedCollections
    {
        [Key(0)] public Dictionary<long, int> Map { get; set; } = new();
        [Key(1)] public HashSet<int> Set { get; set; } = new();
        [Key(2)] public Dictionary<int, int> Custom { get; set; } = new(new NegatedIntComparer());
        [Key(3)] public IDictionary<long, int> Interface { get; set; } = new Dictionary<long, int>();
        [Key(4)] public ISet<int> InterfaceSet { get; set; } = new HashSet<int>();
        [Key(5)] public ICollection<long> Collection { get; set; } = new HashSet<long>();
        [Key(6)] public ICollection<long> ListCollection { get; set; } = new List<long>();
    }

    public sealed class NegatedIntComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;

        public int GetHashCode(int value) => -value;
    }

    [Fact]
    public void Default_ReplacesTheDefaultComparerOfAPreinitializedMember()
    {
        var value = new PreinitializedCollections { Map = { [1] = 10 }, Set = { 2 }, Custom = { [3] = 30 }, Interface = { [4] = 40 }, InterfaceSet = { 5 }, Collection = { 6 }, ListCollection = { 7 } };
        var back = Roundtrip(value);
        Assert.Same(HashFloodingResistantEqualityComparer.Get<long>(), Assert.IsType<HashSet<long>>(back.Collection).Comparer); // ICollection<T> over a HashSet keeps the shape
        Assert.Contains(6, back.Collection);
        Assert.Equal([7L], Assert.IsType<List<long>>(back.ListCollection));
        Assert.Same(HashFloodingResistantEqualityComparer.Get<long>(), back.Map.Comparer);
        Assert.Same(HashFloodingResistantEqualityComparer.Get<int>(), back.Set.Comparer);
        Assert.Same(HashFloodingResistantEqualityComparer.Get<long>(), ((Dictionary<long, int>)back.Interface).Comparer);
        Assert.Same(HashFloodingResistantEqualityComparer.Get<int>(), ((HashSet<int>)back.InterfaceSet).Comparer);
        Assert.IsType<NegatedIntComparer>(back.Custom.Comparer); // a caller-chosen comparer is kept
        Assert.Equal(10, back.Map[1]);
        Assert.Contains(2, back.Set);
        Assert.Equal(30, back.Custom[3]);
        Assert.Equal(40, back.Interface[4]);
        Assert.Contains(5, back.InterfaceSet);

        // a populate into an explicit instance replaces it the same way (the caller sees the new instance)
        var existing = new Dictionary<long, int> { [9] = 9 };
        var target = existing;
        MessagePackSerializer.Deserialize(MessagePackSerializer.Serialize(new Dictionary<long, int> { [1] = 1 }), ref target);
        Assert.NotSame(existing, target);
        Assert.Same(HashFloodingResistantEqualityComparer.Get<long>(), target.Comparer);
        Assert.Equal(1, target[1]);

        // nothing to protect under a trusted resolver: the instance (and its default comparer) is reused as before
        var trustedBack = Roundtrip(value, trusted);
        Assert.Same(EqualityComparer<long>.Default, trustedBack.Map.Comparer);
        Assert.Same(EqualityComparer<int>.Default, trustedBack.Set.Comparer);
    }

    // the rest of the built-in value types a key can plausibly be: each has a default hash an attacker can collide
    // (TimeSpan: ticks hi ^ lo, decimal: its scaled parts, ...)
    [Fact]
    public void OtherBuiltInKeyTypes_AreCovered()
    {
        var timeSpan = HashFloodingResistantEqualityComparer.Get<TimeSpan>()!;
        var hashes = new HashSet<int>();
        for (var i = 1; i < 1000; i++)
        {
            hashes.Add(timeSpan.GetHashCode(TimeSpan.FromTicks(i * 0x100000001L))); // all equal under TimeSpan.GetHashCode
        }
        Assert.True(hashes.Count > 900, $"{hashes.Count} distinct hashes for 999 default-colliding TimeSpan keys");
        Assert.Same(timeSpan, Roundtrip(new Dictionary<TimeSpan, int> { [TimeSpan.FromSeconds(1)] = 1 }).Comparer);

        var dec = HashFloodingResistantEqualityComparer.Get<decimal>()!;
        Assert.Equal(dec.GetHashCode(1m), dec.GetHashCode(1.0m));
        Assert.Equal(dec.GetHashCode(1m), dec.GetHashCode(1.000000000000000000m));
        Assert.Equal(dec.GetHashCode(0m), dec.GetHashCode(decimal.Negate(0m)));
        Assert.Equal(dec.GetHashCode(0m), dec.GetHashCode(0.000m));
        Assert.NotEqual(dec.GetHashCode(1m), dec.GetHashCode(2m));
        Assert.True(dec.Equals(1m, 1.00m));
        var set = Roundtrip(new HashSet<decimal> { 1.0m, 2m });
        Assert.Same(dec, set.Comparer);
        Assert.Contains(1m, set);

        // the object-keyed (Typeless) fallback dispatches on the runtime type and must cover the same set
        var objectComparer = HashFloodingResistantEqualityComparer.Get<object>()!;
        Assert.Equal(timeSpan.GetHashCode(TimeSpan.FromTicks(0x100000001L)), objectComparer.GetHashCode(TimeSpan.FromTicks(0x100000001L)));
        Assert.Equal(dec.GetHashCode(1m), objectComparer.GetHashCode(1.0m));
        var objectHashes = new HashSet<int>();
        for (var i = 1; i < 1000; i++)
        {
            objectHashes.Add(objectComparer.GetHashCode(TimeSpan.FromTicks(i * 0x100000001L)));
        }
        Assert.True(objectHashes.Count > 900, $"{objectHashes.Count} distinct hashes for 999 default-colliding boxed TimeSpan keys");

        Assert.NotNull(HashFloodingResistantEqualityComparer.Get<nint>());
        Assert.NotNull(HashFloodingResistantEqualityComparer.Get<nuint>());
        Assert.NotNull(HashFloodingResistantEqualityComparer.Get<TimeSpan?>());
        Assert.NotNull(HashFloodingResistantEqualityComparer.Get<decimal?>());
#if NET9_0_OR_GREATER
        var half = HashFloodingResistantEqualityComparer.Get<Half>()!;
        Assert.Equal(half.GetHashCode(Half.Zero), half.GetHashCode(Half.NegativeZero));
        Assert.Equal(half.GetHashCode(Half.NaN), half.GetHashCode(-Half.NaN));
        Assert.NotEqual(half.GetHashCode((Half)1), half.GetHashCode((Half)2));
        Assert.NotNull(HashFloodingResistantEqualityComparer.Get<DateOnly>());
        Assert.NotNull(HashFloodingResistantEqualityComparer.Get<TimeOnly>());
        Assert.NotNull(HashFloodingResistantEqualityComparer.Get<Int128>());
        Assert.NotNull(HashFloodingResistantEqualityComparer.Get<UInt128>());
        Assert.NotNull(HashFloodingResistantEqualityComparer.Get<Int128?>());
        Assert.Same(HashFloodingResistantEqualityComparer.Get<DateOnly>(), Roundtrip(new Dictionary<DateOnly, int> { [new DateOnly(2026, 1, 1)] = 1 }).Comparer);
#endif
    }

    [Fact]
    public void NullableComparer_DoesNotCollideLikeTheDefault()
    {
        // ((long)i << 32) | (uint)i: every value has the same long.GetHashCode (hi ^ lo == 0)
        var comparer = HashFloodingResistantEqualityComparer.Get<long?>()!;
        var hashes = new HashSet<int>();
        for (var i = 1; i < 1000; i++)
        {
            hashes.Add(comparer.GetHashCode(((long)i << 32) | (uint)i));
        }
        Assert.True(hashes.Count > 900, $"{hashes.Count} distinct hashes for 999 default-colliding keys");

        Assert.Equal(0, comparer.GetHashCode(null));
        Assert.True(comparer.Equals(null, null));
        Assert.False(comparer.Equals(null, 1));
        Assert.False(comparer.Equals(1, null));
        Assert.True(comparer.Equals(5, 5));
        Assert.False(comparer.Equals(5, 6));
        Assert.Equal(comparer.GetHashCode(5), HashFloodingResistantEqualityComparer.Get<long>()!.GetHashCode(5));
    }

    [Fact]
    public void Default_InjectsFloodingResistantComparer_Sets()
    {
        var set = Roundtrip(new HashSet<long> { 1, 2, 3 });
        Assert.Same(HashFloodingResistantEqualityComparer.Get<long>(), set.Comparer);
        Assert.Equal([1, 2, 3], set.Order());

        var iset = Roundtrip<ISet<int>>(new HashSet<int> { 4, 5 });
        Assert.Same(HashFloodingResistantEqualityComparer.Get<int>(), ((HashSet<int>)iset).Comparer);

        var roset = Roundtrip<IReadOnlySet<int>>(new HashSet<int> { 6 });
        Assert.Same(HashFloodingResistantEqualityComparer.Get<int>(), ((HashSet<int>)roset).Comparer);
    }

    // string on modern .NET passes through (Get<string>() is null), so the flooding tier
    // declines and the Generic tier builds the plain formatter — the BCL's own adaptive
    // randomization covers HashDoS there. Dictionary masks its internal non-randomized
    // comparer and reports EqualityComparer<string>.Default.
    [Fact]
    public void Default_StringKeys_FallThroughToDefaultComparer()
    {
        var back = Roundtrip(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
        Assert.Same(EqualityComparer<string>.Default, back.Comparer);
        Assert.Equal(1, back["a"]);
    }

    // unlisted key types (byte[] here — reference equality, unforgeable hash) decline too
    [Fact]
    public void Default_UnlistedKeys_FallThroughToDefaultComparer()
    {
        var back = Roundtrip(new Dictionary<byte[], int> { [new byte[] { 1 }] = 1 });
        Assert.Same(EqualityComparer<byte[]>.Default, back.Comparer);
        Assert.Single(back);
    }

    [Fact]
    public void ResolverExposesPosture()
    {
        Assert.True(MessagePackSerializerOptions.Default.Resolver.HashFloodingResistant); // secure by default
        Assert.False(trusted.Resolver.HashFloodingResistant);
    }

    [Fact]
    public void Trusted_UsesDefaultComparers()
    {
        var dict = Roundtrip(new Dictionary<int, int> { [1] = 10 }, trusted);
        Assert.Same(EqualityComparer<int>.Default, dict.Comparer);
        Assert.Equal(10, dict[1]);

        var set = Roundtrip(new HashSet<long> { 1, 2 }, trusted);
        Assert.Same(EqualityComparer<long>.Default, set.Comparer);
    }

    // both chains must produce identical wire bytes — the tier only changes the comparer
    // of the materialized collection, never the payload
    [Fact]
    public void DefaultAndTrusted_ProduceIdenticalPayloads()
    {
        var value = new Dictionary<int, string> { [1] = "a", [2] = "b" };
        Assert.Equal(
            MessagePackSerializer.Serialize(value, trusted),
            MessagePackSerializer.Serialize(value));
    }
}
