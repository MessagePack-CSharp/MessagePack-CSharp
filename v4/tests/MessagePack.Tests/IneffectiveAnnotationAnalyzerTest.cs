using MessagePack.SourceGenerator.Analyzers;

namespace MessagePack.Tests;

// MsgPack110: annotations the serializer silently ignores — [Key]+[IgnoreMember] on one
// member, [Key]/[IgnoreMember] on statics, and [Key] on a struct/sealed class that
// carries no type annotation (nothing can inherit it into an annotated hierarchy, and
// contractless never reads [Key]).
public class IneffectiveAnnotationAnalyzerTest
{
    [Fact]
    public async Task KeyAndIgnoreTogether_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new IneffectiveAnnotationAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)]
                [IgnoreMember]
                public int X { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(IneffectiveAnnotationAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("both", diagnostic.GetMessage());
    }

    [Fact]
    public async Task KeyOnStaticMember_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new IneffectiveAnnotationAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public int X { get; set; }
                [Key(1)] public static int Counter { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("static", diagnostic.GetMessage());
    }

    [Fact]
    public async Task KeyOnSealedUnannotatedType_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new IneffectiveAnnotationAnalyzer(), """
            using MessagePack;
            public sealed class Plain
            {
                [Key(0)] public int X { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("cannot serve as a base type", diagnostic.GetMessage());
    }

    [Fact]
    public async Task IgnoreMemberOnSealedUnannotatedType_Silent()
    {
        // the contractless tier honors [IgnoreMember], so it is NOT a dead annotation
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new IneffectiveAnnotationAnalyzer(), """
            using MessagePack;
            public sealed class Plain
            {
                public int X { get; set; }
                [IgnoreMember] public string? Skip { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task KeyOnUnsealedUnannotatedBase_Silent()
    {
        // a derived [MessagePackObject] type honors a base member's [Key]
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new IneffectiveAnnotationAnalyzer(), """
            using MessagePack;
            public class PlainBase
            {
                [Key(0)] public int X { get; set; }
            }
            [MessagePackObject]
            public class Derived : PlainBase
            {
                [Key(1)] public int Y { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AnnotatedTypes_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new IneffectiveAnnotationAnalyzer(), """
            using MessagePack;
            using System.Runtime.Serialization;
            [MessagePackObject]
            public sealed class Holder
            {
                [Key(0)] public int X { get; set; }
                [IgnoreMember] public string? Skip { get; set; }
            }
            [DataContract]
            public sealed class Contracted
            {
                [DataMember(Order = 0)] public int X { get; set; }
                [IgnoreDataMember] public string? Skip { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }
}
