using System.Collections.Frozen;
using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

public class ReviewRound17RegressionTests
{
    static readonly MessagePackSerializerOptions Typeless = new(new MessagePackFormatterResolver(
        MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.AllowedTypes(typeof(FrozenSet<int>), typeof(FrozenDictionary<int, string>)))));

    [MessagePackObject]
    public class FrozenSlot
    {
        [Key(0)] public object? Value { get; set; }
    }

    // ToFrozenSet() / ToFrozenDictionary() return BCL-internal implementations: the runtime-type paths (an object slot,
    // Serialize(Type, ...) with value.GetType(), Typeless) serialize them as their public FrozenSet / FrozenDictionary base
    [Fact]
    public void FrozenCollections_SerializeThroughTheirPublicBase_FromRuntimeTypePaths()
    {
        var set = new[] { 1, 2, 3 }.ToFrozenSet();
        var dictionary = new Dictionary<int, string> { [1] = "a", [2] = "b" }.ToFrozenDictionary();
        Assert.NotEqual(typeof(FrozenSet<int>), set.GetType()); // the premise: an internal subclass

        var viaObject = V4.Serialize<object>(set);
        Assert.Equal([1, 2, 3], V4.Deserialize<int[]>(viaObject)!.OrderBy(static x => x));
        var slot = V4.Deserialize<FrozenSlot>(V4.Serialize(new FrozenSlot { Value = dictionary }))!;
        Assert.Equal(2, Assert.IsType<Dictionary<object, object?>>(slot.Value).Count);

        var viaType = V4.Serialize(set.GetType(), set);
        Assert.Equal(viaObject, viaType);
        Assert.Equal([1, 2], V4.Deserialize<Dictionary<int, string>>(V4.Serialize(dictionary.GetType(), dictionary))!.Keys.OrderBy(static x => x));

        // Typeless names the public base, so the other side reads a FrozenSet<int>, not an internal type name
        var typeless = V4.Serialize<object>(set, Typeless);
        Assert.IsAssignableFrom<FrozenSet<int>>(V4.Deserialize<object>(typeless, Typeless));
        Assert.IsAssignableFrom<FrozenDictionary<int, string>>(V4.Deserialize<object>(V4.Serialize<object>(dictionary, Typeless), Typeless));
    }
}
