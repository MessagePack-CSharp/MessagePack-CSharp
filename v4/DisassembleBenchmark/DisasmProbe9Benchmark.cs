using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using MessagePack;
using SerializerFoundation;
using System.Buffers;
using System.Runtime.CompilerServices;
using UltraMessagePack;
using static UltraMessagePack.MessagePackPrimitives;

// Round 9: the two source-gen output shapes for a 4-int POCO, on both directions.
//   PerValue - current style: one GetReference/Advance (write) or GetSpan/Advance (read)
//              per value, buffer state touched 5x per object
//   Batch    - write: one GetReference(header+4*MaxInt32) reservation, register-resident
//              offset, one Advance. read: one GetSpan() window, TryRead chain accumulating
//              a local offset, one Advance; any TryRead failure falls back to the
//              per-value path (which handles sequence straddling via the stitched retry),
//              nothing consumed before the fallback so it simply re-reads from the start.
// Read-side note: a read batch cannot reserve like the write side (GetSpan(sum) would
// reject valid data ending short) — the window is "whatever remains" and the fallback
// covers the rest. The consumed->next-address serial chain survives batching either way.
//
// DeserializeLoopSwitch (MessagePack-CSharp's for+switch versioning shape) measured 1.02 /
// 1.04 vs straight-line — with count fixed the switch pattern predicts perfectly and hides
// behind the decode chain, so the generated deserialize shape is a readability choice,
// not a performance one.
//
// Measured (2 runs): SerializeBatch 0.89 / 0.84 — a reproducible -11..16% win, unlike
// round 8's BenchPerson where the string transcode diluted the same saving to nothing.
// DeserializeBatch 1.01 / 1.02 — flat as predicted (the serial chain dominates), but not
// slower, and straddle handling collapses to one fallback per object. Conclusion for
// source-gen output: batch the write side over runs of fixed-max-size members; read side
// is a style choice (per-value is simpler and handles count-mismatch versioning naturally).
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DisasmProbe9Benchmark
{
    Int4Poco int4 = default!;
    byte[] payload = default!;
    UltraMessagePack.MessagePackSerializer perValue = default!;
    UltraMessagePack.MessagePackSerializer batch = default!;
    UltraMessagePack.MessagePackSerializer loopSwitch = default!;
    Int4Poco reusable = default!;

    [GlobalSetup]
    public void Setup()
    {
        // one value per encoding class: fixint(1B), uint16(3B), int32(5B), int8(2B)
        int4 = new Int4Poco { A = 1, B = 300, C = -70000, D = -50 };
        perValue = new UltraMessagePack.MessagePackSerializer(new UltraMessagePack.MessagePackFormatterResolver(new Int4PerValueFormatterFactory()));
        batch = new UltraMessagePack.MessagePackSerializer(new UltraMessagePack.MessagePackFormatterResolver(new Int4BatchFormatterFactory()));
        loopSwitch = new UltraMessagePack.MessagePackSerializer(new UltraMessagePack.MessagePackFormatterResolver(new Int4LoopSwitchFormatterFactory()));
        payload = MessagePack.MessagePackSerializer.Serialize(int4);

        // byte identity against MessagePack-CSharp for both write shapes
        foreach (var (name, actual) in new (string, byte[])[]
        {
            (nameof(SerializePerValue), SerializePerValue()),
            (nameof(SerializeBatch), SerializeBatch()),
        })
        {
            if (!actual.AsSpan().SequenceEqual(payload)) throw new InvalidOperationException($"verify failed: {name}");
        }

        // both read shapes from the oracle payload
        foreach (var (name, actual) in new (string, Int4Poco)[]
        {
            (nameof(DeserializePerValue), DeserializePerValue()),
            (nameof(DeserializeBatch), DeserializeBatch()),
            (nameof(DeserializeLoopSwitch), DeserializeLoopSwitch()),
        })
        {
            if (!actual.Equals4(int4)) throw new InvalidOperationException($"verify failed: {name}");
        }

        // populate overload: same instance must be reused (no allocation) with fields updated
        reusable = new Int4Poco();
        var before = reusable;
        perValue.Deserialize(ref reusable, payload);
        if (!ReferenceEquals(before, reusable) || !reusable.Equals4(int4)) throw new InvalidOperationException("verify failed: populate");

        // the batch read's fallback must survive a value straddling a sequence segment
        // boundary: split the payload at every position
        for (int splitAt = 0; splitAt <= payload.Length; splitAt++)
        {
            var sequence = splitAt == payload.Length
                ? new ReadOnlySequence<byte>(payload)
                : NbSequenceSegment.CreateSplit(payload, splitAt);
            var v = batch.Deserialize<Int4Poco>(sequence);
            if (!v.Equals4(int4)) throw new InvalidOperationException($"verify failed: batch sequence splitAt={splitAt}");
        }
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializePerValue() => perValue.Serialize(int4);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeBatch() => batch.Serialize(int4);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public Int4Poco DeserializePerValue() => perValue.Deserialize<Int4Poco>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Int4Poco DeserializeBatch() => batch.Deserialize<Int4Poco>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Int4Poco DeserializeLoopSwitch() => loopSwitch.Deserialize<Int4Poco>(payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public Int4Poco DeserializePopulate()
    {
        perValue.Deserialize(ref reusable, payload);
        return reusable;
    }
}

[MessagePackObject]
public class Int4Poco
{
    [Key(0)] public int A { get; set; }
    [Key(1)] public int B { get; set; }
    [Key(2)] public int C { get; set; }
    [Key(3)] public int D { get; set; }

    public bool Equals4(Int4Poco other) => A == other.A && B == other.B && C == other.C && D == other.D;
}

static class Int4Throws
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InvalidCount(int count) => throw new UltraMessagePack.MessagePackSerializationException($"Invalid array count for Int4Poco: {count}");
}

// hands out exactly one formatter factory; keeps each probe serializer's resolution chain
// independent of the shared Dynamic singleton

// ---- per-value shape (current formatter style) ----

public sealed class Int4PerValueFormatter<TWriteBuffer, TReadBuffer> : UltraMessagePack.IMessagePackFormatter<TWriteBuffer, TReadBuffer, Int4Poco>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(UltraMessagePack.MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref UltraMessagePack.SerializeState state, ref Int4Poco value)
    {
        buffer.WriteArrayHeader(4);
        buffer.WriteInt32(value.A);
        buffer.WriteInt32(value.B);
        buffer.WriteInt32(value.C);
        buffer.WriteInt32(value.D);
    }

    public void Deserialize(ref TReadBuffer buffer, ref UltraMessagePack.DeserializeState state, ref Int4Poco value)
    {
        if (value == null)
        {
            value = new Int4Poco();
        }

        var count = buffer.ReadArrayHeader();
        if (count != 4) Int4Throws.InvalidCount(count);
        value.A = buffer.ReadInt32();
        value.B = buffer.ReadInt32();
        value.C = buffer.ReadInt32();
        value.D = buffer.ReadInt32();
    }
}

public sealed class Int4PerValueFormatterFactory : UltraMessagePack.IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return new Int4PerValueFormatter<TWriteBuffer, TReadBuffer>();
    }
}

// ---- MessagePack-CSharp-style versioning loop shape ----
// what their source generator emits: for + switch(i) so schema evolution (count != N)
// falls out of the same loop. Costs loop control + a switch dispatch per field on the
// hot path; the straight-line shape hoists versioning into a cold count != N branch.

public sealed class Int4LoopSwitchFormatter<TWriteBuffer, TReadBuffer> : UltraMessagePack.IMessagePackFormatter<TWriteBuffer, TReadBuffer, Int4Poco>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(UltraMessagePack.MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref UltraMessagePack.SerializeState state, ref Int4Poco value)
    {
        buffer.WriteArrayHeader(4);
        buffer.WriteInt32(value.A);
        buffer.WriteInt32(value.B);
        buffer.WriteInt32(value.C);
        buffer.WriteInt32(value.D);
    }

    public void Deserialize(ref TReadBuffer buffer, ref UltraMessagePack.DeserializeState state, ref Int4Poco value)
    {
        if (value == null)
        {
            value = new Int4Poco();
        }

        var count = buffer.ReadArrayHeader();
        for (int i = 0; i < count; i++)
        {
            switch (i)
            {
                case 0:
                    value.A = buffer.ReadInt32();
                    break;
                case 1:
                    value.B = buffer.ReadInt32();
                    break;
                case 2:
                    value.C = buffer.ReadInt32();
                    break;
                case 3:
                    value.D = buffer.ReadInt32();
                    break;
                default:
                    break;
            }
        }
    }
}

public sealed class Int4LoopSwitchFormatterFactory : UltraMessagePack.IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return new Int4LoopSwitchFormatter<TWriteBuffer, TReadBuffer>();
    }
}

// ---- batched shape ----

public sealed class Int4BatchFormatter<TWriteBuffer, TReadBuffer> : UltraMessagePack.IMessagePackFormatter<TWriteBuffer, TReadBuffer, Int4Poco>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(UltraMessagePack.MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref UltraMessagePack.SerializeState state, ref Int4Poco value)
    {
        // one worst-case reservation, register-resident offset, one Advance
        ref byte d = ref buffer.GetReference(MaxArrayHeaderLength + 4 * MaxInt32Length);
        var offset = UnsafeWriteArrayHeader(ref d, 4);
        offset += UnsafeWriteInt32(ref Unsafe.Add(ref d, offset), value.A);
        offset += UnsafeWriteInt32(ref Unsafe.Add(ref d, offset), value.B);
        offset += UnsafeWriteInt32(ref Unsafe.Add(ref d, offset), value.C);
        offset += UnsafeWriteInt32(ref Unsafe.Add(ref d, offset), value.D);
        buffer.Advance(offset);
    }

    public void Deserialize(ref TReadBuffer buffer, ref UltraMessagePack.DeserializeState state, ref Int4Poco value)
    {
        if (value == null)
        {
            value = new Int4Poco();
        }

        // one window, local offset, one Advance; no reservation possible on the read side
        // (valid data may end short), so any short/straddling window falls back wholesale —
        // nothing was consumed yet, the fallback re-reads from the start
        var span = buffer.GetSpan();
        if (TryReadArrayHeader(span, out var count, out var offset) != DecodeResult.Success || count != 4) { DeserializeSlow(ref buffer, value); return; }
        if (TryReadInt32(span.Slice(offset), out var a, out var c) != DecodeResult.Success) { DeserializeSlow(ref buffer, value); return; }
        offset += c;
        if (TryReadInt32(span.Slice(offset), out var b, out c) != DecodeResult.Success) { DeserializeSlow(ref buffer, value); return; }
        offset += c;
        if (TryReadInt32(span.Slice(offset), out var c3, out c) != DecodeResult.Success) { DeserializeSlow(ref buffer, value); return; }
        offset += c;
        if (TryReadInt32(span.Slice(offset), out var d, out c) != DecodeResult.Success) { DeserializeSlow(ref buffer, value); return; }
        offset += c;

        buffer.Advance(offset);
        value.A = a;
        value.B = b;
        value.C = c3;
        value.D = d;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DeserializeSlow(ref TReadBuffer buffer, Int4Poco value)
    {
        var count = buffer.ReadArrayHeader();
        if (count != 4) Int4Throws.InvalidCount(count);
        value.A = buffer.ReadInt32();
        value.B = buffer.ReadInt32();
        value.C = buffer.ReadInt32();
        value.D = buffer.ReadInt32();
    }
}

public sealed class Int4BatchFormatterFactory : UltraMessagePack.IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return new Int4BatchFormatter<TWriteBuffer, TReadBuffer>();
    }
}
