using MessagePack.SourceGenerator.Analyzers;

namespace MessagePack.Tests;

// MsgPack110: annotations the serializer silently ignores — [Key]+[IgnoreMember] on one
// member, [Key]/[IgnoreMember] on statics, and [Key] on a struct/sealed class that
// carries no type annotation (nothing can inherit it into an annotated hierarchy, and
// contractless never reads [Key]), and [Key] on a non-public member without AllowPrivate = true.
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

    // [Key] on a member discovery never sees without AllowPrivate: non-public members, and public
    // properties whose getter is non-public. A public property with a private setter is discovered
    // (the getter is public), so it stays silent.
    [Fact]
    public async Task KeyOnNonPublicMember_WithoutAllowPrivate_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new IneffectiveAnnotationAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public int X { get; set; }
                [Key(1)] public int Half { get; private set; }
                [Key(2)] private int secret;
                [Key(3)] internal int Score { get; set; }
                [Key(4)] public int Hidden { private get; set; }
                [Key(5)] protected int Inherited;
            }
            """);
        Assert.Equal(4, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal(IneffectiveAnnotationAnalyzer.DiagnosticId, d.Id));
        Assert.All(diagnostics, d => Assert.Contains("AllowPrivate = true", d.GetMessage()));
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("Holder.secret") && d.GetMessage().Contains("it is not public"));
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("Holder.Score"));
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("Holder.Hidden") && d.GetMessage().Contains("its getter is not public"));
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("Holder.Inherited"));
    }

    [Fact]
    public async Task KeyOnNonPublicMember_WithAllowPrivate_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new IneffectiveAnnotationAnalyzer(), """
            using MessagePack;
            [MessagePackObject(AllowPrivate = true)]
            public partial class Holder
            {
                [Key(0)] public int X { get; set; }
                [Key(1)] private int secret;
                [Key(2)] public int Hidden { private get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NonPublicMember_WithoutKey_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new IneffectiveAnnotationAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public int X { get; set; }
                private int cache;
                [IgnoreMember] private int ignored;
            }
            """);
        Assert.Empty(diagnostics);
    }
}
