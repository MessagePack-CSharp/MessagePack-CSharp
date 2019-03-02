using System;
using System.Buffers;

namespace MessagePack.Formatters
{
    /// <summary>
    /// Serialize by .NET native DateTime binary format.
    /// </summary>
    public sealed class NativeDateTimeFormatter : IMessagePackFormatter<DateTime>
    {
        public static readonly NativeDateTimeFormatter Instance = new NativeDateTimeFormatter();

        public int Serialize(ref byte[] bytes, int offset, DateTime value, IFormatterResolver formatterResolver)
        {
            var dateData = value.ToBinary();
            return MessagePackBinary.WriteInt64(ref bytes, offset, dateData);
        }

        public DateTime Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadDateTime();
        }
    }

    public sealed class NativeDateTimeArrayFormatter : IMessagePackFormatter<DateTime[]>
    {
        public static readonly NativeDateTimeArrayFormatter Instance = new NativeDateTimeArrayFormatter();

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
                    offset += MessagePackBinary.WriteInt64(ref bytes, offset, value[i].ToBinary());
                }

                return offset - startOffset;
            }
        }

        public DateTime[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new DateTime[len];
                for (int i = 0; i < array.Length; i++)
                {
                    var dateData = reader.ReadDateTime();
                }

                return array;
            }
        }
    }

    // Old-Spec
    // bin 8, bin 16, bin 32, str 8, str 16, str 32 -> fixraw or raw 16 or raw 32
    // fixraw -> fixstr, raw16 -> str16, raw32 -> str32
    // https://github.com/msgpack/msgpack/blob/master/spec-old.md

    /// <summary>
    /// Old-MessagePack spec's string formatter.
    /// </summary>
    public sealed class OldSpecStringFormatter : IMessagePackFormatter<string>
    {
        public static readonly OldSpecStringFormatter Instance = new OldSpecStringFormatter();

        // Old spec does not exists str 8 format.
        public int Serialize(ref byte[] bytes, int offset, string value, IFormatterResolver formatterResolver)
        {
            if (value == null) return MessagePackBinary.WriteNil(ref bytes, offset);

            MessagePackBinary.EnsureCapacity(ref bytes, offset, StringEncoding.UTF8.GetMaxByteCount(value.Length) + 5);

            int useOffset;
            if (value.Length <= MessagePackRange.MaxFixStringLength)
            {
                useOffset = 1;
            }
            else if (value.Length <= ushort.MaxValue)
            {
                useOffset = 3;
            }
            else
            {
                useOffset = 5;
            }

            // skip length area
            var writeBeginOffset = offset + useOffset;
            var byteCount = StringEncoding.UTF8.GetBytes(value, 0, value.Length, bytes, writeBeginOffset);

            // move body and write prefix
            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                if (useOffset != 1)
                {
                    Buffer.BlockCopy(bytes, writeBeginOffset, bytes, offset + 1, byteCount);
                }
                bytes[offset] = (byte)(MessagePackCode.MinFixStr | byteCount);
                return byteCount + 1;
            }
            else if (byteCount <= ushort.MaxValue)
            {
                if (useOffset != 3)
                {
                    Buffer.BlockCopy(bytes, writeBeginOffset, bytes, offset + 3, byteCount);
                }

                bytes[offset] = MessagePackCode.Str16;
                bytes[offset + 1] = unchecked((byte)(byteCount >> 8));
                bytes[offset + 2] = unchecked((byte)byteCount);
                return byteCount + 3;
            }
            else
            {
                if (useOffset != 5)
                {
                    Buffer.BlockCopy(bytes, writeBeginOffset, bytes, offset + 5, byteCount);
                }

                bytes[offset] = MessagePackCode.Str32;
                bytes[offset + 1] = unchecked((byte)(byteCount >> 24));
                bytes[offset + 2] = unchecked((byte)(byteCount >> 16));
                bytes[offset + 3] = unchecked((byte)(byteCount >> 8));
                bytes[offset + 4] = unchecked((byte)byteCount);
                return byteCount + 5;
            }
        }

        public string Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            return reader.ReadString();
        }
    }

    /// <summary>
    /// Old-MessagePack spec's binary formatter.
    /// </summary>
    public sealed class OldSpecBinaryFormatter : IMessagePackFormatter<byte[]>
    {
        public static readonly OldSpecBinaryFormatter Instance = new OldSpecBinaryFormatter();

        public int Serialize(ref byte[] bytes, int offset, byte[] value, IFormatterResolver formatterResolver)
        {
            if (value == null) return MessagePackBinary.WriteNil(ref bytes, offset);

            var byteCount = value.Length;

            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                MessagePackBinary.EnsureCapacity(ref bytes, offset, byteCount + 1);

                bytes[offset] = (byte)(MessagePackCode.MinFixStr | byteCount);
                Buffer.BlockCopy(value, 0, bytes, offset + 1, byteCount);
                return byteCount + 1;
            }
            else if (byteCount <= ushort.MaxValue)
            {
                MessagePackBinary.EnsureCapacity(ref bytes, offset, byteCount + 3);

                bytes[offset] = MessagePackCode.Str16;
                bytes[offset + 1] = unchecked((byte)(byteCount >> 8));
                bytes[offset + 2] = unchecked((byte)byteCount);
                Buffer.BlockCopy(value, 0, bytes, offset + 3, byteCount);
                return byteCount + 3;
            }
            else
            {
                MessagePackBinary.EnsureCapacity(ref bytes, offset, byteCount + 5);

                bytes[offset] = MessagePackCode.Str32;
                bytes[offset + 1] = unchecked((byte)(byteCount >> 24));
                bytes[offset + 2] = unchecked((byte)(byteCount >> 16));
                bytes[offset + 3] = unchecked((byte)(byteCount >> 8));
                bytes[offset + 4] = unchecked((byte)byteCount);
                Buffer.BlockCopy(value, 0, bytes, offset + 5, byteCount);
                return byteCount + 5;
            }
        }

        public byte[] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            return reader.ReadBytes().ToArray();
        }
    }
}
