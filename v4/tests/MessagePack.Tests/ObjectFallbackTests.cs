extern alias V3;
using MessagePack;
using MessagePack.Formatters;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;

namespace MessagePack.Tests;

// object-typed slots serialize by RUNTIME type (ObjectFallbackFormatter, the v3
// DynamicObjectTypeFallbackFormatter port) on the non-AOT chains. The oracle is v3's
// StandardResolver/ContractlessStandardResolver, whose object handling was exactly this
// fallback; byte identity is asserted wherever v4 has not ratified a different wire
// (boxed SCALARS keep v4's forced-width codes, so those assert type round-trip instead).
public class ObjectFallbackTests
{
    static readonly V4Options Contractless = new(new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless()]));

    static readonly V3::MessagePack.MessagePackSerializerOptions OracleStandard = V3::MessagePack.MessagePackSerializerOptions.Standard;
    static readonly V3::MessagePack.MessagePackSerializerOptions OracleContractless =
        V3::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(V3::MessagePack.Resolvers.ContractlessStandardResolver.Instance);

    [Fact]
    public void AnnotatedPoco_BoxedInObjectSlot_MatchesOracle()
    {
        object boxed = new FallbackKeyed { X = 42, Name = "abc" };
        Assert.Equal(Oracle.Serialize(boxed, OracleStandard), V4.Serialize(boxed));
    }

    [Fact]
    public void Contractless_PocoBehindObjectMember_MatchesOracle()
    {
        var holder = new FallbackHolder { V = new FallbackAddress { Street = "St", Number = 999 } };
        Assert.Equal(Oracle.Serialize(holder, OracleContractless), V4.Serialize(holder, Contractless));

        // and the v3 asymmetry: no type tag on the wire, so it comes back as a map
        var back = Assert.IsType<FallbackHolder>(V4.Deserialize<FallbackHolder>(V4.Serialize(holder, Contractless), Contractless));
        Assert.IsType<Dictionary<object, object>>(back.V);
    }

    [Fact]
    public void BoxedCollections_WriteThroughTypedFormatters_MatchingOracle()
    {
        // v3's fallback routed dictionaries/collections to their typed formatters (compact
        // integers), NOT the primitive mini-protocol; these are byte-identical to v3
        object[] values =
        [
            new Dictionary<string, int> { ["a"] = 999, ["b"] = 3 },
            new List<int> { 1, 999, 70000 },
            new[] { 1, 999 },
            new object[] { "x", new[] { 999 } },
        ];
        foreach (var value in values)
        {
            Assert.Equal(Oracle.Serialize(value, OracleStandard), V4.Serialize(value));
        }
    }

    [Fact]
    public void BareObject_WritesEmptyMap_MatchingOracle()
    {
        object bare = new object();
        var bytes = V4.Serialize(bare);
        Assert.Equal(new byte[] { 0x80 }, bytes);
        Assert.Equal(Oracle.Serialize(bare, OracleStandard), bytes);
    }

    [Fact]
    public void BoxedDecimal_WritesThroughDecimalFormatter_MatchingOracle()
    {
        object boxed = 1.5m; // no arm in the primitive mini-protocol; v3 dispatched it too
        Assert.Equal(Oracle.Serialize(boxed, OracleStandard), V4.Serialize(boxed));
        Assert.Equal(boxed, V4.Deserialize<decimal>(V4.Serialize(boxed)));
    }

    [Fact]
    public void BoxedScalars_KeepForcedWidthWire()
    {
        // the ratified v4 divergence survives the fallback: scalar runtime types still go
        // through PrimitiveObjectFormatter's forced-width writers and round-trip their type
        Assert.Equal(new byte[] { 0xD2, 0x00, 0x00, 0x03, 0xE7 }, V4.Serialize<object>(999));
        Assert.IsType<ushort>(V4.Deserialize<object>(V4.Serialize<object>((ushort)5)));
    }

    [Fact]
    public void AotChain_KeepsClosedTable()
    {
        // v3 drew the same line: AvoidDynamicCode fell back to PrimitiveObjectResolver
        object boxed = new FallbackAddress { Street = "St" };
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(boxed, V4Options.DefaultAot));
        Assert.Contains("Not supported primitive object resolver", ex.Message);
    }
}

[MessagePackObject]
[V3::MessagePack.MessagePackObject]
public class FallbackKeyed
{
    [Key(0)]
    [V3::MessagePack.Key(0)]
    public int X { get; set; }

    [Key(1)]
    [V3::MessagePack.Key(1)]
    public string? Name { get; set; }
}

public class FallbackAddress
{
    public string? Street { get; set; }

    public int Number { get; set; }
}

public class FallbackHolder
{
    public object? V { get; set; }
}
