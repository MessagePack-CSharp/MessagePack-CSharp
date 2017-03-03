using System;
using System.Globalization;
using System.Text;

namespace MessagePack
{
    // JSON API
    public static partial class MessagePackSerializer
    {
        /// <summary>
        /// Dump to JSON string.
        /// </summary>
        public static string ToJson<T>(T obj)
        {
            return ToJson(Serialize(obj));
        }

        /// <summary>
        /// Dump to JSON string.
        /// </summary>
        public static string ToJson<T>(T obj, IFormatterResolver resolver)
        {
            return ToJson(Serialize(obj, resolver));
        }

        /// <summary>
        /// Dump message-pack binary to JSON string.
        /// </summary>
        public static string ToJson(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return "";

            var sb = new StringBuilder();
            ToJsonCore(bytes, 0, sb);
            return sb.ToString();
        }

        static int ToJsonCore(byte[] bytes, int offset, StringBuilder builder)
        {
            var readSize = 0;
            var type = MessagePackBinary.GetMessagePackType(bytes, offset);
            switch (type)
            {
                case MessagePackType.Integer:
                    var code = bytes[offset];
                    if (MessagePackCode.MinNegativeFixInt <= code && code <= MessagePackCode.MaxNegativeFixInt) builder.Append(MessagePackBinary.ReadSByte(bytes, offset, out readSize));
                    else if (MessagePackCode.MinFixInt <= code && code <= MessagePackCode.MaxFixInt) builder.Append(MessagePackBinary.ReadByte(bytes, offset, out readSize));
                    else if (code == MessagePackCode.Int8) builder.Append(MessagePackBinary.ReadSByte(bytes, offset, out readSize));
                    else if (code == MessagePackCode.Int16) builder.Append(MessagePackBinary.ReadInt16(bytes, offset, out readSize));
                    else if (code == MessagePackCode.Int32) builder.Append(MessagePackBinary.ReadInt32(bytes, offset, out readSize));
                    else if (code == MessagePackCode.Int64) builder.Append(MessagePackBinary.ReadInt64(bytes, offset, out readSize));
                    else if (code == MessagePackCode.UInt8) builder.Append(MessagePackBinary.ReadByte(bytes, offset, out readSize));
                    else if (code == MessagePackCode.UInt16) builder.Append(MessagePackBinary.ReadUInt16(bytes, offset, out readSize));
                    else if (code == MessagePackCode.UInt32) builder.Append(MessagePackBinary.ReadUInt32(bytes, offset, out readSize));
                    else if (code == MessagePackCode.UInt64) builder.Append(MessagePackBinary.ReadUInt64(bytes, offset, out readSize));
                    break;
                case MessagePackType.Boolean:
                    builder.Append(MessagePackBinary.ReadBoolean(bytes, offset, out readSize) ? "true" : "false");
                    break;
                case MessagePackType.Float:
                    builder.Append(MessagePackBinary.ReadDouble(bytes, offset, out readSize));
                    break;
                case MessagePackType.String:
                    WriteJsonString(MessagePackBinary.ReadString(bytes, offset, out readSize), builder);
                    break;
                case MessagePackType.Binary:
                    builder.Append("\"" + Convert.ToBase64String(MessagePackBinary.ReadBytes(bytes, offset, out readSize)) + "\"");
                    break;
                case MessagePackType.Array:
                    {
                        var length = MessagePackBinary.ReadArrayHeaderRaw(bytes, offset, out readSize);
                        var totalReadSize = readSize;
                        offset += readSize;
                        builder.Append("[");
                        for (int i = 0; i < length; i++)
                        {
                            readSize = ToJsonCore(bytes, offset, builder);
                            offset += readSize;
                            totalReadSize += readSize;

                            if (i != length - 1)
                            {
                                builder.Append(",");
                            }
                        }
                        builder.Append("]");

                        return totalReadSize;
                    }
                case MessagePackType.Map:
                    {
                        var length = MessagePackBinary.ReadMapHeaderRaw(bytes, offset, out readSize);
                        var totalReadSize = readSize;
                        offset += readSize;
                        builder.Append("{");
                        for (int i = 0; i < length; i++)
                        {
                            // write key
                            {
                                var keyType = MessagePackBinary.GetMessagePackType(bytes, offset);
                                if (keyType == MessagePackType.String || keyType == MessagePackType.Binary)
                                {
                                    readSize = ToJsonCore(bytes, offset, builder);
                                }
                                else
                                {
                                    builder.Append("\"");
                                    readSize = ToJsonCore(bytes, offset, builder);
                                    builder.Append("\"");
                                }
                                offset += readSize;
                                totalReadSize += readSize;
                            }

                            builder.Append(":");

                            // write body
                            {
                                readSize = ToJsonCore(bytes, offset, builder);
                                offset += readSize;
                                totalReadSize += readSize;
                            }

                            if (i != length - 1)
                            {
                                builder.Append(",");
                            }
                        }
                        builder.Append("}");

                        return totalReadSize;
                    }
                case MessagePackType.Extension:
                    var ext = MessagePackBinary.ReadExtensionFormat(bytes, offset, out readSize);
                    if (ext.TypeCode == ReservedMessagePackExtensionTypeCode.DateTime)
                    {
                        var dt = MessagePackBinary.ReadDateTime(bytes, offset, out readSize);
                        builder.Append("\"");
                        builder.Append(dt.ToString("o", CultureInfo.InvariantCulture));
                        builder.Append("\"");
                    }
                    else
                    {
                        builder.Append("[");
                        builder.Append(ext.TypeCode);
                        builder.Append(",");
                        builder.Append("\"");
                        builder.Append(Convert.ToBase64String(ext.Data));
                        builder.Append("\"");
                        builder.Append("]");
                    }
                    break;
                case MessagePackType.Unknown:
                case MessagePackType.Nil:
                default:
                    readSize = 1;
                    builder.Append("null");
                    break;
            }

            return readSize;
        }

        // escape string
        static void WriteJsonString(string value, StringBuilder builder)
        {
            builder.Append('\"');

            var len = value.Length;
            for (int i = 0; i < len; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            builder.Append('\"');
        }
    }
}