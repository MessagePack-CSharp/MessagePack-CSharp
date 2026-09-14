using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MessagePack.Tests;

// ObjectParser's AllowCircularReferences gates (MsgPack015), driven through the real
// generator: register-before-populate demands a class on the pure populate path, and the
// wire envelope demands the source-generated formatter.
public class CircularReferenceDiagnosticsTests
{
    static GeneratorDriverRunResult RunGenerator(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "CircularProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, CancellationToken.None).GetRunResult();
    }

    [Fact]
    public void PopulateShapedClass_GeneratesWithoutDiagnostics()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(AllowCircularReferences = true)]
            public class Node
            {
                [Key(0)] public int Value { get; set; }
                [Key(1)] public Node? Next { get; set; }
            }
            """);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "MsgPack015");
        Assert.Contains(result.GeneratedTrees, t => t.ToString().Contains("TrackCircularReference"));
    }

    [Fact]
    public void Struct_ReportsMsgPack015()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(AllowCircularReferences = true)]
            public struct NotANode
            {
                [Key(0)] public int Value { get; set; }
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack015" && d.GetMessage().Contains("class"));
    }

    [Fact]
    public void MatchedConstructor_ReportsMsgPack015()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(AllowCircularReferences = true)]
            public class CtorNode
            {
                public CtorNode(int value) { Value = value; }
                [Key(0)] public int Value { get; }
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack015" && d.GetMessage().Contains("populating"));
    }

    [Fact]
    public void InitOnlyMember_ReportsMsgPack015()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(AllowCircularReferences = true)]
            public class InitNode
            {
                [Key(0)] public int Value { get; init; }
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack015");
    }

    [Fact]
    public void RequiredMember_ReportsMsgPack015()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(AllowCircularReferences = true)]
            public class RequiredNode
            {
                [Key(0)] public required int Value { get; set; }
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack015");
    }

    [Fact]
    public void SerializationConstructorEscapeHatch_SelectsThePopulatePath()
    {
        // a parameterized constructor would be matched; pinning the parameterless one
        // with [SerializationConstructor] restores the populate shape
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(AllowCircularReferences = true)]
            public class PinnedNode
            {
                [SerializationConstructor]
                public PinnedNode() { }
                public PinnedNode(int value) { Value = value; }
                [Key(0)] public int Value { get; set; }
            }
            """);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "MsgPack015");
    }

    [Fact]
    public void SuppressSourceGeneration_ReportsMsgPack015()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(AllowCircularReferences = true, SuppressSourceGeneration = true)]
            public class SuppressedNode
            {
                [Key(0)] public int Value { get; set; }
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack015" && d.GetMessage().Contains("SuppressSourceGeneration"));
    }

    [Fact]
    public void UnionRoot_ReportsMsgPack015()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(AllowCircularReferences = true)]
            [UnionTag(typeof(Leaf), 0)]
            public abstract class Root { }

            [MessagePackObject]
            public class Leaf : Root
            {
                [Key(0)] public int Value { get; set; }
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack015" && d.GetMessage().Contains("union"));
    }
}
