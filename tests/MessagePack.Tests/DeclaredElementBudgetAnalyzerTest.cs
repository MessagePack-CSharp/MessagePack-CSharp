using MessagePack.SourceGenerator.Analyzers;

namespace MessagePack.Tests;

// MsgPack115: a body with a DeserializeState in scope must read array/map headers through the charging
// overloads (buffer.ReadArrayHeader(ref state) / buffer.ReadMapHeader(ref state)), so the declared-element
// budget sees every header of the message.
public class DeclaredElementBudgetAnalyzerTest
{
    const string Shell = """

        [MessagePackObject]
        public class Inner { [Key(0)] public int X { get; set; } }
        """;

    static string Formatter(string deserializeBody, string extraMembers = "") => $$"""
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
                buffer.WriteNil();
            }
            public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Inner[] value)
            {
                {{deserializeBody}}
            }
            {{extraMembers}}
        }
        """ + Shell;

    static Task<System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>> RunAsync(string source) =>
        AnalyzerTestHost.RunAnalyzerAsync(new DeclaredElementBudgetAnalyzer(), source);

    [Fact]
    public async Task ChargingOverloads_Silent()
    {
        var diagnostics = await RunAsync(Formatter("""
            var count = buffer.ReadArrayHeader(ref state);
            var entries = buffer.ReadMapHeader(ref state);
            value = new Inner[count + entries];
            """));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task StatelessArrayHeader_InDeserialize_Reports()
    {
        var diagnostics = await RunAsync(Formatter("""
            var count = buffer.ReadArrayHeader();
            value = new Inner[count];
            state.Enter();
            for (int i = 0; i < count; i++)
            {
                inner.Deserialize(ref buffer, ref state, ref value[i]);
            }
            state.Exit();
            """));
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DeclaredElementBudgetAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("ReadArrayHeader(ref state)", diagnostic.GetMessage());
        Assert.Contains("ProbeFormatter<TWriteBuffer, TReadBuffer>.Deserialize", diagnostic.GetMessage());
    }

    [Fact]
    public async Task StatelessMapHeader_InDeserialize_Reports()
    {
        var diagnostics = await RunAsync(Formatter("""
            var count = buffer.ReadMapHeader();
            value = new Inner[count];
            """));
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DeclaredElementBudgetAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("ReadMapHeader(ref state)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task StatelessHeader_InHelperWithStateParameter_Reports()
    {
        // the state is available in the helper too; the budget must see nested containers read there
        var diagnostics = await RunAsync(Formatter("""
            value = ReadItems(ref buffer, ref state);
            """, """
            static Inner[] ReadItems(ref TReadBuffer buffer, ref DeserializeState state)
            {
                var count = buffer.ReadArrayHeader();
                return new Inner[count];
            }
            """));
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DeclaredElementBudgetAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("ReadItems", diagnostic.GetMessage());
    }

    [Fact]
    public async Task StatelessHeader_InLocalFunctionWithStateParameter_Reports()
    {
        var diagnostics = await RunAsync(Formatter("""
            value = ReadItems(ref buffer, ref state);
            static Inner[] ReadItems(ref TReadBuffer buffer, ref DeserializeState state)
            {
                return new Inner[buffer.ReadArrayHeader()];
            }
            """));
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DeclaredElementBudgetAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("ReadItems", diagnostic.GetMessage());
    }

    [Fact]
    public async Task StatelessHeader_WithoutStateInScope_Silent()
    {
        // a scanner that never allocates (Skip-like) has no state to charge and stays on the plain reader
        var diagnostics = await RunAsync(Formatter("""
            value = Array.Empty<Inner>();
            """, """
            static int CountItems(ref TReadBuffer buffer)
            {
                return buffer.ReadArrayHeader() + buffer.ReadMapHeader();
            }
            """));
        Assert.Empty(diagnostics);
    }
}
