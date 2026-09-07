extern alias V3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using MessagePack;
using MessagePack.Formatters;
using SerializerFoundation;

// EnumCollectionFormatters.cs: TEnum[] reinterpreted (MemoryMarshal.Cast) as the underlying
// integer array and pushed through the primitive element codecs, vs the per-element path
// it replaced (ArrayFormatter<TEnum> over EnumInt32Formatter / EnumByteFormatter, i.e.
// one interface call per element) vs MessagePack-CSharp v3 (the same per-element shape).
// The idea is protobuf-net v4's packed-enum pun at the call site: no enum-specific codec,
// the column IS the integer column. Both the int32-backed and the byte-backed enum are
// measured because byte got a NEW codec (byte[] itself is bin-format and never had one).
//   Small - every value a positive fixint: the 16-lane verbatim-copy superlane end to end
//   Mixed - values across every token width of the underlying type: scalar chunks + re-probe
// N=1000 lets the predictor memorize the data (CLAUDE.md pitfall); N=100_000 does not.
//
// MEASURED 2026-09-02 (i7-13700KF, AVX2 tier, ratio vs the per-element control in-run;
// Int32 cells re-run with --job medium, Byte cells ShortRun):
//                      Int32 ser   Int32 deser   Byte ser   Byte deser     v3 (Mpcs) vs control
//   1000    Small        0.18         0.16          0.11        0.07        1.6-2.0x SLOWER
//   1000    Mixed        0.90         0.96          0.64        0.71        1.5-3.2x slower
//   100000  Small        0.39         0.57          0.24        0.19        1.6-2.0x slower
//   100000  Mixed        1.09         0.98          0.98        1.04        1.2-3.3x slower
// All-fixint runs (the realistic enum column: small discriminators) take the verbatim
// 16/32-lane copy and gain 2.5-9x; the 8-class Mixed distribution is the int[] codec's
// known trade (probes that rarely hit, CLAUDE.md tie band at 100k), inherited unchanged
// since the enum column IS the integer column. VERDICT: adopted, no enum-specific code.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class EnumArrayFormatterBenchmark
{
    public enum IntLevel { Zero = 0 }
    public enum ByteFlag : byte { Zero = 0 }

    [Params(1000, 100_000)]
    public int N = 1000;

    [Params("Small", "Mixed")]
    public string Dist = "Small";

    IntLevel[] intData = default!;
    ByteFlag[] byteData = default!;
    byte[] intPayload = default!;
    byte[] bytePayload = default!;
    MessagePackSerializerOptions perElement = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        intData = new IntLevel[N];
        byteData = new ByteFlag[N];
        for (int i = 0; i < N; i++)
        {
            intData[i] = (IntLevel)(Dist switch
            {
                "Small" => rand.Next(0, 128),
                _ => rand.Next(8) switch
                {
                    0 => rand.Next(0, 128),
                    1 => rand.Next(-32, 0),
                    2 => rand.Next(128, 256),
                    3 => rand.Next(-128, -32),
                    4 => rand.Next(256, 65536),
                    5 => rand.Next(-32768, -128),
                    6 => rand.Next(65536, int.MaxValue),
                    _ => rand.Next(int.MinValue, -32768),
                },
            });
            byteData[i] = (ByteFlag)(Dist switch
            {
                "Small" => rand.Next(0, 128),
                _ => rand.Next(0, 256),
            });
        }

        // the per-element shape the codec path replaced: ArrayFormatter<TEnum> over the enum
        // scalar formatter, claimed ahead of the routing that now picks EnumArrayFormatter
        perElement = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [new PerElementEnumArrayFactory(), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));

        intPayload = MessagePackSerializer.Serialize(intData);
        bytePayload = MessagePackSerializer.Serialize(byteData);
        Verify(intData, intPayload, perElement);
        Verify(byteData, bytePayload, perElement);
    }

    // every candidate must agree with v3 and with each other before anything is timed
    internal static void Verify<TEnum>(TEnum[] data, byte[] payload, MessagePackSerializerOptions perElement)
        where TEnum : struct, Enum
    {
        var oracle = V3::MessagePack.MessagePackSerializer.Serialize(data);
        if (!payload.AsSpan().SequenceEqual(oracle)) throw new InvalidOperationException($"verify failed: {typeof(TEnum).Name}[] codec bytes vs oracle");
        if (!MessagePackSerializer.Serialize(data, perElement).AsSpan().SequenceEqual(oracle)) throw new InvalidOperationException($"verify failed: {typeof(TEnum).Name}[] per-element bytes vs oracle");
        if (!MessagePackSerializer.Deserialize<TEnum[]>(payload)!.AsSpan().SequenceEqual(data)) throw new InvalidOperationException($"verify failed: {typeof(TEnum).Name}[] codec roundtrip");
        if (!MessagePackSerializer.Deserialize<TEnum[]>(payload, perElement)!.AsSpan().SequenceEqual(data)) throw new InvalidOperationException($"verify failed: {typeof(TEnum).Name}[] per-element roundtrip");
        if (!V3::MessagePack.MessagePackSerializer.Deserialize<TEnum[]>(payload).AsSpan().SequenceEqual(data)) throw new InvalidOperationException($"verify failed: {typeof(TEnum).Name}[] v3 reads codec bytes");
    }

    [BenchmarkCategory("Serialize Int32"), Benchmark(Baseline = true)]
    public byte[] SerializeInt32PerElement() => MessagePackSerializer.Serialize(intData, perElement);

    [BenchmarkCategory("Serialize Int32"), Benchmark]
    public byte[] SerializeInt32Codec() => MessagePackSerializer.Serialize(intData);

    [BenchmarkCategory("Serialize Int32"), Benchmark]
    public byte[] SerializeInt32Mpcs() => V3::MessagePack.MessagePackSerializer.Serialize(intData);

    [BenchmarkCategory("Deserialize Int32"), Benchmark(Baseline = true)]
    public IntLevel[] DeserializeInt32PerElement() => MessagePackSerializer.Deserialize<IntLevel[]>(intPayload, perElement)!;

    [BenchmarkCategory("Deserialize Int32"), Benchmark]
    public IntLevel[] DeserializeInt32Codec() => MessagePackSerializer.Deserialize<IntLevel[]>(intPayload)!;

    [BenchmarkCategory("Deserialize Int32"), Benchmark]
    public IntLevel[] DeserializeInt32Mpcs() => V3::MessagePack.MessagePackSerializer.Deserialize<IntLevel[]>(intPayload);

    [BenchmarkCategory("Serialize Byte"), Benchmark(Baseline = true)]
    public byte[] SerializeBytePerElement() => MessagePackSerializer.Serialize(byteData, perElement);

    [BenchmarkCategory("Serialize Byte"), Benchmark]
    public byte[] SerializeByteCodec() => MessagePackSerializer.Serialize(byteData);

    [BenchmarkCategory("Serialize Byte"), Benchmark]
    public byte[] SerializeByteMpcs() => V3::MessagePack.MessagePackSerializer.Serialize(byteData);

    [BenchmarkCategory("Deserialize Byte"), Benchmark(Baseline = true)]
    public ByteFlag[] DeserializeBytePerElement() => MessagePackSerializer.Deserialize<ByteFlag[]>(bytePayload, perElement)!;

    [BenchmarkCategory("Deserialize Byte"), Benchmark]
    public ByteFlag[] DeserializeByteCodec() => MessagePackSerializer.Deserialize<ByteFlag[]>(bytePayload)!;

    [BenchmarkCategory("Deserialize Byte"), Benchmark]
    public ByteFlag[] DeserializeByteMpcs() => V3::MessagePack.MessagePackSerializer.Deserialize<ByteFlag[]>(bytePayload);
}

// the replaced routing, kept as the control: the generic per-element array formatter over
// the resolver's enum scalar formatter (what GenericFormatterFactory returned for TEnum[]
// before EnumArrayFormatterFactory)
public sealed partial class PerElementEnumArrayFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        if (type == typeof(EnumArrayFormatterBenchmark.IntLevel[])) return new ArrayFormatter<TWriteBuffer, TReadBuffer, EnumArrayFormatterBenchmark.IntLevel>();
        if (type == typeof(EnumArrayFormatterBenchmark.ByteFlag[])) return new ArrayFormatter<TWriteBuffer, TReadBuffer, EnumArrayFormatterBenchmark.ByteFlag>();
        return null;
    }
}
