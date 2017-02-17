using System;

namespace MessagePack.Formatters
{
    public class Int16Formatter : IMessagePackFormatter<Int16>
    {
        public int Serialize(ref byte[] bytes, int offset, Int16 value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteInt16(ref bytes, offset, value);
        }

        public Int16 Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadInt16(bytes, offset, out readSize);
        }
    }

    public class NullableInt16Formatter : IMessagePackFormatter<Int16?>
    {
        public int Serialize(ref byte[] bytes, int offset, Int16? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteInt16(ref bytes, offset, value.Value);
            }
        }

        public Int16? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadInt16(bytes, offset, out readSize);
            }
        }
    }

    public class Int32Formatter : IMessagePackFormatter<Int32>
    {
        public int Serialize(ref byte[] bytes, int offset, Int32 value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteInt32(ref bytes, offset, value);
        }

        public Int32 Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadInt32(bytes, offset, out readSize);
        }
    }

    public class NullableInt32Formatter : IMessagePackFormatter<Int32?>
    {
        public int Serialize(ref byte[] bytes, int offset, Int32? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteInt32(ref bytes, offset, value.Value);
            }
        }

        public Int32? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadInt32(bytes, offset, out readSize);
            }
        }
    }

    public class Int64Formatter : IMessagePackFormatter<Int64>
    {
        public int Serialize(ref byte[] bytes, int offset, Int64 value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteInt64(ref bytes, offset, value);
        }

        public Int64 Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadInt64(bytes, offset, out readSize);
        }
    }

    public class NullableInt64Formatter : IMessagePackFormatter<Int64?>
    {
        public int Serialize(ref byte[] bytes, int offset, Int64? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteInt64(ref bytes, offset, value.Value);
            }
        }

        public Int64? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadInt64(bytes, offset, out readSize);
            }
        }
    }

    public class UInt16Formatter : IMessagePackFormatter<UInt16>
    {
        public int Serialize(ref byte[] bytes, int offset, UInt16 value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteUInt16(ref bytes, offset, value);
        }

        public UInt16 Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadUInt16(bytes, offset, out readSize);
        }
    }

    public class NullableUInt16Formatter : IMessagePackFormatter<UInt16?>
    {
        public int Serialize(ref byte[] bytes, int offset, UInt16? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteUInt16(ref bytes, offset, value.Value);
            }
        }

        public UInt16? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadUInt16(bytes, offset, out readSize);
            }
        }
    }

    public class UInt32Formatter : IMessagePackFormatter<UInt32>
    {
        public int Serialize(ref byte[] bytes, int offset, UInt32 value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteUInt32(ref bytes, offset, value);
        }

        public UInt32 Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadUInt32(bytes, offset, out readSize);
        }
    }

    public class NullableUInt32Formatter : IMessagePackFormatter<UInt32?>
    {
        public int Serialize(ref byte[] bytes, int offset, UInt32? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteUInt32(ref bytes, offset, value.Value);
            }
        }

        public UInt32? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadUInt32(bytes, offset, out readSize);
            }
        }
    }

    public class UInt64Formatter : IMessagePackFormatter<UInt64>
    {
        public int Serialize(ref byte[] bytes, int offset, UInt64 value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteUInt64(ref bytes, offset, value);
        }

        public UInt64 Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadUInt64(bytes, offset, out readSize);
        }
    }

    public class NullableUInt64Formatter : IMessagePackFormatter<UInt64?>
    {
        public int Serialize(ref byte[] bytes, int offset, UInt64? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteUInt64(ref bytes, offset, value.Value);
            }
        }

        public UInt64? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadUInt64(bytes, offset, out readSize);
            }
        }
    }

    public class SingleFormatter : IMessagePackFormatter<Single>
    {
        public int Serialize(ref byte[] bytes, int offset, Single value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteSingle(ref bytes, offset, value);
        }

        public Single Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadSingle(bytes, offset, out readSize);
        }
    }

    public class NullableSingleFormatter : IMessagePackFormatter<Single?>
    {
        public int Serialize(ref byte[] bytes, int offset, Single? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteSingle(ref bytes, offset, value.Value);
            }
        }

        public Single? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadSingle(bytes, offset, out readSize);
            }
        }
    }

    public class DoubleFormatter : IMessagePackFormatter<Double>
    {
        public int Serialize(ref byte[] bytes, int offset, Double value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteDouble(ref bytes, offset, value);
        }

        public Double Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadDouble(bytes, offset, out readSize);
        }
    }

    public class NullableDoubleFormatter : IMessagePackFormatter<Double?>
    {
        public int Serialize(ref byte[] bytes, int offset, Double? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteDouble(ref bytes, offset, value.Value);
            }
        }

        public Double? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadDouble(bytes, offset, out readSize);
            }
        }
    }

    public class BooleanFormatter : IMessagePackFormatter<Boolean>
    {
        public int Serialize(ref byte[] bytes, int offset, Boolean value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteBoolean(ref bytes, offset, value);
        }

        public Boolean Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadBoolean(bytes, offset, out readSize);
        }
    }

    public class NullableBooleanFormatter : IMessagePackFormatter<Boolean?>
    {
        public int Serialize(ref byte[] bytes, int offset, Boolean? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteBoolean(ref bytes, offset, value.Value);
            }
        }

        public Boolean? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadBoolean(bytes, offset, out readSize);
            }
        }
    }

    public class ByteFormatter : IMessagePackFormatter<Byte>
    {
        public int Serialize(ref byte[] bytes, int offset, Byte value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteByte(ref bytes, offset, value);
        }

        public Byte Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadByte(bytes, offset, out readSize);
        }
    }

    public class NullableByteFormatter : IMessagePackFormatter<Byte?>
    {
        public int Serialize(ref byte[] bytes, int offset, Byte? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteByte(ref bytes, offset, value.Value);
            }
        }

        public Byte? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadByte(bytes, offset, out readSize);
            }
        }
    }

    public class SByteFormatter : IMessagePackFormatter<SByte>
    {
        public int Serialize(ref byte[] bytes, int offset, SByte value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteSByte(ref bytes, offset, value);
        }

        public SByte Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadSByte(bytes, offset, out readSize);
        }
    }

    public class NullableSByteFormatter : IMessagePackFormatter<SByte?>
    {
        public int Serialize(ref byte[] bytes, int offset, SByte? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteSByte(ref bytes, offset, value.Value);
            }
        }

        public SByte? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadSByte(bytes, offset, out readSize);
            }
        }
    }

    public class CharFormatter : IMessagePackFormatter<Char>
    {
        public int Serialize(ref byte[] bytes, int offset, Char value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteChar(ref bytes, offset, value);
        }

        public Char Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadChar(bytes, offset, out readSize);
        }
    }

    public class NullableCharFormatter : IMessagePackFormatter<Char?>
    {
        public int Serialize(ref byte[] bytes, int offset, Char? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteChar(ref bytes, offset, value.Value);
            }
        }

        public Char? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadChar(bytes, offset, out readSize);
            }
        }
    }

    public class DateTimeFormatter : IMessagePackFormatter<DateTime>
    {
        public int Serialize(ref byte[] bytes, int offset, DateTime value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteDateTime(ref bytes, offset, value);
        }

        public DateTime Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadDateTime(bytes, offset, out readSize);
        }
    }

    public class NullableDateTimeFormatter : IMessagePackFormatter<DateTime?>
    {
        public int Serialize(ref byte[] bytes, int offset, DateTime? value, IFormatterResolver typeResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteDateTime(ref bytes, offset, value.Value);
            }
        }

        public DateTime? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                return MessagePackBinary.ReadDateTime(bytes, offset, out readSize);
            }
        }
    }

}