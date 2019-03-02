using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;
using MessagePack.Formatters;
using MessagePack.Internal;

namespace MessagePack
{
    // JSON API
    public partial class MessagePackSerializer
    {
        /// <summary>
        /// Serialize an object to JSON string.
        /// </summary>
        public string SerializeToJson<T>(T obj)
        {
            return ConvertToJson(Serialize(obj));
        }

        /// <summary>
        /// Serialize an object to JSON string.
        /// </summary>
        public string SerializeToJson<T>(T obj, IFormatterResolver resolver)
        {
            return ConvertToJson(Serialize(obj, resolver));
        }

        /// <summary>
        /// Convert a message-pack binary to a JSON string.
        /// </summary>
        public string ConvertToJson(ReadOnlyMemory<byte> bytes) => this.ConvertToJson(new ReadOnlySequence<byte>(bytes));

        /// <summary>
        /// Convert a message-pack binary to a JSON string.
        /// </summary>
        public string ConvertToJson(ReadOnlySequence<byte> bytes)
        {
            var jsonWriter = new StringWriter();
            var reader = new MessagePackReader(bytes);
            this.ConvertToJson(ref reader, jsonWriter);
            return jsonWriter.ToString();
        }

        /// <summary>
        /// Convert a message-pack binary to a JSON string.
        /// </summary>
        public virtual void ConvertToJson(ref MessagePackReader reader, TextWriter jsonWriter)
        {
            if (reader.End)
            {
                return;
            }

            ToJsonCore(ref reader, jsonWriter);
        }

        public byte[] ConvertFromJson(string str)
        {
            using (var sr = new StringReader(str))
            {
                return ConvertFromJson(sr);
            }
        }

        /// <summary>
        /// From Json String to MessagePack binary
        /// </summary>
        public virtual byte[] ConvertFromJson(TextReader reader)
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

        private static void ToJsonCore(ref MessagePackReader reader, TextWriter writer)
        {
            var type = reader.NextMessagePackType;
            switch (type)
            {
                case MessagePackType.Integer:
                    if (MessagePackCode.IsSignedInteger(reader.NextCode))
                    {
                        writer.Write(reader.ReadInt64().ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        writer.Write(reader.ReadUInt64().ToString(CultureInfo.InvariantCulture));
                    }

                    break;
                case MessagePackType.Boolean:
                    writer.Write(reader.ReadBoolean() ? "true" : "false");
                    break;
                case MessagePackType.Float:
                    if (reader.NextCode == MessagePackCode.Float32)
                    {
                        writer.Write(reader.ReadSingle().ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        writer.Write(reader.ReadDouble().ToString(CultureInfo.InvariantCulture));
                    }

                    break;
                case MessagePackType.String:
                    WriteJsonString(reader.ReadString(), writer);
                    break;
                case MessagePackType.Binary:
                    writer.Write("\"" + Convert.ToBase64String(reader.ReadBytes().ToArray()) + "\"");
                    break;
                case MessagePackType.Array:
                    {
                        int length = reader.ReadArrayHeader();
                        writer.Write("[");
                        for (int i = 0; i < length; i++)
                        {
                            ToJsonCore(ref reader, writer);

                            if (i != length - 1)
                            {
                                writer.Write(",");
                            }
                        }
                        writer.Write("]");
                        return;
                    }
                case MessagePackType.Map:
                    {
                        int length = reader.ReadMapHeader();
                        writer.Write("{");
                        for (int i = 0; i < length; i++)
                        {
                            // write key
                            {
                                var keyType = reader.NextMessagePackType;
                                if (keyType == MessagePackType.String || keyType == MessagePackType.Binary)
                                {
                                    ToJsonCore(ref reader, writer);
                                }
                                else
                                {
                                    writer.Write("\"");
                                    ToJsonCore(ref reader, writer);
                                    writer.Write("\"");
                                }
                            }

                            writer.Write(":");

                            // write body
                            {
                                ToJsonCore(ref reader, writer);
                            }

                            if (i != length - 1)
                            {
                                writer.Write(",");
                            }
                        }
                        writer.Write("}");

                        return;
                    }
                case MessagePackType.Extension:
                    var extHeader = reader.ReadExtensionFormatHeader();
                    if (extHeader.TypeCode == ReservedMessagePackExtensionTypeCode.DateTime)
                    {
                        var dt = reader.ReadDateTime(extHeader);
                        writer.Write("\"");
                        writer.Write(dt.ToString("o", CultureInfo.InvariantCulture));
                        writer.Write("\"");
                    }
#if !UNITY
                    else if (extHeader.TypeCode == TypelessFormatter.ExtensionTypeCode)
                    {
                        // prepare type name token
                        var privateBuilder = new StringBuilder();
                        var typeNameTokenBuilder = new StringBuilder();
                        var positionBeforeTypeNameRead = reader.Position;
                        ToJsonCore(ref reader, new StringWriter(typeNameTokenBuilder));
                        int typeNameReadSize = (int)reader.Sequence.Slice(positionBeforeTypeNameRead, reader.Position).Length;
                        if (extHeader.Length > typeNameReadSize)
                        {
                            // object map or array
                            var typeInside = reader.NextMessagePackType;
                            if (typeInside != MessagePackType.Array && typeInside != MessagePackType.Map)
                            {
                                privateBuilder.Append("{");
                            }

                            ToJsonCore(ref reader, new StringWriter(privateBuilder));
                            // insert type name token to start of object map or array
                            if (typeInside != MessagePackType.Array)
                            {
                                typeNameTokenBuilder.Insert(0, "\"$type\":");
                            }

                            if (typeInside != MessagePackType.Array && typeInside != MessagePackType.Map)
                            {
                                privateBuilder.Append("}");
                            }

                            if (privateBuilder.Length > 2)
                            {
                                typeNameTokenBuilder.Append(",");
                            }

                            privateBuilder.Insert(1, typeNameTokenBuilder.ToString());

                            writer.Write(privateBuilder.ToString());
                        }
                        else
                        {
                            writer.Write("{\"$type\":\"" + typeNameTokenBuilder.ToString() + "}");
                        }
                    }
#endif
                    else
                    {
                        var ext = reader.ReadExtensionFormat();
                        writer.Write("[");
                        writer.Write(ext.TypeCode);
                        writer.Write(",");
                        writer.Write("\"");
                        writer.Write(Convert.ToBase64String(ext.Data.ToArray()));
                        writer.Write("\"");
                        writer.Write("]");
                    }
                    break;
                case MessagePackType.Nil:
                    reader.Skip();
                    writer.Write("null");
                    break;
                default:
                    throw new NotSupportedException($"code is invalid. code: {reader.NextCode} format: {MessagePackCode.ToFormatName(reader.NextCode)}");
            }
        }

        // escape string
        private static void WriteJsonString(string value, TextWriter builder)
        {
            builder.Write('\"');

            var len = value.Length;
            for (int i = 0; i < len; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '"':
                        builder.Write("\\\"");
                        break;
                    case '\\':
                        builder.Write("\\\\");
                        break;
                    case '\b':
                        builder.Write("\\b");
                        break;
                    case '\f':
                        builder.Write("\\f");
                        break;
                    case '\n':
                        builder.Write("\\n");
                        break;
                    case '\r':
                        builder.Write("\\r");
                        break;
                    case '\t':
                        builder.Write("\\t");
                        break;
                    default:
                        builder.Write(c);
                        break;
                }
            }

            builder.Write('\"');
        }
    }
}