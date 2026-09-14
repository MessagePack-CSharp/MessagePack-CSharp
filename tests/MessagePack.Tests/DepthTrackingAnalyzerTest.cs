using MessagePack.SourceGenerator.Analyzers;

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
}
