extern alias V3;
using MessagePack;
using MessagePack.Formatters;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;

namespace MessagePack.Tests;

// the payload types below are deliberately v3-annotated and served by the reflection
// tier at runtime, which the compile-time coverage analyzer cannot see
#pragma warning disable MsgPack108

// Runtime [Union] polymorphism (ReflectionUnionFormatter): the types below are annotated
// ONLY with the v3 MessagePack.Annotations attributes - exactly what a pre-built v3 DLL
// carries and the scenario this tier exists for. The source generator never sees them
// (v3's UnionAttribute is not its trigger), so every roundtrip here exercises the
// runtime tier, byte-compared against the genuine v3 oracle.
public class ReflectionUnionTests
{
    static readonly V3::MessagePack.MessagePackSerializerOptions OracleStandard = V3::MessagePack.MessagePackSerializerOptions.Standard;

    [Fact]
    public void V3AnnotatedUnion_MatchesOracle_BothDirections()
    {
        V3Shape[] values = [new V3Circle { Radius = 2.5 }, new V3Rect { W = 3, H = 4 }];
        foreach (V3Shape value in values)
        {
            var ours = V4.Serialize<V3Shape>(value);
            Assert.Equal(Oracle.Serialize<V3Shape>(value, OracleStandard), ours);

            Assert.Equal(value.GetType(), V4.Deserialize<V3Shape>(ours)!.GetType());
            Assert.Equal(value.GetType(), V4.Deserialize<V3Shape>(Oracle.Serialize<V3Shape>(value, OracleStandard))!.GetType());
            Assert.Equal(value.GetType(), Oracle.Deserialize<V3Shape>(ours, OracleStandard).GetType());
        }

        Assert.Equal(Oracle.Serialize<V3Shape?>(null, OracleStandard), V4.Serialize<V3Shape?>(null));
    }

    [Fact]
    public void InterfaceRoot_WorksToo()
    {
        var bin = V4.Serialize<IV3Message>(new V3TextMessage { Text = "hi" });
        Assert.Equal(Oracle.Serialize<IV3Message>(new V3TextMessage { Text = "hi" }, OracleStandard), bin);
        Assert.Equal("hi", Assert.IsType<V3TextMessage>(V4.Deserialize<IV3Message>(bin)).Text);
    }

    [Fact]
    public void UntaggedRuntimeType_WritesNil_LikeV3()
    {
        // version-skew tolerance is this tier's point: v3 wrote nil for a runtime type
        // outside the declared cases (the source-generated formatter throws instead)
        var untagged = new V3UntaggedShape();
        Assert.Equal(Oracle.Serialize<V3Shape>(untagged, OracleStandard), V4.Serialize<V3Shape>(untagged));
        Assert.Equal(new byte[] { 0xC0 }, V4.Serialize<V3Shape>(untagged));
    }

    [Fact]
    public void UnknownTag_SkipsAndYieldsNull()
    {
        byte[] payload = [0x92, 0x63, 0x81, 0xA1, (byte)'x', 0x01]; // [99, {"x":1}]
        Assert.Null(V4.Deserialize<V3Shape>(payload));
    }

    [Fact]
    public void AotChain_DoesNotClaim()
    {
        Assert.ThrowsAny<Exception>(() => V4.Serialize<V3Shape>(new V3Circle { Radius = 1 }, V4Options.DefaultAot));
    }

    [Fact]
    public void MigrationAliasAttribute_ReadsAsV4ThroughTheBaseChain()
    {
        // the root is annotated with the local MessagePack.UnionAttribute alias
        // (UnionMigrationAliasAttribute.cs): v3's name, no Key property, [UnionTag] base.
        // The v3-name duck read must fall through to the base chain, not throw.
        var bin = V4.Serialize<AliasShape>(new AliasCircle { Radius = 1.5 });
        Assert.Equal(0x92, bin[0]); // fixarray(2) union envelope
        Assert.Equal(1.5, Assert.IsType<AliasCircle>(V4.Deserialize<AliasShape>(bin)).Radius);
    }

    [Fact]
    public void UnionRoot_BeatsTypelessTail()
    {
        // under WithTypeless the interface/abstract tail must NOT wrap a union root in the
        // typeless envelope: the reflection tier claims it first, same order as v3
#pragma warning disable CS0618 // the obsolete gate is the point: LoadAnyType is the v3-shaped typeless tier under test
        var typeless = new V4Options(new MessagePackFormatterResolver(
            [MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())]));
#pragma warning restore CS0618
        var bin = V4.Serialize<V3Shape>(new V3Circle { Radius = 9 }, typeless);
        Assert.Equal(0x92, bin[0]); // fixarray(2) union envelope, not ext100
        Assert.IsType<V3Circle>(V4.Deserialize<V3Shape>(bin, typeless));
    }
}

// v3 Annotations only: [Union] on the roots, [MessagePackObject]/[Key] on the cases
[V3::MessagePack.Union(0, typeof(V3Circle))]
[V3::MessagePack.Union(1, typeof(V3Rect))]
public abstract class V3Shape
{
}

[V3::MessagePack.MessagePackObject]
public class V3Circle : V3Shape
{
    [V3::MessagePack.Key(0)]
    public double Radius { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class V3Rect : V3Shape
{
    [V3::MessagePack.Key(0)]
    public int W { get; set; }

    [V3::MessagePack.Key(1)]
    public int H { get; set; }
}

[V3::MessagePack.MessagePackObject]
public class V3UntaggedShape : V3Shape
{
}

[V3::MessagePack.Union(0, typeof(V3TextMessage))]
public interface IV3Message
{
}

[V3::MessagePack.MessagePackObject(true)]
public class V3TextMessage : IV3Message
{
    public string? Text { get; set; }
}

// migration-alias root: [Union] spelling, [UnionTag] semantics (no [MessagePackObject],
// so the source generator stays out and the runtime tier serves the root). MsgPack103
// steers UnionTag-derived annotations toward the generator; staying on the runtime tier
// is this type's point, so the steering is suppressed.
#pragma warning disable MsgPack103
[MessagePack.Union(0, typeof(AliasCircle))]
public abstract class AliasShape
{
}
#pragma warning restore MsgPack103

[MessagePackObject]
public class AliasCircle : AliasShape
{
    [Key(0)]
    public double Radius { get; set; }
}
