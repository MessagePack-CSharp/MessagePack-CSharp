extern alias V3;
using System.Runtime.Serialization;
using SerializerFoundation;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// [DataContract]/[DataMember]/[IgnoreDataMember]: v3's DynamicObjectResolver honored them
// as an alternative annotation family, mpc did not — v4 keeps that split (the reflection
// tier and the Default chain's tail serve them, the generator stays out), byte-checked
// against the v3 oracle. One deliberate deviation: [IgnoreDataMember] is an [IgnoreMember]
// alias in BOTH tiers now, ending v3's dynamic-vs-codegen inconsistency.
public class DataContractTests
{
    static readonly MessagePack.MessagePackSerializerOptions reflectionOptions = new(
        new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

    static void AssertMatchesOracle<T>(T value)
    {
        var oracle = Oracle.Serialize(value);
        // the Default chain's reflection tail serves [DataContract] with no options work
        Assert.Equal(oracle, V4.Serialize(value));
        Assert.Equal(oracle, V4.Serialize(value, reflectionOptions));

        var back = V4.Deserialize<T>(oracle, reflectionOptions);
        Assert.Equal(oracle, Oracle.Serialize(back));
    }

    [Fact]
    public void OrderedMembers_SerializeAsArray()
    {
        AssertMatchesOracle(new DcOrdered { A = 1, B = "x" });
        // [1, "x"]: Order is the int key
        Assert.Equal(new byte[] { 0x92, 0x01, 0xA1, (byte)'x' }, V4.Serialize(new DcOrdered { A = 1, B = "x" }));
    }

    [Fact]
    public void NamedMembers_SerializeAsMap()
    {
        AssertMatchesOracle(new DcNamed { A = 1, B = "x" });
    }

    [Fact]
    public void BareDataMembers_MapByMemberName()
    {
        AssertMatchesOracle(new DcBare { A = 1, B = "x" });
    }

    [Fact]
    public void MemberWithoutDataMember_IsExcluded()
    {
        AssertMatchesOracle(new DcPartial { A = 1, B = "dropped" });
        // fixarray1 [1]: B carries no [DataMember]
        Assert.Equal(new byte[] { 0x91, 0x01 }, V4.Serialize(new DcPartial { A = 1, B = "dropped" }));
    }

    [Fact]
    public void MixedOrderAndName_Throws()
    {
        Assert.Throws<MessagePackSerializationException>(
            () => V4.Serialize(new DcMixed { A = 1, B = "x" }, reflectionOptions));
    }

    [Fact]
    public void Contractless_HonorsIgnoreMember()
    {
        var contractless = new MessagePack.MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(),
        ]));
        // {"A":1}: v3's DynamicContractlessObjectResolver honored [IgnoreMember] and
        // [IgnoreDataMember] (oracle-probed), and v4's contractless table now does too
        Assert.Equal(new byte[] { 0x81, 0xA1, (byte)'A', 0x01 }, V4.Serialize(new ContractlessSkipPoco { A = 1, Skip = "no" }, contractless));
    }

    [Fact]
    public void IgnoreDataMember_ExcludesInBothTiers()
    {
        var value = new DcIgnoreAlias { A = 1, Skip = "no" };
        var oracle = Oracle.Serialize(value);
        // {"A":1}: the generated formatter (Default chain) and the reflection tier both
        // treat [IgnoreDataMember] as [IgnoreMember]
        Assert.Equal(new byte[] { 0x81, 0xA1, (byte)'A', 0x01 }, oracle);
        Assert.Equal(oracle, V4.Serialize(value));
        Assert.Equal(oracle, V4.Serialize(value, reflectionOptions));
    }
}

[DataContract]
public class DcOrdered
{
    [DataMember(Order = 0)] public int A { get; set; }
    [DataMember(Order = 1)] public string? B { get; set; }
}

[DataContract]
public class DcNamed
{
    [DataMember(Name = "alpha")] public int A { get; set; }
    [DataMember(Name = "beta")] public string? B { get; set; }
}

[DataContract]
public class DcBare
{
    [DataMember] public int A { get; set; }
    [DataMember] public string? B { get; set; }
}

[DataContract]
public class DcPartial
{
    [DataMember(Order = 0)] public int A { get; set; }
    public string? B { get; set; }
}

[DataContract]
public class DcMixed
{
    [DataMember(Order = 0)] public int A { get; set; }
    [DataMember(Name = "beta")] public string? B { get; set; }
}

public sealed class ContractlessSkipPoco
{
    public int A { get; set; }
    [IgnoreMember] public string? Skip { get; set; }
}

// the V3-aliased attribute IS MessagePack.MessagePackObjectAttribute by full name, so the
// oracle's dynamic resolver AND v4's source generator both serve this one declaration
[V3::MessagePack.MessagePackObject(true)]
public class DcIgnoreAlias
{
    public int A { get; set; }
    [IgnoreDataMember] public string? Skip { get; set; }
}
