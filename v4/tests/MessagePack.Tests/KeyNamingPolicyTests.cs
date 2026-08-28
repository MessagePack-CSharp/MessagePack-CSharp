using System.Text.Json;
using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// [MessagePackObject(KeyNamingPolicy.X)]: member names convert to string keys exactly like
// System.Text.Json's JsonNamingPolicy (KeyNamingPolicyConverter is a 1:1 port), with an
// explicit [Key("...")] winning over the policy. The conversion fidelity tests use the REAL
// JsonNamingPolicy as the oracle, so the port is verified, not trusted.
public class KeyNamingPolicyTests
{
    static readonly MessagePack.MessagePackSerializerOptions reflectionOptions = new(
        new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

    static readonly (KeyNamingPolicy Policy, JsonNamingPolicy Oracle)[] policyPairs =
    [
        (KeyNamingPolicy.CamelCase, JsonNamingPolicy.CamelCase),
        (KeyNamingPolicy.PascalCase, JsonNamingPolicy.PascalCase),
        (KeyNamingPolicy.SnakeCaseLower, JsonNamingPolicy.SnakeCaseLower),
        (KeyNamingPolicy.SnakeCaseUpper, JsonNamingPolicy.SnakeCaseUpper),
        (KeyNamingPolicy.KebabCaseLower, JsonNamingPolicy.KebabCaseLower),
        (KeyNamingPolicy.KebabCaseUpper, JsonNamingPolicy.KebabCaseUpper),
    ];

    [Fact]
    public void ConvertName_MatchesSystemTextJson_Corpus()
    {
        // the word-boundary corners: acronym runs (IOStream, XMLReader), acronym+digit runs
        // (SHA512Hash), digits, spaces (edge and inner), punctuation resets, non-ASCII letters,
        // already-converted inputs, and single-character shapes
        string[] corpus =
        [
            "", "x", "X", "AB", "ABc", "aB",
            "IOStream", "XMLReader", "XMLHttpRequest", "SHA512Hash", "Sha512Hash", "IPAddress",
            "TempCelsius", "tempCelsius", "already_snake", "already-kebab", "M1X2", "P0",
            "Leading Space", " LeadingSpace", "TrailingSpace ", "Two  Spaces", "a b",
            "ABC???def", "_Underscore", "Name日本語", "日本語Name", "Straße", "ÀCCENT",
            "ABCDef", "HTMLParser5", "A1", "1A", "Ab1Cd",
        ];

        foreach (var (policy, oracle) in policyPairs)
        {
            foreach (var name in corpus)
            {
                Assert.Equal(oracle.ConvertName(name), KeyNamingPolicyConverter.ConvertName(policy, name));
            }
            Assert.Equal("Name As IS", KeyNamingPolicyConverter.ConvertName(KeyNamingPolicy.None, "Name As IS"));
        }
    }

    [Fact]
    public void ConvertName_MatchesSystemTextJson_Fuzz()
    {
        // random strings over a pool that exercises every UnicodeCategory branch of the state
        // machine, including surrogate halves (both implementations pass them through as-is)
        char[] pool =
        [
            'a', 'z', 'A', 'Z', '0', '9', ' ', '_', '-', '?', '.',
            'あ', 'Ａ', 'ａ', 'ß', 'À', 'é', '\uD835', '\uDC9C',
        ];
        var rand = new Random(42);
        for (int i = 0; i < 20_000; i++)
        {
            var chars = new char[rand.Next(0, 13)];
            for (int c = 0; c < chars.Length; c++)
            {
                chars[c] = pool[rand.Next(pool.Length)];
            }
            var name = new string(chars);
            foreach (var (policy, oracle) in policyPairs)
            {
                Assert.Equal(oracle.ConvertName(name), KeyNamingPolicyConverter.ConvertName(policy, name));
            }
        }
    }

    [Fact]
    public void CamelCasePolicy_ConvertedKeysAndExplicitKeyPrecedence()
    {
        var value = new CamelCasePolicyPoco { IOStream = 1, XMLHttpRequest = 2, TempCelsius = 3, Sha512Hash = 4, Renamed = 5 };
        var payload = V4.Serialize(value);

        var map = V4.Deserialize<Dictionary<string, int>>(payload)!;
        Assert.Equal(5, map.Count);
        Assert.Equal(1, map[JsonNamingPolicy.CamelCase.ConvertName(nameof(CamelCasePolicyPoco.IOStream))]);
        Assert.Equal(2, map[JsonNamingPolicy.CamelCase.ConvertName(nameof(CamelCasePolicyPoco.XMLHttpRequest))]);
        Assert.Equal(3, map[JsonNamingPolicy.CamelCase.ConvertName(nameof(CamelCasePolicyPoco.TempCelsius))]);
        Assert.Equal(4, map[JsonNamingPolicy.CamelCase.ConvertName(nameof(CamelCasePolicyPoco.Sha512Hash))]);
        Assert.Equal(5, map["Explicit"]); // [Key("Explicit")] wins over the policy

        // the deserialize automata must match the converted keys too
        var back = V4.Deserialize<CamelCasePolicyPoco>(payload)!;
        Assert.Equal(1, back.IOStream);
        Assert.Equal(5, back.Renamed);

        // reflection tier: byte-identical serialize, same roundtrip
        Assert.Equal(payload, V4.Serialize(value, reflectionOptions));
        Assert.Equal(2, V4.Deserialize<CamelCasePolicyPoco>(payload, reflectionOptions)!.XMLHttpRequest);
    }

    [Fact]
    public void SnakeCasePolicy_ConvertedKeys()
    {
        var value = new SnakeCasePolicyPoco { TempCelsius = 1, XMLReader = 2, SHA512Hash = 3 };
        var payload = V4.Serialize(value);

        var map = V4.Deserialize<Dictionary<string, int>>(payload)!;
        Assert.Equal(1, map["temp_celsius"]);
        Assert.Equal(2, map["xml_reader"]);
        Assert.Equal(3, map["sha512_hash"]);

        Assert.Equal(payload, V4.Serialize(value, reflectionOptions));
        Assert.Equal(3, V4.Deserialize<SnakeCasePolicyPoco>(payload)!.SHA512Hash);
    }

    [Fact]
    public void NonePolicy_EqualsKeyAsPropertyName()
    {
        var policyBytes = V4.Serialize(new NonePolicyPoco { TempCelsius = 7, IOStream = 8 });
        var boolBytes = V4.Serialize(new VerbatimBoolPoco { TempCelsius = 7, IOStream = 8 });
        Assert.Equal(boolBytes, policyBytes);
    }

    [Fact]
    public void PascalCasePolicy_ConvertedKeys()
    {
        var value = new PascalCasePolicyPoco { XMLReader = 1, SHA512Hash = 2, TempCelsius = 3 };
        var payload = V4.Serialize(value);

        var map = V4.Deserialize<Dictionary<string, int>>(payload)!;
        Assert.Equal(1, map["XmlReader"]);   // acronym run keeps only its first letter
        Assert.Equal(2, map["Sha512Hash"]);
        Assert.Equal(3, map["TempCelsius"]); // already Pascal: unchanged

        Assert.Equal(payload, V4.Serialize(value, reflectionOptions));
        Assert.Equal(1, V4.Deserialize<PascalCasePolicyPoco>(payload)!.XMLReader);
    }

    [Fact]
    public void PolicyInducedKeyCollision_Throws_ReflectionTier()
    {
        // "Id" and "ID" both camel-convert to "id"; the source generator rejects the same shape
        // at compile time via MsgPack003 (this type opts out with SuppressSourceGeneration so it can
        // exist here), the reflection tier at table build
        var thrown = Assert.Throws<MessagePackSerializationException>(
            () => V4.Serialize(new CamelCollisionPoco { Id = 1, ID = 2 }, reflectionOptions));
        Assert.Contains("id", thrown.Message);
    }
}

[MessagePackObject(KeyNamingPolicy.CamelCase)]
public class CamelCasePolicyPoco
{
    public int IOStream { get; set; }
    public int XMLHttpRequest { get; set; }
    public int TempCelsius { get; set; }
    public int Sha512Hash { get; set; }
    [Key("Explicit")]
    public int Renamed { get; set; }
}

[MessagePackObject(KeyNamingPolicy.SnakeCaseLower)]
public class SnakeCasePolicyPoco
{
    public int TempCelsius { get; set; }
    public int XMLReader { get; set; }
    public int SHA512Hash { get; set; }
}

[MessagePackObject(KeyNamingPolicy.None)]
public class NonePolicyPoco
{
    public int TempCelsius { get; set; }
    public int IOStream { get; set; }
}

[MessagePackObject(KeyNamingPolicy.PascalCase)]
public class PascalCasePolicyPoco
{
    public int XMLReader { get; set; }
    public int SHA512Hash { get; set; }
    public int TempCelsius { get; set; }
}

[MessagePackObject(true)]
public class VerbatimBoolPoco
{
    public int TempCelsius { get; set; }
    public int IOStream { get; set; }
}

[MessagePackObject(KeyNamingPolicy.CamelCase, SuppressSourceGeneration = true)]
public class CamelCollisionPoco
{
    public int Id { get; set; }
    public int ID { get; set; }
}
