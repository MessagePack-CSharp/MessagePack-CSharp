// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.IO;
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

        public MessagePackReaderTests(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

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

        private delegate void WriterEncoder(ref MessagePackWriter writer);

        private static ReadOnlySequence<byte> Encode(WriterEncoder cb)
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            cb(ref writer);
            writer.Flush();
            return sequence.AsReadOnlySequence;
        }
    }

    internal static class MessagePackWriterExtensions
    {
        internal static void WriteByte(ref this MessagePackWriter writer, byte value) => writer.WriteUInt8(value);

        internal static void WriteSByte(ref this MessagePackWriter writer, sbyte value) => writer.WriteInt8(value);
    }
}
