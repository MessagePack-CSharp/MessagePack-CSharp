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

    // NET9_0_OR_GREATER matches the test project's own compilation: probes exercise the
    // same #if branches of generated code that the real build takes
    public static CSharpCompilation CreateCompilation(string source, string assemblyName = "AnalyzerProbe")
    {
        var parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(LanguageVersion.Latest)
            .WithPreprocessorSymbols("NET9_0_OR_GREATER");
        return CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    public static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(DiagnosticAnalyzer analyzer, string source)
    {
        var compilation = CreateCompilation(source);
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var withAnalyzers = compilation.WithAnalyzers([analyzer]);
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None);
    }
}
