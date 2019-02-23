using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MessagePack.Formatters;
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
        public override void Serialize<T>(ref MessagePackWriter writer, T value, IFormatterResolver resolver = null)
        {
            var scratchWriter = writer.Clone();
            base.Serialize(ref scratchWriter, value, resolver);
            scratchWriter.Flush();
            ToLZ4BinaryCore(scratchWriter.WrittenBytes, ref writer);
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

        private static void ToLZ4BinaryCore(ReadOnlySequence<byte> msgpackUncompressedData, ref MessagePackWriter writer)
        {
            if (msgpackUncompressedData.Length < NotCompressionSize)
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
                    writer.WriteExtensionFormatHeader(new ExtensionHeader(ExtensionTypeCode, LengthOfUncompressedDataSizeHeader + (uint)lz4Length));
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
