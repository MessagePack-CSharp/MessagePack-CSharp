extern alias V3;
using BenchmarkDotNet.Attributes;

// The serialize side of AlternatingTypeBenchmark: two bare-value serializations per op, each producing its own
// byte[] (the result array is part of every library's entry cost here, 1 byte of payload each), the same-type pairs as
// the all-hit baselines and the int/bool pair as the alternating stream. The row result is the summed length so the
// arrays are consumed. Categories end in "Ser" so the serialize rows group apart from the deserialize rows; the
// method names start with "Ser" so `--filter "AlternatingTypeBenchmark.Ser*"` runs this side alone.
//
// MEASURED 2026-10-17 (i7-13700KF, .NET 11 RC1, 10 warmup / 20 iterations, ns/op; every row allocates its two 32 B
// result arrays, so the result array dominates and the generic gap to Nerdbank is only ~10%):
//   generic:     V4 22.1 / 21.8 / 22.7 (int,int / bool,bool / int,bool) | Nerdbank 1.4.82 24.6 / 24.3 / 26.2
//                | mpcs 30.5 / 31.0 / 31.0 | STJ 52.8 / 53.5 / 60.6 | STJ TypeInfo 52.8 / 49.8 / 51.7
//   non-generic: V4 24.6 / 24.3 / 30.2 | mpcs 37.9 / 37.6 / 36.4 | Nerdbank 52.9 / 51.8 / 54.4
//                | STJ 55.0 / 53.5 / 60.8 | STJ TypeInfo 52.4 / 50.0 / 57.6
// V4's non-generic int/bool row pays 5.7 ns (23%) over its same-type rows while its deserialize twin pays nothing.
// Not a cache in the library (Serialize(Type, object) is a TypeKeyHashTable lookup and a virtual NonGenericEntry call,
// no last-type state): JIT dumps (--jit-alt=SerIntBoolV4Ng) show tiered PGO's guarded devirtualization guessing
// NonGenericEntry<bool> at BOTH inlined call sites (`call Serialize[bool]` x2, no Serialize[int]). The virtual call
// sits inside the inlined Serialize(Type, object, options), whose class profile is keyed by the inlinee's IL offset and
// therefore aggregated across the two call sites (50/50 int/bool), so one type wins the guess and the other site
// fails its guard on every call: a virtual call plus the lost inlining of Serialize<int>. The deserialize twin
// guesses int at both sites and the failing bool site costs nothing measurable (its body returns a shared box).
// DOTNET_TieredPGO=0 removes the asymmetry (43.7 / 45.4 / 42.5) and slows every row ~1.8x, so PGO stays on; the
// penalty is a property of a call site that alternates between exactly two Type arguments, not of the library.
//
// IBufferWriter entry (reused ArrayBufferWriter, no result array; same day, same job): V4 19.3 / 18.9 / 18.6
// | Nerdbank 19.3 / 18.1 / 20.9 | STJ (reused Utf8JsonWriter + JsonTypeInfo) 25.4 / 22.9 / 24.8 | mpcs 28.7 / 28.9 / 28.6.
// Against the byte[] rows that is only 2.8 ns less for two calls, so the 32 B result array costs ~1.4 ns and the
// ~9.5 ns per call is the entry itself: Serialize<int>'s Tier1 body (1364 B) keeps an interface call to the formatter
// (`call [r11] IMessagePackFormatter...:Serialize`, not devirtualized although the site is monomorphic), the write
// buffer's ctor / GetSpan / Advance / Dispose, and on the byte[] path AllocateUninitializedArray + Memmove in ToArray.
// The deserialize entry has none of the buffer setup or copy and lands at 3 ns per call.
//
// Buffer-level entry (SpanWriteBuffer over stackalloc byte[5], same job): 3.3 ns for the two writes, and the formatter
// called directly (no entry) 2.2 ns. So the entry logic itself is ~0.6 ns per call and the ~8 ns per call of the byte[]
// and IBufferWriter rows is the write-buffer implementations (ArrayPoolListWriteBuffer's 1 KB scratch setup, ToArray
// and Dispose; BufferWriterWriteBuffer's GetSpan / Advance interface calls and Flush), which SpanWriteBuffer does not
// have. For tiny messages the buffer-level entry is 5-8x faster than the whole-message entries.
//
// SerializerFoundation 1.2.0 (lighter ArrayPoolListWriteBuffer): byte[] int,int 24.9 -> 12.6 ns, non-generic
// int,int 30.2 -> 16.0 ns; IBufferWriter 18.6 and SpanWriteBuffer 3.3 unchanged (different buffer types).
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

    // ---- IBufferWriter<byte> entry: no result array, the writer is reused across calls, so this is the entry's own
    // fixed cost (formatter fetch, buffer setup, the write) without the allocation that dominates the byte[] rows ----

    readonly System.Buffers.ArrayBufferWriter<byte> bw = new(256);
    readonly PolyType.ITypeShape<int> nbIntShapeT = PolyType.Abstractions.TypeShapeResolver.Resolve<int, Witness>();
    readonly PolyType.ITypeShape<bool> nbBoolShapeT = PolyType.Abstractions.TypeShapeResolver.Resolve<bool, Witness>();
    // System.Text.Json's no-allocation shape: a Utf8JsonWriter reset onto the same ArrayBufferWriter per call
    readonly System.Text.Json.Utf8JsonWriter stjWriter;

    public AlternatingTypeBenchmark()
    {
        stjWriter = new System.Text.Json.Utf8JsonWriter(bw);
    }

    [BenchmarkCategory("V4BwSer"), Benchmark(Baseline = true)]
    public int SerIntIntV4Bw() { bw.ResetWrittenCount(); MessagePack.MessagePackSerializer.Serialize(bw, IntValue); MessagePack.MessagePackSerializer.Serialize(bw, IntValue); return bw.WrittenCount; }

    [BenchmarkCategory("V4BwSer"), Benchmark]
    public int SerBoolBoolV4Bw() { bw.ResetWrittenCount(); MessagePack.MessagePackSerializer.Serialize(bw, BoolValue); MessagePack.MessagePackSerializer.Serialize(bw, BoolValue); return bw.WrittenCount; }

    [BenchmarkCategory("V4BwSer"), Benchmark]
    public int SerIntBoolV4Bw() { bw.ResetWrittenCount(); MessagePack.MessagePackSerializer.Serialize(bw, IntValue); MessagePack.MessagePackSerializer.Serialize(bw, BoolValue); return bw.WrittenCount; }

    [BenchmarkCategory("MpcsBwSer"), Benchmark(Baseline = true)]
    public int SerIntIntMpcsBw() { bw.ResetWrittenCount(); V3::MessagePack.MessagePackSerializer.Serialize(bw, IntValue); V3::MessagePack.MessagePackSerializer.Serialize(bw, IntValue); return bw.WrittenCount; }

    [BenchmarkCategory("MpcsBwSer"), Benchmark]
    public int SerBoolBoolMpcsBw() { bw.ResetWrittenCount(); V3::MessagePack.MessagePackSerializer.Serialize(bw, BoolValue); V3::MessagePack.MessagePackSerializer.Serialize(bw, BoolValue); return bw.WrittenCount; }

    [BenchmarkCategory("MpcsBwSer"), Benchmark]
    public int SerIntBoolMpcsBw() { bw.ResetWrittenCount(); V3::MessagePack.MessagePackSerializer.Serialize(bw, IntValue); V3::MessagePack.MessagePackSerializer.Serialize(bw, BoolValue); return bw.WrittenCount; }

    [BenchmarkCategory("NerdbankBwSer"), Benchmark(Baseline = true)]
    public int SerIntIntNerdbankBw() { bw.ResetWrittenCount(); var i = IntValue; nerdbank.Serialize(bw, in i, nbIntShapeT); nerdbank.Serialize(bw, in i, nbIntShapeT); return bw.WrittenCount; }

    [BenchmarkCategory("NerdbankBwSer"), Benchmark]
    public int SerBoolBoolNerdbankBw() { bw.ResetWrittenCount(); var b = BoolValue; nerdbank.Serialize(bw, in b, nbBoolShapeT); nerdbank.Serialize(bw, in b, nbBoolShapeT); return bw.WrittenCount; }

    [BenchmarkCategory("NerdbankBwSer"), Benchmark]
    public int SerIntBoolNerdbankBw() { bw.ResetWrittenCount(); var i = IntValue; var b = BoolValue; nerdbank.Serialize(bw, in i, nbIntShapeT); nerdbank.Serialize(bw, in b, nbBoolShapeT); return bw.WrittenCount; }

    [BenchmarkCategory("StjBwSer"), Benchmark(Baseline = true)]
    public int SerIntIntStjBw() { bw.ResetWrittenCount(); stjWriter.Reset(bw); System.Text.Json.JsonSerializer.Serialize(stjWriter, IntValue, AltJsonContext.Default.Int32); stjWriter.Reset(bw); System.Text.Json.JsonSerializer.Serialize(stjWriter, IntValue, AltJsonContext.Default.Int32); return bw.WrittenCount; }

    [BenchmarkCategory("StjBwSer"), Benchmark]
    public int SerBoolBoolStjBw() { bw.ResetWrittenCount(); stjWriter.Reset(bw); System.Text.Json.JsonSerializer.Serialize(stjWriter, BoolValue, AltJsonContext.Default.Boolean); stjWriter.Reset(bw); System.Text.Json.JsonSerializer.Serialize(stjWriter, BoolValue, AltJsonContext.Default.Boolean); return bw.WrittenCount; }

    [BenchmarkCategory("StjBwSer"), Benchmark]
    public int SerIntBoolStjBw() { bw.ResetWrittenCount(); stjWriter.Reset(bw); System.Text.Json.JsonSerializer.Serialize(stjWriter, IntValue, AltJsonContext.Default.Int32); stjWriter.Reset(bw); System.Text.Json.JsonSerializer.Serialize(stjWriter, BoolValue, AltJsonContext.Default.Boolean); return bw.WrittenCount; }

    // ---- the buffer-level entry: a SpanWriteBuffer over stackalloc byte[5] per call (an int is at most 5 bytes), so no
    // pool, no copy, no result array; and the floor below the entry, the formatter fetched once and called directly ----

    readonly MessagePack.IMessagePackFormatter<SerializerFoundation.SpanWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, int> spanIntFormatter =
        MessagePack.MessagePackSerializerOptions.Default.Resolver.GetFormatter<SerializerFoundation.SpanWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, int>();

    [BenchmarkCategory("V4SpanSer"), Benchmark(Baseline = true)]
    public long SerIntIntV4Span()
    {
        var a = new SerializerFoundation.SpanWriteBuffer(stackalloc byte[5]);
        MessagePack.MessagePackSerializer.Serialize(ref a, IntValue);
        var b = new SerializerFoundation.SpanWriteBuffer(stackalloc byte[5]);
        MessagePack.MessagePackSerializer.Serialize(ref b, IntValue);
        return a.BytesWritten + b.BytesWritten;
    }

    [BenchmarkCategory("V4SpanSer"), Benchmark]
    public long SerIntIntV4SpanFormatter()
    {
        var state = new MessagePack.SerializeState(500);
        var a = new SerializerFoundation.SpanWriteBuffer(stackalloc byte[5]);
        spanIntFormatter.Serialize(ref a, ref state, IntValue);
        var b = new SerializerFoundation.SpanWriteBuffer(stackalloc byte[5]);
        spanIntFormatter.Serialize(ref b, ref state, IntValue);
        return a.BytesWritten + b.BytesWritten;
    }
}
