using MessagePack.SourceGenerator.Analyzers;
using Microsoft.CodeAnalysis;

namespace MessagePack.Tests;

// MsgPack106: a closed [UnionTag] root enumerates its complete derived set in metadata, so
// every concrete type in the closure must be tagged — an untagged one serializes as nil.
public class ClosedUnionCoverageAnalyzerTest
{
    // the analyzer reads the [IsClosedType] attribute the `closed` modifier lowers to, so
    // the tests apply it directly and stay off the preview compiler
    const string ClosedPolyfill = """

        namespace System.Runtime.CompilerServices
        {
            internal sealed class IsClosedTypeAttribute : System.Attribute
            {
                public System.Type[]? DerivedTypes { get; set; }
            }
        }
        """;

    [Fact]
    public async Task UntaggedConcreteDerived_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new ClosedUnionCoverageAnalyzer(), """
            using MessagePack;
            using System.Runtime.CompilerServices;

            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            [IsClosedType(DerivedTypes = new[] { typeof(Circle), typeof(Square) })]
            public abstract class Shape { }

            [MessagePackObject] public class Circle : Shape { [Key(0)] public double R { get; set; } }
            [MessagePackObject] public class Square : Shape { [Key(0)] public double S { get; set; } }
            """ + ClosedPolyfill);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(ClosedUnionCoverageAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Square", diagnostic.GetMessage());
    }

    [Fact]
    public async Task FullyTaggedClosure_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new ClosedUnionCoverageAnalyzer(), """
            using MessagePack;
            using System.Runtime.CompilerServices;

            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            [UnionTag<Square>(1)]
            [IsClosedType(DerivedTypes = new[] { typeof(Circle), typeof(Square) })]
            public abstract class Shape { }

            [MessagePackObject] public class Circle : Shape { [Key(0)] public double R { get; set; } }
            [MessagePackObject] public class Square : Shape { [Key(0)] public double S { get; set; } }
            """ + ClosedPolyfill);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NestedClosedIntermediate_RecursesToItsCases()
    {
        // Middle is closed (implicitly abstract): not a case itself, but its own derived
        // list joins the root's universe — the untagged Square inside it reports
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new ClosedUnionCoverageAnalyzer(), """
            using MessagePack;
            using System.Runtime.CompilerServices;

            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            [IsClosedType(DerivedTypes = new[] { typeof(Circle), typeof(Middle) })]
            public abstract class Shape { }

            [MessagePackObject] public class Circle : Shape { [Key(0)] public double R { get; set; } }

            [IsClosedType(DerivedTypes = new[] { typeof(Square) })]
            public abstract class Middle : Shape { }

            [MessagePackObject] public class Square : Middle { [Key(0)] public double S { get; set; } }
            """ + ClosedPolyfill);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Square", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ConcreteDescendantBehindOpenIntermediate_StillReports()
    {
        // closed confines the hierarchy to the assembly, so even a concrete type behind
        // an abstract non-closed intermediate is knowable — and untagged, it reports
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new ClosedUnionCoverageAnalyzer(), """
            using MessagePack;
            using System.Runtime.CompilerServices;

            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            [IsClosedType(DerivedTypes = new[] { typeof(Circle), typeof(Open) })]
            public abstract class Shape { }

            [MessagePackObject] public class Circle : Shape { [Key(0)] public double R { get; set; } }

            public abstract class Open : Shape { }
            public class Hidden : Open { }
            """ + ClosedPolyfill);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Hidden", diagnostic.GetMessage());
    }

    [Fact]
    public async Task OpenUnionRoot_DemandsNothing()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new ClosedUnionCoverageAnalyzer(), """
            using MessagePack;

            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public abstract class Shape { }

            [MessagePackObject] public class Circle : Shape { [Key(0)] public double R { get; set; } }
            public class Square : Shape { }
            """ + ClosedPolyfill);
        Assert.Empty(diagnostics);
    }
}
