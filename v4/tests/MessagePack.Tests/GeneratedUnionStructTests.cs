using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// struct unions — C# union declarations and hand-written IUnion structs — on the v3 union
// wire: fixarray(2) [int tag, payload]. nil round-trips against default(TUnion) (Value is
// null), covering null, an untagged held case, and an unknown tag alike. No v3 oracle
// exists for these roots, so wire expectations are hand-assembled from the member payload.
public class GeneratedUnionStructTests
{
    [Fact]
    public void DeclaredUnion_WireShape_TagThenMemberPayload()
    {
        UPet pet = new UCat { Name = "tama" };
        var expected = new byte[] { 0x92, 0x00 }.Concat(V4.Serialize(new UCat { Name = "tama" })).ToArray();
        Assert.Equal(expected, V4.Serialize(pet));
    }

    [Fact]
    public void DeclaredUnion_RoundtripsEachCase()
    {
        var cat = V4.Deserialize<UPet>(V4.Serialize<UPet>(new UCat { Name = "tama" }));
        Assert.Equal("tama", Assert.IsType<UCat>(cat.Value).Name);

        var dog = V4.Deserialize<UPet>(V4.Serialize<UPet>(new UDog { Bark = 3 }));
        Assert.Equal(3, Assert.IsType<UDog>(dog.Value).Bark);
    }

    [Fact]
    public void DefaultUnion_WritesNil_AndNilReadsBackAsDefault()
    {
        Assert.Equal(new byte[] { 0xc0 }, V4.Serialize(default(UPet)));
        Assert.Null(V4.Deserialize<UPet>(new byte[] { 0xc0 }).Value);
    }

    [Fact]
    public void UntaggedHeldCase_Throws()
    {
        // UBird is a declared case of the union but carries no [UnionTag]: writing it is
        // a declaration bug, so it throws (nil stays reserved for a genuinely empty union)
        UPet pet = new UBird();
        var exception = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(pet));
        Assert.Contains("UBird", exception.Message);
    }

    [Fact]
    public void NonBoxingUnion_UntaggedHeldCase_Throws_DefaultStaysNil()
    {
        // the non-boxing dispatch reads Value only on the miss path, and still separates
        // "no held value" (nil) from "held but untagged" (throw)
        var exception = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(new UIntOrText("x")));
        Assert.Contains("String", exception.Message);
        Assert.Equal(new byte[] { 0xc0 }, V4.Serialize(default(UIntOrText)));
    }

    [Fact]
    public void UnknownTag_SkipsPayloadAndYieldsDefault()
    {
        // fixarray(2) [99, nil]: an unregistered case tag from a "newer" writer
        Assert.Null(V4.Deserialize<UPet>(new byte[] { 0x92, 0x63, 0xc0 }).Value);
    }

    [Fact]
    public void CustomIUnionStruct_RidesTheSameWire()
    {
        var bytes = V4.Serialize(new UCustomUnion(new UDog { Bark = 7 }));
        var expected = new byte[] { 0x92, 0x01 }.Concat(V4.Serialize(new UDog { Bark = 7 })).ToArray();
        Assert.Equal(expected, bytes);
        Assert.Equal(7, Assert.IsType<UDog>(V4.Deserialize<UCustomUnion>(bytes).Value).Bark);
    }

    [Fact]
    public void UnionMember_InsideAnObjectGraph()
    {
        var holder = new UPetHolder { Pet = new UCat { Name = "n" }, Count = 2 };
        var back = V4.Deserialize<UPetHolder>(V4.Serialize(holder))!;
        Assert.Equal("n", Assert.IsType<UCat>(back.Pet.Value).Name);
        Assert.Equal(2, back.Count);
    }

    [Fact]
    public void GenericDeclaredUnion_UnboundTypeofTag_Roundtrips()
    {
        GOption<int> some = new GSome<int> { Item = 42 };
        var bytes = V4.Serialize(some);
        var expected = new byte[] { 0x92, 0x01 }.Concat(V4.Serialize(new GSome<int> { Item = 42 })).ToArray();
        Assert.Equal(expected, bytes);
        Assert.Equal(42, Assert.IsType<GSome<int>>(V4.Deserialize<GOption<int>>(bytes).Value).Item);

        GOption<int> none = new GNone();
        Assert.IsType<GNone>(V4.Deserialize<GOption<int>>(V4.Serialize(none)).Value);
    }

    [Fact]
    public void GenericUnion_TypeParameterCase_TaggedByName()
    {
        GOutcome<string> ok = "yes";
        var bytes = V4.Serialize(ok);
        var expected = new byte[] { 0x92, 0x00 }.Concat(V4.Serialize("yes")).ToArray();
        Assert.Equal(expected, bytes);
        Assert.Equal("yes", Assert.IsType<string>(V4.Deserialize<GOutcome<string>>(bytes).Value));

        GOutcome<string> failed = new GFailure { Message = "m" };
        Assert.Equal("m", Assert.IsType<GFailure>(V4.Deserialize<GOutcome<string>>(V4.Serialize(failed)).Value).Message);
    }

    [Fact]
    public void GenericUnion_AsMember_ResolvesThroughTheHarvestedInstantiation()
    {
        var holder = new GOptionHolder { Option = new GSome<int> { Item = 7 } };
        var back = V4.Deserialize<GOptionHolder>(V4.Serialize(holder))!;
        Assert.Equal(7, Assert.IsType<GSome<int>>(back.Option.Value).Item);
    }

    [Fact]
    public void NonBoxingUnion_ValueTypeCases_Roundtrip()
    {
        Assert.Equal(new byte[] { 0x92, 0x00, 0x2a }, V4.Serialize(new UIntOrBool(42)));
        Assert.Equal(new byte[] { 0x92, 0x01, 0xc3 }, V4.Serialize(new UIntOrBool(true)));
        Assert.Equal(new byte[] { 0xc0 }, V4.Serialize(default(UIntOrBool)));

        Assert.Equal(42, Assert.IsType<int>(V4.Deserialize<UIntOrBool>(V4.Serialize(new UIntOrBool(42))).Value));
        Assert.True(Assert.IsType<bool>(V4.Deserialize<UIntOrBool>(V4.Serialize(new UIntOrBool(true))).Value));
        Assert.Null(V4.Deserialize<UIntOrBool>(new byte[] { 0xc0 }).Value);
    }

    [Fact]
    public void ProviderUnion_ClassRoot_Roundtrip()
    {
        var bytes = V4.Serialize(UResult.IUnionMembers.Create(new UCat { Name = "ok" }));
        var expected = new byte[] { 0x92, 0x00 }.Concat(V4.Serialize(new UCat { Name = "ok" })).ToArray();
        Assert.Equal(expected, bytes);

        var back = V4.Deserialize<UResult>(bytes)!;
        Assert.Equal("ok", Assert.IsType<UCat>(((UResult.IUnionMembers)back).Value).Name);

        Assert.Equal(new byte[] { 0xc0 }, V4.Serialize<UResult?>(null));
        Assert.Null(V4.Deserialize<UResult?>(new byte[] { 0xc0 }));
    }
}

[MessagePackObject]
public class UCat
{
    [Key(0)] public string? Name { get; set; }
}

[MessagePackObject]
public class UDog
{
    [Key(0)] public int Bark { get; set; }
}

public class UBird
{
}

// UBird stays untagged ON PURPOSE (the runtime-throw tests need it); the pragma is the
// sanctioned escape hatch for a deliberately unserialized case
#pragma warning disable MsgPack107
[MessagePackObject]
[UnionTag<UCat>(0)]
[UnionTag(typeof(UDog), 1)]
public union UPet(UCat, UDog, UBird);
#pragma warning restore MsgPack107

[MessagePackObject]
[UnionTag<UCat>(0)]
[UnionTag<UDog>(1)]
public readonly struct UCustomUnion : System.Runtime.CompilerServices.IUnion
{
    readonly object? value;

    public UCustomUnion(object? value) => this.value = value;

    public object? Value => value;
}

[MessagePackObject]
public class UPetHolder
{
    [Key(0)] public UPet Pet { get; set; }
    [Key(1)] public int Count { get; set; }
}

// hand-written non-boxing pattern union ([Union] marker, no IUnion interface): value-type
// cases dispatch through TryGetValue, so serialization never boxes the held int/bool
[System.Runtime.CompilerServices.Union]
[MessagePackObject]
[UnionTag(typeof(int), 0)]
[UnionTag(typeof(bool), 1)]
public readonly struct UIntOrBool
{
    readonly bool isBool;
    readonly int rawValue;
    readonly bool hasValue;

    public UIntOrBool(int value)
    {
        this.isBool = false;
        this.rawValue = value;
        this.hasValue = true;
    }

    public UIntOrBool(bool value)
    {
        this.isBool = true;
        this.rawValue = value ? 1 : 0;
        this.hasValue = true;
    }

    public object? Value => !this.hasValue ? null : this.isBool ? this.rawValue == 1 : this.rawValue;

    public bool TryGetValue(out int value)
    {
        value = this.rawValue;
        return this.hasValue && !this.isBool;
    }

    public bool TryGetValue(out bool value)
    {
        value = this.isBool && this.rawValue == 1;
        return this.hasValue && this.isBool;
    }
}

// generic union declarations: the generic case rides an UNBOUND typeof (closed over the
// root's parameters via the declaration's own constructor), a bare type-parameter case is
// tagged by its name — typeof cannot express one
[MessagePackObject]
public class GNone
{
}

[MessagePackObject]
public class GSome<T>
{
    [Key(0)] public T? Item { get; set; }
}

[MessagePackObject]
[UnionTag(typeof(GNone), 0)]
[UnionTag(typeof(GSome<>), 1)]
public union GOption<T>(GNone, GSome<T>);

[MessagePackObject]
public class GFailure
{
    [Key(0)] public string? Message { get; set; }
}

[MessagePackObject]
[UnionTag("T", 0)]
[UnionTag(typeof(GFailure), 1)]
public union GOutcome<T>(T, GFailure);

[MessagePackObject]
public class GOptionHolder
{
    [Key(0)] public GOption<int> Option { get; set; }
}

// non-boxing union whose string case is deliberately untagged: TryGetValue(int) misses,
// and the cold path must distinguish an empty union (nil) from an untagged value (throw)
#pragma warning disable MsgPack107
[System.Runtime.CompilerServices.Union]
[MessagePackObject]
[UnionTag(typeof(int), 0)]
public readonly struct UIntOrText
{
    readonly object? heldValue;

    public UIntOrText(int value) => this.heldValue = value;

    public UIntOrText(string value) => this.heldValue = value;

    public object? Value => this.heldValue;

    public bool TryGetValue(out int value)
    {
        if (this.heldValue is int held)
        {
            value = held;
            return true;
        }
        value = 0;
        return false;
    }
}

#pragma warning restore MsgPack107

// provider-based class union (the spec's Result<T> shape, non-generic): union members
// live on the nested IUnionMembers interface, Value is explicitly implemented
[System.Runtime.CompilerServices.Union]
[MessagePackObject]
[UnionTag(typeof(UCat), 0)]
[UnionTag(typeof(UDog), 1)]
public record class UResult : UResult.IUnionMembers
{
    object? heldValue;

    public interface IUnionMembers
    {
        public static UResult Create(UCat value) => new() { heldValue = value };

        public static UResult Create(UDog value) => new() { heldValue = value };

        public object? Value { get; }
    }

    object? IUnionMembers.Value => this.heldValue;
}
