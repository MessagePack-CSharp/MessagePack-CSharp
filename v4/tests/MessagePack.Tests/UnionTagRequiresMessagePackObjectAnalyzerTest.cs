using MessagePack.SourceGenerator.Analyzers;
using Microsoft.CodeAnalysis;

namespace MessagePack.Tests;

// MsgPack103: union discovery is driven by [MessagePackObject] alone, so a base carrying
// only [UnionTag] silently gets no formatter — the analyzer turns that into an error.
public class UnionTagRequiresMessagePackObjectAnalyzerTest
{
    [Fact]
    public async Task UnionTagWithoutMessagePackObject_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new UnionTagRequiresMessagePackObjectAnalyzer(), """
            using MessagePack;
            [UnionTag(typeof(Circle), 0)]
            public interface IShape { }
            [MessagePackObject]
            public class Circle : IShape
            {
                [Key(0)] public double R { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(UnionTagRequiresMessagePackObjectAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("IShape", diagnostic.GetMessage());
    }

    [Fact]
    public async Task UnionTagWithMessagePackObject_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new UnionTagRequiresMessagePackObjectAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public interface IShape { }
            [MessagePackObject]
            public class Circle : IShape
            {
                [Key(0)] public double R { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GenericUnionTagWithoutMessagePackObject_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new UnionTagRequiresMessagePackObjectAnalyzer(), """
            using MessagePack;
            [UnionTag<Circle>(0)]
            public interface IShape { }
            [MessagePackObject]
            public class Circle : IShape
            {
                [Key(0)] public double R { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(UnionTagRequiresMessagePackObjectAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("IShape", diagnostic.GetMessage());
    }

    [Fact]
    public async Task PlainMessagePackObject_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new UnionTagRequiresMessagePackObjectAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Plain
            {
                [Key(0)] public int X { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }
}
