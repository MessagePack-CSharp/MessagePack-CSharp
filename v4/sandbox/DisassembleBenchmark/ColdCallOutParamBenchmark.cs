using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using MessagePack;

// Does the TryReadToken finding (a cold NoInlining call taking the hot out params BY
// ADDRESS forces them into stack slots on the fast path) also tax the per-value
// readers? TryReadInt32's internal cold calls share the hot locals:
//   TryReadInt32NineByteToken(source, out value, out tokenSize)   - both exposed
//   TryReadInt64ShortBuffer(source, out long v, out tokenSize)    - tokenSize exposed
// and tokenSize is the consumed->next-address chain in every array/POCO decode loop.
// Candidates, identical except for the cold-call plumbing:
//   OutParamShared - copy of the current TryReadInt32 (cold calls write the hot outs)
//   OutParamSplit  - cold calls write fresh temps, copied to the hot outs after; the
//                    address exposure is confined to the temps, so the hot-path locals
//                    can stay enregistered while the helpers stay NoInlining
//   FullyInlined   - no call at all (ceiling; not shippable for the wide readers:
//                    unlike TryReadToken's 2 call sites these inline into every
//                    formatter x every field)
// Payloads (100k values, random data per the predictor pitfall):
//   Fix  - all fixint: the cold paths never RUN, so any delta is pure exposure tax
//   Wide - fixint + uint8/16/32 + int8/16/32 spread: the 5-byte table path
public class ColdCallOutParamBenchmark
{
    const int Count = 100_000;

    [Params("Fix", "Wide")]
    public string Payload = "Fix";

    byte[] data = null!;
    long expectedSum;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var bytes = new List<byte>(Count * 5);
        long sum = 0;
        for (var i = 0; i < Count; i++)
        {
            if (Payload == "Fix")
            {
                var v = random.Next(-32, 128);
                sum += v;
                bytes.Add(unchecked((byte)(sbyte)v));
                continue;
            }
            switch (random.Next(7))
            {
                case 0:
                    {
                        var v = random.Next(-32, 128);
                        sum += v;
                        bytes.Add(unchecked((byte)(sbyte)v));
                        break;
                    }
                case 1:
                    {
                        var v = random.Next(128, 256);
                        sum += v;
                        bytes.Add(0xcc); bytes.Add((byte)v);
                        break;
                    }
                case 2:
                    {
                        var v = random.Next(256, 65536);
                        sum += v;
                        bytes.Add(0xcd); bytes.Add((byte)(v >> 8)); bytes.Add((byte)v);
                        break;
                    }
                case 3:
                    {
                        var v = random.Next(65536, int.MaxValue);
                        sum += v;
                        bytes.Add(0xce);
                        bytes.Add((byte)(v >> 24)); bytes.Add((byte)(v >> 16)); bytes.Add((byte)(v >> 8)); bytes.Add((byte)v);
                        break;
                    }
                case 4:
                    {
                        var v = random.Next(-128, -32);
                        sum += v;
                        bytes.Add(0xd0); bytes.Add(unchecked((byte)(sbyte)v));
                        break;
                    }
                case 5:
                    {
                        var v = random.Next(-32768, -128);
                        sum += v;
                        bytes.Add(0xd1); bytes.Add((byte)((uint)v >> 8)); bytes.Add(unchecked((byte)v));
                        break;
                    }
                default:
                    {
                        var v = random.Next(int.MinValue, -32768);
                        sum += v;
                        bytes.Add(0xd2);
                        bytes.Add((byte)((uint)v >> 24)); bytes.Add((byte)((uint)v >> 16)); bytes.Add((byte)((uint)v >> 8)); bytes.Add(unchecked((byte)v));
                        break;
                    }
            }
        }
        data = bytes.ToArray();
        expectedSum = sum;

        Check(nameof(OutParamShared), OutParamShared());
        Check(nameof(OutParamSplit), OutParamSplit());
        Check(nameof(FullyInlined), FullyInlined());

        void Check(string name, long sum)
        {
            if (sum != expectedSum)
            {
                throw new InvalidOperationException($"{name} summed {sum}, expected {expectedSum}");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public long OutParamShared()
    {
        var span = data.AsSpan();
        long sum = 0;
        var index = 0;
        while (index < span.Length)
        {
            var result = TryReadInt32Shared(span.Slice(index), out var value, out var tokenSize);
            if (result != DecodeResult.Success) ThrowUnexpected();
            sum += value;
            index += tokenSize;
        }
        return sum;
    }

    [Benchmark]
    public long OutParamSplit()
    {
        var span = data.AsSpan();
        long sum = 0;
        var index = 0;
        while (index < span.Length)
        {
            var result = TryReadInt32Split(span.Slice(index), out var value, out var tokenSize);
            if (result != DecodeResult.Success) ThrowUnexpected();
            sum += value;
            index += tokenSize;
        }
        return sum;
    }

    [Benchmark]
    public long FullyInlined()
    {
        var span = data.AsSpan();
        long sum = 0;
        var index = 0;
        while (index < span.Length)
        {
            var result = TryReadInt32Inline(span.Slice(index), out var value, out var tokenSize);
            if (result != DecodeResult.Success) ThrowUnexpected();
            sum += value;
            index += tokenSize;
        }
        return sum;
    }

    static void ThrowUnexpected() => throw new InvalidOperationException("decode failed on a complete well-formed payload");

    #region candidates (copies of MessagePackPrimitives.TryReadInt32 machinery)

    const uint R32U1 = 2 | (24u << 4);
    const uint R32U2 = 3 | (16u << 4);
    const uint R32U4 = 5 | (0u << 4);
    const uint R32W8 = 9;
    const uint R32I1 = 2 | (24u << 4) | (56u << 10);
    const uint R32I2 = 3 | (16u << 4) | (48u << 10);
    const uint R32I4 = 5 | (0u << 4) | (32u << 10);

    static ReadOnlySpan<uint> Int32ReadTable =>
    [
        R32U1, R32U2, R32U4, R32W8, R32I1, R32I2, R32I4, R32W8,
    ];

    // current library shape: the cold calls take the hot out params by address
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadInt32Shared(ReadOnlySpan<byte> source, out int value, out int tokenSize)
    {
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((byte)(code0 + 32) <= 159)
            {
                value = unchecked((sbyte)code0);
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }

        if (source.Length >= 5)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            byte code = s;
            uint fmt = (uint)(code - MessagePackCode.UInt8);
            if (fmt > 7)
            {
                value = 0;
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Int32ReadTable), (int)fmt);
            if ((e & 0xf) == 9)
            {
                return NineByteToken(source, out value, out tokenSize);
            }
            uint p = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
            uint sel = p >> (int)((e >> 4) & 0x3f);
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            int len = (int)(e & 0xf);
            value = (int)v;
            bool ok = v == value;
            tokenSize = ok ? len : 0;
            return ok ? DecodeResult.Success : DecodeResult.TokenMismatch;
        }
        else
        {
            var r = Int64ShortBuffer(source, out long v, out tokenSize);
            value = (int)v;
            return r;
        }
    }

    // temp-split: identical, but the cold calls write fresh temps and the hot outs are
    // only ever assigned by plain moves
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadInt32Split(ReadOnlySpan<byte> source, out int value, out int tokenSize)
    {
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((byte)(code0 + 32) <= 159)
            {
                value = unchecked((sbyte)code0);
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }

        if (source.Length >= 5)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            byte code = s;
            uint fmt = (uint)(code - MessagePackCode.UInt8);
            if (fmt > 7)
            {
                value = 0;
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Int32ReadTable), (int)fmt);
            if ((e & 0xf) == 9)
            {
                var nineResult = NineByteToken(source, out var nineValue, out var nineSize);
                value = nineValue;
                tokenSize = nineSize;
                return nineResult;
            }
            uint p = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
            uint sel = p >> (int)((e >> 4) & 0x3f);
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            int len = (int)(e & 0xf);
            value = (int)v;
            bool ok = v == value;
            tokenSize = ok ? len : 0;
            return ok ? DecodeResult.Success : DecodeResult.TokenMismatch;
        }
        else
        {
            var shortResult = Int64ShortBuffer(source, out long shortValue, out var shortSize);
            value = (int)shortValue;
            tokenSize = shortSize;
            return shortResult;
        }
    }

    // ceiling: the cold halves written into the same method, no call anywhere
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadInt32Inline(ReadOnlySpan<byte> source, out int value, out int tokenSize)
    {
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((byte)(code0 + 32) <= 159)
            {
                value = unchecked((sbyte)code0);
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }

        if (source.Length >= 5)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            byte code = s;
            uint fmt = (uint)(code - MessagePackCode.UInt8);
            if (fmt > 7)
            {
                value = 0;
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Int32ReadTable), (int)fmt);
            if ((e & 0xf) == 9)
            {
                if (source.Length >= 9)
                {
                    long nineWide = unchecked((long)ReadUInt64BigEndian(source));
                    value = (int)nineWide;
                    if (((code == MessagePackCode.Int64) | (nineWide >= 0)) & (nineWide == value))
                    {
                        tokenSize = 9;
                        return DecodeResult.Success;
                    }
                    tokenSize = 0;
                    return DecodeResult.TokenMismatch;
                }
                value = 0;
                tokenSize = 9;
                return DecodeResult.InsufficientBuffer;
            }
            uint p = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
            uint sel = p >> (int)((e >> 4) & 0x3f);
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            int len = (int)(e & 0xf);
            value = (int)v;
            bool ok = v == value;
            tokenSize = ok ? len : 0;
            return ok ? DecodeResult.Success : DecodeResult.TokenMismatch;
        }
        else
        {
            value = 0;
            if (source.IsEmpty)
            {
                tokenSize = 1;
                return DecodeResult.InsufficientBuffer;
            }
            byte code = source[0];
            switch (code)
            {
                case MessagePackCode.UInt8:
                    tokenSize = 2;
                    if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                    value = source[1];
                    return DecodeResult.Success;
                case MessagePackCode.UInt16:
                    tokenSize = 3;
                    if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                    value = ReadUInt16BigEndian(source);
                    return DecodeResult.Success;
                case MessagePackCode.Int8:
                    tokenSize = 2;
                    if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                    value = unchecked((sbyte)source[1]);
                    return DecodeResult.Success;
                case MessagePackCode.Int16:
                    tokenSize = 3;
                    if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                    value = unchecked((short)ReadUInt16BigEndian(source));
                    return DecodeResult.Success;
                case MessagePackCode.UInt32:
                case MessagePackCode.Int32:
                    tokenSize = 5;
                    return DecodeResult.InsufficientBuffer;
                case MessagePackCode.UInt64:
                case MessagePackCode.Int64:
                    tokenSize = 9;
                    return DecodeResult.InsufficientBuffer;
                default:
                    tokenSize = 0;
                    return DecodeResult.TokenMismatch;
            }
        }
    }

    // cold halves shared by Shared/Split (copies of the library's)

    [MethodImpl(MethodImplOptions.NoInlining)]
    static DecodeResult NineByteToken(ReadOnlySpan<byte> source, out int value, out int tokenSize)
    {
        if (source.Length >= 9)
        {
            byte code = MemoryMarshal.GetReference(source);
            long v = unchecked((long)ReadUInt64BigEndian(source));
            value = (int)v;
            if (((code == MessagePackCode.Int64) | (v >= 0)) & (v == value))
            {
                tokenSize = 9;
                return DecodeResult.Success;
            }
            tokenSize = 0;
            return DecodeResult.TokenMismatch;
        }
        value = 0;
        tokenSize = 9;
        return DecodeResult.InsufficientBuffer;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static DecodeResult Int64ShortBuffer(ReadOnlySpan<byte> source, out long value, out int tokenSize)
    {
        value = 0;
        if (source.IsEmpty)
        {
            tokenSize = 1;
            return DecodeResult.InsufficientBuffer;
        }
        byte code = source[0];
        switch (code)
        {
            case MessagePackCode.UInt8:
                tokenSize = 2;
                if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                value = source[1];
                return DecodeResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                value = ReadUInt16BigEndian(source);
                return DecodeResult.Success;
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                value = unchecked((sbyte)source[1]);
                return DecodeResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                value = unchecked((short)ReadUInt16BigEndian(source));
                return DecodeResult.Success;
            case MessagePackCode.UInt32:
            case MessagePackCode.Int32:
                tokenSize = 5;
                return DecodeResult.InsufficientBuffer;
            case MessagePackCode.UInt64:
            case MessagePackCode.Int64:
                tokenSize = 9;
                return DecodeResult.InsufficientBuffer;
            default:
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ushort ReadUInt16BigEndian(ReadOnlySpan<byte> source)
        => MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), 1)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ulong ReadUInt64BigEndian(ReadOnlySpan<byte> source)
        => MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), 1)));

    #endregion
}
