using System.Collections.Immutable;
using System.Reflection;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace MessagePack.Tests;

// Generator behavior that only downlevel (net48 / netstandard2.x) consumers hit: the polyfill decision and the
// language floor (below).
// The ModuleInitializerAttribute polyfill decision. Every compilation here has the BCL attribute, so the
// decision is probed with a made-up attribute name standing in for it. The case that broke: a downlevel app
// referencing exactly one assembly that carries an internal copy (a shared DTO library built by this generator,
// NativeCompressions.*.Core). Found by name but not applicable here, the copy suppressed the polyfill and the
// generated [ModuleInitializer] failed with CS0122.
public class DownlevelGeneratorTests
{
    const string ProbeName = "Probe.ProbeInitializerAttribute";

    static readonly Func<Compilation, string, bool> NeedsPolyfill = (Func<Compilation, string, bool>)typeof(MessagePackGenerator)
        .GetMethod("NeedsPolyfill", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate(typeof(Func<Compilation, string, bool>));

    static string Declaration(string accessibility) =>
        $"namespace Probe {{ {accessibility} sealed class ProbeInitializerAttribute : System.Attribute {{ }} }}";

    static MetadataReference Library(string name, string source)
    {
        var library = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source)],
            AnalyzerTestHost.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return library.ToMetadataReference();
    }

    static Compilation App(string source, params MetadataReference[] libraries) => CSharpCompilation.Create(
        "PolyfillProbeApp",
        [CSharpSyntaxTree.ParseText(source)],
        [.. AnalyzerTestHost.References, .. libraries],
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    [Fact]
    public void NoCopy_EmitsPolyfill() => Assert.True(NeedsPolyfill(App(""), ProbeName));

    [Fact]
    public void OneInternalCopyInAReference_EmitsPolyfill()
    {
        var app = App("", Library("DtoLibrary", Declaration("internal")));
        Assert.True(NeedsPolyfill(app, ProbeName));
    }

    [Fact]
    public void OnePublicCopyInAReference_UsesIt()
    {
        var app = App("", Library("PublicPolyfill", Declaration("public")));
        Assert.False(NeedsPolyfill(app, ProbeName));
    }

    [Fact]
    public void InternalCopyVisibleThroughInternalsVisibleTo_UsesIt()
    {
        var library = Library("Friend", "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"PolyfillProbeApp\")]\n" + Declaration("internal"));
        Assert.False(NeedsPolyfill(App("", library), ProbeName));
    }

    [Fact]
    public void TwoPublicCopies_EmitsPolyfill()
    {
        // ambiguous between references (CS0433); a source copy outranks both
        var app = App("", Library("PublicA", Declaration("public")), Library("PublicB", Declaration("public")));
        Assert.True(NeedsPolyfill(app, ProbeName));
    }

    [Fact]
    public void SourceCopy_UsesIt()
    {
        var app = App(Declaration("internal"), Library("PublicA", Declaration("public")));
        Assert.False(NeedsPolyfill(app, ProbeName));
    }

    // ---- language floor: net48/netstandard2.x default to C# 7.3, where every generated file failed inside .g.cs ----

    const string Model = """
        using MessagePack;

        [MessagePackObject]
        public class Dto
        {
            [Key(0)] public int Id { get; set; }
        }
        """;

    static (ImmutableArray<Diagnostic> Diagnostics, int GeneratedCount) Generate(string source, LanguageVersion version, params string[] symbols)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(version).WithPreprocessorSymbols(symbols);
        var compilation = CSharpCompilation.Create(
            "LanguageFloorProbe",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            AnalyzerTestHost.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var driver = CSharpGeneratorDriver.Create(
            [new MessagePackGenerator().AsSourceGenerator(), new BufferConstraintsGenerator().AsSourceGenerator(), new FactoryBridgeGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        var result = driver.RunGenerators(compilation).GetRunResult();
        return (result.Diagnostics, result.GeneratedTrees.Length);
    }

    [Fact]
    public void BelowCSharp9_ReportsOnceAndGeneratesNothing()
    {
        var (diagnostics, generated) = Generate(Model, LanguageVersion.CSharp7_3);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MsgPack024", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("C# 9.0", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Equal(0, generated);
    }

    [Fact]
    public void BelowCSharp9_WithoutGeneratedTypes_StaysSilent()
    {
        // a project using only the reflection tier keeps building on C# 7.3
        var (diagnostics, generated) = Generate("public class Plain { public int Id { get; set; } }", LanguageVersion.CSharp7_3);
        Assert.Empty(diagnostics);
        Assert.Equal(0, generated);
    }

    [Fact]
    public void CSharp9_Downlevel_Generates()
    {
        var (diagnostics, generated) = Generate(Model, LanguageVersion.CSharp9);
        Assert.Empty(diagnostics);
        Assert.NotEqual(0, generated);
    }

    [Fact]
    public void Net9BelowCSharp13_ReportsAllowsRefStructFloor()
    {
        var (diagnostics, generated) = Generate(Model, LanguageVersion.CSharp12, "NET9_0_OR_GREATER");
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MsgPack024", diagnostic.Id);
        Assert.Contains("C# 13", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Equal(0, generated);
    }
}
