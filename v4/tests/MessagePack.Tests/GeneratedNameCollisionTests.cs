using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace MessagePack.Tests;

// Generated formatter names flatten the full type name into one identifier, and that
// encoding must be INJECTIVE: if two types map to one name, the build dies loudly but
// cryptically (CS0101 in MessagePack.Generated, or a duplicate-hint generator failure).
// Sanitize doubles literal underscores and drops the space of ", " so separator
// underscores (single) never alias identifier underscores (even runs).
public class GeneratedNameCollisionTests
{
    static GeneratorDriverRunResult RunGenerator(string source, out Compilation updated)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "NameCollisionProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out updated, out _, CancellationToken.None).GetRunResult();
        return result;
    }

    [Fact]
    public void UnderscoreVersusNamespaceSeparator()
    {
        var result = RunGenerator("""
            using MessagePack;
            namespace Ns { [MessagePackObject] public class A_B { [Key(0)] public int X { get; set; } } }
            namespace Ns.A { [MessagePackObject] public class B { [Key(0)] public int X { get; set; } } }
            """, out var updated);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains(result.GeneratedTrees, static t => t.FilePath.Contains("Ns_A__BFormatter"));
        Assert.Contains(result.GeneratedTrees, static t => t.FilePath.Contains("Ns_A_BFormatter"));
    }

    [Fact]
    public void ParameterListVersusUnderscoredParameterName()
    {
        // Foo<T, U> (arity 2) and Foo<T_U> (arity 1) legally coexist; their formatter
        // names must too
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject] public class Foo<T, U> { [Key(0)] public T? A { get; set; } [Key(1)] public U? B { get; set; } }
            [MessagePackObject] public class Foo<T_U> { [Key(0)] public T_U? A { get; set; } }
            """, out var updated);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains(result.GeneratedTrees, static t => t.FilePath.Contains("Foo_T_U_Formatter"));
        Assert.Contains(result.GeneratedTrees, static t => t.FilePath.Contains("Foo_T__U_Formatter"));
    }
}
