using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MessagePack.Formatters;
using MessagePack.Internal;
using Nerdbank.Streams;

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

            var rentBuffer = ArrayPool<byte>.Shared.Rent(65535);
            try
            {
                var buffer = rentBuffer;
                var len = formatter.Serialize(ref buffer, 0, obj, resolver);

                // do not need resize.
                await stream.WriteAsync(buffer, 0, len).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentBuffer);
            }
        }

#endif

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="byteSequence">The sequence to deserialize from.</param>
        /// <param name="resolver">The resolver to use during deserialization. Use <c>null</c> to use the <see cref="DefaultResolver"/>.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(ReadOnlySequence<byte> byteSequence, IFormatterResolver resolver = null)
        {
            var reader = new MessagePackReader(byteSequence);
            return this.Deserialize<T>(ref reader, resolver);
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="reader">The reader to deserialize from.</param>
        /// <param name="resolver">The resolver to use during deserialization. Use <c>null</c> to use the <see cref="DefaultResolver"/>.</param>
        /// <returns>The deserialized value.</returns>
        public virtual T Deserialize<T>(ref MessagePackReader reader, IFormatterResolver resolver = null)
        {
            resolver = resolver ?? this.DefaultResolver;
            return resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, resolver);
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="buffer">The buffer to deserialize from.</param>
        /// <param name="resolver">The resolver to use during deserialization. Use <c>null</c> to use the <see cref="DefaultResolver"/>.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(Memory<byte> buffer, IFormatterResolver resolver = null)
        {
            var reader = new MessagePackReader(new ReadOnlySequence<byte>(buffer));
            return this.Deserialize<T>(ref reader, resolver);
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="buffer">The array to deserialize from.</param>
        /// <param name="offset">The position in the array to start deserialization.</param>
        /// <param name="resolver">The resolver to use during deserialization. Use <c>null</c> to use the <see cref="DefaultResolver"/>.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(byte[] buffer, int offset = 0, IFormatterResolver resolver = null)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            var reader = new MessagePackReader(new ReadOnlySequence<byte>(buffer.AsMemory(offset)));
            return Deserialize<T>(ref reader, resolver);
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="buffer">The array to deserialize from.</param>
        /// <param name="offset">The position in the array to start deserialization.</param>
        /// <param name="resolver">The resolver to use during deserialization. Use <c>null</c> to use the <see cref="DefaultResolver"/>.</param>
        /// <param name="bytesRead">The number of bytes read.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(byte[] buffer, int offset, IFormatterResolver resolver, out int bytesRead)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            var sequence = new ReadOnlySequence<byte>(buffer.AsMemory(offset));
            var reader = new MessagePackReader(sequence);
            T result = Deserialize<T>(ref reader, resolver);
            bytesRead = (int)sequence.Slice(0, reader.Position).Length;
            return result;
        }

        /// <summary>
        /// Deserializes the entire content of a <see cref="Stream"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <param name="resolver">The resolver to use during deserialization. Use <c>null</c> to use the <see cref="DefaultResolver"/>.</param>
        /// <returns>The deserialized value.</returns>
        public T Deserialize<T>(Stream stream, IFormatterResolver resolver = null)
        {
            using (var sequence = new Sequence<byte>())
            {
                int bytesRead;
                do
                {
                    var span = sequence.GetSpan(stream.CanSeek ? (int)(stream.Length - stream.Position) : 0);
                    bytesRead = stream.Read(span);
                    sequence.Advance(bytesRead);
                } while (bytesRead > 0);

                return this.Deserialize<T>(sequence.AsReadOnlySequence, resolver);
            }
        }

        /// <summary>
        /// Deserializes the entire content of a <see cref="Stream"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <param name="resolver">The resolver to use during deserialization. Use <c>null</c> to use the <see cref="DefaultResolver"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The deserialized value.</returns>
        public async ValueTask<T> DeserializeAsync<T>(Stream stream, IFormatterResolver resolver = null, CancellationToken cancellationToken = default)
        {
            using (var sequence = new Sequence<byte>())
            {
                int bytesRead;
                do
                {
                    var memory = sequence.GetMemory(stream.CanSeek ? (int)(stream.Length - stream.Position) : 0);
                    bytesRead = await stream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
                    sequence.Advance(bytesRead);
                } while (bytesRead > 0);

                return this.Deserialize<T>(sequence.AsReadOnlySequence, resolver);
            }
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