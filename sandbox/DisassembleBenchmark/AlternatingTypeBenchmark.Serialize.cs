extern alias V3;
using BenchmarkDotNet.Attributes;

// The serialize side of AlternatingTypeBenchmark: two bare-value serializations per op, each producing its own
// byte[] (the result array is part of every library's entry cost here, 1 byte of payload each), the same-type pairs as
// the all-hit baselines and the int/bool pair as the alternating stream. The row result is the summed length so the
// arrays are consumed. Categories end in "Ser" so the serialize rows group apart from the deserialize rows; the
// method names start with "Ser" so `--filter "AlternatingTypeBenchmark.Ser*"` runs this side alone.
public partial class AlternatingTypeBenchmark
{
    [BenchmarkCategory("V4Ser"), Benchmark(Baseline = true)]
    public int SerIntIntV4() => MessagePack.MessagePackSerializer.Serialize(IntValue).Length + MessagePack.MessagePackSerializer.Serialize(IntValue).Length;

    [BenchmarkCategory("V4Ser"), Benchmark]
    public int SerBoolBoolV4() => MessagePack.MessagePackSerializer.Serialize(BoolValue).Length + MessagePack.MessagePackSerializer.Serialize(BoolValue).Length;

    [BenchmarkCategory("V4Ser"), Benchmark]
    public int SerIntBoolV4() => MessagePack.MessagePackSerializer.Serialize(IntValue).Length + MessagePack.MessagePackSerializer.Serialize(BoolValue).Length;

    [BenchmarkCategory("MpcsSer"), Benchmark(Baseline = true)]
    public int SerIntIntMpcs() => V3::MessagePack.MessagePackSerializer.Serialize(IntValue).Length + V3::MessagePack.MessagePackSerializer.Serialize(IntValue).Length;

    [BenchmarkCategory("MpcsSer"), Benchmark]
    public int SerBoolBoolMpcs() => V3::MessagePack.MessagePackSerializer.Serialize(BoolValue).Length + V3::MessagePack.MessagePackSerializer.Serialize(BoolValue).Length;

    [BenchmarkCategory("MpcsSer"), Benchmark]
    public int SerIntBoolMpcs() => V3::MessagePack.MessagePackSerializer.Serialize(IntValue).Length + V3::MessagePack.MessagePackSerializer.Serialize(BoolValue).Length;

    [BenchmarkCategory("NerdbankSer"), Benchmark(Baseline = true)]
    public int SerIntIntNerdbank() => nerdbank.Serialize<int, Witness>(IntValue).Length + nerdbank.Serialize<int, Witness>(IntValue).Length;

    [BenchmarkCategory("NerdbankSer"), Benchmark]
    public int SerBoolBoolNerdbank() => nerdbank.Serialize<bool, Witness>(BoolValue).Length + nerdbank.Serialize<bool, Witness>(BoolValue).Length;

    [BenchmarkCategory("NerdbankSer"), Benchmark]
    public int SerIntBoolNerdbank() => nerdbank.Serialize<int, Witness>(IntValue).Length + nerdbank.Serialize<bool, Witness>(BoolValue).Length;

    [BenchmarkCategory("StjSer"), Benchmark(Baseline = true)]
    public int SerIntIntStj() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(IntValue).Length + System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(IntValue).Length;

    [BenchmarkCategory("StjSer"), Benchmark]
    public int SerBoolBoolStj() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoolValue).Length + System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoolValue).Length;

    [BenchmarkCategory("StjSer"), Benchmark]
    public int SerIntBoolStj() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(IntValue).Length + System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoolValue).Length;

    [BenchmarkCategory("StjTypeInfoSer"), Benchmark(Baseline = true)]
    public int SerIntIntStjTypeInfo() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(IntValue, AltJsonContext.Default.Int32).Length + System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(IntValue, AltJsonContext.Default.Int32).Length;

    [BenchmarkCategory("StjTypeInfoSer"), Benchmark]
    public int SerBoolBoolStjTypeInfo() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoolValue, AltJsonContext.Default.Boolean).Length + System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoolValue, AltJsonContext.Default.Boolean).Length;

    [BenchmarkCategory("StjTypeInfoSer"), Benchmark]
    public int SerIntBoolStjTypeInfo() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(IntValue, AltJsonContext.Default.Int32).Length + System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoolValue, AltJsonContext.Default.Boolean).Length;

    // ---- non-generic: the value arrives boxed with its Type (v4 / v3 / STJ) or its ITypeShape (Nerdbank) ----

    static readonly object BoxedInt = IntValue;
    static readonly object BoxedBool = BoolValue;

    [BenchmarkCategory("V4NgSer"), Benchmark(Baseline = true)]
    public int SerIntIntV4Ng() => MessagePack.MessagePackSerializer.Serialize(typeof(int), BoxedInt).Length + MessagePack.MessagePackSerializer.Serialize(typeof(int), BoxedInt).Length;

    [BenchmarkCategory("V4NgSer"), Benchmark]
    public int SerBoolBoolV4Ng() => MessagePack.MessagePackSerializer.Serialize(typeof(bool), BoxedBool).Length + MessagePack.MessagePackSerializer.Serialize(typeof(bool), BoxedBool).Length;

    [BenchmarkCategory("V4NgSer"), Benchmark]
    public int SerIntBoolV4Ng() => MessagePack.MessagePackSerializer.Serialize(typeof(int), BoxedInt).Length + MessagePack.MessagePackSerializer.Serialize(typeof(bool), BoxedBool).Length;

    [BenchmarkCategory("MpcsNgSer"), Benchmark(Baseline = true)]
    public int SerIntIntMpcsNg() => V3::MessagePack.MessagePackSerializer.Serialize(typeof(int), BoxedInt).Length + V3::MessagePack.MessagePackSerializer.Serialize(typeof(int), BoxedInt).Length;

    [BenchmarkCategory("MpcsNgSer"), Benchmark]
    public int SerBoolBoolMpcsNg() => V3::MessagePack.MessagePackSerializer.Serialize(typeof(bool), BoxedBool).Length + V3::MessagePack.MessagePackSerializer.Serialize(typeof(bool), BoxedBool).Length;

    [BenchmarkCategory("MpcsNgSer"), Benchmark]
    public int SerIntBoolMpcsNg() => V3::MessagePack.MessagePackSerializer.Serialize(typeof(int), BoxedInt).Length + V3::MessagePack.MessagePackSerializer.Serialize(typeof(bool), BoxedBool).Length;

    [BenchmarkCategory("NerdbankNgSer"), Benchmark(Baseline = true)]
    public int SerIntIntNerdbankNg() => nerdbank.SerializeObject(BoxedInt, nbIntShape).Length + nerdbank.SerializeObject(BoxedInt, nbIntShape).Length;

    [BenchmarkCategory("NerdbankNgSer"), Benchmark]
    public int SerBoolBoolNerdbankNg() => nerdbank.SerializeObject(BoxedBool, nbBoolShape).Length + nerdbank.SerializeObject(BoxedBool, nbBoolShape).Length;

    [BenchmarkCategory("NerdbankNgSer"), Benchmark]
    public int SerIntBoolNerdbankNg() => nerdbank.SerializeObject(BoxedInt, nbIntShape).Length + nerdbank.SerializeObject(BoxedBool, nbBoolShape).Length;

    [BenchmarkCategory("StjNgSer"), Benchmark(Baseline = true)]
    public int SerIntIntStjNg() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoxedInt, typeof(int)).Length + System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoxedInt, typeof(int)).Length;

    [BenchmarkCategory("StjNgSer"), Benchmark]
    public int SerBoolBoolStjNg() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoxedBool, typeof(bool)).Length + System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoxedBool, typeof(bool)).Length;

    [BenchmarkCategory("StjNgSer"), Benchmark]
    public int SerIntBoolStjNg() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoxedInt, typeof(int)).Length + System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoxedBool, typeof(bool)).Length;

    [BenchmarkCategory("StjTypeInfoNgSer"), Benchmark(Baseline = true)]
    public int SerIntIntStjTypeInfoNg() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoxedInt, stjIntInfo).Length + System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoxedInt, stjIntInfo).Length;

    [BenchmarkCategory("StjTypeInfoNgSer"), Benchmark]
    public int SerBoolBoolStjTypeInfoNg() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoxedBool, stjBoolInfo).Length + System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoxedBool, stjBoolInfo).Length;

    [BenchmarkCategory("StjTypeInfoNgSer"), Benchmark]
    public int SerIntBoolStjTypeInfoNg() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoxedInt, stjIntInfo).Length + System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(BoxedBool, stjBoolInfo).Length;
}
