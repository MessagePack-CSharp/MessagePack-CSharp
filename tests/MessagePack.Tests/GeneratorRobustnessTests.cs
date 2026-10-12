using System.Collections.Immutable;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace MessagePack.Tests;

// Generator inputs that used to throw inside the generator (CS8785: every file it generates for the compilation is
// lost) or to fail only inside the generated files, from the preview1 review's minor findings (F48, F49, F50, F51,
// F52, F58, G203). Each now gets a MsgPack diagnostic at the declaration, and the rest of the compilation keeps its
// formatters.
public class GeneratorRobustnessTests
{
    static (ImmutableArray<Diagnostic> GeneratorDiagnostics, Compilation Output, GeneratorDriverRunResult Run) Generate(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "RobustnessProbe" + Guid.NewGuid().ToString("N"));
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
        var driver = CSharpGeneratorDriver.Create(
            [new MessagePackGenerator().AsSourceGenerator(), new BufferConstraintsGenerator().AsSourceGenerator(), new FactoryBridgeGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        var ran = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        return (diagnostics, output, ran.GetRunResult());
    }

    static void AssertCompiles(Compilation output) =>
        Assert.Empty(output.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));

    // ---- F48: an undefined KeyNamingPolicy value ----

    [Fact]
    public void UndefinedKeyNamingPolicy_IsADiagnostic_NotAGeneratorCrash()
    {
        var (diagnostics, _, run) = Generate("""
            using MessagePack;
            [MessagePackObject((KeyNamingPolicy)42)]
            public class Odd { public int Id { get; set; } }
            [MessagePackObject]
            public class Fine { [Key(0)] public int Id { get; set; } }
            """);
        Assert.Contains(diagnostics, static d => d.Id == "MsgPack025");
        Assert.DoesNotContain(diagnostics, static d => d.Id == "CS8785");
        Assert.Contains(run.GeneratedTrees, static t => t.FilePath.Contains("Fine", StringComparison.Ordinal)); // the other type still generates
    }

    // ---- F49: [MessagePackObject] on two partial declarations of one type ----

    [Fact]
    public void DuplicateObjectAttributeOnPartialDeclarations_EmitsOneFormatter()
    {
        var (diagnostics, output, run) = Generate("""
            using MessagePack;
            [MessagePackObject] public partial class Twice { [Key(0)] public int A { get; set; } }
            [MessagePackObject] public partial class Twice { [Key(1)] public int B { get; set; } }
            """);
        Assert.DoesNotContain(diagnostics, static d => d.Id == "CS8785");
        Assert.Single(run.GeneratedTrees, static t => t.FilePath.Contains("Twice", StringComparison.Ordinal));
        Assert.Contains(output.GetDiagnostics(), static d => d.Id == "CS0579"); // the duplicate itself stays the compiler's error
    }

    // ---- F50: an inaccessible union case ----

    [Fact]
    public void PrivateUnionCase_IsMsgPack006_NotCS0122()
    {
        var (diagnostics, output, _) = Generate("""
            using MessagePack;
            [MessagePackObject]
            [UnionTag(typeof(Hidden), 0)]
            public abstract partial class Root
            {
                [MessagePackObject] private sealed class Hidden : Root { [Key(0)] public int X { get; set; } }
            }
            """);
        Assert.Contains(diagnostics, static d => d.Id == "MsgPack006" && d.GetMessage().Contains("UnionTag", StringComparison.Ordinal));
        Assert.DoesNotContain(output.GetDiagnostics(), static d => d.Id == "CS0122");
    }

    // ---- F51: a ref struct ----

    [Fact]
    public void RefStruct_IsMsgPack005_NotGeneratedFileErrors()
    {
        var (diagnostics, output, _) = Generate("""
            using MessagePack;
            [MessagePackObject]
            public ref struct Window { [Key(0)] public int Start; }
            """);
        Assert.Contains(diagnostics, static d => d.Id == "MsgPack005" && d.GetMessage().Contains("ref struct", StringComparison.Ordinal));
        AssertCompiles(output);
    }

    // ---- F52: a member under [Obsolete(error: true)] ----

    [Fact]
    public void ObsoleteErrorMember_IsMsgPack026_NotCS0619()
    {
        var (diagnostics, output, _) = Generate("""
            using System;
            using MessagePack;
            [MessagePackObject]
            public class Legacy
            {
                [Key(0)] public int Id { get; set; }
                [Key(1), Obsolete("gone", error: true)] public int Old { get; set; }
            }
            """);
        Assert.Contains(diagnostics, static d => d.Id == "MsgPack026" && d.GetMessage().Contains("IgnoreMember", StringComparison.Ordinal));
        Assert.DoesNotContain(output.GetDiagnostics(), static d => d.Id == "CS0619");
    }

    // ---- F58: a partial formatter / factory nested in a non-partial type ----

    [Fact]
    public void PartialFormatterInNonPartialOuter_IsMsgPack027_NotCS0260()
    {
        var (diagnostics, output, _) = Generate("""
            using MessagePack;
            using SerializerFoundation;
            public static class Outer
            {
                public sealed partial class IntFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
                {
                    public void Initialize(MessagePackFormatterResolver resolver) { }
                    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value) { }
                    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value) { }
                }
                public sealed partial class IntFactory : MessagePackFormatterFactory
                {
                    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(global::System.Type type) => null;
                }
            }
            """);
        Assert.Equal(2, diagnostics.Count(static d => d.Id == "MsgPack027"));
        Assert.DoesNotContain(output.GetDiagnostics(), static d => d.Id == "CS0260");
    }

    // ---- G203: a string key containing a character C# treats as a line terminator ----

    [Fact]
    public void StringKeyWithLineSeparator_StaysInsideItsComment()
    {
        var (_, output, run) = Generate("""
            using MessagePack;
            [MessagePackObject]
            public class Odd { [Key("a\u2028b")] public int Id { get; set; } }
            """);
        AssertCompiles(output);
        var formatter = run.GeneratedTrees.Single(static t => t.FilePath.Contains("Odd", StringComparison.Ordinal)).ToString();
        Assert.DoesNotContain("\u2028", formatter, StringComparison.Ordinal);
        Assert.Contains("\\u2028", formatter, StringComparison.Ordinal);
    }
}
