using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

namespace UltraMessagePack;

// Span-checked write primitives for hand-written callers. Deliberately NOT wrappers over
// the UnsafeWrite* layer: that layer trades safety for speed (branchless cores, scratch
// stores, caller-guaranteed worst-case windows) and is what source-generated formatters
// target. This layer is a simple compare cascade with exact-size semantics instead:
// the destination only needs the bytes actually written for this value, nothing is
// written on failure (bytesWritten = 0, destination untouched), and value contracts that
// are Debug.Assert-only on the Unsafe side (negative counts, fix ranges) return false.
public static partial class MessagePackPrimitives
{
    // Unchecked big-endian stores for sites already dominated by an explicit Length
    // check — same library policy as the read side's UnsafeRead*BigEndian: the checked
    // BinaryPrimitives.WriteXxxBigEndian(span) forms keep their internal length check
    // (the JIT cannot propagate the guard through Slice), which costs a cmp+throw block
    // and a forced stack frame per call site. Guard once, never pay a second check.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void UnsafeWriteUInt16BigEndian(Span<byte> destination, int offset, ushort value)
        => Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(destination), offset), BinaryPrimitives.ReverseEndianness(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void UnsafeWriteUInt32BigEndian(Span<byte> destination, int offset, uint value)
        => Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(destination), offset), BinaryPrimitives.ReverseEndianness(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void UnsafeWriteUInt64BigEndian(Span<byte> destination, int offset, ulong value)
        => Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(destination), offset), BinaryPrimitives.ReverseEndianness(value));

    // exact-size slot writers shared by the cascades: one format code + big-endian payload
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryWrite1(Span<byte> destination, byte b0, out int bytesWritten)
    {
        if (destination.Length >= 1)
        {
            destination[0] = b0;
            bytesWritten = 1;
            return true;
        }
        bytesWritten = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryWrite2(Span<byte> destination, byte code, byte payload, out int bytesWritten)
    {
        if (destination.Length >= 2)
        {
            destination[0] = code;
            destination[1] = payload;
            bytesWritten = 2;
            return true;
        }
        bytesWritten = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryWrite3(Span<byte> destination, byte code, ushort payload, out int bytesWritten)
    {
        if (destination.Length >= 3)
        {
            destination[0] = code;
            UnsafeWriteUInt16BigEndian(destination, 1, payload);
            bytesWritten = 3;
            return true;
        }
        bytesWritten = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryWrite5(Span<byte> destination, byte code, uint payload, out int bytesWritten)
    {
        if (destination.Length >= 5)
        {
            destination[0] = code;
            UnsafeWriteUInt32BigEndian(destination, 1, payload);
            bytesWritten = 5;
            return true;
        }
        bytesWritten = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryWrite9(Span<byte> destination, byte code, ulong payload, out int bytesWritten)
    {
        if (destination.Length >= 9)
        {
            destination[0] = code;
            UnsafeWriteUInt64BigEndian(destination, 1, payload);
            bytesWritten = 9;
            return true;
        }
        bytesWritten = 0;
        return false;
    }

    #region TryWriteInt32, 64

    /// <summary>Writes an int32 in the smallest msgpack format. Requires only the encoded size (1..5 bytes).</summary>
    public static bool TryWriteInt32(Span<byte> destination, int value, out int bytesWritten)
    {
        if (value >= 0)
        {
            if (value <= 0x7f)
            {
                return TryWrite1(destination, unchecked((byte)value), out bytesWritten);
            }
            if (value <= byte.MaxValue)
            {
                return TryWrite2(destination, MessagePackCode.UInt8, (byte)value, out bytesWritten);
            }
            if (value <= ushort.MaxValue)
            {
                return TryWrite3(destination, MessagePackCode.UInt16, (ushort)value, out bytesWritten);
            }
            return TryWrite5(destination, MessagePackCode.UInt32, (uint)value, out bytesWritten);
        }
        if (value >= -32)
        {
            return TryWrite1(destination, unchecked((byte)value), out bytesWritten);
        }
        if (value >= sbyte.MinValue)
        {
            return TryWrite2(destination, MessagePackCode.Int8, unchecked((byte)value), out bytesWritten);
        }
        if (value >= short.MinValue)
        {
            return TryWrite3(destination, MessagePackCode.Int16, unchecked((ushort)value), out bytesWritten);
        }
        return TryWrite5(destination, MessagePackCode.Int32, unchecked((uint)value), out bytesWritten);
    }

    /// <summary>Writes an int64 in the smallest msgpack format. Requires only the encoded size (1..9 bytes).</summary>
    public static bool TryWriteInt64(Span<byte> destination, long value, out int bytesWritten)
    {
        if (value >= 0)
        {
            if (value <= int.MaxValue)
            {
                return TryWriteInt32(destination, (int)value, out bytesWritten);
            }
            if (value <= uint.MaxValue)
            {
                return TryWrite5(destination, MessagePackCode.UInt32, (uint)value, out bytesWritten);
            }
            return TryWrite9(destination, MessagePackCode.UInt64, (ulong)value, out bytesWritten);
        }
        if (value >= int.MinValue)
        {
            return TryWriteInt32(destination, (int)value, out bytesWritten);
        }
        return TryWrite9(destination, MessagePackCode.Int64, unchecked((ulong)value), out bytesWritten);
    }

    /// <summary>Writes a uint32 in the smallest msgpack format. Requires only the encoded size (1..5 bytes).</summary>
    public static bool TryWriteUInt32(Span<byte> destination, uint value, out int bytesWritten)
    {
        if (value <= 0x7f)
        {
            return TryWrite1(destination, (byte)value, out bytesWritten);
        }
        if (value <= byte.MaxValue)
        {
            return TryWrite2(destination, MessagePackCode.UInt8, (byte)value, out bytesWritten);
        }
        if (value <= ushort.MaxValue)
        {
            return TryWrite3(destination, MessagePackCode.UInt16, (ushort)value, out bytesWritten);
        }
        return TryWrite5(destination, MessagePackCode.UInt32, value, out bytesWritten);
    }

    /// <summary>Writes a uint64 in the smallest msgpack format. Requires only the encoded size (1..9 bytes).</summary>
    public static bool TryWriteUInt64(Span<byte> destination, ulong value, out int bytesWritten)
    {
        if (value <= uint.MaxValue)
        {
            return TryWriteUInt32(destination, (uint)value, out bytesWritten);
        }
        return TryWrite9(destination, MessagePackCode.UInt64, value, out bytesWritten);
    }

    #endregion

    #region TryWriteInt8, 16

    /// <summary>Writes a byte in the smallest msgpack format. Requires only the encoded size (1..2 bytes).</summary>
    public static bool TryWriteByte(Span<byte> destination, byte value, out int bytesWritten)
    {
        if (value <= 0x7f)
        {
            return TryWrite1(destination, value, out bytesWritten);
        }
        return TryWrite2(destination, MessagePackCode.UInt8, value, out bytesWritten);
    }

    /// <summary>Writes an sbyte in the smallest msgpack format. Requires only the encoded size (1..2 bytes).</summary>
    public static bool TryWriteSByte(Span<byte> destination, sbyte value, out int bytesWritten)
    {
        if (value >= -32)
        {
            return TryWrite1(destination, unchecked((byte)value), out bytesWritten);
        }
        return TryWrite2(destination, MessagePackCode.Int8, unchecked((byte)value), out bytesWritten);
    }

    /// <summary>Writes an int16 in the smallest msgpack format. Requires only the encoded size (1..3 bytes).</summary>
    public static bool TryWriteInt16(Span<byte> destination, short value, out int bytesWritten)
        => TryWriteInt32(destination, value, out bytesWritten);

    /// <summary>Writes a uint16 in the smallest msgpack format. Requires only the encoded size (1..3 bytes).</summary>
    public static bool TryWriteUInt16(Span<byte> destination, ushort value, out int bytesWritten)
        => TryWriteUInt32(destination, value, out bytesWritten);

    /// <summary>Writes a char in the smallest msgpack format (as uint16). Requires only the encoded size (1..3 bytes).</summary>
    public static bool TryWriteChar(Span<byte> destination, char value, out int bytesWritten)
        => TryWriteUInt32(destination, value, out bytesWritten);

    #endregion

    #region fixed-length(Nil, Boolean, Single, Double)

    /// <summary>Writes nil. Requires 1 byte.</summary>
    public static bool TryWriteNil(Span<byte> destination, out int bytesWritten)
        => TryWrite1(destination, MessagePackCode.Nil, out bytesWritten);

    /// <summary>Writes a boolean. Requires 1 byte.</summary>
    public static bool TryWriteBoolean(Span<byte> destination, bool value, out int bytesWritten)
        => TryWrite1(destination, value ? MessagePackCode.True : MessagePackCode.False, out bytesWritten);

    /// <summary>Writes a float32. Requires 5 bytes.</summary>
    public static bool TryWriteSingle(Span<byte> destination, float value, out int bytesWritten)
        => TryWrite5(destination, MessagePackCode.Float32, BitConverter.SingleToUInt32Bits(value), out bytesWritten);

    /// <summary>Writes a float64. Requires 9 bytes.</summary>
    public static bool TryWriteDouble(Span<byte> destination, double value, out int bytesWritten)
        => TryWrite9(destination, MessagePackCode.Float64, BitConverter.DoubleToUInt64Bits(value), out bytesWritten);

    #endregion

    #region headers(array, map, string, bin), binary

    /// <summary>Writes an array header in the smallest format. Requires only the encoded size (1..5 bytes). False when count is negative.</summary>
    public static bool TryWriteArrayHeader(Span<byte> destination, int count, out int bytesWritten)
    {
        if (count < 0)
        {
            bytesWritten = 0;
            return false;
        }
        if (count <= MessagePackCode.MaxFixArrayCount)
        {
            return TryWrite1(destination, (byte)(MessagePackCode.MinFixArray | count), out bytesWritten);
        }
        if (count <= ushort.MaxValue)
        {
            return TryWrite3(destination, MessagePackCode.Array16, (ushort)count, out bytesWritten);
        }
        return TryWrite5(destination, MessagePackCode.Array32, (uint)count, out bytesWritten);
    }

    /// <summary>Writes a map header in the smallest format. Requires only the encoded size (1..5 bytes). False when count is negative.</summary>
    public static bool TryWriteMapHeader(Span<byte> destination, int count, out int bytesWritten)
    {
        if (count < 0)
        {
            bytesWritten = 0;
            return false;
        }
        if (count <= MessagePackCode.MaxFixMapCount)
        {
            return TryWrite1(destination, (byte)(MessagePackCode.MinFixMap | count), out bytesWritten);
        }
        if (count <= ushort.MaxValue)
        {
            return TryWrite3(destination, MessagePackCode.Map16, (ushort)count, out bytesWritten);
        }
        return TryWrite5(destination, MessagePackCode.Map32, (uint)count, out bytesWritten);
    }

    /// <summary>Writes a fixarray header. Requires 1 byte. False when count is outside 0..15.</summary>
    public static bool TryWriteFixArrayHeader(Span<byte> destination, int count, out int bytesWritten)
    {
        if ((uint)count <= MessagePackCode.MaxFixArrayCount)
        {
            return TryWrite1(destination, (byte)(MessagePackCode.MinFixArray | count), out bytesWritten);
        }
        bytesWritten = 0;
        return false;
    }

    /// <summary>Writes a fixmap header. Requires 1 byte. False when count is outside 0..15.</summary>
    public static bool TryWriteFixMapHeader(Span<byte> destination, int count, out int bytesWritten)
    {
        if ((uint)count <= MessagePackCode.MaxFixMapCount)
        {
            return TryWrite1(destination, (byte)(MessagePackCode.MinFixMap | count), out bytesWritten);
        }
        bytesWritten = 0;
        return false;
    }

    /// <summary>Writes a str header in the smallest format. Requires only the encoded size (1..5 bytes). False when byteCount is negative.</summary>
    public static bool TryWriteStringHeader(Span<byte> destination, int byteCount, out int bytesWritten)
    {
        if (byteCount < 0)
        {
            bytesWritten = 0;
            return false;
        }
        if (byteCount <= MessagePackCode.MaxFixStringLength)
        {
            return TryWrite1(destination, (byte)(MessagePackCode.MinFixStr | byteCount), out bytesWritten);
        }
        if (byteCount <= byte.MaxValue)
        {
            return TryWrite2(destination, MessagePackCode.Str8, (byte)byteCount, out bytesWritten);
        }
        if (byteCount <= ushort.MaxValue)
        {
            return TryWrite3(destination, MessagePackCode.Str16, (ushort)byteCount, out bytesWritten);
        }
        return TryWrite5(destination, MessagePackCode.Str32, (uint)byteCount, out bytesWritten);
    }

    /// <summary>Writes a bin header in the smallest format. Requires only the encoded size (2..5 bytes). False when byteCount is negative.</summary>
    public static bool TryWriteBinHeader(Span<byte> destination, int byteCount, out int bytesWritten)
    {
        if (byteCount < 0)
        {
            bytesWritten = 0;
            return false;
        }
        if (byteCount <= byte.MaxValue)
        {
            return TryWrite2(destination, MessagePackCode.Bin8, (byte)byteCount, out bytesWritten);
        }
        if (byteCount <= ushort.MaxValue)
        {
            return TryWrite3(destination, MessagePackCode.Bin16, (ushort)byteCount, out bytesWritten);
        }
        return TryWrite5(destination, MessagePackCode.Bin32, (uint)byteCount, out bytesWritten);
    }

    /// <summary>Writes a bin (header + payload). Requires only the encoded size (header + value.Length bytes).</summary>
    public static bool TryWriteBinary(Span<byte> destination, ReadOnlySpan<byte> value, out int bytesWritten)
    {
        int headerSize = value.Length <= byte.MaxValue ? 2 : value.Length <= ushort.MaxValue ? 3 : 5;
        // subtraction form so headerSize + value.Length can't overflow int
        if (destination.Length - headerSize >= value.Length)
        {
            TryWriteBinHeader(destination, value.Length, out _);
            value.CopyTo(destination.Slice(headerSize));
            bytesWritten = headerSize + value.Length;
            return true;
        }
        bytesWritten = 0;
        return false;
    }

    #endregion

    #region Ext, Timestamp

    /// <summary>Writes an ext header (fixext or ext8/16/32). Requires only the encoded size (2..6 bytes). False when dataLength is negative.</summary>
    public static bool TryWriteExtHeader(Span<byte> destination, sbyte typeCode, int dataLength, out int bytesWritten)
    {
        if (dataLength < 0)
        {
            bytesWritten = 0;
            return false;
        }
        if (BitOperations.IsPow2(dataLength) && (uint)dataLength <= 16)
        {
            return TryWrite2(destination, (byte)(MessagePackCode.FixExt1 + BitOperations.Log2((uint)dataLength)), unchecked((byte)typeCode), out bytesWritten);
        }
        if (dataLength <= byte.MaxValue)
        {
            if (destination.Length >= 3)
            {
                destination[0] = MessagePackCode.Ext8;
                destination[1] = (byte)dataLength;
                destination[2] = unchecked((byte)typeCode);
                bytesWritten = 3;
                return true;
            }
        }
        else if (dataLength <= ushort.MaxValue)
        {
            if (destination.Length >= 4)
            {
                destination[0] = MessagePackCode.Ext16;
                UnsafeWriteUInt16BigEndian(destination, 1, (ushort)dataLength);
                destination[3] = unchecked((byte)typeCode);
                bytesWritten = 4;
                return true;
            }
        }
        else if (destination.Length >= 6)
        {
            destination[0] = MessagePackCode.Ext32;
            UnsafeWriteUInt32BigEndian(destination, 1, (uint)dataLength);
            destination[5] = unchecked((byte)typeCode);
            bytesWritten = 6;
            return true;
        }
        bytesWritten = 0;
        return false;
    }

    /// <summary>Writes a msgpack timestamp (ext type -1) in the smallest form. Requires only the encoded size (6, 10, or 15 bytes).</summary>
    public static bool TryWriteTimestamp(Span<byte> destination, DateTime value, out int bytesWritten)
    {
        if (value.Kind == DateTimeKind.Local)
        {
            value = value.ToUniversalTime();
        }

        long seconds = value.Ticks / TimeSpan.TicksPerSecond - BclSecondsAtUnixEpoch;
        long nanoseconds = value.Ticks % TimeSpan.TicksPerSecond * NanosecondsPerTick;

        if ((ulong)seconds >> 34 == 0)
        {
            // data64: [nanoseconds in 30-bit | seconds in 34-bit]
            ulong data64 = (ulong)(nanoseconds << 34) | (ulong)seconds;
            if (data64 >> 32 == 0)
            {
                // timestamp 32: fixext4(-1), seconds only
                if (destination.Length >= 6)
                {
                    destination[0] = MessagePackCode.FixExt4;
                    destination[1] = unchecked((byte)MessagePackCode.TimestampExtensionTypeCode);
                    UnsafeWriteUInt32BigEndian(destination, 2, (uint)data64);
                    bytesWritten = 6;
                    return true;
                }
            }
            else
            {
                // timestamp 64: fixext8(-1)
                if (destination.Length >= 10)
                {
                    destination[0] = MessagePackCode.FixExt8;
                    destination[1] = unchecked((byte)MessagePackCode.TimestampExtensionTypeCode);
                    UnsafeWriteUInt64BigEndian(destination, 2, data64);
                    bytesWritten = 10;
                    return true;
                }
            }
        }
        else if (destination.Length >= 15)
        {
            // timestamp 96: ext8(12, -1), nanoseconds u32 + seconds i64
            destination[0] = MessagePackCode.Ext8;
            destination[1] = 12;
            destination[2] = unchecked((byte)MessagePackCode.TimestampExtensionTypeCode);
            UnsafeWriteUInt32BigEndian(destination, 3, (uint)nanoseconds);
            UnsafeWriteUInt64BigEndian(destination, 7, unchecked((ulong)seconds));
            bytesWritten = 15;
            return true;
        }
        bytesWritten = 0;
        return false;
    }

    #endregion

    #region String

    /// <summary>
    /// Writes a string in the smallest msgpack str format (nil when null). Requires only
    /// the encoded size (exact UTF-8 byte count + header); unlike UnsafeWriteString this
    /// walks the string once upfront (GetByteCount) instead of speculating.
    /// </summary>
    public static bool TryWriteString(Span<byte> destination, string? value, out int bytesWritten)
    {
        if (value == null)
        {
            return TryWriteNil(destination, out bytesWritten);
        }
        int byteCount = Encoding.UTF8.GetByteCount(value);
        int headerSize = byteCount <= MessagePackCode.MaxFixStringLength ? 1 : byteCount <= byte.MaxValue ? 2 : byteCount <= ushort.MaxValue ? 3 : 5;
        // subtraction form so headerSize + byteCount can't overflow int
        if (destination.Length - headerSize >= byteCount)
        {
            TryWriteStringHeader(destination, byteCount, out _);
            // GetByteCount (replacement fallback) and FromUtf16 (replace: true) agree on
            // U+FFFD for invalid surrogates, so the exact-size span is always filled exactly
            Utf8.FromUtf16(value, destination.Slice(headerSize, byteCount), out _, out _);
            bytesWritten = headerSize + byteCount;
            return true;
        }
        bytesWritten = 0;
        return false;
    }

    /// <summary>Writes a str from already-encoded UTF-8 bytes. Requires only the encoded size (header + utf8Value.Length bytes).</summary>
    public static bool TryWriteString(Span<byte> destination, ReadOnlySpan<byte> utf8Value, out int bytesWritten)
    {
        int byteCount = utf8Value.Length;
        int headerSize = byteCount <= MessagePackCode.MaxFixStringLength ? 1 : byteCount <= byte.MaxValue ? 2 : byteCount <= ushort.MaxValue ? 3 : 5;
        // subtraction form so headerSize + byteCount can't overflow int
        if (destination.Length - headerSize >= byteCount)
        {
            TryWriteStringHeader(destination, byteCount, out _);
            utf8Value.CopyTo(destination.Slice(headerSize));
            bytesWritten = headerSize + byteCount;
            return true;
        }
        bytesWritten = 0;
        return false;
    }

    #endregion

    #region forced-width (TryWriteAs*)

    /// <summary>Writes value in the int8 format (0xd0). Requires 2 bytes.</summary>
    public static bool TryWriteAsInt8(Span<byte> destination, sbyte value, out int bytesWritten)
        => TryWrite2(destination, MessagePackCode.Int8, unchecked((byte)value), out bytesWritten);

    /// <summary>Writes value in the uint8 format (0xcc). Requires 2 bytes.</summary>
    public static bool TryWriteAsUInt8(Span<byte> destination, byte value, out int bytesWritten)
        => TryWrite2(destination, MessagePackCode.UInt8, value, out bytesWritten);

    /// <summary>Writes value in the int16 format (0xd1). Requires 3 bytes.</summary>
    public static bool TryWriteAsInt16(Span<byte> destination, short value, out int bytesWritten)
        => TryWrite3(destination, MessagePackCode.Int16, unchecked((ushort)value), out bytesWritten);

    /// <summary>Writes value in the uint16 format (0xcd). Requires 3 bytes.</summary>
    public static bool TryWriteAsUInt16(Span<byte> destination, ushort value, out int bytesWritten)
        => TryWrite3(destination, MessagePackCode.UInt16, value, out bytesWritten);

    /// <summary>Writes value in the int32 format (0xd2). Requires 5 bytes.</summary>
    public static bool TryWriteAsInt32(Span<byte> destination, int value, out int bytesWritten)
        => TryWrite5(destination, MessagePackCode.Int32, unchecked((uint)value), out bytesWritten);

    /// <summary>Writes value in the uint32 format (0xce). Requires 5 bytes.</summary>
    public static bool TryWriteAsUInt32(Span<byte> destination, uint value, out int bytesWritten)
        => TryWrite5(destination, MessagePackCode.UInt32, value, out bytesWritten);

    /// <summary>Writes value in the int64 format (0xd3). Requires 9 bytes.</summary>
    public static bool TryWriteAsInt64(Span<byte> destination, long value, out int bytesWritten)
        => TryWrite9(destination, MessagePackCode.Int64, unchecked((ulong)value), out bytesWritten);

    /// <summary>Writes value in the uint64 format (0xcf). Requires 9 bytes.</summary>
    public static bool TryWriteAsUInt64(Span<byte> destination, ulong value, out int bytesWritten)
        => TryWrite9(destination, MessagePackCode.UInt64, value, out bytesWritten);

    /// <summary>Writes an array32 header (0xdd). Requires 5 bytes. False when count is negative.</summary>
    public static bool TryWriteAsArray32Header(Span<byte> destination, int count, out int bytesWritten)
    {
        if (count >= 0)
        {
            return TryWrite5(destination, MessagePackCode.Array32, (uint)count, out bytesWritten);
        }
        bytesWritten = 0;
        return false;
    }

    /// <summary>Writes a map32 header (0xdf). Requires 5 bytes. False when count is negative.</summary>
    public static bool TryWriteAsMap32Header(Span<byte> destination, int count, out int bytesWritten)
    {
        if (count >= 0)
        {
            return TryWrite5(destination, MessagePackCode.Map32, (uint)count, out bytesWritten);
        }
        bytesWritten = 0;
        return false;
    }

    /// <summary>Writes a str32 header (0xdb). Requires 5 bytes. False when byteCount is negative.</summary>
    public static bool TryWriteAsStr32Header(Span<byte> destination, int byteCount, out int bytesWritten)
    {
        if (byteCount >= 0)
        {
            return TryWrite5(destination, MessagePackCode.Str32, (uint)byteCount, out bytesWritten);
        }
        bytesWritten = 0;
        return false;
    }

    /// <summary>Writes a bin32 header (0xc6). Requires 5 bytes. False when byteCount is negative.</summary>
    public static bool TryWriteAsBin32Header(Span<byte> destination, int byteCount, out int bytesWritten)
    {
        if (byteCount >= 0)
        {
            return TryWrite5(destination, MessagePackCode.Bin32, (uint)byteCount, out bytesWritten);
        }
        bytesWritten = 0;
        return false;
    }

    #endregion
}
