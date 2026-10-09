using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace MessagePack.Tests;

// Generated formatter names flatten the full type name into one identifier, and that
// encoding must be INJECTIVE: if two types map to one name, the build dies loudly but
// cryptically (CS0101 in MessagePack.Generated, or a duplicate-hint generator failure).
// Sanitize encodes a literal underscore as "_0" (an identifier never starts with a digit) and every separator as a
// single '_', dropping the space of ", ", so no two type names share one identifier.
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
        Assert.Contains(result.GeneratedTrees, static t => t.FilePath.Contains("Ns_A_0BFormatter"));
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
        Assert.Contains(result.GeneratedTrees, static t => t.FilePath.Contains("Foo_T_0U_Formatter"));
    }

    [Fact]
    public void LeadingUnderscoreVersusTrailingUnderscore()
    {
        // Ns._A and Ns_.A: the old "double a literal underscore" encoding spelled both as Ns___A
        var result = RunGenerator("""
            using MessagePack;
            namespace Ns { [MessagePackObject] public class _A { [Key(0)] public int X { get; set; } } }
            namespace Ns_ { [MessagePackObject] public class A { [Key(0)] public int X { get; set; } } }
            """, out var updated);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains(result.GeneratedTrees, static t => t.FilePath.Contains("Ns__0AFormatter"));
        Assert.Contains(result.GeneratedTrees, static t => t.FilePath.Contains("Ns_0_AFormatter"));
    }

    // a self-expanding collection shape: C<int> : List<C<int[]>> : List<List<C<int[][]>>> ...; the harvest (and the
    // analyzer) must stop instead of opening closed types forever. The CLR refuses to load such a type, so this is
    // a generator-only concern and only the generator harness can exercise it.
    [Fact]
    public void RecursiveCollectionShape_TerminatesTheGenerator()
    {
        var result = RunGenerator("""
            using System.Collections.Generic;
            using MessagePack;
            public class RecursiveBag<T> : List<RecursiveBag<T[]>> { }
            [MessagePackSerializable<RecursiveBag<int>>] public partial class RecursiveBagFactory { }
            """, out var updated);
        Assert.Empty(result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
    }

    // a type-level attribute DERIVED from MessagePackFormatterAttribute is not what the registration pipeline matches:
    // the type says so (MsgPack014) instead of silently taking the generated object formatter
    [Fact]
    public void DerivedTypeLevelFormatterAttribute_IsReportedAsNotGenerated()
    {
        var result = RunGenerator("""
            using System;
            using MessagePack;
            public sealed class MyFormatterAttribute : MessagePackFormatterAttribute
            {
                public MyFormatterAttribute() : base(typeof(BuiltInFormatterFactory)) { }
            }
            [MessagePackObject]
            [MyFormatter]
            public class Annotated { [Key(0)] public int X { get; set; } }
            """, out var updated);
        Assert.True(result.Diagnostics.Any(static d => d.Id == "MsgPack014"), string.Join(", ", result.Diagnostics.Select(static d => d.Id + ": " + d.GetMessage())));

        // and on a union root, which the pipeline routes to UnionParser
        var unionResult = RunGenerator("""
            using System;
            using MessagePack;
            public sealed class MyFormatterAttribute : MessagePackFormatterAttribute
            {
                public MyFormatterAttribute() : base(typeof(BuiltInFormatterFactory)) { }
            }
            [MessagePackObject]
            [MyFormatter]
            [UnionTag(typeof(Yes), 0)]
            public abstract class Choice { }
            [MessagePackObject]
            public class Yes : Choice { [Key(0)] public int X { get; set; } }
            """, out _);
        Assert.True(unionResult.Diagnostics.Any(static d => d.Id == "MsgPack014"), string.Join(", ", unionResult.Diagnostics.Select(static d => d.Id + ": " + d.GetMessage())));
    }

    // a collection type with a `required` member cannot be `new()`-ed by the Add-based formatters: the harvest must not
    // register one (CS9040 in the generated code), and a member served by its own [MessagePackFormatter] is not
    // harvested at all
    [Fact]
    public void RequiredMemberCollection_IsNotRegisteredWithANewConstraint()
    {
        var result = RunGenerator("""
            using System.Collections.Generic;
            using MessagePack;
            public class LabeledList : List<int> { public required string Label { get; set; } }
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public LabeledList? Items { get; set; }
                [Key(1)] [MessagePackFormatter(typeof(BuiltInFormatterFactory))] public LabeledList? Custom { get; set; }
            }
            """, out var updated);
        Assert.Empty(result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
    }

    // an explicit interface implementation under AllowPrivate: not serialized (no member access the generated code could
    // compile), reported as MsgPack005 when it carries a [Key], and the rest of the type still generates
    [Fact]
    public void ExplicitInterfaceImplementation_IsReportedAndSkipped()
    {
        var result = RunGenerator("""
            using MessagePack;
            public interface IFoo { int Value { get; set; } }
            [MessagePackObject(AllowPrivate = true)]
            public partial class Explicit : IFoo
            {
                [Key(0)] int IFoo.Value { get; set; }
                [Key(1)] public int Other { get; set; }
            }
            """, out var updated);
        Assert.Contains(result.Diagnostics, static d => d.Id == "MsgPack005");
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
    }

    // a derived KeyAttribute whose key is not its constructor argument cannot be read at compile time: the type is
    // refused (MsgPack005) and left to the reflection tier, instead of being generated under a different key
    [Fact]
    public void DerivedKeyAttributeWithoutConstructorKey_IsRefused()
    {
        var result = RunGenerator("""
            using MessagePack;
            public sealed class FixedKeyAttribute : KeyAttribute { public FixedKeyAttribute() : base("fixed") { } }
            [MessagePackObject(true)]
            public class Fixed { [FixedKey] public int Value { get; set; } }
            """, out var updated);
        Assert.Contains(result.Diagnostics, static d => d.Id == "MsgPack005");
        Assert.DoesNotContain(result.GeneratedTrees, static t => t.FilePath.Contains("FixedFormatter"));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));

        // a constructor that transforms the key before handing it to the base is just as unreadable
        var transformed = RunGenerator("""
            using MessagePack;
            public sealed class PrefixKeyAttribute : KeyAttribute { public PrefixKeyAttribute(string key) : base("prefix_" + key) { } }
            [MessagePackObject(true)]
            public class Prefixed { [PrefixKey("x")] public int Value { get; set; } }
            """, out _);
        Assert.Contains(transformed.Diagnostics, static d => d.Id == "MsgPack005");
        Assert.DoesNotContain(transformed.GeneratedTrees, static t => t.FilePath.Contains("PrefixedFormatter"));

        // while a primary constructor forwarding the key as it is reads like [Key]
        var primary = RunGenerator("""
            using MessagePack;
            public sealed class PrimaryKeyAttribute(string key) : KeyAttribute(key);
            [MessagePackObject(true)]
            public class Primary { [PrimaryKey("wire")] public int Value { get; set; } }
            """, out var primaryUpdated);
        Assert.DoesNotContain(primary.Diagnostics, static d => d.Id == "MsgPack005");
        Assert.Contains(primary.GeneratedTrees, static t => t.FilePath.Contains("PrimaryFormatter"));
        Assert.Empty(primaryUpdated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
    }

    // a file-local type is visible in its own file only: refused with a diagnostic instead of generated code that
    // cannot name it (CS0400)
    [Fact]
    public void FileLocalType_IsRefused()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject]
            file class FileDto { [Key(0)] public int X { get; set; } }
            """, out var updated);
        Assert.Contains(result.Diagnostics, static d => d.GetMessage().Contains("file-local"));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
    }

    // an AllowPrivate holder whose private enum closes a generic the compilation does not generate (suppressed): the
    // private-scope factory must not construct a formatter that does not exist
    [Fact]
    public void PrivateClosureOfASuppressedGeneric_DoesNotReferenceAMissingFormatter()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject(SuppressSourceGeneration = true)]
            public class Box<T> { [Key(0)] public T? Value { get; set; } }
            [MessagePackObject(AllowPrivate = true)]
            public partial class Holder
            {
                private enum Mode { A, B }
                [Key(0)] private Box<Mode>? boxed;
                [Key(1)] private Mode mode;
            }
            """, out var updated);
        Assert.Empty(result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        var holder = Assert.Single(result.GeneratedTrees, static t => t.FilePath.EndsWith("HolderFormatter.g.cs")).ToString();
        Assert.DoesNotContain("BoxFormatter", holder);
        Assert.Contains("MessagePackPrivateTypesFactory", holder); // the enum itself still registers through the private factory
    }

    // [MessagePackObject] next to a type-level [MessagePackFormatter]: the attributed factory serves the type, so the
    // object shape rules (here: a constructor whose parameter matches no member, MsgPack004) do not apply
    [Fact]
    public void TypeLevelFormatter_SkipsTheObjectShapeRules()
    {
        var result = RunGenerator("""
            using System;
            using MessagePack;
            public sealed class PointFactory : MessagePackFormatterFactory
            {
                public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType) => null;
            }
            [MessagePackObject]
            [MessagePackFormatter(typeof(PointFactory))]
            public class Point
            {
                public Point(int unrelated) { }
                [Key(0)] public int X { get; }
            }
            """, out var updated);
        Assert.Empty(result.Diagnostics.Select(static d => d.ToString()));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.DoesNotContain(result.GeneratedTrees, static t => t.FilePath.EndsWith("PointFormatter.g.cs"));
        Assert.Contains(result.GeneratedTrees, static t => t.ToString().Contains("new global::PointFactory()"));
    }

    // a member attribute naming a factory nested privately in the DTO: the formatter generated outside the type cannot
    // construct it (CS0122), so it is refused with a diagnostic; AllowPrivate nests the formatter and can
    [Fact]
    public void MemberFormatterAttribute_RequiresAnAccessibleFactory()
    {
        var refused = RunGenerator("""
            using System;
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                private sealed class PrivateFactory : MessagePackFormatterFactory
                {
                    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType) => null;
                }
                [Key(0)] [MessagePackFormatter(typeof(PrivateFactory))] public int X { get; set; }
            }
            """, out var refusedUpdated);
        Assert.Contains(refused.Diagnostics, static d => d.Id == "MsgPack012" && d.GetMessage().Contains("not accessible"));
        Assert.Empty(refusedUpdated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));

        var nested = RunGenerator("""
            using System;
            using MessagePack;
            [MessagePackObject(AllowPrivate = true)]
            public partial class Holder
            {
                private sealed class PrivateFactory : MessagePackFormatterFactory
                {
                    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType) => null;
                }
                [Key(0)] [MessagePackFormatter(typeof(PrivateFactory))] public int X { get; set; }
            }
            """, out var nestedUpdated);
        Assert.DoesNotContain(nested.Diagnostics, static d => d.Id == "MsgPack012");
        Assert.Empty(nestedUpdated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
    }

    // a type parameter named like a type the generated formatter uses unqualified would shadow it: refused with a
    // diagnostic instead of generated code that does not compile
    [Fact]
    public void TypeParameterNamedLikeALibraryType_IsRefused()
    {
        var result = RunGenerator("""
            using MessagePack;
            [MessagePackObject] public class Model<MessagePackFormatterResolver> { [Key(0)] public int X { get; set; } }
            [MessagePackObject] public class Other<SerializeState> { [Key(0)] public int X { get; set; } }
            [MessagePackObject] public class Method<Initialize> { [Key(0)] public int X { get; set; } }
            [MessagePackObject] public class Slot<fItem> { [Key(0)] public string? Item { get; set; } }
            [MessagePackObject] public class Local<v_Item> { [Key(0)] public string? Item { get; set; } }
            [MessagePackObject] public class Fine<TResolver> { [Key(0)] public int X { get; set; } }
            [MessagePackObject] public class AlsoFine<fOther> { [Key(0)] public int Item { get; set; } }
            """, out var updated);
        Assert.Equal(5, result.Diagnostics.Count(static d => d.Id == "MsgPack005"));
        Assert.Contains(result.GeneratedTrees, static t => System.IO.Path.GetFileName(t.FilePath).StartsWith("AlsoFine") && t.FilePath.EndsWith("Formatter.g.cs"));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.Contains(result.GeneratedTrees, static t => System.IO.Path.GetFileName(t.FilePath).StartsWith("Fine") && t.FilePath.EndsWith("Formatter.g.cs"));
    }

    // an unread required member whose initializer is not a constant: reported (MsgPack023), and the generated code
    // still compiles with default
    [Fact]
    public void UnreadRequiredMember_NonConstantInitializer_IsReported()
    {
        var result = RunGenerator("""
            using System;
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public int X { get; set; }
                [IgnoreMember] public required string Label { get; set; } = Guid.NewGuid().ToString();
            }
            """, out var updated);
        Assert.Contains(result.Diagnostics, static d => d.Id == "MsgPack023");
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
    }

    // an expression-form argument naming an escaped identifier keeps the escape in the generated code
    [Fact]
    public void ExpressionArgument_KeepsIdentifierEscaping()
    {
        var result = RunGenerator("""
            using System;
            using System.Collections.Generic;
            using MessagePack;
            using MessagePack.Formatters;
            public static class Comparers { public static readonly StringComparer @default = StringComparer.OrdinalIgnoreCase; }
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] [MessagePackFormatter(typeof(DictionaryFormatterFactory<string, int>), "Comparers.@default")] public Dictionary<string, int>? Map { get; set; }
            }
            """, out var updated);
        Assert.Empty(result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.Contains(result.GeneratedTrees, static t => t.ToString().Contains("Comparers.@default"));
    }

    // a finite chain of distinct generic DTOs deeper than any fixed closure budget: every closed type on it is
    // harvested (Native AOT has no dynamic fallback for the last ones)
    [Fact]
    public void LongFiniteGenericChain_IsHarvestedToItsEnd()
    {
        var source = "using MessagePack;\n";
        for (var i = 1; i <= 12; i++)
        {
            source += i < 12
                ? $"[MessagePackObject] public class A{i}<T> {{ [Key(0)] public A{i + 1}<T>? Next {{ get; set; }} }}\n"
                : $"[MessagePackObject] public class A{i}<T> {{ [Key(0)] public T? Value {{ get; set; }} }}\n";
        }
        source += "[MessagePackSerializable<A1<int>>] public partial class ChainFactory { }\n";
        var result = RunGenerator(source, out var updated);
        Assert.Empty(result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.Contains(result.GeneratedTrees, static t => t.ToString().Contains("typeof(global::A12<int>)"));
    }

    // a finite member type nested deeper than any absolute budget (a 40-layer List<...<T>>, past any budget): harvested in
    // full, only an expanding definition is ever cut
    [Fact]
    public void DeeplyNestedFiniteMemberType_IsHarvested()
    {
        var nested = string.Concat(Enumerable.Repeat("List<", 40)) + "T" + new string('>', 40);
        var result = RunGenerator($$"""
            using System.Collections.Generic;
            using MessagePack;
            [MessagePackObject] public class Deep<T> { [Key(0)] public {{nested}}? Value { get; set; } }
            [MessagePackSerializable<Deep<int>>] public partial class DeepFactory { }
            """, out var updated);
        Assert.Empty(result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        var closed = string.Concat(Enumerable.Repeat("global::System.Collections.Generic.List<", 40)) + "int" + new string('>', 40);
        Assert.Contains(result.GeneratedTrees, t => t.ToString().Contains($"typeof({closed})"));
    }

    // an interface the catch-all never closes (an empty marker ITag<D<List<T>>>) must not make a definition look
    // expanding: the finite graph through it is harvested in full, even past the growth budget
    [Fact]
    public void MarkerInterface_DoesNotMakeADefinitionExpanding()
    {
        var deep = string.Concat(Enumerable.Repeat("List<", 40)) + "T" + new string('>', 40); // 40 levels past the root D<int>, beyond the growth budget
        var result = RunGenerator($$"""
            using System.Collections.Generic;
            using MessagePack;
            public interface ITag<T> { }
            public class E<T> : List<int>, ITag<D<List<T>>> { }
            [MessagePackObject] public class D<T> { [Key(0)] public E<{{deep}}>? Inner { get; set; } }
            [MessagePackObject] public class Root { [Key(0)] public D<int>? Value { get; set; } }
            """, out var updated);
        Assert.Empty(result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        var closedE = "global::E<" + string.Concat(Enumerable.Repeat("global::System.Collections.Generic.List<", 40)) + "int" + new string('>', 40) + ">";
        Assert.Contains(result.GeneratedTrees, t => t.ToString().Contains($"typeof({closedE})"));
    }

    // a formatter implementing IMessagePackFormatter for two types serves a member of either type
    [Fact]
    public void MultiTypeFormatter_MatchesTheMemberType()
    {
        var result = RunGenerator("""
            using MessagePack;
            using SerializerFoundation;
            public sealed class Both<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>, IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?>
                where TWriteBuffer : struct, IWriteBuffer, allows ref struct
                where TReadBuffer : struct, IReadBuffer, allows ref struct
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value) => buffer.WriteInt32(value);
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value) => value = buffer.ReadInt32();
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, string? value) => buffer.WriteString(value);
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref string? value) => value = buffer.ReadString();
            }
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] [MessagePackFormatter(typeof(Both<,>))] public int A { get; set; }
                [Key(1)] [MessagePackFormatter(typeof(Both<,>))] public string? B { get; set; }
            }
            """, out var updated);
        Assert.DoesNotContain(result.Diagnostics, static d => d.Id == "MsgPack012");
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
    }

    // a nested type named like a base member hides it (CS0108): the generated access goes through the base type
    [Fact]
    public void NestedTypeHidingABaseMember_IsAccessedThroughTheBase()
    {
        var result = RunGenerator("""
            using MessagePack;
            public class Base { public int Item { get; set; } }
            [MessagePackObject(true)]
            public class Derived : Base
            {
                public new class Item { }
                public int Other { get; set; }
            }
            """, out var updated);
        Assert.Empty(result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        var formatter = Assert.Single(result.GeneratedTrees, static t => t.FilePath.EndsWith("DerivedFormatter.g.cs")).ToString();
        Assert.Contains("((global::Base)value).Item", formatter);
    }

    // a formatter type whose buffer parameters do not allow ref struct buffers cannot be closed by the generated
    // formatter on a net9+ target (CS9244): refused with guidance toward the factory form
    [Fact]
    public void DownlevelFormatterType_IsRefusedWithGuidance()
    {
        var result = RunGenerator("""
            using MessagePack;
            using SerializerFoundation;
            public sealed class Downlevel<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
                where TWriteBuffer : struct, IWriteBuffer
                where TReadBuffer : struct, IReadBuffer
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value) => buffer.WriteInt32(value);
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value) => value = buffer.ReadInt32();
            }
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] [MessagePackFormatter(typeof(Downlevel<,>))] public int A { get; set; }
            }
            """, out var updated);
        Assert.Contains(result.Diagnostics, static d => d.Id == "MsgPack012" && d.GetMessage().Contains("allows ref struct"));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
    }

    // an expression-form argument naming a private static member of the DTO: the attribute site can name it, the
    // generated formatter outside the type cannot (CS0122), so it is refused; AllowPrivate nests the formatter and can
    [Fact]
    public void ExpressionArgument_MustBeAccessibleToTheGeneratedFormatter()
    {
        var refused = RunGenerator("""
            using System;
            using System.Collections.Generic;
            using MessagePack;
            using MessagePack.Formatters;
            [MessagePackObject]
            public class Dto
            {
                private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
                [Key(0)] [MessagePackFormatter(typeof(DictionaryFormatterFactory<string, int>), "Dto.Comparer")] public Dictionary<string, int>? Map { get; set; }
            }
            """, out var refusedUpdated);
        Assert.Contains(refused.Diagnostics, static d => d.Id == "MsgPack013" && d.GetMessage().Contains("not accessible"));
        Assert.Empty(refusedUpdated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));

        var nested = RunGenerator("""
            using System;
            using System.Collections.Generic;
            using MessagePack;
            using MessagePack.Formatters;
            [MessagePackObject(AllowPrivate = true)]
            public partial class Dto
            {
                private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
                [Key(0)] [MessagePackFormatter(typeof(DictionaryFormatterFactory<string, int>), "Dto.Comparer")] public Dictionary<string, int>? Map { get; set; }
            }
            """, out var nestedUpdated);
        Assert.DoesNotContain(nested.Diagnostics, static d => d.Id == "MsgPack013");
        Assert.Empty(nestedUpdated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
    }

    // attribute arguments the parameter takes by an ordinary implicit conversion bind as themselves: a string into
    // an object parameter is that string (not an expression), an array into IEnumerable<int> keeps its element type
    [Fact]
    public void FormatterArguments_BindByImplicitConversion()
    {
        var result = RunGenerator("""
            using System;
            using System.Collections.Generic;
            using MessagePack;
            public sealed class TagFactory : MessagePackFormatterFactory
            {
                public TagFactory(object option) { }
                public TagFactory(IEnumerable<int> options, bool flag) { }
                public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType) => null;
            }
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] [MessagePackFormatter(typeof(TagFactory), "tag")] public int A { get; set; }
                [Key(1)] [MessagePackFormatter(typeof(TagFactory), new int[] { 1, 2 }, true)] public int B { get; set; }
            }
            """, out var updated);
        Assert.Empty(result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        var formatter = Assert.Single(result.GeneratedTrees, static t => t.FilePath.EndsWith("HolderFormatter.g.cs")).ToString();
        Assert.Contains("(object)(\"tag\")", formatter);
        Assert.Contains("new int[] {", formatter);
    }

    // a root factory named like a model's generated formatter: the two generated files must not share a hint name
    [Fact]
    public void RootFactoryNamedLikeAFormatter_DoesNotCollide()
    {
        var result = RunGenerator("""
            using MessagePack;
            namespace Demo
            {
                [MessagePackObject] public class Person { [Key(0)] public int Id { get; set; } }
                [MessagePackSerializable(typeof(Person[]))] public partial class PersonFormatter { }
            }
            """, out var updated);
        Assert.Empty(result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Select(static d => d.ToString()));
        Assert.Contains(result.GeneratedTrees, static t => t.FilePath.EndsWith("Demo_PersonFormatter.g.cs"));
        Assert.Contains(result.GeneratedTrees, static t => t.FilePath.EndsWith("Demo_PersonFormatter.SerializableFactory.g.cs"));
    }

    // Outer<T>.Choice is generic through its container only: refused with MsgPack005 instead of crashing the generator
    [Fact]
    public void UnionNestedInGenericType_IsRefusedWithADiagnostic()
    {
        var result = RunGenerator("""
            using MessagePack;
            public class Outer<T>
            {
                [MessagePackObject]
                [UnionTag(typeof(Yes), 0)]
                public abstract class Choice { }

                [MessagePackObject]
                public class Yes : Choice { [Key(0)] public int X { get; set; } }
            }
            """, out var updated);
        Assert.True(result.Diagnostics.Any(static d => d.Id == "MsgPack005"), string.Join(", ", result.Diagnostics.Select(static d => d.Id + ": " + d.GetMessage())));
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error && d.Id.StartsWith("CS8785")));
    }
}
