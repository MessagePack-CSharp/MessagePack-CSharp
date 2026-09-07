using System.Buffers.Binary;
using System.Numerics;
using static MessagePack.MessagePackPrimitives;

namespace MessagePack.Formatters;

// System.Numerics scalars, created by BuiltInFormatterFactory.

// fused read helpers: the caller has already validated the element code byte, so these
// load payload bytes only (BE bits -> host float via Unsafe.As, which keeps ns2.0 on the same code path — it lacks Int32BitsToSingle)
file static class NumericFusion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float ReadSingle(ReadOnlySpan<byte> span, int payloadOffset)
    {
        var bits = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), payloadOffset)));
        return Unsafe.As<uint, float>(ref bits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ReadDouble(ReadOnlySpan<byte> span, int payloadOffset)
    {
        var bits = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), payloadOffset)));
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

#if NET9_0_OR_GREATER
/// <summary>Serializes <see cref="System.Runtime.InteropServices.NFloat"/> as a float64 — a lossless widening on every platform (a 32-bit reader narrows on assignment, as the platform itself would).</summary>
public sealed partial class NFloatFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, System.Runtime.InteropServices.NFloat>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, System.Runtime.InteropServices.NFloat value)
    {
        buffer.WriteDouble((double)value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref System.Runtime.InteropServices.NFloat value)
    {
        value = (System.Runtime.InteropServices.NFloat)buffer.ReadDouble();
    }
}
#endif

#if NET11_0_OR_GREATER
/// <summary>Serializes <see cref="BFloat16"/> as a float32 (BFloat16 is a truncated float32, so every value is exactly representable — the Half treatment).</summary>
public sealed partial class BFloat16Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, BFloat16>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, BFloat16 value)
    {
        buffer.WriteSingle((float)value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref BFloat16 value)
    {
        value = (BFloat16)buffer.ReadSingle();
    }
}

// The IEEE 754 decimal trio rides bin(4/8/16) of its interchange bits, little-endian —
// the Int128 treatment. The string form is out: the default positional rendering can
// reach kilobytes (Decimal128 max is a 6146-character string), and the compact
// scientific forms do not preserve the cohort (1.5 vs 1.50 are distinct values here).
// The bit image preserves cohort, NaN payloads and infinities exactly.

/// <summary>Serializes <see cref="Decimal32"/> as a 4-byte little-endian bin of its IEEE 754 interchange bits.</summary>
public sealed partial class Decimal32Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Decimal32>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Decimal32 value)
    {
        var span = buffer.GetSpan(2 + 4);
        BinaryPrimitives.WriteUInt16LittleEndian(span, (4 << 8) | MessagePackCode.Bin8);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(2), Unsafe.BitCast<Decimal32, uint>(value));
        buffer.Advance(2 + 4);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Decimal32 value)
    {
        var byteCount = buffer.ReadBinHeader();
        if (byteCount != 4)
        {
            throw new MessagePackSerializationException("Invalid Decimal32 data.");
        }
        if (!buffer.TryGetSpan(4, out var payload))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading Decimal32.");
        }
        value = Unsafe.BitCast<uint, Decimal32>(BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)));
        buffer.Advance(4);
    }
}

/// <summary>Serializes <see cref="Decimal64"/> as an 8-byte little-endian bin of its IEEE 754 interchange bits.</summary>
public sealed partial class Decimal64Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Decimal64>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Decimal64 value)
    {
        var span = buffer.GetSpan(2 + 8);
        BinaryPrimitives.WriteUInt16LittleEndian(span, (8 << 8) | MessagePackCode.Bin8);
        BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(2), Unsafe.BitCast<Decimal64, ulong>(value));
        buffer.Advance(2 + 8);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Decimal64 value)
    {
        var byteCount = buffer.ReadBinHeader();
        if (byteCount != 8)
        {
            throw new MessagePackSerializationException("Invalid Decimal64 data.");
        }
        if (!buffer.TryGetSpan(8, out var payload))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading Decimal64.");
        }
        value = Unsafe.BitCast<ulong, Decimal64>(BinaryPrimitives.ReadUInt64LittleEndian(payload.Slice(0, 8)));
        buffer.Advance(8);
    }
}

/// <summary>Serializes <see cref="Decimal128"/> as a 16-byte little-endian bin of its IEEE 754 interchange bits.</summary>
public sealed partial class Decimal128Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Decimal128>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Decimal128 value)
    {
        var span = buffer.GetSpan(2 + 16);
        BinaryPrimitives.WriteUInt16LittleEndian(span, (16 << 8) | MessagePackCode.Bin8);
        BinaryPrimitives.WriteUInt128LittleEndian(span.Slice(2), Unsafe.BitCast<Decimal128, UInt128>(value));
        buffer.Advance(2 + 16);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Decimal128 value)
    {
        var byteCount = buffer.ReadBinHeader();
        if (byteCount != 16)
        {
            throw new MessagePackSerializationException("Invalid Decimal128 data.");
        }
        if (!buffer.TryGetSpan(16, out var payload))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading Decimal128.");
        }
        value = Unsafe.BitCast<UInt128, Decimal128>(BinaryPrimitives.ReadUInt128LittleEndian(payload.Slice(0, 16)));
        buffer.Advance(16);
    }
}

/// <summary>Serializes <see cref="Complex{T}"/> as [real, imaginary] — the same wire the non-generic <see cref="Complex"/> uses, so <c>Complex&lt;double&gt;</c> is byte-identical to it.</summary>
public sealed class ComplexFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Complex<T>>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> componentFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        componentFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Complex<T> value)
    {
        state.Enter(); // descends into the resolver's T formatter, so this is a nesting level
        buffer.WriteFixArrayHeader(2);
        componentFormatter.Serialize(ref buffer, ref state, value.Real);
        componentFormatter.Serialize(ref buffer, ref state, value.Imaginary);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Complex<T> value)
    {
        if (buffer.ReadArrayHeader() != 2)
        {
            throw new MessagePackSerializationException("Invalid Complex format.");
        }
        state.Enter();
        T real = default!;
        T imaginary = default!;
        componentFormatter.Deserialize(ref buffer, ref state, ref real);
        componentFormatter.Deserialize(ref buffer, ref state, ref imaginary);
        state.Exit();
        value = new Complex<T>(real, imaginary);
    }
}

public sealed partial class ComplexFormatterFactory<T> : MessagePackFormatterFactory
    where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return type == typeof(Complex<T>) ? new ComplexFormatter<TWriteBuffer, TReadBuffer, T>() : null;
    }
}
#endif
