using MessagePack.SourceGenerator.Analyzers;

namespace MessagePack.Tests;

// MsgPack114: a Serialize/Deserialize path that never touches the buffer loses (or
// leaves) one value on the wire and shifts every later element.
public class BufferUntouchedPathAnalyzerTest
{
    const string Shell = """

        [MessagePackObject]
        public class Inner { [Key(0)] public int X { get; set; } }
        """;

    static string Formatter(string serializeBody, string deserializeBody = "buffer.ReadArrayHeader();") => $$"""
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
            static void WriteAll(ref TWriteBuffer buffer, ref SerializeState state, Inner[] value) { }
        }
        """ + Shell;

    [Fact]
    public async Task NullReturnWithoutWriteNil_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new BufferUntouchedPathAnalyzer(), Formatter("""
            if (value == null)
            {
                return;
            }
            state.Enter();
            buffer.WriteArrayHeader(value.Length);
            state.Exit();
            """));
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(BufferUntouchedPathAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("ProbeFormatter", diagnostic.GetMessage());
        Assert.Contains("Serialize", diagnostic.GetMessage());
        Assert.Contains("without writing", diagnostic.GetMessage());
    }

    [Fact]
    public async Task NullReturnWithWriteNil_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new BufferUntouchedPathAnalyzer(), Formatter("""
            if (value == null)
            {
                buffer.WriteNil();
                return;
            }
            buffer.WriteArrayHeader(value.Length);
            """));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ThrowPath_Silent()
    {
        // a throw ends the operation and is not a path through the stream
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new BufferUntouchedPathAnalyzer(), Formatter("""
            if (value == null)
            {
                throw new MessagePackSerializationException("null is not allowed here");
            }
            buffer.WriteArrayHeader(value.Length);
            """));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DelegatedToHelperOnEveryPath_Silent()
    {
        // a ref forward counts as touching the buffer (the helper is trusted)
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new BufferUntouchedPathAnalyzer(), Formatter("""
            if (value == null)
            {
                inner.Serialize(ref buffer, ref state, null!);
                return;
            }
            WriteAll(ref buffer, ref state, value);
            """));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DeserializeReturnsWithoutReading_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new BufferUntouchedPathAnalyzer(), Formatter("buffer.WriteNil();", """
            if (value != null)
            {
                return; // keeps the caller's instance, but the element is still in the stream
            }
            var count = buffer.ReadArrayHeader();
            value = new Inner[count];
            """));
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Deserialize", diagnostic.GetMessage());
        Assert.Contains("without reading", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DeserializeReadsOnEveryPath_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new BufferUntouchedPathAnalyzer(), Formatter("buffer.WriteNil();", """
            if (buffer.TryReadNil())
            {
                value = null!;
                return;
            }
            var count = buffer.ReadArrayHeader();
            value = new Inner[count];
            """));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LoopWithConditionalWrite_Reports()
    {
        // a loop that may run zero times leaves a path with no write at all
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new BufferUntouchedPathAnalyzer(), Formatter("""
            for (int i = 0; i < value.Length; i++)
            {
                inner.Serialize(ref buffer, ref state, value[i]);
            }
            """));
        Assert.Single(diagnostics);
    }
}
