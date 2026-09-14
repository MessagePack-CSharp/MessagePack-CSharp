using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MessagePack.Tests;

// UnionParser's struct-root gates, driven through the real generator: [UnionTag] on a
// struct demands IUnion (the lowered shape of a C# union declaration), a public Value,
// and a constructor that accepts each tagged case.
public class UnionStructDiagnosticsTests
{
    static GeneratorDriverRunResult RunGenerator(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "UnionProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, CancellationToken.None).GetRunResult();
    }

    const string SharedTypes = """

        [MessagePackObject]
        public class Circle { [Key(0)] public double R { get; set; } }

        namespace System.Runtime.CompilerServices
        {
            internal interface IUnion { object? Value { get; } }

            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
            internal sealed class UnionAttribute : System.Attribute { }
        }
        """;

    [Fact]
    public void UnionTagOnPlainStruct_ReportsUMP011()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public struct NotAUnion { }
            """ + SharedTypes);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack011" && d.GetMessage().Contains("[Union] pattern"));
    }

    [Fact]
    public void IUnionStructWithoutCaseConstructor_ReportsUMP011()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public struct NoConstructor : System.Runtime.CompilerServices.IUnion
            {
                public object? Value => null;
            }
            """ + SharedTypes);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack011" && d.GetMessage().Contains("constructor"));
    }

    [Fact]
    public void IUnionStructWithHiddenValue_ReportsUMP011()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public struct HiddenValue : System.Runtime.CompilerServices.IUnion
            {
                public HiddenValue(object? value) { }
                object? System.Runtime.CompilerServices.IUnion.Value => null;
            }
            """ + SharedTypes);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack011" && d.GetMessage().Contains("Value"));
    }

    [Fact]
    public void SuppressedUnionRoot_ReportsMsgPack011()
    {
        // SuppressSourceGeneration on an object type routes it to the runtime reflection tier, but unions have no runtime tier at all.
        // The old behavior was a silent skip whose first symptom was a formatter-not-found at runtime; the gate must reject it at compile time.
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(SuppressSourceGeneration = true)]
            [UnionTag(typeof(Leaf), 0)]
            public abstract class SuppressedRoot { }

            [MessagePackObject]
            public class Leaf : SuppressedRoot
            {
                [Key(0)] public int Value { get; set; }
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack011" && d.GetMessage().Contains("SuppressSourceGeneration"));
        Assert.DoesNotContain(result.GeneratedTrees, t => t.FilePath.Contains("SuppressedRootFormatter"));
    }

    [Fact]
    public void NonBoxingUnion_DispatchesThroughTryGetValue()
    {
        var result = RunGenerator("""
            using MessagePack;
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public struct Boxless
            {
                public Boxless(Circle value) { }
                public object? Value => null;
                public bool TryGetValue(out Circle value) { value = null!; return false; }
            }
            """ + SharedTypes);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "MsgPack011");
        var source = Assert.Single(result.Results[0].GeneratedSources, s => s.HintName.Contains("Boxless")).SourceText.ToString();
        Assert.Contains("TryGetValue(out global::Circle", source);
        Assert.DoesNotContain("var runtimeType", source);
    }

    [Fact]
    public void PartialTryGetValue_FallsBackToValueDispatch()
    {
        // only one of two tagged cases has TryGetValue: the Value path must serve both
        var result = RunGenerator("""
            using MessagePack;
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            [UnionTag(typeof(string), 1)]
            public struct Halfway
            {
                public Halfway(Circle value) { }
                public Halfway(string value) { }
                public object? Value => null;
                public bool TryGetValue(out Circle value) { value = null!; return false; }
            }
            """ + SharedTypes);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "MsgPack011");
        var source = Assert.Single(result.Results[0].GeneratedSources, s => s.HintName.Contains("Halfway")).SourceText.ToString();
        Assert.DoesNotContain("TryGetValue", source);
        Assert.Contains(".Value;", source);
    }

    [Fact]
    public void ProviderUnion_ReadsAndCreatesThroughTheInterface()
    {
        var result = RunGenerator("""
            using MessagePack;
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public class Wrapped : Wrapped.IUnionMembers
            {
                object? heldValue;
                public interface IUnionMembers
                {
                    public static Wrapped Create(Circle value) => new Wrapped { heldValue = value };
                    public object? Value { get; }
                }
                object? IUnionMembers.Value => heldValue;
            }
            """ + SharedTypes);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "MsgPack011");
        var source = Assert.Single(result.Results[0].GeneratedSources, s => s.HintName.Contains("Wrapped")).SourceText.ToString();
        Assert.Contains("((global::Wrapped.IUnionMembers)value).Value", source);
        Assert.Contains("global::Wrapped.IUnionMembers.Create(", source);
    }

    [Fact]
    public void ProviderUnion_MissingCreateForTaggedCase_ReportsUMP011()
    {
        var result = RunGenerator("""
            using MessagePack;
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            [UnionTag(typeof(string), 1)]
            public class Wrapped : Wrapped.IUnionMembers
            {
                object? heldValue;
                public interface IUnionMembers
                {
                    public static Wrapped Create(Circle value) => new Wrapped { heldValue = value };
                    public object? Value { get; }
                }
                object? IUnionMembers.Value => heldValue;
            }
            """ + SharedTypes);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack011" && d.GetMessage().Contains("creation member"));
    }

    [Fact]
    public void GenericPatternUnion_GeneratesTheOpenFormatter()
    {
        var result = RunGenerator("""
            using MessagePack;
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag("T", 0)]
            [UnionTag(typeof(Circle), 1)]
            public struct Outcome<T>
            {
                public Outcome(T value) { }
                public Outcome(Circle value) { }
                public object? Value => null;
            }
            """ + SharedTypes);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "MsgPack011");
        var source = Assert.Single(result.Results[0].GeneratedSources, s => s.HintName.Contains("Outcome")).SourceText.ToString();
        // the formatter closes over the root's parameter, and the T case dispatches by
        // `is` (a type parameter has no exact runtime type to compare)
        Assert.Contains("Formatter<TWriteBuffer, TReadBuffer, T>", source);
        Assert.Contains("is T case0", source);
    }

    [Fact]
    public void UnboundTagWithoutMatchingCase_ReportsMsgPack011()
    {
        var result = RunGenerator("""
            using System.Collections.Generic;
            using MessagePack;
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag(typeof(List<>), 0)]
            public struct Wrapper<T>
            {
                public Wrapper(Circle value) { }
                public object? Value => null;
            }
            """ + SharedTypes);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack011" && d.GetMessage().Contains("resolve"));
    }

    [Fact]
    public void TypeParameterNameNotFound_ReportsMsgPack011()
    {
        var result = RunGenerator("""
            using MessagePack;
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag("TMissing", 0)]
            public struct Outcome<T>
            {
                public Outcome(T value) { }
                public object? Value => null;
            }
            """ + SharedTypes);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack011" && d.GetMessage().Contains("resolve"));
    }

    [Fact]
    public void WellFormedIUnionStruct_GeneratesTheFormatter()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public struct Good : System.Runtime.CompilerServices.IUnion
            {
                readonly object? value;
                public Good(object? value) => this.value = value;
                public object? Value => value;
            }
            """ + SharedTypes);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "MsgPack011");
        Assert.Contains(result.Results[0].GeneratedSources, s => s.HintName.Contains("Good"));
    }
}
