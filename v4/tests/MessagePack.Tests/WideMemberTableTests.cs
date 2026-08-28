using System.Text;
using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// The seenMembers wide tier: string-key types beyond 64 members switch the duplicate-key /
// required-member tracker from the ulong bitmask to a Span<bool> (stackalloc for <=256
// members, heap array beyond), on both the generated and the reflection tiers. These tests
// pin the 64/65 boundary: bit 63 is the last narrow slot, index 64 the first wide one.
// The >256 heap tier differs from the stackalloc tier only in the tracker's declaration
// (every check/set/validate line is shared), so it is not modeled with a 257-member type.
public class WideMemberTableTests
{
    static readonly MessagePack.MessagePackSerializerOptions reflectionOptions = new(
        new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

    // fixstr keys + positive fixint values, map16 header: hand-built so known keys can repeat
    static byte[] MapPayload(params (string Key, byte Value)[] entries)
    {
        var bytes = new List<byte> { 0xde, (byte)(entries.Length >> 8), (byte)entries.Length };
        foreach (var (key, value) in entries)
        {
            bytes.Add((byte)(0xa0 | key.Length));
            bytes.AddRange(Encoding.ASCII.GetBytes(key));
            bytes.Add(value);
        }
        return [.. bytes];
    }

    // value = 1000 + the index parsed from the P{n} name, so nothing depends on the
    // (officially unspecified) reflection enumeration order
    static T FillByReflection<T>(T value) where T : notnull
    {
        foreach (var property in typeof(T).GetProperties())
        {
            property.SetValue(value, 1000 + int.Parse(property.Name.Substring(1)));
        }
        return value;
    }

    [Fact]
    public void Wide65_Roundtrip_BothTiers()
    {
        var value = FillByReflection(new Wide65Poco());
        var generated = V4.Serialize(value);
        Assert.Equal(generated, V4.Serialize(value, reflectionOptions));

        var back = V4.Deserialize<Wide65Poco>(generated)!;
        Assert.Equal(1000, back.P0);
        Assert.Equal(1063, back.P63);
        Assert.Equal(1064, back.P64);
        Assert.Equal(generated, V4.Serialize(back));

        var reflectionBack = V4.Deserialize<Wide65Poco>(generated, reflectionOptions)!;
        Assert.Equal(generated, V4.Serialize(reflectionBack));
    }

    [Fact]
    public void Wide65_DuplicateKnownKey_Throws_BothTiers()
    {
        // P64 is the first index past the ulong bitmask; P0 pins the low end of the wide table
        foreach (var payload in new[]
        {
            MapPayload(("P64", 1), ("P64", 2)),
            MapPayload(("P0", 1), ("P0", 2)),
            MapPayload(("P0", 1), ("P64", 2), ("P64", 3)),
        })
        {
            Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Wide65Poco>(payload));
            Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Wide65Poco>(payload, reflectionOptions));
        }

        // unknown keys may repeat: version tolerance keeps skipping them
        var unknownDuplicate = MapPayload(("zz", 1), ("zz", 2));
        Assert.NotNull(V4.Deserialize<Wide65Poco>(unknownDuplicate));
        Assert.NotNull(V4.Deserialize<Wide65Poco>(unknownDuplicate, reflectionOptions));
    }

    [Fact]
    public void Wide64_Boundary_DuplicateKnownKey_Throws_BothTiers()
    {
        // 64 members stay on the ulong bitmask; P63 is bit 63, the last one before the wide switch
        foreach (var payload in new[]
        {
            MapPayload(("P63", 1), ("P63", 2)),
            MapPayload(("P0", 1), ("P0", 2)),
        })
        {
            Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Wide64Poco>(payload));
            Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Wide64Poco>(payload, reflectionOptions));
        }

        var value = FillByReflection(new Wide64Poco());
        var generated = V4.Serialize(value);
        Assert.Equal(generated, V4.Serialize(value, reflectionOptions));
        Assert.Equal(generated, V4.Serialize(V4.Deserialize<Wide64Poco>(generated)!));
    }

    [Fact]
    public void Wide65_MissingRequiredMember_Throws_BothTiers()
    {
        // all 65 keys present: fine
        var full = new (string, byte)[65];
        for (int i = 0; i < 65; i++)
        {
            full[i] = ($"P{i}", (byte)(i + 1));
        }
        Assert.Equal(65, V4.Deserialize<Wide65RequiredPoco>(MapPayload(full))!.P64);
        Assert.Equal(65, V4.Deserialize<Wide65RequiredPoco>(MapPayload(full), reflectionOptions)!.P64);

        // required P64 (wide index) absent
        var missingLast = full.Take(64).ToArray();
        var thrown = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Wide65RequiredPoco>(MapPayload(missingLast)));
        Assert.Contains("P64", thrown.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Wide65RequiredPoco>(MapPayload(missingLast), reflectionOptions));

        // required P0 (low wide index) absent
        var missingFirst = full.Skip(1).ToArray();
        thrown = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Wide65RequiredPoco>(MapPayload(missingFirst)));
        Assert.Contains("P0", thrown.Message);
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Wide65RequiredPoco>(MapPayload(missingFirst), reflectionOptions));
    }
}

[MessagePackObject(true)]
public class Wide64Poco
{
    public int P0 { get; set; }
    public int P1 { get; set; }
    public int P2 { get; set; }
    public int P3 { get; set; }
    public int P4 { get; set; }
    public int P5 { get; set; }
    public int P6 { get; set; }
    public int P7 { get; set; }
    public int P8 { get; set; }
    public int P9 { get; set; }
    public int P10 { get; set; }
    public int P11 { get; set; }
    public int P12 { get; set; }
    public int P13 { get; set; }
    public int P14 { get; set; }
    public int P15 { get; set; }
    public int P16 { get; set; }
    public int P17 { get; set; }
    public int P18 { get; set; }
    public int P19 { get; set; }
    public int P20 { get; set; }
    public int P21 { get; set; }
    public int P22 { get; set; }
    public int P23 { get; set; }
    public int P24 { get; set; }
    public int P25 { get; set; }
    public int P26 { get; set; }
    public int P27 { get; set; }
    public int P28 { get; set; }
    public int P29 { get; set; }
    public int P30 { get; set; }
    public int P31 { get; set; }
    public int P32 { get; set; }
    public int P33 { get; set; }
    public int P34 { get; set; }
    public int P35 { get; set; }
    public int P36 { get; set; }
    public int P37 { get; set; }
    public int P38 { get; set; }
    public int P39 { get; set; }
    public int P40 { get; set; }
    public int P41 { get; set; }
    public int P42 { get; set; }
    public int P43 { get; set; }
    public int P44 { get; set; }
    public int P45 { get; set; }
    public int P46 { get; set; }
    public int P47 { get; set; }
    public int P48 { get; set; }
    public int P49 { get; set; }
    public int P50 { get; set; }
    public int P51 { get; set; }
    public int P52 { get; set; }
    public int P53 { get; set; }
    public int P54 { get; set; }
    public int P55 { get; set; }
    public int P56 { get; set; }
    public int P57 { get; set; }
    public int P58 { get; set; }
    public int P59 { get; set; }
    public int P60 { get; set; }
    public int P61 { get; set; }
    public int P62 { get; set; }
    public int P63 { get; set; }
}

[MessagePackObject(true)]
public class Wide65Poco
{
    public int P0 { get; set; }
    public int P1 { get; set; }
    public int P2 { get; set; }
    public int P3 { get; set; }
    public int P4 { get; set; }
    public int P5 { get; set; }
    public int P6 { get; set; }
    public int P7 { get; set; }
    public int P8 { get; set; }
    public int P9 { get; set; }
    public int P10 { get; set; }
    public int P11 { get; set; }
    public int P12 { get; set; }
    public int P13 { get; set; }
    public int P14 { get; set; }
    public int P15 { get; set; }
    public int P16 { get; set; }
    public int P17 { get; set; }
    public int P18 { get; set; }
    public int P19 { get; set; }
    public int P20 { get; set; }
    public int P21 { get; set; }
    public int P22 { get; set; }
    public int P23 { get; set; }
    public int P24 { get; set; }
    public int P25 { get; set; }
    public int P26 { get; set; }
    public int P27 { get; set; }
    public int P28 { get; set; }
    public int P29 { get; set; }
    public int P30 { get; set; }
    public int P31 { get; set; }
    public int P32 { get; set; }
    public int P33 { get; set; }
    public int P34 { get; set; }
    public int P35 { get; set; }
    public int P36 { get; set; }
    public int P37 { get; set; }
    public int P38 { get; set; }
    public int P39 { get; set; }
    public int P40 { get; set; }
    public int P41 { get; set; }
    public int P42 { get; set; }
    public int P43 { get; set; }
    public int P44 { get; set; }
    public int P45 { get; set; }
    public int P46 { get; set; }
    public int P47 { get; set; }
    public int P48 { get; set; }
    public int P49 { get; set; }
    public int P50 { get; set; }
    public int P51 { get; set; }
    public int P52 { get; set; }
    public int P53 { get; set; }
    public int P54 { get; set; }
    public int P55 { get; set; }
    public int P56 { get; set; }
    public int P57 { get; set; }
    public int P58 { get; set; }
    public int P59 { get; set; }
    public int P60 { get; set; }
    public int P61 { get; set; }
    public int P62 { get; set; }
    public int P63 { get; set; }
    public int P64 { get; set; }
}

[MessagePackObject(true)]
public class Wide65RequiredPoco
{
    public required int P0 { get; set; }
    public int P1 { get; set; }
    public int P2 { get; set; }
    public int P3 { get; set; }
    public int P4 { get; set; }
    public int P5 { get; set; }
    public int P6 { get; set; }
    public int P7 { get; set; }
    public int P8 { get; set; }
    public int P9 { get; set; }
    public int P10 { get; set; }
    public int P11 { get; set; }
    public int P12 { get; set; }
    public int P13 { get; set; }
    public int P14 { get; set; }
    public int P15 { get; set; }
    public int P16 { get; set; }
    public int P17 { get; set; }
    public int P18 { get; set; }
    public int P19 { get; set; }
    public int P20 { get; set; }
    public int P21 { get; set; }
    public int P22 { get; set; }
    public int P23 { get; set; }
    public int P24 { get; set; }
    public int P25 { get; set; }
    public int P26 { get; set; }
    public int P27 { get; set; }
    public int P28 { get; set; }
    public int P29 { get; set; }
    public int P30 { get; set; }
    public int P31 { get; set; }
    public int P32 { get; set; }
    public int P33 { get; set; }
    public int P34 { get; set; }
    public int P35 { get; set; }
    public int P36 { get; set; }
    public int P37 { get; set; }
    public int P38 { get; set; }
    public int P39 { get; set; }
    public int P40 { get; set; }
    public int P41 { get; set; }
    public int P42 { get; set; }
    public int P43 { get; set; }
    public int P44 { get; set; }
    public int P45 { get; set; }
    public int P46 { get; set; }
    public int P47 { get; set; }
    public int P48 { get; set; }
    public int P49 { get; set; }
    public int P50 { get; set; }
    public int P51 { get; set; }
    public int P52 { get; set; }
    public int P53 { get; set; }
    public int P54 { get; set; }
    public int P55 { get; set; }
    public int P56 { get; set; }
    public int P57 { get; set; }
    public int P58 { get; set; }
    public int P59 { get; set; }
    public int P60 { get; set; }
    public int P61 { get; set; }
    public int P62 { get; set; }
    public int P63 { get; set; }
    public required int P64 { get; set; }
}
