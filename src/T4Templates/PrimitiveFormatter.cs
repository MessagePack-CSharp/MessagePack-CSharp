using System;

namespace MessagePack.Formatters
{
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

}