using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace MessagePack.Tests;

// Drives the generator over the declaration mistakes a migrating user meets first and checks that each one is
// reported under its own id, with the branch-specific message, and that the refused type gets no formatter (an error
// diagnostic that still emitted a formatter would compile into a different build error). The reflection tier's
// run-time refusals of the same shapes are tested elsewhere; this is the compile-time side.
public class GeneratorDiagnosticFiringTests
{
    static GeneratorDriverRunResult Run(string source, out Compilation updated)
    {
        var compilation = AnalyzerTestHost.CreateCompilation("using MessagePack;" + Environment.NewLine + source, "DiagnosticProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out updated, out _, CancellationToken.None).GetRunResult();
    }

    static Diagnostic Single(GeneratorDriverRunResult result, string id, DiagnosticSeverity severity)
    {
        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == id);
        Assert.Equal(severity, diagnostic.Severity);
        return diagnostic;
    }

    static void NoFormatterFor(GeneratorDriverRunResult result, string typeName)
        => Assert.DoesNotContain(result.GeneratedTrees, t => t.FilePath.Contains(typeName + "Formatter", StringComparison.Ordinal));

    // ---- MsgPack002 ----

    [Fact]
    public void MixedKeys_IsMsgPack002()
    {
        var result = Run("""
            [MessagePackObject] public class Mixed { [Key(0)] public int A { get; set; } [Key("b")] public int B { get; set; } }
            """, out _);
        var diagnostic = Single(result, "MsgPack002", DiagnosticSeverity.Error);
        Assert.Contains("mixes int keys and string keys", diagnostic.GetMessage());
        NoFormatterFor(result, "Mixed");
    }

    // ---- MsgPack003 ----

    [Fact]
    public void DuplicateIntKey_IsMsgPack003()
    {
        var result = Run("""
            [MessagePackObject] public class Twice { [Key(0)] public int A { get; set; } [Key(0)] public int B { get; set; } }
            """, out _);
        var diagnostic = Single(result, "MsgPack003", DiagnosticSeverity.Error);
        Assert.Contains("key 0 more than once", diagnostic.GetMessage());
        NoFormatterFor(result, "Twice");
    }

    [Fact]
    public void DuplicateStringKey_IsMsgPack003()
    {
        var result = Run("""
            [MessagePackObject] public class Twice { [Key("same")] public int A { get; set; } [Key("same")] public int B { get; set; } }
            """, out _);
        var diagnostic = Single(result, "MsgPack003", DiagnosticSeverity.Error);
        Assert.Contains("key \"same\" more than once", diagnostic.GetMessage());
        NoFormatterFor(result, "Twice");
    }

    [Fact]
    public void DuplicateKeys_AreReportedPerMember_NotOncePerType()
    {
        // two independent duplicates: one diagnostic each, both at the offending member
        var result = Run("""
            [MessagePackObject] public class Twice
            {
                [Key(0)] public int A { get; set; }
                [Key(0)] public int B { get; set; }
                [Key(1)] public int C { get; set; }
                [Key(1)] public int D { get; set; }
            }
            """, out _);
        var duplicates = result.Diagnostics.Where(static d => d.Id == "MsgPack003").ToArray();
        Assert.Equal(2, duplicates.Length);
        Assert.All(duplicates, static d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
        Assert.Contains(duplicates, static d => d.GetMessage().Contains("key 0 more than once"));
        Assert.Contains(duplicates, static d => d.GetMessage().Contains("key 1 more than once"));
    }

    // ---- MsgPack004 ----

    [Fact]
    public void ClassWithoutMatchingConstructor_IsMsgPack004()
    {
        var result = Run("""
            [MessagePackObject] public class NoCtor { public NoCtor(int unrelated) { } [Key(0)] public int X { get; set; } }
            """, out _);
        var diagnostic = Single(result, "MsgPack004", DiagnosticSeverity.Error);
        Assert.Contains("needs an accessible parameterless constructor", diagnostic.GetMessage());
        Assert.Contains("v4 binds by name only", diagnostic.GetMessage());
        NoFormatterFor(result, "NoCtor");
    }

    [Fact]
    public void StructWithoutMatchingConstructor_IsNotMsgPack004()
    {
        // default(T) is always available for a struct
        var result = Run("""
            [MessagePackObject] public struct NoCtor { public NoCtor(int unrelated) { X = 0; } [Key(0)] public int X { get; set; } }
            """, out var updated);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Id == "MsgPack004");
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void ConstructorMatchingByName_IsNotMsgPack004()
    {
        var result = Run("""
            [MessagePackObject] public class ByName { public ByName(int x) { X = x; } [Key(0)] public int X { get; } }
            """, out var updated);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Id == "MsgPack004");
        Assert.DoesNotContain(result.Diagnostics, static d => d.Id == "MsgPack007"); // the parameter reads the member
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
    }

    // ---- MsgPack006 ----

    [Fact]
    public void PrivateNestedType_IsMsgPack006()
    {
        var result = Run("""
            public class Outer { [MessagePackObject] private class Inner { [Key(0)] public int X { get; set; } } }
            """, out var updated);
        var diagnostic = Single(result, "MsgPack006", DiagnosticSeverity.Error);
        Assert.Contains("is Private", diagnostic.GetMessage());
        Assert.Contains("only reach public or internal types", diagnostic.GetMessage());
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)); // no CS0122 from a generated file
    }

    [Fact]
    public void PublicTypeInsidePrivateContainingType_IsMsgPack006()
    {
        // the chain of containing types is what the generated factory has to name
        var result = Run("""
            public class Outer { private class Mid { [MessagePackObject] public class Inner { [Key(0)] public int X { get; set; } } } }
            """, out var updated);
        var diagnostic = Single(result, "MsgPack006", DiagnosticSeverity.Error);
        Assert.Contains("(or a containing type)", diagnostic.GetMessage());
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void PrivateUnionBase_IsMsgPack006()
    {
        // UnionParser's own accessibility check on the base, before the case types are examined (a case less
        // accessible than its public base is GeneratorRobustnessTests' CS0122 guard); the private case is the object
        // parser's report, so two diagnostics name two types
        var result = Run("""
            public class Outer
            {
                [MessagePackObject] [UnionTag(typeof(Leaf), 0)] private abstract class Root { }
                [MessagePackObject] private sealed class Leaf : Root { [Key(0)] public int X { get; set; } }
            }
            """, out var updated);
        var inaccessible = result.Diagnostics.Where(static d => d.Id == "MsgPack006").ToArray();
        Assert.Equal(2, inaccessible.Length);
        Assert.Contains(inaccessible, static d => d.GetMessage().StartsWith("'Outer.Root'") && d.GetMessage().Contains("is Private"));
        Assert.Contains(inaccessible, static d => d.GetMessage().StartsWith("'Outer.Leaf'") && d.GetMessage().Contains("is Private"));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void FileLocalUnionBase_IsMsgPack006()
    {
        var result = Run("""
            [MessagePackObject] [UnionTag(typeof(Leaf), 0)] file abstract class Root { }
            [MessagePackObject] file sealed class Leaf : Root { [Key(0)] public int X { get; set; } }
            """, out var updated);
        var fileLocal = result.Diagnostics.Where(static d => d.Id == "MsgPack006").ToArray();
        Assert.Equal(2, fileLocal.Length); // the union root (UnionParser) and the case (ObjectParser)
        Assert.Contains(fileLocal, static d => d.GetMessage().StartsWith("'Root'"));
        Assert.All(fileLocal, static d => Assert.Contains("file-local type", d.GetMessage()));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
    }

    // ---- MsgPack007 ----

    [Fact]
    public void GetterOnlyKeyedMember_WithoutConstructorParameter_IsMsgPack007()
    {
        var result = Run("""
            [MessagePackObject] public class ReadOnly { [Key(0)] public int X { get; } [Key(1)] public int Y { get; set; } }
            """, out var updated);
        var diagnostic = Single(result, "MsgPack007", DiagnosticSeverity.Warning);
        Assert.Contains("'ReadOnly.X'", diagnostic.GetMessage());
        Assert.Contains("payload is skipped on deserialization", diagnostic.GetMessage());
        // a warning: the formatter is still generated and compiles
        Assert.Contains(result.GeneratedTrees, static t => t.FilePath.Contains("ReadOnlyFormatter", StringComparison.Ordinal));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void GetterOnlyKeyedMember_FedByConstructorParameter_IsNotMsgPack007()
    {
        var result = Run("""
            [MessagePackObject] public class ReadOnly { public ReadOnly(int x, int y) { X = x; Y = y; } [Key(0)] public int X { get; } [Key(1)] public int Y { get; } }
            """, out _);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Id == "MsgPack007");
    }

    // ---- MsgPack008 ----

    [Fact]
    public void NegativeKey_IsMsgPack008()
    {
        var result = Run("""
            [MessagePackObject] public class Negative { [Key(-1)] public int X { get; set; } [Key(0)] public int Y { get; set; } }
            """, out _);
        var diagnostic = Single(result, "MsgPack008", DiagnosticSeverity.Error);
        Assert.Contains("'Negative.X' has a negative key (-1)", diagnostic.GetMessage());
        NoFormatterFor(result, "Negative");
    }

    // ---- MsgPack009 (the second branch; the two-constructors branch is in SerializationConstructorTests) ----

    [Fact]
    public void SerializationConstructor_WithUnmatchedParameter_IsMsgPack009()
    {
        var result = Run("""
            [MessagePackObject] public class Ctor
            {
                [SerializationConstructor] public Ctor(int x, string unrelated) { X = x; }
                public Ctor() { }
                [Key(0)] public int X { get; set; }
            }
            """, out _);
        var diagnostic = Single(result, "MsgPack009", DiagnosticSeverity.Error);
        Assert.Contains("every parameter must match a serialized member", diagnostic.GetMessage());
        NoFormatterFor(result, "Ctor");
    }

    [Fact]
    public void SerializationConstructor_WithByRefParameter_IsMsgPack009()
    {
        var result = Run("""
            [MessagePackObject] public class Ctor
            {
                [SerializationConstructor] public Ctor(ref int x) { X = x; }
                public Ctor() { }
                [Key(0)] public int X { get; set; }
            }
            """, out _);
        var diagnostic = Single(result, "MsgPack009", DiagnosticSeverity.Error);
        Assert.Contains("by value (no ref/out/in)", diagnostic.GetMessage());
        NoFormatterFor(result, "Ctor");
    }

    // ---- MsgPack010 ----

    [Fact]
    public void AllowPrivate_OnNonPartialType_IsMsgPack010()
    {
        var result = Run("""
            [MessagePackObject(AllowPrivate = true)] public class Sealed { [Key(0)] private int x; [IgnoreMember] public int X => x; }
            """, out var updated);
        var diagnostic = Single(result, "MsgPack010", DiagnosticSeverity.Error);
        Assert.Contains("must be declared partial", diagnostic.GetMessage());
        NoFormatterFor(result, "Sealed");
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)); // no CS0260 from a generated partial
    }

    [Fact]
    public void AllowPrivate_OnPartialTypeInsideNonPartialContainer_IsMsgPack010()
    {
        // the nested formatter is emitted through every containing declaration, so each one must be partial
        var result = Run("""
            public class Outer { [MessagePackObject(AllowPrivate = true)] public partial class Inner { [Key(0)] private int x; [IgnoreMember] public int X => x; } }
            """, out var updated);
        var diagnostic = Single(result, "MsgPack010", DiagnosticSeverity.Error);
        Assert.Contains("every containing type", diagnostic.GetMessage());
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void AllowPrivate_OnPartialChain_IsNotMsgPack010()
    {
        var result = Run("""
            public partial class Outer { [MessagePackObject(AllowPrivate = true)] public partial class Inner { [Key(0)] private int x; [IgnoreMember] public int X => x; } }
            """, out var updated);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Id == "MsgPack010");
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
    }
}
