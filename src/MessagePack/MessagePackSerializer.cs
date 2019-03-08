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
        /// Serializes a given value with the specified buffer writer.
        /// </summary>
        /// <param name="writer">The buffer writer to serialize with.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="resolver">The resolver to use during deserialization. Use <c>null</c> to use the <see cref="DefaultResolver"/>.</param>
        public void Serialize<T>(IBufferWriter<byte> writer, T value, IFormatterResolver resolver = null)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (resolver == null)
            {
                resolver = this.DefaultResolver;
            }

            var fastWriter = new MessagePackWriter(writer);
            this.Serialize(ref fastWriter, value, resolver);
            fastWriter.Flush();
        }

        /// <summary>
        /// Serializes a given value with the specified buffer writer.
        /// </summary>
        /// <param name="writer">The buffer writer to serialize with.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="resolver">The resolver to use during deserialization. Use <c>null</c> to use the <see cref="DefaultResolver"/>.</param>
        public virtual void Serialize<T>(ref MessagePackWriter writer, T value, IFormatterResolver resolver = null)
        {
            resolver = resolver ?? this.DefaultResolver;
            resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value, resolver);
        }

        /// <summary>
        /// Serializes a given value with the specified buffer writer.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="resolver">The resolver to use during deserialization. Use <c>null</c> to use the <see cref="DefaultResolver"/>.</param>
        /// <returns>A byte array with the serialized value.</returns>
        public byte[] Serialize<T>(T value, IFormatterResolver resolver = null)
        {
            using (var sequence = new Sequence<byte>(MemoryPoolWithMinSize.Instance))
            {
                this.Serialize(sequence, value, resolver);
                return sequence.AsReadOnlySequence.ToArray();
            }
        }

        /// <summary>
        /// Serializes a given value to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to serialize to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="resolver">The resolver to use during deserialization. Use <c>null</c> to use the <see cref="DefaultResolver"/>.</param>
        public void Serialize<T>(Stream stream, T value, IFormatterResolver resolver = null)
        {
            using (var sequence = new Sequence<byte>(MemoryPoolWithMinSize.Instance))
            {
                this.Serialize<T>(sequence, value, resolver);
                foreach (var segment in sequence.AsReadOnlySequence)
                {
                    stream.Write(segment.Span);
                }
            }
        }

        /// <summary>
        /// Serializes a given value to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to serialize to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="resolver">The resolver to use during deserialization. Use <c>null</c> to use the <see cref="DefaultResolver"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task that completes with the result of the async serialization operation.</returns>
        public async ValueTask SerializeAsync<T>(Stream stream, T value, IFormatterResolver resolver = null, CancellationToken cancellationToken = default)
        {
            System.IO.Pipelines.PipeWriter writer = stream.UseStrictPipeWriter();
            this.Serialize(writer, value, resolver);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

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
