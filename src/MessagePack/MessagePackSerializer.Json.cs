﻿using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;
using MessagePack.Formatters;
using Nerdbank.Streams;

namespace MessagePack
{
    // JSON API
    public partial class MessagePackSerializer
    {
        /// <summary>
        /// Serialize an object to JSON string.
        /// </summary>
        public void SerializeToJson<T>(TextWriter textWriter, T obj, IFormatterResolver resolver = null)
        {
            using (var sequence = new Sequence<byte>())
            {
                var msgpackWriter = new MessagePackWriter(sequence);
                Serialize(ref msgpackWriter, obj, resolver);
                msgpackWriter.Flush();
                var msgpackReader = new MessagePackReader(sequence.AsReadOnlySequence);
                ConvertToJson(ref msgpackReader, textWriter);
            }
        }

        /// <summary>
        /// Serialize an object to JSON string.
        /// </summary>
        public string SerializeToJson<T>(T obj, IFormatterResolver resolver = null)
        {
            var writer = new StringWriter();
            SerializeToJson(writer, obj, resolver);
            return writer.ToString();
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

        /// <summary>
        /// From Json String to MessagePack binary
        /// </summary>
        public void ConvertFromJson(string str, ref MessagePackWriter writer)
        {
            using (var sr = new StringReader(str))
            {
                ConvertFromJson(sr, ref writer);
            }
        }

        /// <summary>
        /// From Json String to MessagePack binary
        /// </summary>
        public virtual void ConvertFromJson(TextReader reader, ref MessagePackWriter writer)
        {
            using (var jr = new TinyJsonReader(reader, false))
            {
                FromJsonCore(jr, ref writer);
            }
        }

        private static uint FromJsonCore(TinyJsonReader jr, ref MessagePackWriter writer)
        {
            uint count = 0;
            while (jr.Read())
            {
                switch (jr.TokenType)
                {
                    case TinyJsonToken.None:
                        break;
                    case TinyJsonToken.StartObject:
                        // Set up a scratch area to serialize the collection since we don't know its length yet, which must be written first.
                        using (var scratch = new Sequence<byte>())
                        {
                            var scratchWriter = writer.Clone(scratch);
                            var mapCount = FromJsonCore(jr, ref scratchWriter);
                            scratchWriter.Flush();

                            mapCount = mapCount / 2; // remove propertyname string count.
                            writer.WriteMapHeader(mapCount);
                            writer.WriteRaw(scratch.AsReadOnlySequence);
                        }

                        count++;
                        break;
                    case TinyJsonToken.EndObject:
                        return count; // break
                    case TinyJsonToken.StartArray:
                        // Set up a scratch area to serialize the collection since we don't know its length yet, which must be written first.
                        using (var scratch = new Sequence<byte>())
                        {
                            var scratchWriter = writer.Clone(scratch);
                            var arrayCount = FromJsonCore(jr, ref scratchWriter);
                            scratchWriter.Flush();

                            writer.WriteArrayHeader(arrayCount);
                            writer.WriteRaw(scratch.AsReadOnlySequence);
                        }

                        count++;
                        break;
                    case TinyJsonToken.EndArray:
                        return count; // break
                    case TinyJsonToken.Number:
                        var v = jr.ValueType;
                        if (v == ValueType.Double)
                        {
                            writer.Write(jr.DoubleValue);
                        }
                        if (v == ValueType.Float)
                        {
                            offset += MessagePackBinary.WriteSingle(ref binary, offset, jr.FloatValue);
                        }
                        else if (v == ValueType.Long)
                        {
                            writer.Write(jr.LongValue);
                        }
                        else if (v == ValueType.ULong)
                        {
                            writer.Write(jr.ULongValue);
                        }
                        else if (v == ValueType.Decimal)
                        {
                            DecimalFormatter.Instance.Serialize(ref writer, jr.DecimalValue, null);
                        }
                        count++;
                        break;
                    case TinyJsonToken.String:
                        writer.Write(jr.StringValue);
                        count++;
                        break;
                    case TinyJsonToken.True:
                        writer.Write(true);
                        count++;
                        break;
                    case TinyJsonToken.False:
                        writer.Write(false);
                        count++;
                        break;
                    case TinyJsonToken.Null:
                        writer.WriteNil();
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