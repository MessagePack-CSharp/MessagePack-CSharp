using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using MessagePack;

// Lz4FrameProcessor's output windows: how much it asks the output for per encoder call.
//   Answer    = the 1658 B poco from AnswerBenchmark, a frame well under one LZ4 block (64KB)
//   BigBytes  = a 16MB message of a period-100 pattern; the staging buffer's segments double up to 8MB here
// Each row serializes into the byte[] entry (staging over a 1KB stackalloc scratch, then ArrayPool segments) or into a
// fresh ArrayBufferWriter, whose growth follows the size hints it is given.
//
// Before (NativeCompressions 1.3.0, windows from the encoder's streaming bound): every call asked for at least ~64KB,
// because lz4's bound assumes a nearly full block is buffered, and the frame ending had a ~64KB window of its own.
// After: a message under one block uses the whole frame's one-shot bound (tens of bytes over the message) for every
// call; a larger one keeps a call per staging segment in the encoder's bound, the last call also ending the frame.
//
// MEASURED (i7-13700KF, ShortRun; frames: Answer 1118 B, BigBytes 69254 B before and after):
//   AnswerToArray              1.88 us / 1.12 KB    ->  1.95 us / 1.12 KB   (the 1685 B window still exceeds the 1KB scratch)
//   AnswerToNewBufferWriter   29.67 us / 192.16 KB  ->  2.03 us / 5.02 KB
//   BigBytesToArray            1.88 ms / 67.66 KB   ->  1.88 ms / 67.66 KB
//   BigBytesToNewBufferWriter  2.58 ms / 16449 KB   ->  2.62 ms / 16449 KB  (unchanged: a 16MB segment still gets its bound)
// Capping each call at 1MB as LZ4Stream does took BigBytesToNewBufferWriter to 3264 KB, but lz4 can drop its history at
// a call boundary: the 1MB pieces cost this frame 1515 B when cut on block boundaries, and an int[4M] of i%1000*1000
// went from 89,948 B to 99,682 B, so the cap was not kept (see Lz4FrameProcessor's write side).
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Lz4FrameWindowBenchmark
{
    static readonly MessagePackSerializerOptions lz4Frame = MessagePackSerializerOptions.Default.WithLz4Frame();

    Answer answer = default!;
    byte[] bigBytes = default!;

    [GlobalSetup]
    public void Setup()
    {
        answer = AnswerBenchmark.CreateAnswer();
        bigBytes = new byte[16 * 1024 * 1024];
        for (var i = 0; i < bigBytes.Length; i++)
        {
            bigBytes[i] = (byte)(i % 100);
        }

        var answerFrame = MessagePackSerializer.Serialize(answer, lz4Frame);
        if (!MessagePackSerializer.Serialize(MessagePackSerializer.Deserialize<Answer>(answerFrame, lz4Frame)!).AsSpan().SequenceEqual(MessagePackSerializer.Serialize(answer)))
        {
            throw new InvalidOperationException("Answer did not roundtrip through Lz4Frame");
        }
        var bigFrame = MessagePackSerializer.Serialize(bigBytes, lz4Frame);
        if (!MessagePackSerializer.Deserialize<byte[]>(bigFrame, lz4Frame)!.AsSpan().SequenceEqual(bigBytes))
        {
            throw new InvalidOperationException("BigBytes did not roundtrip through Lz4Frame");
        }
        Console.WriteLine($"// Answer frame {answerFrame.Length} B, BigBytes frame {bigFrame.Length} B");
    }

    [BenchmarkCategory("Answer"), Benchmark]
    public byte[] AnswerToArray() => MessagePackSerializer.Serialize(answer, lz4Frame);

    [BenchmarkCategory("Answer"), Benchmark]
    public int AnswerToNewBufferWriter()
    {
        var writer = new ArrayBufferWriter<byte>();
        MessagePackSerializer.Serialize(writer, answer, lz4Frame);
        return writer.WrittenCount;
    }

    [BenchmarkCategory("BigBytes"), Benchmark]
    public byte[] BigBytesToArray() => MessagePackSerializer.Serialize(bigBytes, lz4Frame);

    [BenchmarkCategory("BigBytes"), Benchmark]
    public int BigBytesToNewBufferWriter()
    {
        var writer = new ArrayBufferWriter<byte>();
        MessagePackSerializer.Serialize(writer, bigBytes, lz4Frame);
        return writer.WrittenCount;
    }
}
