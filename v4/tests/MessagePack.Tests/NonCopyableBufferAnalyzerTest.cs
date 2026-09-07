using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using SerializerFoundation.Analyzers;

namespace MessagePack.Tests;

// Copy-rule coverage over the corlib-stub world: any struct implementing a buffer
// interface is single-owner; moves (fresh values) are allowed, copies are flagged.
public class NonCopyableBufferAnalyzerTest
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
            public struct IntPtr { }
            public abstract class Delegate { }
            public abstract class MulticastDelegate : Delegate { }
            public abstract class Array
            {
                public int Length { get { return 0; } }
            }
        }
        namespace System.Runtime.InteropServices
        {
            public sealed class InAttribute : Attribute { }
        }
        namespace System.Runtime.CompilerServices
        {
            public sealed class IsReadOnlyAttribute : Attribute { }
            public sealed class RequiresLocationAttribute : Attribute { }
        }
        namespace System.Collections
        {
            public interface IEnumerator
            {
                object Current { get; }
                bool MoveNext();
            }
            public interface IEnumerable
            {
                IEnumerator GetEnumerator();
            }
        }
        namespace System
        {
            public struct Nullable<T> where T : struct
            {
                T value;
                bool hasValue;
                public Nullable(T value) { this.value = value; hasValue = true; }
                public bool HasValue { get { return hasValue; } }
                public T Value { get { return value; } }
            }
            public struct ValueTuple<T1, T2>
            {
                public T1 Item1;
                public T2 Item2;
                public ValueTuple(T1 item1, T2 item2) { Item1 = item1; Item2 = item2; }
            }
        }
        """;

    const string CoreLibraryStub = """
        namespace SerializerFoundation
        {
            public interface IWriteBuffer : System.IDisposable { }
            public interface IReadBuffer : System.IDisposable { }
        }
        """;

    const string BufferDefinition = """
        public struct MyBuffer : SerializerFoundation.IWriteBuffer
        {
            public int written;
            public void Dispose() { }
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
        var coreLibrary = CompileStub("MessagePack", CoreLibraryStub, corlib);

        var compilation = CSharpCompilation.Create(
            "UserAssembly",
            [CSharpSyntaxTree.ParseText(BufferDefinition), CSharpSyntaxTree.ParseText(userSource)],
            [corlib, coreLibrary],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var withAnalyzers = compilation.WithAnalyzers([new NonCopyableBufferAnalyzer()]);
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CopyAssignment_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Run()
                {
                    var a = new MyBuffer();
                    var b = a;
                    b.Dispose();
                    a.Dispose();
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(NonCopyableBufferAnalyzer.DiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task FreshInitializationAndRefUse_Silent()
    {
        const string source = """
            public static class Code
            {
                public static void Run()
                {
                    var a = new MyBuffer();
                    Use(ref a);
                    MyBuffer b = default;
                    Use(ref b);
                    var c = Make();
                    Use(ref c);
                }

                static void Use(ref MyBuffer buffer) { }

                static MyBuffer Make() { return new MyBuffer(); }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ByValueParameterDeclaration_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Consume(MyBuffer buffer) { }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("received by value", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ByValueArgumentOfExistingValue_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Run()
                {
                    var a = new MyBuffer();
                    External.Take(a);
                }
            }
            """;
        // the callee lives in a separate compilation so only the ARGUMENT rule fires
        var corlib = CompileStub("corlibstub", CorlibStub);
        var coreLibrary = CompileStub("MessagePack", CoreLibraryStub, corlib);
        var callee = CompileStub("Callee", BufferDefinition + """
            public static class External
            {
                public static void Take(MyBuffer buffer) { }
            }
            """, corlib, coreLibrary);
        var compilation = CSharpCompilation.Create(
            "UserAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            [corlib, coreLibrary, callee],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var diagnostics = await compilation.WithAnalyzers([new NonCopyableBufferAnalyzer()])
            .GetAnalyzerDiagnosticsAsync(CancellationToken.None);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("passed by value", diagnostic.GetMessage());
    }

    [Fact]
    public async Task InParameterDeclaration_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Consume(in MyBuffer buffer) { }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("readonly reference", diagnostic.GetMessage());
    }

    [Fact]
    public async Task RefReadonlyParameterDeclaration_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Consume(ref readonly MyBuffer buffer) { }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("readonly reference", diagnostic.GetMessage());
    }

    [Fact]
    public async Task RefAndOutParameters_Silent()
    {
        const string source = """
            public static class Code
            {
                public static void Mutate(ref MyBuffer buffer) { }
                public static void Create(out MyBuffer buffer) { buffer = new MyBuffer(); }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task InArgumentToExternalCallee_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Run()
                {
                    var a = new MyBuffer();
                    External.Take(in a);
                    External.Take(a);
                }
            }
            """;
        // the callee lives in a separate compilation so only the ARGUMENT rule fires;
        // both the explicit `in a` and the implicit readonly-reference binding are copies in disguise
        var corlib = CompileStub("corlibstub", CorlibStub);
        var coreLibrary = CompileStub("MessagePack", CoreLibraryStub, corlib);
        var callee = CompileStub("Callee", BufferDefinition + """
            public static class External
            {
                public static void Take(in MyBuffer buffer) { }
            }
            """, corlib, coreLibrary);
        var compilation = CSharpCompilation.Create(
            "UserAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            [corlib, coreLibrary, callee],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var diagnostics = await compilation.WithAnalyzers([new NonCopyableBufferAnalyzer()])
            .GetAnalyzerDiagnosticsAsync(CancellationToken.None);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Contains("readonly reference", d.GetMessage()));
    }

    [Fact]
    public async Task RefLocalAndRefReassignment_Silent()
    {
        const string source = """
            public static class Code
            {
                public static void Run(ref MyBuffer a, ref MyBuffer b)
                {
                    ref MyBuffer r = ref a;
                    r.Dispose();
                    r = ref b;
                    r.Dispose();
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ConditionalOfFreshValues_Silent()
    {
        const string source = """
            public static class Code
            {
                public static void Run(bool flag)
                {
                    var a = flag ? new MyBuffer() : default(MyBuffer);
                    var b = flag switch { true => new MyBuffer(), false => default(MyBuffer) };
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReadOnlyFieldReceiver_Reports()
    {
        const string source = """
            public class Holder
            {
                readonly MyBuffer buffer;
                public void Run() { buffer.Dispose(); }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("readonly reference", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ReadOnlyStructFieldReceiver_Reports()
    {
        const string source = """
            public readonly struct Holder
            {
                readonly MyBuffer buffer;
                public void Run() { buffer.Dispose(); }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("readonly reference", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ReadOnlyMemberReceiver_Reports()
    {
        const string source = """
            public struct Holder
            {
                MyBuffer buffer;
                public readonly void Run() { buffer.Dispose(); }
                public void Mutable() { buffer.Dispose(); }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("readonly reference", diagnostic.GetMessage());
    }

    [Fact]
    public async Task RefReadonlyLocalAndReturnReceiver_Reports()
    {
        const string source = """
            public class Holder
            {
                MyBuffer buffer;
                public ref readonly MyBuffer Peek() { return ref buffer; }
                public void Run()
                {
                    ref readonly MyBuffer r = ref buffer;
                    r.Dispose();
                    Peek().Dispose();
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Contains("readonly reference", d.GetMessage()));
    }

    [Fact]
    public async Task InParameterPathReceiver_Reports()
    {
        const string source = """
            public struct Holder
            {
                public MyBuffer buffer;
            }
            public static class Code
            {
                public static void Run(in Holder holder) { holder.buffer.Dispose(); }
            }
            """;
        // Holder is not a buffer, so its `in` parameter is legal; the buffer inside is still readonly through it
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("readonly reference", diagnostic.GetMessage());
    }

    [Fact]
    public async Task MutableFieldAndRefReceivers_Silent()
    {
        const string source = """
            public class Holder
            {
                MyBuffer buffer;
                public ref MyBuffer Get() { return ref buffer; }
                public void Run(ref MyBuffer parameter)
                {
                    buffer.Dispose();
                    Get().Dispose();
                    parameter.Dispose();
                    var fresh = new MyBuffer();
                    fresh.Dispose();
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Boxing_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Run()
                {
                    var a = new MyBuffer();
                    SerializerFoundation.IWriteBuffer boxed = a;
                    boxed.Dispose();
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("boxed"));
    }

    [Fact]
    public async Task GenericBufferParameterByValue_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Consume<TWriteBuffer>(TWriteBuffer buffer)
                    where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer
                {
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("received by value", diagnostic.GetMessage());
    }

    [Fact]
    public async Task NonBufferStruct_Silent()
    {
        const string source = """
            public struct Plain { public int value; }
            public static class Code
            {
                public static void Run()
                {
                    var a = new Plain();
                    var b = a;
                    Consume(b);
                }
                static void Consume(Plain plain) { }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }

    // ---- v2 rules ----

    [Fact]
    public async Task LambdaByValueParameter_Reports()
    {
        // the generic delegate's DECLARED Invoke parameter is T (not a buffer), so only
        // the lambda's own parameter fires — isolating the AnonymousFunction rule
        const string source = """
            public delegate void Generic<T>(T value);
            public static class Code
            {
                public static void Run()
                {
                    Generic<MyBuffer> handler = (MyBuffer x) => { };
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("received by value", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DelegateDeclarationByValueParameter_Reports()
    {
        const string source = """
            public delegate void Handler(MyBuffer buffer);
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("received by value", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ReturnOfExistingStorage_Reports()
    {
        const string source = """
            public class Holder
            {
                MyBuffer field;
                public MyBuffer Take() { return field; }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("returned by value", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ReturnOfLocalFreshOrByRef_Silent()
    {
        // returning a local or a fresh value is the factory move; a ref-returning
        // member hands out the storage itself
        const string source = """
            public class Holder
            {
                MyBuffer field;
                public MyBuffer MakeDirect() { return new MyBuffer(); }
                public MyBuffer MakeViaLocal() { var local = new MyBuffer(); return local; }
                public ref MyBuffer Storage() { return ref field; }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ByValueProperty_Reports()
    {
        const string source = """
            public class Holder
            {
                MyBuffer store; // not named "field": that is the C# 14 backing-field keyword inside accessors
                public MyBuffer Buffer { get { return store; } }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        // the property declaration AND its getter's return-of-field both fire
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("by-value property"));
    }

    [Fact]
    public async Task RefReturningProperty_Silent()
    {
        const string source = """
            public class Holder
            {
                MyBuffer store; // not named "field": that is the C# 14 backing-field keyword inside accessors
                public ref MyBuffer Buffer { get { return ref store; } }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ForeachCopy_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Run(MyBuffer[] buffers)
                {
                    foreach (var buffer in buffers)
                    {
                    }
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("copied by foreach", diagnostic.GetMessage());
    }

    [Fact]
    public async Task PatternMatch_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Check(object candidate)
                {
                    if (candidate is MyBuffer buffer)
                    {
                    }
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("pattern match", diagnostic.GetMessage());
    }

    [Fact]
    public async Task WithExpression_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Run()
                {
                    var a = new MyBuffer();
                    var b = a with { written = 1 };
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("with-expression"));
    }

    [Fact]
    public async Task TupleCapture_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Run()
                {
                    var a = new MyBuffer();
                    var pair = (a, 1);
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("tuple", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ArrayInitializer_ExistingReportsFreshSilent()
    {
        const string source = """
            public static class Code
            {
                public static void Run()
                {
                    var a = new MyBuffer();
                    var copies = new MyBuffer[] { a };
                    var moves = new MyBuffer[] { new MyBuffer() };
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("copied into a collection", diagnostic.GetMessage());
    }

    [Fact]
    public async Task NullableWrap_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Run()
                {
                    var a = new MyBuffer();
                    MyBuffer? maybe = a;
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Nullable", diagnostic.GetMessage());
    }

    [Fact]
    public async Task RefReturnReceivedByValue_Reports()
    {
        // a ref-returning method hands out existing storage: receiving the result by value
        // (assignment, argument, or a by-value return) dereferences and copies it
        const string source = """
            public class Holder
            {
                MyBuffer field;
                public ref MyBuffer Get() { return ref field; }
                public MyBuffer TakeCopy() { return Get(); }
                public void Run()
                {
                    var copy = Get();
                    External.Take(Get());
                    ref var alias = ref Get();
                    alias.Dispose();
                }
            }
            """;
        var corlib = CompileStub("corlibstub", CorlibStub);
        var coreLibrary = CompileStub("MessagePack", CoreLibraryStub, corlib);
        var callee = CompileStub("Callee", BufferDefinition + """
            public static class External
            {
                public static void Take(MyBuffer buffer) { }
            }
            """, corlib, coreLibrary);
        var compilation = CSharpCompilation.Create(
            "UserAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            [corlib, coreLibrary, callee],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var diagnostics = await compilation.WithAnalyzers([new NonCopyableBufferAnalyzer()]).GetAnalyzerDiagnosticsAsync(CancellationToken.None);

        Assert.Equal(3, diagnostics.Length);
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("returned by value from existing storage"));
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("copied by assignment"));
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("passed by value"));
    }

    [Fact]
    public async Task ReturnOfUsingLocal_Reports()
    {
        // the frame disposes a using local on exit, so the returned copy is already dead
        const string source = """
            public static class Code
            {
                public static MyBuffer FromDeclaration()
                {
                    using var local = new MyBuffer();
                    return local;
                }

                public static MyBuffer FromStatement()
                {
                    using (var local = new MyBuffer())
                    {
                        return local;
                    }
                }

                public static MyBuffer PlainLocal()
                {
                    var local = new MyBuffer();
                    return local;
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Contains("using local", d.GetMessage()));
    }

    [Fact]
    public async Task UsingStatementOnExisting_Reports()
    {
        const string source = """
            public static class Code
            {
                public static void Run()
                {
                    var a = new MyBuffer();
                    using (a)
                    {
                    }
                }
            }
            """;
        var diagnostics = await RunAnalyzerAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("using statement", diagnostic.GetMessage());
    }
}
