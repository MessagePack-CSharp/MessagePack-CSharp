using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using UltraMessagePack.SourceGenerator.Analyzers;

namespace UltraMessagePack.Tests;

// Corlib-stub approach (originally built for the retired SF001 tests), extended with the
// typeof machinery the base class's bridge body needs.
public class FactoryGenericOverrideAnalyzerTest
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
            public struct RuntimeTypeHandle { }
            public class Type
            {
                public static Type GetTypeFromHandle(RuntimeTypeHandle handle) { return null; }
            }
        }
        """;

    const string ModernCoreStub = """
        namespace SerializerFoundation
        {
            public interface IWriteBuffer : System.IDisposable { }
            public interface IReadBuffer : System.IDisposable { }
        }
        namespace UltraMessagePack
        {
            public abstract class MessagePackFormatterFactory
            {
                public virtual object CreateFormatter<TWriteBuffer, TReadBuffer>(System.Type type)
                    where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer
                    where TReadBuffer : struct, SerializerFoundation.IReadBuffer
                    => CreateFormatter(typeof(TWriteBuffer), typeof(TReadBuffer), type);

                public abstract object CreateFormatter(System.Type writeBufferType, System.Type readBufferType, System.Type valueType);
            }
        }
        """;

    const string DownlevelCoreStub = """
        namespace SerializerFoundation
        {
            public interface IWriteBuffer : System.IDisposable { }
            public interface IReadBuffer : System.IDisposable { }
        }
        namespace UltraMessagePack
        {
            public abstract class MessagePackFormatterFactory
            {
                public abstract object CreateFormatter(System.Type writeBufferType, System.Type readBufferType, System.Type valueType);
            }
        }
        """;

    const string TypeBasedOnlySource = """
        public sealed class OnlyTypeBasedFactory : UltraMessagePack.MessagePackFormatterFactory
        {
            public override object CreateFormatter(System.Type writeBufferType, System.Type readBufferType, System.Type valueType)
            {
                return null;
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

    static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(string userSource, bool modernCore)
    {
        var corlib = CompileStub("corlibstub", CorlibStub);
        var coreLibrary = CompileStub("UltraMessagePack", modernCore ? ModernCoreStub : DownlevelCoreStub, corlib);

        var compilation = CSharpCompilation.Create(
            "UserAssembly",
            [CSharpSyntaxTree.ParseText(userSource)],
            [corlib, coreLibrary],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var withAnalyzers = compilation.WithAnalyzers([new FactoryGenericOverrideAnalyzer()]);
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None);
    }

    [Fact]
    public async Task TypeBasedOnlyOnModernSurface_Reports()
    {
        // compiles (the default implementation satisfies the generic member) but rides
        // the bridge — exactly what UMP102 exists to surface
        var diagnostics = await RunAnalyzerAsync(TypeBasedOnlySource, modernCore: true);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(FactoryGenericOverrideAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("OnlyTypeBasedFactory", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ImplementsBoth_Silent()
    {
        const string source = """
            public sealed class FullFactory : UltraMessagePack.MessagePackFormatterFactory
            {
                public override object CreateFormatter<TWriteBuffer, TReadBuffer>(System.Type type)
                {
                    return null;
                }

                public override object CreateFormatter(System.Type writeBufferType, System.Type readBufferType, System.Type valueType)
                {
                    return null;
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source, modernCore: true);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DownlevelSurface_Silent()
    {
        // no generic member exists downlevel: the Type-based member IS the whole
        // contract, nothing to nudge
        var diagnostics = await RunAnalyzerAsync(TypeBasedOnlySource, modernCore: false);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task UnrelatedType_Silent()
    {
        var diagnostics = await RunAnalyzerAsync("public class Plain { }", modernCore: true);
        Assert.Empty(diagnostics);
    }
}
