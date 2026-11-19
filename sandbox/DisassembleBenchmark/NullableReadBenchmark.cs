using BenchmarkDotNet.Attributes;
using MessagePack;
using SerializerFoundation;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Instruction count of a nullable primitive member read. AnswerDeserializeBreakdownBenchmark found Answer's
// deserialize instruction-bound (IPC about 4, 10 mispredicts per op) with 61 int? / bool? members, and the
// generated read for one int? member is 86 instructions in Tier1 (BadgeCountFormatter, uint16-coded value):
//   loop + switch dispatch 11, TryReadNil 10, a second GetUnreadSpan + emptiness test 9, four stack temps zeroed
//   for the cold calls' out parameters 4, fixint test 5, table decode 31, the DecodeResult rebuilt and re-tested
//   (sete / movzx / test / je, test / je, xor, test / jne) 8, Advance 7, the Nullable<int> store 4.
// The generated expression is `buffer.TryReadNil() ? default(int?) : buffer.ReadInt32()`: two readers, each opening
// its own window on the buffer, and ReadInt32 reporting through (value, tokenSize, DecodeResult) out parameters
// that the inlined caller turns back into branches.
//
//   Shipped  - the expression the generator emitted until the readers were adopted (kept here as the baseline)
//   Library  - ReadBufferExtensions.ReadNullableInt32, the adopted reader (InlineReturningNullable's shape)
//   PeekOnce - one window: nil test, then the shipped TryReadInt32 on the same span
//   InlineReturningNullable - Inline behind a reader that returns int? through a single return expression
//   Inline   - one window, the code byte loaded once: fixint, nil, then the table decode written out with the
//              range check as its only exit (no out parameters, nothing on the stack); anything else, including
//              a window shorter than 5 bytes behind a non-fixint code, re-reads through the shipped pair
// The cold path of both candidates is the shipped expression, called after nothing was consumed, so every
// failure (wrong token, out of range, truncation, a straddled sequence) behaves exactly as today.
//
// Shape of the candidates: `bool TryRead...OrNil(out T value)`, composed at the call site as
// `buffer.ReadInt32OrNilInline(out var v) ? v : default(int?)`. The first version returned int? from the helper
// and measured 1.4-2.6x SLOWER than Shipped on every width with fewer instructions: an inlined method with several
// `return` points gets its Nullable<int> result in one unpromoted stack temp, written as a byte (hasValue) and an
// int (value) and read back as one 8-byte load to store into the field. Two narrow stores cannot forward to a
// wider load, so every member paid a store-forwarding stall (~5 ns flat). Shipped never builds the struct: its
// `cond ? default : value` keeps hasValue and value in two registers and stores the two fields directly. The cold
// helpers return one packed scalar for the same reason (an out parameter would pin the caller's local to the stack).
//
// Blob loop as in IntRunFuseControlBenchmark: 50k objects of three int? members in the generated for / switch shape.
// Width: Fixint, UInt16 (Answer's badge counts and scores), UInt32 (its ids), Mixed (1 / 2 / 3 / 5 bytes),
// NilThird (a third of the members nil, the rest uint16).
//
// MEASURED (i7-13700KF, .NET 11; time from a non-elevated ShortRun, instructions from one elevated run whose times
// were inflated and noisy and are not used; ratios to Shipped, instructions per member over 150k members):
//   Fixint    PeekOnce 0.89   Inline 0.70     -5.7 / -20.9 instructions per member
//   UInt16    PeekOnce 1.01   Inline 0.89     -6.7 / -22.6
//   UInt32    PeekOnce 0.98   Inline 0.88     -8.2 / -23.9
//   Mixed     PeekOnce 0.99   Inline 0.91     -7.2 / -22.9   (40k mispredicts per 150k members in all three shapes)
//   NilThird  PeekOnce 1.02   Inline 0.91     -2.6 / -12.3
// Sharing the window alone (PeekOnce) saves 6-8 instructions and no measurable time; the saving is in writing the
// decode out so the caller sees branches instead of a DecodeResult, with nothing on the stack. End to end on Answer
// (AnswerNullableReadEndToEndBenchmark): 20,299 -> 19,049 instructions (-6.2%, -20.5 per member over 61 members),
// time 0.95 / 0.94 in two clean runs and 1.01 in a third whose candidate row had a StdDev of 70 ns.
// Library row (the adopted reader, after the adoption; time not elevated, instructions elevated), ratio to Shipped:
//   Fixint 0.68 (-24.5 instructions per member)   UInt16 0.87 (-26.3)   UInt32 0.91 (-26.4)   Mixed 0.97 (-26.9)
//   NilThird 0.89 (-16.0)
//
// ADOPTED: ReadBufferExtensions.ReadNullableX (twelve readers, each through a one-window ReadXOrNil helper of this
// Inline shape; the other kinds are measured in NullableReaderPairBenchmark), and
// the generator emits them for Nullable<primitive> members. The candidates stay here under their own names; the
// helpers were first called TryRead...OrNil and renamed, because a Try that throws on a wrong token breaks the
// library's convention (TryRead never throws, Read does).
//
// InlineReturningNullable (a later run, ratios to Shipped): Fixint 0.67, UInt16 0.90, UInt32 0.85, Mixed 0.90,
// NilThird 0.92; Inline in the same run 0.71 / 0.91 / 0.88 / 0.91 / 0.93. So a reader may return int? as long as it
// has ONE return expression of the `cond ? value : default` form: the stall belongs to several return points, not to
// the return type, and the generated member read can be the single call `buffer.ReadNullableInt32()`.
// Measured on the .NET 11 JIT only.
public class NullableReadBenchmark
{
    [Params("Fixint", "UInt16", "UInt32", "Mixed", "NilThird")]
    public string Width = "UInt16";

    const int ObjectCount = 50_000;

    byte[] blob = null!;
    readonly NullableInt3 poco = new();

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
                    if (Width == "NilThird" && rand.Next(3) == 0)
                    {
                        buffer.WriteNil();
                        continue;
                    }
                    var widthClass = Width switch
                    {
                        "Fixint" => 0,
                        "UInt16" or "NilThird" => 2,
                        "UInt32" => 3,
                        "Mixed" => rand.Next(4),
                        _ => throw new InvalidOperationException(Width),
                    };
                    buffer.WriteInt32(widthClass switch
                    {
                        0 => rand.Next(0, 128),
                        1 => rand.Next(128, 256),
                        2 => rand.Next(256, 65536),
                        _ => rand.Next(100_000, int.MaxValue),
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

        var expected = Shipped();
        if (PeekOnce() != expected || Inline() != expected || InlineReturningNullable() != expected || Library() != expected)
        {
            throw new InvalidOperationException($"checksum mismatch on {Width}");
        }
    }

    [Benchmark(Baseline = true)]
    public long Shipped()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.TryReadNil() ? default(int?) : buffer.ReadInt32(); break;
                    case 1: value.B = buffer.TryReadNil() ? default(int?) : buffer.ReadInt32(); break;
                    case 2: value.C = buffer.TryReadNil() ? default(int?) : buffer.ReadInt32(); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }

    [Benchmark]
    public long PeekOnce()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.ReadInt32OrNilPeekOnce(out var a) ? a : default(int?); break;
                    case 1: value.B = buffer.ReadInt32OrNilPeekOnce(out var b) ? b : default(int?); break;
                    case 2: value.C = buffer.ReadInt32OrNilPeekOnce(out var c) ? c : default(int?); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }

    [Benchmark]
    public long Inline()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.ReadInt32OrNilInline(out var a) ? a : default(int?); break;
                    case 1: value.B = buffer.ReadInt32OrNilInline(out var b) ? b : default(int?); break;
                    case 2: value.C = buffer.ReadInt32OrNilInline(out var c) ? c : default(int?); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }

    [Benchmark]
    public long InlineReturningNullable()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.ReadNullableInt32Candidate(); break;
                    case 1: value.B = buffer.ReadNullableInt32Candidate(); break;
                    case 2: value.C = buffer.ReadNullableInt32Candidate(); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }

    // what shipped: ReadBufferExtensions.ReadNullableInt32, the InlineReturningNullable shape inside the library
    [Benchmark]
    public long Library()
    {
        var buffer = new ReadOnlySpanReadBuffer(blob);
        var value = poco;
        long sum = 0;
        for (int o = 0; o < ObjectCount; o++)
        {
            var count = buffer.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: value.A = buffer.ReadNullableInt32(); break;
                    case 1: value.B = buffer.ReadNullableInt32(); break;
                    case 2: value.C = buffer.ReadNullableInt32(); break;
                    default: buffer.Skip(); break;
                }
            }
            sum += value.Checksum();
        }
        return sum;
    }

    // ---- verification -----------------------------------------------------

    // Every first byte 0x00..0xff in front of a set of payload patterns, at every truncation: each candidate must
    // return what the shipped expression returns, leave the same bytes unread, and fail with the same exception.
    public static void VerifyCandidates()
    {
        ulong[] patterns =
        [
            0x0000000000000000, 0x0000000000000001, 0x000000000000007f, 0x0000000000000080, 0x00000000000000ff,
            0x7f00000000000000, 0x8000000000000000, 0xff00000000000000, 0x7fff000000000000, 0x8000000000000000,
            0xffff000000000000, 0x7fffffff00000000, 0x8000000000000000, 0xffffffff00000000, 0x000000007fffffff,
            0x0000000080000000, 0x00000000ffffffff, 0xffffffff80000000, 0xffffffff7fffffff, 0x7fffffffffffffff,
            0xffffffffffffffff, 0x0102030405060708, 0xc0c0c0c0c0c0c0c0, 0xc2c3c2c3c2c3c2c3,
        ];
        Span<byte> payload = stackalloc byte[10];
        for (int code = 0; code <= 0xff; code++)
        {
            foreach (var pattern in patterns)
            {
                payload[0] = (byte)code;
                BinaryPrimitives.WriteUInt64BigEndian(payload.Slice(1), pattern);
                payload[9] = 0x2a; // a trailing fixint the read must leave alone
                for (int length = payload.Length; length >= 0; length--)
                {
                    var cut = payload.Slice(0, length).ToArray();
                    var expectedInt = Run(cut, static (ref ReadOnlySpanReadBuffer b) => b.TryReadNil() ? default(int?) : b.ReadInt32());
                    Check("PeekOnce", cut, expectedInt, Run(cut, static (ref ReadOnlySpanReadBuffer b) => b.ReadInt32OrNilPeekOnce(out var v) ? v : default(int?)));
                    Check("Inline", cut, expectedInt, Run(cut, static (ref ReadOnlySpanReadBuffer b) => b.ReadInt32OrNilInline(out var v) ? v : default(int?)));

                    Check("InlineReturningNullable", cut, expectedInt, Run(cut, static (ref ReadOnlySpanReadBuffer b) => b.ReadNullableInt32Candidate()));

                    var expectedBool = Run(cut, static (ref ReadOnlySpanReadBuffer b) => b.TryReadNil() ? default(bool?) : b.ReadBoolean());
                    Check("BooleanInline", cut, expectedBool, Run(cut, static (ref ReadOnlySpanReadBuffer b) => b.ReadBooleanOrNilInline(out var v) ? v : default(bool?)));
                }
            }
        }
    }

    delegate T Read<T>(ref ReadOnlySpanReadBuffer buffer);

    static (string Value, long Remaining, string? Error) Run<T>(byte[] payload, Read<T> read)
    {
        var buffer = new ReadOnlySpanReadBuffer(payload);
        try
        {
            var value = read(ref buffer);
            return (value?.ToString() ?? "null", buffer.BytesRemaining, null);
        }
        catch (MessagePackSerializationException ex)
        {
            return ("", buffer.BytesRemaining, ex.Message);
        }
    }

    static void Check(string name, byte[] payload, (string Value, long Remaining, string? Error) expected, (string Value, long Remaining, string? Error) actual)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"{name} differs on {Convert.ToHexString(payload)}: {actual} (expected {expected})");
        }
    }
}

public sealed class NullableInt3
{
    public int? A;
    public int? B;
    public int? C;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Checksum() => (long)A.GetValueOrDefault() + B.GetValueOrDefault() + C.GetValueOrDefault()
        + (A.HasValue ? 1 : 0) + (B.HasValue ? 1 : 0) + (C.HasValue ? 1 : 0);
}

public static class NullableReadCandidates
{
    // MessagePackPrimitives.Int32ReadTable (private there): len | rawShift << 4 | ext << 10, indexed by code - 0xcc
    static ReadOnlySpan<uint> Int32ReadTable =>
    [
        2 | (24u << 4), 3 | (16u << 4), 5, 9, 2 | (24u << 4) | (56u << 10), 3 | (16u << 4) | (48u << 10), 5 | (32u << 10), 9,
    ];

    /// <summary>True with the value, false for nil (consumed either way). Anything else fails as the shipped pair does.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ReadInt32OrNilPeekOnce<TReadBuffer>(ref this TReadBuffer buffer, out int value)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty && span[0] == MessagePackCode.Nil)
        {
            buffer.Advance(1);
            value = 0;
            return false;
        }
        if (MessagePack.MessagePackPrimitives.TryReadInt32(span, out var decoded, out var tokenSize) == DecodeResult.Success)
        {
            buffer.Advance(tokenSize);
            value = decoded;
            return true;
        }
        var cold = ReadNullableInt32Slow(ref buffer);
        value = (int)cold;
        return cold < 0;
    }

    /// <summary>True with the value, false for nil (consumed either way). Anything else fails as the shipped pair does.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ReadInt32OrNilInline<TReadBuffer>(ref this TReadBuffer buffer, out int value)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            uint code = s;
            if ((byte)(code + 32) <= 159) // fixint
            {
                buffer.Advance(1);
                value = (sbyte)code;
                return true;
            }
            if (code == MessagePackCode.Nil)
            {
                buffer.Advance(1);
                value = 0;
                return false;
            }
            uint format = code - MessagePackCode.UInt8;
            if (format <= 7 && span.Length >= 5)
            {
                uint entry = Unsafe.Add(ref MemoryMarshal.GetReference(Int32ReadTable), (nint)format);
                int length = (int)(entry & 0xf);
                if (length != 9)
                {
                    // the unconditional 4-byte load stays inside the window (Length >= 5); the entry right-aligns and
                    // sign-extends it. A uint32 above int.MaxValue fails the range test and takes the cold path.
                    uint payload = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
                    uint selected = payload >> (int)((entry >> 4) & 0x1f);
                    int extend = (int)(entry >> 10);
                    long wide = ((long)selected << extend) >> extend;
                    if (wide == (int)wide)
                    {
                        buffer.Advance(length);
                        value = (int)wide;
                        return true;
                    }
                }
            }
        }
        var cold = ReadNullableInt32Slow(ref buffer);
        value = (int)cold;
        return cold < 0;
    }

    // The call-site composition moved inside a reader that returns int?: ONE return expression over the bool / out
    // reader, the same `cond ? value : default` shape the generated code has today. Asks whether the stall of the
    // first version came from returning Nullable<T> at all or from returning it at several points.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int? ReadNullableInt32Candidate<TReadBuffer>(ref this TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
        => buffer.ReadInt32OrNilInline(out var value) ? value : default(int?);

    /// <summary>True with the value, false for nil (consumed either way). Anything else fails as the shipped pair does.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ReadBooleanOrNilInline<TReadBuffer>(ref this TReadBuffer buffer, out bool value)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty)
        {
            uint code = span[0];
            uint d = code - MessagePackCode.False;
            if (d <= 1)
            {
                buffer.Advance(1);
                value = d != 0;
                return true;
            }
            if (code == MessagePackCode.Nil)
            {
                buffer.Advance(1);
                value = false;
                return false;
            }
        }
        var cold = ReadNullableBooleanSlow(ref buffer);
        value = (cold & 1) != 0;
        return cold < 0;
    }

    // Nothing has been consumed: the shipped pair decides, so every failure is today's failure. The result comes
    // back as one scalar (sign bit = has a value, low bits = the value) so the caller's locals stay in registers.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ReadNullableInt32Slow<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
        => buffer.TryReadNil() ? 0 : long.MinValue | (uint)buffer.ReadInt32();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadNullableBooleanSlow<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
        => buffer.TryReadNil() ? 0 : int.MinValue | (buffer.ReadBoolean() ? 1 : 0);
}
