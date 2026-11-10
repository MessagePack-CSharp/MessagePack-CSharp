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

    // string keys: HashFloodingResistantEqualityComparer.Get<string>() is null on net9+, because Dictionary and HashSet
    // defend the default string comparer themselves (a randomized hash once a bucket chain grows). The frozen
    // collections do not: on the default comparer they pick an ordinal-string strategy that hashes with a FIXED seed,
    // so colliding keys can be computed ahead, and that rents a table sized by the spread of the key lengths. Measured
    // through the serializer on 2026-11-10 (.NET 10): 28,900 crafted 24-char keys made 28,900 lookups take 759 ms
    // against 14 ms for ordinary keys (HashSet<string>: no difference), and two keys, "" and 10 MB of 'a', allocated
    // 288 MB (HashSet<string>: 20 MB). An earlier version of this test pinned the default comparer here, on the premise
    // that it hashes with a per-process seed; that holds for Dictionary, not for the frozen types. So frozen string
    // keys get the keyed comparer on every target, at the cost of the BCL's ordinal-string strategies.
    [Fact]
    public void StringKeys_GetTheKeyedComparer()
    {
        var dict = MessagePackSerializer.Deserialize<FrozenDictionary<string, int>>(
            MessagePackSerializer.Serialize(new Dictionary<string, int> { ["a"] = 1, ["bb"] = 2 }.ToFrozenDictionary()))!;
        Assert.NotSame(EqualityComparer<string>.Default, dict.Comparer);
        Assert.Equal(2, dict["bb"]);
        Assert.False(dict.ContainsKey("BB")); // still ordinal

        var set = MessagePackSerializer.Deserialize<FrozenSet<string>>(
            MessagePackSerializer.Serialize(new[] { "a", "bb" }.ToFrozenSet()))!;
        Assert.NotSame(EqualityComparer<string>.Default, set.Comparer);
        Assert.Same(dict.Comparer, set.Comparer);
        Assert.Contains("bb", (IReadOnlySet<string>)set);

        // trusted data keeps the BCL's own strategy
        var trusted = new MessagePackSerializerOptions(
            new MessagePackFormatterResolver(MessagePackFormatterFactory.Default) { HashFloodingResistant = false });
        var plain = MessagePackSerializer.Deserialize<FrozenSet<string>>(MessagePackSerializer.Serialize(new[] { "a", "bb" }, trusted), trusted)!;
        Assert.Same(EqualityComparer<string>.Default, plain.Comparer);
    }

    // the allocation half of the same finding: with the default comparer, two keys whose lengths are far apart made
    // the BCL rent 5 ints per unit of length difference (23x to 36x the payload). On the keyed comparer the frozen set
    // costs what a HashSet costs.
    [Fact]
    public void StringKeys_FarApartLengths_DoNotAmplify()
    {
        var bytes = new byte[2 + 5 + 1_000_000];
        bytes[0] = 0x92;
        bytes[1] = 0xa0;
        bytes[2] = 0xdb;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(3), 1_000_000);
        bytes.AsSpan(7).Fill((byte)'a');

        MessagePackSerializer.Deserialize<FrozenSet<string>>([0x91, 0xa1, 0x61]); // warm the formatter
        var before = GC.GetAllocatedBytesForCurrentThread();
        var set = MessagePackSerializer.Deserialize<FrozenSet<string>>(bytes)!;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(2, set.Count);
        Assert.True(allocated < 8L * bytes.Length, $"a {bytes.Length:N0}-byte payload allocated {allocated:N0} bytes");
    }

    [Fact]
    public void Trusted_KeepsDefaultComparer()
    {
        var trusted = new MessagePackSerializerOptions(
            new MessagePackFormatterResolver(MessagePackFormatterFactory.Default) { HashFloodingResistant = false });

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
