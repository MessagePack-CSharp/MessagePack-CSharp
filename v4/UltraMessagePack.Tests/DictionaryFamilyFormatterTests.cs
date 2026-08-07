using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using UltraMessagePack;
using Xunit;
using Oracle = MessagePack.MessagePackSerializer;

namespace UltraMessagePack.Tests;

// TKey/TValue family (DictionaryFormatters.cs): wire-compared against MessagePack v3 and
// roundtripped; hash-based ones must come back with the flooding-resistant comparer.
public class DictionaryFamilyFormatterTests
{
    static Dictionary<int, string> NewSource() => new() { [1] = "a", [2] = "b", [30000] = "c" };

    static void AssertOracleAndRoundtrip<T>(T value) where T : IEnumerable<KeyValuePair<int, string>>
    {
        var ours = MessagePackSerializer.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), ours);
        var back = MessagePackSerializer.Deserialize<T>(ours)!;
        Assert.Equal(value.OrderBy(kv => kv.Key), back.OrderBy(kv => kv.Key));
    }

    [Fact]
    public void ConcreteDictionaries_MatchOracleAndRoundtrip()
    {
        AssertOracleAndRoundtrip(new SortedList<int, string>(NewSource()));
        AssertOracleAndRoundtrip(new SortedDictionary<int, string>(NewSource()));
        AssertOracleAndRoundtrip(new ReadOnlyDictionary<int, string>(NewSource()));
        AssertOracleAndRoundtrip(new ConcurrentDictionary<int, string>(NewSource()));
    }

    [Fact]
    public void InterfaceDictionaries_MatchOracleAndRoundtrip()
    {
        AssertOracleAndRoundtrip<IDictionary<int, string>>(NewSource());
        AssertOracleAndRoundtrip<IReadOnlyDictionary<int, string>>(NewSource());
    }

    [Fact]
    public void Nulls_Roundtrip()
    {
        Assert.Null(MessagePackSerializer.Deserialize<SortedList<int, string>?>(MessagePackSerializer.Serialize<SortedList<int, string>?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<SortedDictionary<int, string>?>(MessagePackSerializer.Serialize<SortedDictionary<int, string>?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<ReadOnlyDictionary<int, string>?>(MessagePackSerializer.Serialize<ReadOnlyDictionary<int, string>?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<ConcurrentDictionary<int, string>?>(MessagePackSerializer.Serialize<ConcurrentDictionary<int, string>?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<IDictionary<int, string>?>(MessagePackSerializer.Serialize<IDictionary<int, string>?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<IReadOnlyDictionary<int, string>?>(MessagePackSerializer.Serialize<IReadOnlyDictionary<int, string>?>(null)));
    }

    [Fact]
    public void HashBased_GetFloodingResistantComparer()
    {
        var bytes = MessagePackSerializer.Serialize(NewSource());
        var expected = HashFloodingResistantEqualityComparer.Get<int>();

        var idict = MessagePackSerializer.Deserialize<IDictionary<int, string>>(bytes)!;
        Assert.Same(expected, ((Dictionary<int, string>)idict).Comparer);

        var rodict = MessagePackSerializer.Deserialize<IReadOnlyDictionary<int, string>>(bytes)!;
        Assert.Same(expected, ((Dictionary<int, string>)rodict).Comparer);
    }

    [Fact]
    public void ComparisonBased_KeepDefaultComparer()
    {
        var bytes = MessagePackSerializer.Serialize(NewSource());
        var sorted = MessagePackSerializer.Deserialize<SortedDictionary<int, string>>(bytes)!;
        Assert.Same(Comparer<int>.Default, sorted.Comparer);
    }

    // last-win duplicate-key policy must hold across the family (matching Dictionary)
    [Fact]
    public void DuplicateKeys_LastWins()
    {
        // hand-craft {1:"first", 1:"second"} — no dictionary type can produce this payload
        var bytes = SerializeAsMap([new(1, "first"), new(1, "second")]);

        var dict = MessagePackSerializer.Deserialize<IDictionary<int, string>>(bytes)!;
        Assert.Single(dict);
        Assert.Equal("second", dict[1]);

        var sorted = MessagePackSerializer.Deserialize<SortedDictionary<int, string>>(bytes)!;
        Assert.Single(sorted);
        Assert.Equal("second", sorted[1]);
    }

    // net9+ OrderedDictionary: hash-based + insertion-ordered; no v3 oracle (v3 has no
    // formatter for it), so the wire framing is validated by cross-reading as Dictionary
    [Fact]
    public void OrderedDictionary_PreservesInsertionOrder_AndGetsComparer()
    {
        var source = new OrderedDictionary<int, string> { [30000] = "c", [1] = "a", [2] = "b" };
        var bytes = MessagePackSerializer.Serialize(source);

        var back = MessagePackSerializer.Deserialize<OrderedDictionary<int, string>>(bytes)!;
        Assert.Equal(source, back); // sequence-compare: insertion order must survive
        Assert.Same(HashFloodingResistantEqualityComparer.Get<int>(), back.Comparer);

        var asDict = MessagePackSerializer.Deserialize<Dictionary<int, string>>(bytes)!;
        Assert.Equal(source.OrderBy(kv => kv.Key), asDict.OrderBy(kv => kv.Key));

        Assert.Null(MessagePackSerializer.Deserialize<OrderedDictionary<int, string>?>(MessagePackSerializer.Serialize<OrderedDictionary<int, string>?>(null)));
    }

    [Fact]
    public void PriorityQueue_RoundtripsElementsAndPriorities()
    {
        var source = new PriorityQueue<string, int>();
        source.Enqueue("low", 30);
        source.Enqueue("high", 1);
        source.Enqueue("mid", 15);
        source.Enqueue("high2", 1); // duplicate priority, distinct element

        var bytes = MessagePackSerializer.Serialize(source);
        var back = MessagePackSerializer.Deserialize<PriorityQueue<string, int>>(bytes)!;

        Assert.Equal(source.Count, back.Count);
        // dequeue-with-priority order comparison (ties broken arbitrarily → compare sorted pairs)
        static List<(string, int)> Drain(PriorityQueue<string, int> q)
        {
            var drained = new List<(string, int)>();
            while (q.TryDequeue(out var e, out var p))
            {
                drained.Add((e, p));
            }
            return drained;
        }
        Assert.Equal(Drain(source).OrderBy(x => x), Drain(back).OrderBy(x => x));

        Assert.Null(MessagePackSerializer.Deserialize<PriorityQueue<string, int>?>(MessagePackSerializer.Serialize<PriorityQueue<string, int>?>(null)));
    }

    static byte[] SerializeAsMap(List<KeyValuePair<int, string>> pairs)
    {
        // msgpack map header + alternating key/value, encoded via the oracle primitives
        var writer = new ArrayBufferWriter<byte>();
        var w = new MessagePack.MessagePackWriter(writer);
        w.WriteMapHeader(pairs.Count);
        foreach (var kv in pairs)
        {
            w.Write(kv.Key);
            w.Write(kv.Value);
        }
        w.Flush();
        return writer.WrittenSpan.ToArray();
    }
}
