using MessagePack.SourceGenerator.Analyzers;

namespace MessagePack.Tests;

// MsgPack109: entry calls inside a formatter restart serialization with default options,
// bypassing the active resolver, buffer and depth tracking.
public class FormatterEntryCallAnalyzerTest
{
    const string FormatterShell = """

        [MessagePackObject]
        public class Inner { [Key(0)] public int X { get; set; } }
        """;

    [Fact]
    public async Task EntryCallInsideFormatter_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new FormatterEntryCallAnalyzer(), """
            using MessagePack;
            using SerializerFoundation;
            public sealed class BadFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Inner>
                where TWriteBuffer : struct, IWriteBuffer
                where TReadBuffer : struct, IReadBuffer
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Inner value)
                {
                    var bytes = MessagePackSerializer.Serialize(value.X);
                }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Inner value) { }
            }
            """ + FormatterShell);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(FormatterEntryCallAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("BadFormatter", diagnostic.GetMessage());
        Assert.Contains("Serialize", diagnostic.GetMessage());
    }

    [Fact]
    public async Task EntryCallInsideLambdaInFormatter_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new FormatterEntryCallAnalyzer(), """
            using System;
            using MessagePack;
            using SerializerFoundation;
            public sealed class LambdaFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Inner>
                where TWriteBuffer : struct, IWriteBuffer
                where TReadBuffer : struct, IReadBuffer
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Inner value)
                {
                    Func<byte[]> capture = () => MessagePackSerializer.Serialize(1);
                    capture();
                }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Inner value) { }
            }
            """ + FormatterShell);
        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task EntryCallOutsideFormatters_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new FormatterEntryCallAnalyzer(), """
            using MessagePack;
            public static class Caller
            {
                public static byte[] Run() => MessagePackSerializer.Serialize(new Inner());
            }
            """ + FormatterShell);
        Assert.Empty(diagnostics);
    }
}
