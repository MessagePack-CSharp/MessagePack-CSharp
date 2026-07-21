using BenchmarkDotNet.Attributes;
using SerializerFoundation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UltraMessagePack;

// Round 10: optimistic all-fixint decode for array-format POCOs. When every field of
// Int4Poco is a fixint the payload is exactly [0x94][b0][b1][b2][b3]: one 4-byte load,
// a SWAR "all fixint?" test, and the values are the bytes themselves — replacing four
// serial ~15-cycle decode links with ~10 straight-line cycles. The bet is data-dependent:
// a non-fixint field mispredicts the guard and pays the check plus the full per-value
// fallback. Per CLAUDE.md's predictor pitfall, 100k DISTINCT payloads per distribution
// (same-payload repetition would let Zen 5 memorize the branch outcome perfectly):
//   AllFix - every field in [-32, 127]: the optimistic bet always wins
//   Mixed  - every field uniformly random across encoding classes: bet almost always loses
//   Half   - 50/50 all-fix objects vs mixed objects: worst case for the predictor
// Populate-based (reusable instance) so allocation noise doesn't dilute the decode delta.
//
// Measured (2 runs): AllFix 0.18 / 0.22 (~5x), Half 0.82 / 0.69 (wins even at a 50% miss
// rate), Mixed 1.18 / 1.14 (~15% penalty when the bet never hits). A worthwhile generated
// fast path whenever a POCO's fields are typically small (ids, counts, enums, flags);
// the downside is bounded and the upside is the first result in this repo that actually
// breaks the ~15-cycle-per-value serial decode chain for POCOs.
//
// Round 5 addendum (PerValueCascade): the cascade primitive that won the contiguous micro
// loop 3.8x LOSES here end-to-end — 1.22 (AllFix) / 1.38 (Half) / 1.55 (Mixed) vs the
// per-value table baseline. Across independent payloads the table's chain latency hides
// under memory/entry overlap while cascade's branches and code size only add cost. The
// SWAR optimistic path remains the only shape that beats the table on real POCO streams.
public class DisasmProbe10Benchmark
{
    const int Count = 100_000;

    [Params("AllFix", "Mixed", "Half")]
    public string Distribution = "AllFix";

    byte[][] payloads = default!;
    Int4Poco reusable = default!;
    MessagePackSerializer perValue = default!;
    MessagePackSerializer optimistic = default!;
    MessagePackSerializer cascade = default!;
    MessagePackSerializer hybrid = default!;

    [GlobalSetup]
    public void Setup()
    {
        perValue = new MessagePackSerializer(new UltraMessagePack.MessagePackFormatterResolver(new Int4PerValueFormatterFactory()));
        optimistic = new MessagePackSerializer(new UltraMessagePack.MessagePackFormatterResolver(new Int4OptimisticFormatterFactory()));
        cascade = new MessagePackSerializer(new UltraMessagePack.MessagePackFormatterResolver(new Int4CascadeReadFormatterFactory()));
        hybrid = new MessagePackSerializer(new UltraMessagePack.MessagePackFormatterResolver(new Int4HybridReadFormatterFactory()));
        reusable = new Int4Poco();

        var rand = new Random(42);
        int NextMixed() => rand.Next(8) switch
        {
            0 => rand.Next(0, 128),
            1 => rand.Next(-32, 0),
            2 => rand.Next(128, 256),
            3 => rand.Next(-128, -32),
            4 => rand.Next(256, 65536),
            5 => rand.Next(-32768, -128),
            6 => rand.Next(65536, int.MaxValue),
            _ => rand.Next(int.MinValue, -32768),
        };
        int NextFix() => rand.Next(-32, 128);

        payloads = new byte[Count][];
        for (int i = 0; i < Count; i++)
        {
            var allFix = Distribution switch
            {
                "AllFix" => true,
                "Mixed" => false,
                _ => rand.Next(2) == 0,
            };
            var poco = allFix
                ? new Int4Poco { A = NextFix(), B = NextFix(), C = NextFix(), D = NextFix() }
                : new Int4Poco { A = NextMixed(), B = NextMixed(), C = NextMixed(), D = NextMixed() };
            payloads[i] = MessagePack.MessagePackSerializer.Serialize(poco);
        }

        // optimistic and cascade must agree with per-value on every payload
        var a = new Int4Poco();
        var b = new Int4Poco();
        var c = new Int4Poco();
        for (int i = 0; i < Count; i++)
        {
            perValue.Deserialize(ref a, payloads[i]);
            optimistic.Deserialize(ref b, payloads[i]);
            cascade.Deserialize(ref c, payloads[i]);
            if (!a.Equals4(b)) throw new InvalidOperationException($"verify failed: optimistic payload {i}");
            if (!a.Equals4(c)) throw new InvalidOperationException($"verify failed: cascade payload {i}");
            hybrid.Deserialize(ref c, payloads[i]);
            if (!a.Equals4(c)) throw new InvalidOperationException($"verify failed: hybrid payload {i}");
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int PerValue()
    {
        int sum = 0;
        for (int i = 0; i < Count; i++)
        {
            perValue.Deserialize(ref reusable, payloads[i]);
            sum += reusable.A;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Optimistic()
    {
        int sum = 0;
        for (int i = 0; i < Count; i++)
        {
            optimistic.Deserialize(ref reusable, payloads[i]);
            sum += reusable.A;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int PerValueCascade()
    {
        int sum = 0;
        for (int i = 0; i < Count; i++)
        {
            cascade.Deserialize(ref reusable, payloads[i]);
            sum += reusable.A;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int PerValueHybrid()
    {
        int sum = 0;
        for (int i = 0; i < Count; i++)
        {
            hybrid.Deserialize(ref reusable, payloads[i]);
            sum += reusable.A;
        }
        return sum;
    }
}

// fixint-first branch + library table fallback, per field (the read-side mirror of the
// write side's fixint fast path); same per-value shape as Int4PerValueFormatter
public sealed class Int4HybridReadFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Int4Poco>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref Int4Poco value)
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
        value.A = ReadInt32Hybrid(ref buffer);
        value.B = ReadInt32Hybrid(ref buffer);
        value.C = ReadInt32Hybrid(ref buffer);
        value.D = ReadInt32Hybrid(ref buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int ReadInt32Hybrid(ref TReadBuffer buffer)
    {
        if (global::MessagePackPrimitives.TryReadInt32HybridDr(buffer.GetSpan(), out var v, out var consumed) == DecodeResult.Success)
        {
            buffer.Advance(consumed);
            return v;
        }
        Int4Throws.InvalidCount(-1);
        return 0;
    }
}

public sealed class Int4HybridReadFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return new Int4HybridReadFormatter<TWriteBuffer, TReadBuffer>();
    }
}

// Round 5 rematch end-to-end probe: same per-value shape as Int4PerValueFormatter but the
// four int fields decode through the cascade primitive (speculation-friendly). Span-buffer
// only (no stitched slow path) — enough for the probe's contiguous payloads.
public sealed class Int4CascadeReadFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Int4Poco>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref Int4Poco value)
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
        value.A = ReadInt32Cascade(ref buffer);
        value.B = ReadInt32Cascade(ref buffer);
        value.C = ReadInt32Cascade(ref buffer);
        value.D = ReadInt32Cascade(ref buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int ReadInt32Cascade(ref TReadBuffer buffer)
    {
        if (global::MessagePackPrimitives.TryReadInt32CascadeDr(buffer.GetSpan(), out var v, out var consumed) == DecodeResult.Success)
        {
            buffer.Advance(consumed);
            return v;
        }
        Int4Throws.InvalidCount(-1);
        return 0;
    }
}

public sealed class Int4CascadeReadFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return new Int4CascadeReadFormatter<TWriteBuffer, TReadBuffer>();
    }
}

public sealed class Int4OptimisticFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Int4Poco>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref Int4Poco value)
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

        var span = buffer.GetSpan();
        if (span.Length >= 5)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            // [0x94][b0][b1][b2][b3] when all four fields are fixint
            uint x = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1));
            // SWAR all-fixint test: a byte is BAD when its high bit is set but bits 6..5
            // are not both set (0x80-0xdf); fixint is 0x00-0x7f or 0xe0-0xff
            uint high = x & 0x80808080u;
            uint good56 = (x & 0x40404040u) & ((x & 0x20202020u) << 1); // 0x40 where bits 6&5 set
            uint bad = high & ~(good56 << 1);
            if (s == 0x94 && bad == 0)
            {
                value.A = (sbyte)x;
                value.B = (sbyte)(x >> 8);
                value.C = (sbyte)(x >> 16);
                value.D = (sbyte)(x >> 24);
                buffer.Advance(5);
                return;
            }
        }
        DeserializeSlow(ref buffer, value);
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

public sealed class Int4OptimisticFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return new Int4OptimisticFormatter<TWriteBuffer, TReadBuffer>();
    }
}
