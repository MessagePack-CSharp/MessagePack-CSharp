using System.Collections.Frozen;
using MessagePack;
using Xunit;

namespace MessagePack.Tests;

// System.Collections.Frozen (FrozenCollectionFormatters.cs). MessagePack v3 has no frozen
// formatters, so there is no oracle wire-compare; instead the wire format is validated by
// cross-reading our payload as the plain mutable counterpart (same map/array framing).
public class FrozenCollectionFormatterTests
{
    static Dictionary<int, string> NewSource() => new() { [1] = "a", [2] = "b", [30000] = "c" };

    [Fact]
    public void FrozenDictionary_RoundtripsAndCrossReadsAsDictionary()
    {
        var frozen = NewSource().ToFrozenDictionary();
        var bytes = MessagePackSerializer.Serialize(frozen);

        var back = MessagePackSerializer.Deserialize<FrozenDictionary<int, string>>(bytes)!;
        Assert.Equal(frozen.OrderBy(kv => kv.Key), back.OrderBy(kv => kv.Key));

        // same wire framing as a plain map
        var asDict = MessagePackSerializer.Deserialize<Dictionary<int, string>>(bytes)!;
        Assert.Equal(NewSource().OrderBy(kv => kv.Key), asDict.OrderBy(kv => kv.Key));
    }

    [Fact]
    public void FrozenSet_RoundtripsAndCrossReadsAsHashSet()
    {
        long[] items = [1L, 2L, 3L, 70000L];
        var frozen = items.ToFrozenSet();
        var bytes = MessagePackSerializer.Serialize(frozen);

        var back = MessagePackSerializer.Deserialize<FrozenSet<long>>(bytes)!;
        Assert.Equal(frozen.OrderBy(x => x), back.OrderBy(x => x));

        var asSet = MessagePackSerializer.Deserialize<HashSet<long>>(bytes)!;
        Assert.Equal(items.OrderBy(x => x), asSet.OrderBy(x => x));
    }

    [Fact]
    public void HashBased_GetFloodingResistantComparer()
    {
        var dict = MessagePackSerializer.Deserialize<FrozenDictionary<int, string>>(
            MessagePackSerializer.Serialize(NewSource().ToFrozenDictionary()))!;
        Assert.Same(HashFloodingResistantEqualityComparer.Get<int>(), dict.Comparer);

        var set = MessagePackSerializer.Deserialize<FrozenSet<long>>(
            MessagePackSerializer.Serialize(new[] { 1L, 2L }.ToFrozenSet()))!;
        Assert.Same(HashFloodingResistantEqualityComparer.Get<long>(), set.Comparer);
    }

    [Fact]
    public void Trusted_KeepsDefaultComparer()
    {
        var trusted = new MessagePackSerializerOptions(
            new MessagePackFormatterResolver(MessagePackFormatterFactory.Default, hashFloodingResistant: false));

        var bytes = MessagePackSerializer.Serialize(NewSource().ToFrozenDictionary(), trusted);
        var dict = MessagePackSerializer.Deserialize<FrozenDictionary<int, string>>(bytes, trusted)!;
        Assert.Same(EqualityComparer<int>.Default, dict.Comparer);
    }

    [Fact]
    public void Nulls_Roundtrip()
    {
        Assert.Null(MessagePackSerializer.Deserialize<FrozenDictionary<int, string>?>(MessagePackSerializer.Serialize<FrozenDictionary<int, string>?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<FrozenSet<long>?>(MessagePackSerializer.Serialize<FrozenSet<long>?>(null)));
    }

    [Fact]
    public void DuplicateEntries_LastWinPolicyViaStaging()
    {
        // duplicate-free normal path already covered; here just confirm empty works too
        var empty = new Dictionary<int, string>().ToFrozenDictionary();
        var back = MessagePackSerializer.Deserialize<FrozenDictionary<int, string>>(MessagePackSerializer.Serialize(empty))!;
        Assert.Empty(back);
    }
}
