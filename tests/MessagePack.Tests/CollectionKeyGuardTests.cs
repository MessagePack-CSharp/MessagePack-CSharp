using System.Collections.Frozen;
using System.Collections.Immutable;
using MessagePack;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Malformed input must surface as MessagePackSerializationException (the robustness contract), never as the
// ArgumentNullException / ArgumentException the BCL collections throw on their own: a nil map key reaching a
// dictionary indexer, or keys of different runtime types reaching Comparer<object>.Default in a sorted collection.
public class CollectionKeyGuardTests
{
    static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Default;

    static readonly byte[] NilKeyMap = [0x81, 0xC0, 0x01]; // { nil: 1 }
    static readonly byte[] NilKeyLookup = [0x91, 0x92, 0xC0, 0x90]; // [ [nil, []] ]
    static readonly byte[] MixedKeyMap = [0x82, 0x01, 0xC0, 0xA1, 0x61, 0xC0]; // { 1: nil, "a": nil }
    static readonly byte[] MixedElementArray = [0x92, 0x01, 0xA1, 0x61]; // [ 1, "a" ]
    static readonly byte[] MixedPriorityQueue = [0x92, 0x92, 0x01, 0x01, 0x92, 0xA1, 0x61, 0xA1, 0x61]; // [ [1, 1], ["a", "a"] ]

    static void AssertRejected<T>(byte[] payload)
    {
        var ex = Record.Exception(() => V4.Deserialize<T>(payload, Options));
        Assert.IsType<MessagePackSerializationException>(ex);
    }

    [Fact]
    public void NilMapKey_IsADataError_InEveryDictionaryShape()
    {
        AssertRejected<Dictionary<string, int>>(NilKeyMap); // the baseline the others follow
        AssertRejected<FrozenDictionary<string, int>>(NilKeyMap);
        AssertRejected<ImmutableDictionary<string, int>>(NilKeyMap);
        AssertRejected<ImmutableSortedDictionary<string, int>>(NilKeyMap);
        AssertRejected<IImmutableDictionary<string, int>>(NilKeyMap);

        // ILookup is the exception: LINQ's Lookup holds one group under a null key (ToLookup(x => (string)null!)),
        // so a single nil-keyed group is legal data, and only a second one is the duplicate-key error
        var lookup = V4.Deserialize<ILookup<string, int>>(NilKeyLookup, Options)!;
        Assert.Equal(1, lookup.Count);
        Assert.True(lookup.Contains(null!));
        Assert.Empty(lookup[null!]);
        AssertRejected<ILookup<string, int>>([0x92, 0x92, 0xC0, 0x90, 0x92, 0xC0, 0x90]); // [ [nil, []], [nil, []] ]
    }

    [Fact]
    public void MixedRuntimeTypeKeys_AreADataError_InComparisonBasedCollections()
    {
        AssertRejected<SortedDictionary<object, object?>>(MixedKeyMap);
        AssertRejected<ImmutableSortedDictionary<object, object?>>(MixedKeyMap);
        AssertRejected<SortedSet<object>>(MixedElementArray);
        AssertRejected<ImmutableSortedSet<object>>(MixedElementArray);
        AssertRejected<PriorityQueue<object, object>>(MixedPriorityQueue);
    }

    [Fact]
    public void HomogeneousObjectKeys_StillRoundTrip()
    {
        // the guards only reword the failure; comparable object keys keep working through the same path
        var sorted = V4.Deserialize<SortedDictionary<object, object?>>(V4.Serialize(new SortedDictionary<object, object?> { [1] = "x", [2] = null }, Options), Options)!;
        Assert.Equal(2, sorted.Count);
        var set = V4.Deserialize<SortedSet<object>>(V4.Serialize(new SortedSet<object> { "b", "a" }, Options), Options)!;
        Assert.Equal(["a", "b"], set.ToArray());
    }
}
