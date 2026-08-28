extern alias V3;
using V3::MessagePack.Resolvers;
using System.Runtime.Serialization;
using MessagePack.Formatters;
using Oracle = V3::MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

public class EnumAsStringFormatterTests
{
    // opt-in is chain composition: the enum-as-string factory BEFORE the default chain
    static readonly MessagePackSerializerOptions options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [new GenericEnumAsStringFormatterFactory(), MessagePackFormatterFactory.Default]));

    // the oracle is the v3 formatter this implementation was ported from
    static V3::MessagePack.MessagePackSerializerOptions OracleOptions(bool ignoreCase = false) =>
        V3::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                [
                    new V3::MessagePack.Formatters.EnumAsStringFormatter<Simple>(ignoreCase),
                    new V3::MessagePack.Formatters.EnumAsStringFormatter<Renamed>(ignoreCase),
                    new V3::MessagePack.Formatters.EnumAsStringFormatter<FlagsRenamed>(ignoreCase),
                ],
                [StandardResolver.Instance]));

    public enum Simple { A, B, C }

    public enum Renamed
    {
        [EnumMember(Value = "a")] Alpha,
        [EnumMember(Value = "b")] Beta,
        Gamma, // no rename: CLR name on the wire
    }

    [Flags]
    public enum FlagsRenamed
    {
        None = 0,
        [EnumMember(Value = "first")] One = 1,
        [EnumMember(Value = "second")] Two = 2,
        Four = 4,
    }

    static void AssertMatchesOracleAndRoundtrips<T>(T value)
    {
        var ours = MessagePackSerializer.Serialize(value, options);
        var oracle = Oracle.Serialize(value, OracleOptions());
        Assert.True(ours.AsSpan().SequenceEqual(oracle),
            $"bytes mismatch for {value}: ours={Convert.ToHexString(ours)} oracle={Convert.ToHexString(oracle)}");
        Assert.Equal(value, MessagePackSerializer.Deserialize<T>(ours, options));
        Assert.Equal(value, Oracle.Deserialize<T>(ours, OracleOptions()));
        Assert.Equal(value, MessagePackSerializer.Deserialize<T>(oracle, options));
    }

    [Fact]
    public void NamedValues_MatchV3OracleAndRoundtrip()
    {
        // wire format is the plain name as a str
        var bytes = MessagePackSerializer.Serialize(Simple.B, options);
        Assert.Equal(new byte[] { 0xA1, (byte)'B' }, bytes);

        foreach (var v in new[] { Simple.A, Simple.B, Simple.C })
        {
            AssertMatchesOracleAndRoundtrips(v);
        }
    }

    [Fact]
    public void EnumMemberRenames_MatchV3OracleAndRoundtrip()
    {
        // [EnumMember(Value)] overrides the serialized name
        Assert.Equal("a", System.Text.Encoding.UTF8.GetString(
            MessagePackSerializer.Serialize(Renamed.Alpha, options).AsSpan(1)));

        foreach (var v in new[] { Renamed.Alpha, Renamed.Beta, Renamed.Gamma })
        {
            AssertMatchesOracleAndRoundtrips(v);
        }
    }

    [Fact]
    public void FlagsCombinations_MatchV3OracleAndRoundtrip()
    {
        // combined flags serialize as ", "-joined names with renames applied per element
        Assert.Equal("first, second", System.Text.Encoding.UTF8.GetString(
            MessagePackSerializer.Serialize(FlagsRenamed.One | FlagsRenamed.Two, options).AsSpan(1)));

        foreach (var v in new[]
        {
            FlagsRenamed.None,
            FlagsRenamed.One,
            FlagsRenamed.One | FlagsRenamed.Two,
            FlagsRenamed.One | FlagsRenamed.Four, // renamed + non-renamed mix
            FlagsRenamed.One | FlagsRenamed.Two | FlagsRenamed.Four,
            (FlagsRenamed)8, // no matching flags: numeric string
        })
        {
            AssertMatchesOracleAndRoundtrips(v);
        }
    }

    [Fact]
    public void UnnamedValue_MatchesV3OracleAndRoundtrips()
    {
        // values with no name serialize as their numeric string ("99")
        AssertMatchesOracleAndRoundtrips((Simple)99);
    }

    [Fact]
    public void NestedInCollection_ElementsAreStrings()
    {
        var list = new List<Simple> { Simple.A, Simple.C };
        var ours = MessagePackSerializer.Serialize(list, options);
        Assert.Equal(Oracle.Serialize(list, OracleOptions()), ours);
        Assert.Equal(0x92, ours[0]); // fixarray(2)
        Assert.Equal(0xA1, ours[1]); // element is a fixstr, not an int
        Assert.Equal(list, MessagePackSerializer.Deserialize<List<Simple>>(ours, options));
    }

    [Fact]
    public void IgnoreCase_DeserializesMismatchedCasing()
    {
        var ci = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [new GenericEnumAsStringFormatterFactory(ignoreCase: true), MessagePackFormatterFactory.Default]));

        var lower = new byte[] { 0xA1, (byte)'b' };
        Assert.Equal(Simple.B, MessagePackSerializer.Deserialize<Simple>(lower, ci));
        Assert.Equal(Oracle.Deserialize<Simple>(lower, OracleOptions(ignoreCase: true)),
            MessagePackSerializer.Deserialize<Simple>(lower, ci));

        // case-sensitive options reject the same payload as malformed data, thrown at the
        // point of detection (not a serializer-entry wrap like v3 — same outer type though)
        var ex = Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<Simple>(lower, options));
        Assert.Contains("'b'", ex.Message);
        Assert.Throws<V3::MessagePack.MessagePackSerializationException>(
            () => Oracle.Deserialize<Simple>(lower, OracleOptions()));
    }

    [Fact]
    public void UnknownNames_ThrowSerializationException()
    {
        var unknown = MessagePackSerializer.Serialize("NotAName", options);
        var ex = Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<Simple>(unknown, options));
        Assert.Contains("NotAName", ex.Message);
        Assert.Contains(typeof(Simple).FullName!, ex.Message);

        // flags: one known (renamed) + one unknown element refuses the whole value
        var mixed = MessagePackSerializer.Serialize("first, bogus", options);
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<FlagsRenamed>(mixed, options));
    }

    [Fact]
    public void Nil_ThrowsSerializationException()
    {
        var ex = Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<Simple>(new byte[] { 0xC0 }, options));
        Assert.Contains(typeof(Simple).FullName!, ex.Message);
    }

    [Fact]
    public void GenericFactory_NonEnumsFallThroughToDefaults()
    {
        Assert.Equal(new byte[] { 123 }, MessagePackSerializer.Serialize(123, options));

        var list = new List<int> { 1, 2, 3 };
        Assert.Equal(list, MessagePackSerializer.Deserialize<List<int>>(
            MessagePackSerializer.Serialize(list, options), options));
    }

    [Fact]
    public void ClosedFactory_InChain_ServesOnlyItsEnum()
    {
        // chain discipline: the closed factory answers for Simple only and declines
        // every other type, so the rest of the chain still resolves normally
        var chain = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [new EnumAsStringFormatterFactory<Simple>(), MessagePackFormatterFactory.Default]));

        Assert.Equal(0xA1, MessagePackSerializer.Serialize(Simple.B, chain)[0]); // string
        Assert.Equal(new byte[] { 1 }, MessagePackSerializer.Serialize(Renamed.Beta, chain)); // other enum: default int encoding
        Assert.Equal(new byte[] { 123 }, MessagePackSerializer.Serialize(123, chain)); // non-enum untouched
    }
}
