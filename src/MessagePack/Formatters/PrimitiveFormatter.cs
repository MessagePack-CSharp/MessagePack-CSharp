using System;
using System.Buffers;

namespace MessagePack.Formatters
{
    public sealed class Int16Formatter : IMessagePackFormatter<Int16>
    {
        public static readonly Int16Formatter Instance = new Int16Formatter();

        Int16Formatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, Int16 value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteInt16(ref bytes, offset, value);
        }

        public Int16 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadInt16();
        }
    }

    public sealed class NullableInt16Formatter : IMessagePackFormatter<Int16?>
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

        public Int16? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadInt16();
            }
        }
    }

    public sealed class Int16ArrayFormatter : IMessagePackFormatter<Int16[]>
    {
        public static readonly Int16ArrayFormatter Instance = new Int16ArrayFormatter();

        Int16ArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Int16[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteInt16(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public Int16[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new Int16[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadInt16();
                }
                return array;
            }
        }
    }

    public sealed class Int32Formatter : IMessagePackFormatter<Int32>
    {
        public static readonly Int32Formatter Instance = new Int32Formatter();

        Int32Formatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, Int32 value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteInt32(ref bytes, offset, value);
        }

        public Int32 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadInt32();
        }
    }

    public sealed class NullableInt32Formatter : IMessagePackFormatter<Int32?>
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

        public Int32? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadInt32();
            }
        }
    }

    public sealed class Int32ArrayFormatter : IMessagePackFormatter<Int32[]>
    {
        public static readonly Int32ArrayFormatter Instance = new Int32ArrayFormatter();

        Int32ArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Int32[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteInt32(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public Int32[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new Int32[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadInt32();
                }
                return array;
            }
        }
    }

    public sealed class Int64Formatter : IMessagePackFormatter<Int64>
    {
        public static readonly Int64Formatter Instance = new Int64Formatter();

        Int64Formatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, Int64 value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteInt64(ref bytes, offset, value);
        }

        public Int64 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadInt64();
        }
    }

    public sealed class NullableInt64Formatter : IMessagePackFormatter<Int64?>
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

        public Int64? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadInt64();
            }
        }
    }

    public sealed class Int64ArrayFormatter : IMessagePackFormatter<Int64[]>
    {
        public static readonly Int64ArrayFormatter Instance = new Int64ArrayFormatter();

        Int64ArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Int64[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteInt64(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public Int64[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new Int64[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadInt64();
                }
                return array;
            }
        }
    }

    public sealed class UInt16Formatter : IMessagePackFormatter<UInt16>
    {
        public static readonly UInt16Formatter Instance = new UInt16Formatter();

        UInt16Formatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, UInt16 value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteUInt16(ref bytes, offset, value);
        }

        public UInt16 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadUInt16();
        }
    }

    public sealed class NullableUInt16Formatter : IMessagePackFormatter<UInt16?>
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

        public UInt16? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadUInt16();
            }
        }
    }

    public sealed class UInt16ArrayFormatter : IMessagePackFormatter<UInt16[]>
    {
        public static readonly UInt16ArrayFormatter Instance = new UInt16ArrayFormatter();

        UInt16ArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, UInt16[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteUInt16(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public UInt16[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new UInt16[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadUInt16();
                }
                return array;
            }
        }
    }

    public sealed class UInt32Formatter : IMessagePackFormatter<UInt32>
    {
        public static readonly UInt32Formatter Instance = new UInt32Formatter();

        UInt32Formatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, UInt32 value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteUInt32(ref bytes, offset, value);
        }

        public UInt32 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadUInt32();
        }
    }

    public sealed class NullableUInt32Formatter : IMessagePackFormatter<UInt32?>
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

        public UInt32? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadUInt32();
            }
        }
    }

    public sealed class UInt32ArrayFormatter : IMessagePackFormatter<UInt32[]>
    {
        public static readonly UInt32ArrayFormatter Instance = new UInt32ArrayFormatter();

        UInt32ArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, UInt32[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteUInt32(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public UInt32[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new UInt32[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadUInt32();
                }
                return array;
            }
        }
    }

    public sealed class UInt64Formatter : IMessagePackFormatter<UInt64>
    {
        public static readonly UInt64Formatter Instance = new UInt64Formatter();

        UInt64Formatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, UInt64 value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteUInt64(ref bytes, offset, value);
        }

        public UInt64 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadUInt64();
        }
    }

    public sealed class NullableUInt64Formatter : IMessagePackFormatter<UInt64?>
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

        public UInt64? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadUInt64();
            }
        }
    }

    public sealed class UInt64ArrayFormatter : IMessagePackFormatter<UInt64[]>
    {
        public static readonly UInt64ArrayFormatter Instance = new UInt64ArrayFormatter();

        UInt64ArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, UInt64[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteUInt64(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public UInt64[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new UInt64[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadUInt64();
                }
                return array;
            }
        }
    }

    public sealed class SingleFormatter : IMessagePackFormatter<Single>
    {
        public static readonly SingleFormatter Instance = new SingleFormatter();

        SingleFormatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, Single value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteSingle(ref bytes, offset, value);
        }

        public Single Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadSingle();
        }
    }

    public sealed class NullableSingleFormatter : IMessagePackFormatter<Single?>
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

        public Single? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadSingle();
            }
        }
    }

    public sealed class SingleArrayFormatter : IMessagePackFormatter<Single[]>
    {
        public static readonly SingleArrayFormatter Instance = new SingleArrayFormatter();

        SingleArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Single[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteSingle(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public Single[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new Single[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadSingle();
                }
                return array;
            }
        }
    }

    public sealed class DoubleFormatter : IMessagePackFormatter<Double>
    {
        public static readonly DoubleFormatter Instance = new DoubleFormatter();

        DoubleFormatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, Double value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteDouble(ref bytes, offset, value);
        }

        public Double Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadDouble();
        }
    }

    public sealed class NullableDoubleFormatter : IMessagePackFormatter<Double?>
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

        public Double? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadDouble();
            }
        }
    }

    public sealed class DoubleArrayFormatter : IMessagePackFormatter<Double[]>
    {
        public static readonly DoubleArrayFormatter Instance = new DoubleArrayFormatter();

        DoubleArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Double[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteDouble(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public Double[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new Double[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadDouble();
                }
                return array;
            }
        }
    }

    public sealed class BooleanFormatter : IMessagePackFormatter<Boolean>
    {
        public static readonly BooleanFormatter Instance = new BooleanFormatter();

        BooleanFormatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, Boolean value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteBoolean(ref bytes, offset, value);
        }

        public Boolean Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadBoolean();
        }
    }

    public sealed class NullableBooleanFormatter : IMessagePackFormatter<Boolean?>
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

        public Boolean? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadBoolean();
            }
        }
    }

    public sealed class BooleanArrayFormatter : IMessagePackFormatter<Boolean[]>
    {
        public static readonly BooleanArrayFormatter Instance = new BooleanArrayFormatter();

        BooleanArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Boolean[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteBoolean(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public Boolean[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new Boolean[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadBoolean();
                }
                return array;
            }
        }
    }

    public sealed class ByteFormatter : IMessagePackFormatter<Byte>
    {
        public static readonly ByteFormatter Instance = new ByteFormatter();

        ByteFormatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, Byte value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteByte(ref bytes, offset, value);
        }

        public Byte Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadByte();
        }
    }

    public sealed class NullableByteFormatter : IMessagePackFormatter<Byte?>
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

        public Byte? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadByte();
            }
        }
    }


    public sealed class SByteFormatter : IMessagePackFormatter<SByte>
    {
        public static readonly SByteFormatter Instance = new SByteFormatter();

        SByteFormatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, SByte value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteSByte(ref bytes, offset, value);
        }

        public SByte Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadSByte();
        }
    }

    public sealed class NullableSByteFormatter : IMessagePackFormatter<SByte?>
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

        public SByte? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadSByte();
            }
        }
    }

    public sealed class SByteArrayFormatter : IMessagePackFormatter<SByte[]>
    {
        public static readonly SByteArrayFormatter Instance = new SByteArrayFormatter();

        SByteArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, SByte[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteSByte(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public SByte[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new SByte[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadSByte();
                }
                return array;
            }
        }
    }

    public sealed class CharFormatter : IMessagePackFormatter<Char>
    {
        public static readonly CharFormatter Instance = new CharFormatter();

        CharFormatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, Char value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteChar(ref bytes, offset, value);
        }

        public Char Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadChar();
        }
    }

    public sealed class NullableCharFormatter : IMessagePackFormatter<Char?>
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

        public Char? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadChar();
            }
        }
    }

    public sealed class CharArrayFormatter : IMessagePackFormatter<Char[]>
    {
        public static readonly CharArrayFormatter Instance = new CharArrayFormatter();

        CharArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Char[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteChar(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public Char[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new Char[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadChar();
                }
                return array;
            }
        }
    }

    public sealed class DateTimeFormatter : IMessagePackFormatter<DateTime>
    {
        public static readonly DateTimeFormatter Instance = new DateTimeFormatter();

        DateTimeFormatter()
        {
        }

        public int Serialize(ref byte[] bytes, int offset, DateTime value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteDateTime(ref bytes, offset, value);
        }

        public DateTime Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadDateTime();
        }
    }

    public sealed class NullableDateTimeFormatter : IMessagePackFormatter<DateTime?>
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

        public DateTime? Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadDateTime();
            }
        }
    }

    public sealed class DateTimeArrayFormatter : IMessagePackFormatter<DateTime[]>
    {
        public static readonly DateTimeArrayFormatter Instance = new DateTimeArrayFormatter();

        DateTimeArrayFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, DateTime[] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    offset += MessagePackBinary.WriteDateTime(ref bytes, offset, value[i]);
                }

                return offset - startOffset;
            }
        }

        public DateTime[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new DateTime[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadDateTime();
                }
                return array;
            }
        }
    }

}
