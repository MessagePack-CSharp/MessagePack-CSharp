// MsgPack104: hand-written baseline formatter; the direct primitive calls are the benchmarked subject.
#pragma warning disable MsgPack104

extern alias V3;
using System.Buffers;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using SerializerFoundation;
using MessagePack;
using static MessagePack.MessagePackPrimitives;

// Faithful port of Nerdbank.MessagePack's own benchmark (test/Benchmarks/SimplePoco.cs +
// PocoClass.cs + Data.cs, MIT), plus MessagePack under the same rules, to check whether
// the official "on par with MessagePack-CSharp" result reproduces on this machine. Differences
// from our SerializerPocoBenchmark, all copied from their setup: 2-field POCO (int + string),
// Serialize writes into a reused ArrayBufferWriter (no result-array allocation), Deserialize
// reads a ReadOnlySequence, results are discarded (void), and the Nerdbank serializer sets
// SerializeDefaultValues = Always.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class NerdbankOfficialBenchmark
{
    readonly Nerdbank.MessagePack.MessagePackSerializer serializer = new() { SerializeDefaultValues = Nerdbank.MessagePack.SerializeDefaultValuesPolicy.Always };
    ArrayBufferWriter<byte> buffer = new(); // non-readonly: V4's Serialize takes ref TBufferWriter

    [GlobalSetup]
    public void Setup()
    {
    }

    [BenchmarkCategory("map", "Serialize"), Benchmark]
    public void SerializeMap_Nerdbank()
    {
        this.serializer.Serialize(this.buffer, NbData.PocoMapSingle);
        this.buffer.Clear();
    }

    [BenchmarkCategory("map", "Serialize"), Benchmark(Baseline = true)]
    public void SerializeMap_MsgPackCSharp()
    {
        V3::MessagePack.MessagePackSerializer.Serialize(this.buffer, NbData.PocoMapSingle, V3::MessagePack.MessagePackSerializerOptions.Standard);
        this.buffer.Clear();
    }

    [BenchmarkCategory("map", "Serialize"), Benchmark]
    public void SerializeMap_MsgPackCSharpSourceGen()
    {
        V3::MessagePack.MessagePackSerializer.Serialize(this.buffer, NbData.PocoMapSingleV3SourceGen, V3GeneratedModels.V3SourceGen.Options);
        this.buffer.Clear();
    }

    [BenchmarkCategory("map", "Serialize"), Benchmark]
    public void SerializeMap_V4()
    {
        MessagePack.MessagePackSerializer.Serialize(this.buffer, NbData.PocoMapSingle);
        this.buffer.Clear();
    }

    [BenchmarkCategory("map", "Deserialize"), Benchmark]
    public void DeserializeMap_Nerdbank()
    {
        this.serializer.Deserialize<NbPocoMap>(NbData.PocoMapMsgpack);
    }

    [BenchmarkCategory("map", "Deserialize"), Benchmark(Baseline = true)]
    public void DeserializeMap_MsgPackCSharp()
    {
        V3::MessagePack.MessagePackSerializer.Deserialize<NbPocoMap>(NbData.PocoMapMsgpack, V3::MessagePack.MessagePackSerializerOptions.Standard);
    }

    [BenchmarkCategory("map", "Deserialize"), Benchmark]
    public void DeserializeMap_MsgPackCSharpSourceGen()
    {
        V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.NbPocoMap>(NbData.PocoMapMsgpack, V3GeneratedModels.V3SourceGen.Options);
    }

    [BenchmarkCategory("map", "Deserialize"), Benchmark]
    public void DeserializeMap_V4()
    {
        MessagePack.MessagePackSerializer.Deserialize<NbPocoMap>(NbData.PocoMapMsgpack);
    }

    [BenchmarkCategory("array", "Serialize"), Benchmark]
    public void SerializeAsArray_Nerdbank()
    {
        this.serializer.Serialize(this.buffer, NbData.PocoAsArraySingle);
        this.buffer.Clear();
    }

    [BenchmarkCategory("array", "Serialize"), Benchmark(Baseline = true)]
    public void SerializeAsArray_MsgPackCSharp()
    {
        V3::MessagePack.MessagePackSerializer.Serialize(this.buffer, NbData.PocoAsArraySingle, V3::MessagePack.MessagePackSerializerOptions.Standard);
        this.buffer.Clear();
    }

    [BenchmarkCategory("array", "Serialize"), Benchmark]
    public void SerializeAsArray_MsgPackCSharpSourceGen()
    {
        V3::MessagePack.MessagePackSerializer.Serialize(this.buffer, NbData.PocoAsArraySingleV3SourceGen, V3GeneratedModels.V3SourceGen.Options);
        this.buffer.Clear();
    }

    [BenchmarkCategory("array", "Serialize"), Benchmark]
    public void SerializeAsArray_V4()
    {
        MessagePack.MessagePackSerializer.Serialize(this.buffer, NbData.PocoAsArraySingle);
        this.buffer.Clear();
    }

    [BenchmarkCategory("array", "Deserialize"), Benchmark]
    public void DeserializeAsArray_Nerdbank()
    {
        this.serializer.Deserialize<NbPocoAsArray>(NbData.PocoAsArrayMsgpack);
    }

    [BenchmarkCategory("array", "Deserialize"), Benchmark(Baseline = true)]
    public void DeserializeAsArray_MsgPackCSharp()
    {
        V3::MessagePack.MessagePackSerializer.Deserialize<NbPocoAsArray>(NbData.PocoAsArrayMsgpack, V3::MessagePack.MessagePackSerializerOptions.Standard);
    }

    [BenchmarkCategory("array", "Deserialize"), Benchmark]
    public void DeserializeAsArray_MsgPackCSharpSourceGen()
    {
        V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.NbPocoAsArray>(NbData.PocoAsArrayMsgpack, V3GeneratedModels.V3SourceGen.Options);
    }

    [BenchmarkCategory("array", "Deserialize"), Benchmark]
    public void DeserializeAsArray_V4()
    {
        MessagePack.MessagePackSerializer.Deserialize<NbPocoAsArray>(NbData.PocoAsArrayMsgpack);
    }
}

static class NbData
{
    internal static readonly NbPocoMap PocoMapSingle = new() { SomeInt = 42, SomeString = "Hello, World!" };
    internal static readonly ReadOnlySequence<byte> PocoMapMsgpack = new(V3::MessagePack.MessagePackSerializer.Serialize(PocoMapSingle, V3::MessagePack.MessagePackSerializerOptions.Standard));

    internal static readonly NbPocoAsArray PocoAsArraySingle = new() { SomeInt = 42, SomeString = "Hello, World!" };
    internal static readonly ReadOnlySequence<byte> PocoAsArrayMsgpack = new(V3::MessagePack.MessagePackSerializer.Serialize(PocoAsArraySingle, V3::MessagePack.MessagePackSerializerOptions.Standard));

    // v3 source-gen twins, materialized from the standard payloads (see AnswerBenchmark
    // for the satellite rationale; identical data by construction)
    internal static readonly V3GeneratedModels.NbPocoMap PocoMapSingleV3SourceGen =
        V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.NbPocoMap>(PocoMapMsgpack, V3GeneratedModels.V3SourceGen.Options);
    internal static readonly V3GeneratedModels.NbPocoAsArray PocoAsArraySingleV3SourceGen =
        V3::MessagePack.MessagePackSerializer.Deserialize<V3GeneratedModels.NbPocoAsArray>(PocoAsArrayMsgpack, V3GeneratedModels.V3SourceGen.Options);
}

[PolyType.GenerateShape]
[V3::MessagePack.MessagePackObject(keyAsPropertyName: true)]
public partial class NbPocoMap
{
    public int SomeInt { get; set; }

    public string? SomeString { get; set; }
}

[PolyType.GenerateShape]
[V3::MessagePack.MessagePackObject]
public partial class NbPocoAsArray
{
    [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
    public int SomeInt { get; set; }

    [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
    public string? SomeString { get; set; }
}

// Hand-written V4 formatters standing in for what a source generator would emit
// (BenchPersonFormatter style). Map keys are compared as utf8 in place — no string
// allocation, the counterpart of MessagePack-CSharp's automata key matching.
public sealed class NbPocoMapFormatter<TWriteBuffer, TReadBuffer> : MessagePack.IMessagePackFormatter<TWriteBuffer, TReadBuffer, NbPocoMap>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePack.MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref MessagePack.SerializeState state, NbPocoMap value)
    {
        buffer.Advance(UnsafeWriteFixMapHeader(ref buffer.GetReference(1), 2));
        buffer.Advance(UnsafeWriteString(ref buffer.GetReference("SomeInt"u8.Length + 5), "SomeInt"u8));
        buffer.Advance(UnsafeWriteInt32(ref buffer.GetReference(MaxInt32Length), value.SomeInt));
        buffer.Advance(UnsafeWriteString(ref buffer.GetReference("SomeString"u8.Length + 5), "SomeString"u8));
        buffer.Advance(UnsafeWriteString(ref buffer.GetReference(GetMaxStringByteCount(value.SomeString)), value.SomeString));
    }

    public void Deserialize(ref TReadBuffer buffer, ref MessagePack.DeserializeState state, ref NbPocoMap value)
    {
        var count = buffer.ReadMapHeader();

        if (value == null)
        {
            value = new NbPocoMap();
        }

        for (int i = 0; i < count; i++)
        {
            // compare BEFORE Advance: the span may alias a pooled stitch buffer
            var byteCount = buffer.ReadStringHeader();
            if (!buffer.TryGetSpan(byteCount, out var key))
            {
                throw new MessagePack.MessagePackSerializationException("Unexpected end of data while reading a map key.");
            }
            key = key.Slice(0, byteCount);
            if (key.SequenceEqual("SomeInt"u8))
            {
                buffer.Advance(byteCount);
                value.SomeInt = buffer.ReadInt32();
            }
            else if (key.SequenceEqual("SomeString"u8))
            {
                buffer.Advance(byteCount);
                value.SomeString = buffer.ReadString();
            }
            else
            {
                throw new MessagePack.MessagePackSerializationException("Unknown key in NbPocoMap map.");
            }
        }
    }
}
