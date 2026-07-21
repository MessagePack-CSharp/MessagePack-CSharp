using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Unicode;

namespace UltraMessagePack;

// Write primitives. Contract: the caller guarantees the destination has the worst-case
// size for the method (UnsafeWriteInt32: 5 bytes, UnsafeWriteInt64/UnsafeWriteDouble: 9 bytes, ...).
// Methods may write scratch bytes beyond the returned length, always within that
// worst-case window. MessagePackWriter.Reserve provides the guarantee.

public static partial class MessagePackPrimitives
{
    // Contract Size
    public const int MaxInt8Length = 2;
    public const int MaxInt16Length = 3;
    public const int MaxInt32Length = 5;
    public const int MaxInt64Length = 9;
    public const int MaxUInt8Length = 2;
    public const int MaxUInt16Length = 3;
    public const int MaxUInt32Length = 5;
    public const int MaxUInt64Length = 9;
    public const int MaxFloat32Length = 5;
    public const int MaxFloat64Length = 9;
    public const int MaxTimestampLength = 15;
    public const int MaxArrayHeaderLength = 5;
    public const int MaxMapHeaderLength = 5;
    public const int MaxBinHeaderLength = 5;
    public const int MaxStrHeaderLength = 5;
    public const int MaxExtHeaderLength = 6;

    // packed entry: len | header << 8
    const uint WU8 = 2 | ((uint)MessagePackCode.UInt8 << 8);
    const uint WU16 = 3 | ((uint)MessagePackCode.UInt16 << 8);
    const uint WU32 = 5 | ((uint)MessagePackCode.UInt32 << 8);
    const uint WU64 = 9 | ((uint)MessagePackCode.UInt64 << 8);
    const uint WI8 = 2 | ((uint)MessagePackCode.Int8 << 8);
    const uint WI16 = 3 | ((uint)MessagePackCode.Int16 << 8);
    const uint WI32 = 5 | ((uint)MessagePackCode.Int32 << 8);
    const uint WI64 = 9 | ((uint)MessagePackCode.Int64 << 8);

    // index = ((value >>> 26) & 32) | significantBits(value ^ (value >> 31)); fixint rows
    // (bits <= 7 positive / <= 5 negative) are unreachable behind the fixint fast path
    // but hold the correct next-larger format for safety.
    static ReadOnlySpan<uint> Int32Formats =>
    [
        // non-negative: bits 0..8 uint8, 9..16 uint16, 17..31 uint32
        WU8, WU8, WU8, WU8, WU8, WU8, WU8, WU8, WU8,
        WU16, WU16, WU16, WU16, WU16, WU16, WU16, WU16,
        WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32,
        // negative (bits of ~value): 0..7 int8, 8..15 int16, 16..31 int32
        WI8, WI8, WI8, WI8, WI8, WI8, WI8, WI8,
        WI16, WI16, WI16, WI16, WI16, WI16, WI16, WI16,
        WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32,
    ];

    // index = ((value >>> 57) & 64) | significantBits(value ^ (value >> 63))
    static ReadOnlySpan<uint> Int64Formats =>
    [
        // non-negative: bits 0..8 uint8, 9..16 uint16, 17..32 uint32, 33..63 uint64
        WU8, WU8, WU8, WU8, WU8, WU8, WU8, WU8, WU8,
        WU16, WU16, WU16, WU16, WU16, WU16, WU16, WU16,
        WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32,
        WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64,
        WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64,
        // negative (bits of ~value): 0..7 int8, 8..15 int16, 16..31 int32, 32..63 int64
        WI8, WI8, WI8, WI8, WI8, WI8, WI8, WI8,
        WI16, WI16, WI16, WI16, WI16, WI16, WI16, WI16,
        WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32, WI32,
        WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64,
        WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64, WI64,
    ];

    // bits 0..8 -> uint8 (0..7 unreachable behind the fixint fast path), 9..16 -> uint16, 17..32 -> uint32
    static ReadOnlySpan<uint> UInt32Formats =>
    [
        WU8, WU8, WU8, WU8, WU8, WU8, WU8, WU8, WU8,
        WU16, WU16, WU16, WU16, WU16, WU16, WU16, WU16,
        WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32,
    ];

    // bits 8 -> uint8, 9..16 -> uint16, 17..32 -> uint32, 33..64 -> uint64 (bits >= 8 here)
    static ReadOnlySpan<uint> UInt64Formats =>
    [
        WU8, WU8, WU8, WU8, WU8, WU8, WU8, WU8, WU8,
        WU16, WU16, WU16, WU16, WU16, WU16, WU16, WU16,
        WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32, WU32,
        WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64,
        WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64, WU64,
    ];

    #region WriteInt32, 64

    /// <summary>Writes an int32 in the smallest msgpack format. Destination must have 5 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteInt32(ref byte destination, int value)
    {
        // The hybrid approach (a branch of fixint) minimizes overall branching errors while ensuring high performance for small integers that occur frequently.
        // Measurements show that this is the most well-balanced heuristic.

        // fixint fast path: 0..127 positive, -32..-1 negative 
        if ((uint)(value + 32) <= 159)
        {
            destination = unchecked((byte)value);
            return 1;
        }
        else
        {
            // Branchless code: significant-bit count -> format table -> one header store + one unconditional big-endian store (movbe)
            // it achieves no data-dependent branches
            int x = value ^ (value >> 31); // abs
            int bits = 32 - BitOperations.LeadingZeroCount((uint)x); // 0..31(5bit)
            int idx = ((value >>> 26) & 32) | bits; // sign bit(1) + bits(5) = 6-bit index(0..63)
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Int32Formats), idx); // get format-table(len|header) entry
            int len = (int)(e & 0xff);
            destination = (byte)(e >> 8); // write header
            // len in {2,3,5}: shift in {24,16,0}, big-endian payload right after the header
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((uint)value << ((5 - len) * 8)));
            return len;
        }
    }

    /// <summary>Writes an int64 in the smallest msgpack format. Destination must have 9 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteInt64(ref byte destination, long value)
    {
        // fixint fast path
        if ((ulong)(value + 32) <= 159)
        {
            destination = unchecked((byte)value);
            return 1;
        }
        else
        {
            // branchless body, similar as UnsafeWriteInt32, but with 64-bit operands and a 7-bit index (0..127)
            long x = value ^ (value >> 63);
            int bits = 64 - BitOperations.LeadingZeroCount((ulong)x); // 0..63
            int idx = (int)((value >>> 57) & 64) | bits;
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Int64Formats), idx);
            int len = (int)(e & 0xff);
            destination = (byte)(e >> 8);
            // len in {2,3,5,9}: shift in {56,48,32,0} — the fixint fast path removed len==1,
            // so the shift never reaches 64 (which C# would mask to 0)
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((ulong)value << ((9 - len) * 8)));
            return len;
        }
    }

    /// <summary>Writes a uint32 in the smallest msgpack format. Destination must have 5 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteUInt32(ref byte destination, uint value)
    {
        if (value <= 0x7f)
        {
            destination = (byte)value;
            return 1;
        }
        return WriteHeaderCore(ref destination, value, ref MemoryMarshal.GetReference(UInt32Formats));
    }

    // shared branchless core for every wide 32-bit-value format: significant-bit count
    // indexes a 33-entry table of {len, header}; one header store + one unconditional
    // big-endian store (len in {2,3,5}: shift in {24,16,0}, scratch stays within the
    // 5-byte window). Every caller's hybrid front filters out the fix-form range, so
    // the tables hold no fix rows and no header needs value bits folded in (a former
    // fix-mask fold here was dead on all paths — 3 ALU ops saved).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int WriteHeaderCore(ref byte destination, uint value, ref uint table)
    {
        int bits = 32 - BitOperations.LeadingZeroCount(value);
        uint e = Unsafe.Add(ref table, bits);
        int len = (int)(e & 0xff);
        destination = (byte)(e >> 8);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(value << ((5 - len) * 8)));
        return len;
    }

    /// <summary>Writes a uint64 in the smallest msgpack format. Destination must have 9 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteUInt64(ref byte destination, ulong value)
    {
        if (value <= 0x7f)
        {
            destination = (byte)value;
            return 1;
        }
        else
        {
            int bits = 64 - BitOperations.LeadingZeroCount(value); // 8..64
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(UInt64Formats), bits);
            int len = (int)(e & 0xff);
            destination = (byte)(e >> 8);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(value << ((9 - len) * 8)));
            return len;
        }
    }

    #endregion

    #region WriteInt8, 16

    /// <summary>Writes a byte in the smallest msgpack format. Destination must have 2 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteByte(ref byte destination, byte value)
    {
        if (value <= 0x7f)
        {
            destination = value;
            return 1;
        }
        else
        {
            destination = MessagePackCode.UInt8;
            Unsafe.Add(ref destination, 1) = value;
            return 2;
        }
    }

    /// <summary>Writes an sbyte in the smallest msgpack format. Destination must have 2 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteSByte(ref byte destination, sbyte value)
    {
        // fixint covers -32..127; everything left (-128..-33) is int8
        if ((uint)(value + 32) <= 159)
        {
            destination = unchecked((byte)value);
            return 1;
        }
        else
        {
            destination = MessagePackCode.Int8;
            Unsafe.Add(ref destination, 1) = unchecked((byte)value);
            return 2;
        }
    }

    /// <summary>Writes an int16 in the smallest msgpack format. Destination must have 3 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteInt16(ref byte destination, short value)
    {
        // same shape as UnsafeWriteInt32 (fixint front + Int32Formats core, index layout
        // identical; short can never reach the len-5 rows) but the unconditional payload
        // store narrows to 2 bytes, giving a 3-byte contract.
        int v = value;
        if ((uint)(v + 32) <= 159)
        {
            destination = unchecked((byte)v);
            return 1;
        }
        else
        {
            int x = v ^ (v >> 31);
            int bits = 32 - BitOperations.LeadingZeroCount((uint)x); // 6..15
            int idx = ((v >>> 26) & 32) | bits;
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Int32Formats), idx);
            int len = (int)(e & 0xff);
            destination = (byte)(e >> 8);
            // len in {2,3}: shift in {8,0}, big-endian payload right after the header
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((ushort)(v << ((3 - len) * 8))));
            return len;
        }
    }

    /// <summary>Writes a uint16 in the smallest msgpack format. Destination must have 3 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteUInt16(ref byte destination, ushort value)
    {
        if (value <= 0x7f)
        {
            destination = (byte)value;
            return 1;
        }
        // uint8 or uint16, picked by one setcc; one unconditional 2-byte big-endian store
        // (wide=0: value << 8 -> [value, scratch]; wide=1: [hi, lo])
        int wide = value > 0xff ? 1 : 0;
        destination = (byte)(MessagePackCode.UInt8 + wide); // 0xcc or 0xcd
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((ushort)(value << ((1 - wide) * 8))));
        return 2 + wide;
    }

    /// <summary>Writes a char in the smallest msgpack format (as uint16, MessagePack-CSharp compatible). Destination must have 3 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteChar(ref byte destination, char value) => UnsafeWriteUInt16(ref destination, value);

    #endregion

    #region forced-width (WriteAs*)

    // WriteAs* = always the named msgpack format regardless of value ("as int32"), unlike
    // the smallest-format writers above. Fully branchless (header store + big-endian payload
    // store), constant return, exact-size destination contract, no scratch bytes. Use cases:
    // fixed-size slots that get patched later, stable layouts, and constant-time writes for
    // unpredictable value distributions (trading output size for zero branches).

    /// <summary>Writes value in the int8 format (0xd0). Destination must have 2 bytes. Returns 2.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteAsInt8(ref byte destination, sbyte value)
    {
        destination = MessagePackCode.Int8;
        Unsafe.Add(ref destination, 1) = unchecked((byte)value);
        return 2;
    }

    /// <summary>Writes value in the uint8 format (0xcc). Destination must have 2 bytes. Returns 2.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteAsUInt8(ref byte destination, byte value)
    {
        destination = MessagePackCode.UInt8;
        Unsafe.Add(ref destination, 1) = value;
        return 2;
    }

    /// <summary>Writes value in the int16 format (0xd1). Destination must have 3 bytes. Returns 3.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteAsInt16(ref byte destination, short value)
    {
        destination = MessagePackCode.Int16;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(unchecked((ushort)value)));
        return 3;
    }

    /// <summary>Writes value in the uint16 format (0xcd). Destination must have 3 bytes. Returns 3.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteAsUInt16(ref byte destination, ushort value)
    {
        destination = MessagePackCode.UInt16;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(value));
        return 3;
    }

    /// <summary>Writes value in the int32 format (0xd2). Destination must have 5 bytes. Returns 5.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteAsInt32(ref byte destination, int value)
    {
        destination = MessagePackCode.Int32;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(unchecked((uint)value)));
        return 5;
    }

    /// <summary>Writes value in the uint32 format (0xce). Destination must have 5 bytes. Returns 5.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteAsUInt32(ref byte destination, uint value)
    {
        destination = MessagePackCode.UInt32;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(value));
        return 5;
    }

    /// <summary>Writes value in the int64 format (0xd3). Destination must have 9 bytes. Returns 9.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteAsInt64(ref byte destination, long value)
    {
        destination = MessagePackCode.Int64;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(unchecked((ulong)value)));
        return 9;
    }

    /// <summary>Writes value in the uint64 format (0xcf). Destination must have 9 bytes. Returns 9.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteAsUInt64(ref byte destination, ulong value)
    {
        destination = MessagePackCode.UInt64;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(value));
        return 9;
    }

    #endregion

    #region fixed-length(Nil, Boolean, Single, Double)

    /// <summary>Destination must have 1 byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteNil(ref byte destination)
    {
        destination = MessagePackCode.Nil;
        return 1;
    }

    /// <summary>Destination must have 1 byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteBoolean(ref byte destination, bool value)
    {
        // simple implementation `value ? True : False` is slower:
        // the asm generates `test je mov` (a real branch — if-conversion is disabled inside loops)
        // measured 11.7x slower on random bools

        // MessagePack False is 1100_0010
        // MessagePack True is  1100_0011
        // .NET False is        0000_0000
        // .NET True is         0000_0001
        // `(uint)-raw >> 31` is normalize, so that any non-zero value (interop, etc) becomes 1, and 0 stays 0.
        byte raw = Unsafe.BitCast<bool, byte>(value);
        destination = (byte)(MessagePackCode.False | ((uint)-raw >> 31));
        return 1;
    }

    /// <summary>Destination must have 5 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteSingle(ref byte destination, float value)
    {
        destination = MessagePackCode.Float32;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(BitConverter.SingleToUInt32Bits(value)));
        return 5;
    }

    /// <summary>Destination must have 9 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteDouble(ref byte destination, double value)
    {
        destination = MessagePackCode.Float64;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(BitConverter.DoubleToUInt64Bits(value)));
        return 9;
    }

    #endregion

    #region headers(array, map, string, bin), binary

    // packed entry: len | header << 8, same as the integer tables
    const uint WA16 = 3 | ((uint)MessagePackCode.Array16 << 8);
    const uint WA32 = 5 | ((uint)MessagePackCode.Array32 << 8);
    const uint WM16 = 3 | ((uint)MessagePackCode.Map16 << 8);
    const uint WM32 = 5 | ((uint)MessagePackCode.Map32 << 8);
    const uint WS8 = 2 | ((uint)MessagePackCode.Str8 << 8);
    const uint WS16 = 3 | ((uint)MessagePackCode.Str16 << 8);
    const uint WS32 = 5 | ((uint)MessagePackCode.Str32 << 8);
    const uint WB8 = 2 | ((uint)MessagePackCode.Bin8 << 8);
    const uint WB16 = 3 | ((uint)MessagePackCode.Bin16 << 8);
    const uint WB32 = 5 | ((uint)MessagePackCode.Bin32 << 8);

    static ReadOnlySpan<uint> ArrayHeaderFormats =>
    [
        // bits 0..4(count 0..15 = fixarray) is unreachable behind the fast path, but holds the correct next-larger format for safety
        WA16, WA16, WA16, WA16, WA16,
        // bits 5..16 array16
        WA16, WA16, WA16, WA16, WA16, WA16, WA16, WA16, WA16, WA16, WA16, WA16,
        // bits 17..32 array32
        WA32, WA32, WA32, WA32, WA32, WA32, WA32, WA32, WA32, WA32, WA32, WA32, WA32, WA32, WA32, WA32,
    ];

    static ReadOnlySpan<uint> MapHeaderFormats =>
    [
        // bits 0..4(count 0..15 = fixmap) is unreachable behind the fast path, but holds the correct next-larger format for safety
        WM16, WM16, WM16, WM16, WM16,
        // bits 5..16 map16
        WM16, WM16, WM16, WM16, WM16, WM16, WM16, WM16, WM16, WM16, WM16, WM16,
        // bits 17..32 map32
        WM32, WM32, WM32, WM32, WM32, WM32, WM32, WM32, WM32, WM32, WM32, WM32, WM32, WM32, WM32, WM32,
    ];

    static ReadOnlySpan<uint> StringHeaderFormats =>
    [
        // bits 0..5(byteCount 0..31 = fixstr) is unreachable behind the fast path, but holds the correct next-larger format for safety
        WS8, WS8, WS8, WS8, WS8, WS8,
        // bits 6..8 str8
        WS8, WS8, WS8,
        // bits 9..16 str16
        WS16, WS16, WS16, WS16, WS16, WS16, WS16, WS16,
        // bits 17..32 str32
        WS32, WS32, WS32, WS32, WS32, WS32, WS32, WS32, WS32, WS32, WS32, WS32, WS32, WS32, WS32, WS32,
    ];

    static ReadOnlySpan<uint> BinHeaderFormats =>
    [
        // bits 0..8 bin8 (bin has no fix form, so every row is reachable)
        WB8, WB8, WB8, WB8, WB8, WB8, WB8, WB8, WB8,
        // bits 9..16 bin16
        WB16, WB16, WB16, WB16, WB16, WB16, WB16, WB16,
        // bits 17..32 bin32
        WB32, WB32, WB32, WB32, WB32, WB32, WB32, WB32, WB32, WB32, WB32, WB32, WB32, WB32, WB32, WB32,
    ];

    // Hybrid front for the fix forms: collection counts and string lengths at a given call
    // site are typically small and often constant (POCO formatters write a fixed field count),
    // making this branch near-perfectly predictable — 1 compare + 1 store beats the table.
    // Unpredictable count distributions fall into the flat branchless core, so the worst
    // case stays bounded, same trade as the integer writers.

    /// <summary>Writes an array header. Destination must have 5 bytes. count must be non-negative.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteArrayHeader(ref byte destination, int count)
    {
        if ((uint)count <= MessagePackCode.MaxFixArrayCount)
        {
            destination = (byte)(MessagePackCode.MinFixArray | count);
            return 1;
        }
        return WriteHeaderCore(ref destination, (uint)count, ref MemoryMarshal.GetReference(ArrayHeaderFormats));
    }

    /// <summary>
    /// Writes a fixarray header. Destination must have 1 byte. Unlike the other Unsafe
    /// methods this also has a value contract: count must be 0..15, a violation silently
    /// corrupts the stream. Intended for source-generated call sites where the count is a
    /// compile-time constant; keeps the inlined IL to two instructions so huge generated
    /// serializers don't burn JIT inliner budget on the general header body.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteFixArrayHeader(ref byte destination, int count)
    {
        Debug.Assert((uint)count <= MessagePackCode.MaxFixArrayCount);
        destination = (byte)(MessagePackCode.MinFixArray | count);
        return 1;
    }

    /// <summary>Writes a map header. Destination must have 5 bytes. count must be non-negative.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteMapHeader(ref byte destination, int count)
    {
        if ((uint)count <= MessagePackCode.MaxFixMapCount)
        {
            destination = (byte)(MessagePackCode.MinFixMap | count);
            return 1;
        }
        return WriteHeaderCore(ref destination, (uint)count, ref MemoryMarshal.GetReference(MapHeaderFormats));
    }

    /// <summary>
    /// Writes a fixmap header. Destination must have 1 byte. Same contract as
    /// <see cref="UnsafeWriteFixArrayHeader"/>: count must be 0..15, caller-verified
    /// (source-generated constant counts).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteFixMapHeader(ref byte destination, int count)
    {
        Debug.Assert((uint)count <= MessagePackCode.MaxFixMapCount);
        destination = (byte)(MessagePackCode.MinFixMap | count);
        return 1;
    }

    /// <summary>Writes a str header. Destination must have 5 bytes. byteCount must be non-negative.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteStringHeader(ref byte destination, int byteCount)
    {
        if ((uint)byteCount <= MessagePackCode.MaxFixStringLength)
        {
            destination = (byte)(MessagePackCode.MinFixStr | byteCount);
            return 1;
        }
        return WriteHeaderCore(ref destination, (uint)byteCount, ref MemoryMarshal.GetReference(StringHeaderFormats));
    }

    /// <summary>Writes a bin header. Destination must have 5 bytes. byteCount must be non-negative.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteBinHeader(ref byte destination, int byteCount)
        => WriteHeaderCore(ref destination, (uint)byteCount, ref MemoryMarshal.GetReference(BinHeaderFormats));

    // WriteAs*Header = always the named format regardless of count, same rule as the
    // WriteAs* value writers. The primary use is fixed-size placeholder headers written
    // before the count is known (sequences of unknown length) and patched afterwards.

    /// <summary>Writes an array32 header (0xdd). Destination must have 5 bytes. Returns 5. count must be non-negative.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteAsArray32Header(ref byte destination, int count)
    {
        destination = MessagePackCode.Array32;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((uint)count));
        return 5;
    }

    /// <summary>Writes a map32 header (0xdf). Destination must have 5 bytes. Returns 5. count must be non-negative.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteAsMap32Header(ref byte destination, int count)
    {
        destination = MessagePackCode.Map32;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((uint)count));
        return 5;
    }

    /// <summary>Writes a str32 header (0xdb). Destination must have 5 bytes. Returns 5. byteCount must be non-negative.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteAsStr32Header(ref byte destination, int byteCount)
    {
        destination = MessagePackCode.Str32;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((uint)byteCount));
        return 5;
    }

    /// <summary>Writes a bin32 header (0xc6). Destination must have 5 bytes. Returns 5. byteCount must be non-negative.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteAsBin32Header(ref byte destination, int byteCount)
    {
        destination = MessagePackCode.Bin32;
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((uint)byteCount));
        return 5;
    }

    /// <summary>Writes a bin payload. Destination must have value.Length + 5 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteBinary(ref byte destination, ReadOnlySpan<byte> value)
    {
        // bin header class is exact upfront (the length IS the byte count); the fast header's
        // scratch bytes land in [1..5) and are overwritten by the payload copy right after
        int headerSize = UnsafeWriteBinHeader(ref destination, value.Length);
        value.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination, headerSize), value.Length));
        return headerSize + value.Length;
    }

    #endregion

    #region Ext, Timestamp

    const uint WE8 = 3 | ((uint)MessagePackCode.Ext8 << 8);
    const uint WE16 = 4 | ((uint)MessagePackCode.Ext16 << 8);
    const uint WE32 = 6 | ((uint)MessagePackCode.Ext32 << 8);
    const uint WF1 = 2 | ((uint)MessagePackCode.FixExt1 << 8);
    const uint WF2 = 2 | ((uint)MessagePackCode.FixExt2 << 8);
    const uint WF4 = 2 | ((uint)MessagePackCode.FixExt4 << 8);
    const uint WF8 = 2 | ((uint)MessagePackCode.FixExt8 << 8);
    const uint WF16 = 2 | ((uint)MessagePackCode.FixExt16 << 8);

    // idx 0..31 = significant bits of dataLength (non-fix forms), idx 32..37 = fix flag set:
    // 32 is dataLength == 0 (passes the pow2-or-zero test, must stay ext8), 33..37 are
    // fixext1/2/4/8/16 (bits = log2 + 1, and the FixExt codes are consecutive)
    static ReadOnlySpan<uint> ExtHeaderFormats =>
    [
        // bits 0..8 ext8
        WE8, WE8, WE8, WE8, WE8, WE8, WE8, WE8, WE8,
        // bits 9..16 ext16
        WE16, WE16, WE16, WE16, WE16, WE16, WE16, WE16,
        // bits 17..31 ext32
        WE32, WE32, WE32, WE32, WE32, WE32, WE32, WE32, WE32, WE32, WE32, WE32, WE32, WE32, WE32,
        // idx 32: dataLength == 0 (arrives with the fix flag) -> ext8
        WE8,
        // idx 33..37: fixext1/2/4/8/16
        WF1, WF2, WF4, WF8, WF16,
    ];

    /// <summary>
    /// Writes an ext header: fixext1/2/4/8/16 (2 bytes) when dataLength is exactly
    /// 1/2/4/8/16, otherwise ext8/16/32 (3/4/6 bytes) — same format choice as
    /// MessagePack-CSharp. Destination must have 6 bytes. dataLength must be non-negative.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteExtHeader(ref byte destination, sbyte typeCode, int dataLength)
    {
        // Fully branchless (loop-counter-only asm, ExtTimestampWriteBenchmark): the fixext
        // class test is an arithmetic flag folded into the table index, so every format
        // class runs the same instruction path. Unlike the container headers, ext lengths
        // have no dominant fix range to justify a hybrid front: the flat core is level
        // ~1.13ns on every distribution, vs the cascade's 0.49ns predictable / 4.08ns
        // mixed — same bounded-worst-case trade as the bin header.

        // fixMask == 0 iff dataLength in {0, 1, 2, 4, 8, 16}: pow2-or-zero AND below 32
        int fixMask = (dataLength & (dataLength - 1)) | (dataLength & ~31);
        int isFix = (int)((uint)(fixMask - 1) >> 31); // 1 when fixMask == 0
        int bits = 32 - BitOperations.LeadingZeroCount((uint)dataLength); // 0..31
        int idx = (isFix << 5) | bits;
        uint e = Unsafe.Add(ref MemoryMarshal.GetReference(ExtHeaderFormats), idx);
        int len = (int)(e & 0xff);
        destination = (byte)(e >> 8);
        // len in {3,4,6}: shift in {24,16,0} puts the big-endian length right after the
        // header; len == 2 (fixext) computes shift 32 -> masked to 0 by C#, but all four
        // stored bytes are scratch there (typeCode overwrites offset 1 just below)
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((uint)dataLength << ((6 - len) * 8)));
        Unsafe.Add(ref destination, len - 1) = unchecked((byte)typeCode);
        return len;
    }

    const long BclSecondsAtUnixEpoch = 62135596800; // DateTime.UnixEpoch.Ticks / TicksPerSecond
    const int NanosecondsPerTick = 100;

    /// <summary>
    /// Writes a msgpack timestamp (ext type -1) in the smallest form. Destination must have 15 bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteTimestamp(ref byte destination, DateTime value)
    {
        // Timestamp spec: https://github.com/msgpack/msgpack/blob/master/spec.md#timestamp-extension-type
        // timestamp 32: FixExt4(-1) => seconds |  [1970-01-01 00:00:00 UTC, 2106-02-07 06:28:16 UTC) range. Nanoseconds part is 0
        // timestamp 64: FixExt8(-1) => nanoseconds + seconds | [1970-01-01 00:00:00.000000000 UTC, 2514-05-30 01:53:04.000000000 UTC) range
        // timestamp 96: Ext8(12,-1) => nanoseconds + seconds | [-584554047284-02-23 16:59:44 UTC, 584554051223-11-09 07:00:16.000000000 UTC) range

        if (value.Kind == DateTimeKind.Local)
        {
            value = value.ToUniversalTime();
        }

        long seconds = value.Ticks / TimeSpan.TicksPerSecond - BclSecondsAtUnixEpoch;
        long nanoseconds = value.Ticks % TimeSpan.TicksPerSecond * NanosecondsPerTick;

        // timestamp32/64 collapse to one flat path (both are fixext with the same layout;
        // 0xd6/0xd7 differ by 1 and the ts32 payload is data64's low u32 lifted to the high
        // half for one unconditional 8-byte store) — the 32-vs-64 pick is per-value data
        // (ns == 0 or not) and mispredicts hard as a branch: mixed-precision streams ran
        // 5.08ns -> 2.13ns (ExtTimestampWriteBenchmark). Only timestamp96 branches; real
        // data is essentially never pre-1970/post-2514, so that branch stays predicted.

        if ((ulong)seconds >> 34 == 0) // ts64 is 0(unix epoch) ~ 2^34(2514-05-30 01:53:04 UTC)
        {
            // data64: [nanoseconds in 30-bit | seconds in 34-bit]
            ulong data64 = (ulong)(nanoseconds << 34) | (ulong)seconds;
            ulong hi = data64 >> 32;
            int wide = (int)((hi | (0UL - hi)) >> 63); // 1 -> timestamp64 (data64 needs more than 4 bytes)
            // one 2-byte store: [0xd6 + wide, 0xff] (no carry into the type byte)
            Unsafe.WriteUnaligned(ref destination, (ushort)(0xffd6 + (uint)wide));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 2), BinaryPrimitives.ReverseEndianness(data64 << ((1 - wide) << 5)));
            return 6 + (wide << 2);
        }
        else
        {
            // In .NET, UtcNow will typically use the Timestamp 64 format so Timestamp 96 as slow-path.
            return WriteTimestamp96(ref destination, seconds, nanoseconds);
        }

        // cold (pre-1970/post-2514 only), out of line so the AggressiveInlining hot body
        // stays a Kind check + constant div/mod + two stores at every call site
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int WriteTimestamp96(ref byte destination, long seconds, long nanoseconds)
        {
            // data96: [nanoseconds in 32-bit unsigned | seconds in 64-bit signed]
            // one 8-byte store packs [Ext8][len 12][type -1][nanoseconds BE][scratch]
            // (little-endian: low byte lands first); the seconds store at offset 7
            // overwrites the scratch byte
            ulong head = MessagePackCode.Ext8 | (12u << 8)
                | ((uint)unchecked((byte)MessagePackCode.TimestampExtensionTypeCode) << 16)
                | ((ulong)BinaryPrimitives.ReverseEndianness((uint)nanoseconds) << 24);
            Unsafe.WriteUnaligned(ref destination, head);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 7), BinaryPrimitives.ReverseEndianness(unchecked((ulong)seconds)));
            return 15;
        }
    }

    #endregion

    #region String

    // above this length the O(1) worst-case bound 3n+5 no longer fits in int; the huge
    // paths below switch to an exact GetByteCount walk so any string whose UTF-8 fits in
    // a byte[] stays writable (instead of failing on an artificial worst-case limit)
    const int MaxWorstCaseStringLength = (int.MaxValue - 5) / 3; // 715,827,880

    /// <summary>
    /// Destination size sufficient for UnsafeWriteString: largest header (5) + UTF-8 worst
    /// case (3 bytes per char); 1 (nil) when null. O(1) normally; for strings longer
    /// than 715,827,880 chars the worst-case bound overflows int, so this walks the
    /// string once (O(n)) for an exact count instead. Throws
    /// <see cref="OverflowException"/> / <see cref="ArgumentException"/> only when the
    /// encoded bytes cannot fit in an array at all.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMaxStringByteCount(string? value)
    {
        if (value == null)
        {
            return 1;
        }
        int length = value.Length;
        if (length > MaxWorstCaseStringLength)
        {
            return GetHugeStringByteCount(value);
        }
        return length * 3 + 5;

        // cold, out of line to keep the hot path within inlining budget. GetByteCount throws
        // ArgumentException when the utf8 length itself exceeds int.MaxValue; checked catches
        // the +5 wraparound just below that (a negative return would pass callers' capacity
        // checks and turn into an out-of-bounds write inside UnsafeWriteString)
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int GetHugeStringByteCount(string value) => checked(Encoding.UTF8.GetByteCount(value) + 5);
    }

    /// <summary>
    /// Writes a string in the smallest msgpack str format (nil when null).
    /// Destination must have GetMaxStringByteCount(value) bytes (utf8 worst case + largest header).
    /// </summary>
    public static int UnsafeWriteString(ref byte destination, string? value) // no inlining
    {
        if (value == null)
        {
            return UnsafeWriteNil(ref destination);
        }

        int length = value.Length;
        if (length > MaxWorstCaseStringLength)
        {
            // the speculative path's 3 * length span would overflow int here
            return WriteHugeString(ref destination, value);
        }

        // Single pass, class-stability strategy: byteCount always lies in [length, 3*length],
        // so wherever class(length) == class(3*length) the guessed header size is provably
        // exact (L in [0..10], [32..85], [256..21845], [65536..]); in the straddle zones the
        // guess is exact for ASCII (byteCount == length), and only non-ASCII near a class
        // boundary pays a payload memmove — far cheaper than a second UTF8 walk (GetByteCount).
        int headerSize = length <= 31 ? 1
                       : length <= 255 ? 2
                       : length <= 65535 ? 3
                       : 5;

        // Utf8.FromUtf16 over Encoding.UTF8.GetBytes: both inline down to the same
        // Utf8Utility.TranscodeToUtf8, but GetBytes pays a frozen Encoding.UTF8 load plus a
        // pointer-diff/2 chain to recompute charsConsumed; FromUtf16's OperationStatus check
        // is 2 instructions and charsRead is discarded (-0.3ns/op on short strings).
        // Invalid surrogates replace with U+FFFD in both, so output is identical.
        Utf8.FromUtf16(value, MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination, headerSize), 3 * length), out _, out int byteCount);

        int actualHeaderSize = byteCount <= 31 ? 1
                             : byteCount <= 255 ? 2
                             : byteCount <= 65535 ? 3
                             : 5;
        if (actualHeaderSize != headerSize)
        {
            // byteCount >= length implies the guess can only be too small: move forward
            MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination, headerSize), byteCount)
                .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination, actualHeaderSize), byteCount));
        }

        // header written after the payload, so no scratch bytes allowed (unlike
        // UnsafeWriteStringHeader, whose scratch would clobber the payload start): once per string.
        // must switch on actualHeaderSize: when the guess missed, the payload was moved and
        // the header class is the recomputed one
        switch (actualHeaderSize)
        {
            case 1:
                destination = (byte)(MessagePackCode.MinFixStr | byteCount);
                break;
            case 2:
                destination = MessagePackCode.Str8;
                Unsafe.Add(ref destination, 1) = (byte)byteCount;
                break;
            case 3:
                destination = MessagePackCode.Str16;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((ushort)byteCount));
                break;
            default:
                destination = MessagePackCode.Str32;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((uint)byteCount));
                break;
        }

        return actualHeaderSize + byteCount;

        // byteCount >= length > 65535 means always str32: the header is known upfront, so no
        // speculation needed — one exact GetByteCount walk, then encode once at the final
        // position. GetByteCount (replacement fallback) and FromUtf16 (replace: true) agree
        // on U+FFFD for invalid surrogates, so the exact-size span is always filled exactly.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int WriteHugeString(ref byte destination, string value)
        {
            int byteCount = Encoding.UTF8.GetByteCount(value);
            destination = MessagePackCode.Str32;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((uint)byteCount));
            Utf8.FromUtf16(value, MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination, 5), byteCount), out _, out _);
            return checked(5 + byteCount);
        }
    }

    /// <summary>
    /// Writes a str from already-encoded UTF-8 bytes (the fast path for cached property
    /// names / u8 literals; the caller guarantees the bytes are valid UTF-8, they are
    /// copied as-is). Destination must have utf8Value.Length + 5 bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsafeWriteString(ref byte destination, ReadOnlySpan<byte> utf8Value)
    {
        // unlike the string overload no speculation is needed: the length IS the byte
        // count, so the header class is exact upfront (same shape as UnsafeWriteBinary);
        // the fast header's scratch bytes land in [1..5) and are overwritten by the
        // payload copy right after
        int headerSize = UnsafeWriteStringHeader(ref destination, utf8Value.Length);
        utf8Value.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination, headerSize), utf8Value.Length));
        return headerSize + utf8Value.Length;
    }

    #endregion
}
