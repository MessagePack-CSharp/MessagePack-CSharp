using UltraMessagePack.Formatters;
using Oracle = MessagePack.MessagePackSerializer;

namespace UltraMessagePack.Tests;

// ForceSizeFormatters.cs: v3-name-compatible force-size formatters. Every case is
// wire-compared against MessagePack v3's own Force*BlockFormatter instances and
// roundtripped through our reader (which stays lenient about integer formats).
public partial class ForceSizeFormatterTests
{
    // the intended consumption model: not in any default chain, composed explicitly
    // (partial: FactoryBridgeGenerator supplies the Type-based CreateFormatter tier)
    sealed partial class ForceSizeFactory : MessagePackFormatterFactory
    {
        public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        {
            if (type == typeof(sbyte)) return new ForceSByteBlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(byte)) return new ForceByteBlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(short)) return new ForceInt16BlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(ushort)) return new ForceUInt16BlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(int)) return new ForceInt32BlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(uint)) return new ForceUInt32BlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(long)) return new ForceInt64BlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(ulong)) return new ForceUInt64BlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(sbyte?)) return new NullableForceSByteBlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(byte?)) return new NullableForceByteBlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(short?)) return new NullableForceInt16BlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(ushort?)) return new NullableForceUInt16BlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(int?)) return new NullableForceInt32BlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(uint?)) return new NullableForceUInt32BlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(long?)) return new NullableForceInt64BlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(ulong?)) return new NullableForceUInt64BlockFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(sbyte[])) return new ForceSByteBlockArrayFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(short[])) return new ForceInt16BlockArrayFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(ushort[])) return new ForceUInt16BlockArrayFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(int[])) return new ForceInt32BlockArrayFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(uint[])) return new ForceUInt32BlockArrayFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(long[])) return new ForceInt64BlockArrayFormatter<TWriteBuffer, TReadBuffer>();
            if (type == typeof(ulong[])) return new ForceUInt64BlockArrayFormatter<TWriteBuffer, TReadBuffer>();
            return null;
        }
    }

    static readonly MessagePackSerializerOptions options =
        new([new ForceSizeFactory(), MessagePackFormatterFactory.Default]);

    static readonly MessagePack.MessagePackSerializerOptions oracleOptions =
        MessagePack.MessagePackSerializerOptions.Standard.WithResolver(
            MessagePack.Resolvers.CompositeResolver.Create(
                new MessagePack.Formatters.IMessagePackFormatter[]
                {
                    MessagePack.Formatters.ForceSByteBlockFormatter.Instance,
                    MessagePack.Formatters.ForceByteBlockFormatter.Instance,
                    MessagePack.Formatters.ForceInt16BlockFormatter.Instance,
                    MessagePack.Formatters.ForceUInt16BlockFormatter.Instance,
                    MessagePack.Formatters.ForceInt32BlockFormatter.Instance,
                    MessagePack.Formatters.ForceUInt32BlockFormatter.Instance,
                    MessagePack.Formatters.ForceInt64BlockFormatter.Instance,
                    MessagePack.Formatters.ForceUInt64BlockFormatter.Instance,
                    MessagePack.Formatters.NullableForceSByteBlockFormatter.Instance,
                    MessagePack.Formatters.NullableForceByteBlockFormatter.Instance,
                    MessagePack.Formatters.NullableForceInt16BlockFormatter.Instance,
                    MessagePack.Formatters.NullableForceUInt16BlockFormatter.Instance,
                    MessagePack.Formatters.NullableForceInt32BlockFormatter.Instance,
                    MessagePack.Formatters.NullableForceUInt32BlockFormatter.Instance,
                    MessagePack.Formatters.NullableForceInt64BlockFormatter.Instance,
                    MessagePack.Formatters.NullableForceUInt64BlockFormatter.Instance,
                    MessagePack.Formatters.ForceSByteBlockArrayFormatter.Instance,
                    MessagePack.Formatters.ForceInt16BlockArrayFormatter.Instance,
                    MessagePack.Formatters.ForceUInt16BlockArrayFormatter.Instance,
                    MessagePack.Formatters.ForceInt32BlockArrayFormatter.Instance,
                    MessagePack.Formatters.ForceUInt32BlockArrayFormatter.Instance,
                    MessagePack.Formatters.ForceInt64BlockArrayFormatter.Instance,
                    MessagePack.Formatters.ForceUInt64BlockArrayFormatter.Instance,
                },
                new MessagePack.IFormatterResolver[] { MessagePack.Resolvers.StandardResolver.Instance }));

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
