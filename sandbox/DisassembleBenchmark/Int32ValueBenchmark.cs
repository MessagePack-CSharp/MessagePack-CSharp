extern alias V3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

// A bare int as the whole message: the Redis-style pattern (counters, ids, scores pushed
// as single values) where entry overhead IS the workload — formatter body is a couple of
// instructions, so this measures the serializer's fixed cost per call: options/resolver
// touch, formatter fetch, buffer setup, result-array allocation.
// v4 (MessagePack) vs v3 (MessagePack-CSharp) vs Nerdbank.MessagePack, byte[] in/out.
// Value ranges pick one representative per msgpack wire width: positive fixint (1B),
// uint8 (2B), uint16 (3B), uint32 (5B). All three libraries write the canonical smallest
// encoding, verified byte-identical in Setup.
// System.Text.Json rows ride their own utf8 JSON payload (comparable 1-6 byte sizes;
// this benchmark compares ENTRY architecture, not wire): the plain generic entry pays
// the options/JsonTypeInfo lookup per call, the JsonTypeInfo row hands the source-gen
// metadata in directly — STJ's analog of the v4 typed-formatter-in-hand shape.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public partial class Int32ValueBenchmark
{
    [Params(8, 200, 3000, 300_000)]
    public int Value { get; set; }

    byte[] payload = default!;
    byte[] jsonPayload = default!;
    readonly Nerdbank.MessagePack.MessagePackSerializer nerdbank = new();

    [PolyType.GenerateShapeFor<int>]
    partial class Witness;

    [System.Text.Json.Serialization.JsonSerializable(typeof(int))]
    partial class IntJsonContext : System.Text.Json.Serialization.JsonSerializerContext;

    [GlobalSetup]
    public void Setup()
    {
        payload = MessagePack.MessagePackSerializer.Serialize(Value);
        jsonPayload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(Value);

        if (!V3::MessagePack.MessagePackSerializer.Serialize(Value).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: v3 bytes vs v4");
        if (!nerdbank.Serialize<int, Witness>(Value).AsSpan().SequenceEqual(payload)) throw new InvalidOperationException("verify failed: nerdbank bytes vs v4");
        if (!System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(Value, IntJsonContext.Default.Int32).AsSpan().SequenceEqual(jsonPayload)) throw new InvalidOperationException("verify failed: STJ source-gen bytes vs reflection");
        if (MessagePack.MessagePackSerializer.Deserialize<int>(payload) != Value) throw new InvalidOperationException("verify failed: v4 roundtrip");
        if (V3::MessagePack.MessagePackSerializer.Deserialize<int>(payload) != Value) throw new InvalidOperationException("verify failed: v3 roundtrip");
        if (nerdbank.Deserialize<int, Witness>(payload) != Value) throw new InvalidOperationException("verify failed: nerdbank roundtrip");
        if (System.Text.Json.JsonSerializer.Deserialize<int>(jsonPayload) != Value) throw new InvalidOperationException("verify failed: STJ roundtrip");
        if (System.Text.Json.JsonSerializer.Deserialize(jsonPayload, IntJsonContext.Default.Int32) != Value) throw new InvalidOperationException("verify failed: STJ typeinfo roundtrip");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeV4() => MessagePack.MessagePackSerializer.Serialize(Value);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeMpcs() => V3::MessagePack.MessagePackSerializer.Serialize(Value);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeNerdbank() => nerdbank.Serialize<int, Witness>(Value);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeSystemTextJson() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(Value);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeSystemTextJsonTypeInfo() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(Value, IntJsonContext.Default.Int32);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public int DeserializeV4() => MessagePack.MessagePackSerializer.Deserialize<int>(payload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public int DeserializeMpcs() => V3::MessagePack.MessagePackSerializer.Deserialize<int>(payload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public int DeserializeNerdbank() => nerdbank.Deserialize<int, Witness>(payload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public int DeserializeSystemTextJson() => System.Text.Json.JsonSerializer.Deserialize<int>(jsonPayload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public int DeserializeSystemTextJsonTypeInfo() => System.Text.Json.JsonSerializer.Deserialize(jsonPayload, IntJsonContext.Default.Int32);
}
