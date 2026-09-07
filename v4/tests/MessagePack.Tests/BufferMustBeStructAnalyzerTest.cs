using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using SerializerFoundation.Analyzers;

namespace MessagePack.Tests;

// SF001 over the corlib-stub world: a class implementing IWriteBuffer/IReadBuffer is a
// guaranteed dead end (every consuming API constrains buffers to `struct`), so the
// declaration itself is the error. Structs and derived buffer interfaces stay silent.
public class BufferMustBeStructAnalyzerTest
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
        """;

    const string CoreLibraryStub = """
        namespace SerializerFoundation
        {
            public interface IWriteBuffer : System.IDisposable { }
            public interface IReadBuffer : System.IDisposable { }
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

    static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(string userSource)
    {
        var corlib = CompileStub("corlibstub", CorlibStub);
        var coreLibrary = CompileStub("SerializerFoundation", CoreLibraryStub, corlib);

        var compilation = CSharpCompilation.Create(
            "UserAssembly",
            [CSharpSyntaxTree.ParseText(userSource)],
            [corlib, coreLibrary],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var withAnalyzers = compilation.WithAnalyzers([new BufferMustBeStructAnalyzer()]);
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ClassImplementation_Reports()
    {
        var diagnostics = await RunAnalyzerAsync("""
            public class ClassBuffer : SerializerFoundation.IWriteBuffer
            {
                public void Dispose() { }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(BufferMustBeStructAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("ClassBuffer", diagnostic.GetMessage());
        Assert.Contains("IWriteBuffer", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ClassImplementingReadBuffer_Reports()
    {
        var diagnostics = await RunAnalyzerAsync("""
            public class ClassReader : SerializerFoundation.IReadBuffer
            {
                public void Dispose() { }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("IReadBuffer", diagnostic.GetMessage());
    }

    [Fact]
    public async Task StructImplementation_Silent()
    {
        var diagnostics = await RunAnalyzerAsync("""
            public struct StructBuffer : SerializerFoundation.IWriteBuffer
            {
                public void Dispose() { }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DerivedBufferInterface_Silent()
    {
        // an interface extending a buffer interface is a legal abstraction: a struct can
        // still implement it, and the struct constraint bites where concrete types appear
        var diagnostics = await RunAnalyzerAsync("""
            public interface ISpecializedBuffer : SerializerFoundation.IWriteBuffer
            {
            }
            public struct Specialized : ISpecializedBuffer
            {
                public void Dispose() { }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ClassViaDerivedInterface_Reports()
    {
        var diagnostics = await RunAnalyzerAsync("""
            public interface ISpecializedBuffer : SerializerFoundation.IWriteBuffer
            {
            }
            public class SneakyBuffer : ISpecializedBuffer
            {
                public void Dispose() { }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("SneakyBuffer", diagnostic.GetMessage());
    }

    [Fact]
    public async Task SubclassOfDiagnosedClass_ReportsOnlyOnce()
    {
        // one error per hierarchy, at the type that introduced the interface
        var diagnostics = await RunAnalyzerAsync("""
            public class BaseBuffer : SerializerFoundation.IWriteBuffer
            {
                public void Dispose() { }
            }
            public class DerivedBuffer : BaseBuffer
            {
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("BaseBuffer", diagnostic.GetMessage());
    }

    [Fact]
    public async Task UnrelatedTypes_Silent()
    {
        var diagnostics = await RunAnalyzerAsync("""
            public class Plain : System.IDisposable
            {
                public void Dispose() { }
            }
            """);
        Assert.Empty(diagnostics);
    }
}
