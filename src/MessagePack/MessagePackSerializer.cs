using MessagePack.Internal;
using System;
using System.IO;

namespace MessagePack
{
    /// <summary>
    /// High-Level API of MessagePack for C#.
    /// </summary>
    public static partial class MessagePackSerializer
    {
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
            var formatter = resolver.GetFormatterWithVerify<T>();

            var buffer = InternalMemoryPool.GetBuffer();

            var len = formatter.Serialize(ref buffer, 0, obj, resolver);

            // do not return MemoryPool.Buffer.
            return MessagePackBinary.FastCloneWithResize(buffer, len);
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
            if (resolver == null) resolver = DefaultResolver;
            var formatter = resolver.GetFormatterWithVerify<T>();

            var buffer = InternalMemoryPool.GetBuffer();

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
            if (resolver == null) resolver = DefaultResolver;
            var formatter = resolver.GetFormatterWithVerify<T>();

            var buffer = InternalMemoryPool.GetBuffer();

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
            if (resolver == null) resolver = DefaultResolver;
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
            if (resolver == null) resolver = DefaultResolver;
            var formatter = resolver.GetFormatterWithVerify<T>();

#if NETSTANDARD1_4

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
#endif

            // no else.
            {
                var buffer = InternalMemoryPool.GetBuffer();

                FillFromStream(stream, ref buffer);

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
    }
}

namespace MessagePack.Internal
{
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