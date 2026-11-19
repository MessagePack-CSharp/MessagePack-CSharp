using BenchmarkDotNet.Attributes;
using MessagePack;
using SerializerFoundation;
using System.Runtime.CompilerServices;
using static MessagePack.MessagePackPrimitives;

// Control for FloatRunFuseBenchmark: the same fusion (one window check, register-resident offset, one Advance, then
// the shared for / switch) over a run of INT members, in the same latency-bound blob harness. An int token's size
// is only known after its code byte is decoded, so the fused run cannot use constant offsets: each read's address
// is the previous read's result. PocoPerValueVsBatchBenchmark round 9 measured this flat (1.01 / 1.02) end to end
// on one repeated payload; this asks whether the float result is the harness or the fixed width.
//
//   Loop (baseline) - the shipped generated shape: ReadArrayHeader + for / switch (i) with ReadInt32 per member
//   FusedThenLoop   - `count >= 3` + window of 3 * MaxInt32Length, TryReadInt32 chain on a local offset, one
//                     Advance, then the loop from i = 3 (a miss runs it from i = 0)
//
// Width (50k objects x 4 tokens, distinct payloads):
//   Fixint - every member 0..127 (1 byte): TryReadInt32's fixint branch predicts, its tokenSize is a constant
//   Fixed5 - every member uint32-coded (5 bytes): one table path, the size still comes out of the table load
//   Mixed  - every member drawn uniformly from the 1 / 2 / 3 / 5-byte classes
//
// MEASURED (i7-13700KF, .NET 11, ShortRun x2, FusedThenLoop ratio to Loop):
//   Fixint 0.69 / 0.64   (4.8 -> 3.3 ns/object)
//   Fixed5 0.94 / 0.98   (10.1 -> 9.9 ns/object)
//   Mixed  0.92 / 0.90   (13.3 -> 12.2 ns/object)
// The width is the variable. Where the size is a predicted constant (fixint: the branch predicts and tokenSize is
// the literal 1) the fusion wins as it does for floats; where the size comes out of the table load it is a data
// dependency no fusion removes, and the same three 5-byte tokens that cost 4.6 ns as floats cost 10 ns as ints
// either way. Never slower in this harness. Not measured end to end; round 9's end-to-end 1.01 / 1.02 was on a
// mixed-width payload.
public class IntRunFuseControlBenchmark
{
    [Params("Fixint", "Fixed5", "Mixed")]
    public string Width = "Fixint";

    const int ObjectCount = 50_000;

    byte[] blob = null!;
    readonly Int3 poco = new();

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            for (int o = 0; o < ObjectCount; o++)
            {
                buffer.WriteArrayHeader(3);
                for (int i = 0; i < 3; i++)
                {
                    var widthClass = Width switch
                    {
                        "Fixint" => 0,
                        "Fixed5" => 3,
                        "Mixed" => rand.Next(4),
                        _ => throw new InvalidOperationException(Width),
                    };
                    buffer.WriteInt32(widthClass switch
                    {
                        0 => rand.Next(0, 128),            // fixint, 1 byte
                        1 => rand.Next(128, 256),          // uint8, 2 bytes
                        2 => rand.Next(256, 65536),        // uint16, 3 bytes
                        _ => rand.Next(100_000, int.MaxValue), // uint32, 5 bytes
                    });
                }
            }
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
        blob = writer.WrittenSpan.ToArray();

        if (FusedThenLoop() != Loop())
        {
            throw new InvalidOperationException($"FusedThenLoop checksum mismatch on {Width}");
        }
    }

    [Benchmark(Baseline = true)]
    public long Loop()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            ReadLoop(ref buffer, value);
            sum += (long)value.A + value.B + value.C;
        }
        return sum;
    }

    [Benchmark]
    public long FusedThenLoop()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            ReadFusedThenLoop(ref buffer, value);
            sum += (long)value.A + value.B + value.C;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void ReadLoop(ref ReadOnlySpanReadBuffer buffer, Int3 value)
    {
        var count = buffer.ReadArrayHeader();
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.A = buffer.ReadInt32(); break;
                case 1: value.B = buffer.ReadInt32(); break;
                case 2: value.C = buffer.ReadInt32(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void ReadFusedThenLoop(ref ReadOnlySpanReadBuffer buffer, Int3 value)
    {
        var count = buffer.ReadArrayHeader();
        var i = 0;
        var span = buffer.GetUnreadSpan();
        if (count >= 3 && span.Length >= 3 * MaxInt32Length)
        {
            // each Slice starts where the previous token ended: the offset is data, not a constant
            if (TryReadInt32(span, out var a, out var size) == DecodeResult.Success)
            {
                var offset = size;
                if (TryReadInt32(span.Slice(offset), out var b, out size) == DecodeResult.Success)
                {
                    offset += size;
                    if (TryReadInt32(span.Slice(offset), out var c, out size) == DecodeResult.Success)
                    {
                        offset += size;
                        value.A = a;
                        value.B = b;
                        value.C = c;
                        buffer.Advance(offset);
                        i = 3;
                    }
                }
            }
        }
        for (; i < count; i++)
        {
            switch (i)
            {
                case 0: value.A = buffer.ReadInt32(); break;
                case 1: value.B = buffer.ReadInt32(); break;
                case 2: value.C = buffer.ReadInt32(); break;
                default: buffer.Skip(); break;
            }
        }
    }
}

public sealed class Int3
{
    public int A;
    public int B;
    public int C;
}
