using System.Buffers.Binary;
using System.Numerics;
using static UltraMessagePack.MessagePackPrimitives;

namespace UltraMessagePack.Formatters;

// System.Numerics scalars, created by BuiltInFormatterFactory.

// fused read helpers: the caller has already validated the element code byte, so these
// load payload bytes only (BE bits -> host float via Unsafe.As, which keeps ns2.0 on the same code path — it lacks Int32BitsToSingle)
file static class NumericFusion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float ReadSingle(ReadOnlySpan<byte> span, int payloadOffset)
    {
        var bits = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), payloadOffset)));
        return Unsafe.As<uint, float>(ref bits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ReadDouble(ReadOnlySpan<byte> span, int payloadOffset)
    {
        var bits = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), payloadOffset)));
        return Unsafe.As<ulong, double>(ref bits);
    }
}

/// <summary>Serializes <see cref="BigInteger"/> as a bin of its little-endian two's-complement bytes.</summary>
public sealed partial class BigIntegerFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, BigInteger>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, BigInteger value)
    {
#if NETSTANDARD2_0
        // ns2.0 has no GetByteCount/TryWriteBytes: the array route is the only one
        buffer.WriteBinary(value.ToByteArray());
#else
        var byteCount = value.GetByteCount();
        if (byteCount <= byte.MaxValue)
        {
            // bin8: dynamic but single-store fused header, payload written in place
            var span = buffer.GetSpan(2 + byteCount);
            BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)((byteCount << 8) | MessagePackCode.Bin8));
            value.TryWriteBytes(span.Slice(2), out var written); // written == byteCount by construction
            buffer.Advance(2 + written);
        }
        else
        {
            // >255 bytes is rare: take the array route into bin16/bin32
            buffer.WriteBinary(value.ToByteArray());
        }
#endif
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref BigInteger value)
    {
        var byteCount = buffer.ReadBinHeader();
        if (!buffer.TryGetSpan(byteCount, out var payload))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading BigInteger.");
        }
#if NETSTANDARD2_0
        value = new BigInteger(payload.Slice(0, byteCount).ToArray());
#else
        value = new BigInteger(payload.Slice(0, byteCount));
#endif
        buffer.Advance(byteCount);
    }
}

/// <summary>Serializes <see cref="Complex"/> as [real, imaginary] float64s.</summary>
public sealed partial class ComplexFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Complex>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Complex value)
    {
        // fixarray(2) + 2x float64 = 19 bytes
        ref var r = ref buffer.GetReference(19);
        r = (byte)(MessagePackCode.MinFixArray | 2);
        UnsafeWriteDouble(ref Unsafe.Add(ref r, 1), value.Real);
        UnsafeWriteDouble(ref Unsafe.Add(ref r, 10), value.Imaginary);
        buffer.Advance(19);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Complex value)
    {
        var span = buffer.GetCurrentSpan();
        if (span.Length >= 19 &&
            span[0] == (MessagePackCode.MinFixArray | 2) &&
            span[1] == MessagePackCode.Float64 &&
            span[10] == MessagePackCode.Float64)
        {
            value = new Complex(
                NumericFusion.ReadDouble(span, 2),
                NumericFusion.ReadDouble(span, 11));
            buffer.Advance(19);
            return;
        }
        DeserializeSlow(ref buffer, ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DeserializeSlow(ref TReadBuffer buffer, ref Complex value)
    {
        if (buffer.ReadArrayHeader() != 2)
        {
            throw new MessagePackSerializationException("Invalid Complex format.");
        }
        var real = buffer.ReadDouble();
        var imaginary = buffer.ReadDouble();
        value = new Complex(real, imaginary);
    }
}

/// <summary>Serializes <see cref="Vector2"/> as [x, y] float32s.</summary>
public sealed partial class Vector2Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Vector2>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Vector2 value)
    {
        // fixarray(2) + 2x float32 = 11 bytes
        ref var r = ref buffer.GetReference(11);
        r = (byte)(MessagePackCode.MinFixArray | 2);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 1), value.X);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 6), value.Y);
        buffer.Advance(11);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Vector2 value)
    {
        var span = buffer.GetCurrentSpan();
        if (span.Length >= 11 &&
            span[0] == (MessagePackCode.MinFixArray | 2) &&
            span[1] == MessagePackCode.Float32 &&
            span[6] == MessagePackCode.Float32)
        {
            value = new Vector2(
                NumericFusion.ReadSingle(span, 2),
                NumericFusion.ReadSingle(span, 7));
            buffer.Advance(11);
            return;
        }
        DeserializeSlow(ref buffer, ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DeserializeSlow(ref TReadBuffer buffer, ref Vector2 value)
    {
        if (buffer.ReadArrayHeader() != 2)
        {
            throw new MessagePackSerializationException("Invalid Vector2 data.");
        }
        value = new Vector2(buffer.ReadSingle(), buffer.ReadSingle());
    }
}

/// <summary>Serializes <see cref="Vector3"/> as [x, y, z] float32s.</summary>
public sealed partial class Vector3Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Vector3>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Vector3 value)
    {
        // fixarray(3) + 3x float32 = 16 bytes
        ref var r = ref buffer.GetReference(16);
        r = (byte)(MessagePackCode.MinFixArray | 3);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 1), value.X);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 6), value.Y);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 11), value.Z);
        buffer.Advance(16);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Vector3 value)
    {
        var span = buffer.GetCurrentSpan();
        if (span.Length >= 16 &&
            span[0] == (MessagePackCode.MinFixArray | 3) &&
            span[1] == MessagePackCode.Float32 &&
            span[6] == MessagePackCode.Float32 &&
            span[11] == MessagePackCode.Float32)
        {
            value = new Vector3(
                NumericFusion.ReadSingle(span, 2),
                NumericFusion.ReadSingle(span, 7),
                NumericFusion.ReadSingle(span, 12));
            buffer.Advance(16);
            return;
        }
        DeserializeSlow(ref buffer, ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DeserializeSlow(ref TReadBuffer buffer, ref Vector3 value)
    {
        if (buffer.ReadArrayHeader() != 3)
        {
            throw new MessagePackSerializationException("Invalid Vector3 data.");
        }
        value = new Vector3(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle());
    }
}

/// <summary>Serializes <see cref="Vector4"/> as [x, y, z, w] float32s.</summary>
public sealed partial class Vector4Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Vector4>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Vector4 value)
    {
        // fixarray(4) + 4x float32 = 21 bytes
        ref var r = ref buffer.GetReference(21);
        r = (byte)(MessagePackCode.MinFixArray | 4);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 1), value.X);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 6), value.Y);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 11), value.Z);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 16), value.W);
        buffer.Advance(21);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Vector4 value)
    {
        var span = buffer.GetCurrentSpan();
        if (span.Length >= 21 &&
            span[0] == (MessagePackCode.MinFixArray | 4) &&
            span[1] == MessagePackCode.Float32 &&
            span[6] == MessagePackCode.Float32 &&
            span[11] == MessagePackCode.Float32 &&
            span[16] == MessagePackCode.Float32)
        {
            value = new Vector4(
                NumericFusion.ReadSingle(span, 2),
                NumericFusion.ReadSingle(span, 7),
                NumericFusion.ReadSingle(span, 12),
                NumericFusion.ReadSingle(span, 17));
            buffer.Advance(21);
            return;
        }
        DeserializeSlow(ref buffer, ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DeserializeSlow(ref TReadBuffer buffer, ref Vector4 value)
    {
        if (buffer.ReadArrayHeader() != 4)
        {
            throw new MessagePackSerializationException("Invalid Vector4 data.");
        }
        value = new Vector4(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle());
    }
}

/// <summary>Serializes <see cref="Quaternion"/> as [x, y, z, w] float32s.</summary>
public sealed partial class QuaternionFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Quaternion>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Quaternion value)
    {
        ref var r = ref buffer.GetReference(21);
        r = (byte)(MessagePackCode.MinFixArray | 4);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 1), value.X);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 6), value.Y);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 11), value.Z);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 16), value.W);
        buffer.Advance(21);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Quaternion value)
    {
        var span = buffer.GetCurrentSpan();
        if (span.Length >= 21 &&
            span[0] == (MessagePackCode.MinFixArray | 4) &&
            span[1] == MessagePackCode.Float32 &&
            span[6] == MessagePackCode.Float32 &&
            span[11] == MessagePackCode.Float32 &&
            span[16] == MessagePackCode.Float32)
        {
            value = new Quaternion(
                NumericFusion.ReadSingle(span, 2),
                NumericFusion.ReadSingle(span, 7),
                NumericFusion.ReadSingle(span, 12),
                NumericFusion.ReadSingle(span, 17));
            buffer.Advance(21);
            return;
        }
        DeserializeSlow(ref buffer, ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DeserializeSlow(ref TReadBuffer buffer, ref Quaternion value)
    {
        if (buffer.ReadArrayHeader() != 4)
        {
            throw new MessagePackSerializationException("Invalid Quaternion data.");
        }
        value = new Quaternion(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle());
    }
}

/// <summary>Serializes <see cref="Plane"/> as [normalX, normalY, normalZ, d] float32s.</summary>
public sealed partial class PlaneFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Plane>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Plane value)
    {
        ref var r = ref buffer.GetReference(21);
        r = (byte)(MessagePackCode.MinFixArray | 4);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 1), value.Normal.X);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 6), value.Normal.Y);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 11), value.Normal.Z);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 16), value.D);
        buffer.Advance(21);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Plane value)
    {
        var span = buffer.GetCurrentSpan();
        if (span.Length >= 21 &&
            span[0] == (MessagePackCode.MinFixArray | 4) &&
            span[1] == MessagePackCode.Float32 &&
            span[6] == MessagePackCode.Float32 &&
            span[11] == MessagePackCode.Float32 &&
            span[16] == MessagePackCode.Float32)
        {
            value = new Plane(
                NumericFusion.ReadSingle(span, 2),
                NumericFusion.ReadSingle(span, 7),
                NumericFusion.ReadSingle(span, 12),
                NumericFusion.ReadSingle(span, 17));
            buffer.Advance(21);
            return;
        }
        DeserializeSlow(ref buffer, ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DeserializeSlow(ref TReadBuffer buffer, ref Plane value)
    {
        if (buffer.ReadArrayHeader() != 4)
        {
            throw new MessagePackSerializationException("Invalid Plane data.");
        }
        value = new Plane(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle());
    }
}

/// <summary>Serializes <see cref="Matrix3x2"/> as its six elements row-major.</summary>
public sealed partial class Matrix3x2Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Matrix3x2>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Matrix3x2 value)
    {
        // fixarray(6) + 6x float32 = 31 bytes
        ref var r = ref buffer.GetReference(31);
        r = (byte)(MessagePackCode.MinFixArray | 6);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 1), value.M11);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 6), value.M12);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 11), value.M21);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 16), value.M22);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 21), value.M31);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 26), value.M32);
        buffer.Advance(31);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Matrix3x2 value)
    {
        var span = buffer.GetCurrentSpan();
        if (span.Length >= 31 &&
            span[0] == (MessagePackCode.MinFixArray | 6) &&
            span[1] == MessagePackCode.Float32 &&
            span[6] == MessagePackCode.Float32 &&
            span[11] == MessagePackCode.Float32 &&
            span[16] == MessagePackCode.Float32 &&
            span[21] == MessagePackCode.Float32 &&
            span[26] == MessagePackCode.Float32)
        {
            value = new Matrix3x2(
                NumericFusion.ReadSingle(span, 2), NumericFusion.ReadSingle(span, 7),
                NumericFusion.ReadSingle(span, 12), NumericFusion.ReadSingle(span, 17),
                NumericFusion.ReadSingle(span, 22), NumericFusion.ReadSingle(span, 27));
            buffer.Advance(31);
            return;
        }
        DeserializeSlow(ref buffer, ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DeserializeSlow(ref TReadBuffer buffer, ref Matrix3x2 value)
    {
        if (buffer.ReadArrayHeader() != 6)
        {
            throw new MessagePackSerializationException("Invalid Matrix3x2 data.");
        }
        value = new Matrix3x2(
            buffer.ReadSingle(), buffer.ReadSingle(),
            buffer.ReadSingle(), buffer.ReadSingle(),
            buffer.ReadSingle(), buffer.ReadSingle());
    }
}

/// <summary>Serializes <see cref="Matrix4x4"/> as its sixteen elements row-major.</summary>
public sealed partial class Matrix4x4Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Matrix4x4>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Matrix4x4 value)
    {
        // array16 (16 exceeds fixarray's 15-element ceiling) + 16x float32 = 83 bytes
        ref var r = ref buffer.GetReference(83);
        r = MessagePackCode.Array16;
        Unsafe.Add(ref r, 1) = 0;
        Unsafe.Add(ref r, 2) = 16;
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 3), value.M11);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 8), value.M12);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 13), value.M13);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 18), value.M14);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 23), value.M21);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 28), value.M22);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 33), value.M23);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 38), value.M24);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 43), value.M31);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 48), value.M32);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 53), value.M33);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 58), value.M34);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 63), value.M41);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 68), value.M42);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 73), value.M43);
        UnsafeWriteSingle(ref Unsafe.Add(ref r, 78), value.M44);
        buffer.Advance(83);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Matrix4x4 value)
    {
        var span = buffer.GetCurrentSpan();
        if (span.Length >= 83 &&
            span[0] == MessagePackCode.Array16 && span[1] == 0 && span[2] == 16 &&
            span[3] == MessagePackCode.Float32 && span[8] == MessagePackCode.Float32 &&
            span[13] == MessagePackCode.Float32 && span[18] == MessagePackCode.Float32 &&
            span[23] == MessagePackCode.Float32 && span[28] == MessagePackCode.Float32 &&
            span[33] == MessagePackCode.Float32 && span[38] == MessagePackCode.Float32 &&
            span[43] == MessagePackCode.Float32 && span[48] == MessagePackCode.Float32 &&
            span[53] == MessagePackCode.Float32 && span[58] == MessagePackCode.Float32 &&
            span[63] == MessagePackCode.Float32 && span[68] == MessagePackCode.Float32 &&
            span[73] == MessagePackCode.Float32 && span[78] == MessagePackCode.Float32)
        {
            value = new Matrix4x4(
                NumericFusion.ReadSingle(span, 4), NumericFusion.ReadSingle(span, 9), NumericFusion.ReadSingle(span, 14), NumericFusion.ReadSingle(span, 19),
                NumericFusion.ReadSingle(span, 24), NumericFusion.ReadSingle(span, 29), NumericFusion.ReadSingle(span, 34), NumericFusion.ReadSingle(span, 39),
                NumericFusion.ReadSingle(span, 44), NumericFusion.ReadSingle(span, 49), NumericFusion.ReadSingle(span, 54), NumericFusion.ReadSingle(span, 59),
                NumericFusion.ReadSingle(span, 64), NumericFusion.ReadSingle(span, 69), NumericFusion.ReadSingle(span, 74), NumericFusion.ReadSingle(span, 79));
            buffer.Advance(83);
            return;
        }
        DeserializeSlow(ref buffer, ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DeserializeSlow(ref TReadBuffer buffer, ref Matrix4x4 value)
    {
        if (buffer.ReadArrayHeader() != 16)
        {
            throw new MessagePackSerializationException("Invalid Matrix4x4 data.");
        }
        value = new Matrix4x4(
            buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(),
            buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(),
            buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(),
            buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle());
    }
}
