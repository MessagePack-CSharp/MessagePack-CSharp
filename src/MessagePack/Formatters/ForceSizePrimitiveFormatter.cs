using System;
using System.Buffers;

namespace MessagePack.Formatters
{
    public sealed class ForceInt16BlockFormatter : IMessagePackFormatter<Int16>
    {
        public static readonly ForceInt16BlockFormatter Instance = new ForceInt16BlockFormatter();

        ForceInt16BlockFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int16 value, IFormatterResolver resolver)
        {
            writer.WriteInt16(value);
        }

        public Int16 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadInt16();
        }
    }

    public sealed class NullableForceInt16BlockFormatter : IMessagePackFormatter<Int16?>
    {
        public static readonly NullableForceInt16BlockFormatter Instance = new NullableForceInt16BlockFormatter();

        NullableForceInt16BlockFormatter()
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
                writer.WriteInt16(value.Value);
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

    public sealed class ForceInt16BlockArrayFormatter : IMessagePackFormatter<Int16[]>
    {
        public static readonly ForceInt16BlockArrayFormatter Instance = new ForceInt16BlockArrayFormatter();

        ForceInt16BlockArrayFormatter()
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
                    writer.WriteInt16(value[i]);
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

    public sealed class ForceInt32BlockFormatter : IMessagePackFormatter<Int32>
    {
        public static readonly ForceInt32BlockFormatter Instance = new ForceInt32BlockFormatter();

        ForceInt32BlockFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int32 value, IFormatterResolver resolver)
        {
            writer.WriteInt32(value);
        }

        public Int32 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadInt32();
        }
    }

    public sealed class NullableForceInt32BlockFormatter : IMessagePackFormatter<Int32?>
    {
        public static readonly NullableForceInt32BlockFormatter Instance = new NullableForceInt32BlockFormatter();

        NullableForceInt32BlockFormatter()
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
                writer.WriteInt32(value.Value);
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

    public sealed class ForceInt32BlockArrayFormatter : IMessagePackFormatter<Int32[]>
    {
        public static readonly ForceInt32BlockArrayFormatter Instance = new ForceInt32BlockArrayFormatter();

        ForceInt32BlockArrayFormatter()
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
                    writer.WriteInt32(value[i]);
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

    public sealed class ForceInt64BlockFormatter : IMessagePackFormatter<Int64>
    {
        public static readonly ForceInt64BlockFormatter Instance = new ForceInt64BlockFormatter();

        ForceInt64BlockFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int64 value, IFormatterResolver resolver)
        {
            writer.WriteInt64(value);
        }

        public Int64 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadInt64();
        }
    }

    public sealed class NullableForceInt64BlockFormatter : IMessagePackFormatter<Int64?>
    {
        public static readonly NullableForceInt64BlockFormatter Instance = new NullableForceInt64BlockFormatter();

        NullableForceInt64BlockFormatter()
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
                writer.WriteInt64(value.Value);
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

    public sealed class ForceInt64BlockArrayFormatter : IMessagePackFormatter<Int64[]>
    {
        public static readonly ForceInt64BlockArrayFormatter Instance = new ForceInt64BlockArrayFormatter();

        ForceInt64BlockArrayFormatter()
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
                    writer.WriteInt64(value[i]);
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

    public sealed class ForceUInt16BlockFormatter : IMessagePackFormatter<UInt16>
    {
        public static readonly ForceUInt16BlockFormatter Instance = new ForceUInt16BlockFormatter();

        ForceUInt16BlockFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt16 value, IFormatterResolver resolver)
        {
            writer.WriteUInt16(value);
        }

        public UInt16 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadUInt16();
        }
    }

    public sealed class NullableForceUInt16BlockFormatter : IMessagePackFormatter<UInt16?>
    {
        public static readonly NullableForceUInt16BlockFormatter Instance = new NullableForceUInt16BlockFormatter();

        NullableForceUInt16BlockFormatter()
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
                writer.WriteUInt16(value.Value);
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

    public sealed class ForceUInt16BlockArrayFormatter : IMessagePackFormatter<UInt16[]>
    {
        public static readonly ForceUInt16BlockArrayFormatter Instance = new ForceUInt16BlockArrayFormatter();

        ForceUInt16BlockArrayFormatter()
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
                    writer.WriteUInt16(value[i]);
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

    public sealed class ForceUInt32BlockFormatter : IMessagePackFormatter<UInt32>
    {
        public static readonly ForceUInt32BlockFormatter Instance = new ForceUInt32BlockFormatter();

        ForceUInt32BlockFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt32 value, IFormatterResolver resolver)
        {
            writer.WriteUInt32(value);
        }

        public UInt32 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadUInt32();
        }
    }

    public sealed class NullableForceUInt32BlockFormatter : IMessagePackFormatter<UInt32?>
    {
        public static readonly NullableForceUInt32BlockFormatter Instance = new NullableForceUInt32BlockFormatter();

        NullableForceUInt32BlockFormatter()
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
                writer.WriteUInt32(value.Value);
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

    public sealed class ForceUInt32BlockArrayFormatter : IMessagePackFormatter<UInt32[]>
    {
        public static readonly ForceUInt32BlockArrayFormatter Instance = new ForceUInt32BlockArrayFormatter();

        ForceUInt32BlockArrayFormatter()
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
                    writer.WriteUInt32(value[i]);
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

    public sealed class ForceUInt64BlockFormatter : IMessagePackFormatter<UInt64>
    {
        public static readonly ForceUInt64BlockFormatter Instance = new ForceUInt64BlockFormatter();

        ForceUInt64BlockFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt64 value, IFormatterResolver resolver)
        {
            writer.WriteUInt64(value);
        }

        public UInt64 Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadUInt64();
        }
    }

    public sealed class NullableForceUInt64BlockFormatter : IMessagePackFormatter<UInt64?>
    {
        public static readonly NullableForceUInt64BlockFormatter Instance = new NullableForceUInt64BlockFormatter();

        NullableForceUInt64BlockFormatter()
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
                writer.WriteUInt64(value.Value);
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

    public sealed class ForceUInt64BlockArrayFormatter : IMessagePackFormatter<UInt64[]>
    {
        public static readonly ForceUInt64BlockArrayFormatter Instance = new ForceUInt64BlockArrayFormatter();

        ForceUInt64BlockArrayFormatter()
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
                    writer.WriteUInt64(value[i]);
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

    public sealed class ForceByteBlockFormatter : IMessagePackFormatter<Byte>
    {
        public static readonly ForceByteBlockFormatter Instance = new ForceByteBlockFormatter();

        ForceByteBlockFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Byte value, IFormatterResolver resolver)
        {
            writer.WriteUInt8(value);
        }

        public Byte Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadByte();
        }
    }

    public sealed class NullableForceByteBlockFormatter : IMessagePackFormatter<Byte?>
    {
        public static readonly NullableForceByteBlockFormatter Instance = new NullableForceByteBlockFormatter();

        NullableForceByteBlockFormatter()
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
                writer.WriteUInt8(value.Value);
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


    public sealed class ForceSByteBlockFormatter : IMessagePackFormatter<SByte>
    {
        public static readonly ForceSByteBlockFormatter Instance = new ForceSByteBlockFormatter();

        ForceSByteBlockFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, SByte value, IFormatterResolver resolver)
        {
            writer.WriteInt8(value);
        }

        public SByte Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadSByte();
        }
    }

    public sealed class NullableForceSByteBlockFormatter : IMessagePackFormatter<SByte?>
    {
        public static readonly NullableForceSByteBlockFormatter Instance = new NullableForceSByteBlockFormatter();

        NullableForceSByteBlockFormatter()
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
                writer.WriteInt8(value.Value);
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

    public sealed class ForceSByteBlockArrayFormatter : IMessagePackFormatter<SByte[]>
    {
        public static readonly ForceSByteBlockArrayFormatter Instance = new ForceSByteBlockArrayFormatter();

        ForceSByteBlockArrayFormatter()
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
                    writer.WriteInt8(value[i]);
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

}
