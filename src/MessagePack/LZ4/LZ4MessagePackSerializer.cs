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

        public static byte[] ToLZ4Binary(ArraySegment<byte> messagePackBinary)
        {
            var buffer = ToLZ4BinaryCore(messagePackBinary);
            return MessagePackBinary.FastCloneWithResize(buffer.Array, buffer.Count);
        }

        static ArraySegment<byte> SerializeCore<T>(T obj, IFormatterResolver resolver)
        {
            var serializedData = MessagePackSerializer.SerializeUnsafe(obj, resolver);
            return ToLZ4BinaryCore(serializedData);
        }

        static ArraySegment<byte> ToLZ4BinaryCore(ArraySegment<byte> serializedData)
        {
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
            return DeserializeCore<T>(new ArraySegment<byte>(bytes, 0, bytes.Length), resolver);
        }

        public static T Deserialize<T>(ArraySegment<byte> bytes)
        {
            return DeserializeCore<T>(bytes, null);
        }

        public static T Deserialize<T>(ArraySegment<byte> bytes, IFormatterResolver resolver)
        {
            return DeserializeCore<T>(bytes, resolver);
        }

        public static T Deserialize<T>(Stream stream)
        {
            return Deserialize<T>(stream, null);
        }

        public static T Deserialize<T>(Stream stream, IFormatterResolver resolver)
        {
            return Deserialize<T>(stream, resolver, false);
        }

        public static T Deserialize<T>(Stream stream, bool readStrict)
        {
            return Deserialize<T>(stream, MessagePackSerializer.DefaultResolver, readStrict);
        }

        public static T Deserialize<T>(Stream stream, IFormatterResolver resolver, bool readStrict)
        {
            if (!readStrict)
            {
                var buffer = MessagePack.Internal.InternalMemoryPool.GetBuffer(); // use MessagePackSerializer.Pool!
                var len = FillFromStream(stream, ref buffer);
                return DeserializeCore<T>(new ArraySegment<byte>(buffer, 0, len), resolver);
            }
            else
            {
                int blockSize;
                var bytes = MessagePackBinary.ReadMessageBlockFromStreamUnsafe(stream, false, out blockSize);
                return DeserializeCore<T>(new ArraySegment<byte>(bytes, 0, blockSize), resolver);
            }
        }

        public static byte[] Decode(Stream stream, bool readStrict = false)
        {
            if (!readStrict)
            {
                var buffer = MessagePack.Internal.InternalMemoryPool.GetBuffer(); // use MessagePackSerializer.Pool!
                var len = FillFromStream(stream, ref buffer);
                return Decode(new ArraySegment<byte>(buffer, 0, len));
            }
            else
            {
                int blockSize;
                var bytes = MessagePackBinary.ReadMessageBlockFromStreamUnsafe(stream, false, out blockSize);
                return Decode(new ArraySegment<byte>(bytes, 0, blockSize));
            }
        }

        public static byte[] Decode(byte[] bytes)
        {
            return Decode(new ArraySegment<byte>(bytes, 0, bytes.Length));
        }

        public static byte[] Decode(ArraySegment<byte> bytes)
        {
            int readSize;
            if (MessagePackBinary.GetMessagePackType(bytes.Array, bytes.Offset) == MessagePackType.Extension)
            {
                var header = MessagePackBinary.ReadExtensionFormatHeader(bytes.Array, bytes.Offset, out readSize);
                if (header.TypeCode == ExtensionTypeCode)
                {
                    // decode lz4
                    var offset = bytes.Offset + readSize;
                    var length = MessagePackBinary.ReadInt32(bytes.Array, offset, out readSize);
                    offset += readSize;

                    var buffer = new byte[length]; // use new buffer.

                    // LZ4 Decode
                    var len = bytes.Count + bytes.Offset - offset;
                    LZ4Codec.Decode(bytes.Array, offset, len, buffer, 0, length);

                    return buffer;
                }
            }

            if (bytes.Offset == 0 && bytes.Array.Length == bytes.Count)
            {
                // return same reference
                return bytes.Array;
            }
            else
            {
                var result = new byte[bytes.Count];
                Buffer.BlockCopy(bytes.Array, bytes.Offset, result, 0, result.Length);
                return result;
            }
        }


        /// <summary>
        /// Get the war memory pool byte[]. The result can not share across thread and can not hold and can not call LZ4Deserialize before use it.
        /// </summary>
        public static byte[] DecodeUnsafe(byte[] bytes)
        {
            return DecodeUnsafe(new ArraySegment<byte>(bytes, 0, bytes.Length));
        }

        /// <summary>
        /// Get the war memory pool byte[]. The result can not share across thread and can not hold and can not call LZ4Deserialize before use it.
        /// </summary>
        public static byte[] DecodeUnsafe(ArraySegment<byte> bytes)
        {
            int readSize;
            if (MessagePackBinary.GetMessagePackType(bytes.Array, bytes.Offset) == MessagePackType.Extension)
            {
                var header = MessagePackBinary.ReadExtensionFormatHeader(bytes.Array, bytes.Offset, out readSize);
                if (header.TypeCode == ExtensionTypeCode)
                {
                    // decode lz4
                    var offset = bytes.Offset + readSize;
                    var length = MessagePackBinary.ReadInt32(bytes.Array, offset, out readSize);
                    offset += readSize;

                    var buffer = LZ4MemoryPool.GetBuffer(); // use LZ4 Pool(Unsafe)
                    if (buffer.Length < length)
                    {
                        buffer = new byte[length];
                    }

                    // LZ4 Decode
                    var len = bytes.Count + bytes.Offset - offset;
                    LZ4Codec.Decode(bytes.Array, offset, len, buffer, 0, length);

                    return buffer; // return pooled bytes.
                }
            }

            if (bytes.Offset == 0 && bytes.Array.Length == bytes.Count)
            {
                // return same reference
                return bytes.Array;
            }
            else
            {
                var result = new byte[bytes.Count];
                Buffer.BlockCopy(bytes.Array, bytes.Offset, result, 0, result.Length);
                return result;
            }
        }

        static T DeserializeCore<T>(ArraySegment<byte> bytes, IFormatterResolver resolver)
        {
            if (resolver == null) resolver = MessagePackSerializer.DefaultResolver;
            var formatter = resolver.GetFormatterWithVerify<T>();

            int readSize;
            if (MessagePackBinary.GetMessagePackType(bytes.Array, bytes.Offset) == MessagePackType.Extension)
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
                    var len = bytes.Count + bytes.Offset - offset;
                    LZ4Codec.Decode(bytes.Array, offset, len, buffer, 0, length);

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