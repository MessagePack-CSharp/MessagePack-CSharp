using MessagePack.Formatters;
using MessagePack.Internal;
using System;
using System.Globalization;
using System.IO;
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

        public static byte[] FromJson(string str)
        {
            using (var sr = new StringReader(str))
            {
                return FromJson(sr);
            }
        }

        /// <summary>
        /// From Json String to MessagePack binary
        /// </summary>
        public static byte[] FromJson(TextReader reader)
        {
            var offset = 0;
            byte[] binary = null;
            using (var jr = new TinyJsonReader(reader, false))
            {
                FromJsonCore(jr, ref binary, ref offset);
            }
            MessagePackBinary.FastResize(ref binary, offset);
            return binary;
        }

        /// <summary>
        /// return buffer is from memory pool, be careful to use. 
        /// </summary>
        internal static ArraySegment<byte> FromJsonUnsafe(TextReader reader)
        {
            var offset = 0;
            byte[] binary = InternalMemoryPool.GetBuffer();  // from memory pool.
            using (var jr = new TinyJsonReader(reader, false))
            {
                FromJsonCore(jr, ref binary, ref offset);
            }
            return new ArraySegment<byte>(binary, 0, offset);
        }

        static uint FromJsonCore(TinyJsonReader jr, ref byte[] binary, ref int offset)
        {
            uint count = 0;
            while (jr.Read())
            {
                switch (jr.TokenType)
                {
                    case TinyJsonToken.None:
                        break;
                    case TinyJsonToken.StartObject:
                        {
                            var startOffset = offset;
                            offset += 5;
                            var mapCount = FromJsonCore(jr, ref binary, ref offset);
                            mapCount = mapCount / 2; // remove propertyname string count.
                            MessagePackBinary.WriteMapHeaderForceMap32Block(ref binary, startOffset, mapCount);
                            count++;
                            break;
                        }
                    case TinyJsonToken.EndObject:
                        return count; // break
                    case TinyJsonToken.StartArray:
                        {
                            var startOffset = offset;
                            offset += 5;
                            var arrayCount = FromJsonCore(jr, ref binary, ref offset);
                            MessagePackBinary.WriteArrayHeaderForceArray32Block(ref binary, startOffset, arrayCount);
                            count++;
                            break;
                        }
                    case TinyJsonToken.EndArray:
                        return count; // break
                    case TinyJsonToken.Number:
                        var v = jr.ValueType;
                        if (v == ValueType.Double)
                        {
                            offset += MessagePackBinary.WriteDouble(ref binary, offset, jr.DoubleValue);
                        }
                        else if (v == ValueType.Long)
                        {
                            offset += MessagePackBinary.WriteInt64(ref binary, offset, jr.LongValue);
                        }
                        else if (v == ValueType.ULong)
                        {
                            offset += MessagePackBinary.WriteUInt64(ref binary, offset, jr.ULongValue);
                        }
                        else if (v == ValueType.Decimal)
                        {
                            offset += DecimalFormatter.Instance.Serialize(ref binary, offset, jr.DecimalValue, null);
                        }
                        count++;
                        break;
                    case TinyJsonToken.String:
                        offset += MessagePackBinary.WriteString(ref binary, offset, jr.StringValue);
                        count++;
                        break;
                    case TinyJsonToken.True:
                        offset += MessagePackBinary.WriteBoolean(ref binary, offset, true);
                        count++;
                        break;
                    case TinyJsonToken.False:
                        offset += MessagePackBinary.WriteBoolean(ref binary, offset, false);
                        count++;
                        break;
                    case TinyJsonToken.Null:
                        offset += MessagePackBinary.WriteNil(ref binary, offset);
                        count++;
                        break;
                    default:
                        break;
                }
            }
            return count;
        }

        static int ToJsonCore(byte[] bytes, int offset, StringBuilder builder)
        {
            var readSize = 0;
            var type = MessagePackBinary.GetMessagePackType(bytes, offset);
            switch (type)
            {
                case MessagePackType.Integer:
                    var code = bytes[offset];
                    if (MessagePackCode.MinNegativeFixInt <= code && code <= MessagePackCode.MaxNegativeFixInt) builder.Append(MessagePackBinary.ReadSByte(bytes, offset, out readSize).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    else if (MessagePackCode.MinFixInt <= code && code <= MessagePackCode.MaxFixInt) builder.Append(MessagePackBinary.ReadByte(bytes, offset, out readSize).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    else if (code == MessagePackCode.Int8) builder.Append(MessagePackBinary.ReadSByte(bytes, offset, out readSize).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    else if (code == MessagePackCode.Int16) builder.Append(MessagePackBinary.ReadInt16(bytes, offset, out readSize).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    else if (code == MessagePackCode.Int32) builder.Append(MessagePackBinary.ReadInt32(bytes, offset, out readSize).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    else if (code == MessagePackCode.Int64) builder.Append(MessagePackBinary.ReadInt64(bytes, offset, out readSize).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    else if (code == MessagePackCode.UInt8) builder.Append(MessagePackBinary.ReadByte(bytes, offset, out readSize).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    else if (code == MessagePackCode.UInt16) builder.Append(MessagePackBinary.ReadUInt16(bytes, offset, out readSize).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    else if (code == MessagePackCode.UInt32) builder.Append(MessagePackBinary.ReadUInt32(bytes, offset, out readSize).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    else if (code == MessagePackCode.UInt64) builder.Append(MessagePackBinary.ReadUInt64(bytes, offset, out readSize).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case MessagePackType.Boolean:
                    builder.Append(MessagePackBinary.ReadBoolean(bytes, offset, out readSize) ? "true" : "false");
                    break;
                case MessagePackType.Float:
                    var floatCode = bytes[offset];
                    if (floatCode == MessagePackCode.Float32)
                    {
                        builder.Append(MessagePackBinary.ReadSingle(bytes, offset, out readSize).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(MessagePackBinary.ReadDouble(bytes, offset, out readSize).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
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
                    var extHeader = MessagePackBinary.ReadExtensionFormatHeader(bytes, offset, out readSize);
                    if (extHeader.TypeCode == ReservedMessagePackExtensionTypeCode.DateTime)
                    {
                        var dt = MessagePackBinary.ReadDateTime(bytes, offset, out readSize);
                        builder.Append("\"");
                        builder.Append(dt.ToString("o", CultureInfo.InvariantCulture));
                        builder.Append("\"");
                    }
#if NETSTANDARD
                    else if (extHeader.TypeCode == TypelessFormatter.ExtensionTypeCode)
                    {
                        int startOffset = offset;
                        // prepare type name token
                        offset += 6;
                        var typeNameToken = new StringBuilder();
                        var typeNameReadSize = ToJsonCore(bytes, offset, typeNameToken);
                        offset += typeNameReadSize;
                        int startBuilderLength = builder.Length;
                        if (extHeader.Length > typeNameReadSize)
                        {
                            // object map or array
                            var typeInside = MessagePackBinary.GetMessagePackType(bytes, offset);
                            if (typeInside != MessagePackType.Array && typeInside != MessagePackType.Map)
                                builder.Append("{");
                            offset += ToJsonCore(bytes, offset, builder);
                            // insert type name token to start of object map or array
                            if (typeInside != MessagePackType.Array)
                                typeNameToken.Insert(0, "\"$type\":");
                            if (typeInside != MessagePackType.Array && typeInside != MessagePackType.Map)
                                builder.Append("}");
                            if (builder.Length - startBuilderLength > 2)
                                typeNameToken.Append(",");
                            builder.Insert(startBuilderLength + 1, typeNameToken.ToString());
                        }
                        else
                        {
                            builder.Append("{\"$type\":\"" + typeNameToken.ToString() + "}");
                        }
                        readSize = offset - startOffset;
                    }
#endif
                    else
                    {
                        var ext = MessagePackBinary.ReadExtensionFormat(bytes, offset, out readSize);
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