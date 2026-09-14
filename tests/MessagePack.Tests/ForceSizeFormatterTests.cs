extern alias V3;
using MessagePack.Formatters;
using Oracle = V3::MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// ForceSizeFormatters.cs: v3-name-compatible force-size formatters. Every case is
// wire-compared against MessagePack v3's own Force*BlockFormatter instances and
// roundtripped through our reader (which stays lenient about integer formats).
public partial class ForceSizeFormatterTests
{
    // the chain-composition consumption model: ForceSizeFormatterFactory is in no default
    // chain, so it is placed ahead of Default to force every integer width in the graph
    static readonly MessagePackSerializerOptions options =
        new(new MessagePackFormatterResolver([ForceSizeFormatterFactory.Instance, MessagePackFormatterFactory.Default]));

    static readonly V3::MessagePack.MessagePackSerializerOptions oracleOptions =
        V3::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(
            V3::MessagePack.Resolvers.CompositeResolver.Create(
                new V3::MessagePack.Formatters.IMessagePackFormatter[]
                {
                    V3::MessagePack.Formatters.ForceSByteBlockFormatter.Instance,
                    V3::MessagePack.Formatters.ForceByteBlockFormatter.Instance,
                    V3::MessagePack.Formatters.ForceInt16BlockFormatter.Instance,
                    V3::MessagePack.Formatters.ForceUInt16BlockFormatter.Instance,
                    V3::MessagePack.Formatters.ForceInt32BlockFormatter.Instance,
                    V3::MessagePack.Formatters.ForceUInt32BlockFormatter.Instance,
                    V3::MessagePack.Formatters.ForceInt64BlockFormatter.Instance,
                    V3::MessagePack.Formatters.ForceUInt64BlockFormatter.Instance,
                    V3::MessagePack.Formatters.NullableForceSByteBlockFormatter.Instance,
                    V3::MessagePack.Formatters.NullableForceByteBlockFormatter.Instance,
                    V3::MessagePack.Formatters.NullableForceInt16BlockFormatter.Instance,
                    V3::MessagePack.Formatters.NullableForceUInt16BlockFormatter.Instance,
                    V3::MessagePack.Formatters.NullableForceInt32BlockFormatter.Instance,
                    V3::MessagePack.Formatters.NullableForceUInt32BlockFormatter.Instance,
                    V3::MessagePack.Formatters.NullableForceInt64BlockFormatter.Instance,
                    V3::MessagePack.Formatters.NullableForceUInt64BlockFormatter.Instance,
                    V3::MessagePack.Formatters.ForceSByteBlockArrayFormatter.Instance,
                    V3::MessagePack.Formatters.ForceInt16BlockArrayFormatter.Instance,
                    V3::MessagePack.Formatters.ForceUInt16BlockArrayFormatter.Instance,
                    V3::MessagePack.Formatters.ForceInt32BlockArrayFormatter.Instance,
                    V3::MessagePack.Formatters.ForceUInt32BlockArrayFormatter.Instance,
                    V3::MessagePack.Formatters.ForceInt64BlockArrayFormatter.Instance,
                    V3::MessagePack.Formatters.ForceUInt64BlockArrayFormatter.Instance,
                },
                new V3::MessagePack.IFormatterResolver[] { V3::MessagePack.Resolvers.StandardResolver.Instance }));

    static void AssertForced<T>(T value, byte expectedCode, int expectedLength)
    {
        var ours = MessagePackSerializer.Serialize(value, options);
        Assert.Equal(expectedLength, ours.Length);
        Assert.Equal(expectedCode, ours[0]);
        Assert.Equal(Oracle.Serialize(value, oracleOptions), ours);
        Assert.Equal(value, MessagePackSerializer.Deserialize<T>(ours, options));
    }

    [Fact]
    public void Scalars_AlwaysNamedFormat_MatchOracleAndRoundtrip()
    {
        foreach (var v in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }) AssertForced(v, 0xd0, 2);
        foreach (var v in new byte[] { 0, 1, byte.MaxValue }) AssertForced(v, 0xcc, 2);
        foreach (var v in new short[] { 0, 1, -1, short.MinValue, short.MaxValue }) AssertForced(v, 0xd1, 3);
        foreach (var v in new ushort[] { 0, 1, ushort.MaxValue }) AssertForced(v, 0xcd, 3);
        foreach (var v in new int[] { 0, 1, -1, int.MinValue, int.MaxValue }) AssertForced(v, 0xd2, 5);
        foreach (var v in new uint[] { 0, 1, uint.MaxValue }) AssertForced(v, 0xce, 5);
        foreach (var v in new long[] { 0, 1, -1, long.MinValue, long.MaxValue }) AssertForced(v, 0xd3, 9);
        foreach (var v in new ulong[] { 0, 1, ulong.MaxValue }) AssertForced(v, 0xcf, 9);
    }

    [Fact]
    public void Nullables_ValueForcedNullIsNil_MatchOracleAndRoundtrip()
    {
        AssertForced<sbyte?>(-1, 0xd0, 2);
        AssertForced<byte?>(1, 0xcc, 2);
        AssertForced<short?>(-1, 0xd1, 3);
        AssertForced<ushort?>(1, 0xcd, 3);
        AssertForced<int?>(-1, 0xd2, 5);
        AssertForced<uint?>(1, 0xce, 5);
        AssertForced<long?>(-1, 0xd3, 9);
        AssertForced<ulong?>(1, 0xcf, 9);

        AssertForced<sbyte?>(null, 0xc0, 1);
        AssertForced<byte?>(null, 0xc0, 1);
        AssertForced<short?>(null, 0xc0, 1);
        AssertForced<ushort?>(null, 0xc0, 1);
        AssertForced<int?>(null, 0xc0, 1);
        AssertForced<uint?>(null, 0xc0, 1);
        AssertForced<long?>(null, 0xc0, 1);
        AssertForced<ulong?>(null, 0xc0, 1);
    }

    static void AssertForcedArray<T>(T[]? value)
    {
        var ours = MessagePackSerializer.Serialize(value, options);
        Assert.Equal(Oracle.Serialize(value, oracleOptions), ours);
        Assert.Equal(value, MessagePackSerializer.Deserialize<T[]?>(ours, options));
    }

    [Fact]
    public void Arrays_EveryElementForced_MatchOracleAndRoundtrip()
    {
        // no byte[] case: v3 has no ForceByteBlockArrayFormatter (byte[] is bin), so neither do we
        AssertForcedArray<sbyte>([sbyte.MinValue, 0, sbyte.MaxValue]);
        AssertForcedArray<short>([short.MinValue, 0, short.MaxValue]);
        AssertForcedArray<ushort>([0, 1, ushort.MaxValue]);
        AssertForcedArray<int>([int.MinValue, 0, int.MaxValue]);
        AssertForcedArray<uint>([0, 1, uint.MaxValue]);
        AssertForcedArray<long>([long.MinValue, 0, long.MaxValue]);
        AssertForcedArray<ulong>([0, 1, ulong.MaxValue]);

        AssertForcedArray<int>([]);
        AssertForcedArray<int>(null);

        // 3 int32 elements = fixarray header + 3 x 5 bytes; spot-check the layout claim
        var wire = MessagePackSerializer.Serialize(new[] { 1, 2, 3 }, options);
        Assert.Equal(16, wire.Length);
        Assert.Equal(0x93, wire[0]);
        Assert.Equal(0xd2, wire[1]);
    }

    [Fact]
    public void PerMember_FactoryAttribute_MatchesV3PerFieldFormatters()
    {
        // v3 named the width per member ([MessagePackFormatter(typeof(ForceInt32BlockFormatter))]);
        // v4 puts the one factory on the member and the type dispatch picks the width from the
        // member type, scalar, nullable and array alike. Untouched members keep the compact format.
        var v4 = new ForcedMembersPoco { Value = 1, Wide = 1, MaybeValue = 1, Values = [1, 2], Compact = 1 };
        var v3 = new V3ForcedMembersPoco { Value = 1, Wide = 1, MaybeValue = 1, Values = [1, 2], Compact = 1 };

        var ours = MessagePackSerializer.Serialize(v4);
        Assert.Equal(Oracle.Serialize(v3), ours);

        // [0x95, d2 00000001, d3 0000000000000001, d2 00000001, 92 d2..1 d2..2, 01]
        Assert.Equal(1 + 5 + 9 + 5 + 11 + 1, ours.Length);
        Assert.Equal(0xd2, ours[1]);
        Assert.Equal(0xd3, ours[6]);
        Assert.Equal(0xd2, ours[15]);
        Assert.Equal(0x92, ours[20]);
        Assert.Equal(0x01, ours[^1]);

        var back = MessagePackSerializer.Deserialize<ForcedMembersPoco>(ours)!;
        Assert.Equal(1, back.Value);
        Assert.Equal(1L, back.Wide);
        Assert.Equal(1, back.MaybeValue);
        Assert.Equal(new[] { 1, 2 }, back.Values);
        Assert.Equal(1, back.Compact);

        // null goes to nil through the nullable companion
        v4.MaybeValue = null;
        v3.MaybeValue = null;
        Assert.Equal(Oracle.Serialize(v3), MessagePackSerializer.Serialize(v4));
    }

    [Fact]
    public void PerMember_OpenFormatterForm_StillNamesTheV3Formatter()
    {
        // the v3 spelling with <,> appended keeps working next to the factory form
        var ours = MessagePackSerializer.Serialize(new OpenFormatterFormPoco { Value = 1 });
        Assert.Equal(new byte[] { 0x91, 0xd2, 0, 0, 0, 1 }, ours);
        Assert.Equal(1, MessagePackSerializer.Deserialize<OpenFormatterFormPoco>(ours)!.Value);
    }

    [Fact]
    public void LenientRead_CompactWireStillDeserializes()
    {
        // bytes produced by the DEFAULT chain (smallest-format encoding: fixint etc.)
        // must deserialize under the force options — force is a write-side stance only
        Assert.Equal(42, MessagePackSerializer.Deserialize<int>(MessagePackSerializer.Serialize(42), options));
        Assert.Equal((short)-5, MessagePackSerializer.Deserialize<short>(MessagePackSerializer.Serialize((short)-5), options));
        Assert.Equal((ulong)7, MessagePackSerializer.Deserialize<ulong>(MessagePackSerializer.Serialize((ulong)7), options));
        Assert.Equal(new long[] { 1, 2, 3 }, MessagePackSerializer.Deserialize<long[]>(MessagePackSerializer.Serialize(new long[] { 1, 2, 3 }), options));
    }
}

[MessagePackObject]
public class ForcedMembersPoco
{
    [Key(0)]
    [MessagePackFormatter(typeof(ForceSizeFormatterFactory))]
    public int Value { get; set; }

    [Key(1)]
    [MessagePackFormatter<ForceSizeFormatterFactory>]
    public long Wide { get; set; }

    [Key(2)]
    [MessagePackFormatter<ForceSizeFormatterFactory>]
    public int? MaybeValue { get; set; }

    [Key(3)]
    [MessagePackFormatter<ForceSizeFormatterFactory>]
    public int[]? Values { get; set; }

    [Key(4)]
    public int Compact { get; set; }
}

[MessagePackObject]
public class OpenFormatterFormPoco
{
    [Key(0)]
    [MessagePackFormatter(typeof(ForceInt32BlockFormatter<,>))]
    public int Value { get; set; }
}

// oracle only, serialized by v3; SuppressSourceGeneration keeps v4's generator (which matches
// the attribute by name) off it, and MsgPack105 is right that v3's non-generic formatter
// types are unusable from v4, which is exactly why the factory exists
#pragma warning disable MsgPack105
[V3::MessagePack.MessagePackObject(SuppressSourceGeneration = true)]
public class V3ForcedMembersPoco
{
    [V3::MessagePack.Key(0)]
    [V3::MessagePack.MessagePackFormatter(typeof(V3::MessagePack.Formatters.ForceInt32BlockFormatter))]
    public int Value { get; set; }

    [V3::MessagePack.Key(1)]
    [V3::MessagePack.MessagePackFormatter(typeof(V3::MessagePack.Formatters.ForceInt64BlockFormatter))]
    public long Wide { get; set; }

    [V3::MessagePack.Key(2)]
    [V3::MessagePack.MessagePackFormatter(typeof(V3::MessagePack.Formatters.NullableForceInt32BlockFormatter))]
    public int? MaybeValue { get; set; }

    [V3::MessagePack.Key(3)]
    [V3::MessagePack.MessagePackFormatter(typeof(V3::MessagePack.Formatters.ForceInt32BlockArrayFormatter))]
    public int[]? Values { get; set; }

    [V3::MessagePack.Key(4)]
    public int Compact { get; set; }
}
#pragma warning restore MsgPack105
