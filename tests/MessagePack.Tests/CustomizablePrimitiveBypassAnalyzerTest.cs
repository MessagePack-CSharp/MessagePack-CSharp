using MessagePack.SourceGenerator.Analyzers;

namespace MessagePack.Tests;

// MsgPack104: string and DateTime are the resolver-customizable primitives (interning, native
// DateTime format), so a formatter calling the buffer primitive directly bypasses that
// configuration; structural writes (utf8 span overload, header APIs) stay legal.
public class CustomizablePrimitiveBypassAnalyzerTest
{
    const string FormatterHeader = """
        using System;
        using MessagePack;
        using SerializerFoundation;
        """;

    [Fact]
    public async Task StringPrimitivesInsideFormatter_Report()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new CustomizablePrimitiveBypassAnalyzer(), FormatterHeader + """
            public sealed class ProbeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?>
                where TWriteBuffer : struct, IWriteBuffer, allows ref struct
                where TReadBuffer : struct, IReadBuffer, allows ref struct
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, string? value)
                {
                    buffer.WriteString(value);
                }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref string? value)
                {
                    value = buffer.ReadString();
                }
            }
            """);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal(CustomizablePrimitiveBypassAnalyzer.DiagnosticId, d.Id));
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("'WriteString'") && d.GetMessage().Contains("string"));
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("'ReadString'"));
    }

    [Fact]
    public async Task TimestampPrimitivesInsideFormatter_Report()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new CustomizablePrimitiveBypassAnalyzer(), FormatterHeader + """
            public sealed class ProbeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, DateTime>
                where TWriteBuffer : struct, IWriteBuffer, allows ref struct
                where TReadBuffer : struct, IReadBuffer, allows ref struct
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, DateTime value)
                {
                    buffer.WriteTimestamp(value);
                }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref DateTime value)
                {
                    value = buffer.ReadTimestamp();
                }
            }
            """);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Contains("DateTime", d.GetMessage()));
    }

    [Fact]
    public async Task NestedHelperInsideFormatter_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new CustomizablePrimitiveBypassAnalyzer(), FormatterHeader + """
            public sealed class ProbeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?>
                where TWriteBuffer : struct, IWriteBuffer, allows ref struct
                where TReadBuffer : struct, IReadBuffer, allows ref struct
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, string? value)
                {
                    Helper.Write(ref buffer, value);
                }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref string? value)
                {
                    buffer.Skip();
                }
                static class Helper
                {
                    public static void Write(ref TWriteBuffer buffer, string? value)
                    {
                        buffer.WriteString(value);
                    }
                }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(CustomizablePrimitiveBypassAnalyzer.DiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task StructuralApisInsideFormatter_Silent()
    {
        // the utf8 span overload and the header APIs are the sanctioned routes for
        // protocol-structural strings (map keys, enum names, wire representations)
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new CustomizablePrimitiveBypassAnalyzer(), FormatterHeader + """
            public sealed class ProbeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?>
                where TWriteBuffer : struct, IWriteBuffer, allows ref struct
                where TReadBuffer : struct, IReadBuffer, allows ref struct
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, string? value)
                {
                    buffer.WriteString("probe"u8);
                }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref string? value)
                {
                    var byteCount = buffer.ReadStringHeader();
                    buffer.Advance(byteCount);
                    value = null;
                }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ResolverFormatterRoute_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new CustomizablePrimitiveBypassAnalyzer(), FormatterHeader + """
            public sealed class ProbeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?>
                where TWriteBuffer : struct, IWriteBuffer, allows ref struct
                where TReadBuffer : struct, IReadBuffer, allows ref struct
            {
                IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?> stringFormatter = null!;
                public void Initialize(MessagePackFormatterResolver resolver)
                {
                    stringFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, string?>();
                }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, string? value)
                {
                    stringFormatter.Serialize(ref buffer, ref state, value);
                }
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref string? value)
                {
                    stringFormatter.Deserialize(ref buffer, ref state, ref value);
                }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task OutsideFormatter_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new CustomizablePrimitiveBypassAnalyzer(), FormatterHeader + """
            public static class TopLevelProtocol
            {
                public static void Write<TWriteBuffer>(ref TWriteBuffer buffer, string? value, DateTime stamp)
                    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
                {
                    buffer.WriteString(value);
                    buffer.WriteTimestamp(stamp);
                }
            }
            """);
        Assert.Empty(diagnostics);
    }
}
