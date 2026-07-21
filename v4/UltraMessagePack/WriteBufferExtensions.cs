using SerializerFoundation;
using static UltraMessagePack.MessagePackPrimitives;

namespace UltraMessagePack;

public static class WriteBufferExtensions
{
    extension<TWriteBuffer>(ref TWriteBuffer buffer)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
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
