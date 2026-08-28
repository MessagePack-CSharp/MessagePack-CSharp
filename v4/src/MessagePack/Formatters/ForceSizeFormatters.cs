// MessagePack-CSharp compatible force-size formatters (ForceSizePrimitiveFormatter.cs in v3):
// Serialize always emits the named msgpack format regardless of value, Deserialize accepts
// any integer format (same write-forced / read-lenient split as v3). Not part of any factory
// chain — these exist to be named explicitly, so the type names match v3 exactly
// (Force<Type>Block, not the primitive layer's Forced<FormatName> vocabulary).

namespace MessagePack.Formatters;

#region scalar

/// <summary>Always writes int8 format (0xd0); reads accept any integer format.</summary>
public sealed partial class ForceSByteBlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, sbyte>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, sbyte value)
    {
        buffer.WriteForcedInt8(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref sbyte value)
    {
        value = buffer.ReadSByte();
    }
}

/// <summary>Always writes uint8 format (0xcc); reads accept any integer format.</summary>
public sealed partial class ForceByteBlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, byte>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, byte value)
    {
        buffer.WriteForcedUInt8(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref byte value)
    {
        value = buffer.ReadByte();
    }
}

/// <summary>Always writes int16 format (0xd1); reads accept any integer format.</summary>
public sealed partial class ForceInt16BlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, short>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, short value)
    {
        buffer.WriteForcedInt16(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref short value)
    {
        value = buffer.ReadInt16();
    }
}

/// <summary>Always writes uint16 format (0xcd); reads accept any integer format.</summary>
public sealed partial class ForceUInt16BlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ushort>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ushort value)
    {
        buffer.WriteForcedUInt16(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ushort value)
    {
        value = buffer.ReadUInt16();
    }
}

/// <summary>Always writes int32 format (0xd2); reads accept any integer format.</summary>
public sealed partial class ForceInt32BlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value)
    {
        buffer.WriteForcedInt32(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value)
    {
        value = buffer.ReadInt32();
    }
}

/// <summary>Always writes uint32 format (0xce); reads accept any integer format.</summary>
public sealed partial class ForceUInt32BlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, uint>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, uint value)
    {
        buffer.WriteForcedUInt32(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref uint value)
    {
        value = buffer.ReadUInt32();
    }
}

/// <summary>Always writes int64 format (0xd3); reads accept any integer format.</summary>
public sealed partial class ForceInt64BlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, long>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, long value)
    {
        buffer.WriteForcedInt64(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref long value)
    {
        value = buffer.ReadInt64();
    }
}

/// <summary>Always writes uint64 format (0xcf); reads accept any integer format.</summary>
public sealed partial class ForceUInt64BlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ulong>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ulong value)
    {
        buffer.WriteForcedUInt64(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ulong value)
    {
        value = buffer.ReadUInt64();
    }
}

#endregion

#region nullable

/// <summary>Nullable companion of <see cref="ForceSByteBlockFormatter{TWriteBuffer, TReadBuffer}"/>; null is nil.</summary>
public sealed partial class NullableForceSByteBlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, sbyte?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, sbyte? value)
    {
        if (value is sbyte inner)
        {
            buffer.WriteForcedInt8(inner);
        }
        else
        {
            buffer.WriteNil();
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref sbyte? value)
    {
        value = buffer.TryReadNil() ? null : buffer.ReadSByte();
    }
}

/// <summary>Nullable companion of <see cref="ForceByteBlockFormatter{TWriteBuffer, TReadBuffer}"/>; null is nil.</summary>
public sealed partial class NullableForceByteBlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, byte?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, byte? value)
    {
        if (value is byte inner)
        {
            buffer.WriteForcedUInt8(inner);
        }
        else
        {
            buffer.WriteNil();
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref byte? value)
    {
        value = buffer.TryReadNil() ? null : buffer.ReadByte();
    }
}

/// <summary>Nullable companion of <see cref="ForceInt16BlockFormatter{TWriteBuffer, TReadBuffer}"/>; null is nil.</summary>
public sealed partial class NullableForceInt16BlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, short?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, short? value)
    {
        if (value is short inner)
        {
            buffer.WriteForcedInt16(inner);
        }
        else
        {
            buffer.WriteNil();
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref short? value)
    {
        value = buffer.TryReadNil() ? null : buffer.ReadInt16();
    }
}

/// <summary>Nullable companion of <see cref="ForceUInt16BlockFormatter{TWriteBuffer, TReadBuffer}"/>; null is nil.</summary>
public sealed partial class NullableForceUInt16BlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ushort?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ushort? value)
    {
        if (value is ushort inner)
        {
            buffer.WriteForcedUInt16(inner);
        }
        else
        {
            buffer.WriteNil();
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ushort? value)
    {
        value = buffer.TryReadNil() ? null : buffer.ReadUInt16();
    }
}

/// <summary>Nullable companion of <see cref="ForceInt32BlockFormatter{TWriteBuffer, TReadBuffer}"/>; null is nil.</summary>
public sealed partial class NullableForceInt32BlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int? value)
    {
        if (value is int inner)
        {
            buffer.WriteForcedInt32(inner);
        }
        else
        {
            buffer.WriteNil();
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int? value)
    {
        value = buffer.TryReadNil() ? null : buffer.ReadInt32();
    }
}

/// <summary>Nullable companion of <see cref="ForceUInt32BlockFormatter{TWriteBuffer, TReadBuffer}"/>; null is nil.</summary>
public sealed partial class NullableForceUInt32BlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, uint?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, uint? value)
    {
        if (value is uint inner)
        {
            buffer.WriteForcedUInt32(inner);
        }
        else
        {
            buffer.WriteNil();
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref uint? value)
    {
        value = buffer.TryReadNil() ? null : buffer.ReadUInt32();
    }
}

/// <summary>Nullable companion of <see cref="ForceInt64BlockFormatter{TWriteBuffer, TReadBuffer}"/>; null is nil.</summary>
public sealed partial class NullableForceInt64BlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, long?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, long? value)
    {
        if (value is long inner)
        {
            buffer.WriteForcedInt64(inner);
        }
        else
        {
            buffer.WriteNil();
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref long? value)
    {
        value = buffer.TryReadNil() ? null : buffer.ReadInt64();
    }
}

/// <summary>Nullable companion of <see cref="ForceUInt64BlockFormatter{TWriteBuffer, TReadBuffer}"/>; null is nil.</summary>
public sealed partial class NullableForceUInt64BlockFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ulong?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ulong? value)
    {
        if (value is ulong inner)
        {
            buffer.WriteForcedUInt64(inner);
        }
        else
        {
            buffer.WriteNil();
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ulong? value)
    {
        value = buffer.TryReadNil() ? null : buffer.ReadUInt64();
    }
}

#endregion

#region array

/// <summary>Array companion of <see cref="ForceSByteBlockFormatter{TWriteBuffer, TReadBuffer}"/>: every element in int8 format.</summary>
public sealed partial class ForceSByteBlockArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, sbyte[]?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, sbyte[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteArrayHeader(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            buffer.WriteForcedInt8(value[i]);
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref sbyte[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadArrayHeader();
        if (count == 0)
        {
            value = [];
            return;
        }
        // Populate contract: reuse the incoming array only when the length matches exactly
        var result = (value != null && value.Length == count) ? value : new sbyte[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = buffer.ReadSByte();
        }
        value = result;
    }
}

// No ForceByteBlockArrayFormatter: byte[] is bin territory (ByteArrayFormatters), and v3
// has no such type either — its force-size T4 skips the byte[] arity for the same reason.

/// <summary>Array companion of <see cref="ForceInt16BlockFormatter{TWriteBuffer, TReadBuffer}"/>: every element in int16 format.</summary>
public sealed partial class ForceInt16BlockArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, short[]?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, short[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteArrayHeader(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            buffer.WriteForcedInt16(value[i]);
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref short[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadArrayHeader();
        if (count == 0)
        {
            value = [];
            return;
        }
        var result = (value != null && value.Length == count) ? value : new short[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = buffer.ReadInt16();
        }
        value = result;
    }
}

/// <summary>Array companion of <see cref="ForceUInt16BlockFormatter{TWriteBuffer, TReadBuffer}"/>: every element in uint16 format.</summary>
public sealed partial class ForceUInt16BlockArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ushort[]?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ushort[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteArrayHeader(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            buffer.WriteForcedUInt16(value[i]);
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ushort[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadArrayHeader();
        if (count == 0)
        {
            value = [];
            return;
        }
        var result = (value != null && value.Length == count) ? value : new ushort[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = buffer.ReadUInt16();
        }
        value = result;
    }
}

/// <summary>Array companion of <see cref="ForceInt32BlockFormatter{TWriteBuffer, TReadBuffer}"/>: every element in int32 format.</summary>
public sealed partial class ForceInt32BlockArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int[]?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteArrayHeader(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            buffer.WriteForcedInt32(value[i]);
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadArrayHeader();
        if (count == 0)
        {
            value = [];
            return;
        }
        var result = (value != null && value.Length == count) ? value : new int[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = buffer.ReadInt32();
        }
        value = result;
    }
}

/// <summary>Array companion of <see cref="ForceUInt32BlockFormatter{TWriteBuffer, TReadBuffer}"/>: every element in uint32 format.</summary>
public sealed partial class ForceUInt32BlockArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, uint[]?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, uint[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteArrayHeader(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            buffer.WriteForcedUInt32(value[i]);
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref uint[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadArrayHeader();
        if (count == 0)
        {
            value = [];
            return;
        }
        var result = (value != null && value.Length == count) ? value : new uint[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = buffer.ReadUInt32();
        }
        value = result;
    }
}

/// <summary>Array companion of <see cref="ForceInt64BlockFormatter{TWriteBuffer, TReadBuffer}"/>: every element in int64 format.</summary>
public sealed partial class ForceInt64BlockArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, long[]?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, long[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteArrayHeader(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            buffer.WriteForcedInt64(value[i]);
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref long[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadArrayHeader();
        if (count == 0)
        {
            value = [];
            return;
        }
        var result = (value != null && value.Length == count) ? value : new long[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = buffer.ReadInt64();
        }
        value = result;
    }
}

/// <summary>Array companion of <see cref="ForceUInt64BlockFormatter{TWriteBuffer, TReadBuffer}"/>: every element in uint64 format.</summary>
public sealed partial class ForceUInt64BlockArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ulong[]?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ulong[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteArrayHeader(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            buffer.WriteForcedUInt64(value[i]);
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ulong[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        var count = buffer.ReadArrayHeader();
        if (count == 0)
        {
            value = [];
            return;
        }
        var result = (value != null && value.Length == count) ? value : new ulong[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = buffer.ReadUInt64();
        }
        value = result;
    }
}

#endregion
