using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MessagePack;
using SerializerFoundation;

// Array-format (int-key) deserialization loop shapes for the source generator's ObjectEmitter.EmitReadLoop.
// The generated code today is `for (i < count) switch (i)` — a loop with a jump-table indirect branch per element.
// Array elements arrive in declaration order by definition (positional), so unlike the map/string-key loop no
// matching is needed at all: the only variable is count (version tolerance — an old writer wrote fewer fields,
// a new writer appended unknown trailing fields that must be skipped).
//
// Candidates:
//   LoopSwitch               - the current generated shape: for + switch(i) with default: Skip()
//   LoopIfElseChain          - for + if/else-if chain on i (compare ladder instead of jump table)
//   LoopSwitchSplitTail      - for i < min(count, N) + switch WITHOUT default, then a separate skip loop for extras
//   FlatIfLadder             - no loop: `if (count > 0) …; if (count > 1) …;` sequential, then skip loop (the
//                              deleted hand-written formatter shape, cf. GenVsHandAsArrayBenchmark)
//   NestedIfLadder           - the same ladder nested so an early-out skips the remaining compares
//   CountExactThenLoopSwitch - `if (count == N)` straight-line reads, anything else falls back to LoopSwitch
//   CountExactThenLadder     - the same fast path, fallback is FlatIfLadder
//   CountAtLeastThenLadder   - `if (count >= N)` straight-line reads + skip loop (new-writer payloads stay on the
//                              fast path), fallback is FlatIfLadder
//   CountSwitchFullUnroll    - switch (count) with a fully unrolled straight-line body per case (zero per-field
//                              branches, O(N^2) code size — the extreme point of the trade-off)
//
// Values are uniformly uint32-coded (0xce, 5 bytes) so every ReadInt32 takes the same internal branch path and
// the measured differences isolate the loop skeleton, not the value-width predictor. Distributions (100k fields
// per CLAUDE.md's predictor pitfall):
//   SameVersion  - every array has exactly N elements: the dominant real-world case every fast path targets
//   OldWriter    - every array has fewer elements (N=16: 12, N=4: 2): reader is newer than the writer
//   NewWriter    - every array has more elements (N=16: 20, N=4: 6): writer appended fields, extras are skipped
//   MixedVersion - per-object random pick of the three counts: rolling-upgrade worst case for count checks
//
// Setup() is self-verifying (every candidate's checksum must equal the LoopSwitch oracle on the generated blob);
// VerifyCandidates() (wired into --verify) sweeps single-object payloads at every count 0..N+8 and checks every
// field value, untouched-sentinel preservation, and full payload consumption.
public class ArrayShapePoco16
{
    public int Value0; public int Value1; public int Value2; public int Value3;
    public int Value4; public int Value5; public int Value6; public int Value7;
    public int Value8; public int Value9; public int Value10; public int Value11;
    public int Value12; public int Value13; public int Value14; public int Value15;
}

public class ArrayShapePoco4
{
    public int Value0; public int Value1; public int Value2; public int Value3;
}

public class ArrayDeserializeShape16Benchmark
{
    [Params("SameVersion", "OldWriter", "NewWriter", "MixedVersion")]
    public string Distribution = "SameVersion";

    const int FieldCount = 16;
    const int OldWriterCount = 12;
    const int NewWriterCount = 20;
    const int ObjectCount = 6250; // x16 fields = 100,000

    byte[] blob = null!;
    int objectCount;
    readonly ArrayShapePoco16 poco = new();
    long expectedSum;

    [GlobalSetup]
    public void Setup()
    {
        blob = BuildBlob(Distribution, out objectCount);
        expectedSum = LoopSwitch();
        foreach (var (name, sum) in new (string, long)[]
        {
            (nameof(LoopIfElseChain), LoopIfElseChain()),
            (nameof(LoopSwitchSplitTail), LoopSwitchSplitTail()),
            (nameof(FlatIfLadder), FlatIfLadder()),
            (nameof(NestedIfLadder), NestedIfLadder()),
            (nameof(CountExactThenLoopSwitch), CountExactThenLoopSwitch()),
            (nameof(CountExactThenLadder), CountExactThenLadder()),
            (nameof(CountAtLeastThenLadder), CountAtLeastThenLadder()),
            (nameof(CountSwitchFullUnroll), CountSwitchFullUnroll()),
            (nameof(LoopBlockOf4), LoopBlockOf4()),
        })
        {
            if (sum != expectedSum)
            {
                throw new InvalidOperationException($"{name} checksum mismatch on {Distribution}: {sum} (expected {expectedSum})");
            }
        }
    }

    static byte[] BuildBlob(string distribution, out int objectCount)
    {
        var rand = new Random(42);
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            objectCount = ObjectCount;
            for (int o = 0; o < ObjectCount; o++)
            {
                var count = distribution switch
                {
                    "SameVersion" => FieldCount,
                    "OldWriter" => OldWriterCount,
                    "NewWriter" => NewWriterCount,
                    "MixedVersion" => rand.Next(3) switch { 0 => OldWriterCount, 1 => FieldCount, _ => NewWriterCount },
                    _ => throw new InvalidOperationException(distribution),
                };
                buffer.WriteArrayHeader(count);
                for (int i = 0; i < count; i++)
                {
                    // [100_000, int.MaxValue): always uint32-coded (0xce), keeping ReadInt32's width branches constant
                    buffer.WriteInt32(rand.Next(100_000, int.MaxValue));
                }
            }
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
        return writer.WrittenSpan.ToArray();
    }

    // ---- benchmarks -------------------------------------------------------

    [Benchmark(Baseline = true)]
    public long LoopSwitch()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadLoopSwitch(ref buffer, value);
            sum += value.Value0 + value.Value7 + value.Value11 + value.Value15;
        }
        return sum;
    }

    [Benchmark]
    public long LoopIfElseChain()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadLoopIfElseChain(ref buffer, value);
            sum += value.Value0 + value.Value7 + value.Value11 + value.Value15;
        }
        return sum;
    }

    [Benchmark]
    public long LoopSwitchSplitTail()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadLoopSwitchSplitTail(ref buffer, value);
            sum += value.Value0 + value.Value7 + value.Value11 + value.Value15;
        }
        return sum;
    }

    [Benchmark]
    public long FlatIfLadder()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadFlatIfLadder(ref buffer, value);
            sum += value.Value0 + value.Value7 + value.Value11 + value.Value15;
        }
        return sum;
    }

    [Benchmark]
    public long NestedIfLadder()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadNestedIfLadder(ref buffer, value);
            sum += value.Value0 + value.Value7 + value.Value11 + value.Value15;
        }
        return sum;
    }

    [Benchmark]
    public long CountExactThenLoopSwitch()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadCountExactThenLoopSwitch(ref buffer, value);
            sum += value.Value0 + value.Value7 + value.Value11 + value.Value15;
        }
        return sum;
    }

    [Benchmark]
    public long CountExactThenLadder()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadCountExactThenLadder(ref buffer, value);
            sum += value.Value0 + value.Value7 + value.Value11 + value.Value15;
        }
        return sum;
    }

    [Benchmark]
    public long CountAtLeastThenLadder()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadCountAtLeastThenLadder(ref buffer, value);
            sum += value.Value0 + value.Value7 + value.Value11 + value.Value15;
        }
        return sum;
    }

    [Benchmark]
    public long CountSwitchFullUnroll()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadCountSwitchFullUnroll(ref buffer, value);
            sum += value.Value0 + value.Value7 + value.Value11 + value.Value15;
        }
        return sum;
    }

    [Benchmark]
    public long LoopBlockOf4()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadLoopBlockOf4(ref buffer, value);
            sum += value.Value0 + value.Value7 + value.Value11 + value.Value15;
        }
        return sum;
    }

    // ---- shapes -----------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadLoopSwitch(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco16 value)
    {
        var count = buffer.ReadArrayHeader();
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.Value0 = buffer.ReadInt32(); break;
                case 1: value.Value1 = buffer.ReadInt32(); break;
                case 2: value.Value2 = buffer.ReadInt32(); break;
                case 3: value.Value3 = buffer.ReadInt32(); break;
                case 4: value.Value4 = buffer.ReadInt32(); break;
                case 5: value.Value5 = buffer.ReadInt32(); break;
                case 6: value.Value6 = buffer.ReadInt32(); break;
                case 7: value.Value7 = buffer.ReadInt32(); break;
                case 8: value.Value8 = buffer.ReadInt32(); break;
                case 9: value.Value9 = buffer.ReadInt32(); break;
                case 10: value.Value10 = buffer.ReadInt32(); break;
                case 11: value.Value11 = buffer.ReadInt32(); break;
                case 12: value.Value12 = buffer.ReadInt32(); break;
                case 13: value.Value13 = buffer.ReadInt32(); break;
                case 14: value.Value14 = buffer.ReadInt32(); break;
                case 15: value.Value15 = buffer.ReadInt32(); break;
                default: buffer.Skip(); break; // version tolerance: unknown trailing member
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadLoopIfElseChain(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco16 value)
    {
        var count = buffer.ReadArrayHeader();
        for (int i = 0; i < count; i++)
        {
            if (i == 0) value.Value0 = buffer.ReadInt32();
            else if (i == 1) value.Value1 = buffer.ReadInt32();
            else if (i == 2) value.Value2 = buffer.ReadInt32();
            else if (i == 3) value.Value3 = buffer.ReadInt32();
            else if (i == 4) value.Value4 = buffer.ReadInt32();
            else if (i == 5) value.Value5 = buffer.ReadInt32();
            else if (i == 6) value.Value6 = buffer.ReadInt32();
            else if (i == 7) value.Value7 = buffer.ReadInt32();
            else if (i == 8) value.Value8 = buffer.ReadInt32();
            else if (i == 9) value.Value9 = buffer.ReadInt32();
            else if (i == 10) value.Value10 = buffer.ReadInt32();
            else if (i == 11) value.Value11 = buffer.ReadInt32();
            else if (i == 12) value.Value12 = buffer.ReadInt32();
            else if (i == 13) value.Value13 = buffer.ReadInt32();
            else if (i == 14) value.Value14 = buffer.ReadInt32();
            else if (i == 15) value.Value15 = buffer.ReadInt32();
            else buffer.Skip();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadLoopSwitchSplitTail(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco16 value)
    {
        var count = buffer.ReadArrayHeader();
        var known = count < 16 ? count : 16;
        for (int i = 0; i < known; i++)
        {
            switch (i)
            {
                case 0: value.Value0 = buffer.ReadInt32(); break;
                case 1: value.Value1 = buffer.ReadInt32(); break;
                case 2: value.Value2 = buffer.ReadInt32(); break;
                case 3: value.Value3 = buffer.ReadInt32(); break;
                case 4: value.Value4 = buffer.ReadInt32(); break;
                case 5: value.Value5 = buffer.ReadInt32(); break;
                case 6: value.Value6 = buffer.ReadInt32(); break;
                case 7: value.Value7 = buffer.ReadInt32(); break;
                case 8: value.Value8 = buffer.ReadInt32(); break;
                case 9: value.Value9 = buffer.ReadInt32(); break;
                case 10: value.Value10 = buffer.ReadInt32(); break;
                case 11: value.Value11 = buffer.ReadInt32(); break;
                case 12: value.Value12 = buffer.ReadInt32(); break;
                case 13: value.Value13 = buffer.ReadInt32(); break;
                case 14: value.Value14 = buffer.ReadInt32(); break;
                case 15: value.Value15 = buffer.ReadInt32(); break;
            }
        }
        for (int i = 16; i < count; i++)
        {
            buffer.Skip();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadFlatIfLadder(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco16 value)
    {
        var count = buffer.ReadArrayHeader();
        if (count > 0) value.Value0 = buffer.ReadInt32();
        if (count > 1) value.Value1 = buffer.ReadInt32();
        if (count > 2) value.Value2 = buffer.ReadInt32();
        if (count > 3) value.Value3 = buffer.ReadInt32();
        if (count > 4) value.Value4 = buffer.ReadInt32();
        if (count > 5) value.Value5 = buffer.ReadInt32();
        if (count > 6) value.Value6 = buffer.ReadInt32();
        if (count > 7) value.Value7 = buffer.ReadInt32();
        if (count > 8) value.Value8 = buffer.ReadInt32();
        if (count > 9) value.Value9 = buffer.ReadInt32();
        if (count > 10) value.Value10 = buffer.ReadInt32();
        if (count > 11) value.Value11 = buffer.ReadInt32();
        if (count > 12) value.Value12 = buffer.ReadInt32();
        if (count > 13) value.Value13 = buffer.ReadInt32();
        if (count > 14) value.Value14 = buffer.ReadInt32();
        if (count > 15) value.Value15 = buffer.ReadInt32();
        for (int i = 16; i < count; i++)
        {
            buffer.Skip();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadNestedIfLadder(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco16 value)
    {
        var count = buffer.ReadArrayHeader();
        // nested so a short (old-writer) array exits after its own compares instead of testing all 16
        if (count > 0) { value.Value0 = buffer.ReadInt32();
        if (count > 1) { value.Value1 = buffer.ReadInt32();
        if (count > 2) { value.Value2 = buffer.ReadInt32();
        if (count > 3) { value.Value3 = buffer.ReadInt32();
        if (count > 4) { value.Value4 = buffer.ReadInt32();
        if (count > 5) { value.Value5 = buffer.ReadInt32();
        if (count > 6) { value.Value6 = buffer.ReadInt32();
        if (count > 7) { value.Value7 = buffer.ReadInt32();
        if (count > 8) { value.Value8 = buffer.ReadInt32();
        if (count > 9) { value.Value9 = buffer.ReadInt32();
        if (count > 10) { value.Value10 = buffer.ReadInt32();
        if (count > 11) { value.Value11 = buffer.ReadInt32();
        if (count > 12) { value.Value12 = buffer.ReadInt32();
        if (count > 13) { value.Value13 = buffer.ReadInt32();
        if (count > 14) { value.Value14 = buffer.ReadInt32();
        if (count > 15) { value.Value15 = buffer.ReadInt32();
        for (int i = 16; i < count; i++)
        {
            buffer.Skip();
        }
        } } } } } } } } } } } } } } } }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadCountExactThenLoopSwitch(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco16 value)
    {
        var count = buffer.ReadArrayHeader();
        if (count == 16)
        {
            value.Value0 = buffer.ReadInt32();
            value.Value1 = buffer.ReadInt32();
            value.Value2 = buffer.ReadInt32();
            value.Value3 = buffer.ReadInt32();
            value.Value4 = buffer.ReadInt32();
            value.Value5 = buffer.ReadInt32();
            value.Value6 = buffer.ReadInt32();
            value.Value7 = buffer.ReadInt32();
            value.Value8 = buffer.ReadInt32();
            value.Value9 = buffer.ReadInt32();
            value.Value10 = buffer.ReadInt32();
            value.Value11 = buffer.ReadInt32();
            value.Value12 = buffer.ReadInt32();
            value.Value13 = buffer.ReadInt32();
            value.Value14 = buffer.ReadInt32();
            value.Value15 = buffer.ReadInt32();
            return;
        }
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.Value0 = buffer.ReadInt32(); break;
                case 1: value.Value1 = buffer.ReadInt32(); break;
                case 2: value.Value2 = buffer.ReadInt32(); break;
                case 3: value.Value3 = buffer.ReadInt32(); break;
                case 4: value.Value4 = buffer.ReadInt32(); break;
                case 5: value.Value5 = buffer.ReadInt32(); break;
                case 6: value.Value6 = buffer.ReadInt32(); break;
                case 7: value.Value7 = buffer.ReadInt32(); break;
                case 8: value.Value8 = buffer.ReadInt32(); break;
                case 9: value.Value9 = buffer.ReadInt32(); break;
                case 10: value.Value10 = buffer.ReadInt32(); break;
                case 11: value.Value11 = buffer.ReadInt32(); break;
                case 12: value.Value12 = buffer.ReadInt32(); break;
                case 13: value.Value13 = buffer.ReadInt32(); break;
                case 14: value.Value14 = buffer.ReadInt32(); break;
                case 15: value.Value15 = buffer.ReadInt32(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadCountExactThenLadder(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco16 value)
    {
        var count = buffer.ReadArrayHeader();
        if (count == 16)
        {
            value.Value0 = buffer.ReadInt32();
            value.Value1 = buffer.ReadInt32();
            value.Value2 = buffer.ReadInt32();
            value.Value3 = buffer.ReadInt32();
            value.Value4 = buffer.ReadInt32();
            value.Value5 = buffer.ReadInt32();
            value.Value6 = buffer.ReadInt32();
            value.Value7 = buffer.ReadInt32();
            value.Value8 = buffer.ReadInt32();
            value.Value9 = buffer.ReadInt32();
            value.Value10 = buffer.ReadInt32();
            value.Value11 = buffer.ReadInt32();
            value.Value12 = buffer.ReadInt32();
            value.Value13 = buffer.ReadInt32();
            value.Value14 = buffer.ReadInt32();
            value.Value15 = buffer.ReadInt32();
            return;
        }
        if (count > 0) value.Value0 = buffer.ReadInt32();
        if (count > 1) value.Value1 = buffer.ReadInt32();
        if (count > 2) value.Value2 = buffer.ReadInt32();
        if (count > 3) value.Value3 = buffer.ReadInt32();
        if (count > 4) value.Value4 = buffer.ReadInt32();
        if (count > 5) value.Value5 = buffer.ReadInt32();
        if (count > 6) value.Value6 = buffer.ReadInt32();
        if (count > 7) value.Value7 = buffer.ReadInt32();
        if (count > 8) value.Value8 = buffer.ReadInt32();
        if (count > 9) value.Value9 = buffer.ReadInt32();
        if (count > 10) value.Value10 = buffer.ReadInt32();
        if (count > 11) value.Value11 = buffer.ReadInt32();
        if (count > 12) value.Value12 = buffer.ReadInt32();
        if (count > 13) value.Value13 = buffer.ReadInt32();
        if (count > 14) value.Value14 = buffer.ReadInt32();
        if (count > 15) value.Value15 = buffer.ReadInt32();
        for (int i = 16; i < count; i++)
        {
            buffer.Skip();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadCountAtLeastThenLadder(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco16 value)
    {
        var count = buffer.ReadArrayHeader();
        if (count >= 16)
        {
            value.Value0 = buffer.ReadInt32();
            value.Value1 = buffer.ReadInt32();
            value.Value2 = buffer.ReadInt32();
            value.Value3 = buffer.ReadInt32();
            value.Value4 = buffer.ReadInt32();
            value.Value5 = buffer.ReadInt32();
            value.Value6 = buffer.ReadInt32();
            value.Value7 = buffer.ReadInt32();
            value.Value8 = buffer.ReadInt32();
            value.Value9 = buffer.ReadInt32();
            value.Value10 = buffer.ReadInt32();
            value.Value11 = buffer.ReadInt32();
            value.Value12 = buffer.ReadInt32();
            value.Value13 = buffer.ReadInt32();
            value.Value14 = buffer.ReadInt32();
            value.Value15 = buffer.ReadInt32();
            for (int i = 16; i < count; i++)
            {
                buffer.Skip();
            }
            return;
        }
        if (count > 0) value.Value0 = buffer.ReadInt32();
        if (count > 1) value.Value1 = buffer.ReadInt32();
        if (count > 2) value.Value2 = buffer.ReadInt32();
        if (count > 3) value.Value3 = buffer.ReadInt32();
        if (count > 4) value.Value4 = buffer.ReadInt32();
        if (count > 5) value.Value5 = buffer.ReadInt32();
        if (count > 6) value.Value6 = buffer.ReadInt32();
        if (count > 7) value.Value7 = buffer.ReadInt32();
        if (count > 8) value.Value8 = buffer.ReadInt32();
        if (count > 9) value.Value9 = buffer.ReadInt32();
        if (count > 10) value.Value10 = buffer.ReadInt32();
        if (count > 11) value.Value11 = buffer.ReadInt32();
        if (count > 12) value.Value12 = buffer.ReadInt32();
        if (count > 13) value.Value13 = buffer.ReadInt32();
        if (count > 14) value.Value14 = buffer.ReadInt32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadCountSwitchFullUnroll(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco16 value)
    {
        var count = buffer.ReadArrayHeader();
        switch (count)
        {
            case 0:
                break;
            case 1:
                value.Value0 = buffer.ReadInt32();
                break;
            case 2:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32();
                break;
            case 3:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32();
                break;
            case 4:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                break;
            case 5:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32();
                break;
            case 6:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32();
                break;
            case 7:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32(); value.Value6 = buffer.ReadInt32();
                break;
            case 8:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32(); value.Value6 = buffer.ReadInt32(); value.Value7 = buffer.ReadInt32();
                break;
            case 9:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32(); value.Value6 = buffer.ReadInt32(); value.Value7 = buffer.ReadInt32();
                value.Value8 = buffer.ReadInt32();
                break;
            case 10:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32(); value.Value6 = buffer.ReadInt32(); value.Value7 = buffer.ReadInt32();
                value.Value8 = buffer.ReadInt32(); value.Value9 = buffer.ReadInt32();
                break;
            case 11:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32(); value.Value6 = buffer.ReadInt32(); value.Value7 = buffer.ReadInt32();
                value.Value8 = buffer.ReadInt32(); value.Value9 = buffer.ReadInt32(); value.Value10 = buffer.ReadInt32();
                break;
            case 12:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32(); value.Value6 = buffer.ReadInt32(); value.Value7 = buffer.ReadInt32();
                value.Value8 = buffer.ReadInt32(); value.Value9 = buffer.ReadInt32(); value.Value10 = buffer.ReadInt32(); value.Value11 = buffer.ReadInt32();
                break;
            case 13:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32(); value.Value6 = buffer.ReadInt32(); value.Value7 = buffer.ReadInt32();
                value.Value8 = buffer.ReadInt32(); value.Value9 = buffer.ReadInt32(); value.Value10 = buffer.ReadInt32(); value.Value11 = buffer.ReadInt32();
                value.Value12 = buffer.ReadInt32();
                break;
            case 14:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32(); value.Value6 = buffer.ReadInt32(); value.Value7 = buffer.ReadInt32();
                value.Value8 = buffer.ReadInt32(); value.Value9 = buffer.ReadInt32(); value.Value10 = buffer.ReadInt32(); value.Value11 = buffer.ReadInt32();
                value.Value12 = buffer.ReadInt32(); value.Value13 = buffer.ReadInt32();
                break;
            case 15:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32(); value.Value6 = buffer.ReadInt32(); value.Value7 = buffer.ReadInt32();
                value.Value8 = buffer.ReadInt32(); value.Value9 = buffer.ReadInt32(); value.Value10 = buffer.ReadInt32(); value.Value11 = buffer.ReadInt32();
                value.Value12 = buffer.ReadInt32(); value.Value13 = buffer.ReadInt32(); value.Value14 = buffer.ReadInt32();
                break;
            case 16:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32(); value.Value6 = buffer.ReadInt32(); value.Value7 = buffer.ReadInt32();
                value.Value8 = buffer.ReadInt32(); value.Value9 = buffer.ReadInt32(); value.Value10 = buffer.ReadInt32(); value.Value11 = buffer.ReadInt32();
                value.Value12 = buffer.ReadInt32(); value.Value13 = buffer.ReadInt32(); value.Value14 = buffer.ReadInt32(); value.Value15 = buffer.ReadInt32();
                break;
            default:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32(); value.Value6 = buffer.ReadInt32(); value.Value7 = buffer.ReadInt32();
                value.Value8 = buffer.ReadInt32(); value.Value9 = buffer.ReadInt32(); value.Value10 = buffer.ReadInt32(); value.Value11 = buffer.ReadInt32();
                value.Value12 = buffer.ReadInt32(); value.Value13 = buffer.ReadInt32(); value.Value14 = buffer.ReadInt32(); value.Value15 = buffer.ReadInt32();
                for (int i = 16; i < count; i++)
                {
                    buffer.Skip();
                }
                break;
        }
    }

    // hypothesis probe for the round-1 result (straight-line 1.26x slower than loop+switch): the fast path
    // loops over 4 blocks of 4 straight-line reads through the same jump-table dispatch as LoopSwitch.
    // If block layout inside switch cases is what makes the loop version fast, this should match LoopSwitch
    // with a quarter of the dispatches; if straight-line expansions are inherently laid out worse, it should
    // land near FlatIfLadder.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadLoopBlockOf4(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco16 value)
    {
        var count = buffer.ReadArrayHeader();
        if (count == 16)
        {
            for (int block = 0; block < 4; block++)
            {
                switch (block)
                {
                    case 0:
                        value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32();
                        value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                        break;
                    case 1:
                        value.Value4 = buffer.ReadInt32(); value.Value5 = buffer.ReadInt32();
                        value.Value6 = buffer.ReadInt32(); value.Value7 = buffer.ReadInt32();
                        break;
                    case 2:
                        value.Value8 = buffer.ReadInt32(); value.Value9 = buffer.ReadInt32();
                        value.Value10 = buffer.ReadInt32(); value.Value11 = buffer.ReadInt32();
                        break;
                    case 3:
                        value.Value12 = buffer.ReadInt32(); value.Value13 = buffer.ReadInt32();
                        value.Value14 = buffer.ReadInt32(); value.Value15 = buffer.ReadInt32();
                        break;
                }
            }
            return;
        }
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.Value0 = buffer.ReadInt32(); break;
                case 1: value.Value1 = buffer.ReadInt32(); break;
                case 2: value.Value2 = buffer.ReadInt32(); break;
                case 3: value.Value3 = buffer.ReadInt32(); break;
                case 4: value.Value4 = buffer.ReadInt32(); break;
                case 5: value.Value5 = buffer.ReadInt32(); break;
                case 6: value.Value6 = buffer.ReadInt32(); break;
                case 7: value.Value7 = buffer.ReadInt32(); break;
                case 8: value.Value8 = buffer.ReadInt32(); break;
                case 9: value.Value9 = buffer.ReadInt32(); break;
                case 10: value.Value10 = buffer.ReadInt32(); break;
                case 11: value.Value11 = buffer.ReadInt32(); break;
                case 12: value.Value12 = buffer.ReadInt32(); break;
                case 13: value.Value13 = buffer.ReadInt32(); break;
                case 14: value.Value14 = buffer.ReadInt32(); break;
                case 15: value.Value15 = buffer.ReadInt32(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    // ---- verification -----------------------------------------------------

    delegate void ReadShape16(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco16 value);

    public static void VerifyCandidates()
    {
        var shapes = new (string Name, ReadShape16 Read)[]
        {
            (nameof(ReadLoopSwitch), ReadLoopSwitch),
            (nameof(ReadLoopIfElseChain), ReadLoopIfElseChain),
            (nameof(ReadLoopSwitchSplitTail), ReadLoopSwitchSplitTail),
            (nameof(ReadFlatIfLadder), ReadFlatIfLadder),
            (nameof(ReadNestedIfLadder), ReadNestedIfLadder),
            (nameof(ReadCountExactThenLoopSwitch), ReadCountExactThenLoopSwitch),
            (nameof(ReadCountExactThenLadder), ReadCountExactThenLadder),
            (nameof(ReadCountAtLeastThenLadder), ReadCountAtLeastThenLadder),
            (nameof(ReadCountSwitchFullUnroll), ReadCountSwitchFullUnroll),
            (nameof(ReadLoopBlockOf4), ReadLoopBlockOf4),
        };

        // count sweep 0..24: below/at/above the field count, every candidate must read fields
        // [0, min(count, 16)), preserve the sentinel in the rest, and consume the payload fully
        for (int count = 0; count <= 24; count++)
        {
            var writer = new System.Buffers.ArrayBufferWriter<byte>();
            var write = new BufferWriterWriteBuffer(writer);
            try
            {
                write.WriteArrayHeader(count);
                for (int i = 0; i < count; i++)
                {
                    write.WriteInt32(ValueAt(count, i));
                }
                write.Flush();
            }
            finally
            {
                write.Dispose();
            }
            var payload = writer.WrittenSpan.ToArray();

            foreach (var (name, read) in shapes)
            {
                var value = new ArrayShapePoco16
                {
                    Value0 = -1, Value1 = -2, Value2 = -3, Value3 = -4, Value4 = -5, Value5 = -6, Value6 = -7, Value7 = -8,
                    Value8 = -9, Value9 = -10, Value10 = -11, Value11 = -12, Value12 = -13, Value13 = -14, Value14 = -15, Value15 = -16,
                };
                var buffer = new ReadOnlySpanReadBuffer(payload);
                read(ref buffer, value);
                if (buffer.BytesRemaining != 0)
                {
                    throw new InvalidOperationException($"{name} count={count}: {buffer.BytesRemaining} bytes left unconsumed");
                }
                int[] fields = [value.Value0, value.Value1, value.Value2, value.Value3, value.Value4, value.Value5, value.Value6, value.Value7,
                    value.Value8, value.Value9, value.Value10, value.Value11, value.Value12, value.Value13, value.Value14, value.Value15];
                for (int i = 0; i < 16; i++)
                {
                    var expected = i < count ? ValueAt(count, i) : -(i + 1);
                    if (fields[i] != expected)
                    {
                        throw new InvalidOperationException($"{name} count={count} field {i}: {fields[i]} (expected {expected})");
                    }
                }
            }
        }

        // dataset cross-check (Setup throws on candidate checksum mismatch) for every distribution
        foreach (var distribution in new[] { "SameVersion", "OldWriter", "NewWriter", "MixedVersion" })
        {
            new ArrayDeserializeShape16Benchmark { Distribution = distribution }.Setup();
            new ArrayDeserializeShape4Benchmark { Distribution = distribution }.Setup();
        }

        static int ValueAt(int count, int index) => 100_000 + count * 1000 + index * 7;
    }
}

// the small-N twin: most real POCOs have a handful of fields; jump table vs compare chain and
// fast-path overhead trade off differently at N=4
public class ArrayDeserializeShape4Benchmark
{
    [Params("SameVersion", "OldWriter", "NewWriter", "MixedVersion")]
    public string Distribution = "SameVersion";

    const int FieldCount = 4;
    const int OldWriterCount = 2;
    const int NewWriterCount = 6;
    const int ObjectCount = 25_000; // x4 fields = 100,000

    byte[] blob = null!;
    int objectCount;
    readonly ArrayShapePoco4 poco = new();
    long expectedSum;

    [GlobalSetup]
    public void Setup()
    {
        blob = BuildBlob(Distribution, out objectCount);
        expectedSum = LoopSwitch();
        foreach (var (name, sum) in new (string, long)[]
        {
            (nameof(LoopIfElseChain), LoopIfElseChain()),
            (nameof(FlatIfLadder), FlatIfLadder()),
            (nameof(NestedIfLadder), NestedIfLadder()),
            (nameof(CountExactThenLoopSwitch), CountExactThenLoopSwitch()),
            (nameof(CountExactThenLadder), CountExactThenLadder()),
            (nameof(CountAtLeastThenLadder), CountAtLeastThenLadder()),
            (nameof(CountSwitchFullUnroll), CountSwitchFullUnroll()),
        })
        {
            if (sum != expectedSum)
            {
                throw new InvalidOperationException($"{name} checksum mismatch on {Distribution}: {sum} (expected {expectedSum})");
            }
        }
    }

    static byte[] BuildBlob(string distribution, out int objectCount)
    {
        var rand = new Random(42);
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            objectCount = ObjectCount;
            for (int o = 0; o < ObjectCount; o++)
            {
                var count = distribution switch
                {
                    "SameVersion" => FieldCount,
                    "OldWriter" => OldWriterCount,
                    "NewWriter" => NewWriterCount,
                    "MixedVersion" => rand.Next(3) switch { 0 => OldWriterCount, 1 => FieldCount, _ => NewWriterCount },
                    _ => throw new InvalidOperationException(distribution),
                };
                buffer.WriteArrayHeader(count);
                for (int i = 0; i < count; i++)
                {
                    buffer.WriteInt32(rand.Next(100_000, int.MaxValue));
                }
            }
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
        return writer.WrittenSpan.ToArray();
    }

    // ---- benchmarks -------------------------------------------------------

    [Benchmark(Baseline = true)]
    public long LoopSwitch()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadLoopSwitch(ref buffer, value);
            sum += value.Value0 + value.Value1 + value.Value3;
        }
        return sum;
    }

    [Benchmark]
    public long LoopIfElseChain()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadLoopIfElseChain(ref buffer, value);
            sum += value.Value0 + value.Value1 + value.Value3;
        }
        return sum;
    }

    [Benchmark]
    public long FlatIfLadder()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadFlatIfLadder(ref buffer, value);
            sum += value.Value0 + value.Value1 + value.Value3;
        }
        return sum;
    }

    [Benchmark]
    public long NestedIfLadder()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadNestedIfLadder(ref buffer, value);
            sum += value.Value0 + value.Value1 + value.Value3;
        }
        return sum;
    }

    [Benchmark]
    public long CountExactThenLoopSwitch()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadCountExactThenLoopSwitch(ref buffer, value);
            sum += value.Value0 + value.Value1 + value.Value3;
        }
        return sum;
    }

    [Benchmark]
    public long CountExactThenLadder()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadCountExactThenLadder(ref buffer, value);
            sum += value.Value0 + value.Value1 + value.Value3;
        }
        return sum;
    }

    [Benchmark]
    public long CountAtLeastThenLadder()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadCountAtLeastThenLadder(ref buffer, value);
            sum += value.Value0 + value.Value1 + value.Value3;
        }
        return sum;
    }

    [Benchmark]
    public long CountSwitchFullUnroll()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < objectCount; o++)
        {
            ReadCountSwitchFullUnroll(ref buffer, value);
            sum += value.Value0 + value.Value1 + value.Value3;
        }
        return sum;
    }

    // ---- shapes -----------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadLoopSwitch(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco4 value)
    {
        var count = buffer.ReadArrayHeader();
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.Value0 = buffer.ReadInt32(); break;
                case 1: value.Value1 = buffer.ReadInt32(); break;
                case 2: value.Value2 = buffer.ReadInt32(); break;
                case 3: value.Value3 = buffer.ReadInt32(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadLoopIfElseChain(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco4 value)
    {
        var count = buffer.ReadArrayHeader();
        for (int i = 0; i < count; i++)
        {
            if (i == 0) value.Value0 = buffer.ReadInt32();
            else if (i == 1) value.Value1 = buffer.ReadInt32();
            else if (i == 2) value.Value2 = buffer.ReadInt32();
            else if (i == 3) value.Value3 = buffer.ReadInt32();
            else buffer.Skip();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadFlatIfLadder(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco4 value)
    {
        var count = buffer.ReadArrayHeader();
        if (count > 0) value.Value0 = buffer.ReadInt32();
        if (count > 1) value.Value1 = buffer.ReadInt32();
        if (count > 2) value.Value2 = buffer.ReadInt32();
        if (count > 3) value.Value3 = buffer.ReadInt32();
        for (int i = 4; i < count; i++)
        {
            buffer.Skip();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadNestedIfLadder(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco4 value)
    {
        var count = buffer.ReadArrayHeader();
        if (count > 0) { value.Value0 = buffer.ReadInt32();
        if (count > 1) { value.Value1 = buffer.ReadInt32();
        if (count > 2) { value.Value2 = buffer.ReadInt32();
        if (count > 3) { value.Value3 = buffer.ReadInt32();
        for (int i = 4; i < count; i++)
        {
            buffer.Skip();
        }
        } } } }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadCountExactThenLoopSwitch(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco4 value)
    {
        var count = buffer.ReadArrayHeader();
        if (count == 4)
        {
            value.Value0 = buffer.ReadInt32();
            value.Value1 = buffer.ReadInt32();
            value.Value2 = buffer.ReadInt32();
            value.Value3 = buffer.ReadInt32();
            return;
        }
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0: value.Value0 = buffer.ReadInt32(); break;
                case 1: value.Value1 = buffer.ReadInt32(); break;
                case 2: value.Value2 = buffer.ReadInt32(); break;
                case 3: value.Value3 = buffer.ReadInt32(); break;
                default: buffer.Skip(); break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadCountExactThenLadder(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco4 value)
    {
        var count = buffer.ReadArrayHeader();
        if (count == 4)
        {
            value.Value0 = buffer.ReadInt32();
            value.Value1 = buffer.ReadInt32();
            value.Value2 = buffer.ReadInt32();
            value.Value3 = buffer.ReadInt32();
            return;
        }
        if (count > 0) value.Value0 = buffer.ReadInt32();
        if (count > 1) value.Value1 = buffer.ReadInt32();
        if (count > 2) value.Value2 = buffer.ReadInt32();
        if (count > 3) value.Value3 = buffer.ReadInt32();
        for (int i = 4; i < count; i++)
        {
            buffer.Skip();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadCountAtLeastThenLadder(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco4 value)
    {
        var count = buffer.ReadArrayHeader();
        if (count >= 4)
        {
            value.Value0 = buffer.ReadInt32();
            value.Value1 = buffer.ReadInt32();
            value.Value2 = buffer.ReadInt32();
            value.Value3 = buffer.ReadInt32();
            for (int i = 4; i < count; i++)
            {
                buffer.Skip();
            }
            return;
        }
        if (count > 0) value.Value0 = buffer.ReadInt32();
        if (count > 1) value.Value1 = buffer.ReadInt32();
        if (count > 2) value.Value2 = buffer.ReadInt32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadCountSwitchFullUnroll(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco4 value)
    {
        var count = buffer.ReadArrayHeader();
        switch (count)
        {
            case 0:
                break;
            case 1:
                value.Value0 = buffer.ReadInt32();
                break;
            case 2:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32();
                break;
            case 3:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32();
                break;
            case 4:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                break;
            default:
                value.Value0 = buffer.ReadInt32(); value.Value1 = buffer.ReadInt32(); value.Value2 = buffer.ReadInt32(); value.Value3 = buffer.ReadInt32();
                for (int i = 4; i < count; i++)
                {
                    buffer.Skip();
                }
                break;
        }
    }

    // ---- verification -----------------------------------------------------

    delegate void ReadShape4(ref ReadOnlySpanReadBuffer buffer, ArrayShapePoco4 value);

    public static void VerifyCandidates()
    {
        var shapes = new (string Name, ReadShape4 Read)[]
        {
            (nameof(ReadLoopSwitch), ReadLoopSwitch),
            (nameof(ReadLoopIfElseChain), ReadLoopIfElseChain),
            (nameof(ReadFlatIfLadder), ReadFlatIfLadder),
            (nameof(ReadNestedIfLadder), ReadNestedIfLadder),
            (nameof(ReadCountExactThenLoopSwitch), ReadCountExactThenLoopSwitch),
            (nameof(ReadCountExactThenLadder), ReadCountExactThenLadder),
            (nameof(ReadCountAtLeastThenLadder), ReadCountAtLeastThenLadder),
            (nameof(ReadCountSwitchFullUnroll), ReadCountSwitchFullUnroll),
        };

        for (int count = 0; count <= 12; count++)
        {
            var writer = new System.Buffers.ArrayBufferWriter<byte>();
            var write = new BufferWriterWriteBuffer(writer);
            try
            {
                write.WriteArrayHeader(count);
                for (int i = 0; i < count; i++)
                {
                    write.WriteInt32(ValueAt(count, i));
                }
                write.Flush();
            }
            finally
            {
                write.Dispose();
            }
            var payload = writer.WrittenSpan.ToArray();

            foreach (var (name, read) in shapes)
            {
                var value = new ArrayShapePoco4 { Value0 = -1, Value1 = -2, Value2 = -3, Value3 = -4 };
                var buffer = new ReadOnlySpanReadBuffer(payload);
                read(ref buffer, value);
                if (buffer.BytesRemaining != 0)
                {
                    throw new InvalidOperationException($"{name} count={count}: {buffer.BytesRemaining} bytes left unconsumed");
                }
                int[] fields = [value.Value0, value.Value1, value.Value2, value.Value3];
                for (int i = 0; i < 4; i++)
                {
                    var expected = i < count ? ValueAt(count, i) : -(i + 1);
                    if (fields[i] != expected)
                    {
                        throw new InvalidOperationException($"{name} count={count} field {i}: {fields[i]} (expected {expected})");
                    }
                }
            }
        }

        static int ValueAt(int count, int index) => 100_000 + count * 1000 + index * 7;
    }
}
