using MessagePack;

namespace MessagePack.Tests.NetFx;

// [MessagePackObject(EnumAsString = true)] on the netstandard2.0 asset: the initializer interfaces without the ref
// struct allowance, the generated trees and name formatters over the Compatible buffers, and the reflection tier's tree
// creation through the factories' Type-based overload (the generic one is the net9+ path). The wire itself is asserted by
// the main test project.
public class DownlevelEnumAsStringTests
{
    const string Json = """{"Single":"Blue","List":["Red","Blue"],"ByKey":{"Red":1},"Nested":{"x":["Blue"]},"Items":["Red"],"Inner":{"Color":2},"Access":"Read, write-access"}""";

    static T Fill<T>()
        where T : DownlevelEnumAsStringPoco, new() => new T
    {
        Single = DownlevelShade.Blue,
        List = [DownlevelShade.Red, DownlevelShade.Blue],
        ByKey = new() { [DownlevelShade.Red] = 1 },
        Nested = new() { ["x"] = [DownlevelShade.Blue] },
        Items = Iterate(),
        Inner = new() { Color = DownlevelShade.Blue },
        Access = DownlevelAccess.Read | DownlevelAccess.Write,
    };

    static IEnumerable<DownlevelShade> Iterate()
    {
        yield return DownlevelShade.Red;
    }

    [Fact]
    public void Generated_WritesByName()
    {
        var bytes = MessagePackSerializer.Serialize(Fill<DownlevelEnumAsStringPoco>());
        Assert.Equal(Json, MessagePackSerializer.ConvertToJson(bytes));
        Assert.Equal(Json, MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(MessagePackSerializer.Deserialize<DownlevelEnumAsStringPoco>(bytes))));
        Assert.Equal("[2]", MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(new List<DownlevelShade> { DownlevelShade.Blue })));
    }

    [Fact]
    public void Reflection_WritesTheSameBytes()
    {
        var generated = MessagePackSerializer.Serialize(Fill<DownlevelEnumAsStringPoco>());
        var reflected = MessagePackSerializer.Serialize(Fill<DownlevelEnumAsStringPocoReflected>());
        Assert.Equal(generated, reflected);
        Assert.Equal(Json, MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(MessagePackSerializer.Deserialize<DownlevelEnumAsStringPocoReflected>(generated))));
    }
}

public enum DownlevelShade
{
    Red,
    Green,
    Blue,
}

// the generated name formatter's [Flags] rename translation, compiled against the netstandard2.0 surface
[Flags]
public enum DownlevelAccess
{
    None = 0,
    Read = 1,
    [System.Runtime.Serialization.EnumMember(Value = "write-access")]
    Write = 2,
}

[MessagePackObject(true)]
public class DownlevelEnumInner
{
    public DownlevelShade Color { get; set; }
}

[MessagePackObject(true, EnumAsString = true)]
public class DownlevelEnumAsStringPoco
{
    public DownlevelShade Single { get; set; }
    public List<DownlevelShade>? List { get; set; }
    public Dictionary<DownlevelShade, int>? ByKey { get; set; }
    public Dictionary<string, List<DownlevelShade>>? Nested { get; set; }
    public IEnumerable<DownlevelShade>? Items { get; set; }
    public DownlevelEnumInner? Inner { get; set; }
    public DownlevelAccess Access { get; set; }
}

[MessagePackObject(true, EnumAsString = true, SuppressSourceGeneration = true)]
public class DownlevelEnumAsStringPocoReflected : DownlevelEnumAsStringPoco
{
}
