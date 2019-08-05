// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MessagePack.LZ4;
using Nerdbank.Streams;

namespace MessagePack
{
    /// <summary>
    /// High-Level API of MessagePack for C#.
    /// </summary>
    public static partial class MessagePackSerializer
    {
        public const sbyte LZ4ExtensionTypeCode = 99;

        private const int LZ4NotCompressionSize = 64;

        /// <summary>
        /// The default set of options to run with.
        /// </summary>
        public static readonly MessagePackSerializerOptions DefaultOptions = MessagePackSerializerOptions.Standard;

        /// <summary>
        /// A thread-safe pool of reusable <see cref="Sequence{T}"/> objects.
        /// </summary>
        private static readonly SequencePool ReusableSequenceWithMinSize = new SequencePool(Environment.ProcessorCount);

        /// <summary>
        /// A thread-local, recyclable array that may be used for short bursts of code.
        /// </summary>
        [ThreadStatic]
        private static byte[] scratchArray;

        /// <summary>
        /// Serializes a given value with the specified buffer writer.
        /// </summary>
        /// <param name="writer">The buffer writer to serialize with.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        public static void Serialize<T>(IBufferWriter<byte> writer, T value, MessagePackSerializerOptions options = null)
        {
            var fastWriter = new MessagePackWriter(writer);
            Serialize(ref fastWriter, value, options);
            fastWriter.Flush();
        }

        /// <summary>
        /// Serializes a given value with the specified buffer writer.
        /// </summary>
        /// <param name="writer">The buffer writer to serialize with.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        public static void Serialize<T>(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options = null)
        {
            options = options ?? DefaultOptions;
            bool originalOldSpecValue = writer.OldSpec;
            if (options.OldSpec.HasValue)
            {
                writer.OldSpec = options.OldSpec.Value;
            }

            try
            {
                if (options.UseLZ4Compression)
                {
                    using (var scratch = new Nerdbank.Streams.Sequence<byte>())
                    {
                        MessagePackWriter scratchWriter = writer.Clone(scratch);
                        options.Resolver.GetFormatterWithVerify<T>().Serialize(ref scratchWriter, value, options);
                        scratchWriter.Flush();
                        ToLZ4BinaryCore(scratch.AsReadOnlySequence, ref writer);
                    }
                }
                else
                {
                    options.Resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value, options);
                }
            }
            finally
            {
                writer.OldSpec = originalOldSpecValue;
            }
        }

        /// <summary>
        /// Serializes a given value with the specified buffer writer.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <returns>A byte array with the serialized value.</returns>
        public static byte[] Serialize<T>(T value, MessagePackSerializerOptions options = null)
        {
            byte[] array = scratchArray;
            if (array == null)
            {
                scratchArray = array = new byte[65536];
            }

            var msgpackWriter = new MessagePackWriter(ReusableSequenceWithMinSize, array);
            Serialize(ref msgpackWriter, value, options);
            return msgpackWriter.FlushAndGetArray();
        }

        /// <summary>
        /// Serializes a given value to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to serialize to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        public static void Serialize<T>(Stream stream, T value, MessagePackSerializerOptions options = null)
        {
            using (SequencePool.Rental sequenceRental = ReusableSequenceWithMinSize.Rent())
            {
                Serialize<T>(sequenceRental.Value, value, options);
                foreach (ReadOnlyMemory<byte> segment in sequenceRental.Value.AsReadOnlySequence)
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
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task that completes with the result of the async serialization operation.</returns>
        public static async Task SerializeAsync<T>(Stream stream, T value, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            System.IO.Pipelines.PipeWriter writer = stream.UseStrictPipeWriter();
            Serialize(writer, value, options);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="byteSequence">The sequence to deserialize from.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <returns>The deserialized value.</returns>
        public static T Deserialize<T>(in ReadOnlySequence<byte> byteSequence, MessagePackSerializerOptions options = null)
        {
            var reader = new MessagePackReader(byteSequence);
            return Deserialize<T>(ref reader, options);
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="reader">The reader to deserialize from.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <returns>The deserialized value.</returns>
        public static T Deserialize<T>(ref MessagePackReader reader, MessagePackSerializerOptions options = null)
        {
            options = options ?? DefaultOptions;
            if (options.UseLZ4Compression)
            {
                using (var msgPackUncompressed = new Nerdbank.Streams.Sequence<byte>())
                {
                    if (TryDecompress(ref reader, msgPackUncompressed))
                    {
                        MessagePackReader uncompressedReader = reader.Clone(msgPackUncompressed.AsReadOnlySequence);
                        return options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref uncompressedReader, options);
                    }
                    else
                    {
                        return options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);
                    }
                }
            }
            else
            {
                return options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);
            }
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="buffer">The buffer to deserialize from.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <returns>The deserialized value.</returns>
        public static T Deserialize<T>(ReadOnlyMemory<byte> buffer, MessagePackSerializerOptions options = null)
        {
            var reader = new MessagePackReader(buffer);
            return Deserialize<T>(ref reader, options);
        }

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="buffer">The memory to deserialize from.</param>
        /// <param name="bytesRead">The number of bytes read.</param>
        /// <returns>The deserialized value.</returns>
        public static T Deserialize<T>(ReadOnlyMemory<byte> buffer, out int bytesRead) => Deserialize<T>(buffer, options: null, out bytesRead);

        /// <summary>
        /// Deserializes a value of a given type from a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="buffer">The memory to deserialize from.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="bytesRead">The number of bytes read.</param>
        /// <returns>The deserialized value.</returns>
        public static T Deserialize<T>(ReadOnlyMemory<byte> buffer, MessagePackSerializerOptions options, out int bytesRead)
        {
            var reader = new MessagePackReader(buffer);
            T result = Deserialize<T>(ref reader, options);
            bytesRead = buffer.Slice(0, (int)reader.Consumed).Length;
            return result;
        }

        /// <summary>
        /// Deserializes the entire content of a <see cref="Stream"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <returns>The deserialized value.</returns>
        public static T Deserialize<T>(Stream stream, MessagePackSerializerOptions options = null)
        {
            if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> streamBuffer))
            {
                return Deserialize<T>(streamBuffer, options);
            }

            using (var sequence = new Sequence<byte>())
            {
                int bytesRead;
                do
                {
                    Span<byte> span = sequence.GetSpan(stream.CanSeek ? (int)(stream.Length - stream.Position) : 0);
                    bytesRead = stream.Read(span);
                    sequence.Advance(bytesRead);
                }
                while (bytesRead > 0);

                return Deserialize<T>(sequence.AsReadOnlySequence, options);
            }
        }

        /// <summary>
        /// Deserializes the entire content of a <see cref="Stream"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to deserialize.</typeparam>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <param name="options">The options. Use <c>null</c> to use default options.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The deserialized value.</returns>
        public static async ValueTask<T> DeserializeAsync<T>(Stream stream, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> streamBuffer))
            {
                return Deserialize<T>(streamBuffer, options);
            }

            using (var sequence = new Sequence<byte>())
            {
                int bytesRead;
                do
                {
                    Memory<byte> memory = sequence.GetMemory(stream.CanSeek ? (int)(stream.Length - stream.Position) : 0);
                    bytesRead = await stream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
                    sequence.Advance(bytesRead);
                }
                while (bytesRead > 0);

                return Deserialize<T>(sequence.AsReadOnlySequence, options);
            }
        }

        private delegate int LZ4Transform(ReadOnlySpan<byte> input, Span<byte> output);

        /// <summary>
        /// Performs LZ4 compression or decompression.
        /// </summary>
        /// <param name="input">The input for the operation.</param>
        /// <param name="output">The buffer to write the result of the operation.</param>
        /// <param name="lz4Operation">The LZ4 codec transformation.</param>
        /// <returns>The number of bytes written to the <paramref name="output"/>.</returns>
        private static int LZ4Operation(in ReadOnlySequence<byte> input, Span<byte> output, LZ4Transform lz4Operation)
        {
            ReadOnlySpan<byte> inputSpan;
            byte[] rentedInputArray = null;
            if (input.IsSingleSegment)
            {
                inputSpan = input.First.Span;
            }
            else
            {
                rentedInputArray = ArrayPool<byte>.Shared.Rent((int)input.Length);
                input.CopyTo(rentedInputArray);
                inputSpan = rentedInputArray.AsSpan(0, (int)input.Length);
            }

            try
            {
                return lz4Operation(inputSpan, output);
            }
            finally
            {
                if (rentedInputArray != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedInputArray);
                }
            }
        }

        private static bool TryDecompress(ref MessagePackReader reader, IBufferWriter<byte> writer)
        {
            if (!reader.End && reader.NextMessagePackType == MessagePackType.Extension)
            {
                MessagePackReader peekReader = reader.CreatePeekReader();
                ExtensionHeader header = peekReader.ReadExtensionFormatHeader();
                if (header.TypeCode == LZ4ExtensionTypeCode)
                {
                    // Read the extension using the original reader, so we "consume" it.
                    ExtensionResult extension = reader.ReadExtensionFormat();
                    var extReader = new MessagePackReader(extension.Data);

                    // The first part of the extension payload is a MessagePack-encoded Int32 that
                    // tells us the length the data will be AFTER decompression.
                    int uncompressedLength = extReader.ReadInt32();

                    // The rest of the payload is the compressed data itself.
                    ReadOnlySequence<byte> compressedData = extReader.Sequence.Slice(extReader.Position);

                    Span<byte> uncompressedSpan = writer.GetSpan(uncompressedLength).Slice(0, uncompressedLength);
                    int actualUncompressedLength = LZ4Operation(compressedData, uncompressedSpan, LZ4Codec.Decode);
                    Debug.Assert(actualUncompressedLength == uncompressedLength, "Unexpected length of uncompressed data.");
                    writer.Advance(actualUncompressedLength);
                    return true;
                }
            }

            return false;
        }

        private static void ToLZ4BinaryCore(in ReadOnlySequence<byte> msgpackUncompressedData, ref MessagePackWriter writer)
        {
            if (msgpackUncompressedData.Length < LZ4NotCompressionSize)
            {
                writer.WriteRaw(msgpackUncompressedData);
            }
            else
            {
                var maxCompressedLength = LZ4Codec.MaximumOutputLength((int)msgpackUncompressedData.Length);
                var lz4Span = ArrayPool<byte>.Shared.Rent(maxCompressedLength);
                try
                {
                    int lz4Length = LZ4Operation(msgpackUncompressedData, lz4Span, LZ4Codec.Encode);

                    const int LengthOfUncompressedDataSizeHeader = 5;
                    writer.WriteExtensionFormatHeader(new ExtensionHeader(LZ4ExtensionTypeCode, LengthOfUncompressedDataSizeHeader + (uint)lz4Length));
                    writer.WriteInt32((int)msgpackUncompressedData.Length);
                    writer.WriteRaw(lz4Span.AsSpan(0, lz4Length));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(lz4Span);
                }
            }
        }
    }
}
