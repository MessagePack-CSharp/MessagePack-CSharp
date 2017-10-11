using System;

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

        public DateTime Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.GetMessagePackType(bytes, offset) == MessagePackType.Extension)
            {
                return DateTimeFormatter.Instance.Deserialize(bytes, offset, formatterResolver, out readSize);
            }

            var dateData = MessagePackBinary.ReadInt64(bytes, offset, out readSize);
            return DateTime.FromBinary(dateData);
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

        public DateTime[] Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                var startOffset = offset;

                var len = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                offset += readSize;
                var array = new DateTime[len];
                for (int i = 0; i < array.Length; i++)
                {
                    var dateData = MessagePackBinary.ReadInt64(bytes, offset, out readSize);
                    array[i] = DateTime.FromBinary(dateData);
                    offset += readSize;
                }
                readSize = offset - startOffset;
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

        public string Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return MessagePackBinary.ReadString(bytes, offset, out readSize);
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

        public byte[] Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var type = MessagePackBinary.GetMessagePackType(bytes, offset);
            if (type == MessagePackType.Nil)
            {
                readSize = 1;
                return null;
            }
            else if (type == MessagePackType.Binary)
            {
                return MessagePackBinary.ReadBytes(bytes, offset, out readSize);
            }
            else if (type == MessagePackType.String)
            {
                var code = bytes[offset];
                unchecked
                {
                    if (MessagePackCode.MinFixStr <= code && code <= MessagePackCode.MaxFixStr)
                    {
                        var length = bytes[offset] & 0x1F;
                        readSize = length + 1;
                        var result = new byte[length];
                        Buffer.BlockCopy(bytes, offset + 1, result, 0, result.Length);
                        return result;
                    }
                    else if (code == MessagePackCode.Str8)
                    {
                        var length = (int)bytes[offset + 1];
                        readSize = length + 2;
                        var result = new byte[length];
                        Buffer.BlockCopy(bytes, offset + 2, result, 0, result.Length);
                        return result;
                    }
                    else if (code == MessagePackCode.Str16)
                    {
                        var length = (bytes[offset + 1] << 8) + (bytes[offset + 2]);
                        readSize = length + 3;
                        var result = new byte[length];
                        Buffer.BlockCopy(bytes, offset + 3, result, 0, result.Length);
                        return result;
                    }
                    else if (code == MessagePackCode.Str32)
                    {
                        var length = (int)((uint)(bytes[offset + 1] << 24) | (uint)(bytes[offset + 2] << 16) | (uint)(bytes[offset + 3] << 8) | (uint)bytes[offset + 4]);
                        readSize = length + 5;
                        var result = new byte[length];
                        Buffer.BlockCopy(bytes, offset + 5, result, 0, result.Length);
                        return result;
                    }
                }
            }

            throw new InvalidOperationException(string.Format("code is invalid. code:{0} format:{1}", bytes[offset], MessagePackCode.ToFormatName(bytes[offset])));
        }
    }
}
