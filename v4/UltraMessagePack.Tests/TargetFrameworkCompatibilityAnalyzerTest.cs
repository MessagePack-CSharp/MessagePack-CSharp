using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using UltraMessagePack.SourceGenerator.Analyzers;

namespace UltraMessagePack.Tests;

// The downlevel (netstandard-like) world is simulated with a hand-rolled corlib stub:
// real reference assemblies for old TFMs are not available at test runtime, and the
// analyzer's downlevel signal is purely "RuntimeFeature.ByRefLikeGenerics missing from
// corelib", which the stub controls with one const.
public class TargetFrameworkCompatibilityAnalyzerTest
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
                // manual accessors: auto-property backing fields would drag more
                // compiler-required members into the stub
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

    const string CoreLibraryStub = """
        {{MARKER}}
        namespace SerializerFoundation
        {
            [System.AttributeUsage(System.AttributeTargets.All)]
            internal sealed class ModernBufferSurfaceAttribute : System.Attribute { }

            public interface IWriteBuffer : System.IDisposable { }
            public interface IReadBuffer : System.IDisposable { }
        }
        namespace UltraMessagePack
        {
            public interface IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
                where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer
                where TReadBuffer : struct, SerializerFoundation.IReadBuffer
            {
            }
            public interface IMessagePackFormatterFactory
            {
                object CreateFormatter<TWriteBuffer, TReadBuffer>()
                    where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer
                    where TReadBuffer : struct, SerializerFoundation.IReadBuffer;
            }
        }
        """;

    const string FormatterSource = """
        using SerializerFoundation;

        public sealed class MyFormatter<TWriteBuffer, TReadBuffer>
            : UltraMessagePack.IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
            where TWriteBuffer : struct, IWriteBuffer
            where TReadBuffer : struct, IReadBuffer
        {
        }
        """;

    const string FactorySource = """
        using SerializerFoundation;

        public sealed class MyFactory : UltraMessagePack.IMessagePackFormatterFactory
        {
            public object CreateFormatter<TWriteBuffer, TReadBuffer>()
                where TWriteBuffer : struct, IWriteBuffer
                where TReadBuffer : struct, IReadBuffer
                => null;
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
        // the corlib stub has no reference carrying a runtime metadata version, so emit
        // needs one supplied explicitly
        var emit = compilation.Emit(stream, options: new Microsoft.CodeAnalysis.Emit.EmitOptions(runtimeMetadataVersion: "v4.0.30319"));
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(
        string userSource,
        bool modernRuntime,
        string? targetFramework = null,
        string? targetFrameworks = null,
        string? escapedTargetFrameworks = null,
        bool modernCore = false)
    {
        var corlibSource = CorlibStub.Replace(
            "{{BYREFLIKE}}",
            modernRuntime ? """public const string ByRefLikeGenerics = "ByRefLikeGenerics";""" : "");
        var corlib = CompileStub("corlibstub", corlibSource);
        var coreSource = CoreLibraryStub.Replace(
            "{{MARKER}}",
            modernCore ? "[assembly: SerializerFoundation.ModernBufferSurface]" : "");
        var coreLibrary = CompileStub("UltraMessagePack", coreSource, corlib);

        var compilation = CSharpCompilation.Create(
            "UserAssembly",
            [CSharpSyntaxTree.ParseText(userSource)],
            [corlib, coreLibrary],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var properties = new Dictionary<string, string>();
        if (targetFramework is not null)
        {
            properties["build_property.TargetFramework"] = targetFramework;
        }
        if (targetFrameworks is not null)
        {
            properties["build_property.TargetFrameworks"] = targetFrameworks;
        }
        if (escapedTargetFrameworks is not null)
        {
            properties["build_property.UltraMessagePackTargetFrameworks"] = escapedTargetFrameworks;
        }

        var withAnalyzers = compilation.WithAnalyzers(
            [new TargetFrameworkCompatibilityAnalyzer()],
            new AnalyzerOptions([], new TestOptionsProvider(properties)));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DownlevelFormatterWithoutNet10Target_Reports()
    {
        var diagnostics = await RunAnalyzerAsync(FormatterSource, modernRuntime: false, targetFramework: "netstandard2.0");
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(TargetFrameworkCompatibilityAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("MyFormatter", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DownlevelFactoryWithoutNet10Target_Reports()
    {
        var diagnostics = await RunAnalyzerAsync(FactorySource, modernRuntime: false, targetFramework: "netstandard2.0");
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(TargetFrameworkCompatibilityAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("CreateFormatter", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DownlevelWithNet10InTargetFrameworks_Silent()
    {
        var diagnostics = await RunAnalyzerAsync(
            FormatterSource, modernRuntime: false,
            targetFramework: "netstandard2.0", targetFrameworks: "netstandard2.0;netstandard2.1;net10.0");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DownlevelWithNet10InEscapedTargetFrameworks_Silent()
    {
        // the real MSBuild path: ';' is an editorconfig comment char, so the build
        // passes a comma-separated copy via UltraMessagePackTargetFrameworks
        var diagnostics = await RunAnalyzerAsync(
            FormatterSource, modernRuntime: false,
            targetFramework: "netstandard2.0", escapedTargetFrameworks: "netstandard2.0,netstandard2.1,net10.0");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DownlevelWithTruncatedRawTargetFrameworks_Reports()
    {
        // what the raw property looks like after editorconfig comment truncation when
        // net10.0 is not the FIRST entry: the analyzer must not be fooled into silence
        var diagnostics = await RunAnalyzerAsync(
            FormatterSource, modernRuntime: false,
            targetFramework: "netstandard2.0", targetFrameworks: "netstandard2.0");
        Assert.Single(diagnostics);
    }

    [Theory]
    [InlineData("netstandard2.0,net11.0")] // NuGet never selects a HIGHER TFM: net10.0 apps get the netstandard asset
    [InlineData("netstandard2.0,net10.0-windows")] // platform-specific does not cover platform-neutral consumers
    public async Task HigherOrPlatformSpecificTfmIsNotEnough_Reports(string escapedTargetFrameworks)
    {
        var diagnostics = await RunAnalyzerAsync(
            FormatterSource, modernRuntime: false,
            targetFramework: "netstandard2.0", escapedTargetFrameworks: escapedTargetFrameworks);
        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task DerivedBufferInterfaceConstraint_Reports()
    {
        const string source = """
            using SerializerFoundation;

            public interface ICustomWriteBuffer : IWriteBuffer { }

            public class CustomConsumer<TWriteBuffer>
                where TWriteBuffer : struct, ICustomWriteBuffer
            {
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source, modernRuntime: false, targetFramework: "netstandard2.0");
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("CustomConsumer", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Net472IsNotMistakenForModern_Reports()
    {
        var diagnostics = await RunAnalyzerAsync(
            FormatterSource, modernRuntime: false,
            targetFramework: "netstandard2.0", targetFrameworks: "netstandard2.0;net472");
        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task ModernCoreSurface_Silent()
    {
        // referencing the marker-carrying (net10.0) core build: nothing to enforce
        var diagnostics = await RunAnalyzerAsync(
            FormatterSource, modernRuntime: true, targetFramework: "net10.0", modernCore: true);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Net9ProjectAgainstDownlevelCore_Reports()
    {
        // THE closed blind spot: a net9.0 project's corelib HAS ByRefLikeGenerics (the
        // old capability probe stayed silent here), but it resolves the marker-less
        // netstandard2.1 core asset and compiles the hazardous no-flag shape
        var diagnostics = await RunAnalyzerAsync(
            FormatterSource, modernRuntime: true, targetFramework: "net9.0", modernCore: false);
        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task DownlevelTypeWithoutBufferConstraints_Silent()
    {
        var diagnostics = await RunAnalyzerAsync(
            "public class Plain<T> where T : struct { }",
            modernRuntime: false, targetFramework: "netstandard2.0");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MissingBuildProperties_StillReports()
    {
        // no CompilerVisibleProperty flowing at all: correctness over silence
        var diagnostics = await RunAnalyzerAsync(FormatterSource, modernRuntime: false);
        Assert.Single(diagnostics);
    }

    sealed class TestOptionsProvider(Dictionary<string, string> properties) : AnalyzerConfigOptionsProvider
    {
        readonly TestOptions options = new(properties);

        public override AnalyzerConfigOptions GlobalOptions => options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => options;

        sealed class TestOptions(Dictionary<string, string> properties) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value) => properties.TryGetValue(key, out value!);
        }
    }
}
