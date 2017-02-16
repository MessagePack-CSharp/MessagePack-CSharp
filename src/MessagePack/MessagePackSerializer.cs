using MessagePack.Internal;
using System;
using System.IO;
using System.Text;

namespace MessagePack
{
    public static partial class MessagePackSerializer
    {
        static IFormatterResolver defaultResolver = Resolvers.DefaultResolver.Instance;

        /// <summary>
        /// Change default resolver(configuration).
        /// </summary>
        /// <param name="resolver"></param>
        public static void SetDefaultResolver(IFormatterResolver resolver)
        {
            defaultResolver = resolver;
        }

        /// <summary>
        /// Serialize to binary with default resolver.
        /// </summary>
        public static byte[] Serialize<T>(T obj)
        {
            return Serialize(obj, defaultResolver);
        }

        /// <summary>
        /// Serialize to binary with specified resolver.
        /// </summary>
        public static byte[] Serialize<T>(T obj, IFormatterResolver resolver)
        {
            var formatter = resolver.GetFormatterWithVerify<T>();

            var buffer = InternalMemoryPool.Buffer;

            var len = formatter.Serialize(ref buffer, 0, obj, resolver);

            // do not return MemoryPool.Buffer.
            return MessagePackBinary.FastResizeClone(buffer, len);
        }

        /// <summary>
        /// Serialize to binary. Get the raw memory pool byte[]. The result can not share across thread and can not hold, so use quickly.
        /// </summary>
        public static ArraySegment<byte> SerializeUnsafe<T>(T obj)
        {
            return SerializeUnsafe(obj, defaultResolver);
        }

        /// <summary>
        /// Serialize to binary with specified resolver. Get the raw memory pool byte[]. The result can not share across thread and can not hold, so use quickly.
        /// </summary>
        public static ArraySegment<byte> SerializeUnsafe<T>(T obj, IFormatterResolver resolver)
        {
            var formatter = resolver.GetFormatterWithVerify<T>();

            var buffer = InternalMemoryPool.Buffer;

            var len = formatter.Serialize(ref buffer, 0, obj, resolver);

            // return raw memory pool, unsafe!
            return new ArraySegment<byte>(buffer, 0, len);
        }

        /// <summary>
        /// Serialize to stream.
        /// </summary>
        public static void Serialize<T>(Stream stream, T obj)
        {
            Serialize(stream, obj, defaultResolver);
        }

        /// <summary>
        /// Serialize to stream with specified resolver.
        /// </summary>
        public static void Serialize<T>(Stream stream, T obj, IFormatterResolver resolver)
        {
            var formatter = resolver.GetFormatterWithVerify<T>();

            var buffer = InternalMemoryPool.Buffer;

            var len = formatter.Serialize(ref buffer, 0, obj, resolver);

            // do not need resize.
            stream.Write(buffer, 0, len);
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            return Deserialize<T>(bytes, defaultResolver);
        }

        public static T Deserialize<T>(byte[] bytes, IFormatterResolver resolver)
        {
            var formatter = resolver.GetFormatterWithVerify<T>();

            int readSize;
            return formatter.Deserialize(bytes, 0, resolver, out readSize);
        }

        public static T Deserialize<T>(Stream stream)
        {
            return Deserialize<T>(stream, defaultResolver);
        }

        public static T Deserialize<T>(Stream stream, IFormatterResolver resolver)
        {
            var formatter = resolver.GetFormatterWithVerify<T>();

            var ms = stream as MemoryStream;
            if (ms != null)
            {
                // optimize for MemoryStream
                ArraySegment<byte> buffer;
                if (ms.TryGetBuffer(out buffer))
                {
                    int readSize;
                    return formatter.Deserialize(buffer.Array, buffer.Offset, resolver, out readSize);
                }
            }

            // no else.
            {
                var buffer = InternalMemoryPool.Buffer;
                var length = FillFromStream(stream, ref buffer);

                int readSize;
                return formatter.Deserialize(buffer, 0, resolver, out readSize);
            }
        }

        static int FillFromStream(Stream input, ref byte[] buffer)
        {
            int length = 0;
            int read;
            while ((read = input.Read(buffer, length, buffer.Length - length)) > 0)
            {
                length += read;
                if (length == buffer.Length)
                {
                    MessagePackBinary.FastResize(ref buffer, length * 2);
                }
            }

            return length;
        }

        // JSON API

        /// <summary>
        /// Dump message-pack binary to JSON string.
        /// </summary>
        public static string ToJson(byte[] bytes)
        {
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
                    if (code == MessagePackCode.UInt64)
                    {
                        builder.Append(MessagePackBinary.ReadUInt64(bytes, offset, out readSize));
                    }
                    else
                    {
                        builder.Append(MessagePackBinary.ReadInt64(bytes, offset, out readSize));
                    }
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
                    if (ext.TypeCode == -1)
                    {
                        var dt = MessagePackBinary.ReadDateTime(bytes, offset, out readSize);
                        builder.Append("\"");
                        builder.Append(dt.ToString("o"));
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

        // TODO:NonGeneric API
    }
}

namespace MessagePack.Internal
{
    internal static class InternalMemoryPool
    {
        [ThreadStatic]
        public static readonly byte[] Buffer = new byte[65536];
    }
}