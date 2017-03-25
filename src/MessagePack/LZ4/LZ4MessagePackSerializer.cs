using MessagePack.Internal;
using System;
using System.IO;
using MessagePack.LZ4;

namespace MessagePack
{
    /// <summary>
    /// LZ4 Compressed special serializer.
    /// </summary>
    public static partial class LZ4MessagePackSerializer
    {
        public const sbyte ExtensionTypeCode = 99;

        public const int NotCompressionSize = 64;

        /// <summary>
        /// Serialize to binary with default resolver.
        /// </summary>
        public static byte[] Serialize<T>(T obj)
        {
            return Serialize(obj, null);
        }

        /// <summary>
        /// Serialize to binary with specified resolver.
        /// </summary>
        public static byte[] Serialize<T>(T obj, IFormatterResolver resolver)
        {
            if (resolver == null) resolver = MessagePackSerializer.DefaultResolver;
            var buffer = SerializeCore(obj, resolver);

            return MessagePackBinary.FastCloneWithResize(buffer.Array, buffer.Count);
        }

        /// <summary>
        /// Serialize to stream.
        /// </summary>
        public static void Serialize<T>(Stream stream, T obj)
        {
            Serialize(stream, obj, null);
        }

        /// <summary>
        /// Serialize to stream with specified resolver.
        /// </summary>
        public static void Serialize<T>(Stream stream, T obj, IFormatterResolver resolver)
        {
            if (resolver == null) resolver = MessagePackSerializer.DefaultResolver;
            var buffer = SerializeCore(obj, resolver);

            stream.Write(buffer.Array, 0, buffer.Count);
        }

        public static int SerializeToBlock<T>(ref byte[] bytes, int offset, T obj, IFormatterResolver resolver)
        {
            var serializedData = MessagePackSerializer.SerializeUnsafe(obj, resolver);

            if (serializedData.Count < NotCompressionSize)
            {
                // can't write direct, shoganai...
                MessagePackBinary.EnsureCapacity(ref bytes, offset, serializedData.Count);
                Buffer.BlockCopy(serializedData.Array, serializedData.Offset, bytes, offset, serializedData.Count);
                return serializedData.Count;
            }
            else
            {
                var maxOutCount = LZ4Codec.MaximumOutputLength(serializedData.Count);

                MessagePackBinary.EnsureCapacity(ref bytes, offset, 6 + 5 + maxOutCount); // (ext header size + fixed length size)

                // acquire ext header position
                var extHeaderOffset = offset;
                offset += (6 + 5);

                // write body
                var lz4Length = LZ4Codec.Encode(serializedData.Array, serializedData.Offset, serializedData.Count, bytes, offset, bytes.Length - offset);

                // write extension header(always 6 bytes)
                extHeaderOffset += MessagePackBinary.WriteExtensionFormatHeaderForceExt32Block(ref bytes, extHeaderOffset, (sbyte)ExtensionTypeCode, lz4Length + 5);

                // write length(always 5 bytes)
                MessagePackBinary.WriteInt32ForceInt32Block(ref bytes, extHeaderOffset, serializedData.Count);

                return 6 + 5 + lz4Length;
            }
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
                var buffer = LZ4MemoryPool.GetBuffer();
                var maxOutCount = LZ4Codec.MaximumOutputLength(serializedData.Count);
                if (buffer.Length + 6 + 5 < maxOutCount) // (ext header size + fixed length size)
                {
                    buffer = new byte[6 + 5 + maxOutCount];
                }

                // acquire ext header position
                var extHeaderOffset = offset;
                offset += (6 + 5);

                // write body
                var lz4Length = LZ4Codec.Encode(serializedData.Array, serializedData.Offset, serializedData.Count, buffer, offset, buffer.Length - offset);

                // write extension header(always 6 bytes)
                extHeaderOffset += MessagePackBinary.WriteExtensionFormatHeaderForceExt32Block(ref buffer, extHeaderOffset, (sbyte)ExtensionTypeCode, lz4Length + 5);

                // write length(always 5 bytes)
                MessagePackBinary.WriteInt32ForceInt32Block(ref buffer, extHeaderOffset, serializedData.Count);

                return new ArraySegment<byte>(buffer, 0, 6 + 5 + lz4Length);
            }
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            return Deserialize<T>(bytes, null);
        }

        public static T Deserialize<T>(byte[] bytes, IFormatterResolver resolver)
        {
            if (resolver == null) resolver = MessagePackSerializer.DefaultResolver;

            return DeserializeCore<T>(new ArraySegment<byte>(bytes, 0, bytes.Length), resolver);
        }

        public static T Deserialize<T>(Stream stream)
        {
            return Deserialize<T>(stream, null);
        }

        public static T Deserialize<T>(Stream stream, IFormatterResolver resolver)
        {
            if (resolver == null) resolver = MessagePackSerializer.DefaultResolver;

            var buffer = MessagePack.Internal.InternalMemoryPool.GetBuffer(); // use MessagePackSerializer.Pool!

            var len = FillFromStream(stream, ref buffer);
            return DeserializeCore<T>(new ArraySegment<byte>(buffer, 0, len), resolver);
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

                    var buffer = LZ4MemoryPool.GetBuffer(); // use LZ4 Pool
                    if (buffer.Length < length)
                    {
                        buffer = new byte[length];
                    }

                    // LZ4 Decode
                    LZ4Codec.Decode(bytes.Array, offset, bytes.Count - offset, buffer, 0, length);

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
}

namespace MessagePack.Internal
{
    internal static class LZ4MemoryPool
    {
        [ThreadStatic]
        static byte[] lz4buffer = null;

        public static byte[] GetBuffer()
        {
            if (lz4buffer == null)
            {
                lz4buffer = new byte[LZ4.LZ4Codec.MaximumOutputLength(65536)];
            }
            return lz4buffer;
        }
    }
}