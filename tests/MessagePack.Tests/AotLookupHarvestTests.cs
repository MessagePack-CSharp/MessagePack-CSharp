using Xunit;

namespace MessagePack.Tests;

// InterfaceLookupFormatter resolves IGrouping<K,V>, and InterfaceGroupingFormatter IEnumerable<V>, from the resolver.
// The generator harvested only K and V, so on the AOT chains (no GenericFormatterFactory to close the rest at run time)
// a non-empty ILookup or a non-null IGrouping member was "Type ... is not found", while the Default chain passed.
public class AotLookupHarvestTests
{
    [MessagePackObject]
    public class LookupHolder
    {
        [Key(0)]
        public ILookup<string, int>? ByName { get; set; }

        [Key(1)]
        public IGrouping<string, int>? Group { get; set; }
    }

    static readonly MessagePackSerializerOptions Aot = MessagePackSerializerOptions.DefaultAot;

    [Fact]
    public void DefaultAot_ServesLookupAndGroupingMembers()
    {
        var pairs = new[] { ("a", 1), ("a", 2), ("b", 3) };
        var holder = new LookupHolder
        {
            ByName = pairs.ToLookup(p => p.Item1, p => p.Item2),
            Group = pairs.GroupBy(p => p.Item1, p => p.Item2).First(),
        };

        var bytes = MessagePackSerializer.Serialize(holder, Aot);
        Assert.Equal(MessagePackSerializer.Serialize(holder), bytes); // same wire as the Default chain

        var back = MessagePackSerializer.Deserialize<LookupHolder>(bytes, Aot)!;
        Assert.Equal(2, back.ByName!.Count);
        Assert.Equal([1, 2], back.ByName["a"]);
        Assert.Equal([3], back.ByName["b"]);
        Assert.Equal("a", back.Group!.Key);
        Assert.Equal([1, 2], back.Group);

        // empty and nil never needed the nested formatters; they still work
        var empty = MessagePackSerializer.Deserialize<LookupHolder>(MessagePackSerializer.Serialize(new LookupHolder { ByName = Array.Empty<(string, int)>().ToLookup(p => p.Item1, p => p.Item2) }, Aot), Aot)!;
        Assert.Empty(empty.ByName!);
        Assert.Null(empty.Group);
    }
}
