extern alias V3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

// Two bare-value deserializations per op, same entry shape as Int32ValueBenchmark, to expose
// single-entry "last used converter" caches: Nerdbank's ConverterCache keeps the last
// (shape, converter) pair, so a same-type stream (int, int, ...) always hits it while an
// alternating stream (int, bool, int, bool, ...) misses on every call and falls to the
// dictionary lookup. IntInt / BoolBool are the all-hit baselines; with no such cache
// IntBool lands on (IntInt + BoolBool) / 2, and the excess over that midpoint is the cost
// of two misses.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public partial class AlternatingTypeBenchmark
{
    const int IntValue = 8;
    const bool BoolValue = true;

    byte[] intPayload = default!;
    byte[] boolPayload = default!;
    byte[] intJson = default!;
    byte[] boolJson = default!;
    readonly Nerdbank.MessagePack.MessagePackSerializer nerdbank = new();
    readonly PolyType.ITypeShape nbIntShape = PolyType.Abstractions.TypeShapeResolver.Resolve<int, Witness>();
    readonly PolyType.ITypeShape nbBoolShape = PolyType.Abstractions.TypeShapeResolver.Resolve<bool, Witness>();
    readonly PolyType.ITypeShapeProvider nbProvider = PolyType.Abstractions.TypeShapeResolver.Resolve<int, Witness>().Provider;
    // non-generic JsonTypeInfo: the static type hides T, so the call binds to the object-returning overload
    readonly System.Text.Json.Serialization.Metadata.JsonTypeInfo stjIntInfo = AltJsonContext.Default.Int32;
    readonly System.Text.Json.Serialization.Metadata.JsonTypeInfo stjBoolInfo = AltJsonContext.Default.Boolean;

    [PolyType.GenerateShapeFor<int>]
    [PolyType.GenerateShapeFor<bool>]
    partial class Witness;

    // System.Text.Json rows ride their own utf8 JSON payloads ("8" / "true"), as in Int32ValueBenchmark:
    // the generic entry resolves JsonTypeInfo from the default options per call (JsonSerializerOptions
    // also keeps a single last-used JsonTypeInfo), the TypeInfo rows hand the source-gen metadata in.
    [System.Text.Json.Serialization.JsonSerializable(typeof(int))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(bool))]
    partial class AltJsonContext : System.Text.Json.Serialization.JsonSerializerContext;

    [GlobalSetup]
    public void Setup()
    {
        intPayload = MessagePack.MessagePackSerializer.Serialize(IntValue);
        boolPayload = MessagePack.MessagePackSerializer.Serialize(BoolValue);
        intJson = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(IntValue);
        boolJson = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoolValue);

        if (!V3::MessagePack.MessagePackSerializer.Serialize(IntValue).AsSpan().SequenceEqual(intPayload)) throw new InvalidOperationException("verify failed: v3 int bytes vs v4");
        if (!V3::MessagePack.MessagePackSerializer.Serialize(BoolValue).AsSpan().SequenceEqual(boolPayload)) throw new InvalidOperationException("verify failed: v3 bool bytes vs v4");
        if (!nerdbank.Serialize<int, Witness>(IntValue).AsSpan().SequenceEqual(intPayload)) throw new InvalidOperationException("verify failed: nerdbank int bytes vs v4");
        if (!nerdbank.Serialize<bool, Witness>(BoolValue).AsSpan().SequenceEqual(boolPayload)) throw new InvalidOperationException("verify failed: nerdbank bool bytes vs v4");

        if (IntIntV4() != IntValue * 2 || BoolBoolV4() != 2 || IntBoolV4() != IntValue + 1) throw new InvalidOperationException("verify failed: v4 roundtrip");
        if (IntIntMpcs() != IntValue * 2 || BoolBoolMpcs() != 2 || IntBoolMpcs() != IntValue + 1) throw new InvalidOperationException("verify failed: v3 roundtrip");
        if (IntIntNerdbank() != IntValue * 2 || BoolBoolNerdbank() != 2 || IntBoolNerdbank() != IntValue + 1) throw new InvalidOperationException("verify failed: nerdbank roundtrip");
        if (IntIntStj() != IntValue * 2 || BoolBoolStj() != 2 || IntBoolStj() != IntValue + 1) throw new InvalidOperationException("verify failed: STJ roundtrip");
        if (IntIntStjTypeInfo() != IntValue * 2 || BoolBoolStjTypeInfo() != 2 || IntBoolStjTypeInfo() != IntValue + 1) throw new InvalidOperationException("verify failed: STJ typeinfo roundtrip");

        foreach (var (name, actual) in new (string, int)[]
        {
            ("v4", IntIntV4Ng() + BoolBoolV4Ng() + IntBoolV4Ng()), ("v3", IntIntMpcsNg() + BoolBoolMpcsNg() + IntBoolMpcsNg()),
            ("nerdbank", IntIntNerdbankNg() + BoolBoolNerdbankNg() + IntBoolNerdbankNg()), ("nerdbank from type", IntIntNerdbankNgFromType() + BoolBoolNerdbankNgFromType() + IntBoolNerdbankNgFromType()),
            ("STJ", IntIntStjNg() + BoolBoolStjNg() + IntBoolStjNg()), ("STJ typeinfo", IntIntStjTypeInfoNg() + BoolBoolStjTypeInfoNg() + IntBoolStjTypeInfoNg()),
        })
        {
            if (actual != IntValue * 3 + 3) throw new InvalidOperationException($"verify failed: {name} non-generic roundtrip");
        }

        // serialize rows: every pair writes 2 bytes (fixint + bool), and the bytes match the deserialize payloads
        foreach (var (name, actual) in new (string, int)[]
        {
            ("v4", SerIntIntV4() + SerBoolBoolV4() + SerIntBoolV4()), ("v3", SerIntIntMpcs() + SerBoolBoolMpcs() + SerIntBoolMpcs()),
            ("nerdbank", SerIntIntNerdbank() + SerBoolBoolNerdbank() + SerIntBoolNerdbank()),
            ("v4 ng", SerIntIntV4Ng() + SerBoolBoolV4Ng() + SerIntBoolV4Ng()), ("v3 ng", SerIntIntMpcsNg() + SerBoolBoolMpcsNg() + SerIntBoolMpcsNg()),
            ("nerdbank ng", SerIntIntNerdbankNg() + SerBoolBoolNerdbankNg() + SerIntBoolNerdbankNg()),
        })
        {
            if (actual != 6) throw new InvalidOperationException($"verify failed: {name} serialize lengths");
        }
        if (!MessagePack.MessagePackSerializer.Serialize(typeof(bool), BoxedBool).AsSpan().SequenceEqual(boolPayload) || !nerdbank.SerializeObject(BoxedInt, nbIntShape).AsSpan().SequenceEqual(intPayload)) throw new InvalidOperationException("verify failed: non-generic serialize bytes");
        // JSON: "8" is 1 byte, "true" is 4
        if (SerIntIntStj() != 2 || SerBoolBoolStj() != 8 || SerIntBoolStj() != 5 || SerIntBoolStjTypeInfo() != 5 || SerIntBoolStjNg() != 5 || SerIntBoolStjTypeInfoNg() != 5) throw new InvalidOperationException("verify failed: STJ serialize lengths");
    }

    [BenchmarkCategory("V4"), Benchmark(Baseline = true)]
    public int IntIntV4() => MessagePack.MessagePackSerializer.Deserialize<int>(intPayload) + MessagePack.MessagePackSerializer.Deserialize<int>(intPayload);

    [BenchmarkCategory("V4"), Benchmark]
    public int BoolBoolV4() => (MessagePack.MessagePackSerializer.Deserialize<bool>(boolPayload) ? 1 : 0) + (MessagePack.MessagePackSerializer.Deserialize<bool>(boolPayload) ? 1 : 0);

    [BenchmarkCategory("V4"), Benchmark]
    public int IntBoolV4() => MessagePack.MessagePackSerializer.Deserialize<int>(intPayload) + (MessagePack.MessagePackSerializer.Deserialize<bool>(boolPayload) ? 1 : 0);

    [BenchmarkCategory("Mpcs"), Benchmark(Baseline = true)]
    public int IntIntMpcs() => V3::MessagePack.MessagePackSerializer.Deserialize<int>(intPayload) + V3::MessagePack.MessagePackSerializer.Deserialize<int>(intPayload);

    [BenchmarkCategory("Mpcs"), Benchmark]
    public int BoolBoolMpcs() => (V3::MessagePack.MessagePackSerializer.Deserialize<bool>(boolPayload) ? 1 : 0) + (V3::MessagePack.MessagePackSerializer.Deserialize<bool>(boolPayload) ? 1 : 0);

    [BenchmarkCategory("Mpcs"), Benchmark]
    public int IntBoolMpcs() => V3::MessagePack.MessagePackSerializer.Deserialize<int>(intPayload) + (V3::MessagePack.MessagePackSerializer.Deserialize<bool>(boolPayload) ? 1 : 0);

    [BenchmarkCategory("Nerdbank"), Benchmark(Baseline = true)]
    public int IntIntNerdbank() => nerdbank.Deserialize<int, Witness>(intPayload) + nerdbank.Deserialize<int, Witness>(intPayload);

    [BenchmarkCategory("Nerdbank"), Benchmark]
    public int BoolBoolNerdbank() => (nerdbank.Deserialize<bool, Witness>(boolPayload) ? 1 : 0) + (nerdbank.Deserialize<bool, Witness>(boolPayload) ? 1 : 0);

    [BenchmarkCategory("Nerdbank"), Benchmark]
    public int IntBoolNerdbank() => nerdbank.Deserialize<int, Witness>(intPayload) + (nerdbank.Deserialize<bool, Witness>(boolPayload) ? 1 : 0);

    [BenchmarkCategory("Stj"), Benchmark(Baseline = true)]
    public int IntIntStj() => System.Text.Json.JsonSerializer.Deserialize<int>(intJson) + System.Text.Json.JsonSerializer.Deserialize<int>(intJson);

    [BenchmarkCategory("Stj"), Benchmark]
    public int BoolBoolStj() => (System.Text.Json.JsonSerializer.Deserialize<bool>(boolJson) ? 1 : 0) + (System.Text.Json.JsonSerializer.Deserialize<bool>(boolJson) ? 1 : 0);

    [BenchmarkCategory("Stj"), Benchmark]
    public int IntBoolStj() => System.Text.Json.JsonSerializer.Deserialize<int>(intJson) + (System.Text.Json.JsonSerializer.Deserialize<bool>(boolJson) ? 1 : 0);

    [BenchmarkCategory("StjTypeInfo"), Benchmark(Baseline = true)]
    public int IntIntStjTypeInfo() => System.Text.Json.JsonSerializer.Deserialize(intJson, AltJsonContext.Default.Int32) + System.Text.Json.JsonSerializer.Deserialize(intJson, AltJsonContext.Default.Int32);

    [BenchmarkCategory("StjTypeInfo"), Benchmark]
    public int BoolBoolStjTypeInfo() => (System.Text.Json.JsonSerializer.Deserialize(boolJson, AltJsonContext.Default.Boolean) ? 1 : 0) + (System.Text.Json.JsonSerializer.Deserialize(boolJson, AltJsonContext.Default.Boolean) ? 1 : 0);

    [BenchmarkCategory("StjTypeInfo"), Benchmark]
    public int IntBoolStjTypeInfo() => System.Text.Json.JsonSerializer.Deserialize(intJson, AltJsonContext.Default.Int32) + (System.Text.Json.JsonSerializer.Deserialize(boolJson, AltJsonContext.Default.Boolean) ? 1 : 0);

    // ---- non-generic twins: the type arrives as a runtime value and the result comes back boxed.
    // v4 / v3 / STJ take a System.Type. Nerdbank's non-generic entry takes an ITypeShape instead, so it gets
    // two shapes: the shape held in a field (its natural form) and the shape resolved from a Type per call
    // through the provider, which is what a caller holding only a Type has to do.

    static int B(object? o) => (bool)o! ? 1 : 0;

    [BenchmarkCategory("V4Ng"), Benchmark(Baseline = true)]
    public int IntIntV4Ng() => (int)MessagePack.MessagePackSerializer.Deserialize(typeof(int), intPayload)! + (int)MessagePack.MessagePackSerializer.Deserialize(typeof(int), intPayload)!;

    [BenchmarkCategory("V4Ng"), Benchmark]
    public int BoolBoolV4Ng() => B(MessagePack.MessagePackSerializer.Deserialize(typeof(bool), boolPayload)) + B(MessagePack.MessagePackSerializer.Deserialize(typeof(bool), boolPayload));

    [BenchmarkCategory("V4Ng"), Benchmark]
    public int IntBoolV4Ng() => (int)MessagePack.MessagePackSerializer.Deserialize(typeof(int), intPayload)! + B(MessagePack.MessagePackSerializer.Deserialize(typeof(bool), boolPayload));

    [BenchmarkCategory("MpcsNg"), Benchmark(Baseline = true)]
    public int IntIntMpcsNg() => (int)V3::MessagePack.MessagePackSerializer.Deserialize(typeof(int), intPayload)! + (int)V3::MessagePack.MessagePackSerializer.Deserialize(typeof(int), intPayload)!;

    [BenchmarkCategory("MpcsNg"), Benchmark]
    public int BoolBoolMpcsNg() => B(V3::MessagePack.MessagePackSerializer.Deserialize(typeof(bool), boolPayload)) + B(V3::MessagePack.MessagePackSerializer.Deserialize(typeof(bool), boolPayload));

    [BenchmarkCategory("MpcsNg"), Benchmark]
    public int IntBoolMpcsNg() => (int)V3::MessagePack.MessagePackSerializer.Deserialize(typeof(int), intPayload)! + B(V3::MessagePack.MessagePackSerializer.Deserialize(typeof(bool), boolPayload));

    [BenchmarkCategory("NerdbankNg"), Benchmark(Baseline = true)]
    public int IntIntNerdbankNg() => (int)nerdbank.DeserializeObject(intPayload, nbIntShape)! + (int)nerdbank.DeserializeObject(intPayload, nbIntShape)!;

    [BenchmarkCategory("NerdbankNg"), Benchmark]
    public int BoolBoolNerdbankNg() => B(nerdbank.DeserializeObject(boolPayload, nbBoolShape)) + B(nerdbank.DeserializeObject(boolPayload, nbBoolShape));

    [BenchmarkCategory("NerdbankNg"), Benchmark]
    public int IntBoolNerdbankNg() => (int)nerdbank.DeserializeObject(intPayload, nbIntShape)! + B(nerdbank.DeserializeObject(boolPayload, nbBoolShape));

    [BenchmarkCategory("NerdbankNgFromType"), Benchmark(Baseline = true)]
    public int IntIntNerdbankNgFromType() => (int)nerdbank.DeserializeObject(intPayload, nbProvider.GetTypeShape(typeof(int))!)! + (int)nerdbank.DeserializeObject(intPayload, nbProvider.GetTypeShape(typeof(int))!)!;

    [BenchmarkCategory("NerdbankNgFromType"), Benchmark]
    public int BoolBoolNerdbankNgFromType() => B(nerdbank.DeserializeObject(boolPayload, nbProvider.GetTypeShape(typeof(bool))!)) + B(nerdbank.DeserializeObject(boolPayload, nbProvider.GetTypeShape(typeof(bool))!));

    [BenchmarkCategory("NerdbankNgFromType"), Benchmark]
    public int IntBoolNerdbankNgFromType() => (int)nerdbank.DeserializeObject(intPayload, nbProvider.GetTypeShape(typeof(int))!)! + B(nerdbank.DeserializeObject(boolPayload, nbProvider.GetTypeShape(typeof(bool))!));

    [BenchmarkCategory("StjNg"), Benchmark(Baseline = true)]
    public int IntIntStjNg() => (int)System.Text.Json.JsonSerializer.Deserialize(intJson, typeof(int))! + (int)System.Text.Json.JsonSerializer.Deserialize(intJson, typeof(int))!;

    [BenchmarkCategory("StjNg"), Benchmark]
    public int BoolBoolStjNg() => B(System.Text.Json.JsonSerializer.Deserialize(boolJson, typeof(bool))) + B(System.Text.Json.JsonSerializer.Deserialize(boolJson, typeof(bool)));

    [BenchmarkCategory("StjNg"), Benchmark]
    public int IntBoolStjNg() => (int)System.Text.Json.JsonSerializer.Deserialize(intJson, typeof(int))! + B(System.Text.Json.JsonSerializer.Deserialize(boolJson, typeof(bool)));

    [BenchmarkCategory("StjTypeInfoNg"), Benchmark(Baseline = true)]
    public int IntIntStjTypeInfoNg() => (int)System.Text.Json.JsonSerializer.Deserialize(intJson, stjIntInfo)! + (int)System.Text.Json.JsonSerializer.Deserialize(intJson, stjIntInfo)!;

    [BenchmarkCategory("StjTypeInfoNg"), Benchmark]
    public int BoolBoolStjTypeInfoNg() => B(System.Text.Json.JsonSerializer.Deserialize(boolJson, stjBoolInfo)) + B(System.Text.Json.JsonSerializer.Deserialize(boolJson, stjBoolInfo));

    [BenchmarkCategory("StjTypeInfoNg"), Benchmark]
    public int IntBoolStjTypeInfoNg() => (int)System.Text.Json.JsonSerializer.Deserialize(intJson, stjIntInfo)! + B(System.Text.Json.JsonSerializer.Deserialize(boolJson, stjBoolInfo));

    // the read-buffer non-generic entry: NonGenericEntry.Deserialize<TReadBuffer> is a generic virtual method
    [BenchmarkCategory("V4NgBuffer"), Benchmark(Baseline = true)]
    public int IntIntV4NgBuffer()
    {
        var a = new SerializerFoundation.ReadOnlySpanReadBuffer(intPayload);
        var b = new SerializerFoundation.ReadOnlySpanReadBuffer(intPayload);
        return (int)MessagePack.MessagePackSerializer.Deserialize(typeof(int), ref a)! + (int)MessagePack.MessagePackSerializer.Deserialize(typeof(int), ref b)!;
    }

    [BenchmarkCategory("V4NgBuffer"), Benchmark]
    public int IntBoolV4NgBuffer()
    {
        var a = new SerializerFoundation.ReadOnlySpanReadBuffer(intPayload);
        var b = new SerializerFoundation.ReadOnlySpanReadBuffer(boolPayload);
        return (int)MessagePack.MessagePackSerializer.Deserialize(typeof(int), ref a)! + B(MessagePack.MessagePackSerializer.Deserialize(typeof(bool), ref b));
    }
}
