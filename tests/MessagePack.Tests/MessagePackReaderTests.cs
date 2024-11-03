﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Nerdbank.Streams;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Tests
{
    public partial class MessagePackReaderTests
    {
        private const sbyte ByteNegativeValue = -3;
        private const byte BytePositiveValue = 3;
        private static readonly ReadOnlySequence<byte> StringEncodedAsFixStr = Encode((ref MessagePackWriter w) => w.Write("hi"));

        private readonly ITestOutputHelper logger;

#if !UNITY_2018_3_OR_NEWER

        public MessagePackReaderTests(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

#else
        public MessagePackReaderTests()
        {
            this.logger = new NullTestOutputHelper();
        }

#endif

        [Fact]
        public void ReadSingle_ReadIntegersOfVariousLengthsAndMagnitudes()
        {
            foreach ((System.Numerics.BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
                Assert.Equal((float)(double)value, new MessagePackReader(encoded).ReadSingle());
            }
        }

        [Fact]
        public void ReadSingle_CanReadDouble()
        {
            var reader = new MessagePackReader(Encode((ref MessagePackWriter w) => w.Write(1.23)));
            Assert.Equal(1.23f, reader.ReadSingle());
        }

        [Fact]
        public void ReadArrayHeader_MitigatesLargeAllocations()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.WriteArrayHeader(9999);
            writer.Flush();

            Assert.Throws<EndOfStreamException>(() =>
            {
                var reader = new MessagePackReader(sequence);
                reader.ReadArrayHeader();
            });
        }

        [Fact]
        public void TryReadArrayHeader()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            const int expectedCount = 100;
            writer.WriteArrayHeader(expectedCount);
            writer.Flush();

            var reader = new MessagePackReader(sequence.AsReadOnlySequence.Slice(0, sequence.Length - 1));
            Assert.False(reader.TryReadArrayHeader(out _));

            reader = new MessagePackReader(sequence);
            Assert.True(reader.TryReadArrayHeader(out int actualCount));
            Assert.Equal(expectedCount, actualCount);
        }

        [Fact]
        public void ReadMapHeader_MitigatesLargeAllocations()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.WriteMapHeader(9999);
            writer.Flush();

            Assert.Throws<EndOfStreamException>(() =>
            {
                var reader = new MessagePackReader(sequence);
                reader.ReadMapHeader();
            });
        }

        [Fact]
        public void TryReadMapHeader()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            const int expectedCount = 100;
            writer.WriteMapHeader(expectedCount);
            writer.Flush();

            var reader = new MessagePackReader(sequence.AsReadOnlySequence.Slice(0, sequence.Length - 1));
            Assert.False(reader.TryReadMapHeader(out _));

            reader = new MessagePackReader(sequence);
            Assert.True(reader.TryReadMapHeader(out int actualCount));
            Assert.Equal(expectedCount, actualCount);
        }

        [Fact]
        public void TryReadMapHeader_Ranges()
        {
            AssertCodeRange((ref MessagePackReader r) => r.TryReadMapHeader(out _), c => c is >= MessagePackCode.MinFixMap and <= MessagePackCode.MaxFixMap, c => c is MessagePackCode.Map16 or MessagePackCode.Map32);
        }

        [Fact]
        public void TryReadArrayHeader_Ranges()
        {
            AssertCodeRange((ref MessagePackReader r) => r.TryReadArrayHeader(out _), c => c is >= MessagePackCode.MinFixArray and <= MessagePackCode.MaxFixArray, c => c is MessagePackCode.Array16 or MessagePackCode.Array32);
        }

        [Fact]
        public void TryReadString_Ranges()
        {
            AssertCodeRange((ref MessagePackReader r) => r.TryReadStringSpan(out _), c => c is MessagePackCode.Nil, c => c is (>= MessagePackCode.MinFixStr and <= MessagePackCode.MaxFixStr) or MessagePackCode.Str8 or MessagePackCode.Str16 or MessagePackCode.Str32);
        }

        [Fact]
        public void TryReadInt_Ranges()
        {
            AssertCodeRange((ref MessagePackReader r) => r.ReadInt32(), c => c is (>= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt) or (>= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt), c => c is MessagePackCode.Int64 or MessagePackCode.Int32 or MessagePackCode.Int16 or MessagePackCode.Int8 or MessagePackCode.UInt64 or MessagePackCode.UInt32 or MessagePackCode.UInt16 or MessagePackCode.UInt8);
        }

        private delegate void RangeChecker(ref MessagePackReader reader);

        private void AssertCodeRange(RangeChecker predicate, Func<byte, bool> isOneByteRepresentation, Func<byte, bool> isIntroductoryByte)
        {
            bool mismatch = false;
            byte[] buffer = new byte[1];
            foreach (byte code in Enumerable.Range(byte.MinValue, byte.MaxValue))
            {
                try
                {
                    bool expectedOneByte = isOneByteRepresentation(code);
                    bool expectedLeadingByte = isIntroductoryByte(code);
                    if (expectedLeadingByte && expectedOneByte)
                    {
                        throw new Exception("Byte cannot be both a leading byte and a one-byte representation.");
                    }

                    bool expectedMatch = expectedOneByte || expectedLeadingByte;
                    buffer[0] = code;
                    MessagePackReader reader = new(buffer);
                    bool actual;
                    try
                    {
                        predicate(ref reader);
                        actual = true;
                    }
                    catch (MessagePackSerializationException)
                    {
                        actual = false;
                    }
                    catch (EndOfStreamException) when (expectedLeadingByte)
                    {
                        actual = true;
                    }

                    if (expectedMatch != actual)
                    {
                        this.logger.WriteLine($"Byte 0x{code:x2} was {actual} but was expected to be {expectedMatch}.");
                        mismatch = true;
                    }
                }
                catch (Exception ex)
                {
                    mismatch = true;
                    this.logger.WriteLine($"Byte 0x{code:x2} threw an exception: {ex}");
                }
            }

            if (mismatch)
            {
                throw new Exception("One or more byte values did not match the expected range.");
            }
        }

        [Fact]
        public void ReadExtensionFormatHeader_MitigatesLargeAllocations()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.WriteExtensionFormatHeader(new ExtensionHeader(3, 1));
            writer.WriteRaw(new byte[1]);
            writer.Flush();

            Assert.Throws<EndOfStreamException>(() =>
            {
                var truncatedReader = new MessagePackReader(sequence.AsReadOnlySequence.Slice(0, sequence.Length - 1));
                truncatedReader.ReadExtensionFormatHeader();
            });

            var reader = new MessagePackReader(sequence);
            reader.ReadExtensionFormatHeader();
        }

        [Fact]
        public void TryReadExtensionFormatHeader()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            var expectedExtensionHeader = new ExtensionHeader(4, 100);
            writer.WriteExtensionFormatHeader(expectedExtensionHeader);
            writer.Flush();

            var reader = new MessagePackReader(sequence.AsReadOnlySequence.Slice(0, sequence.Length - 1));
            Assert.False(reader.TryReadExtensionFormatHeader(out _));

            reader = new MessagePackReader(sequence);
            Assert.True(reader.TryReadExtensionFormatHeader(out ExtensionHeader actualExtensionHeader));
            Assert.Equal(expectedExtensionHeader, actualExtensionHeader);
        }

        [Fact]
        public void TryReadStringSpan_Fragmented()
        {
            var contiguousSequence = new Sequence<byte>();
            var writer = new MessagePackWriter(contiguousSequence);
            var expected = new byte[] { 0x1, 0x2, 0x3 };
            writer.WriteString(expected);
            writer.Flush();
            var fragmentedSequence = BuildSequence(
               contiguousSequence.AsReadOnlySequence.First.Slice(0, 2),
               contiguousSequence.AsReadOnlySequence.First.Slice(2));

            var reader = new MessagePackReader(fragmentedSequence);
            Assert.False(reader.TryReadStringSpan(out ReadOnlySpan<byte> span));
            Assert.Equal(0, span.Length);

            // After failing to read the span, a caller should still be able to read it as a sequence.
            var actualSequence = reader.ReadStringSequence();
            Assert.True(actualSequence.HasValue);
            Assert.False(actualSequence.Value.IsSingleSegment);
            Assert.Equal(new byte[] { 1, 2, 3 }, actualSequence.Value.ToArray());
        }

        [Fact]
        public void TryReadStringSpan_Contiguous()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            var expected = new byte[] { 0x1, 0x2, 0x3 };
            writer.WriteString(expected);
            writer.Flush();

            var reader = new MessagePackReader(sequence);
            Assert.True(reader.TryReadStringSpan(out ReadOnlySpan<byte> span));
            Assert.Equal(expected, span.ToArray());
            Assert.True(reader.End);
        }

        [Fact]
        public void TryReadStringSpan_Nil()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.WriteNil();
            writer.Flush();

            var reader = new MessagePackReader(sequence);
            Assert.False(reader.TryReadStringSpan(out ReadOnlySpan<byte> span));
            Assert.Equal(0, span.Length);
            Assert.Equal(sequence.AsReadOnlySequence.Start, reader.Position);
        }

        [Fact]
        public void ReadString_MultibyteChars()
        {
            var reader = new MessagePackReader(TestConstants.MsgPackEncodedMultibyteCharString);
            string actual = reader.ReadString();
            Assert.Equal(TestConstants.MultibyteCharString, actual);
        }

        [Fact]
        public void CancellationToken()
        {
            var reader = new MessagePackReader(default);
            Assert.False(reader.CancellationToken.CanBeCanceled);

            var cts = new CancellationTokenSource();
            reader.CancellationToken = cts.Token;
            Assert.Equal(cts.Token, reader.CancellationToken);
        }

        [Fact]
        public void ReadRaw()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(3);
            writer.WriteArrayHeader(2);
            writer.Write(1);
            writer.Write("Hi");
            writer.Write(5);
            writer.Flush();

            var reader = new MessagePackReader(sequence.AsReadOnlySequence);

            var first = reader.ReadRaw();
            Assert.Equal(1, first.Length);
            Assert.Equal(3, new MessagePackReader(first).ReadInt32());

            var second = reader.ReadRaw();
            Assert.Equal(5, second.Length);

            var third = reader.ReadRaw();
            Assert.Equal(1, third.Length);

            Assert.True(reader.End);
        }

        [Fact]
        public void Depth()
        {
            var reader = new MessagePackReader(Encode((ref MessagePackWriter w) => w.Write(1.23)));
            Assert.Equal(0, reader.Depth);
            reader.Depth++;
            Assert.Equal(1, reader.Depth);
            reader.Depth--;
            Assert.Equal(0, reader.Depth);
        }

        [Fact]
        public void Read_CheckOperations_WithNoBytesLeft()
        {
            ReadOnlySequence<byte> partialMessage = default;

            AssertThrowsEndOfStreamException(partialMessage, (ref MessagePackReader reader) => reader.NextCode);
            AssertThrowsEndOfStreamException(partialMessage, (ref MessagePackReader reader) => reader.NextMessagePackType);

            // These Try methods are meant to return false when it's not a matching code. End of stream when calling these methods is still unexpected.
            AssertThrowsEndOfStreamException(partialMessage, (ref MessagePackReader reader) => reader.TryReadNil());
            AssertThrowsEndOfStreamException(partialMessage, (ref MessagePackReader reader) => reader.TryReadStringSpan(out _));
            AssertThrowsEndOfStreamException(partialMessage, (ref MessagePackReader reader) => reader.IsNil);
        }

        [Fact]
        public void Read_WithInsufficientBytesLeft()
        {
            void AssertIncomplete<T>(WriterEncoder encoder, ReadOperation<T> decoder, bool validMsgPack = true)
            {
                var sequence = Encode(encoder);

                // Test with every possible truncated length.
                for (long len = sequence.Length - 1; len >= 0; len--)
                {
                    var truncated = sequence.Slice(0, len);
                    AssertThrowsEndOfStreamException<T>(truncated, decoder);

                    if (validMsgPack)
                    {
                        AssertThrowsEndOfStreamException(truncated, (ref MessagePackReader reader) => reader.Skip());
                    }
                }
            }

            AssertIncomplete((ref MessagePackWriter writer) => writer.WriteArrayHeader(0xfffffff), (ref MessagePackReader reader) => reader.ReadArrayHeader());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write(true), (ref MessagePackReader reader) => reader.ReadBoolean());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write(0xff), (ref MessagePackReader reader) => reader.ReadByte());
            AssertIncomplete((ref MessagePackWriter writer) => writer.WriteString(Encoding.UTF8.GetBytes("hi")), (ref MessagePackReader reader) => reader.ReadBytes());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write('c'), (ref MessagePackReader reader) => reader.ReadChar());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write(DateTime.Now), (ref MessagePackReader reader) => reader.ReadDateTime());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write(double.MaxValue), (ref MessagePackReader reader) => reader.ReadDouble());
            AssertIncomplete((ref MessagePackWriter writer) => writer.WriteExtensionFormat(new ExtensionResult(5, new byte[3])), (ref MessagePackReader reader) => reader.ReadExtensionFormat());
            AssertIncomplete((ref MessagePackWriter writer) => writer.WriteExtensionFormatHeader(new ExtensionHeader(5, 3)), (ref MessagePackReader reader) => reader.ReadExtensionFormatHeader());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write(0xff), (ref MessagePackReader reader) => reader.ReadInt16());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write(0xff), (ref MessagePackReader reader) => reader.ReadInt32());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write(0xff), (ref MessagePackReader reader) => reader.ReadInt64());
            AssertIncomplete((ref MessagePackWriter writer) => writer.WriteMapHeader(0xfffffff), (ref MessagePackReader reader) => reader.ReadMapHeader());
            AssertIncomplete((ref MessagePackWriter writer) => writer.WriteNil(), (ref MessagePackReader reader) => reader.ReadNil());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write("hi"), (ref MessagePackReader reader) => reader.ReadRaw());
            AssertIncomplete((ref MessagePackWriter writer) => writer.WriteRaw(new byte[10]), (ref MessagePackReader reader) => reader.ReadRaw(10), validMsgPack: false);
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write(0xff), (ref MessagePackReader reader) => reader.ReadSByte());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write(0xff), (ref MessagePackReader reader) => reader.ReadSingle());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write("hi"), (ref MessagePackReader reader) => reader.ReadString());
            AssertIncomplete((ref MessagePackWriter writer) => writer.WriteString(Encoding.UTF8.GetBytes("hi")), (ref MessagePackReader reader) => reader.ReadStringSequence());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write(0xff), (ref MessagePackReader reader) => reader.ReadUInt16());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write(0xff), (ref MessagePackReader reader) => reader.ReadUInt32());
            AssertIncomplete((ref MessagePackWriter writer) => writer.Write(0xff), (ref MessagePackReader reader) => reader.ReadUInt64());
        }

        [Fact]
        public void CreatePeekReader()
        {
            var cts = new CancellationTokenSource();
            var reader = new MessagePackReader(StringEncodedAsFixStr) { CancellationToken = cts.Token };
            reader.ReadRaw(1); // advance to test that the peek reader starts from a non-initial position.
            var peek = reader.CreatePeekReader();

            // Verify equivalence
            Assert.Equal(reader.CancellationToken, peek.CancellationToken);
            Assert.Equal(reader.Position, peek.Position);
            Assert.Equal(reader.Sequence, peek.Sequence);

            // Verify that advancing the peek reader does not advance the original.
            var originalPosition = reader.Position;
            peek.ReadRaw(1);
            Assert.NotEqual(originalPosition, peek.Position);
            Assert.Equal(originalPosition, reader.Position);
        }

        private delegate void ReaderOperation(ref MessagePackReader reader);

        private delegate T ReadOperation<T>(ref MessagePackReader reader);

        private delegate void WriterEncoder(ref MessagePackWriter writer);

        private static void AssertThrowsEndOfStreamException(ReadOnlySequence<byte> sequence, ReaderOperation readOperation)
        {
            Assert.Throws<EndOfStreamException>(() =>
            {
                var reader = new MessagePackReader(sequence);
                readOperation(ref reader);
            });
        }

        private static void AssertThrowsEndOfStreamException<T>(ReadOnlySequence<byte> sequence, ReadOperation<T> readOperation)
        {
            Assert.Throws<EndOfStreamException>(() =>
            {
                Decode(sequence, readOperation);
            });
        }

        private static T Decode<T>(ReadOnlySequence<byte> sequence, ReadOperation<T> readOperation)
        {
            var reader = new MessagePackReader(sequence);
            return readOperation(ref reader);
        }

        private static ReadOnlySequence<byte> Encode(WriterEncoder cb)
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            cb(ref writer);
            writer.Flush();
            return sequence.AsReadOnlySequence;
        }

        private static ReadOnlySequence<T> BuildSequence<T>(params ReadOnlyMemory<T>[] memoryChunks)
        {
            var sequence = new Sequence<T>(new ExactArrayPool<T>())
            {
                MinimumSpanLength = -1,
            };
            foreach (var chunk in memoryChunks)
            {
                var span = sequence.GetSpan(chunk.Length);
                chunk.Span.CopyTo(span);
                sequence.Advance(chunk.Length);
            }

            return sequence;
        }

        private class ExactArrayPool<T> : ArrayPool<T>
        {
            public override T[] Rent(int minimumLength) => new T[minimumLength];

            public override void Return(T[] array, bool clearArray = false)
            {
            }
        }

        /// <summary>
        /// Verifies that a ref struct can create and store a MessagePackReader given a short-lived copy of a
        /// <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        /// <remarks>
        /// This is a 'test' simply by being declared, since C# won't compile it if it's not valid.
        /// </remarks>
        private ref struct MySequenceReader
        {
            private MessagePackReader reader;

            public MySequenceReader(ReadOnlySequence<byte> seq)
            {
                this.reader = new MessagePackReader(seq);
            }
        }
    }

    internal static class MessagePackWriterExtensions
    {
        internal static void WriteByte(ref this MessagePackWriter writer, byte value) => writer.WriteUInt8(value);

        internal static void WriteSByte(ref this MessagePackWriter writer, sbyte value) => writer.WriteInt8(value);
    }
}
