using System.Collections.Immutable;
using MessagePack.SourceGenerator.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.Tests;

// MsgPack112: state.Enter()/state.Exit() must pair on every path through a body.
// MsgPack113: a container formatter (header + nested formatter descent) must Enter at all.
public class DepthTrackingAnalyzerTest
{
    const string Shell = """

        [MessagePackObject]
        public class Inner { [Key(0)] public int X { get; set; } }
        """;

    // a hand-written array-of-Inner formatter with the given Serialize / Deserialize bodies
    static string Formatter(string serializeBody, string deserializeBody = "") => $$"""
        using System;
        using MessagePack;
        using SerializerFoundation;
        public sealed class ProbeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Inner[]>
            where TWriteBuffer : struct, IWriteBuffer
            where TReadBuffer : struct, IReadBuffer
        {
            IMessagePackFormatter<TWriteBuffer, TReadBuffer, Inner> inner = null!;
            public void Initialize(MessagePackFormatterResolver resolver)
            {
                inner = resolver.GetFormatter<TWriteBuffer, TReadBuffer, Inner>();
            }
            public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Inner[] value)
            {
                {{serializeBody}}
            }
            public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Inner[] value)
            {
                {{deserializeBody}}
            }
        }
        """ + Shell;

    static async Task<Microsoft.CodeAnalysis.Diagnostic> SingleAsync(string source, string expectedId)
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(expectedId, diagnostic.Id);
        return diagnostic;
    }

    [Fact]
    public async Task BalancedContainer_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), Formatter("""
            if (value == null) { buffer.WriteNil(); return; }
            state.Enter();
            buffer.WriteArrayHeader(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                inner.Serialize(ref buffer, ref state, value[i]);
            }
            state.Exit();
            """, """
            if (buffer.TryReadNil()) { value = null!; return; }
            var count = buffer.ReadArrayHeader();
            var result = new Inner[count];
            state.Enter();
            for (int i = 0; i < count; i++)
            {
                inner.Deserialize(ref buffer, ref state, ref result[i]);
            }
            state.Exit();
            value = result;
            """));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task EarlyReturnBetweenEnterAndExit_ReportsUnbalanced()
    {
        var diagnostic = await SingleAsync(Formatter("""
            state.Enter();
            buffer.WriteArrayHeader(value.Length);
            if (value.Length == 0)
            {
                return;
            }
            inner.Serialize(ref buffer, ref state, value[0]);
            state.Exit();
            """), DepthTrackingAnalyzer.UnbalancedDiagnosticId);
        Assert.Contains("still open", diagnostic.GetMessage());
    }

    [Fact]
    public async Task EnterInsideLoopWithoutExit_ReportsUnbalanced()
    {
        var diagnostic = await SingleAsync(Formatter("""
            buffer.WriteArrayHeader(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                state.Enter();
                inner.Serialize(ref buffer, ref state, value[i]);
            }
            state.Exit();
            """), DepthTrackingAnalyzer.UnbalancedDiagnosticId);
        Assert.Contains("every path", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ConditionalEnterOnly_ReportsUnbalanced()
    {
        var diagnostic = await SingleAsync(Formatter("""
            if (value.Length > 0)
            {
                state.Enter();
            }
            buffer.WriteArrayHeader(value.Length);
            state.Exit();
            """), DepthTrackingAnalyzer.UnbalancedDiagnosticId);
        Assert.Contains("every path", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ExitWithoutEnter_ReportsUnbalanced()
    {
        var diagnostic = await SingleAsync(Formatter("""
            buffer.WriteArrayHeader(value.Length);
            inner.Serialize(ref buffer, ref state, value[0]);
            state.Exit();
            """), DepthTrackingAnalyzer.UnbalancedDiagnosticId);
        Assert.Contains("without a preceding state.Enter()", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ThrowAfterEnter_Silent()
    {
        // a throw ends the operation; the depth budget dies with the state
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), Formatter("""
            state.Enter();
            buffer.WriteArrayHeader(value.Length);
            if (value.Length > 100)
            {
                throw new MessagePackSerializationException("too many");
            }
            inner.Serialize(ref buffer, ref state, value[0]);
            state.Exit();
            """));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ExitInFinally_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), Formatter("""
            state.Enter();
            try
            {
                buffer.WriteArrayHeader(value.Length);
                if (value.Length == 0)
                {
                    return;
                }
                inner.Serialize(ref buffer, ref state, value[0]);
            }
            finally
            {
                state.Exit();
            }
            """));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task UnbalancedLocalFunction_ReportsAtLocalFunction()
    {
        var diagnostic = await SingleAsync(Formatter("""
            buffer.WriteArrayHeader(value.Length);
            Populate(ref buffer, ref state, value);

            void Populate(ref TWriteBuffer buffer, ref SerializeState state, Inner[] items)
            {
                state.Enter();
                inner.Serialize(ref buffer, ref state, items[0]);
            }
            """), DepthTrackingAnalyzer.UnbalancedDiagnosticId);
        Assert.Contains("Populate", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Fact]
    public async Task BalancedHelperOutsideFormatter_Silent()
    {
        // MsgPack112 follows Enter/Exit into any body, formatter or not
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), """
            using MessagePack;
            public static class Helper
            {
                public static void Nest(ref SerializeState state, bool nested)
                {
                    state.Enter();
                    state.Exit();
                }
            }
            """ + Shell);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task UnbalancedHelperOutsideFormatter_Reports()
    {
        await SingleAsync("""
            using MessagePack;
            public static class Helper
            {
                public static void Nest(ref DeserializeState state, bool nested)
                {
                    state.Enter();
                    if (nested)
                    {
                        return;
                    }
                    state.Exit();
                }
            }
            """ + Shell, DepthTrackingAnalyzer.UnbalancedDiagnosticId);
    }

    [Fact]
    public async Task ContainerWithoutEnter_ReportsMissingEnter()
    {
        var diagnostic = await SingleAsync(Formatter("""
            buffer.WriteArrayHeader(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                inner.Serialize(ref buffer, ref state, value[i]);
            }
            """), DepthTrackingAnalyzer.MissingEnterDiagnosticId);
        Assert.Contains("ProbeFormatter", diagnostic.GetMessage());
        Assert.Contains("Serialize", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ReadSideContainerWithoutEnter_ReportsMissingEnter()
    {
        var diagnostic = await SingleAsync(Formatter("", """
            var count = buffer.ReadArrayHeader();
            var result = new Inner[count];
            for (int i = 0; i < count; i++)
            {
                inner.Deserialize(ref buffer, ref state, ref result[i]);
            }
            value = result;
            """), DepthTrackingAnalyzer.MissingEnterDiagnosticId);
        Assert.Contains("Deserialize", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DelegatingWrapperWithoutHeader_Silent()
    {
        // Nullable/surrogate style: no container of its own, the inner formatter charges its level
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), Formatter("""
            inner.Serialize(ref buffer, ref state, value[0]);
            """, """
            inner.Deserialize(ref buffer, ref state, ref value[0]);
            """));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task EmptyHeaderOnAnotherPath_Silent()
    {
        // ObjectFallbackFormatter shape: a constant-0 header (bare object as empty map) on one
        // path, a delegation to the runtime type's formatter on another; neither is a level
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), Formatter("""
            if (value.Length == 0)
            {
                buffer.WriteMapHeader(0);
                return;
            }
            inner.Serialize(ref buffer, ref state, value[0]);
            """));
        Assert.Empty(diagnostics);
    }

    // a container formatter whose descent is split into a helper of its own (the element loop), declared in the
    // formatter's file or in another file of the partial type
    const string HelperEntry = """
        using System;
        using MessagePack;
        using SerializerFoundation;
        public sealed partial class ProbeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Inner[]>
            where TWriteBuffer : struct, IWriteBuffer
            where TReadBuffer : struct, IReadBuffer
        {
            IMessagePackFormatter<TWriteBuffer, TReadBuffer, Inner> inner = null!;
            public void Initialize(MessagePackFormatterResolver resolver)
            {
                inner = resolver.GetFormatter<TWriteBuffer, TReadBuffer, Inner>();
            }
            public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Inner[] value)
            {
                buffer.WriteArrayHeader(value.Length);
                WriteElements(ref buffer, ref state, value);
            }
            public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Inner[] value)
            {
            }
        }
        """ + Shell;

    const string HelperPart = """
        public sealed partial class ProbeFormatter<TWriteBuffer, TReadBuffer>
            where TWriteBuffer : struct, IWriteBuffer
            where TReadBuffer : struct, IReadBuffer
        {
            void WriteElements(ref TWriteBuffer buffer, ref SerializeState state, Inner[] value)
            {
                foreach (var item in value)
                {
                    inner.Serialize(ref buffer, ref state, item);
                }
            }
        }
        """;

    [Fact]
    public async Task ContainerDescendingThroughHelper_ReportsMissingEnter()
    {
        var diagnostics = await RunAsync(HelperEntry + Environment.NewLine + HelperPart);
        Assert.Equal(DepthTrackingAnalyzer.MissingEnterDiagnosticId, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task ContainerDescendingThroughHelperInAnotherFile_ReportsMissingEnter()
    {
        // the helper's body is in another syntax tree, read through a semantic model of its own
        var diagnostics = await RunAsync(HelperEntry, "using MessagePack;" + Environment.NewLine + "using SerializerFoundation;" + Environment.NewLine + HelperPart);
        Assert.Equal(DepthTrackingAnalyzer.MissingEnterDiagnosticId, Assert.Single(diagnostics).Id);
    }

    static async Task<ImmutableArray<Diagnostic>> RunAsync(params string[] sources)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(sources[0]);
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
        compilation = compilation.AddSyntaxTrees(sources.Skip(1).Select(source => CSharpSyntaxTree.ParseText(source, parseOptions)));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        return await compilation.WithAnalyzers([new DepthTrackingAnalyzer()]).GetAnalyzerDiagnosticsAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FlatHeaderWithoutDescent_Silent()
    {
        // a fixed shape of primitives (BitArray style) writes a header but never nests a formatter
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), Formatter("""
            buffer.WriteFixArrayHeader(2);
            buffer.WriteInt32(value.Length);
            buffer.WriteInt32(value.Length);
            """));
        Assert.Empty(diagnostics);
    }

    // the whole container (header and element loop) moved into a helper the entry hands its buffer and state to
    static string ContainerInHelper(string deserializeBody, string helperBody) => Formatter("buffer.WriteNil();", deserializeBody).Replace(
        "    public void Deserialize(",
        $$"""
            Inner[] ReadItems(ref TReadBuffer buffer, ref DeserializeState state)
            {
                {{helperBody}}
            }
            public void Deserialize(
        """);

    const string HelperReadsItems = """
        var count = buffer.ReadArrayHeader(ref state);
        var result = new Inner[count];
        for (int i = 0; i < count; i++)
        {
            inner.Deserialize(ref buffer, ref state, ref result[i]);
        }
        return result;
        """;

    [Fact]
    public async Task ContainerMovedIntoHelper_ReportsMissingEnter()
    {
        // the entry itself reads no header: the level is the helper's, one call down
        var diagnostic = await SingleAsync(ContainerInHelper("value = ReadItems(ref buffer, ref state);", HelperReadsItems), DepthTrackingAnalyzer.MissingEnterDiagnosticId);
        Assert.Contains("Deserialize", diagnostic.GetMessage());
        Assert.Contains("'ReadItems'", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ContainerMovedIntoHelper_ChargedByTheEntry_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), ContainerInHelper("""
            state.Enter();
            value = ReadItems(ref buffer, ref state);
            state.Exit();
            """, HelperReadsItems));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ContainerMovedIntoHelper_ChargedByTheHelper_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), ContainerInHelper("value = ReadItems(ref buffer, ref state);", """
            var count = buffer.ReadArrayHeader(ref state);
            var result = new Inner[count];
            state.Enter();
            for (int i = 0; i < count; i++)
            {
                inner.Deserialize(ref buffer, ref state, ref result[i]);
            }
            state.Exit();
            return result;
            """));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task HelperThatOnlyReadsScalars_Silent()
    {
        // a header in the entry, a helper that takes the state but descends nowhere: a flat shape
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), ContainerInHelper("""
            var count = buffer.ReadArrayHeader(ref state);
            value = ReadItems(ref buffer, ref state);
            """, """
            buffer.ReadInt32();
            return new Inner[0];
            """));
        Assert.Empty(diagnostics);
    }

    // a tree read by a helper that calls itself: every turn is a level, whatever the entry charges once
    static string RecursiveHelper(string deserializeBody, string nestedCall) => Formatter("buffer.WriteNil();", deserializeBody).Replace(
        "    public void Deserialize(",
        $$"""
            static int CountLeaves(ref TReadBuffer buffer, ref DeserializeState state)
            {
                if (buffer.TryReadNil())
                {
                    return 1;
                }
                var count = buffer.ReadArrayHeader(ref state);
                var leaves = 0;
                for (int i = 0; i < count; i++)
                {
                    {{nestedCall}}
                }
                return leaves;
            }
            public void Deserialize(
        """);

    [Fact]
    public async Task RecursiveHelperWithoutEnter_ReportsTheHelper()
    {
        var diagnostic = await SingleAsync(RecursiveHelper("""
            state.Enter();
            value = new Inner[CountLeaves(ref buffer, ref state)];
            state.Exit();
            """, "leaves += CountLeaves(ref buffer, ref state);"), DepthTrackingAnalyzer.MissingEnterDiagnosticId);
        Assert.Contains("CountLeaves", diagnostic.GetMessage());
        Assert.Contains("calls itself", diagnostic.GetMessage());
        Assert.Contains("CountLeaves", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Fact]
    public async Task RecursiveHelperThatCharges_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new DepthTrackingAnalyzer(), RecursiveHelper(
            "value = new Inner[CountLeaves(ref buffer, ref state)];",
            """
            state.Enter();
            leaves += CountLeaves(ref buffer, ref state);
            state.Exit();
            """));
        Assert.Empty(diagnostics);
    }
}
