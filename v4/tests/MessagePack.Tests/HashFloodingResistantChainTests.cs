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
        new MessagePackFormatterResolver(MessagePackFormatterFactory.Default, hashFloodingResistant: false));

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
    public void OptionsExposeResolverPosture()
    {
        Assert.True(MessagePackSerializerOptions.Default.HashFloodingResistant); // secure by default
        Assert.False(trusted.HashFloodingResistant);
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
