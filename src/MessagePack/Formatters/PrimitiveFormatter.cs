using System;

namespace MessagePack.Formatters
{
    public class Int16Formatter : IMessagePackFormatter<Int16>
    {
		public static readonly Int16Formatter Instance = new Int16Formatter();

		Int16Formatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Int16 value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteInt16(ref bytes, offset, value);
        }

        public Int16 Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadInt16(bytes, offset, out readSize);
        }
    }

    public class NullableInt16Formatter : IMessagePackFormatter<Int16?>
    {
		public static readonly NullableInt16Formatter Instance = new NullableInt16Formatter();

		NullableInt16Formatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Int16? value, IFormatterResolver formatterResolver)
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

        public Int16? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
		public static readonly Int32Formatter Instance = new Int32Formatter();

		Int32Formatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Int32 value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteInt32(ref bytes, offset, value);
        }

        public Int32 Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadInt32(bytes, offset, out readSize);
        }
    }

    public class NullableInt32Formatter : IMessagePackFormatter<Int32?>
    {
		public static readonly NullableInt32Formatter Instance = new NullableInt32Formatter();

		NullableInt32Formatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Int32? value, IFormatterResolver formatterResolver)
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

        public Int32? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
		public static readonly Int64Formatter Instance = new Int64Formatter();

		Int64Formatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Int64 value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteInt64(ref bytes, offset, value);
        }

        public Int64 Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadInt64(bytes, offset, out readSize);
        }
    }

    public class NullableInt64Formatter : IMessagePackFormatter<Int64?>
    {
		public static readonly NullableInt64Formatter Instance = new NullableInt64Formatter();

		NullableInt64Formatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Int64? value, IFormatterResolver formatterResolver)
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

        public Int64? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
		public static readonly UInt16Formatter Instance = new UInt16Formatter();

		UInt16Formatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, UInt16 value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteUInt16(ref bytes, offset, value);
        }

        public UInt16 Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadUInt16(bytes, offset, out readSize);
        }
    }

    public class NullableUInt16Formatter : IMessagePackFormatter<UInt16?>
    {
		public static readonly NullableUInt16Formatter Instance = new NullableUInt16Formatter();

		NullableUInt16Formatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, UInt16? value, IFormatterResolver formatterResolver)
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

        public UInt16? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
		public static readonly UInt32Formatter Instance = new UInt32Formatter();

		UInt32Formatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, UInt32 value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteUInt32(ref bytes, offset, value);
        }

        public UInt32 Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadUInt32(bytes, offset, out readSize);
        }
    }

    public class NullableUInt32Formatter : IMessagePackFormatter<UInt32?>
    {
		public static readonly NullableUInt32Formatter Instance = new NullableUInt32Formatter();

		NullableUInt32Formatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, UInt32? value, IFormatterResolver formatterResolver)
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

        public UInt32? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
		public static readonly UInt64Formatter Instance = new UInt64Formatter();

		UInt64Formatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, UInt64 value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteUInt64(ref bytes, offset, value);
        }

        public UInt64 Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadUInt64(bytes, offset, out readSize);
        }
    }

    public class NullableUInt64Formatter : IMessagePackFormatter<UInt64?>
    {
		public static readonly NullableUInt64Formatter Instance = new NullableUInt64Formatter();

		NullableUInt64Formatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, UInt64? value, IFormatterResolver formatterResolver)
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

        public UInt64? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
		public static readonly SingleFormatter Instance = new SingleFormatter();

		SingleFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Single value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteSingle(ref bytes, offset, value);
        }

        public Single Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadSingle(bytes, offset, out readSize);
        }
    }

    public class NullableSingleFormatter : IMessagePackFormatter<Single?>
    {
		public static readonly NullableSingleFormatter Instance = new NullableSingleFormatter();

		NullableSingleFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Single? value, IFormatterResolver formatterResolver)
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

        public Single? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
		public static readonly DoubleFormatter Instance = new DoubleFormatter();

		DoubleFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Double value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteDouble(ref bytes, offset, value);
        }

        public Double Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadDouble(bytes, offset, out readSize);
        }
    }

    public class NullableDoubleFormatter : IMessagePackFormatter<Double?>
    {
		public static readonly NullableDoubleFormatter Instance = new NullableDoubleFormatter();

		NullableDoubleFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Double? value, IFormatterResolver formatterResolver)
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

        public Double? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
		public static readonly BooleanFormatter Instance = new BooleanFormatter();

		BooleanFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Boolean value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteBoolean(ref bytes, offset, value);
        }

        public Boolean Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadBoolean(bytes, offset, out readSize);
        }
    }

    public class NullableBooleanFormatter : IMessagePackFormatter<Boolean?>
    {
		public static readonly NullableBooleanFormatter Instance = new NullableBooleanFormatter();

		NullableBooleanFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Boolean? value, IFormatterResolver formatterResolver)
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

        public Boolean? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
		public static readonly ByteFormatter Instance = new ByteFormatter();

		ByteFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Byte value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteByte(ref bytes, offset, value);
        }

        public Byte Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadByte(bytes, offset, out readSize);
        }
    }

    public class NullableByteFormatter : IMessagePackFormatter<Byte?>
    {
		public static readonly NullableByteFormatter Instance = new NullableByteFormatter();

		NullableByteFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Byte? value, IFormatterResolver formatterResolver)
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

        public Byte? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
		public static readonly SByteFormatter Instance = new SByteFormatter();

		SByteFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, SByte value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteSByte(ref bytes, offset, value);
        }

        public SByte Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadSByte(bytes, offset, out readSize);
        }
    }

    public class NullableSByteFormatter : IMessagePackFormatter<SByte?>
    {
		public static readonly NullableSByteFormatter Instance = new NullableSByteFormatter();

		NullableSByteFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, SByte? value, IFormatterResolver formatterResolver)
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

        public SByte? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
		public static readonly CharFormatter Instance = new CharFormatter();

		CharFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Char value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteChar(ref bytes, offset, value);
        }

        public Char Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadChar(bytes, offset, out readSize);
        }
    }

    public class NullableCharFormatter : IMessagePackFormatter<Char?>
    {
		public static readonly NullableCharFormatter Instance = new NullableCharFormatter();

		NullableCharFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, Char? value, IFormatterResolver formatterResolver)
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

        public Char? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
		public static readonly DateTimeFormatter Instance = new DateTimeFormatter();

		DateTimeFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, DateTime value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteDateTime(ref bytes, offset, value);
        }

        public DateTime Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadDateTime(bytes, offset, out readSize);
        }
    }

    public class NullableDateTimeFormatter : IMessagePackFormatter<DateTime?>
    {
		public static readonly NullableDateTimeFormatter Instance = new NullableDateTimeFormatter();

		NullableDateTimeFormatter()
		{
		}

        public int Serialize(ref byte[] bytes, int offset, DateTime? value, IFormatterResolver formatterResolver)
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

        public DateTime? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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