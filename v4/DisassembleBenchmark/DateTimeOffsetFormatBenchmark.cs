using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using SerializerFoundation;
using UltraMessagePack;
using UltraMessagePack.Formatters;

namespace DisassembleBenchmark;

// Was removing DotNetOptimizedDateTimeOffsetFormatter justified? Intuition puzzle: the
// DateTime pair (timestamp ext vs ToBinary int64) clearly favors ToBinary, yet
// DateTimeOffset — the same instant PLUS an offset — seemed to gain nothing from the
// analogous constant-shape [forced int64 ticks, forced int16 offset] format.
// Four formatter bodies measured over real buffers: the default DTO formatter
// (fixarray2 + WriteTimestamp cascade + int16), the constant-shape DTO
// (13-byte single-reservation fused write, constant-shape validated read), and the
// DateTime pair for context. Values: modern instants with sub-second ticks (the ts64
// class), random whole-minute offsets in +-14h.
//
// MEASURED round 1 (i7-13700KF, ShortRun, ns/op, N=100k):
//   Write: DtoDefault 6.88 | DtoConstantShape 1.37 (0.20x) | DateTimeTimestamp 2.36 | DateTimeToBinary 1.46
//   Read:  DtoDefault 7.42 | DtoConstantShape 2.58 (0.35x) | DateTimeTimestamp 1.84 | DateTimeToBinary 5.02 (!)
// Verdict: the constant shape's win is the tier's magnitude test passed on PERFORMANCE
// (5x write / 2.9x read) — the formatter was reinstated into DotNetOptimized on these
// numbers. Bonus finding: ToBinary READ was 2.7x slower than the timestamp read because
// DateTime.FromBinary is a non-inlined call and the exception wrap kept EH in the hot
// method → replaced with bit-direct decode for Utc/Unspecified (round 2 below measures it).
//
// MEASURED round 2 (bit-direct decode in the library formatter):
//   Read: DateTimeToBinary 5.02 -> 3.31 (code 1,185 B -> 632 B; EH and calls gone).
//   Remaining gap vs timestamp's 1.84: buffer.ReadInt64's smallest-format decoder — the
//   descriptor-table movbe/shrx/shlx/sarx cascade plus the fixint pre-branch.
// MEASURED round 3 (fused 9-byte read: header check 0xd3 / 0xcf-with-bit63-clear ->
// single big-endian load, generic ReadInt64 kept as the fallback):
//   Read: DateTimeToBinary 3.31 -> 1.26 — now BELOW the timestamp read (1.84). Hot loop
//   is two predictable header compares + movbe + mask/range/kind-pack, no calls, no EH.
//   Total: 5.02 -> 1.26 (4.0x); write unchanged at ~1.46-1.49.
// MEASURED round 4 (write switched to forced int64 — constant 0xd3 9-byte shape,
// dropping the smallest-format width dispatch; v3 byte-identity traded for it):
//   Write: DateTimeToBinary 1.46 -> 0.57 (0.08x of baseline; code 563 B -> 435 B).
//   Read: 1.26 -> 1.17 — every value now hits the first (0xd3) fast-path arm.
public class DateTimeOffsetWriteBenchmark
{
    const int Count = 100_000;

    DateTimeOffset[] values = default!;
    DateTime[] dateTimeValues = default!;
    byte[] writeBuffer = default!;
    int maxDepth;

    DateTimeOffsetFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer> defaultFormatter = default!;
    DotNetOptimizedDateTimeOffsetFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer> constantFormatter = default!;
    DateTimeFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer> timestampFormatter = default!;
    DotNetOptimizedDateTimeFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer> toBinaryFormatter = default!;

    [GlobalSetup]
    public void Setup()
    {
        (values, dateTimeValues) = DateTimeOffsetBenchData.Create(Count);
        writeBuffer = new byte[Count * 16];
        maxDepth = MessagePackSerializerOptions.Default.MaxDepth;

        var resolver = new MessagePackFormatterResolver(BuiltInFormatterFactory.Instance);
        defaultFormatter = new DateTimeOffsetFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer>();
        defaultFormatter.Initialize(resolver);
        constantFormatter = new DotNetOptimizedDateTimeOffsetFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer>();
        constantFormatter.Initialize(resolver);
        timestampFormatter = new DateTimeFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer>();
        timestampFormatter.Initialize(resolver);
        toBinaryFormatter = new DotNetOptimizedDateTimeFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer>();
        toBinaryFormatter.Initialize(resolver);

        // correctness before speed: both DTO forms must roundtrip exactly
        DateTimeOffsetBenchData.VerifyRoundtrip(values, defaultFormatter, constantFormatter, maxDepth);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public long DtoDefault()
    {
        var buffer = new SpanWriteBuffer(writeBuffer);
        var state = new SerializeState(maxDepth);
        var f = defaultFormatter;
        var vals = values;
        for (int i = 0; i < vals.Length; i++)
        {
            f.Serialize(ref buffer, ref state, vals[i]);
        }
        return buffer.BytesWritten;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long DtoConstantShape()
    {
        var buffer = new SpanWriteBuffer(writeBuffer);
        var state = new SerializeState(maxDepth);
        var f = constantFormatter;
        var vals = values;
        for (int i = 0; i < vals.Length; i++)
        {
            f.Serialize(ref buffer, ref state, vals[i]);
        }
        return buffer.BytesWritten;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long DateTimeTimestamp()
    {
        var buffer = new SpanWriteBuffer(writeBuffer);
        var state = new SerializeState(maxDepth);
        var f = timestampFormatter;
        var vals = dateTimeValues;
        for (int i = 0; i < vals.Length; i++)
        {
            f.Serialize(ref buffer, ref state, vals[i]);
        }
        return buffer.BytesWritten;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long DateTimeToBinary()
    {
        var buffer = new SpanWriteBuffer(writeBuffer);
        var state = new SerializeState(maxDepth);
        var f = toBinaryFormatter;
        var vals = dateTimeValues;
        for (int i = 0; i < vals.Length; i++)
        {
            f.Serialize(ref buffer, ref state, vals[i]);
        }
        return buffer.BytesWritten;
    }
}

public class DateTimeOffsetReadBenchmark
{
    const int Count = 100_000;

    byte[] defaultBytes = default!;
    byte[] constantBytes = default!;
    byte[] timestampBytes = default!;
    byte[] toBinaryBytes = default!;
    int maxDepth;

    DateTimeOffsetFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer> defaultFormatter = default!;
    DotNetOptimizedDateTimeOffsetFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer> constantFormatter = default!;
    DateTimeFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer> timestampFormatter = default!;
    DotNetOptimizedDateTimeFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer> toBinaryFormatter = default!;

    [GlobalSetup]
    public void Setup()
    {
        var (values, dateTimeValues) = DateTimeOffsetBenchData.Create(Count);
        maxDepth = MessagePackSerializerOptions.Default.MaxDepth;

        var resolver = new MessagePackFormatterResolver(BuiltInFormatterFactory.Instance);
        defaultFormatter = new DateTimeOffsetFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer>();
        defaultFormatter.Initialize(resolver);
        constantFormatter = new DotNetOptimizedDateTimeOffsetFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer>();
        constantFormatter.Initialize(resolver);
        timestampFormatter = new DateTimeFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer>();
        timestampFormatter.Initialize(resolver);
        toBinaryFormatter = new DotNetOptimizedDateTimeFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer>();
        toBinaryFormatter.Initialize(resolver);

        defaultBytes = DateTimeOffsetBenchData.SerializeAll(values, defaultFormatter, maxDepth, Count * 16);
        constantBytes = DateTimeOffsetBenchData.SerializeAll(values, constantFormatter, maxDepth, Count * 16);
        timestampBytes = DateTimeOffsetBenchData.SerializeAll(dateTimeValues, timestampFormatter, maxDepth, Count * 16);
        toBinaryBytes = DateTimeOffsetBenchData.SerializeAll(dateTimeValues, toBinaryFormatter, maxDepth, Count * 16);

        DateTimeOffsetBenchData.VerifyRoundtrip(values, defaultFormatter, constantFormatter, maxDepth);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public long DtoDefault()
    {
        var buffer = new ReadOnlySpanReadBuffer(defaultBytes);
        var state = new DeserializeState(maxDepth);
        var f = defaultFormatter;
        long acc = 0;
        for (int i = 0; i < Count; i++)
        {
            DateTimeOffset v = default;
            f.Deserialize(ref buffer, ref state, ref v);
            acc += v.Ticks;
        }
        return acc;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long DtoConstantShape()
    {
        var buffer = new ReadOnlySpanReadBuffer(constantBytes);
        var state = new DeserializeState(maxDepth);
        var f = constantFormatter;
        long acc = 0;
        for (int i = 0; i < Count; i++)
        {
            DateTimeOffset v = default;
            f.Deserialize(ref buffer, ref state, ref v);
            acc += v.Ticks;
        }
        return acc;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long DateTimeTimestamp()
    {
        var buffer = new ReadOnlySpanReadBuffer(timestampBytes);
        var state = new DeserializeState(maxDepth);
        var f = timestampFormatter;
        long acc = 0;
        for (int i = 0; i < Count; i++)
        {
            DateTime v = default;
            f.Deserialize(ref buffer, ref state, ref v);
            acc += v.Ticks;
        }
        return acc;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long DateTimeToBinary()
    {
        var buffer = new ReadOnlySpanReadBuffer(toBinaryBytes);
        var state = new DeserializeState(maxDepth);
        var f = toBinaryFormatter;
        long acc = 0;
        for (int i = 0; i < Count; i++)
        {
            DateTime v = default;
            f.Deserialize(ref buffer, ref state, ref v);
            acc += v.Ticks;
        }
        return acc;
    }
}

static class DateTimeOffsetBenchData
{
    internal static (DateTimeOffset[], DateTime[]) Create(int count)
    {
        var rand = new Random(42);
        var values = new DateTimeOffset[count];
        var dateTimes = new DateTime[count];
        for (int i = 0; i < count; i++)
        {
            // modern instants with sub-second ticks (the fixext8/ts64 class in the
            // timestamp cascade) and whole-minute offsets across the +-14h range
            var ticks = new DateTime(2020, 1, 1).Ticks + rand.NextInt64(0, TimeSpan.TicksPerDay * 3650) + rand.Next(1, 10_000_000);
            var offsetMinutes = rand.Next(-56, 57) * 15; // -840..840 in quarter hours
            values[i] = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));
            dateTimes[i] = new DateTime(ticks, DateTimeKind.Utc);
        }
        return (values, dateTimes);
    }

    internal static byte[] SerializeAll<TFormatter, T>(T[] values, TFormatter formatter, int maxDepth, int capacity)
        where TFormatter : IMessagePackFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer, T>
    {
        var storage = new byte[capacity];
        var buffer = new SpanWriteBuffer(storage);
        var state = new SerializeState(maxDepth);
        for (int i = 0; i < values.Length; i++)
        {
            formatter.Serialize(ref buffer, ref state, values[i]);
        }
        return storage.AsSpan(0, (int)buffer.BytesWritten).ToArray();
    }

    internal static void VerifyRoundtrip(
        DateTimeOffset[] values,
        DateTimeOffsetFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer> defaultFormatter,
        DotNetOptimizedDateTimeOffsetFormatter<SpanWriteBuffer, ReadOnlySpanReadBuffer> constantFormatter,
        int maxDepth)
    {
        var defaultBytes = SerializeAll(values, defaultFormatter, maxDepth, values.Length * 16);
        var constantBytes = SerializeAll(values, constantFormatter, maxDepth, values.Length * 16);
        var b1 = new ReadOnlySpanReadBuffer(defaultBytes);
        var b2 = new ReadOnlySpanReadBuffer(constantBytes);
        var s = new DeserializeState(maxDepth);
        for (int i = 0; i < values.Length; i++)
        {
            DateTimeOffset v1 = default, v2 = default;
            defaultFormatter.Deserialize(ref b1, ref s, ref v1);
            constantFormatter.Deserialize(ref b2, ref s, ref v2);
            if (!values[i].EqualsExact(v1) || !values[i].EqualsExact(v2))
            {
                throw new InvalidOperationException($"roundtrip mismatch at {i}: {values[i]:O} -> default {v1:O} / constant {v2:O}");
            }
        }
    }
}
