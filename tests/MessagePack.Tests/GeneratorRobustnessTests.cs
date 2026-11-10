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

    // the fix MsgPack026 names: an [IgnoreMember] member (and a non-public one without [Key]) is never referenced
    [Fact]
    public void ObsoleteErrorMember_Ignored_Generates()
    {
        var (diagnostics, output, run) = Generate("""
            using System;
            using MessagePack;
            [MessagePackObject]
            public class Legacy
            {
                [Key(0)] public int Id { get; set; }
                [IgnoreMember, Obsolete("gone", error: true)] public int Old { get; set; }
                [Obsolete("gone", error: true)] internal int Hidden { get; set; }
            }
            [MessagePackObject(true)]
            public class LegacyMap
            {
                public int Id { get; set; }
                [IgnoreMember, Obsolete("gone", error: true)] public int Old { get; set; }
            }
            """);
        Assert.DoesNotContain(diagnostics, static d => d.Id == "MsgPack026");
        Assert.Contains(run.GeneratedTrees, static t => t.FilePath.Contains("LegacyFormatter", StringComparison.Ordinal));
        Assert.Contains(run.GeneratedTrees, static t => t.FilePath.Contains("LegacyMapFormatter", StringComparison.Ordinal));
        AssertCompiles(output);
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

    // ---- line-by-line audit before rc.1 (2026-11-10): each shape broke the consumer's build or the generator ----

    // negative numbers were written into source with the build machine's culture: under sv-SE, nb-NO or fi-FI (ICU) the
    // sign is U+2212, so a union tag of -1 generated a `case` label the compiler rejects (CS1056), and so did an
    // array-mode type whose only member is MessagePackUnknownMembers (its highest key is -1)
    [Fact]
    public void NegativeNumbers_AreSpelledWithTheInvariantCulture()
    {
        const char MinusSign = (char)0x2212;
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        var minus = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
        minus.NumberFormat.NegativeSign = MinusSign.ToString();
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = minus;
            var (diagnostics, output, run) = Generate("""
                using MessagePack;
                [MessagePackObject, UnionTag(typeof(A), -1), UnionTag(typeof(B), -2147483648)] public interface IRoot { }
                [MessagePackObject] public class A : IRoot { [Key(0)] public int X { get; set; } }
                [MessagePackObject] public class B : IRoot { [Key(0)] public int X { get; set; } }
                [MessagePackObject] public class Bag { public MessagePackUnknownMembers? Rest { get; set; } }
                """);
            Assert.DoesNotContain(diagnostics, static d => d.Severity == DiagnosticSeverity.Error);
            AssertCompiles(output);
            Assert.DoesNotContain(run.GeneratedTrees, static t => t.ToString().Contains(MinusSign));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = saved;
        }
    }

    // a one-letter prefix in front of a member name spells keywords: the field of `or` was `for`, the pattern variable
    // of a nullable `oid` was `void`, and that of `alue` was the parameter `value`
    [Fact]
    public void MemberNamesThatSpelledKeywordsBehindAPrefix_Compile()
    {
        var (diagnostics, output, _) = Generate("""
            using MessagePack;
            [MessagePackObject(true)]
            public class Rule
            {
                public string[]? or { get; set; }
                public string[]? ixed { get; set; }
                public string[]? alse { get; set; }
                public int? oid { get; set; }
                public int? alue { get; set; }
                public long? irtual { get; set; }
            }
            """);
        Assert.DoesNotContain(diagnostics, static d => d.Severity == DiagnosticSeverity.Error);
        AssertCompiles(output);
    }

    // hint names are compared without regard to case: two types whose full names differ only in case added one hint
    // twice, which fails the generator as a whole (CS8785 is a warning, the build went on without generated files)
    [Fact]
    public void TypeNamesDifferingOnlyInCase_BothGetAFormatter()
    {
        var (diagnostics, output, run) = Generate("""
            using MessagePack;
            namespace App.Dto { [MessagePackObject] public class User { [Key(0)] public int X { get; set; } public enum Kind { A } } }
            namespace App.DTO { [MessagePackObject] public class User { [Key(0)] public int X { get; set; } public enum Kind { A } } }
            namespace App { [MessagePackObject(true, EnumAsString = true)] public class Holder { public Dto.User.Kind One { get; set; } public DTO.User.Kind Two { get; set; } } }
            """);
        Assert.DoesNotContain(diagnostics, static d => d.Id == "CS8785");
        AssertCompiles(output);
        Assert.Equal(2, run.GeneratedTrees.Count(static t => System.IO.Path.GetFileName(t.FilePath).StartsWith("App_Dto_UserFormatter", StringComparison.OrdinalIgnoreCase)));
        // an ordinary compilation keeps its plain hint names
        var (_, _, plain) = Generate("using MessagePack; [MessagePackObject] public class Solo { [Key(0)] public int X { get; set; } }");
        Assert.Contains(plain.GeneratedTrees, static t => System.IO.Path.GetFileName(t.FilePath) == "SoloFormatter.g.cs");
    }

    // an int key had no upper bound: [Key(3000000)] was still generating after a minute at 1.7 GB, and the loop
    // variable wrapped for int.MaxValue. Above 65,535 it is MsgPack008 (the reflection tier refuses the same)
    [Theory]
    [InlineData(65536)]
    [InlineData(3000000)]
    [InlineData(int.MaxValue)]
    public void IntKeyAboveTheLimit_IsMsgPack008(int key)
    {
        var (diagnostics, _, run) = Generate($$"""
            using MessagePack;
            [MessagePackObject] public class Huge { [Key({{key}})] public int X { get; set; } }
            [MessagePackObject] public class Fine { [Key(0)] public int Id { get; set; } }
            """);
        Assert.Contains(diagnostics, static d => d.Id == "MsgPack008" && d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(run.GeneratedTrees, static t => t.FilePath.Contains("Huge", StringComparison.Ordinal));
        Assert.Contains(run.GeneratedTrees, static t => t.FilePath.Contains("Fine", StringComparison.Ordinal));
    }

    [Fact]
    public void IntKeyAboveTheLimit_ReflectionTierRefusesItToo()
    {
        // compiled without the generator, so the reflection tier serves it
        var assembly = AnalyzerTestHost.CompileAndLoad("using MessagePack; [MessagePackObject] public class HugeKey { [Key(70000)] public int X { get; set; } }", withGenerator: false);
        var type = assembly.GetType("HugeKey")!;
        var exception = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Serialize(type, Activator.CreateInstance(type)));
        Assert.Contains("65535", exception.Message);
    }

    // TypedConstant.Value throws for an array argument: a derived KeyAttribute taking `params int[]` crashed the generator
    [Fact]
    public void DerivedKeyAttributeWithAnArrayArgument_IsADiagnostic_NotAGeneratorCrash()
    {
        var (diagnostics, _, run) = Generate("""
            using MessagePack;
            public class PathKeyAttribute : KeyAttribute { public PathKeyAttribute(params int[] path) : base(path[0]) { } }
            [MessagePackObject] public class Odd { [PathKey(0)] public int X { get; set; } }
            [MessagePackObject] public class Fine { [Key(0)] public int Id { get; set; } }
            """);
        Assert.DoesNotContain(diagnostics, static d => d.Id == "CS8785");
        Assert.Contains(diagnostics, static d => d.Id == "MsgPack005");
        Assert.Contains(run.GeneratedTrees, static t => t.FilePath.Contains("Fine", StringComparison.Ordinal));
    }

    // a keyed required member whose setter the generated code does not use (internal, on an internal type) was left
    // out of the object initializer the compiler demands: CS9035 in the generated file
    [Fact]
    public void RequiredMemberWithANonPublicSetter_Compiles()
    {
        var (_, output, _) = Generate("""
            using MessagePack;
            [MessagePackObject] internal class Hidden { [Key(0)] public required int X { get; internal set; } [Key(1)] public int Y { get; set; } }
            [MessagePackObject(true)] internal class HiddenMap { public required int X { get; internal set; } public int Y { get; set; } }
            [MessagePackObject] internal class HiddenCtor { public HiddenCtor(int x) { X = x; } [Key(0)] public required int X { get; internal set; } }
            """);
        AssertCompiles(output);
    }

    // a file-local surrogate is internal by accessibility but nameable only in its own file: the generated factory
    // referred to it from another one (CS0400)
    [Fact]
    public void FileLocalSurrogate_IsMsgPack019_NotCS0400()
    {
        var (diagnostics, output, _) = Generate("""
            using MessagePack;
            public class Target { public int X; }
            [MessagePackObject]
            file struct TargetSurrogate : IMessagePackSurrogate<Target, TargetSurrogate>
            {
                [Key(0)] public int X { get; set; }
                public TargetSurrogate ToSurrogate(Target value) => new() { X = value.X };
                public Target ToTarget() => new() { X = X };
            }
            """);
        Assert.Contains(diagnostics, static d => d.Id == "MsgPack019");
        AssertCompiles(output);
    }

    // the expanding-generic guard took any namespace starting with "System" for the BCL: in `Systems.Game` a type
    // whose member is a larger instantiation of itself was closed without end
    [Fact]
    public async Task ExpandingGenericInANamespaceThatStartsWithSystem_IsCut()
    {
        var finished = Task.Run(() => Generate("""
            using System.Collections.Generic;
            using MessagePack;
            namespace Systems.Game
            {
                [MessagePackObject] public class Node<T> { [Key(0)] public Node<List<T>>? Next; }
                [MessagePackObject] public class Root { [Key(0)] public Node<int>? N; }
            }
            """), TestContext.Current.CancellationToken);
        Assert.Same(finished, await Task.WhenAny(finished, Task.Delay(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken)));
        Assert.Contains((await finished).GeneratorDiagnostics, static d => d.Id == "MsgPack029");
    }

    // a keyword as a namespace segment: the default display format escapes it (`Acme.@event`), which is not a legal
    // hint name (the factory bridge failed with CS8785) and not the key the reflection tier and v3 use for a
    // shadowing member (Type.FullName: `Acme.event.Derived.Id`)
    [Fact]
    public void KeywordNamespaceSegment_IsSpelledAsInMetadata()
    {
        var (diagnostics, output, run) = Generate("""
            using MessagePack;
            namespace Acme.@event
            {
                public sealed partial class F : MessagePackFormatterFactory
                {
                    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(global::System.Type type) => null;
                }
                [MessagePackObject(true)] public class Base { public int Id { get; set; } }
                [MessagePackObject(true)] public class Derived : Base { public new int Id { get; set; } }
            }
            """);
        Assert.DoesNotContain(diagnostics, static d => d.Id == "CS8785");
        AssertCompiles(output);
        var derived = run.GeneratedTrees.Single(static t => t.FilePath.Contains("DerivedFormatter", StringComparison.Ordinal)).ToString();
        Assert.Contains("0x65, 0x2e, 0x65, 0x76, 0x65, 0x6e, 0x74, 0x2e", derived, StringComparison.Ordinal); // "e.event."
        Assert.DoesNotContain("0x2e, 0x40, 0x65", derived, StringComparison.Ordinal); // ".@e"
    }

    // tuple element names are not part of the type a formatter serves
    [Fact]
    public void MemberFormatterForATupleWithOtherElementNames_IsAccepted()
    {
        var (diagnostics, output, _) = Generate("""
            using MessagePack;
            using SerializerFoundation;
            public sealed partial class PairFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, (int, int)>
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, (int, int) value) { buffer.WriteNil(); }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref (int, int) value) { buffer.Skip(); }
            }
            [MessagePackObject] public class Holder { [Key(0), MessagePackFormatter(typeof(PairFormatter<,>))] public (int X, int Y) Pair { get; set; } }
            """);
        Assert.DoesNotContain(diagnostics, static d => d.Id == "MsgPack012");
        AssertCompiles(output);
    }

    // In the IDE a project reference is a compilation: a base member's [MessagePackFormatter] and a helper method of
    // another project are source symbols whose syntax trees belong to THAT compilation, and asking this one for their
    // semantic model throws (CS8785 for the generator, AD0001 for the analyzer). A command-line build sees metadata.
    [Fact]
    public async Task SymbolsOfAnotherProject_AreNotAskedForASemanticModel()
    {
        var shared = AnalyzerTestHost.CreateCompilation("""
            using System;
            using MessagePack;
            using SerializerFoundation;
            public sealed class StampFormatter<TW, TR> : IMessagePackFormatter<TW, TR, DateTime>
                where TW : struct, IWriteBuffer, allows ref struct
                where TR : struct, IReadBuffer, allows ref struct
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TW buffer, ref SerializeState state, DateTime value) { buffer.WriteNil(); }
                public void Deserialize(ref TR buffer, ref DeserializeState state, ref DateTime value) { buffer.Skip(); }
            }
            [MessagePackObject] public class Base { [Key(0), MessagePackFormatter(typeof(StampFormatter<,>))] public DateTime Created { get; set; } }
            public static class Helpers
            {
                public static void WriteAll<TW>(ref TW buffer, ref SerializeState state, int n) where TW : struct, IWriteBuffer { }
            }
            """, "SharedProject");
        Assert.Empty(shared.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));

        var app = AnalyzerTestHost.CreateCompilation("""
            using MessagePack;
            using SerializerFoundation;
            [MessagePackObject] public class Derived : Base { [Key(1)] public int X { get; set; } }
            public sealed class Probe<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int[]>
                where TWriteBuffer : struct, IWriteBuffer
                where TReadBuffer : struct, IReadBuffer
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int[] value) { Helpers.WriteAll(ref buffer, ref state, value.Length); }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int[] value) { buffer.Skip(); }
            }
            """, "AppProject").AddReferences(shared.ToMetadataReference());
        Assert.Empty(app.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));

        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: (CSharpParseOptions)app.SyntaxTrees.First().Options);
        var run = driver.RunGeneratorsAndUpdateCompilation(app, out var output, out var generatorDiagnostics).GetRunResult();
        Assert.DoesNotContain(generatorDiagnostics, static d => d.Id == "CS8785");
        Assert.Contains(run.GeneratedTrees, static t => t.FilePath.Contains("DerivedFormatter", StringComparison.Ordinal));
        AssertCompiles(output);

        var analyzerDiagnostics = await Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzerExtensions
            .WithAnalyzers(app, [new MessagePack.SourceGenerator.Analyzers.DepthTrackingAnalyzer()])
            .GetAllDiagnosticsAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(analyzerDiagnostics, static d => d.Id == "AD0001");
    }
}
