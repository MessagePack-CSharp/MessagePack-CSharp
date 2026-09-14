extern alias V3;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SerializerFoundation;
using MessagePack;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Nullable<primitive> members ride the DIRECT emit tier now: the write is a nil-or-value
// expression inside the same fused reservation (nil is 1 byte, under every primitive's
// max), the read is TryReadNil-or-ReadX, and no formatter field exists. The wire is
// unchanged — byte-checked against the reflection tier (NullableFormatter) and the v3
// oracle.
public class NullableDirectTests
{
    static readonly MessagePack.MessagePackSerializerOptions reflectionOptions = new(
        new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

    static void AssertAllTiersAgree<T>(T value)
    {
        var oracle = Oracle.Serialize(value);
        Assert.Equal(oracle, V4.Serialize(value));                    // generated (direct emit)
        Assert.Equal(oracle, V4.Serialize(value, reflectionOptions)); // NullableFormatter path

        var back = V4.Deserialize<T>(oracle);
        Assert.Equal(oracle, Oracle.Serialize(back));
    }

    [Fact]
    public void IntKeyPoco_ValuesAndNulls()
    {
        AssertAllTiersAgree(new NullableDirectPoco { A = 300, B = -1.5, C = true, D = 'x', E = long.MinValue });
        AssertAllTiersAgree(new NullableDirectPoco());
        AssertAllTiersAgree(new NullableDirectPoco { A = 0, C = false });
    }

    [Fact]
    public void MapModePoco_ValuesAndNulls()
    {
        AssertAllTiersAgree(new NullableDirectMapPoco { A = 42, B = null });
        AssertAllTiersAgree(new NullableDirectMapPoco { A = null, B = 2.5f });
    }

    [Fact]
    public void ConstructionPath_NullablePrimitiveParameter()
    {
        var back = V4.Deserialize<NullableDirectRecord>(V4.Serialize(new NullableDirectRecord(7, null)));
        Assert.Equal(7, back!.Count);
        Assert.Null(back.Ratio);

        var full = V4.Deserialize<NullableDirectRecord>(V4.Serialize(new NullableDirectRecord(null, 0.5)));
        Assert.Null(full!.Count);
        Assert.Equal(0.5, full.Ratio);
    }

    [Fact]
    public void GeneratedSource_HasNoFormatterFieldAndFusesTheNilChoice()
    {
        var compilation = AnalyzerTestHost.CreateCompilation("""
            using MessagePack;
            [MessagePackObject]
            public class Probe
            {
                [Key(0)] public int? A { get; set; }
                [Key(1)] public double? B { get; set; }
            }
            """, "NullableDirectProbe");
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: (CSharpParseOptions)compilation.SyntaxTrees[0].Options);
        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _, CancellationToken.None).GetRunResult();

        var source = Assert.Single(result.Results[0].GeneratedSources, s => s.HintName.Contains("Probe")).SourceText.ToString();
        Assert.DoesNotContain("GetFormatter<TWriteBuffer, TReadBuffer, int?>", source);
        Assert.Contains("UnsafeWriteNil", source);
        Assert.Contains("buffer.TryReadNil() ? default(int?) : buffer.ReadInt32()", source);
        Assert.Empty(updated.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }
}

// the V3-aliased attributes serve both serializers (matched by full name)
[V3::MessagePack.MessagePackObject]
public class NullableDirectPoco
{
    [V3::MessagePack.Key(0)] public int? A { get; set; }
    [V3::MessagePack.Key(1)] public double? B { get; set; }
    [V3::MessagePack.Key(2)] public bool? C { get; set; }
    [V3::MessagePack.Key(3)] public char? D { get; set; }
    [V3::MessagePack.Key(4)] public long? E { get; set; }
}

[V3::MessagePack.MessagePackObject(true)]
public class NullableDirectMapPoco
{
    public int? A { get; set; }
    public float? B { get; set; }
}

[MessagePackObject]
public record NullableDirectRecord([property: Key(0)] int? Count, [property: Key(1)] double? Ratio);
