using MessagePack;

namespace MessagePack.Tests.NetFx;

// Executes the collection formatters' downlevel branches (#if !NET: indexer-based List
// populate, Add-loop fresh path) on the netstandard2.0 asset via .NET Framework 4.8.
// The main net10-only test project compiles these branches but never runs them.
public class DownlevelCollectionTests
{
    [Fact]
    public void ListRoundtrip()
    {
        var ints = new List<int> { 1, -1, 128, -129, 70000, int.MaxValue };
        Assert.Equal(ints, MessagePackSerializer.Deserialize<List<int>>(MessagePackSerializer.Serialize(ints)));

        var strings = new List<string?> { "a", "こんにちは", "", null, "longer string value here" };
        Assert.Equal(strings, MessagePackSerializer.Deserialize<List<string?>>(MessagePackSerializer.Serialize(strings)));

        var empty = new List<int>();
        Assert.Equal(empty, MessagePackSerializer.Deserialize<List<int>>(MessagePackSerializer.Serialize(empty)));

        Assert.Null(MessagePackSerializer.Deserialize<List<int>?>(MessagePackSerializer.Serialize<List<int>?>(null)));
    }

    [Fact]
    public void List_FreshPath_NullIncoming()
    {
        var bytes = MessagePackSerializer.Serialize(new List<string> { "x", "yy" });
        List<string>? fresh = null;
        MessagePackSerializer.Deserialize(bytes, ref fresh);
        Assert.Equal(new List<string> { "x", "yy" }, fresh);
    }

    [Fact]
    public void List_Populate_ReusesInstance_GrowsAndShrinks()
    {
        var bytes = MessagePackSerializer.Serialize(new List<string> { "x", "yy" });

        // reuse: same instance, shorter incoming grows to payload size (Add branch)
        var target = new List<string> { "a" };
        var original = target;
        MessagePackSerializer.Deserialize(bytes, ref target);
        Assert.Same(original, target);
        Assert.Equal(new List<string> { "x", "yy" }, target);

        // longer incoming shrinks to payload size (RemoveRange branch)
        var longer = new List<string> { "a", "b", "c" };
        MessagePackSerializer.Deserialize(bytes, ref longer);
        Assert.Equal(new List<string> { "x", "yy" }, longer);

        // exact length: prefix-overwrite branch only
        var exact = new List<string> { "a", "b" };
        MessagePackSerializer.Deserialize(bytes, ref exact);
        Assert.Equal(new List<string> { "x", "yy" }, exact);
    }

    [Fact]
    public void Array_Populate_ReusesOnExactLengthOnly()
    {
        var bytes = MessagePackSerializer.Serialize(new[] { "x", "yy" });

        var target = new[] { "a", "b" };
        var original = target;
        MessagePackSerializer.Deserialize(bytes, ref target);
        Assert.Same(original, target);
        Assert.Equal(new[] { "x", "yy" }, target);

        var mismatched = new[] { "a" };
        var before = mismatched;
        MessagePackSerializer.Deserialize(bytes, ref mismatched);
        Assert.NotSame(before, mismatched);
        Assert.Equal(new[] { "x", "yy" }, mismatched);
    }

    [Fact]
    public void NestedCollectionsRoundtrip()
    {
        var nested = new List<List<int>?> { new List<int> { 1, 2 }, null, new List<int>() };
        var back = MessagePackSerializer.Deserialize<List<List<int>?>>(MessagePackSerializer.Serialize(nested));
        Assert.Equal(nested, back);

        var dict = new Dictionary<string, int> { ["one"] = 1, ["two"] = 2, ["big"] = 100000, ["neg"] = -50 };
        Assert.Equal(dict, MessagePackSerializer.Deserialize<Dictionary<string, int>>(MessagePackSerializer.Serialize(dict)));
    }
}
