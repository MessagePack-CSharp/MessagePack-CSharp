using System.Runtime.Serialization;
using MessagePack.Formatters;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// [MessagePackObject(EnumAsString = true)]: the enums of the declaring type's members are written by name, through a
// formatter tree each member builds for itself, while the resolver's shared formatters keep the integers for every
// other use site. Each shape runs through the generated formatter and through ReflectionObjectFormatter (the
// SuppressSourceGeneration twins derive from the generated types, so both tiers see the same members).
public class EnumAsStringObjectTests
{
    static EnumAsStringPoco Populated<TPoco>()
        where TPoco : EnumAsStringPoco, new() => new TPoco
    {
        Single = EnumAsStringShade.Green,
        Maybe = EnumAsStringShade.Blue,
        Array = [EnumAsStringShade.Red, EnumAsStringShade.Blue],
        List = [EnumAsStringShade.Green],
        ByKey = new() { [EnumAsStringShade.Red] = 1 },
        ByValue = new() { ["x"] = EnumAsStringShade.Blue },
        Nested = new() { ["y"] = [EnumAsStringShade.Red, EnumAsStringShade.Green] },
        ReadOnly = [EnumAsStringShade.Blue],
        Set = [EnumAsStringShade.Green],
        Pair = new(EnumAsStringShade.Red, "r"),
        Lazy = new(() => EnumAsStringShade.Blue),
        Flags = EnumAsStringPermission.Read | EnumAsStringPermission.Write,
        Renamed = EnumAsStringRenamed.First,
        Plain = 7,
        Text = "t",
        Inner = new() { Color = EnumAsStringShade.Blue },
        Inners = [new() { Color = EnumAsStringShade.Green }],
        Pinned = EnumAsStringPinned.B,
        Overridden = EnumAsStringShade.Blue,
    };

    const string PopulatedJson =
        """{"Single":"Green","Maybe":"Blue","Array":["Red","Blue"],"List":["Green"],"ByKey":{"Red":1},"ByValue":{"x":"Blue"},"Nested":{"y":["Red","Green"]},"ReadOnly":["Blue"],"Set":["Green"],"Pair":["Red","r"],"Lazy":"Blue","Flags":"Read, Write","Renamed":"first","Plain":7,"Text":"t","Inner":{"Color":2},"Inners":[{"Color":1}],"Pinned":1,"Overridden":2}""";

    [Fact]
    public void Generated_WritesTheMembersEnumsByName()
    {
        var bytes = V4.Serialize(Populated<EnumAsStringPoco>());

        // an enum member, Nullable, arrays, lists, dictionary keys and values, nested containers, KeyValuePair, Lazy,
        // [Flags] and [EnumMember] by name; the nested [MessagePackObject] keeps its own (integer) schema, the enum with a
        // type-level [MessagePackFormatter] keeps its declaration, and the member-level [MessagePackFormatter] wins
        Assert.Equal(PopulatedJson, V4.ConvertToJson(bytes));
        Assert.Equal(PopulatedJson, V4.ConvertToJson(V4.Serialize(V4.Deserialize<EnumAsStringPoco>(bytes))));
    }

    [Fact]
    public void Reflection_WritesTheSameBytes()
    {
        var generated = V4.Serialize(Populated<EnumAsStringPoco>());
        var reflected = V4.Serialize((EnumAsStringPocoReflected)Populated<EnumAsStringPocoReflected>());

        Assert.Equal(generated, reflected);
        Assert.Equal(PopulatedJson, V4.ConvertToJson(V4.Serialize(V4.Deserialize<EnumAsStringPocoReflected>(generated))));
        Assert.Equal(PopulatedJson, V4.ConvertToJson(V4.Serialize(V4.Deserialize<EnumAsStringPoco>(reflected))));
    }

    [Fact]
    public void SharedFormatters_KeepTheIntegers()
    {
        // the trees are the members' own: after the EnumAsString types ran, every other use site is unchanged
        V4.Serialize(Populated<EnumAsStringPoco>());
        V4.Serialize((EnumAsStringPocoReflected)Populated<EnumAsStringPocoReflected>());

        Assert.Equal("2", V4.ConvertToJson(V4.Serialize(EnumAsStringShade.Blue)));
        Assert.Equal("[2,0]", V4.ConvertToJson(V4.Serialize(new List<EnumAsStringShade> { EnumAsStringShade.Blue, EnumAsStringShade.Red })));
        Assert.Equal("""{"Red":1}""", V4.ConvertToJson(V4.Serialize(new Dictionary<string, EnumAsStringShade> { ["Red"] = EnumAsStringShade.Green })));
        Assert.Equal("""{"Color":2}""", V4.ConvertToJson(V4.Serialize(new EnumAsStringInner { Color = EnumAsStringShade.Blue })));
    }

    static IEnumerable<EnumAsStringShade> Iterate()
    {
        yield return EnumAsStringShade.Blue;
        yield return EnumAsStringShade.Red;
    }

    [Fact]
    public void Enumerable_WithoutACount_StagesByName()
    {
        // an iterator has no count, so its elements are staged over the Compatible buffers first, through the same tree
        const string Json = """{"Items":["Blue","Red"]}""";
        Assert.Equal(Json, V4.ConvertToJson(V4.Serialize(new EnumAsStringSequence { Items = Iterate() })));
        Assert.Equal(Json, V4.ConvertToJson(V4.Serialize(new EnumAsStringSequence { Items = [EnumAsStringShade.Blue, EnumAsStringShade.Red] })));
        Assert.Equal(Json, V4.ConvertToJson(V4.Serialize<EnumAsStringSequence>(new EnumAsStringSequenceReflected { Items = Iterate() })));
        Assert.Equal(Json, V4.ConvertToJson(V4.Serialize(new EnumAsStringSequenceReflected { Items = Iterate() })));
    }

    [Fact]
    public void GenericDeclaration_LeavesTheTypeParameterToTheUseSite()
    {
        // the enum substituted for T is not part of the declaration: Payload and Items keep the integers, in both tiers
        const string Json = """{"Payload":2,"Items":[0],"Kind":"Green"}""";
        var generated = V4.Serialize(new EnumAsStringEnvelope<EnumAsStringShade> { Payload = EnumAsStringShade.Blue, Items = [EnumAsStringShade.Red], Kind = EnumAsStringShade.Green });
        var reflected = V4.Serialize(new EnumAsStringEnvelopeReflected<EnumAsStringShade> { Payload = EnumAsStringShade.Blue, Items = [EnumAsStringShade.Red], Kind = EnumAsStringShade.Green });

        Assert.Equal(Json, V4.ConvertToJson(generated));
        Assert.Equal(generated, reflected);
    }

    [Fact]
    public void AllowPrivate_NestedFormatter_WritesByName()
    {
        var value = new EnumAsStringPrivate();
        value.Fill();
        var bytes = V4.Serialize(value);

        Assert.Equal("""{"hidden":["Beta"]}""", V4.ConvertToJson(bytes));
        Assert.Equal("""{"hidden":["Beta"]}""", V4.ConvertToJson(V4.Serialize(V4.Deserialize<EnumAsStringPrivate>(bytes))));
    }

    [Fact]
    public void ReadingAnInteger_Throws()
    {
        // the wire of EnumAsStringFormatter: reading requires a name
        var integers = V4.Serialize(new EnumAsStringSequenceMirror { Items = [2] });
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<EnumAsStringSequence>(integers));
    }

    [Fact]
    public void Reflection_UnreachableEnum_Throws()
    {
        // the generator reports the same shape as MsgPack030; the SuppressSourceGeneration type reaches the reflection tier
        var exception = Record.Exception(() => V4.Serialize(new EnumAsStringTupleReflected { Pair = (EnumAsStringShade.Red, 1) }));
        Assert.NotNull(exception);
        Assert.Contains("EnumAsString cannot reach the enum", exception.ToString());
        Assert.Contains("EnumAsStringFormatterFactory<EnumAsStringShade>", exception.ToString());
    }

    // ---- the generated name formatters against EnumAsStringFormatter ----

    static readonly MessagePackSerializerOptions ByEnumAsStringFormatter = new(new MessagePackFormatterResolver(
        [new EnumAsStringFormatterFactory<EnumAsStringTricky>(), MessagePackFormatterFactory.Default]));

    [Fact]
    public void NameFormatters_AreGenerated()
    {
        // the leaves are the compiled name tables, not EnumAsStringFormatter: shared ones in MessagePack.Generated, and
        // the AllowPrivate type's private enum inside that type
        var assembly = typeof(EnumAsStringObjectTests).Assembly;
        Assert.NotNull(assembly.GetType("MessagePack.Generated.MessagePack_Tests_EnumAsStringTrickyEnumNames`2"));
        Assert.NotNull(assembly.GetType("MessagePack.Generated.MessagePack_Tests_EnumAsStringShadeEnumNames`2"));
        Assert.NotNull(typeof(EnumAsStringPrivate).GetNestedType("MessagePack_Tests_EnumAsStringPrivate_HiddenEnumNames`2", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public));
    }

    public static TheoryData<EnumAsStringTricky> TrickyValues() =>
    [
        EnumAsStringTricky.None,
        EnumAsStringTricky.Read,           // declared twice (Alias): the value keeps its last declared name
        EnumAsStringTricky.Write,          // [EnumMember] rename
        EnumAsStringTricky.ReadWrite,
        EnumAsStringTricky.Unicode,        // a non-ASCII rename
        EnumAsStringTricky.Long,           // a name past 31 bytes: str8
        EnumAsStringTricky.@class,         // a keyword member
        EnumAsStringTricky.Write | EnumAsStringTricky.Unicode,      // an undeclared [Flags] combination of renames
        EnumAsStringTricky.Read | EnumAsStringTricky.Long,
        (EnumAsStringTricky)64,            // an undeclared value: its number
    ];

    [Theory]
    [MemberData(nameof(TrickyValues))]
    public void NameFormatter_WritesWhatEnumAsStringFormatterWrites(EnumAsStringTricky value)
    {
        var expected = V4.Serialize(value, ByEnumAsStringFormatter);
        var generated = V4.Serialize(new EnumAsStringTrickyHolder { Value = value });

        Assert.Equal([0x91, .. expected], generated);
        Assert.Equal(value, V4.Deserialize<EnumAsStringTrickyHolder>(generated).Value);
    }

    [Theory]
    [InlineData("None")]
    [InlineData("Alias")]
    [InlineData("Read")]                       // the alias's other name, still a declared one
    [InlineData("write-access")]
    [InlineData("Write")]                      // the CLR name of a renamed member, through Enum.TryParse
    [InlineData("ユニコード")]
    [InlineData("a-member-name-long-enough-for-the-str8-header")]
    [InlineData("class")]
    [InlineData("Alias, write-access")]        // a combination in the wire names
    [InlineData("Read, Write")]                // and in the CLR names
    [InlineData("write-access, ユニコード")]
    [InlineData("5")]
    [InlineData(" Read ")]
    [InlineData("read")]                       // ordinal: a case mismatch throws
    [InlineData("Nope")]
    [InlineData("")]
    public void NameFormatter_ReadsWhatEnumAsStringFormatterReads(string name)
    {
        var expected = Record.Exception(() => V4.Deserialize<EnumAsStringTricky>(V4.Serialize(name), ByEnumAsStringFormatter));
        EnumAsStringTricky read = default;
        var generated = Record.Exception(() => read = V4.Deserialize<EnumAsStringTrickyHolder>(V4.Serialize(new[] { name })).Value);

        Assert.Equal(expected?.GetBaseException().Message, generated?.GetBaseException().Message);
        if (expected is null)
        {
            Assert.Equal(V4.Deserialize<EnumAsStringTricky>(V4.Serialize(name), ByEnumAsStringFormatter), read);
        }
    }

    [Fact]
    public void NameFormatter_NilAndNonString_FailLikeEnumAsStringFormatter()
    {
        foreach (var payload in new[] { new byte[] { 0xc0 }, new byte[] { 0x01 } })
        {
            var expected = Record.Exception(() => V4.Deserialize<EnumAsStringTricky>(payload, ByEnumAsStringFormatter));
            var generated = Record.Exception(() => V4.Deserialize<EnumAsStringTrickyHolder>([0x91, .. payload]));
            Assert.NotNull(expected);
            Assert.Equal(expected.GetBaseException().Message, generated?.GetBaseException().Message);
        }
    }

    [Theory]
    [MemberData(nameof(TrickyValues))]
    public void NameFormatter_ReadsAStrAcrossSegments(EnumAsStringTricky value)
    {
        // one byte per segment: no str lies whole in a window, so every read takes the ReadString path
        var bytes = V4.Serialize(new EnumAsStringTrickyHolder { Value = value });
        Assert.Equal(value, V4.Deserialize<EnumAsStringTrickyHolder>(SingleByteSegments(bytes)).Value);
    }

    static System.Buffers.ReadOnlySequence<byte> SingleByteSegments(byte[] data)
    {
        var last = new Segment(data.AsMemory(data.Length - 1, 1), null, data.Length - 1);
        var next = last;
        for (int i = data.Length - 2; i >= 0; i--)
        {
            next = new Segment(data.AsMemory(i, 1), next, i);
        }
        return new System.Buffers.ReadOnlySequence<byte>(next, 0, last, 1);
    }

    sealed class Segment : System.Buffers.ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, Segment? next, long runningIndex)
        {
            Memory = memory;
            Next = next;
            RunningIndex = runningIndex;
        }
    }

    // ---- generator diagnostics (driver-hosted) ----

    static GeneratorDriverRunResult Run(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation("using System.Collections.Generic;" + Environment.NewLine + "using MessagePack;" + Environment.NewLine + "using MessagePack.Formatters;" + Environment.NewLine + source, "EnumAsStringProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, CancellationToken.None).GetRunResult();
    }

    [Theory]
    [InlineData("(Shade, int)", "(Shade, int)")]
    [InlineData("List<(int, Shade)>", "(int, Shade)")]
    [InlineData("Shade[,]", "Shade[,]")]
    [InlineData("Bag<Shade>", "Bag<Shade>")]
    public void UnreachableEnum_IsMsgPack030(string memberType, string container)
    {
        var result = Run($$"""
            public enum Shade { Red }
            public class Bag<T> : List<T> { }
            [MessagePackObject(true, EnumAsString = true)] public class Probe { public {{memberType}} Value { get; set; } }
            """);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Id == "MsgPack030");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        var message = diagnostic.GetMessage();
        Assert.Contains($"inside '{container}'", message);
        Assert.Contains("[MessagePackFormatter(typeof(EnumAsStringFormatterFactory<Shade>))]", message);
        Assert.DoesNotContain(result.GeneratedTrees, static t => t.FilePath.Contains("ProbeFormatter", StringComparison.Ordinal));
    }

    [Theory]
    // the member's own [MessagePackFormatter] owns the wire
    [InlineData("[MessagePackFormatter(typeof(ValueTupleFormatterFactory<Shade, int>))] public (Shade, int) Value { get; set; }")]
    // the enum's own type-level [MessagePackFormatter] decides its wire everywhere
    [InlineData("public (Pinned, int) Value { get; set; }")]
    // no enum inside (and Generic<T> below: the enum a use site substitutes for T is not the declaration's)
    [InlineData("public (string, int) Value { get; set; }")]
    public void ReachableOrOwnedShapes_AreNotReported(string member)
    {
        var result = Run($$"""
            public enum Shade { Red }
            [MessagePackFormatter(typeof(EnumFormatterFactory<Pinned>))] public enum Pinned { A }
            [MessagePackObject(true, EnumAsString = true)] public class Probe { {{member}} }
            [MessagePackObject(true, EnumAsString = true)] public class Generic<T> { public (T, int) Value { get; set; } }
            """);

        Assert.DoesNotContain(result.Diagnostics, static d => d.Id == "MsgPack030");
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity == DiagnosticSeverity.Error);
    }
}

public enum EnumAsStringShade
{
    Red,
    Green,
    Blue,
}

[Flags]
public enum EnumAsStringPermission
{
    None = 0,
    Read = 1,
    Write = 2,
}

public enum EnumAsStringRenamed
{
    [EnumMember(Value = "first")]
    First,
    Second,
}

// the enum's own declaration wins over EnumAsString
[MessagePackFormatter(typeof(EnumFormatterFactory<EnumAsStringPinned>))]
public enum EnumAsStringPinned
{
    A,
    B,
}

[MessagePackObject(true)]
public class EnumAsStringInner
{
    public EnumAsStringShade Color { get; set; }
}

[MessagePackObject(true, EnumAsString = true)]
public class EnumAsStringPoco
{
    public EnumAsStringShade Single { get; set; }
    public EnumAsStringShade? Maybe { get; set; }
    public EnumAsStringShade[]? Array { get; set; }
    public List<EnumAsStringShade>? List { get; set; }
    public Dictionary<EnumAsStringShade, int>? ByKey { get; set; }
    public Dictionary<string, EnumAsStringShade>? ByValue { get; set; }
    public Dictionary<string, List<EnumAsStringShade>>? Nested { get; set; }
    public IReadOnlyList<EnumAsStringShade>? ReadOnly { get; set; }
    public HashSet<EnumAsStringShade>? Set { get; set; }
    public KeyValuePair<EnumAsStringShade, string> Pair { get; set; }
    public Lazy<EnumAsStringShade>? Lazy { get; set; }
    public EnumAsStringPermission Flags { get; set; }
    public EnumAsStringRenamed Renamed { get; set; }
    public int Plain { get; set; }
    public string? Text { get; set; }
    public EnumAsStringInner? Inner { get; set; }
    public List<EnumAsStringInner>? Inners { get; set; }
    public EnumAsStringPinned Pinned { get; set; }
    [MessagePackFormatter(typeof(EnumFormatterFactory<EnumAsStringShade>))]
    public EnumAsStringShade Overridden { get; set; }
}

[MessagePackObject(true, EnumAsString = true, SuppressSourceGeneration = true)]
public class EnumAsStringPocoReflected : EnumAsStringPoco
{
}

[MessagePackObject(true, EnumAsString = true)]
public class EnumAsStringSequence
{
    public IEnumerable<EnumAsStringShade>? Items { get; set; }
}

[MessagePackObject(true, EnumAsString = true, SuppressSourceGeneration = true)]
public class EnumAsStringSequenceReflected : EnumAsStringSequence
{
}

[MessagePackObject(true)]
public class EnumAsStringSequenceMirror
{
    public int[]? Items { get; set; }
}

[MessagePackObject(true, EnumAsString = true)]
public class EnumAsStringEnvelope<T>
{
    public T? Payload { get; set; }
    public List<T>? Items { get; set; }
    public EnumAsStringShade Kind { get; set; }
}

[MessagePackObject(true, EnumAsString = true, SuppressSourceGeneration = true)]
public class EnumAsStringEnvelopeReflected<T> : EnumAsStringEnvelope<T>
{
}

[MessagePackObject(true, EnumAsString = true, AllowPrivate = true)]
public partial class EnumAsStringPrivate
{
    enum Hidden
    {
        Alpha,
        Beta,
    }

    List<Hidden>? hidden;

    public void Fill() => hidden = [Hidden.Beta];
}

[MessagePackObject(true, EnumAsString = true, SuppressSourceGeneration = true)]
public class EnumAsStringTupleReflected
{
    public (EnumAsStringShade, int) Pair { get; set; }
}

[Flags]
public enum EnumAsStringTricky
{
    None = 0,
    Read = 1,
    [EnumMember(Value = "write-access")]
    Write = 2,
    ReadWrite = 3,
    Alias = 1,
    [EnumMember(Value = "ユニコード")]
    Unicode = 4,
    [EnumMember(Value = "a-member-name-long-enough-for-the-str8-header")]
    Long = 8,
    @class = 16,
}

[MessagePackObject(EnumAsString = true)]
public class EnumAsStringTrickyHolder
{
    [Key(0)]
    public EnumAsStringTricky Value { get; set; }
}
