using System.Buffers;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using SerializerFoundation;
using UltraMessagePack;
using static UltraMessagePack.MessagePackPrimitives;

// Faithful port of Nerdbank.MessagePack's own benchmark (test/Benchmarks/SimplePoco.cs +
// PocoClass.cs + Data.cs, MIT), plus UltraMessagePack under the same rules, to check whether
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
    ArrayBufferWriter<byte> buffer = new(); // non-readonly: Ultra's Serialize takes ref TBufferWriter

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
        MessagePack.MessagePackSerializer.Serialize(this.buffer, NbData.PocoMapSingle, MessagePack.MessagePackSerializerOptions.Standard);
        this.buffer.Clear();
    }

    [BenchmarkCategory("map", "Serialize"), Benchmark]
    public void SerializeMap_Ultra()
    {
        UltraMessagePack.MessagePackSerializer.Default.Serialize(this.buffer, NbData.PocoMapSingle);
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
        MessagePack.MessagePackSerializer.Deserialize<NbPocoMap>(NbData.PocoMapMsgpack, MessagePack.MessagePackSerializerOptions.Standard);
    }

    [BenchmarkCategory("map", "Deserialize"), Benchmark]
    public void DeserializeMap_Ultra()
    {
        UltraMessagePack.MessagePackSerializer.Default.Deserialize<NbPocoMap>(NbData.PocoMapMsgpack);
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
        MessagePack.MessagePackSerializer.Serialize(this.buffer, NbData.PocoAsArraySingle, MessagePack.MessagePackSerializerOptions.Standard);
        this.buffer.Clear();
    }

    [BenchmarkCategory("array", "Serialize"), Benchmark]
    public void SerializeAsArray_Ultra()
    {
        UltraMessagePack.MessagePackSerializer.Default.Serialize(this.buffer, NbData.PocoAsArraySingle);
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
        MessagePack.MessagePackSerializer.Deserialize<NbPocoAsArray>(NbData.PocoAsArrayMsgpack, MessagePack.MessagePackSerializerOptions.Standard);
    }

    [BenchmarkCategory("array", "Deserialize"), Benchmark]
    public void DeserializeAsArray_Ultra()
    {
        UltraMessagePack.MessagePackSerializer.Default.Deserialize<NbPocoAsArray>(NbData.PocoAsArrayMsgpack);
    }
}

static class NbData
{
    internal static readonly NbPocoMap PocoMapSingle = new() { SomeInt = 42, SomeString = "Hello, World!" };
    internal static readonly ReadOnlySequence<byte> PocoMapMsgpack = new(MessagePack.MessagePackSerializer.Serialize(PocoMapSingle, MessagePack.MessagePackSerializerOptions.Standard));

    internal static readonly NbPocoAsArray PocoAsArraySingle = new() { SomeInt = 42, SomeString = "Hello, World!" };
    internal static readonly ReadOnlySequence<byte> PocoAsArrayMsgpack = new(MessagePack.MessagePackSerializer.Serialize(PocoAsArraySingle, MessagePack.MessagePackSerializerOptions.Standard));
}

[PolyType.GenerateShape]
[MessagePack.MessagePackObject(keyAsPropertyName: true)]
public partial class NbPocoMap
{
    public int SomeInt { get; set; }

    public string? SomeString { get; set; }
}

[PolyType.GenerateShape]
[MessagePack.MessagePackObject]
public partial class NbPocoAsArray
{
    [MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
    public int SomeInt { get; set; }

    [MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
    public string? SomeString { get; set; }
}

// Hand-written Ultra formatters standing in for what a source generator would emit
// (BenchPersonFormatter style). Map keys are compared as utf8 in place — no string
// allocation, the counterpart of MessagePack-CSharp's automata key matching.
public sealed class NbPocoMapFormatter<TWriteBuffer, TReadBuffer> : UltraMessagePack.IMessagePackFormatter<TWriteBuffer, TReadBuffer, NbPocoMap>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(UltraMessagePack.MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref UltraMessagePack.SerializeState state, ref NbPocoMap value)
    {
        buffer.Advance(UnsafeWriteFixMapHeader(ref buffer.GetReference(1), 2));
        buffer.Advance(UnsafeWriteString(ref buffer.GetReference("SomeInt"u8.Length + 5), "SomeInt"u8));
        buffer.Advance(UnsafeWriteInt32(ref buffer.GetReference(MaxInt32Length), value.SomeInt));
        buffer.Advance(UnsafeWriteString(ref buffer.GetReference("SomeString"u8.Length + 5), "SomeString"u8));
        buffer.Advance(UnsafeWriteString(ref buffer.GetReference(GetMaxStringByteCount(value.SomeString)), value.SomeString));
    }

    public void Deserialize(ref TReadBuffer buffer, ref UltraMessagePack.DeserializeState state, ref NbPocoMap value)
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
            var key = buffer.GetSpan(byteCount).Slice(0, byteCount);
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
                throw new UltraMessagePack.MessagePackSerializationException("Unknown key in NbPocoMap map.");
            }
        }
    }
}
