// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.IO;
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

            var ex = Assert.Throws<MessagePackSerializationException>(() =>
            {
                var reader = new MessagePackReader(sequence);
                reader.ReadArrayHeader();
            });
            Assert.IsType<EndOfStreamException>(ex.InnerException);
        }

        [Fact]
        public void ReadMapHeader_MitigatesLargeAllocations()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.WriteMapHeader(9999);
            writer.Flush();

            var ex = Assert.Throws<MessagePackSerializationException>(() =>
            {
                var reader = new MessagePackReader(sequence);
                reader.ReadMapHeader();
            });
            Assert.IsType<EndOfStreamException>(ex.InnerException);
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

        private delegate void WriterEncoder(ref MessagePackWriter writer);

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
    }

    internal static class MessagePackWriterExtensions
    {
        internal static void WriteByte(ref this MessagePackWriter writer, byte value) => writer.WriteUInt8(value);

        internal static void WriteSByte(ref this MessagePackWriter writer, sbyte value) => writer.WriteInt8(value);
    }
}
