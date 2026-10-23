using MessagePack;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;

namespace MessagePack.Tests;

// a union whose cases form a hierarchy: a payload tagged as the base case must never populate an existing instance of
// the derived case (the old `value as Base` accepted it, filled the base members, and left the derived type and its
// extra members behind, so the next Serialize dispatched it under the derived tag)
[MessagePackObject]
[UnionTag(typeof(ReviewBaseCase), 0)]
[UnionTag(typeof(ReviewDerivedCase), 1)]
public interface IReviewCase
{
}

[MessagePackObject]
public class ReviewBaseCase : IReviewCase
{
    [Key(0)]
    public int A { get; set; }
}

[MessagePackObject]
public class ReviewDerivedCase : ReviewBaseCase
{
    [Key(1)]
    public int B { get; set; }
}

[MessagePackObject]
public class ReviewCaseHolder
{
    [Key(0)]
    public IReviewCase? Case { get; set; }
}

// the plain-Deserialize route into the same bug: the member initializer hands the union formatter a derived instance
[MessagePackObject]
public class ReviewCaseHolderWithDerivedDefault
{
    [Key(0)]
    public IReviewCase? Case { get; set; } = new ReviewDerivedCase { A = 1, B = 2 };
}

// init-only members take the construction shape, whose object initializer assigns every one of them: a payload that
// omits a member (an older writer's) used to leave default where the reflection tier and v3 keep the member's own
// initializer. The generator now re-spells the initializers it can (constants, `new()` without arguments, `[]`, `default`).
[MessagePackObject]
public class ReviewInitDefaults
{
    [Key(0)]
    public int A { get; init; }

    [Key(1)]
    public int Retries { get; init; } = 3;

    [Key(2)]
    public string Name { get; init; } = "n";

    [Key(3)]
    public List<int> Items { get; init; } = new();

    [Key(4)]
    public int[] Numbers { get; init; } = [];

    [Key(5)]
    public IDictionary<string, int> Map { get; init; } = new Dictionary<string, int>();

    [Key(6)]
    public DayOfWeek Day { get; init; } = DayOfWeek.Friday;

    [Key(7)]
    public double Ratio { get; init; } = 0.5;

    [Key(8)]
    public Guid Id { get; init; } = default;
}

// Regressions from the external review of v4.0.0-preview.1 (2026-10-23): the union populate type match and the
// MessagePackUnknownMembers x AllowCircularReferences interaction.
public class PostReleaseReviewRegressionTests
{
    // the generated tier and the reflection tier (the chain an assembly the generator never saw is served by)
    static IEnumerable<V4Options> BothTiers()
    {
        yield return V4Options.Default;
        yield return new V4Options(new MessagePackFormatterResolver(MessagePackFormatterFactory.Combine(
            BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance, new ReflectionFormatterFactory(annotatedOnly: true, allowPrivate: false))));
    }

    [Fact]
    public void UnionPopulate_BaseTagIntoDerivedInstance_CreatesTheBaseCase()
    {
        var bytes = V4.Serialize(new ReviewCaseHolder { Case = new ReviewBaseCase { A = 5 } });
        foreach (var options in BothTiers())
        {
            var holder = new ReviewCaseHolder { Case = new ReviewDerivedCase { A = 1, B = 2 } };
            V4.Deserialize(bytes, ref holder, options);
            var back = Assert.IsType<ReviewBaseCase>(holder!.Case); // exact: a derived instance is not "a base case"
            Assert.Equal(5, back.A);
            Assert.Equal(bytes, V4.Serialize(holder, options)); // and the round trip keeps tag 0
        }
    }

    [Fact]
    public void UnionPopulate_DerivedInitializer_PlainDeserializeCreatesTheBaseCase()
    {
        var bytes = V4.Serialize(new ReviewCaseHolderWithDerivedDefault { Case = new ReviewBaseCase { A = 5 } });
        foreach (var options in BothTiers())
        {
            var holder = V4.Deserialize<ReviewCaseHolderWithDerivedDefault>(bytes, options)!;
            var back = Assert.IsType<ReviewBaseCase>(holder.Case);
            Assert.Equal(5, back.A);
            Assert.Equal(bytes, V4.Serialize(holder, options));
        }
    }

    [Fact]
    public void UnionPopulate_ExactCase_StillReadsIntoTheExistingInstance()
    {
        var bytes = V4.Serialize(new ReviewCaseHolder { Case = new ReviewDerivedCase { A = 5, B = 6 } });
        foreach (var options in BothTiers())
        {
            var existing = new ReviewDerivedCase { A = 1, B = 2 };
            var holder = new ReviewCaseHolder { Case = existing };
            V4.Deserialize(bytes, ref holder, options);
            Assert.Same(existing, holder!.Case);
            Assert.Equal(5, existing.A);
            Assert.Equal(6, existing.B);
        }
    }

    // ---- unknown members must not capture a circular-reference back-reference (ext 97) ----

    const byte FixExt1 = 0xd4;
    const byte Ext8 = 0xc7;
    const byte ExtCircular = 97;

    // {"A":1,"B":"b","C":<value>} for UnkNarrowMap, which declares A and B and captures the rest
    static byte[] MapWithUnknownValue(params byte[] value) =>
        [0x83, 0xa1, (byte)'A', 0x01, 0xa1, (byte)'B', 0xa1, (byte)'b', 0xa1, (byte)'C', .. value];

    [Fact]
    public void UnknownMembers_BackReferenceValue_IsRejected()
    {
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<UnkNarrowMap>(MapWithUnknownValue(FixExt1, ExtCircular, 0x00)));
        Assert.Contains("back-reference", ex.Message);
    }

    [Fact]
    public void UnknownMembers_BackReferenceNestedInValue_IsRejected()
    {
        // [1, ext97(0)]: the walk covers the whole captured subtree
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<UnkNarrowMap>(MapWithUnknownValue(0x92, 0x01, FixExt1, ExtCircular, 0x00)));
        // and every ext header width: ext8 with a 1-byte payload
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<UnkNarrowMap>(MapWithUnknownValue(Ext8, 0x01, ExtCircular, 0x00)));
    }

    [Fact]
    public void UnknownMembers_BackReferenceInArrayMode_IsRejected()
    {
        // [1, "b", ext97(0)] for UnkNarrowArray, which declares keys 0 and 1 and captures the trailing elements
        byte[] bytes = [0x93, 0x01, 0xa1, (byte)'b', FixExt1, ExtCircular, 0x00];
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<UnkNarrowArray>(bytes));
    }

    [Fact]
    public void UnknownMembers_OtherExtensions_StillCaptureVerbatim()
    {
        // a user extension of another type code (and a definition envelope, which is a plain array) pass through
        var bytes = MapWithUnknownValue(FixExt1, 0x01, 0x00);
        var narrow = V4.Deserialize<UnkNarrowMap>(bytes)!;
        Assert.Equal(1, narrow.Extra!.Count);
        Assert.Equal(bytes, V4.Serialize(narrow));
    }

    // ---- init-only initializers survive an omitted member on the generated construction path ----

    [Fact]
    public void InitOnly_OmittedMembers_KeepTheirInitializers_BothTiers()
    {
        byte[] olderWriter = [0x91, 0x07]; // [7]: only key 0
        foreach (var options in BothTiers())
        {
            var back = V4.Deserialize<ReviewInitDefaults>(olderWriter, options)!;
            Assert.Equal(7, back.A);
            Assert.Equal(3, back.Retries);
            Assert.Equal("n", back.Name);
            Assert.NotNull(back.Items);
            Assert.Empty(back.Items);
            Assert.NotNull(back.Numbers);
            Assert.Empty(back.Numbers);
            Assert.IsType<Dictionary<string, int>>(back.Map);
            Assert.Empty(back.Map);
            Assert.Equal(DayOfWeek.Friday, back.Day);
            Assert.Equal(0.5, back.Ratio);
            Assert.Equal(Guid.Empty, back.Id);
        }
    }

    [Fact]
    public void InitOnly_PresentMembers_StillWin()
    {
        var bytes = V4.Serialize(new ReviewInitDefaults { A = 1, Retries = 9, Name = "x", Items = [4], Numbers = [5], Map = new Dictionary<string, int> { ["k"] = 6 }, Day = DayOfWeek.Monday, Ratio = 2, Id = Guid.Parse("11111111-1111-1111-1111-111111111111") });
        foreach (var options in BothTiers())
        {
            var back = V4.Deserialize<ReviewInitDefaults>(bytes, options)!;
            Assert.Equal(9, back.Retries);
            Assert.Equal("x", back.Name);
            Assert.Equal([4], back.Items);
            Assert.Equal([5], back.Numbers);
            Assert.Equal(6, back.Map["k"]);
            Assert.Equal(DayOfWeek.Monday, back.Day);
            Assert.Equal(2, back.Ratio);
            Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), back.Id);
            Assert.Equal(bytes, V4.Serialize(back, options));
        }
    }

    static GeneratorDriverRunResult RunGenerator(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "PostReleaseProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, CancellationToken.None).GetRunResult();
    }

    [Fact]
    public void InitOnly_NonReproducibleInitializer_ReportsMsgPack030()
    {
        var result = RunGenerator("""
            using System;
            using MessagePack;
            [MessagePackObject]
            public class Stamped
            {
                [Key(0)] public int X { get; init; }
                [Key(1)] public Guid Id { get; init; } = Guid.NewGuid();
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack030" && d.GetMessage().Contains("Stamped.Id"));
    }

    [Fact]
    public void InitOnly_ReproducibleInitializers_NoMsgPack030()
    {
        var result = RunGenerator("""
            using System;
            using System.Collections.Generic;
            using MessagePack;
            [MessagePackObject]
            public class Quiet
            {
                [Key(0)] public int Retries { get; init; } = 3;
                [Key(1)] public List<int> Items { get; init; } = new();
                [Key(2)] public int[] Numbers { get; init; } = [];
                [Key(3)] public Guid Id { get; init; } = default;
                [Key(4)] public int Plain { get; set; }
                // a required init-only member is validated as present, so its initializer is not reported either
                [Key(5)] public required string Label { get; init; } = Guid.NewGuid().ToString();
            }
            """);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "MsgPack030");
        Assert.Contains(result.GeneratedTrees, t => t.ToString().Contains("v_Retries = (int)3") && t.ToString().Contains("v_Numbers = []"));
    }

    // ---- Tuple`8 with a nil Rest is a serialization error, not the Tuple constructor's ArgumentException ----

    [Fact]
    public void Tuple8_NilRest_ThrowsSerializationException()
    {
        byte[] bytes = [0x98, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0xc0];
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Tuple<int, int, int, int, int, int, int, Tuple<int>>>(bytes));
        Assert.Contains("Rest", ex.Message);
    }

    // ---- a negative MaxDepth is rejected instead of being silently unlimited ----

    [Fact]
    public void MaxDepth_Negative_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => V4Options.Default with { MaxDepth = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new SerializeState(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DeserializeState(-1, 0));
        Assert.Equal(0, (V4Options.Default with { MaxDepth = 0 }).MaxDepth); // 0 stays the documented "unlimited"
    }
}
