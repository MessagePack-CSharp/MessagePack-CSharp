extern alias V3;
using System;
using MessagePack;
using MessagePack.Formatters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MessagePack.SourceGenerator.Generators;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Member-level [MessagePackFormatter]: the source generator binds the attribute (including
// the expression-string argument form) and emits direct construction; the reflection tier
// honors the v3 forms and rejects the expression form with a pointer at the generator.
public class FormatterAttributeTests
{
    // ---- source-generated tier (the types below compile through the real generator) ----

    [Fact]
    public void Factory_ComparerExpression_AppliesToDeserializedDictionary()
    {
        var value = new ComparerViaFactoryPoco { Map = new Dictionary<string, int> { ["abc"] = 1 } };
        var back = V4.Deserialize<ComparerViaFactoryPoco>(V4.Serialize(value))!;

        Assert.True(back.Map!.ContainsKey("ABC")); // OrdinalIgnoreCase reached the deserializer
        Assert.Equal(1, back.Map["abc"]);

        // a plain dictionary member (no attribute) keeps the default comparer
        var plain = V4.Deserialize<PlainDictionaryPoco>(V4.Serialize(new PlainDictionaryPoco { Map = new Dictionary<string, int> { ["abc"] = 1 } }))!;
        Assert.False(plain.Map!.ContainsKey("ABC"));
    }

    [Fact]
    public void FormatterType_WithConstructorArgument_OverridesEvenDirectPrimitives()
    {
        var bytes = V4.Serialize(new AddendPoco { Value = 1 });

        // the custom formatter wrote value + 100, so int members no longer ride the
        // generator's direct-write path when the attribute is present
        var raw = V4.Deserialize<int[]>(bytes)!;
        Assert.Equal(101, raw[0]);

        Assert.Equal(1, V4.Deserialize<AddendPoco>(bytes)!.Value);
    }

    // ---- reflection tier (SuppressSourceGeneration keeps the types off the generator) ----

    static MessagePackSerializerOptions FreshOptions() =>
        new(new MessagePackFormatterResolver(MessagePackFormatterFactory.Default));

    [Fact]
    public void Reflection_FactoryForm_Works()
    {
        var options = FreshOptions();
        var value = new ReflectionFactoryPoco { Map = new Dictionary<string, int> { ["abc"] = 1 } };
        var back = V4.Deserialize<ReflectionFactoryPoco>(V4.Serialize(value, options), options)!;
        Assert.Equal(1, back.Map!["abc"]);
    }

    [Fact]
    public void Reflection_FormatterForm_WithArgument_Works()
    {
        var options = FreshOptions();
        var bytes = V4.Serialize(new ReflectionAddendPoco { Value = 1 }, options);
        Assert.Equal(101, V4.Deserialize<int[]>(bytes, options)![0]);
        Assert.Equal(1, V4.Deserialize<ReflectionAddendPoco>(bytes, options)!.Value);
    }

    [Fact]
    public void Reflection_ExpressionForm_IsRejectedTowardTheSourceGenerator()
    {
        var options = FreshOptions();
        var exception = Record.Exception(() => V4.Serialize(new ReflectionExpressionPoco(), options));
        Assert.NotNull(exception);
        Assert.Contains("source generator", exception.ToString());
    }

    [Fact]
    public void Factory_DotNetOptimizedDateTime_AppliesToOneMemberOnly()
    {
        var stamp = new DateTime(2026, 8, 24, 1, 2, 3, DateTimeKind.Unspecified);
        var back = V4.Deserialize<OptimizedStampPoco>(V4.Serialize(new OptimizedStampPoco { Stamp = stamp, Plain = stamp }))!;

        // the ToBinary wire form preserves Kind exactly; the default timestamp ext cannot
        Assert.Equal(stamp, back.Stamp);
        Assert.Equal(DateTimeKind.Unspecified, back.Stamp.Kind);
        Assert.Equal(DateTimeKind.Utc, back.Plain.Kind);
    }

    [Fact]
    public void Factory_CombinedDotNetOptimized_ServesTheMemberType()
    {
        var value = new OptimizedGuidPoco { Id = Guid.NewGuid() };
        var bytes = V4.Serialize(value);

        Assert.Equal(16, V4.Deserialize<byte[][]>(bytes)![0].Length); // bin image, not the default 36-char str
        Assert.Equal(value.Id, V4.Deserialize<OptimizedGuidPoco>(bytes)!.Id);
    }

    [Fact]
    public void Factory_BuiltIn_OptsAMemberOutOfTheChainCustomization()
    {
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.DotNetOptimized));
        var stamp = new DateTime(2026, 8, 24, 1, 2, 3, DateTimeKind.Unspecified);
        var back = V4.Deserialize<BuiltInOptOutPoco>(V4.Serialize(new BuiltInOptOutPoco { Stamp = stamp, Plain = stamp }, options), options)!;

        // the annotated member rode the plain timestamp ext (Utc-coercing) even though the
        // chain is DotNetOptimized; the plain member got the chain's Kind-preserving form
        Assert.Equal(DateTimeKind.Utc, back.Stamp.Kind);
        Assert.Equal(DateTimeKind.Unspecified, back.Plain.Kind);
    }

    [Fact]
    public void GenericAttributeVariant_WithAndWithoutArguments()
    {
        var stamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        var value = new GenericAttributePoco { Stamp = stamp, Map = new Dictionary<string, int> { ["abc"] = 1 } };
        var back = V4.Deserialize<GenericAttributePoco>(V4.Serialize(value))!;

        Assert.Equal(stamp, back.Stamp);
        Assert.Equal(DateTimeKind.Unspecified, back.Stamp.Kind);
        Assert.True(back.Map!.ContainsKey("ABC"));
    }

    [Fact]
    public void Reflection_V3AnnotationAssembly_ReadsFormatterTypeByFallback()
    {
        // the v3 attribute carries FormatterType, not FactoryType; the runtime reader
        // falls back to the old property name
        var options = FreshOptions();
        var bytes = V4.Serialize(new V3AnnotatedFormatterPoco { Value = 1 }, options);
        Assert.Equal(101, V4.Deserialize<int[]>(bytes, options)![0]);
        Assert.Equal(1, V4.Deserialize<V3AnnotatedFormatterPoco>(bytes, options)!.Value);
    }

    // ---- type-level [MessagePackFormatter] (compiled into the generated factory's
    // registration; there is no runtime attribute tier) ----

    [Fact]
    public void TypeLevel_Attribute_BeatsTheGeneratedFormatter()
    {
        var bytes = V4.Serialize(new TypeLevelWinsPoco { Value = 1 });

        // the generated formatter would write a 1-element array; the bare int proves the
        // attribute-directed construction outranked it inside the generated factory, v3's priority
        Assert.Equal(1001, V4.Deserialize<int>(bytes));
        Assert.Equal(1, V4.Deserialize<TypeLevelWinsPoco>(bytes)!.Value);
    }

    [Fact]
    public void TypeLevel_PlainType_IsServedByTheAttributeAlone()
    {
        // no [MessagePackObject]: the module-initializer registration alone serves this type
        var bytes = V4.Serialize(new PlainShiftedPoco { Value = 1 });
        Assert.Equal(8, V4.Deserialize<int>(bytes)); // ctor argument 7 reached the formatter
        Assert.Equal(1, V4.Deserialize<PlainShiftedPoco>(bytes)!.Value);
    }

    [Fact]
    public void TypeLevel_IsServedByTheAotChain()
    {
        // DefaultAot is SourceGenerated + BuiltIn only: passing proves the annotation rides
        // the registry tier like any generated formatter, no reflection anywhere
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.DefaultAot));

        var bytes = V4.Serialize(new PlainShiftedPoco { Value = 1 }, options);
        Assert.Equal(8, V4.Deserialize<int>(bytes, options));
        Assert.Equal(1, V4.Deserialize<PlainShiftedPoco>(bytes, options)!.Value);

        var winsBytes = V4.Serialize(new TypeLevelWinsPoco { Value = 1 }, options);
        Assert.Equal(1001, V4.Deserialize<int>(winsBytes, options)); // attribute still beats the generated formatter
    }

    [Fact]
    public void TypeLevel_OnEnum_ClosedEnumAsStringFactory()
    {
        // the enum-as-string opt-in spelling: the factory is generic, so it closes over the
        // annotated enum itself; its constructor is (bool ignoreCase = false), so the
        // zero-argument attribute binds through the trailing optional parameter
        var bytes = V4.Serialize(StringyFruit.Banana);
        Assert.Equal(0xA6, bytes[0]); // fixstr(6) "Banana", not the default fixint encoding
        Assert.Equal(StringyFruit.Banana, V4.Deserialize<StringyFruit>(bytes));
    }

    // ---- generator diagnostics (driver-hosted, real MessagePack references) ----

    const string ExpressionProbe = """
        using System.Collections.Generic;
        using MessagePack;
        using MessagePack.Formatters;

        [MessagePackObject]
        public class Probe
        {
            [Key(0)]
            [MessagePackFormatter(typeof(DictionaryFormatterFactory<string, int>), "StringComparer.OrdinalIgnoreCase")]
            public Dictionary<string, int>? Map { get; set; }
        }
        """;

    static (GeneratorDriverRunResult Result, Compilation Updated) RunGenerator(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "GeneratorProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        var updatedDriver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _, CancellationToken.None);
        return (updatedDriver.GetRunResult(), updated);
    }

    [Fact]
    public void Generator_ExpressionForm_BindsAtTheAttributeSiteAndCompiles()
    {
        var (result, updated) = RunGenerator("using System;\n" + ExpressionProbe);

        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var formatter = Assert.Single(result.Results[0].GeneratedSources, s => s.HintName.Contains("ProbeFormatter"));
        Assert.Contains("global::System.StringComparer.OrdinalIgnoreCase", formatter.SourceText.ToString());
        Assert.Empty(updated.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Generator_UnresolvableExpression_ReportsUMP013()
    {
        // no `using System;`: StringComparer does not bind at the attribute site
        var (result, _) = RunGenerator(ExpressionProbe);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack013");
    }

    const string TypeLevelExpressionProbe = """
        using System.Collections.Generic;
        using MessagePack;
        using MessagePack.Formatters;

        [MessagePackFormatter(typeof(DictionaryFormatterFactory<string, int>), "StringComparer.OrdinalIgnoreCase")]
        public class Probe
        {
        }
        """;

    [Fact]
    public void Generator_TypeLevel_ExpressionForm_LandsInTheGeneratedFactory()
    {
        var (result, updated) = RunGenerator("using System;\n" + TypeLevelExpressionProbe);

        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var factory = Assert.Single(result.Results[0].GeneratedSources, s => s.HintName.Contains("GeneratedMessagePackFormatterFactory"));
        Assert.Contains("global::System.StringComparer.OrdinalIgnoreCase", factory.SourceText.ToString());
        Assert.Contains("SourceGeneratedFormatterFactory.Instance.Register(typeof(global::Probe)", factory.SourceText.ToString());
        Assert.Empty(updated.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Generator_TypeLevel_UnresolvableExpression_ReportsUMP013()
    {
        // no `using System;`: StringComparer does not bind at the attribute site
        var (result, _) = RunGenerator(TypeLevelExpressionProbe);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack013");
    }

    [Fact]
    public void Generator_TypeLevel_GenericAttributeVariant_LandsInTheGeneratedFactory()
    {
        var (result, updated) = RunGenerator("""
            using System;
            using System.Collections.Generic;
            using MessagePack;
            using MessagePack.Formatters;

            [MessagePackFormatter<DictionaryFormatterFactory<string, int>>("StringComparer.OrdinalIgnoreCase")]
            public class Probe
            {
            }
            """);

        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var factory = Assert.Single(result.Results[0].GeneratedSources, s => s.HintName.Contains("GeneratedMessagePackFormatterFactory"));
        Assert.Contains("global::System.StringComparer.OrdinalIgnoreCase", factory.SourceText.ToString());
        Assert.Empty(updated.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Generator_TypeLevel_GenericType_ReportsUMP014()
    {
        // no runtime tier serves what the generator stands down from — the stand-down is a diagnostic now
        var (result, _) = RunGenerator("""
            using MessagePack;
            using MessagePack.Formatters;

            [MessagePackFormatter(typeof(DotNetOptimizedFormatterFactory))]
            public class Probe<T>
            {
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack014");
    }

    [Fact]
    public void Generator_TypeLevel_SuppressSourceGeneration_ReportsUMP014()
    {
        var (result, _) = RunGenerator("""
            using MessagePack;
            using MessagePack.Formatters;

            [MessagePackObject(SuppressSourceGeneration = true)]
            [MessagePackFormatter(typeof(DotNetOptimizedFormatterFactory))]
            public class Probe
            {
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack014");
    }

    [Fact]
    public void Generator_NonFormatterType_ReportsUMP012()
    {
        var (result, _) = RunGenerator("""
            using MessagePack;

            [MessagePackObject]
            public class Probe
            {
                [Key(0)]
                [MessagePackFormatter(typeof(string))]
                public int Value { get; set; }
            }
            """);
        Assert.Contains(result.Diagnostics, d => d.Id == "MsgPack012");
    }
}

// generated tier fixtures

[MessagePackObject]
public class ComparerViaFactoryPoco
{
    [Key(0)]
    [MessagePackFormatter(typeof(DictionaryFormatterFactory<string, int>), "StringComparer.OrdinalIgnoreCase")]
    public Dictionary<string, int>? Map { get; set; }
}

[MessagePackObject]
public class PlainDictionaryPoco
{
    [Key(0)]
    public Dictionary<string, int>? Map { get; set; }
}

[MessagePackObject]
public class AddendPoco
{
    [Key(0)]
    [MessagePackFormatter(typeof(AddendInt32Formatter<,>), 100)]
    public int Value { get; set; }
}

[MessagePackObject]
public class OptimizedStampPoco
{
    [Key(0)]
    [MessagePackFormatter(typeof(DotNetOptimizedFormatterFactory))]
    public DateTime Stamp { get; set; }

    [Key(1)]
    public DateTime Plain { get; set; }
}

[MessagePackObject]
public class BuiltInOptOutPoco
{
    [Key(0)]
    [MessagePackFormatter<BuiltInFormatterFactory>]
    public DateTime Stamp { get; set; }

    [Key(1)]
    public DateTime Plain { get; set; }
}

[MessagePackObject]
public class GenericAttributePoco
{
    [Key(0)]
    [MessagePackFormatter<DotNetOptimizedFormatterFactory>]
    public DateTime Stamp { get; set; }

    [Key(1)]
    [MessagePackFormatter<DictionaryFormatterFactory<string, int>>("StringComparer.OrdinalIgnoreCase")]
    public Dictionary<string, int>? Map { get; set; }
}

// annotated purely with the v3 attribute types; SuppressSourceGeneration routes it to the
// reflection tier, where the attribute contract is matched by NAME
[V3::MessagePack.MessagePackObject(SuppressSourceGeneration = true)]
public class V3AnnotatedFormatterPoco
{
    [V3::MessagePack.Key(0)]
    [V3::MessagePack.MessagePackFormatter(typeof(AddendInt32Formatter<,>), 100)]
    public int Value { get; set; }
}

[MessagePackObject]
public class OptimizedGuidPoco
{
    [Key(0)]
    [MessagePackFormatter(typeof(DotNetOptimizedFormatterFactory))]
    public Guid Id { get; set; }
}

// reflection tier fixtures

[MessagePackObject(SuppressSourceGeneration = true)]
public class ReflectionFactoryPoco
{
    [Key(0)]
    [MessagePackFormatter(typeof(DictionaryFormatterFactory<string, int>))]
    public Dictionary<string, int>? Map { get; set; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class ReflectionAddendPoco
{
    [Key(0)]
    [MessagePackFormatter(typeof(AddendInt32Formatter<,>), 100)]
    public int Value { get; set; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class ReflectionExpressionPoco
{
    [Key(0)]
    [MessagePackFormatter(typeof(DictionaryFormatterFactory<string, int>), "StringComparer.OrdinalIgnoreCase")]
    public Dictionary<string, int>? Map { get; set; }
}

// type-level fixtures

[MessagePackObject]
[MessagePackFormatter(typeof(TypeLevelWinsFormatter<,>))]
public class TypeLevelWinsPoco
{
    [Key(0)]
    public int Value { get; set; }
}

public sealed class TypeLevelWinsFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TypeLevelWinsPoco?>
    where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer, allows ref struct
    where TReadBuffer : struct, SerializerFoundation.IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TypeLevelWinsPoco? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteInt32(value.Value + 1000);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TypeLevelWinsPoco? value)
    {
        value = buffer.TryReadNil() ? null : new TypeLevelWinsPoco { Value = buffer.ReadInt32() - 1000 };
    }
}

[MessagePackFormatter(typeof(ShiftedIntFormatter<,>), 7)]
public class PlainShiftedPoco
{
    public int Value { get; set; }
}

public sealed class ShiftedIntFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, PlainShiftedPoco?>
    where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer, allows ref struct
    where TReadBuffer : struct, SerializerFoundation.IReadBuffer, allows ref struct
{
    readonly int shift;

    public ShiftedIntFormatter(int shift)
    {
        this.shift = shift;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, PlainShiftedPoco? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteInt32(value.Value + shift);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref PlainShiftedPoco? value)
    {
        value = buffer.TryReadNil() ? null : new PlainShiftedPoco { Value = buffer.ReadInt32() - shift };
    }
}

[MessagePackFormatter<EnumAsStringFormatterFactory<StringyFruit>>]
public enum StringyFruit
{
    Apple,
    Banana,
}

public sealed class AddendInt32Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
    where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer, allows ref struct
    where TReadBuffer : struct, SerializerFoundation.IReadBuffer, allows ref struct
{
    readonly int addend;

    public AddendInt32Formatter(int addend)
    {
        this.addend = addend;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value)
    {
        buffer.WriteInt32(value + addend);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value)
    {
        value = buffer.ReadInt32() - addend;
    }
}
