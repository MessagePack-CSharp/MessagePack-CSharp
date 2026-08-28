using MessagePack.SourceGenerator.Analyzers;

namespace MessagePack.Tests;

// MsgPack111: more than 8 [Key(int)] holes warns, uniformly — the array wire form pads
// every hole with a nil byte per instance. Small versioning gaps (retired keys) stay
// under the bar; a type with more retired keys than that suppresses per type.
public class SparseIntKeyAnalyzerTest
{
    [Fact]
    public async Task TensStyleNumbering_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new SparseIntKeyAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Tens
            {
                [Key(0)] public int A { get; set; }
                [Key(10)] public int B { get; set; }
                [Key(20)] public int C { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(SparseIntKeyAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("21-slot array", diagnostic.GetMessage());
        Assert.Contains("18 nil slots", diagnostic.GetMessage());
    }

    [Fact]
    public async Task StrayHugeKey_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new SparseIntKeyAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Huge
            {
                [Key(0)] public int A { get; set; }
                [Key(100)] public int B { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("99 nil slots", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ContiguousKeys_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new SparseIntKeyAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Contiguous
            {
                [Key(0)] public int A { get; set; }
                [Key(1)] public int B { get; set; }
                [Key(2)] public int C { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ExactlyEightHoles_NoDiagnostic()
    {
        // holes = 8 sits ON the threshold: retired-key versioning territory, not a warning
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new SparseIntKeyAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Boundary
            {
                [Key(0)] public int A { get; set; }
                [Key(1)] public int B { get; set; }
                [Key(10)] public int C { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NineHolesAcrossManyMembers_Reports()
    {
        // uniform rule: even when the keyed members outnumber the holes, 9 holes warn —
        // a type whose retired keys legitimately exceed the bar suppresses per type
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new SparseIntKeyAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Versioned
            {
                [Key(0)] public int A { get; set; }
                [Key(2)] public int B { get; set; }
                [Key(4)] public int C { get; set; }
                [Key(6)] public int D { get; set; }
                [Key(8)] public int E { get; set; }
                [Key(10)] public int F { get; set; }
                [Key(12)] public int G { get; set; }
                [Key(14)] public int H { get; set; }
                [Key(16)] public int I { get; set; }
                [Key(18)] public int J { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("9 nil slots", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ReservedLeadingRegion_Reports()
    {
        // [Key(10)]-start "reserved" numbering is not tolerated: the leading slots are
        // pure padding on every instance
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new SparseIntKeyAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Reserved
            {
                [Key(10)] public int A { get; set; }
                [Key(11)] public int B { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("10 nil slots", diagnostic.GetMessage());
    }

    [Fact]
    public async Task StringKeyMapMode_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new SparseIntKeyAnalyzer(), """
            using MessagePack;
            [MessagePackObject(true)]
            public class MapMode
            {
                public int A { get; set; }
                public int B { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task InheritedKeysFillTheGap_NoDiagnostic()
    {
        // the wire array spans the whole hierarchy: base keys make the derived type contiguous
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new SparseIntKeyAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Base
            {
                [Key(0)] public int A { get; set; }
                [Key(1)] public int B { get; set; }
            }
            [MessagePackObject]
            public class Derived : Base
            {
                [Key(2)] public int C { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }
}
