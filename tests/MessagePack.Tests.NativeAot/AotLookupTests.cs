using MessagePack;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests.NativeAot;

[MessagePackObject]
public class AotLookupHolder
{
    [Key(0)] public ILookup<string, int>? ByName { get; set; }
    [Key(1)] public IGrouping<string, int>? Group { get; set; }
}

// InterfaceLookupFormatter resolves IGrouping<K,V>, and InterfaceGroupingFormatter IEnumerable<V>, from the resolver;
// the generator harvested only K and V, so under Native AOT (no run-time closing) a non-empty ILookup member was
// "Type ... is not found" while the JIT Default chain, which closes the rest at run time, hid it.
public class AotLookupTests
{
    static readonly MessagePackSerializerOptions Options = new(new MessagePackFormatterResolver(
    [
        MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
        BuiltInFormatterFactory.Instance,
    ]));

    [Fact]
    public void LookupAndGroupingMembers_Roundtrip()
    {
        var pairs = new[] { ("a", 1), ("a", 2), ("b", 3) };
        var holder = new AotLookupHolder
        {
            ByName = pairs.ToLookup(p => p.Item1, p => p.Item2),
            Group = pairs.GroupBy(p => p.Item1, p => p.Item2).First(),
        };

        var back = V4.Deserialize<AotLookupHolder>(V4.Serialize(holder, Options), Options)!;
        Assert.Equal(2, back.ByName!.Count);
        Assert.Equal([1, 2], back.ByName["a"]);
        Assert.Equal([3], back.ByName["b"]);
        Assert.Equal("a", back.Group!.Key);
        Assert.Equal([1, 2], back.Group);
    }
}
