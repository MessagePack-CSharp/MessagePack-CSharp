using SerializerFoundation;

namespace UltraMessagePack.Formatters;

// PrimitiveFormatters will be created by BuiltInFormatterFactory

public sealed partial class Int32Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value)
    {
        buffer.WriteInt32(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value)
    {
        value = buffer.ReadInt32();
    }
}

public sealed partial class Int64Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, long>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, long value)
    {
        buffer.WriteInt64(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref long value)
    {
        value = buffer.ReadInt64();
    }
}

public sealed partial class UInt32Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, uint>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, uint value)
    {
        buffer.WriteUInt32(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref uint value)
    {
        value = buffer.ReadUInt32();
    }
}

public sealed partial class UInt64Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ulong>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ulong value)
    {
        buffer.WriteUInt64(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ulong value)
    {
        value = buffer.ReadUInt64();
    }
}

public sealed partial class Int16Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, short>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, short value)
    {
        buffer.WriteInt16(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref short value)
    {
        value = buffer.ReadInt16();
    }
}

public sealed partial class UInt16Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ushort>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ushort value)
    {
        buffer.WriteUInt16(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ushort value)
    {
        value = buffer.ReadUInt16();
    }
}

public sealed partial class ByteFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, byte>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, byte value)
    {
        buffer.WriteByte(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref byte value)
    {
        value = buffer.ReadByte();
    }
}

public sealed partial class SByteFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, sbyte>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, sbyte value)
    {
        buffer.WriteSByte(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref sbyte value)
    {
        value = buffer.ReadSByte();
    }
}

public sealed partial class CharFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, char>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, char value)
    {
        buffer.WriteChar(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref char value)
    {
        value = buffer.ReadChar();
    }
}

public sealed partial class BooleanFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, bool>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, bool value)
    {
        buffer.WriteBoolean(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref bool value)
    {
        value = buffer.ReadBoolean();
    }
}

public sealed partial class SingleFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, float>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, float value)
    {
        buffer.WriteSingle(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref float value)
    {
        value = buffer.ReadSingle();
    }
}

public sealed partial class DoubleFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, double>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, double value)
    {
        buffer.WriteDouble(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref double value)
    {
        value = buffer.ReadDouble();
    }
}

public sealed partial class StringFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, string? value)
    {
        buffer.WriteString(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref string? value)
    {
        value = buffer.ReadString();
    }
}

public sealed partial class DateTimeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, DateTime>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, DateTime value)
    {
        buffer.WriteTimestamp(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref DateTime value)
    {
        value = buffer.ReadTimestamp();
    }
}

