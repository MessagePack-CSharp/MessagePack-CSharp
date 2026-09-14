extern alias V3;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using MessagePack;

// The MessageProcessor compression tiers side by side on the byte[] entry: none, LZ4 Block (ext 99),
// LZ4 BlockArray (ext 98), Zstandard (ext 96) at levels 1 / 3 (zstd default) / 9. Two payloads:
//   Answer = the realistic nested poco from AnswerBenchmark (1658 B raw, string-heavy, short)
//   Ints   = int[100_000] with a period-100 pattern (~300KB raw, highly compressible, the shape
//            where compression pays; matches Lz4Tests/ZstandardTests' BigCompressible)
// Setup prints the wire size of every tier and verifies each roundtrip (Answer through the v3
// oracle re-serialize, Ints by sequence equality).
//
// MEASURED (i7-13700KF, ShortRun; wire sizes: Answer none 1658 / LZ4 Block 1092 / LZ4 BlockArray
// 1106 / Zstd1 922 / Zstd3 924 / Zstd9 905 B; Ints none 100005 / LZ4 Block 516 / LZ4 BlockArray 645
// / Zstd 1,3,9 all 135 B):
//   Answer serialize (ns):   none 431 | LZ4 Block 1497 | LZ4 BlockArray 1557 | Zstd1 5100 | Zstd3 6298 | Zstd9 13528
//   Answer deserialize (ns): none 861 | LZ4 Block 1085 | LZ4 BlockArray 1205 | Zstd1 2968 | Zstd3 3363 | Zstd9 2982
//   Ints serialize (us):     none 35.3 | LZ4 Block 12.6 | LZ4 BlockArray 12.4 | Zstd1 16.2 | Zstd3 19.9 | Zstd9 195
//   Ints deserialize (us):   none 88.0 | LZ4 Block 98.9 | LZ4 BlockArray 97.3 | Zstd1 98.5 | Zstd3 99.0 | Zstd9 99.5
// The Zstd rows are after the processor started caching its encoder/decoder contexts (first run,
// one-shot Zstandard.Compress/Decompress creating a context per call: Answer 6660/4371 at level 1);
// what remains is zstd's own per-frame cost, ~3x LZ4 on a 1.6KB message for a 15% smaller wire.
// On the compressible 300KB array both codecs beat raw serialization (the byte[] entry's final
// copy shrinks with the payload), zstd's 135 B is the periodic pattern folded into one match, and
// deserialize is dominated by the 400KB of int[] allocation on every row. Level 9 costs 10x the
// encode time of level 1 for 2% on Answer and nothing on Ints; 1 (or the default 3) is the choice.
//
// net11.0 twin (in-box System.IO.Compression.Zstandard codec instead of NativeCompressions; a
// scratch BenchmarkDotNet project on the InProcess toolchain since 0.15.8 does not know the net11
// moniker; string[60] ~3.2KB and the same Ints, ns): Strings serialize none 527 | LZ4 1502 | Zstd1
// 5774 | Zstd3 5688; deserialize none 940 | LZ4 1264 | Zstd1 3598 | Zstd3 3606. Ints serialize (us)
// none 33.3 | LZ4 13.0 | Zstd1 13.4 | Zstd3 13.3; deserialize all ~100-107. Same wire sizes byte
// for byte (135 B ints), the same ~3-4x LZ4 gap on the small message, and on the 100KB array the
// BCL codec runs level 3 at level-1 speed where NativeCompressions 0.6.1 paid 16.2 -> 19.9 us,
// so the in-box build is the better of the two libzstd bindings on this machine.
//
// In-place encode (2026-09-14: the Zstandard processors compress straight into output.GetSpan behind a
// fixed 11-byte ext32 header, streaming the message segments into the encoder, instead of flatten +
// rented frame + copy): net10 ShortRun within noise, Answer serialize 5292 -> 5223 (L1) / 6448 ->
// 6345 (L3), Ints 14402 -> 14829 (L1) / 15522 -> 15488 (L3), wire +2..3 B for the ext32 width. An
// intermediate build that still flattened multi-segment messages on this tier measured the same
// (5188 / 6360 / 14075 / 14197), so the copies were never the cost: zstd's own per-frame work is.
// On this tier the encoder is the InteropZstandardEncoder stand-in over ZstandardNativeMethods
// (NativeCompressions 0.6.1 has no SetSourceLength); the net11 BCL codec runs the same code.
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CompressionProcessorBenchmark
{
    static readonly MessagePackSerializerOptions none = MessagePackSerializerOptions.Default;
    static readonly MessagePackSerializerOptions lz4Block = MessagePackSerializerOptions.Default.WithLz4Block();
    static readonly MessagePackSerializerOptions lz4BlockArray = MessagePackSerializerOptions.Default.WithLz4BlockArray();
    static readonly MessagePackSerializerOptions zstd1 = MessagePackSerializerOptions.Default.WithZstandardEnvelope(1);
    static readonly MessagePackSerializerOptions zstd3 = MessagePackSerializerOptions.Default.WithZstandardEnvelope(3);
    static readonly MessagePackSerializerOptions zstd9 = MessagePackSerializerOptions.Default.WithZstandardEnvelope(9);

    Answer answer = default!;
    int[] ints = default!;
    byte[] answerOracle = default!;
    byte[] answerNone = default!, answerLz4Block = default!, answerLz4BlockArray = default!, answerZstd1 = default!, answerZstd3 = default!, answerZstd9 = default!;
    byte[] intsNone = default!, intsLz4Block = default!, intsLz4BlockArray = default!, intsZstd1 = default!, intsZstd3 = default!, intsZstd9 = default!;

    [GlobalSetup]
    public void Setup()
    {
        answer = AnswerBenchmark.CreateAnswer();
        answerOracle = V3::MessagePack.MessagePackSerializer.Serialize(answer);
        ints = new int[100_000];
        for (var i = 0; i < ints.Length; i++) ints[i] = i % 100;

        answerNone = Check("Answer none", SerializeAnswerNone, none);
        answerLz4Block = Check("Answer LZ4 Block", SerializeAnswerLz4Block, lz4Block);
        answerLz4BlockArray = Check("Answer LZ4 BlockArray", SerializeAnswerLz4BlockArray, lz4BlockArray);
        answerZstd1 = Check("Answer Zstd 1", SerializeAnswerZstd1, zstd1);
        answerZstd3 = Check("Answer Zstd 3", SerializeAnswerZstd3, zstd3);
        answerZstd9 = Check("Answer Zstd 9", SerializeAnswerZstd9, zstd9);
        intsNone = CheckInts("Ints none", SerializeIntsNone, none);
        intsLz4Block = CheckInts("Ints LZ4 Block", SerializeIntsLz4Block, lz4Block);
        intsLz4BlockArray = CheckInts("Ints LZ4 BlockArray", SerializeIntsLz4BlockArray, lz4BlockArray);
        intsZstd1 = CheckInts("Ints Zstd 1", SerializeIntsZstd1, zstd1);
        intsZstd3 = CheckInts("Ints Zstd 3", SerializeIntsZstd3, zstd3);
        intsZstd9 = CheckInts("Ints Zstd 9", SerializeIntsZstd9, zstd9);
    }

    byte[] Check(string label, Func<byte[]> serialize, MessagePackSerializerOptions options)
    {
        var bytes = serialize();
        var back = MessagePackSerializer.Deserialize<Answer>(bytes, options)!;
        if (!V3::MessagePack.MessagePackSerializer.Serialize(back).AsSpan().SequenceEqual(answerOracle)) throw new InvalidOperationException($"verify failed: {label} roundtrip");
        Console.WriteLine($"// {label}: {bytes.Length} B");
        return bytes;
    }

    byte[] CheckInts(string label, Func<byte[]> serialize, MessagePackSerializerOptions options)
    {
        var bytes = serialize();
        if (!MessagePackSerializer.Deserialize<int[]>(bytes, options)!.AsSpan().SequenceEqual(ints)) throw new InvalidOperationException($"verify failed: {label} roundtrip");
        Console.WriteLine($"// {label}: {bytes.Length} B");
        return bytes;
    }

    [BenchmarkCategory("Answer/Serialize"), Benchmark(Baseline = true)] public byte[] SerializeAnswerNone() => MessagePackSerializer.Serialize(answer, none);
    [BenchmarkCategory("Answer/Serialize"), Benchmark] public byte[] SerializeAnswerLz4Block() => MessagePackSerializer.Serialize(answer, lz4Block);
    [BenchmarkCategory("Answer/Serialize"), Benchmark] public byte[] SerializeAnswerLz4BlockArray() => MessagePackSerializer.Serialize(answer, lz4BlockArray);
    [BenchmarkCategory("Answer/Serialize"), Benchmark] public byte[] SerializeAnswerZstd1() => MessagePackSerializer.Serialize(answer, zstd1);
    [BenchmarkCategory("Answer/Serialize"), Benchmark] public byte[] SerializeAnswerZstd3() => MessagePackSerializer.Serialize(answer, zstd3);
    [BenchmarkCategory("Answer/Serialize"), Benchmark] public byte[] SerializeAnswerZstd9() => MessagePackSerializer.Serialize(answer, zstd9);

    [BenchmarkCategory("Answer/Deserialize"), Benchmark(Baseline = true)] public Answer DeserializeAnswerNone() => MessagePackSerializer.Deserialize<Answer>(answerNone, none)!;
    [BenchmarkCategory("Answer/Deserialize"), Benchmark] public Answer DeserializeAnswerLz4Block() => MessagePackSerializer.Deserialize<Answer>(answerLz4Block, lz4Block)!;
    [BenchmarkCategory("Answer/Deserialize"), Benchmark] public Answer DeserializeAnswerLz4BlockArray() => MessagePackSerializer.Deserialize<Answer>(answerLz4BlockArray, lz4BlockArray)!;
    [BenchmarkCategory("Answer/Deserialize"), Benchmark] public Answer DeserializeAnswerZstd1() => MessagePackSerializer.Deserialize<Answer>(answerZstd1, zstd1)!;
    [BenchmarkCategory("Answer/Deserialize"), Benchmark] public Answer DeserializeAnswerZstd3() => MessagePackSerializer.Deserialize<Answer>(answerZstd3, zstd3)!;
    [BenchmarkCategory("Answer/Deserialize"), Benchmark] public Answer DeserializeAnswerZstd9() => MessagePackSerializer.Deserialize<Answer>(answerZstd9, zstd9)!;

    [BenchmarkCategory("Ints/Serialize"), Benchmark(Baseline = true)] public byte[] SerializeIntsNone() => MessagePackSerializer.Serialize(ints, none);
    [BenchmarkCategory("Ints/Serialize"), Benchmark] public byte[] SerializeIntsLz4Block() => MessagePackSerializer.Serialize(ints, lz4Block);
    [BenchmarkCategory("Ints/Serialize"), Benchmark] public byte[] SerializeIntsLz4BlockArray() => MessagePackSerializer.Serialize(ints, lz4BlockArray);
    [BenchmarkCategory("Ints/Serialize"), Benchmark] public byte[] SerializeIntsZstd1() => MessagePackSerializer.Serialize(ints, zstd1);
    [BenchmarkCategory("Ints/Serialize"), Benchmark] public byte[] SerializeIntsZstd3() => MessagePackSerializer.Serialize(ints, zstd3);
    [BenchmarkCategory("Ints/Serialize"), Benchmark] public byte[] SerializeIntsZstd9() => MessagePackSerializer.Serialize(ints, zstd9);

    [BenchmarkCategory("Ints/Deserialize"), Benchmark(Baseline = true)] public int[] DeserializeIntsNone() => MessagePackSerializer.Deserialize<int[]>(intsNone, none)!;
    [BenchmarkCategory("Ints/Deserialize"), Benchmark] public int[] DeserializeIntsLz4Block() => MessagePackSerializer.Deserialize<int[]>(intsLz4Block, lz4Block)!;
    [BenchmarkCategory("Ints/Deserialize"), Benchmark] public int[] DeserializeIntsLz4BlockArray() => MessagePackSerializer.Deserialize<int[]>(intsLz4BlockArray, lz4BlockArray)!;
    [BenchmarkCategory("Ints/Deserialize"), Benchmark] public int[] DeserializeIntsZstd1() => MessagePackSerializer.Deserialize<int[]>(intsZstd1, zstd1)!;
    [BenchmarkCategory("Ints/Deserialize"), Benchmark] public int[] DeserializeIntsZstd3() => MessagePackSerializer.Deserialize<int[]>(intsZstd3, zstd3)!;
    [BenchmarkCategory("Ints/Deserialize"), Benchmark] public int[] DeserializeIntsZstd9() => MessagePackSerializer.Deserialize<int[]>(intsZstd9, zstd9)!;
}
