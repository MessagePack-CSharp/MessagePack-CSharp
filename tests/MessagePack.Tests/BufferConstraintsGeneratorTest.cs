using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MessagePack.SourceGenerator;
using MessagePack.SourceGenerator.Generators;

namespace MessagePack.Tests;

// Same corlib-stub trick as the analyzer tests. One extra wrinkle: the test project pins
// Microsoft.CodeAnalysis 4.8 (the generator's floor), whose parser predates C# 13 — so
// the `allows ref struct` shape is asserted as TEXT only, while the downlevel shape is
// verified end-to-end (generator output added, whole compilation error-free). The
// allows-shape compiles for real in every net10.0 project of this solution.
public class BufferConstraintsGeneratorTest
{
    const string CorlibStub = """
        namespace System
        {
            public class Object { }
            public abstract class ValueType { }
            public abstract class Enum : ValueType { }
            public struct Void { }
            public struct Boolean { }
            public struct Int32 { }
            public class String { }
            public class Attribute { }
            public enum AttributeTargets { All = 32767 }
            public sealed class AttributeUsageAttribute : Attribute
            {
                public AttributeUsageAttribute(AttributeTargets validOn) { }
                public bool AllowMultiple { get { return false; } set { } }
                public bool Inherited { get { return false; } set { } }
            }
            public interface IDisposable { void Dispose(); }
        }
        namespace System.Runtime.CompilerServices
        {
            public static class RuntimeFeature
            {
                {{BYREFLIKE}}
            }
        }
        """;

    // unlike the analyzer stub this one carries the interface MEMBERS, so the tests prove
    // a real formatter implementation compiles once the generated constraints attach
    const string CoreLibraryStub = """
        namespace SerializerFoundation
        {
            public interface IWriteBuffer : System.IDisposable { }
            public interface IReadBuffer : System.IDisposable { }
        }
        namespace MessagePack
        {
            public struct SerializeState { }
            public struct DeserializeState { public int Depth; }
            public class MessagePackFormatterResolver { }
            public interface IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
                where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer
                where TReadBuffer : struct, SerializerFoundation.IReadBuffer
            {
                void Initialize(MessagePackFormatterResolver resolver);
                void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T value);
                void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value);
            }
        }
        """;

    const string PartialFormatterSource = """
        using SerializerFoundation;
        using MessagePack;

        namespace Sample
        {
            public class Foo { }

            public sealed partial class FooFormatter<TWriteBuffer, TReadBuffer>
                : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Foo>
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Foo value) { }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Foo value) { value = null; }
            }
        }
        """;

    static MetadataReference CompileStub(string assemblyName, string source, params MetadataReference[] references)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream, options: new Microsoft.CodeAnalysis.Emit.EmitOptions(runtimeMetadataVersion: "v4.0.30319"));
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    static (Compilation UpdatedCompilation, ImmutableArray<GeneratedSourceResult> Generated) RunGenerator(
        string userSource, bool modernRuntime, LanguageVersion languageVersion)
    {
        var corlibSource = CorlibStub.Replace(
            "{{BYREFLIKE}}",
            modernRuntime ? """public const string ByRefLikeGenerics = "ByRefLikeGenerics";""" : "");
        var corlib = CompileStub("corlibstub", corlibSource);
        var coreLibrary = CompileStub("MessagePack", CoreLibraryStub, corlib);

        var parseOptions = new CSharpParseOptions(languageVersion);
        var compilation = CSharpCompilation.Create(
            "UserAssembly",
            [CSharpSyntaxTree.ParseText(userSource, parseOptions)],
            [corlib, coreLibrary],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(
            [new BufferConstraintsGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _);
        var runResult = driver.GetRunResult();
        return (updated, runResult.Results[0].GeneratedSources);
    }

    [Fact]
    public void Downlevel_EmitsConstraintsWithoutAllowsRefStruct_AndCompiles()
    {
        var (updated, generated) = RunGenerator(PartialFormatterSource, modernRuntime: false, LanguageVersion.Latest);

        var source = Assert.Single(generated);
        var text = source.SourceText.ToString();
        Assert.Contains("partial class FooFormatter<TWriteBuffer, TReadBuffer>", text);
        Assert.Contains("where TWriteBuffer : struct, global::SerializerFoundation.IWriteBuffer", text);
        Assert.Contains("where TReadBuffer : struct, global::SerializerFoundation.IReadBuffer", text);
        Assert.DoesNotContain("allows ref struct", text);

        // the whole point: with the generated partial attached, the unconstrained
        // user declaration satisfies the interface and the compilation is clean
        Assert.Empty(updated.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void ModernRuntime_EmitsAllowsRefStruct()
    {
        // Preview is the only LanguageVersion >= C# 13 expressible on Roslyn 4.8
        var (_, generated) = RunGenerator(PartialFormatterSource, modernRuntime: true, LanguageVersion.Preview);

        var source = Assert.Single(generated);
        var text = source.SourceText.ToString();
        Assert.Contains("where TWriteBuffer : struct, global::SerializerFoundation.IWriteBuffer, allows ref struct", text);
        Assert.Contains("where TReadBuffer : struct, global::SerializerFoundation.IReadBuffer, allows ref struct", text);
    }

    [Fact]
    public void ModernRuntimeButOldLanguageVersion_OmitsAllowsRefStruct()
    {
        var (_, generated) = RunGenerator(PartialFormatterSource, modernRuntime: true, LanguageVersion.CSharp12);
        var source = Assert.Single(generated);
        Assert.DoesNotContain("allows ref struct", source.SourceText.ToString());
    }

    [Fact]
    public void ManuallyConstrainedType_IsSkipped()
    {
        const string source = """
            using SerializerFoundation;
            using MessagePack;

            public class Foo { }

            public sealed partial class ManualFormatter<TWriteBuffer, TReadBuffer>
                : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Foo>
                where TWriteBuffer : struct, IWriteBuffer
                where TReadBuffer : struct, IReadBuffer
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Foo value) { }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Foo value) { value = null; }
            }
            """;
        var (updated, generated) = RunGenerator(source, modernRuntime: false, LanguageVersion.Latest);
        Assert.Empty(generated);
        Assert.Empty(updated.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void ConcreteBufferTypeArguments_NoOutput()
    {
        const string source = """
            using SerializerFoundation;
            using MessagePack;

            public class Foo { }
            public struct DummyWriteBuffer : IWriteBuffer { public void Dispose() { } }
            public struct DummyReadBuffer : IReadBuffer { public void Dispose() { } }

            public sealed partial class ClosedFormatter
                : IMessagePackFormatter<DummyWriteBuffer, DummyReadBuffer, Foo>
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref DummyWriteBuffer buffer, ref SerializeState state, Foo value) { }
                public void Deserialize(ref DummyReadBuffer buffer, ref DeserializeState state, ref Foo value) { value = null; }
            }
            """;
        var (updated, generated) = RunGenerator(source, modernRuntime: false, LanguageVersion.Latest);
        Assert.Empty(generated);
        Assert.Empty(updated.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void UnrelatedPartialType_NoOutput()
    {
        const string source = """
            public partial class Plain<T> : System.IDisposable
            {
                public void Dispose() { }
            }
            """;
        var (_, generated) = RunGenerator(source, modernRuntime: false, LanguageVersion.Latest);
        Assert.Empty(generated);
    }

    [Fact]
    public void NestedFormatterInPartialContainer_EmitsWrappedPartial_AndCompiles()
    {
        const string source = """
            using SerializerFoundation;
            using MessagePack;

            namespace Sample
            {
                public class Foo { }

                public partial class Container
                {
                    internal sealed partial class NestedFormatter<TWriteBuffer, TReadBuffer>
                        : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Foo>
                    {
                        public void Initialize(MessagePackFormatterResolver resolver) { }
                        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Foo value) { }
                        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Foo value) { value = null; }
                    }
                }
            }
            """;
        var (updated, generated) = RunGenerator(source, modernRuntime: false, LanguageVersion.Latest);
        var text = Assert.Single(generated).SourceText.ToString();
        Assert.Contains("partial class Container", text);
        Assert.Contains("partial class NestedFormatter<TWriteBuffer, TReadBuffer>", text);
        Assert.Empty(updated.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    // ---- mid-edit resilience: half-written code must not strip the constraints and
    // ---- turn one real error into a page of constraint violations

    [Fact]
    public void BodyErrors_AndMissingMembers_StillGenerate()
    {
        const string source = """
            using SerializerFoundation;
            using MessagePack;

            namespace Sample
            {
                public class Foo { }

                public sealed partial class FooFormatter<TWriteBuffer, TReadBuffer>
                    : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Foo>
                {
                    public void Initialize(MessagePackFormatterResolver resolver) { }
                    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Foo value)
                    {
                        this_is_not_a_thing(value);
                    }
                    // Deserialize is not written yet
                }
            }
            """;
        var (_, generated) = RunGenerator(source, modernRuntime: false, LanguageVersion.Latest);
        var text = Assert.Single(generated).SourceText.ToString();
        Assert.Contains("where TWriteBuffer : struct, global::SerializerFoundation.IWriteBuffer", text);
        Assert.Contains("where TReadBuffer : struct, global::SerializerFoundation.IReadBuffer", text);
    }

    [Fact]
    public void UnresolvedSerializedType_StillGenerates()
    {
        const string source = """
            using SerializerFoundation;
            using MessagePack;

            public sealed partial class FreshFormatter<TWriteBuffer, TReadBuffer>
                : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NotYetWritten>
            {
            }
            """;
        var (_, generated) = RunGenerator(source, modernRuntime: false, LanguageVersion.Latest);
        var text = Assert.Single(generated).SourceText.ToString();
        Assert.Contains("partial class FreshFormatter<TWriteBuffer, TReadBuffer>", text);
        Assert.Contains("where TWriteBuffer : struct, global::SerializerFoundation.IWriteBuffer", text);
    }

    [Fact]
    public void TruncatedFile_StillGenerates()
    {
        // the file ends mid-method: unbalanced braces everywhere
        const string source = """
            using SerializerFoundation;
            using MessagePack;

            public class Foo { }

            public sealed partial class HalfFormatter<TWriteBuffer, TReadBuffer>
                : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Foo>
            {
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Foo value)
                {
                    buffer.
            """;
        var (_, generated) = RunGenerator(source, modernRuntime: false, LanguageVersion.Latest);
        var text = Assert.Single(generated).SourceText.ToString();
        Assert.Contains("partial class HalfFormatter<TWriteBuffer, TReadBuffer>", text);
    }

    [Fact]
    public void BrokenSiblingDeclarations_DoNotSuppressHealthyOutput()
    {
        // two half-typed nameless declarations (would collide on hint names) next to a
        // healthy formatter: the healthy one must keep its constraints
        const string source = """
            using SerializerFoundation;
            using MessagePack;

            public class Foo { }

            public sealed partial class <TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Foo> { }
            public sealed partial class <TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Foo> { }

            public sealed partial class FooFormatter<TWriteBuffer, TReadBuffer>
                : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Foo>
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Foo value) { }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Foo value) { value = null; }
            }
            """;
        var (_, generated) = RunGenerator(source, modernRuntime: false, LanguageVersion.Latest);
        Assert.Contains(generated, g => g.SourceText.ToString().Contains("partial class FooFormatter<TWriteBuffer, TReadBuffer>"));
    }

    [Fact]
    public void UnboundInterface_StillGeneratesFromSyntax()
    {
        // when the interface cannot bind semantically at all (broken/missing reference —
        // e.g. the referenced project is itself mid-edit), EVERY formatter would lose its
        // constraints at once without the syntactic fallback
        const string source = """
            public class Foo { }

            public sealed partial class OrphanFormatter<TWriteBuffer, TReadBuffer>
                : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Foo>
            {
            }
            """;
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create(
            "UserAssembly",
            [CSharpSyntaxTree.ParseText(source, parseOptions)], // note: NO references at all
            [],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var driver = CSharpGeneratorDriver.Create(
            [new BufferConstraintsGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var generated = driver.GetRunResult().Results[0].GeneratedSources;
        var text = Assert.Single(generated).SourceText.ToString();
        Assert.Contains("partial class OrphanFormatter<TWriteBuffer, TReadBuffer>", text);
        Assert.Contains("where TWriteBuffer : struct, global::SerializerFoundation.IWriteBuffer", text);
    }

    [Fact]
    public void HalfTypedManualConstraint_SkipsInsteadOfConflicting()
    {
        // the user began writing a manual where-clause: backing off avoids CS0265 noise
        const string source = """
            using SerializerFoundation;
            using MessagePack;

            public class Foo { }

            public sealed partial class ManualFormatter<TWriteBuffer, TReadBuffer>
                : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Foo>
                where TWriteBuffer :
            {
            }
            """;
        var (_, generated) = RunGenerator(source, modernRuntime: false, LanguageVersion.Latest);
        Assert.Empty(generated);
    }

    [Fact]
    public void MultiplePartialDeclarations_SingleOutput()
    {
        const string source = """
            using SerializerFoundation;
            using MessagePack;

            public class Foo { }

            public sealed partial class SplitFormatter<TWriteBuffer, TReadBuffer>
                : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Foo>
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
            }

            public sealed partial class SplitFormatter<TWriteBuffer, TReadBuffer>
            {
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Foo value) { }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Foo value) { value = null; }
            }
            """;
        var (updated, generated) = RunGenerator(source, modernRuntime: false, LanguageVersion.Latest);
        Assert.Single(generated);
        Assert.Empty(updated.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }
}
