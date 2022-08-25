// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
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
        /// <exception cref="MessagePackSerializationException">Thrown if an error occurs during serialization.</exception>
        public static void SerializeToJson<T>(TextWriter textWriter, T obj, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            options = options ?? DefaultOptions;

            using (var sequenceRental = options.SequencePool.Rent())
            {
                var msgpackWriter = new MessagePackWriter(sequenceRental.Value)
                {
                    CancellationToken = cancellationToken,
                };
                Serialize(ref msgpackWriter, obj, options);
                msgpackWriter.Flush();
                var msgpackReader = new MessagePackReader(sequenceRental.Value)
                {
                    CancellationToken = cancellationToken,
                };
                ConvertToJson(ref msgpackReader, textWriter, options);
            }
        }

        /// <summary>
        /// Serialize an object to JSON string.
        /// </summary>
        /// <exception cref="MessagePackSerializationException">Thrown if an error occurs during serialization.</exception>
        public static string SerializeToJson<T>(T obj, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            var writer = new StringWriter();
            SerializeToJson(writer, obj, options, cancellationToken);
            return writer.ToString();
        }

        /// <summary>
        /// Convert a message-pack binary to a JSON string.
        /// </summary>
        /// <exception cref="MessagePackSerializationException">Thrown if an error occurs while reading the messagepack data or writing out the JSON.</exception>
        public static string ConvertToJson(ReadOnlyMemory<byte> bytes, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default) => ConvertToJson(new ReadOnlySequence<byte>(bytes), options, cancellationToken);

        /// <summary>
        /// Convert a message-pack binary to a JSON string.
        /// </summary>
        /// <exception cref="MessagePackSerializationException">Thrown if an error occurs while reading the messagepack data or writing out the JSON.</exception>
        public static string ConvertToJson(in ReadOnlySequence<byte> bytes, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            var jsonWriter = new StringWriter();
            var reader = new MessagePackReader(bytes)
            {
                CancellationToken = cancellationToken,
            };
            ConvertToJson(ref reader, jsonWriter, options);
            return jsonWriter.ToString();
        }

        /// <summary>
        /// Convert a message-pack binary to a JSON string.
        /// </summary>
        /// <exception cref="MessagePackSerializationException">Thrown if an error occurs while reading the messagepack data or writing out the JSON.</exception>
        public static void ConvertToJson(ref MessagePackReader reader, TextWriter jsonWriter, MessagePackSerializerOptions options = null)
        {
            if (reader.End)
            {
                return;
            }

            options = options ?? DefaultOptions;
            try
            {
                if (options.Compression.IsCompression())
                {
                    using (var scratchRental = options.SequencePool.Rent())
                    {
                        if (TryDecompress(ref reader, scratchRental.Value))
                        {
                            var scratchReader = new MessagePackReader(scratchRental.Value)
                            {
                                CancellationToken = reader.CancellationToken,
                            };
                            if (scratchReader.End)
                            {
                                return;
                            }

                            ToJsonCore(ref scratchReader, jsonWriter, options);
                        }
                        else
                        {
                            ToJsonCore(ref reader, jsonWriter, options);
                        }
                    }
                }
                else
                {
                    ToJsonCore(ref reader, jsonWriter, options);
                }
            }
            catch (Exception ex)
            {
                throw new MessagePackSerializationException("Error occurred while translating msgpack to JSON.", ex);
            }
        }

        /// <summary>
        /// Translates the given JSON to MessagePack.
        /// </summary>
        public static void ConvertFromJson(string str, ref MessagePackWriter writer, MessagePackSerializerOptions options = null)
        {
            using (var sr = new StringReader(str))
            {
                ConvertFromJson(sr, ref writer, options);
            }
        }

        /// <summary>
        /// Translates the given JSON to MessagePack.
        /// </summary>
        public static byte[] ConvertFromJson(string str, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            options = options ?? DefaultOptions;

            using (var scratchRental = options.SequencePool.Rent())
            {
                var writer = new MessagePackWriter(scratchRental.Value)
                {
                    CancellationToken = cancellationToken,
                };
                using (var sr = new StringReader(str))
                {
                    ConvertFromJson(sr, ref writer, options);
                }

                writer.Flush();
                return scratchRental.Value.AsReadOnlySequence.ToArray();
            }
        }

        /// <summary>
        /// Translates the given JSON to MessagePack.
        /// </summary>
        public static void ConvertFromJson(TextReader reader, ref MessagePackWriter writer, MessagePackSerializerOptions options = null)
        {
            options = options ?? DefaultOptions;

            if (options.Compression.IsCompression())
            {
                using (var scratchRental = options.SequencePool.Rent())
                {
                    MessagePackWriter scratchWriter = writer.Clone(scratchRental.Value);
                    using (var jr = new TinyJsonReader(reader, false))
                    {
                        FromJsonCore(jr, ref scratchWriter, options);
                    }

                    scratchWriter.Flush();
                    ToLZ4BinaryCore(scratchRental.Value, ref writer, options.Compression, options.CompressionMinLength);
                }
            }
            else
            {
                using (var jr = new TinyJsonReader(reader, false))
                {
                    FromJsonCore(jr, ref writer, options);
                }
            }
        }

        private static uint FromJsonCore(TinyJsonReader jr, ref MessagePackWriter writer, MessagePackSerializerOptions options)
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
                        using (var scratchRental = options.SequencePool.Rent())
                        {
                            MessagePackWriter scratchWriter = writer.Clone(scratchRental.Value);
                            var mapCount = FromJsonCore(jr, ref scratchWriter, options);
                            scratchWriter.Flush();

                            mapCount = mapCount / 2; // remove propertyname string count.
                            writer.WriteMapHeader(mapCount);
                            writer.WriteRaw(scratchRental.Value);
                        }

                        count++;
                        break;
                    case TinyJsonToken.EndObject:
                        return count; // break
                    case TinyJsonToken.StartArray:
                        // Set up a scratch area to serialize the collection since we don't know its length yet, which must be written first.
                        using (var scratchRental = options.SequencePool.Rent())
                        {
                            MessagePackWriter scratchWriter = writer.Clone(scratchRental.Value);
                            var arrayCount = FromJsonCore(jr, ref scratchWriter, options);
                            scratchWriter.Flush();

                            writer.WriteArrayHeader(arrayCount);
                            writer.WriteRaw(scratchRental.Value);
                        }

                        count++;
                        break;
                    case TinyJsonToken.EndArray:
                        return count; // break
                    case TinyJsonToken.Number:
                        ValueType v = jr.ValueType;
                        if (v == ValueType.Double)
                        {
                            writer.Write(jr.DoubleValue);
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

        private static void ToJsonCore(ref MessagePackReader reader, TextWriter writer, MessagePackSerializerOptions options)
        {
            MessagePackType type = reader.NextMessagePackType;
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
                    ArraySegment<byte> segment = ByteArraySegmentFormatter.Instance.Deserialize(ref reader, options);
                    writer.Write("\"" + Convert.ToBase64String(segment.Array, segment.Offset, segment.Count) + "\"");
                    break;
                case MessagePackType.Array:
                    {
                        int length = reader.ReadArrayHeader();
                        options.Security.DepthStep(ref reader);
                        try
                        {
                            writer.Write("[");
                            for (int i = 0; i < length; i++)
                            {
                                ToJsonCore(ref reader, writer, options);

                                if (i != length - 1)
                                {
                                    writer.Write(",");
                                }
                            }

                            writer.Write("]");
                        }
                        finally
                        {
                            reader.Depth--;
                        }

                        return;
                    }

                case MessagePackType.Map:
                    {
                        int length = reader.ReadMapHeader();
                        options.Security.DepthStep(ref reader);
                        try
                        {
                            writer.Write("{");
                            for (int i = 0; i < length; i++)
                            {
                                // write key
                                {
                                    MessagePackType keyType = reader.NextMessagePackType;
                                    if (keyType == MessagePackType.String || keyType == MessagePackType.Binary)
                                    {
                                        ToJsonCore(ref reader, writer, options);
                                    }
                                    else
                                    {
                                        writer.Write("\"");
                                        ToJsonCore(ref reader, writer, options);
                                        writer.Write("\"");
                                    }
                                }

                                writer.Write(":");

                                // write body
                                {
                                    ToJsonCore(ref reader, writer, options);
                                }

                                if (i != length - 1)
                                {
                                    writer.Write(",");
                                }
                            }

                            writer.Write("}");
                        }
                        finally
                        {
                            reader.Depth--;
                        }

                        return;
                    }

                case MessagePackType.Extension:
                    ExtensionHeader extHeader = reader.ReadExtensionFormatHeader();
                    if (extHeader.TypeCode == ReservedMessagePackExtensionTypeCode.DateTime)
                    {
                        DateTime dt = reader.ReadDateTime(extHeader);
                        writer.Write("\"");
                        writer.Write(dt.ToString("o", CultureInfo.InvariantCulture));
                        writer.Write("\"");
                    }
#if !UNITY_2018_3_OR_NEWER
                    else if (extHeader.TypeCode == ThisLibraryExtensionTypeCodes.TypelessFormatter)
                    {
                        // prepare type name token
                        var privateBuilder = new StringBuilder();
                        var typeNameTokenBuilder = new StringBuilder();
                        SequencePosition positionBeforeTypeNameRead = reader.Position;
                        ToJsonCore(ref reader, new StringWriter(typeNameTokenBuilder), options);
                        int typeNameReadSize = (int)reader.Sequence.Slice(positionBeforeTypeNameRead, reader.Position).Length;
                        if (extHeader.Length > typeNameReadSize)
                        {
                            // object map or array
                            MessagePackType typeInside = reader.NextMessagePackType;
                            if (typeInside != MessagePackType.Array && typeInside != MessagePackType.Map)
                            {
                                privateBuilder.Append("{");
                            }

                            ToJsonCore(ref reader, new StringWriter(privateBuilder), options);

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
                            writer.Write("{\"$type\":" + typeNameTokenBuilder.ToString() + "}");
                        }
                    }
#endif
                    else
                    {
                        var data = reader.ReadRaw((long)extHeader.Length);
                        writer.Write("[");
                        writer.Write(extHeader.TypeCode);
                        writer.Write(",");
                        writer.Write("\"");
                        writer.Write(Convert.ToBase64String(data.ToArray()));
                        writer.Write("\"");
                        writer.Write("]");
                    }

                    break;
                case MessagePackType.Nil:
                    reader.Skip();
                    writer.Write("null");
                    break;
                default:
                    throw new MessagePackSerializationException($"code is invalid. code: {reader.NextCode} format: {MessagePackCode.ToFormatName(reader.NextCode)}");
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
