using System;

namespace MessagePack.Formatters
{
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