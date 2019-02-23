using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.LZ4;
using Microsoft;

namespace MessagePack
{
    /// <summary>
    /// LZ4 Compressed special serializer.
    /// </summary>
    /// <remarks>
    /// The spec for this is:
    ///  Extension header: (typecode = 99)
    ///  32-bit integer with length of *uncompressed* data (as a MessagePack Int32 entity)
    ///  compressed data  (raw -- not as a raw/bytes MessagePack entity)
    /// </remarks>
    public partial class LZ4MessagePackSerializer : MessagePackSerializer
    {
        public const sbyte ExtensionTypeCode = 99;

        public const int NotCompressionSize = 64;

        /// <summary>
        /// Initializes a new instance of the <see cref="LZ4MessagePackSerializer"/> class
        /// initialized with the <see cref="Resolvers.StandardResolver"/>.
        /// </summary>
        public LZ4MessagePackSerializer()
            : this(Resolvers.StandardResolver.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LZ4MessagePackSerializer"/> class
        /// </summary>
        /// <param name="defaultResolver">The resolver to use.</param>
        public LZ4MessagePackSerializer(IFormatterResolver defaultResolver)
            : base(defaultResolver)
        {
        }

        /// <summary>
        /// Serialize to binary with default resolver.
        /// </summary>
        public override byte[] Serialize<T>(T obj, IFormatterResolver resolver)
        {
            var buffer = SerializeCore(obj, resolver);

            return MessagePackBinary.FastCloneWithResize(buffer.Array, buffer.Count);
        }

        /// <summary>
        /// Serialize to stream.
        /// </summary>
        public override void Serialize<T>(Stream stream, T obj, IFormatterResolver resolver)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var buffer = SerializeCore(obj, resolver);

            stream.Write(buffer.Array, 0, buffer.Count);
        }

        public int SerializeToBlock<T>(ref byte[] bytes, int offset, T obj, IFormatterResolver resolver)
        {
            var serializedData = this.SerializeUnsafe(obj, resolver);

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
                var lz4Length = LZ4Codec.Encode(serializedData, bytes.AsSpan(offset, bytes.Length - offset));

                // write extension header(always 6 bytes)
                extHeaderOffset += MessagePackBinary.WriteExtensionFormatHeaderForceExt32Block(ref bytes, extHeaderOffset, ExtensionTypeCode, lz4Length + 5);

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

        private ArraySegment<byte> SerializeCore<T>(T obj, IFormatterResolver resolver)
        {
            var serializedData = this.SerializeUnsafe(obj, resolver);
            return ToLZ4BinaryCore(serializedData);
        }

        private static ArraySegment<byte> ToLZ4BinaryCore(ArraySegment<byte> serializedData)
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
                var lz4Length = LZ4Codec.Encode(serializedData, buffer.AsSpan(offset, buffer.Length - offset));

                // write extension header(always 6 bytes)
                extHeaderOffset += MessagePackBinary.WriteExtensionFormatHeaderForceExt32Block(ref buffer, extHeaderOffset, ExtensionTypeCode, lz4Length + 5);

                // write length(always 5 bytes)
                MessagePackBinary.WriteInt32ForceInt32Block(ref buffer, extHeaderOffset, serializedData.Count);

                return new ArraySegment<byte>(buffer, 0, 6 + 5 + lz4Length);
            }
        }

        public override T Deserialize<T>(ref MessagePackReader reader, IFormatterResolver resolver = null)
        {
            using (var msgPackUncompressed = new Nerdbank.Streams.Sequence<byte>())
            {
                if (TryDecompress(ref reader, msgPackUncompressed))
                {
                    var uncompressedReader = reader.Clone(msgPackUncompressed.AsReadOnlySequence);
                    return base.Deserialize<T>(ref uncompressedReader, resolver);
                }
                else
                {
                    return base.Deserialize<T>(ref reader, resolver);
                }
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
        private static int LZ4Operation(ReadOnlySequence<byte> input, Span<byte> output, LZ4Transform lz4Operation)
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
                var peekReader = reader.CreatePeekReader();
                var header = peekReader.ReadExtensionFormatHeader();
                if (header.TypeCode == ExtensionTypeCode)
                {
                    // Read the extension using the original reader, so we "consume" it.
                    var extension = reader.ReadExtensionFormat();
                    var extReader = new MessagePackReader(extension.Data);

                    // The first part of the extension payload is a MessagePack-encoded Int32 that
                    // tells us the length the data will be AFTER decompression.
                    int uncompressedLength = extReader.ReadInt32();

                    // The rest of the payload is the compressed data itself.
                    var compressedData = extReader.Sequence.Slice(extReader.Position);

                    var uncompressedSpan = writer.GetSpan(uncompressedLength).Slice(0, uncompressedLength);
                    int actualUncompressedLength = LZ4Operation(compressedData, uncompressedSpan, LZ4Codec.Decode);
                    Debug.Assert(actualUncompressedLength == uncompressedLength);
                    writer.Advance(actualUncompressedLength);
                    return true;
                }
            }

            return false;
        }
    }
}

namespace MessagePack.Internal
{
    internal static class LZ4MemoryPool
    {
        [ThreadStatic]
        private static byte[] lz4buffer = null;

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
