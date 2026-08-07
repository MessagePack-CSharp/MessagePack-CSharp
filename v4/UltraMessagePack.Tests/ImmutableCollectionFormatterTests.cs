using System.Collections.Immutable;
using UltraMessagePack;
using Xunit;
using Oracle = MessagePack.MessagePackSerializer;

namespace UltraMessagePack.Tests;

// System.Collections.Immutable family (ImmutableCollectionFormatters.cs): wire-compared
// against MessagePack v3 (same instance enumerates identically on both sides) and
// roundtripped; hash-based ones must come back with the flooding-resistant key comparer.
public class ImmutableCollectionFormatterTests
{
    static readonly int[] Payload = [1, -1, 128, 70000, int.MinValue];

    static Dictionary<int, string> NewSource() => new() { [1] = "a", [2] = "b", [30000] = "c" };

    static byte[] AssertOracle<T>(T value)
    {
        var ours = MessagePackSerializer.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), ours);
        return ours;
    }

    [Fact]
    public void Sequences_MatchOracleAndRoundtrip()
    {
        // order-preserving: sequence-compare (ImmutableArray's IEquatable is
        // reference-equality of the backing array, so compare as arrays)
        var array = ImmutableArray.CreateRange(Payload);
        Assert.Equal(Payload, MessagePackSerializer.Deserialize<ImmutableArray<int>>(AssertOracle(array)).ToArray());

        var list = ImmutableList.CreateRange(Payload);
        Assert.Equal(list, MessagePackSerializer.Deserialize<ImmutableList<int>>(AssertOracle(list)));

        var queue = ImmutableQueue.CreateRange(Payload);
        Assert.Equal(queue.ToArray(), MessagePackSerializer.Deserialize<ImmutableQueue<int>>(AssertOracle(queue))!.ToArray());

        var stack = ImmutableStack.CreateRange(Payload);
        Assert.Equal(stack.ToArray(), MessagePackSerializer.Deserialize<ImmutableStack<int>>(AssertOracle(stack))!.ToArray());

        var sortedSet = ImmutableSortedSet.CreateRange(Payload);
        Assert.Equal(sortedSet, MessagePackSerializer.Deserialize<ImmutableSortedSet<int>>(AssertOracle(sortedSet)));

        // hash set: content-compare (roundtrip comparer differs → enumeration order differs)
        var hashSet = ImmutableHashSet.CreateRange(Payload);
        var backSet = MessagePackSerializer.Deserialize<ImmutableHashSet<int>>(AssertOracle(hashSet))!;
        Assert.Equal(hashSet.OrderBy(x => x), backSet.OrderBy(x => x));
    }

    [Fact]
    public void ImmutableArray_DefaultRoundtripsAsNil()
    {
        var bytes = MessagePackSerializer.Serialize(default(ImmutableArray<int>));
        Assert.Equal([0xc0], bytes); // nil
        Assert.True(MessagePackSerializer.Deserialize<ImmutableArray<int>>(bytes).IsDefault);
    }

    [Fact]
    public void Dictionaries_MatchOracleAndRoundtrip()
    {
        var dict = ImmutableDictionary.CreateRange(NewSource());
        var backDict = MessagePackSerializer.Deserialize<ImmutableDictionary<int, string>>(AssertOracle(dict))!;
        Assert.Equal(dict.OrderBy(kv => kv.Key), backDict.OrderBy(kv => kv.Key));

        var sorted = ImmutableSortedDictionary.CreateRange(NewSource());
        Assert.Equal(sorted, MessagePackSerializer.Deserialize<ImmutableSortedDictionary<int, string>>(AssertOracle(sorted)));
    }

    [Fact]
    public void Interfaces_MatchOracleAndRoundtrip()
    {
        var list = (IImmutableList<int>)ImmutableList.CreateRange(Payload);
        Assert.Equal(list, MessagePackSerializer.Deserialize<IImmutableList<int>>(AssertOracle(list)));

        var set = (IImmutableSet<int>)ImmutableHashSet.CreateRange(Payload);
        var backSet = MessagePackSerializer.Deserialize<IImmutableSet<int>>(AssertOracle(set))!;
        Assert.Equal(set.OrderBy(x => x), backSet.OrderBy(x => x));

        var queue = (IImmutableQueue<int>)ImmutableQueue.CreateRange(Payload);
        Assert.Equal(queue.ToArray(), MessagePackSerializer.Deserialize<IImmutableQueue<int>>(AssertOracle(queue))!.ToArray());

        var stack = (IImmutableStack<int>)ImmutableStack.CreateRange(Payload);
        Assert.Equal(stack.ToArray(), MessagePackSerializer.Deserialize<IImmutableStack<int>>(AssertOracle(stack))!.ToArray());

        var dict = (IImmutableDictionary<int, string>)ImmutableDictionary.CreateRange(NewSource());
        var backDict = MessagePackSerializer.Deserialize<IImmutableDictionary<int, string>>(AssertOracle(dict))!;
        Assert.Equal(dict.OrderBy(kv => kv.Key), backDict.OrderBy(kv => kv.Key));
    }

    [Fact]
    public void HashBased_GetFloodingResistantKeyComparer()
    {
        var dict = MessagePackSerializer.Deserialize<ImmutableDictionary<int, string>>(
            MessagePackSerializer.Serialize(ImmutableDictionary.CreateRange(NewSource())))!;
        Assert.Same(HashFloodingResistantEqualityComparer.Get<int>(), dict.KeyComparer);

        var set = MessagePackSerializer.Deserialize<ImmutableHashSet<long>>(
            MessagePackSerializer.Serialize(ImmutableHashSet.CreateRange(new[] { 1L, 2L })))!;
        Assert.Same(HashFloodingResistantEqualityComparer.Get<long>(), set.KeyComparer);
    }

    [Fact]
    public void Nulls_Roundtrip()
    {
        Assert.Null(MessagePackSerializer.Deserialize<ImmutableList<int>?>(MessagePackSerializer.Serialize<ImmutableList<int>?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<ImmutableDictionary<int, string>?>(MessagePackSerializer.Serialize<ImmutableDictionary<int, string>?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<IImmutableSet<int>?>(MessagePackSerializer.Serialize<IImmutableSet<int>?>(null)));
    }
}
