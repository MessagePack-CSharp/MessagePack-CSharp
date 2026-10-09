using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// reflection tier: Base.X [Key(0)] virtual, Middle.X [Key(1)] new virtual (another slot), Derived overrides Middle.X:
// the override's inherited attributes come from its own chain (Middle), not from the unrelated Base slot
public class SlotBase
{
    [Key(0)] public virtual int X { get; set; }
}

public class SlotMiddle : SlotBase
{
    [Key(1)] public new virtual int X { get; set; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class SlotDerived : SlotMiddle
{
    public override int X { get; set; }
}

// a pattern union holding an int[] through its IEnumerable<int> case
[MessagePackObject]
[UnionTag(typeof(IEnumerable<int>), 0)]
[UnionTag(typeof(string), 1)]
public union SequenceOrText(IEnumerable<int>, string);

public class ReviewRound8RegressionTests
{
    [Fact]
    public void Reflection_OverrideInheritsOnlyItsOwnChain()
    {
        var value = new SlotDerived { X = 7 };
        ((SlotBase)value).X = 1;
        var bytes = V4.Serialize(value);
        Assert.Equal(new byte[] { 0x92, 0x01, 0x07 }, bytes); // [Base.X, Middle/Derived.X]
        var back = V4.Deserialize<SlotDerived>(bytes)!;
        Assert.Equal(7, back.X);
        Assert.Equal(1, ((SlotBase)back).X);
    }

    [Fact]
    public void PatternUnion_InterfaceCase_AcceptsAnAssignableValue()
    {
        SequenceOrText sequence = new int[] { 1, 2, 3 };
        var back = V4.Deserialize<SequenceOrText>(V4.Serialize(sequence));
        Assert.Equal([1, 2, 3], Assert.IsAssignableFrom<IEnumerable<int>>(back.Value));

        SequenceOrText text = "t";
        Assert.Equal("t", Assert.IsType<string>(V4.Deserialize<SequenceOrText>(V4.Serialize(text)).Value));
    }

    // a registered type's name is as long as it is: the allow list has no name parse to bound (only LoadAnyType has)
    [Fact]
    public void AllowedTypes_LongTypeName_Roundtrips()
    {
        var type = typeof(Dictionary<string, Tuple<string, string, string, string, string, string, string>>);
        Assert.True(type.AssemblyQualifiedName!.Length > 1024, type.AssemblyQualifiedName.Length.ToString());
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.AllowedTypes(type))));
        var value = new Dictionary<string, Tuple<string, string, string, string, string, string, string>>
        {
            ["k"] = Tuple.Create("a", "b", "c", "d", "e", "f", "g"),
        };
        var back = Assert.IsType<Dictionary<string, Tuple<string, string, string, string, string, string, string>>>(V4.Deserialize<object>(V4.Serialize<object>(value, options), options));
        Assert.Equal("g", back["k"].Item7);

#pragma warning disable CS0618
        var any = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())));
#pragma warning restore CS0618
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<object>(V4.Serialize<object>(value, any), any)); // the parsing loader keeps its cap
    }
}
