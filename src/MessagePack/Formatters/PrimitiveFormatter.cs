using System;
using System.Buffers;

namespace MessagePack.Formatters
{
    internal sealed class Int16Formatter : IMessagePackFormatter<Int16>
    {
        internal static readonly Int16Formatter Instance = new Int16Formatter();

        Int16Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int16 value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public Int16 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadInt16();
        }
    }

    internal sealed class NullableInt16Formatter : IMessagePackFormatter<Int16?>
    {
        internal static readonly NullableInt16Formatter Instance = new NullableInt16Formatter();

        NullableInt16Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int16? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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

    internal sealed class Int16ArrayFormatter : IMessagePackFormatter<Int16[]>
    {
        internal static readonly Int16ArrayFormatter Instance = new Int16ArrayFormatter();

        Int16ArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, Int16[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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

    internal sealed class Int32Formatter : IMessagePackFormatter<Int32>
    {
        internal static readonly Int32Formatter Instance = new Int32Formatter();

        Int32Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int32 value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public Int32 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadInt32();
        }
    }

    internal sealed class NullableInt32Formatter : IMessagePackFormatter<Int32?>
    {
        internal static readonly NullableInt32Formatter Instance = new NullableInt32Formatter();

        NullableInt32Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int32? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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

    internal sealed class Int32ArrayFormatter : IMessagePackFormatter<Int32[]>
    {
        internal static readonly Int32ArrayFormatter Instance = new Int32ArrayFormatter();

        Int32ArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, Int32[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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

    internal sealed class Int64Formatter : IMessagePackFormatter<Int64>
    {
        internal static readonly Int64Formatter Instance = new Int64Formatter();

        Int64Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int64 value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public Int64 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadInt64();
        }
    }

    internal sealed class NullableInt64Formatter : IMessagePackFormatter<Int64?>
    {
        internal static readonly NullableInt64Formatter Instance = new NullableInt64Formatter();

        NullableInt64Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int64? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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

    internal sealed class Int64ArrayFormatter : IMessagePackFormatter<Int64[]>
    {
        internal static readonly Int64ArrayFormatter Instance = new Int64ArrayFormatter();

        Int64ArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, Int64[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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

    internal sealed class UInt16Formatter : IMessagePackFormatter<UInt16>
    {
        internal static readonly UInt16Formatter Instance = new UInt16Formatter();

        UInt16Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt16 value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public UInt16 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadUInt16();
        }
    }

    internal sealed class NullableUInt16Formatter : IMessagePackFormatter<UInt16?>
    {
        internal static readonly NullableUInt16Formatter Instance = new NullableUInt16Formatter();

        NullableUInt16Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt16? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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

    internal sealed class UInt16ArrayFormatter : IMessagePackFormatter<UInt16[]>
    {
        internal static readonly UInt16ArrayFormatter Instance = new UInt16ArrayFormatter();

        UInt16ArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, UInt16[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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

    internal sealed class UInt32Formatter : IMessagePackFormatter<UInt32>
    {
        internal static readonly UInt32Formatter Instance = new UInt32Formatter();

        UInt32Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt32 value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public UInt32 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadUInt32();
        }
    }

    internal sealed class NullableUInt32Formatter : IMessagePackFormatter<UInt32?>
    {
        internal static readonly NullableUInt32Formatter Instance = new NullableUInt32Formatter();

        NullableUInt32Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt32? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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

    internal sealed class UInt32ArrayFormatter : IMessagePackFormatter<UInt32[]>
    {
        internal static readonly UInt32ArrayFormatter Instance = new UInt32ArrayFormatter();

        UInt32ArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, UInt32[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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

    internal sealed class UInt64Formatter : IMessagePackFormatter<UInt64>
    {
        internal static readonly UInt64Formatter Instance = new UInt64Formatter();

        UInt64Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt64 value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public UInt64 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadUInt64();
        }
    }

    internal sealed class NullableUInt64Formatter : IMessagePackFormatter<UInt64?>
    {
        internal static readonly NullableUInt64Formatter Instance = new NullableUInt64Formatter();

        NullableUInt64Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt64? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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

    internal sealed class UInt64ArrayFormatter : IMessagePackFormatter<UInt64[]>
    {
        internal static readonly UInt64ArrayFormatter Instance = new UInt64ArrayFormatter();

        UInt64ArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, UInt64[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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

    internal sealed class SingleFormatter : IMessagePackFormatter<Single>
    {
        internal static readonly SingleFormatter Instance = new SingleFormatter();

        SingleFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Single value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public Single Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadSingle();
        }
    }

    internal sealed class NullableSingleFormatter : IMessagePackFormatter<Single?>
    {
        internal static readonly NullableSingleFormatter Instance = new NullableSingleFormatter();

        NullableSingleFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Single? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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

    internal sealed class SingleArrayFormatter : IMessagePackFormatter<Single[]>
    {
        internal static readonly SingleArrayFormatter Instance = new SingleArrayFormatter();

        SingleArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, Single[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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

    internal sealed class DoubleFormatter : IMessagePackFormatter<Double>
    {
        internal static readonly DoubleFormatter Instance = new DoubleFormatter();

        DoubleFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Double value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public Double Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadDouble();
        }
    }

    internal sealed class NullableDoubleFormatter : IMessagePackFormatter<Double?>
    {
        internal static readonly NullableDoubleFormatter Instance = new NullableDoubleFormatter();

        NullableDoubleFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Double? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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

    internal sealed class DoubleArrayFormatter : IMessagePackFormatter<Double[]>
    {
        internal static readonly DoubleArrayFormatter Instance = new DoubleArrayFormatter();

        DoubleArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, Double[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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

    internal sealed class BooleanFormatter : IMessagePackFormatter<Boolean>
    {
        internal static readonly BooleanFormatter Instance = new BooleanFormatter();

        BooleanFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Boolean value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public Boolean Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadBoolean();
        }
    }

    internal sealed class NullableBooleanFormatter : IMessagePackFormatter<Boolean?>
    {
        internal static readonly NullableBooleanFormatter Instance = new NullableBooleanFormatter();

        NullableBooleanFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Boolean? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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

    internal sealed class BooleanArrayFormatter : IMessagePackFormatter<Boolean[]>
    {
        internal static readonly BooleanArrayFormatter Instance = new BooleanArrayFormatter();

        BooleanArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, Boolean[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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

    internal sealed class ByteFormatter : IMessagePackFormatter<Byte>
    {
        internal static readonly ByteFormatter Instance = new ByteFormatter();

        ByteFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Byte value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public Byte Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadByte();
        }
    }

    internal sealed class NullableByteFormatter : IMessagePackFormatter<Byte?>
    {
        internal static readonly NullableByteFormatter Instance = new NullableByteFormatter();

        NullableByteFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Byte? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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


    internal sealed class SByteFormatter : IMessagePackFormatter<SByte>
    {
        internal static readonly SByteFormatter Instance = new SByteFormatter();

        SByteFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, SByte value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public SByte Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadSByte();
        }
    }

    internal sealed class NullableSByteFormatter : IMessagePackFormatter<SByte?>
    {
        internal static readonly NullableSByteFormatter Instance = new NullableSByteFormatter();

        NullableSByteFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, SByte? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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

    internal sealed class SByteArrayFormatter : IMessagePackFormatter<SByte[]>
    {
        internal static readonly SByteArrayFormatter Instance = new SByteArrayFormatter();

        SByteArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, SByte[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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

    internal sealed class CharFormatter : IMessagePackFormatter<Char>
    {
        internal static readonly CharFormatter Instance = new CharFormatter();

        CharFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Char value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public Char Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadChar();
        }
    }

    internal sealed class NullableCharFormatter : IMessagePackFormatter<Char?>
    {
        internal static readonly NullableCharFormatter Instance = new NullableCharFormatter();

        NullableCharFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Char? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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

    internal sealed class CharArrayFormatter : IMessagePackFormatter<Char[]>
    {
        internal static readonly CharArrayFormatter Instance = new CharArrayFormatter();

        CharArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, Char[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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

    internal sealed class DateTimeFormatter : IMessagePackFormatter<DateTime>
    {
        internal static readonly DateTimeFormatter Instance = new DateTimeFormatter();

        DateTimeFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, DateTime value, IFormatterResolver resolver)
        {
            writer.Write(value);
        }

        public DateTime Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadDateTime();
        }
    }

    internal sealed class NullableDateTimeFormatter : IMessagePackFormatter<DateTime?>
    {
        internal static readonly NullableDateTimeFormatter Instance = new NullableDateTimeFormatter();

        NullableDateTimeFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, DateTime? value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
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

    internal sealed class DateTimeArrayFormatter : IMessagePackFormatter<DateTime[]>
    {
        internal static readonly DateTimeArrayFormatter Instance = new DateTimeArrayFormatter();

        DateTimeArrayFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, DateTime[] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
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
