using System.Collections.Immutable;
using MessagePack.SourceGenerator.Analyzers;
using MessagePack.SourceGenerator.Generators;
using MessagePack.Tests.ForeignSurrogateTarget;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// A surrogate declared in assembly B for a type of assembly A ("foreign-target"): its registration rides B's module
// initializer, which never runs when nothing calls into B, so a consumer serializing the A type saw formatter-not-found
// (or, under a contractless tail, a member map) depending on load order. The consumer's generator now adopts such
// surrogates into the consumer's own factory and module initializer (ReferencedSurrogateScanner), which have run before
// the consumer's code serializes anything: no dependence on B ever being touched.
public class ForeignSurrogateTests
{
    // ---- end to end, through the real build: the fixture assemblies and this assembly's generated factory ----

    const string CanaryKey = "MessagePack.Tests.ForeignSurrogate.Initialized";

    [Fact]
    public void ReferencedForeignTargetSurrogate_IsServedByThisAssemblysFactory()
    {
        // this assembly's generated factory serves the target directly, independent of the registry (which the
        // surrogate's own assembly populates too once its module initializer has run)
        var formatter = MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance
            .CreateFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer>(typeof(ForeignTarget));
        Assert.StartsWith("SurrogateFormatter", Assert.IsAssignableFrom<object>(formatter).GetType().Name, StringComparison.Ordinal);

        var bytes = V4.Serialize(new ForeignTarget { X = 42 });
        Assert.Equal(new byte[] { 0x91, 0x2A }, bytes); // the surrogate's [X] array
        Assert.Equal(42, V4.Deserialize<ForeignTarget>(bytes)!.X);

        // the conversion ran code of the surrogate's assembly, so its module initializer has run by now. "Not run yet"
        // cannot be asserted up front in a parallel suite: the CLR activates that module (running its initializer) as
        // soon as any method referencing its types is compiled, and this assembly's generated factory is one, so the
        // first generated type any test class serializes trips it
        Assert.Equal(true, AppDomain.CurrentDomain.GetData(CanaryKey));
    }

    // ---- the generator decision, over in-memory assemblies ----

    const string TargetSource = """
        namespace TargetLib { public class ForeignTarget { public int X { get; set; } } public class Other { public int Y { get; set; } } }
        """;

    const string SurrogateSource = """
        using MessagePack;
        using TargetLib;
        namespace SurrogateLib
        {
            [MessagePackObject]
            public readonly record struct ForeignTargetSurrogate([property: Key(0)] int X) : IMessagePackSurrogate<ForeignTarget, ForeignTargetSurrogate>
            {
                public ForeignTargetSurrogate ToSurrogate(ForeignTarget value) => new(value.X);
                public ForeignTarget ToTarget() => new() { X = X };
            }

            public class LocalType { public int V { get; set; } }

            // own-assembly target: the registry's miss path runs this module's initializer, nothing to adopt
            [MessagePackObject]
            public readonly record struct LocalSurrogate([property: Key(0)] int V) : IMessagePackSurrogate<LocalType, LocalSurrogate>
            {
                public LocalSurrogate ToSurrogate(LocalType value) => new(value.V);
                public LocalType ToTarget() => new() { V = V };
            }

            // not nameable from another assembly: stays with its own initializer
            [MessagePackObject]
            internal readonly record struct HiddenSurrogate([property: Key(0)] int Y) : IMessagePackSurrogate<Other, HiddenSurrogate>
            {
                public HiddenSurrogate ToSurrogate(Other value) => new(value.Y);
                public Other ToTarget() => new() { Y = Y };
            }
        }
        """;

    static MetadataReference Library(string name, string source, params MetadataReference[] extra)
    {
        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest))],
            [.. AnalyzerTestHost.References, .. extra],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        return compilation.ToMetadataReference();
    }

    static (GeneratorDriverRunResult Result, Compilation Updated) RunGenerator(string appSource, params MetadataReference[] references)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(appSource, "ForeignSurrogateApp").AddReferences(references);
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        var updated = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _, CancellationToken.None);
        return (updated.GetRunResult(), output);
    }

    static string Factory(GeneratorDriverRunResult result) =>
        result.GeneratedTrees.Single(t => t.FilePath.EndsWith("GeneratedMessagePackFormatterFactory.g.cs", StringComparison.Ordinal)).ToString();

    [Fact]
    public void Generator_AdoptsForeignTargetSurrogates_AndOnlyThose()
    {
        var target = Library("TargetLib", TargetSource);
        var surrogate = Library("SurrogateLib", SurrogateSource, target);

        // an app with no MessagePack declarations of its own still gets a factory and a registration for the adopted target
        var (result, updated) = RunGenerator("public class App { }", target, surrogate);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(updated.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var factory = Factory(result);
        Assert.Contains("if (type == typeof(global::TargetLib.ForeignTarget)) return new global::MessagePack.Formatters.SurrogateFormatter<TWriteBuffer, TReadBuffer, global::TargetLib.ForeignTarget, global::SurrogateLib.ForeignTargetSurrogate>();", factory, StringComparison.Ordinal);
        Assert.Contains("SourceGeneratedFormatterFactory.Instance.Register<global::TargetLib.ForeignTarget>(GeneratedMessagePackFormatterFactory.Instance);", factory, StringComparison.Ordinal);
        Assert.DoesNotContain("LocalType", factory, StringComparison.Ordinal);   // own-assembly target
        Assert.DoesNotContain("HiddenSurrogate", factory, StringComparison.Ordinal); // internal to its assembly
    }

    [Fact]
    public void Generator_LocalSurrogateOutranksAnAdoptedOne()
    {
        var target = Library("TargetLib", TargetSource);
        var surrogate = Library("SurrogateLib", SurrogateSource, target);
        var (result, _) = RunGenerator("""
            using MessagePack;
            using TargetLib;
            [MessagePackObject]
            public readonly record struct MySurrogate([property: Key(0)] string Text) : IMessagePackSurrogate<ForeignTarget, MySurrogate>
            {
                public MySurrogate ToSurrogate(ForeignTarget value) => new(value.X.ToString());
                public ForeignTarget ToTarget() => new() { X = int.Parse(Text) };
            }
            """, target, surrogate);

        Assert.Empty(result.Diagnostics);
        var factory = Factory(result);
        Assert.Contains("global::MySurrogate>();", factory, StringComparison.Ordinal);
        Assert.DoesNotContain("SurrogateLib.ForeignTargetSurrogate", factory, StringComparison.Ordinal);
    }

    [Fact]
    public void Generator_TwoAssembliesClaimingOneTarget_AdoptsNeitherAndWarns()
    {
        var target = Library("TargetLib", TargetSource);
        var first = Library("SurrogateLib", SurrogateSource, target);
        var second = Library("OtherLib", """
            using MessagePack;
            using TargetLib;
            namespace OtherLib
            {
                [MessagePackObject]
                public readonly record struct AnotherSurrogate([property: Key(0)] long X) : IMessagePackSurrogate<ForeignTarget, AnotherSurrogate>
                {
                    public AnotherSurrogate ToSurrogate(ForeignTarget value) => new(value.X);
                    public ForeignTarget ToTarget() => new() { X = (int)X };
                }
            }
            """, target);
        var (result, _) = RunGenerator("public class App { }", target, first, second);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MsgPack019", diagnostic.Id);
        Assert.Contains("more than one referenced assembly", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedTrees); // nothing adopted, nothing declared: no factory at all
    }

    [Fact]
    public async Task Analyzer_TreatsAnAdoptedTargetAsCovered()
    {
        var target = Library("TargetLib", TargetSource);
        var surrogate = Library("SurrogateLib", SurrogateSource, target);
        var compilation = AnalyzerTestHost.CreateCompilation("""
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public TargetLib.ForeignTarget? Covered { get; set; }
                [Key(1)] public TargetLib.Other? NotCovered { get; set; }
            }
            """).AddReferences(target, surrogate);
        var diagnostics = await compilation.WithAnalyzers([new SerializableMemberTypeAnalyzer()]).GetAnalyzerDiagnosticsAsync(CancellationToken.None);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("TargetLib.Other", diagnostic.GetMessage(), StringComparison.Ordinal); // only the type no surrogate reaches
    }
}
