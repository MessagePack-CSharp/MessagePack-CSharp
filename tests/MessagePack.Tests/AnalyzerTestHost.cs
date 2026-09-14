using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.Tests;

// Shared probe harness for analyzers that run against the REAL MessagePack surface
// (SerializableMemberTypeAnalyzerTest, UnionTagRequiresMessagePackObjectAnalyzerTest):
// compiles the given source with the runtime's reference set plus the actual
// MessagePack/SerializerFoundation assemblies and returns the analyzer diagnostics.
static class AnalyzerTestHost
{
    public static readonly MetadataReference[] References = BuildReferences();

    static MetadataReference[] BuildReferences()
    {
        var references = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator))
        {
            var file = Path.GetFileName(path);
            // the v3 oracle assemblies define the same MessagePack.* attribute names as
            // the product and would make every probe source ambiguous (CS0433)
            if (file is "MessagePackV3.dll" or "MessagePack.Annotations.dll" || !seen.Add(file))
            {
                continue;
            }
            references.Add(MetadataReference.CreateFromFile(path));
        }
        // the testhost TPA may not carry the app's own dependencies; add them explicitly
        if (seen.Add(Path.GetFileName(typeof(MessagePackSerializer).Assembly.Location)))
        {
            references.Add(MetadataReference.CreateFromFile(typeof(MessagePackSerializer).Assembly.Location));
        }
        if (seen.Add(Path.GetFileName(typeof(SerializerFoundation.IWriteBuffer).Assembly.Location)))
        {
            references.Add(MetadataReference.CreateFromFile(typeof(SerializerFoundation.IWriteBuffer).Assembly.Location));
        }
        return [.. references];
    }

    // NET10_0_OR_GREATER matches the test project's own compilation: probes exercise the
    // same #if branches of generated code that the real build takes
    public static CSharpCompilation CreateCompilation(string source, string assemblyName = "AnalyzerProbe")
    {
        var parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(LanguageVersion.Latest)
            .WithPreprocessorSymbols("NET10_0_OR_GREATER");
        return CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    // Compiles the source through the real generator (or without it) and loads the result
    // into the default load context, for shapes the generator refuses or warns about that a
    // test still needs to execute (the generated tier serving them, or the reflection tier
    // alone when withGenerator is false)
    public static System.Reflection.Assembly CompileAndLoad(string source, bool withGenerator, params MetadataReference[] additionalReferences)
    {
        var compilation = CreateCompilation(source, "Probe" + Guid.NewGuid().ToString("N")).AddReferences(additionalReferences);
        if (withGenerator)
        {
            var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
            var driver = CSharpGeneratorDriver.Create([new MessagePack.SourceGenerator.Generators.MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var generatorDiagnostics);
            Assert.Empty(generatorDiagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
            compilation = (CSharpCompilation)updated;
        }
        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        var assembly = System.Reflection.Assembly.Load(stream.ToArray());
        if (withGenerator)
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }
        return assembly;
    }

    public static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(DiagnosticAnalyzer analyzer, string source)
    {
        var compilation = CreateCompilation(source);
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var withAnalyzers = compilation.WithAnalyzers([analyzer]);
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None);
    }
}
