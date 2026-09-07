extern alias V3;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// IGrouping / ILookup (CollectionFormatters.cs), v3-parity wire: grouping = [key,
// [elements]] fixarray2, lookup = array of groupings — wire-compared against the oracle.
public class GroupingLookupFormatterTests
{
    static ILookup<int, string> NewLookup() =>
        new[] { (1, "a"), (2, "b"), (1, "c"), (3, "d") }.ToLookup(x => x.Item1, x => x.Item2);

    [Fact]
    public void Lookup_MatchesOracleAndRoundtrips()
    {
        var value = NewLookup();
        var ours = MessagePackSerializer.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), ours);

        var back = MessagePackSerializer.Deserialize<ILookup<int, string>>(ours)!;
        Assert.Equal(3, back.Count);
        Assert.Equal(["a", "c"], back[1]);
        Assert.Equal(["b"], back[2]);
        Assert.Equal(["d"], back[3]);
        Assert.True(back.Contains(1));
        Assert.False(back.Contains(4));
        Assert.Empty(back[4]); // missing key = empty sequence, ILookup contract
    }

    [Fact]
    public void Grouping_MatchesOracleAndRoundtrips()
    {
        var value = NewLookup().First(); // key 1: ["a", "c"]
        var ours = MessagePackSerializer.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), ours);

        var back = MessagePackSerializer.Deserialize<IGrouping<int, string>>(ours)!;
        Assert.Equal(1, back.Key);
        Assert.Equal(["a", "c"], back);
    }

    // ILookup contract: groupings enumerate in first-encounter key order. The internal
    // Lookup keeps that order in a List, NOT via Dictionary insertion-order behavior
    // (which is an implementation detail, not a contract).
    [Fact]
    public void EnumerationOrder_PreservesFirstEncounterOrder()
    {
        var value = new[] { (3, "x"), (1, "y"), (2, "z"), (1, "w") }.ToLookup(t => t.Item1, t => t.Item2);
        Assert.Equal([3, 1, 2], value.Select(g => g.Key)); // LINQ's own guarantee

        var back = MessagePackSerializer.Deserialize<ILookup<int, string>>(MessagePackSerializer.Serialize(value))!;
        Assert.Equal([3, 1, 2], back.Select(g => g.Key));
        Assert.Equal(["y", "w"], back[1]);

        // and the roundtrip is wire-stable: re-serializing produces identical bytes
        Assert.Equal(MessagePackSerializer.Serialize(value), MessagePackSerializer.Serialize(back));
    }

    [Fact]
    public void EmptyLookup_Roundtrips()
    {
        var value = Array.Empty<(int, string)>().ToLookup(x => x.Item1, x => x.Item2);
        var back = MessagePackSerializer.Deserialize<ILookup<int, string>>(MessagePackSerializer.Serialize(value))!;
        Assert.Equal(0, back.Count);
        Assert.Empty(back[1]);
    }

    [Fact]
    public void Nulls_Roundtrip()
    {
        Assert.Null(MessagePackSerializer.Deserialize<ILookup<int, string>?>(MessagePackSerializer.Serialize<ILookup<int, string>?>(null)));
        Assert.Null(MessagePackSerializer.Deserialize<IGrouping<int, string>?>(MessagePackSerializer.Serialize<IGrouping<int, string>?>(null)));
    }

    // duplicate group keys cannot come from a real ToLookup — hand-craft the payload;
    // the v4-wide duplicate-rejection policy applies (decision 2026-08-20)
    [Fact]
    public void DuplicateGroupKeys_AreRejected()
    {
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var w = new V3::MessagePack.MessagePackWriter(writer);
        w.WriteArrayHeader(2);
        w.WriteArrayHeader(2); // grouping 1: [1, ["first"]]
        w.Write(1);
        w.WriteArrayHeader(1);
        w.Write("first");
        w.WriteArrayHeader(2); // grouping 2: [1, ["second"]]
        w.Write(1);
        w.WriteArrayHeader(1);
        w.Write("second");
        w.Flush();

        var payload = writer.WrittenSpan.ToArray();
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<ILookup<int, string>>(payload));
    }
}
