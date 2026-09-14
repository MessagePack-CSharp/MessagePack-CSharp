extern alias V3;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// [Union] polymorphism through the source generator: fixarray(2) [int tag, payload] on
// the wire, byte-identical to v3's UnionResolver over the same attributed hierarchy.
public class GeneratedUnionTests
{
    static void AssertBytesAndRoundtrip<T>(T value)
    {
        var ours = V4.Serialize(value);
        var oracle = Oracle.Serialize(value);
        Assert.Equal(oracle, ours);

        var back = V4.Deserialize<T>(ours);
        Assert.Equal(oracle, Oracle.Serialize(back));
        var fromOracle = V4.Deserialize<T>(oracle);
        Assert.Equal(oracle, Oracle.Serialize(fromOracle));
    }

    [Fact]
    public void InterfaceUnion_DispatchesByRuntimeType()
    {
        AssertBytesAndRoundtrip<IGenShape>(new GenCircle { Radius = 2.5 });
        AssertBytesAndRoundtrip<IGenShape>(new GenRectangle { Width = 3, Height = 4 });
        AssertBytesAndRoundtrip<IGenShape?>(null);

        Assert.IsType<GenCircle>(V4.Deserialize<IGenShape>(V4.Serialize<IGenShape>(new GenCircle { Radius = 1 })));
        Assert.IsType<GenRectangle>(V4.Deserialize<IGenShape>(V4.Serialize<IGenShape>(new GenRectangle { Width = 1, Height = 2 })));
    }

    [Fact]
    public void AbstractClassUnion_DispatchesByRuntimeType()
    {
        AssertBytesAndRoundtrip<GenAnimal>(new GenDog { Name = "ポチ", BarkCount = 3 });
        AssertBytesAndRoundtrip<GenAnimal>(new GenCat { Name = "タマ", Lives = 9 });

        var back = (GenDog)V4.Deserialize<GenAnimal>(V4.Serialize<GenAnimal>(new GenDog { Name = "n", BarkCount = 1 }))!;
        Assert.Equal("n", back.Name);
        Assert.Equal(1, back.BarkCount);
    }

    [Fact]
    public void UnionMember_InsideAnObjectGraph()
    {
        AssertBytesAndRoundtrip(new GenShapeHolder { Shape = new GenCircle { Radius = 5 }, Label = "held" });
        AssertBytesAndRoundtrip(new GenShapeHolder { Shape = null, Label = null });
    }

    [Fact]
    public void UnknownTag_SkipsPayloadAndYieldsNull()
    {
        // fixarray(2) [99, nil]: an unregistered case tag from a "newer" writer
        var bytes = new byte[] { 0x92, 0x63, 0xc0 };
        Assert.Null(V4.Deserialize<IGenShape>(bytes));
    }

    [Fact]
    public void UnregisteredRuntimeType_Throws()
    {
        // v4 deviation from v3's silent nil: an untagged runtime type is the writer's own
        // declaration bug, so it throws instead of silently losing the value
        var exception = Assert.Throws<MessagePackSerializationException>(
            () => V4.Serialize<IGenShape>(new GenUnlistedShape()));
        Assert.Contains("GenUnlistedShape", exception.Message);
    }

    [Fact]
    public void InvalidHeader_Throws()
    {
        // fixarray(3) is not a union frame
        Assert.Throws<MessagePackSerializationException>(
            () => V4.Deserialize<IGenShape>(new byte[] { 0x93, 0x00, 0xc0, 0xc0 }));
    }

    [Fact]
    public void ClosedUnionRoot_Roundtrips()
    {
        // a fully tagged closed hierarchy: MsgPack106 stays silent (this file compiling IS the
        // assertion) and the closed root serializes like any abstract union root
        var back = V4.Deserialize<GenClosedVehicle>(V4.Serialize<GenClosedVehicle>(new GenClosedTruck { Load = 9 }));
        Assert.Equal(9, Assert.IsType<GenClosedTruck>(back).Load);
        Assert.IsType<GenClosedBike>(V4.Deserialize<GenClosedVehicle>(V4.Serialize<GenClosedVehicle>(new GenClosedBike { Brand = "b" })));
    }

    [Fact]
    public void GenericUnionTagForm_SameWireAsTypeofForm()
    {
        AssertBytesAndRoundtrip<IGenVehicle>(new GenTruck { Load = 10 });
        AssertBytesAndRoundtrip<IGenVehicle>(new GenBike { Brand = "b" });
        AssertBytesAndRoundtrip<IGenVehicle?>(null);

        Assert.IsType<GenTruck>(V4.Deserialize<IGenVehicle>(V4.Serialize<IGenVehicle>(new GenTruck { Load = 1 })));
        Assert.IsType<GenBike>(V4.Deserialize<IGenVehicle>(V4.Serialize<IGenVehicle>(new GenBike { Brand = "x" })));
    }
}

// v4 declares [MessagePackObject] + [UnionTag] (discovery is driven by the former);
// the v3 attributes on the same base keep the oracle serializing the hierarchy
[V3::MessagePack.Union(0, typeof(GenCircle))]
[V3::MessagePack.Union(1, typeof(GenRectangle))]
[MessagePackObject]
[UnionTag(typeof(GenCircle), 0)]
[UnionTag(typeof(GenRectangle), 1)]
public interface IGenShape
{
}

[V3::MessagePack.MessagePackObject]
public class GenCircle : IGenShape
{
    [V3::MessagePack.Key(0)] public double Radius { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenRectangle : IGenShape
{
    [V3::MessagePack.Key(0)] public double Width { get; set; }
    [V3::MessagePack.Key(1)] public double Height { get; set; }
}

public class GenUnlistedShape : IGenShape
{
}

[V3::MessagePack.Union(0, typeof(GenDog))]
[V3::MessagePack.Union(1, typeof(GenCat))]
[MessagePackObject]
[UnionTag(typeof(GenDog), 0)]
[UnionTag(typeof(GenCat), 1)]
public abstract class GenAnimal
{
}

[V3::MessagePack.MessagePackObject]
public class GenDog : GenAnimal
{
    [V3::MessagePack.Key(0)] public string? Name { get; set; }
    [V3::MessagePack.Key(1)] public int BarkCount { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenCat : GenAnimal
{
    [V3::MessagePack.Key(0)] public string? Name { get; set; }
    [V3::MessagePack.Key(1)] public int Lives { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenShapeHolder
{
    [V3::MessagePack.Key(0)] public IGenShape? Shape { get; set; }
    [V3::MessagePack.Key(1)] public string? Label { get; set; }
}

// UnionTag<TCaseType> (the typed form) mixed with the (typeof, tag) shape on one root:
// both must land in the same case list and the same wire as v3
[V3::MessagePack.Union(0, typeof(GenTruck))]
[V3::MessagePack.Union(1, typeof(GenBike))]
[MessagePackObject]
[UnionTag<GenTruck>(0)]
[UnionTag(typeof(GenBike), 1)]
public interface IGenVehicle
{
}

[V3::MessagePack.MessagePackObject]
public class GenTruck : IGenVehicle
{
    [V3::MessagePack.Key(0)] public int Load { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class GenBike : IGenVehicle
{
    [V3::MessagePack.Key(0)] public string? Brand { get; set; }
}

// real `closed` keyword (lowers to [IsClosedType] via the polyfill): the complete derived
// set is knowable, and every concrete case is tagged — MsgPack106 enforces exactly this
[MessagePackObject]
[UnionTag<GenClosedTruck>(0)]
[UnionTag<GenClosedBike>(1)]
public closed class GenClosedVehicle
{
}

[MessagePackObject]
public class GenClosedTruck : GenClosedVehicle
{
    [Key(0)] public int Load { get; set; }
}

[MessagePackObject]
public class GenClosedBike : GenClosedVehicle
{
    [Key(0)] public string? Brand { get; set; }
}
