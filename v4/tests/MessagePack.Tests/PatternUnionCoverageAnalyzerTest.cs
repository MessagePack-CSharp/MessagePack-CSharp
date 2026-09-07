using MessagePack.SourceGenerator.Analyzers;
using Microsoft.CodeAnalysis;

namespace MessagePack.Tests;

// MsgPack107: a pattern union's creation members enumerate its complete case universe, so a
// tagged union with an untagged case is knowably incomplete (it throws at runtime).
public class PatternUnionCoverageAnalyzerTest
{
    const string SharedTypes = """

        [MessagePackObject]
        public class Circle { [Key(0)] public double R { get; set; } }

        [MessagePackObject]
        public class Square { [Key(0)] public double S { get; set; } }

        namespace System.Runtime.CompilerServices
        {
            internal interface IUnion { object? Value { get; } }

            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
            internal sealed class UnionAttribute : System.Attribute { }
        }
        """;

    [Fact]
    public async Task UntaggedConstructorCase_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new PatternUnionCoverageAnalyzer(), """
            using MessagePack;
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public struct Shape
            {
                public Shape(Circle value) { }
                public Shape(Square value) { }
                public object? Value => null;
            }
            """ + SharedTypes);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(PatternUnionCoverageAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Square", diagnostic.GetMessage());
    }

    [Fact]
    public async Task FullyTaggedCases_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new PatternUnionCoverageAnalyzer(), """
            using MessagePack;
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            [UnionTag<Square>(1)]
            public struct Shape
            {
                public Shape(Circle value) { }
                public Shape(Square value) { }
                public object? Value => null;
            }
            """ + SharedTypes);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ObjectCatchAllConstructor_DemandsNothing()
    {
        // an object parameter is a catch-all, not a case declaration
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new PatternUnionCoverageAnalyzer(), """
            using MessagePack;
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public struct Shape
            {
                public Shape(object? value) { }
                public object? Value => null;
            }
            """ + SharedTypes);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task UntaggedProviderCreateCase_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new PatternUnionCoverageAnalyzer(), """
            using MessagePack;
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public class Shape : Shape.IUnionMembers
            {
                object? heldValue;
                public interface IUnionMembers
                {
                    public static Shape Create(Circle value) => new Shape { heldValue = value };
                    public static Shape Create(Square value) => new Shape { heldValue = value };
                    public object? Value { get; }
                }
                object? IUnionMembers.Value => heldValue;
            }
            """ + SharedTypes);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Square", diagnostic.GetMessage());
    }

    [Fact]
    public async Task UnmarkedTypeWithConstructors_DemandsNothing()
    {
        // no [Union]/IUnion marker: not a pattern union, constructors imply nothing
        // (the [UnionTag] misuse itself is the generator's MsgPack011)
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new PatternUnionCoverageAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public struct Shape
            {
                public Shape(Circle value) { }
                public Shape(Square value) { }
                public object? Value => null;
            }
            """ + SharedTypes);
        Assert.Empty(diagnostics);
    }
}
