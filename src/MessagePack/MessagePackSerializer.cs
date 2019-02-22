using MessagePack.Internal;
using System;
using System.IO;

namespace MessagePack
{
    /// <summary>
    /// High-Level API of MessagePack for C#.
    /// </summary>
    public partial class MessagePackSerializer
    {
        /// <summary>
        /// Backing field for the <see cref="DefaultResolver"/> property.
        /// </summary>
        private IFormatterResolver defaultResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackSerializer"/> class
        /// initialized with the <see cref="Resolvers.StandardResolver"/>.
        /// </summary>
        public MessagePackSerializer()
            : this(Resolvers.StandardResolver.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackSerializer"/> class
        /// </summary>
        /// <param name="defaultResolver">The resolver to use.</param>
        public MessagePackSerializer(IFormatterResolver defaultResolver)
        {
            this.defaultResolver = defaultResolver ?? throw new ArgumentNullException(nameof(defaultResolver));
        }

        /// <summary>
        /// Gets or sets the resolver to use when one is not explicitly specified.
        /// </summary>
        public IFormatterResolver DefaultResolver
        {
            get => this.defaultResolver;
            ////set => this.defaultResolver = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Serialize to binary.
        /// </summary>
        public byte[] Serialize<T>(T obj) => this.Serialize<T>(obj, this.DefaultResolver);

        /// <summary>
        /// Serialize to binary.
        /// </summary>
        public virtual byte[] Serialize<T>(T obj, IFormatterResolver resolver)
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
        protected ArraySegment<byte> SerializeUnsafe<T>(T obj, IFormatterResolver resolver)
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
        public void Serialize<T>(Stream stream, T obj) => this.Serialize<T>(stream, obj, DefaultResolver);

        /// <summary>
        /// Serialize to stream.
        /// </summary>
        public virtual void Serialize<T>(Stream stream, T obj, IFormatterResolver resolver)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (resolver == null) resolver = DefaultResolver;
            var formatter = resolver.GetFormatterWithVerify<T>();

            var buffer = InternalMemoryPool.GetBuffer();

            var len = formatter.Serialize(ref buffer, 0, obj, resolver);

            // do not need resize.
            stream.Write(buffer, 0, len);
        }

        /// <summary>
        /// Reflect of resolver.GetFormatterWithVerify[T].Serialize.
        /// </summary>
        public int Serialize<T>(ref byte[] bytes, int offset, T value, IFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;
            return resolver.GetFormatterWithVerify<T>().Serialize(ref bytes, offset, value, resolver);
        }

#if !UNITY

        /// <summary>
        /// Serialize to stream(async).
        /// </summary>
        public System.Threading.Tasks.Task SerializeAsync<T>(Stream stream, T obj) => SerializeAsync<T>(stream, obj, DefaultResolver);

        /// <summary>
        /// Serialize to stream(async).
        /// </summary>
        public async System.Threading.Tasks.Task SerializeAsync<T>(Stream stream, T obj, IFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;
            var formatter = resolver.GetFormatterWithVerify<T>();

            var rentBuffer = BufferPool.Default.Rent();
            try
            {
                var buffer = rentBuffer;
                var len = formatter.Serialize(ref buffer, 0, obj, resolver);

                // do not need resize.
                await stream.WriteAsync(buffer, 0, len).ConfigureAwait(false);
            }
            finally
            {
                BufferPool.Default.Return(rentBuffer);
            }
        }

#endif

        public T Deserialize<T>(byte[] bytes) => Deserialize<T>(bytes, DefaultResolver);

        public T Deserialize<T>(byte[] bytes, IFormatterResolver resolver) => Deserialize<T>(new ArraySegment<byte>(bytes), resolver);

        public T Deserialize<T>(ArraySegment<byte> bytes) => Deserialize<T>(bytes, DefaultResolver);

        public T Deserialize<T>(ArraySegment<byte> bytes, IFormatterResolver resolver) => Deserialize<T>(bytes, resolver, out int readSize);

        public virtual T Deserialize<T>(ArraySegment<byte> bytes, IFormatterResolver resolver, out int readSize)
        {
            if (resolver == null) resolver = DefaultResolver;
            return resolver.GetFormatterWithVerify<T>().Deserialize(bytes.Array, bytes.Offset, resolver, out readSize);
        }

        public T Deserialize<T>(Stream stream) => Deserialize<T>(stream, DefaultResolver, readStrict: false);

        public T Deserialize<T>(Stream stream, IFormatterResolver resolver) => Deserialize<T>(stream, resolver, readStrict: false);

        public T Deserialize<T>(Stream stream, bool readStrict) => Deserialize<T>(stream, DefaultResolver, readStrict);

        public virtual T Deserialize<T>(Stream stream, IFormatterResolver resolver, bool readStrict)
        {
            if (resolver == null) resolver = DefaultResolver;
            var formatter = resolver.GetFormatterWithVerify<T>();

            if (!readStrict)
            {
#if NETSTANDARD && !NET45

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
            else
            {
                int _;
                var bytes = MessagePackBinary.ReadMessageBlockFromStreamUnsafe(stream, false, out _);
                int readSize;
                return formatter.Deserialize(bytes, 0, resolver, out readSize);
            }
        }

        /// <summary>
        /// Reflect of resolver.GetFormatterWithVerify[T].Deserialize.
        /// </summary>
        public T Deserialize<T>(byte[] bytes, int offset, IFormatterResolver resolver, out int readSize) => Deserialize<T>(new ArraySegment<byte>(bytes, offset, bytes.Length - offset), resolver, out readSize);

#if !UNITY

        public System.Threading.Tasks.Task<T> DeserializeAsync<T>(Stream stream) => DeserializeAsync<T>(stream, DefaultResolver);

        // readStrict async read is too slow(many Task garbage) so I don't provide async option.

        public async System.Threading.Tasks.Task<T> DeserializeAsync<T>(Stream stream, IFormatterResolver resolver)
        {
            if (resolver == null) resolver = DefaultResolver;
            var rentBuffer = BufferPool.Default.Rent();
            var buf = rentBuffer;
            try
            {
                int length = 0;
                int read;
                while ((read = await stream.ReadAsync(buf, length, buf.Length - length).ConfigureAwait(false)) > 0)
                {
                    length += read;
                    if (length == buf.Length)
                    {
                        MessagePackBinary.FastResize(ref buf, length * 2);
                    }
                }

                return Deserialize<T>(buf, resolver);
            }
            finally
            {
                BufferPool.Default.Return(rentBuffer);
            }
        }

#endif

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