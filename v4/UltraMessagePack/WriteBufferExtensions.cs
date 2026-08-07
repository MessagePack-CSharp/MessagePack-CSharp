using SerializerFoundation;
using static UltraMessagePack.MessagePackPrimitives;

namespace UltraMessagePack;

public static class WriteBufferExtensions
{
    extension<TWriteBuffer>(ref TWriteBuffer buffer)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int value)
        {
            buffer.Advance(UnsafeWriteInt32(ref buffer.GetReference(MaxInt32Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64(long value)
        {
            buffer.Advance(UnsafeWriteInt64(ref buffer.GetReference(MaxInt64Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(uint value)
        {
            buffer.Advance(UnsafeWriteUInt32(ref buffer.GetReference(MaxUInt32Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64(ulong value)
        {
            buffer.Advance(UnsafeWriteUInt64(ref buffer.GetReference(MaxUInt64Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value)
        {
            buffer.Advance(UnsafeWriteByte(ref buffer.GetReference(MaxUInt8Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSByte(sbyte value)
        {
            buffer.Advance(UnsafeWriteSByte(ref buffer.GetReference(MaxInt8Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16(short value)
        {
            buffer.Advance(UnsafeWriteInt16(ref buffer.GetReference(MaxInt16Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16(ushort value)
        {
            buffer.Advance(UnsafeWriteUInt16(ref buffer.GetReference(MaxUInt16Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteChar(char value)
        {
            buffer.Advance(UnsafeWriteChar(ref buffer.GetReference(MaxUInt16Length), value));
        }

        // Forced-width writers (WriteForced<FormatName>): the wire code always names the
        // exact format regardless of the value, unlike the plain writers' smallest-format
        // encoding. This is what lets PrimitiveObjectFormatter round-trip the .NET type
        // of a boxed integer through the wire.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedInt8(sbyte value)
        {
            buffer.Advance(UnsafeWriteForcedInt8(ref buffer.GetReference(MaxInt8Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedUInt8(byte value)
        {
            buffer.Advance(UnsafeWriteForcedUInt8(ref buffer.GetReference(MaxUInt8Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedInt16(short value)
        {
            buffer.Advance(UnsafeWriteForcedInt16(ref buffer.GetReference(MaxInt16Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedUInt16(ushort value)
        {
            buffer.Advance(UnsafeWriteForcedUInt16(ref buffer.GetReference(MaxUInt16Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedInt32(int value)
        {
            buffer.Advance(UnsafeWriteForcedInt32(ref buffer.GetReference(MaxInt32Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedUInt32(uint value)
        {
            buffer.Advance(UnsafeWriteForcedUInt32(ref buffer.GetReference(MaxUInt32Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedInt64(long value)
        {
            buffer.Advance(UnsafeWriteForcedInt64(ref buffer.GetReference(MaxInt64Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteForcedUInt64(ulong value)
        {
            buffer.Advance(UnsafeWriteForcedUInt64(ref buffer.GetReference(MaxUInt64Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteNil()
        {
            buffer.Advance(UnsafeWriteNil(ref buffer.GetReference(1)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBoolean(bool value)
        {
            buffer.Advance(UnsafeWriteBoolean(ref buffer.GetReference(1), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSingle(float value)
        {
            buffer.Advance(UnsafeWriteSingle(ref buffer.GetReference(MaxFloat32Length), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string? value)
        {
            buffer.Advance(UnsafeWriteString(ref buffer.GetReference(GetMaxStringByteCount(value)), value));
        }

        /// <summary>Writes a str from already-encoded UTF-8 bytes (the caller guarantees valid UTF-8; copied as-is).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(scoped ReadOnlySpan<byte> utf8Value)
        {
            buffer.Advance(UnsafeWriteString(ref buffer.GetReference(MaxStrHeaderLength + utf8Value.Length), utf8Value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixArrayHeader(int count)
        {
            buffer.Advance(UnsafeWriteFixArrayHeader(ref buffer.GetReference(MaxArrayHeaderLength), count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayHeader(int count)
        {
            buffer.Advance(UnsafeWriteArrayHeader(ref buffer.GetReference(MaxArrayHeaderLength), count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMapHeader(int count)
        {
            buffer.Advance(UnsafeWriteMapHeader(ref buffer.GetReference(MaxMapHeaderLength), count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBinary(ReadOnlySpan<byte> value)
        {
            buffer.Advance(UnsafeWriteBinary(ref buffer.GetReference(MaxBinHeaderLength + value.Length), value));
        }

        // header-only + raw-payload pair for writers whose payload is not contiguous
        // (multi-segment ReadOnlySequence<byte>). Internal: a mismatched header/raw pair
        // corrupts the framing, so this stays off the public surface (same policy as the
        // Unsafe* primitives).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void WriteBinaryHeader(int byteCount)
        {
            buffer.Advance(UnsafeWriteBinHeader(ref buffer.GetReference(MaxBinHeaderLength), byteCount));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void WriteRaw(ReadOnlySpan<byte> value)
        {
            if (value.Length > 0)
            {
                Unsafe.CopyBlockUnaligned(ref buffer.GetReference(value.Length), ref MemoryMarshal.GetReference(value), (uint)value.Length);
                buffer.Advance(value.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTimestamp(DateTime value)
        {
            buffer.Advance(UnsafeWriteTimestamp(ref buffer.GetReference(MaxTimestampLength), value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(double value)
        {
            buffer.Advance(UnsafeWriteDouble(ref buffer.GetReference(MaxFloat64Length), value));
        }
    }
}
