using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MessagePack;
using SerializerFoundation;
using static MessagePack.MessagePackPrimitives;

// Ceiling probe for the cursor-read idea (ArrayDeserializeShapeBenchmark follow-up): how much of the
// per-read cost is the address-exposed buffer state (every ReadInt32 round-trips consumed through a
// stack slot because ReadInt32Slow takes `ref buffer` on the cold path)?
//
//   BufferReads    - the shipping shape: ReadOnlySpanReadBuffer local + buffer.ReadInt32() per value.
//                    The buffer local is address-exposed, so span/consumed live in stack slots and the
//                    consumed store->load chain serializes consecutive reads.
//   CursorReads    - the proposed cursor shape: span + position locals, TryReadInt32 inlined against
//                    span.Slice(position), position += tokenSize in registers. The cold path is a
//                    NoInlining STATIC that takes its inputs by value and returns by value (never takes
//                    the address of span/position), modeling the design where exposure is confined to
//                    the cold block. Never taken on this data, but structurally present.
//   CursorNoCold   - the same hot path with a bare throw instead of a cold call: the theoretical upper
//                    bound with no fallback structure at all.
//
// Data: 100,000 uint32-coded ints (0xce, 5 bytes) in one contiguous blob, so every read takes the same
// TryReadInt32 branch path and never hits a slow path. The Mean difference / 100k = recoverable ns per
// field read. Setup() is self-verifying (all candidates must produce the oracle sum).
public class CursorReadProbeBenchmark
{
    const int ValueCount = 100_000;

    byte[] blob = null!;
    long expectedSum;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            for (int i = 0; i < ValueCount; i++)
            {
                buffer.WriteInt32(rand.Next(100_000, int.MaxValue)); // always uint32-coded
            }
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
        blob = writer.WrittenSpan.ToArray();

        expectedSum = BufferReads();
        if (CursorReads() != expectedSum || CursorNoCold() != expectedSum)
        {
            throw new InvalidOperationException("cursor probe checksum mismatch");
        }
    }

    [Benchmark(Baseline = true)]
    public long BufferReads()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        long sum = 0;
        for (int i = 0; i < ValueCount; i++)
        {
            sum += buffer.ReadInt32();
        }
        return sum;
    }

    [Benchmark]
    public long CursorReads()
    {
        var span = new ReadOnlySpan<byte>(blob);
        int position = 0;
        long sum = 0;
        for (int i = 0; i < ValueCount; i++)
        {
            var r = TryReadInt32(span.Slice(position), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                position += tokenSize;
                sum += value;
            }
            else
            {
                var cold = ReadInt32ColdPath(blob, position);
                position = cold.NewPosition; // plain assignments: span/position stay enregisterable
                sum += cold.Value;
            }
        }
        return sum;
    }

    [Benchmark]
    public long CursorNoCold()
    {
        var span = new ReadOnlySpan<byte>(blob);
        int position = 0;
        long sum = 0;
        for (int i = 0; i < ValueCount; i++)
        {
            var r = TryReadInt32(span.Slice(position), out var value, out var tokenSize);
            if (r != DecodeResult.Success)
            {
                throw new InvalidOperationException("unreachable on this data");
            }
            position += tokenSize;
            sum += value;
        }
        return sum;
    }

    // value-in / value-out cold fallback: mirrors the cursor design's exposure-free slow path.
    // Unreachable on this blob; correctness is covered by the shared-oracle checksum in Setup.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static (int Value, int NewPosition) ReadInt32ColdPath(byte[] source, int position)
    {
        var buffer = new ReadOnlySpanReadBuffer(new ReadOnlySpan<byte>(source).Slice(position));
        var value = buffer.ReadInt32();
        var consumed = source.Length - position - (int)buffer.BytesRemaining;
        return (value, position + consumed);
    }
}
