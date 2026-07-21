using SerializerFoundation;

namespace UltraMessagePack.Formatters;

// PrimitiveFormatters will be created by PrimitiveFormatterFactory

public sealed class Int32Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref int value)
    {
        buffer.WriteInt32(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value)
    {
        value = buffer.ReadInt32();
    }
}

public sealed class Int64Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, long>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref long value)
    {
        buffer.WriteInt64(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref long value)
    {
        value = buffer.ReadInt64();
    }
}

public sealed class UInt32Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, uint>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref uint value)
    {
        buffer.WriteUInt32(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref uint value)
    {
        value = buffer.ReadUInt32();
    }
}

public sealed class UInt64Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ulong>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref ulong value)
    {
        buffer.WriteUInt64(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ulong value)
    {
        value = buffer.ReadUInt64();
    }
}

public sealed class Int16Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, short>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref short value)
    {
        buffer.WriteInt16(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref short value)
    {
        value = buffer.ReadInt16();
    }
}

public sealed class UInt16Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ushort>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref ushort value)
    {
        buffer.WriteUInt16(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ushort value)
    {
        value = buffer.ReadUInt16();
    }
}

public sealed class ByteFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, byte>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref byte value)
    {
        buffer.WriteByte(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref byte value)
    {
        value = buffer.ReadByte();
    }
}

public sealed class SByteFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, sbyte>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref sbyte value)
    {
        buffer.WriteSByte(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref sbyte value)
    {
        value = buffer.ReadSByte();
    }
}

public sealed class CharFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, char>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref char value)
    {
        buffer.WriteChar(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref char value)
    {
        value = buffer.ReadChar();
    }
}

public sealed class BooleanFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, bool>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref bool value)
    {
        buffer.WriteBoolean(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref bool value)
    {
        value = buffer.ReadBoolean();
    }
}

public sealed class SingleFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, float>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref float value)
    {
        buffer.WriteSingle(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref float value)
    {
        value = buffer.ReadSingle();
    }
}

public sealed class DoubleFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, double>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref double value)
    {
        buffer.WriteDouble(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref double value)
    {
        value = buffer.ReadDouble();
    }
}

public sealed class StringFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref string? value)
    {
        buffer.WriteString(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref string? value)
    {
        value = buffer.ReadString();
    }
}

public sealed class DateTimeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, DateTime>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref DateTime value)
    {
        buffer.WriteTimestamp(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref DateTime value)
    {
        value = buffer.ReadTimestamp();
    }
}

public sealed class ByteArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, byte[]?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref byte[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteBinary(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref byte[]? value)
    {
        value = buffer.TryReadNil() ? null : buffer.ReadBinary();
    }
}