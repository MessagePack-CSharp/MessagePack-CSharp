using MessagePack.SourceGenerator.Analyzers;

namespace MessagePack.Tests;

// MsgPack105: the [MessagePackFormatter] type argument only fails at runtime, and the
// generator's MsgPack012 covers just the contexts it consumes; the analyzer checks every
// application live.
public class FormatterAttributeTypeAnalyzerTest
{
    [Fact]
    public async Task NonFactoryType_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new FormatterAttributeTypeAnalyzer(), """
            using MessagePack;
            public class Holder
            {
                [MessagePackFormatter(typeof(string))]
                public int Value { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(FormatterAttributeTypeAnalyzer.DiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task AbstractFactory_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new FormatterAttributeTypeAnalyzer(), """
            using MessagePack;
            public class Holder
            {
                [MessagePackFormatter(typeof(MessagePackFormatterFactory))]
                public int Value { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("abstract", diagnostic.GetMessage());
    }

    [Fact]
    public async Task PrivateConstructorSingleton_Reports()
    {
        // both attribute paths construct via new; Instance does not help
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new FormatterAttributeTypeAnalyzer(), """
            using MessagePack;
            public sealed class SingletonFactory : MessagePackFormatterFactory
            {
                public static readonly SingletonFactory Instance = new SingletonFactory();
                SingletonFactory() { }
                public override object? CreateFormatter(System.Type writeBufferType, System.Type readBufferType, System.Type valueType) => null;
            }
            public class Holder
            {
                [MessagePackFormatter(typeof(SingletonFactory))]
                public int Value { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("no accessible constructor", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ArgumentCountMismatch_Reports()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new FormatterAttributeTypeAnalyzer(), """
            using System.Collections.Generic;
            using MessagePack;
            using MessagePack.Formatters;
            public class Holder
            {
                [MessagePackFormatter(typeof(DictionaryFormatterFactory<string, int>), 1, 2)]
                public Dictionary<string, int>? Map { get; set; }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("2 supplied argument(s)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task MatchingArity_Silent()
    {
        // arity only: the expression-string argument is the generator's business, not this rule's
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new FormatterAttributeTypeAnalyzer(), """
            using System.Collections.Generic;
            using MessagePack;
            using MessagePack.Formatters;
            public class Holder
            {
                [MessagePackFormatter(typeof(DictionaryFormatterFactory<string, int>), "StringComparer.OrdinalIgnoreCase")]
                public Dictionary<string, int>? Map { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ConcreteFactory_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new FormatterAttributeTypeAnalyzer(), """
            using System.Collections.Generic;
            using MessagePack;
            using MessagePack.Formatters;
            public class Holder
            {
                [MessagePackFormatter(typeof(DictionaryFormatterFactory<string, int>))]
                public Dictionary<string, int>? Map { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task TrailingOptionalConstructorParameters_Silent()
    {
        // EnumAsStringFormatterFactory<T>'s constructor is (bool ignoreCase = false): zero
        // supplied arguments bind through the optional tail, like in any C# call
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new FormatterAttributeTypeAnalyzer(), """
            using MessagePack;
            using MessagePack.Formatters;
            [MessagePackFormatter<EnumAsStringFormatterFactory<Fruit>>]
            public enum Fruit { Apple }
            """);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task UnboundFormatterGeneric_Silent()
    {
        var diagnostics = await AnalyzerTestHost.RunAnalyzerAsync(new FormatterAttributeTypeAnalyzer(), """
            using MessagePack;
            using SerializerFoundation;
            public sealed class ProbeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
                where TWriteBuffer : struct, IWriteBuffer, allows ref struct
                where TReadBuffer : struct, IReadBuffer, allows ref struct
            {
                public void Initialize(MessagePackFormatterResolver resolver) { }
                public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value) => buffer.WriteInt32(value);
                public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value) => value = buffer.ReadInt32();
            }
            public class Holder
            {
                [MessagePackFormatter(typeof(ProbeFormatter<,>))]
                public int Value { get; set; }
            }
            """);
        Assert.Empty(diagnostics);
    }
}
