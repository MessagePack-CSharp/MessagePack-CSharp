#if ENABLE_UNSAFE_RESOLVER

using System;
using System.IO;

namespace MessagePack
{
    /// <summary>
    /// LZ4 Compressed special serializer.
    /// </summary>
    public static partial class LZ4MessagePackSerializer
    {
        public const sbyte ExtensionTypeCode = 99;

        public const int NotCompressionSize = 64;

        static IFormatterResolver defaultResolver;

        /// <summary>
        /// FormatterResolver that used resolver less overloads. If does not set it, used StandardResolver.
        /// </summary>
        public static IFormatterResolver DefaultResolver
        {
            get
            {
                if (defaultResolver == null)
                {
                    defaultResolver = MessagePack.Resolvers.StandardResolver.Instance;
                }

                return defaultResolver;
            }
        }

        /// <summary>
        /// Is resolver decided?
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                return defaultResolver != null;
            }
        }

        /// <summary>
        /// Set default resolver of MessagePackSerializer APIs.
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
            if (resolver == null) resolver = DefaultResolver;
            var buffer = SerializeCore(obj, resolver);

            return MessagePackBinary.FastCloneWithResize(buffer.Array, buffer.Count);
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
            if (resolver == null) resolver = DefaultResolver;
            var buffer = SerializeCore(obj, resolver);

            stream.Write(buffer.Array, 0, buffer.Count);
        }

        static ArraySegment<byte> SerializeCore<T>(T obj, IFormatterResolver resolver)
        {
            var serializedData = MessagePackSerializer.SerializeUnsafe(obj, resolver);

            if (serializedData.Count < NotCompressionSize)
            {
                return serializedData;
            }
            else
            {
                var offset = 0;
                var buffer = InternalMemoryPool.GetBuffer();
                var maxOutCount = global::LZ4.LZ4Codec.MaximumOutputLength(serializedData.Count);
                if (buffer.Length + 18 + 5 < maxOutCount) // (max ext header size + fixed length size)
                {
                    buffer = new byte[maxOutCount];
                }

                // write ext header
                offset += MessagePackBinary.WriteExtensionFormatHeader(ref buffer, offset, (sbyte)ExtensionTypeCode, serializedData.Count + 5);

                // write length(always 5 bytes)
                offset += MessagePackBinary.WriteInt32ForceInt32Block(ref buffer, offset, serializedData.Count);

                // write body
                var lz4Length = global::LZ4.LZ4Codec.Encode(serializedData.Array, serializedData.Offset, serializedData.Count, buffer, offset, buffer.Length - offset);

                return new ArraySegment<byte>(buffer, 0, lz4Length + offset);
            }
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            return Deserialize<T>(bytes, defaultResolver);
        }

        public static T Deserialize<T>(byte[] bytes, IFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;

            return DeserializeCore<T>(new ArraySegment<byte>(bytes, 0, bytes.Length), resolver);
        }

        public static T Deserialize<T>(Stream stream)
        {
            return Deserialize<T>(stream, defaultResolver);
        }

        public static T Deserialize<T>(Stream stream, IFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;

            var buffer = InternalMemoryPool.GetBuffer();

            var len = FillFromStream(stream, ref buffer);

            // note, current lz4impl needs to fit input byte[]...
            var newBytes = MessagePackBinary.FastCloneWithResize(buffer, len);
            return DeserializeCore<T>(new ArraySegment<byte>(newBytes, 0, newBytes.Length), resolver);
        }

        static T DeserializeCore<T>(ArraySegment<byte> bytes, IFormatterResolver resolver)
        {
            var formatter = resolver.GetFormatterWithVerify<T>();

            int readSize;
            if (MessagePackBinary.GetMessagePackType(bytes.Array, 0) == MessagePackType.Extension)
            {
                var header = MessagePackBinary.ReadExtensionFormatHeader(bytes.Array, bytes.Offset, out readSize);
                if (header.TypeCode == ExtensionTypeCode)
                {
                    // decode lz4
                    var offset = bytes.Offset + readSize;
                    var length = MessagePackBinary.ReadInt32(bytes.Array, offset, out readSize);
                    offset += readSize;

                    var buffer = InternalMemoryPool.GetBuffer();
                    if (buffer.Length < length)
                    {
                        buffer = new byte[length];
                    }

                    // LZ4 Decode
                    global::LZ4.LZ4Codec.Decode(bytes.Array, offset, bytes.Count - offset, buffer, 0, length, true);

                    return formatter.Deserialize(buffer, 0, resolver, out readSize);
                }
            }

            return formatter.Deserialize(bytes.Array, bytes.Offset, resolver, out readSize);
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
    }

    internal static class InternalMemoryPool
    {
        [ThreadStatic]
        static byte[] buffer = null;

        public static byte[] GetBuffer()
        {
            if (buffer == null)
            {
                buffer = new byte[65536];
            }
            return buffer;
        }
    }
}


#endif