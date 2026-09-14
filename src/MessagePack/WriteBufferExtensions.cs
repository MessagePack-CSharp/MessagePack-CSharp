using System.Diagnostics.CodeAnalysis;
using static MessagePack.MessagePackPrimitives;

namespace MessagePack;

/// <summary>
/// Writes single MessagePack tokens to any <see cref="IWriteBuffer"/>.
/// Each call reserves the worst-case size for the token and advances by the bytes actually written.
/// </summary>
public static class WriteBufferExtensions
{
    extension<TWriteBuffer>(ref TWriteBuffer buffer)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        /// <summary>Writes an int32 in the smallest msgpack format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int value)
        {
            buffer.Advance(UnsafeWriteInt32(ref buffer.GetReference(MaxInt32Length), value));
        }

        /// <summary>Writes an int64 in the smallest msgpack format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64(long value)
        {
            buffer.Advance(UnsafeWriteInt64(ref buffer.GetReference(MaxInt64Length), value));
        }

        /// <summary>Writes a uint32 in the smallest msgpack format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(uint value)
        {
            buffer.Advance(UnsafeWriteUInt32(ref buffer.GetReference(MaxUInt32Length), value));
        }

        /// <summary>Writes a uint64 in the smallest msgpack format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64(ulong value)
        {
            buffer.Advance(UnsafeWriteUInt64(ref buffer.GetReference(MaxUInt64Length), value));
        }

        /// <summary>Writes a byte in the smallest msgpack format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value)
        {
            buffer.Advance(UnsafeWriteByte(ref buffer.GetReference(MaxUInt8Length), value));
        }

        /// <summary>Writes an sbyte in the smallest msgpack format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSByte(sbyte value)
        {
            buffer.Advance(UnsafeWriteSByte(ref buffer.GetReference(MaxInt8Length), value));
        }

        /// <summary>Writes an int16 in the smallest msgpack format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16(short value)
        {
            buffer.Advance(UnsafeWriteInt16(ref buffer.GetReference(MaxInt16Length), value));
        }

        /// <summary>Writes a uint16 in the smallest msgpack format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16(ushort value)
        {
            buffer.Advance(UnsafeWriteUInt16(ref buffer.GetReference(MaxUInt16Length), value));
        }

        /// <summary>Writes a char as a uint16 in the smallest msgpack format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteChar(char value)
        {
            buffer.Advance(UnsafeWriteChar(ref buffer.GetReference(MaxUInt16Length), value));
        }

        // Forced-width writers (WriteForced<FormatName>). The format code always names the exact format regardless of
        // the value, unlike the plain writers' smallest-format encoding.
        // This is what lets PrimitiveObjectFormatter round-trip the .NET type of a boxed integer.

        /// <summary>Writes value in the int8 format regardless of its size.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedInt8(sbyte value)
        {
            buffer.Advance(UnsafeWriteForcedInt8(ref buffer.GetReference(MaxInt8Length), value));
        }

        /// <summary>Writes value in the uint8 format regardless of its size.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedUInt8(byte value)
        {
            buffer.Advance(UnsafeWriteForcedUInt8(ref buffer.GetReference(MaxUInt8Length), value));
        }

        /// <summary>Writes value in the int16 format regardless of its size.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedInt16(short value)
        {
            buffer.Advance(UnsafeWriteForcedInt16(ref buffer.GetReference(MaxInt16Length), value));
        }

        /// <summary>Writes value in the uint16 format regardless of its size.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedUInt16(ushort value)
        {
            buffer.Advance(UnsafeWriteForcedUInt16(ref buffer.GetReference(MaxUInt16Length), value));
        }

        /// <summary>Writes value in the int32 format regardless of its size.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedInt32(int value)
        {
            buffer.Advance(UnsafeWriteForcedInt32(ref buffer.GetReference(MaxInt32Length), value));
        }

        /// <summary>Writes value in the uint32 format regardless of its size.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedUInt32(uint value)
        {
            buffer.Advance(UnsafeWriteForcedUInt32(ref buffer.GetReference(MaxUInt32Length), value));
        }

        /// <summary>Writes value in the int64 format regardless of its size.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedInt64(long value)
        {
            buffer.Advance(UnsafeWriteForcedInt64(ref buffer.GetReference(MaxInt64Length), value));
        }

        /// <summary>Writes value in the uint64 format regardless of its size.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedUInt64(ulong value)
        {
            buffer.Advance(UnsafeWriteForcedUInt64(ref buffer.GetReference(MaxUInt64Length), value));
        }

        /// <summary>Writes nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteNil()
        {
            buffer.Advance(UnsafeWriteNil(ref buffer.GetReference(1)));
        }

        /// <summary>Writes a boolean.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBoolean(bool value)
        {
            buffer.Advance(UnsafeWriteBoolean(ref buffer.GetReference(1), value));
        }

        /// <summary>Writes a float32.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSingle(float value)
        {
            buffer.Advance(UnsafeWriteSingle(ref buffer.GetReference(MaxFloat32Length), value));
        }

        /// <summary>Writes a float64.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(double value)
        {
            buffer.Advance(UnsafeWriteDouble(ref buffer.GetReference(MaxFloat64Length), value));
        }

        /// <summary>Writes a string as a str in the smallest format, or nil when null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string? value)
        {
            buffer.Advance(UnsafeWriteString(ref buffer.GetReference(GetMaxStringByteCount(value)), value));
        }

        /// <summary>Writes a str from already-encoded UTF-8 bytes, copied as they are. The caller guarantees valid UTF-8.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(scoped ReadOnlySpan<byte> utf8Value)
        {
            buffer.Advance(UnsafeWriteString(ref buffer.GetReference(MaxStrHeaderLength + utf8Value.Length), utf8Value));
        }

        /// <summary>Writes a fixarray header. count must be 0..15.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixArrayHeader([ConstantExpected(Min = 0, Max = 15)] int count)
        {
            buffer.Advance(UnsafeWriteFixArrayHeader(ref buffer.GetReference(MaxArrayHeaderLength), count));
        }

        /// <summary>Writes an array header in the smallest format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayHeader(int count)
        {
            buffer.Advance(UnsafeWriteArrayHeader(ref buffer.GetReference(MaxArrayHeaderLength), count));
        }

        /// <summary>Writes a fixmap header. count must be 0..15.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixMapHeader([ConstantExpected(Min = 0, Max = 15)] int count)
        {
            buffer.Advance(UnsafeWriteFixMapHeader(ref buffer.GetReference(MaxMapHeaderLength), count));
        }

        /// <summary>Writes a map header in the smallest format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMapHeader(int count)
        {
            buffer.Advance(UnsafeWriteMapHeader(ref buffer.GetReference(MaxMapHeaderLength), count));
        }

        /// <summary>
        /// Writes a bin header, as bin8, bin16 or bin32 for byteCount.
        /// The caller then writes exactly byteCount payload bytes through <see cref="WriteRaw"/> or GetSpan and Advance.
        /// Use <see cref="WriteBinary"/> when the payload is already a single span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBinHeader(int byteCount)
        {
            buffer.Advance(UnsafeWriteBinHeader(ref buffer.GetReference(MaxBinHeaderLength), byteCount));
        }

        /// <summary>Writes a bin, header and payload.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBinary(ReadOnlySpan<byte> value)
        {
            buffer.Advance(UnsafeWriteBinary(ref buffer.GetReference(MaxBinHeaderLength + value.Length), value));
        }

        /// <summary>
        /// Writes a bin, header and payload, from a sequence whose segments are copied one by one without stitching.
        /// </summary>
        public void WriteBinary(in ReadOnlySequence<byte> value)
        {
            if (value.IsSingleSegment)
            {
                buffer.WriteBinary(value.First.Span);
                return;
            }
            buffer.WriteBinHeader(checked((int)value.Length));
            buffer.WriteRaw(value);
        }

        /// <summary>
        /// Writes an ext header, as fixext when dataLength is exactly 1, 2, 4, 8 or 16 and as ext8, ext16 or ext32 otherwise.
        /// The caller then writes exactly dataLength payload bytes through GetSpan and Advance. dataLength must be non-negative.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteExtHeader(sbyte typeCode, int dataLength)
        {
            buffer.Advance(UnsafeWriteExtHeader(ref buffer.GetReference(MaxExtHeaderLength), typeCode, dataLength));
        }

        /// <summary>Writes a msgpack timestamp (ext type -1) in the smallest form.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTimestamp(DateTime value)
        {
            buffer.Advance(UnsafeWriteTimestamp(ref buffer.GetReference(MaxTimestampLength), value));
        }

        /// <summary>
        /// Copies value into the output without interpreting it.
        /// value must be a complete MessagePack value, such as the bytes returned by ReadRaw, or the payload promised by a preceding header.
        /// Nothing is validated. An empty span writes nothing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRaw(ReadOnlySpan<byte> value)
        {
            if (value.Length > 0)
            {
                Unsafe.CopyBlockUnaligned(ref buffer.GetReference(value.Length), ref MemoryMarshal.GetReference(value), (uint)value.Length);
                buffer.Advance(value.Length);
            }
        }

        /// <summary>
        /// The <see cref="ReadOnlySequence{T}"/> form of <see cref="WriteRaw(ReadOnlySpan{byte})"/>, copying each segment in turn.
        /// The same contract applies to the sequence as a whole.
        /// </summary>
        public void WriteRaw(in ReadOnlySequence<byte> value)
        {
            if (value.IsSingleSegment)
            {
                buffer.WriteRaw(value.First.Span);
                return;
            }
            foreach (var segment in value)
            {
                buffer.WriteRaw(segment.Span);
            }
        }
    }
}
