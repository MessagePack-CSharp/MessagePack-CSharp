using System;
using System.Numerics;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Tests
{
    public partial class MessagePackReaderTests
    {
        private const sbyte ByteNegativeValue = -3;
        private const byte BytePositiveValue = 3;
        private static readonly ReadOnlyMemory<byte> StringEncodedAsFixStr = Encode(b => MessagePackBinary.WriteString(ref b, 0, "hi"));
        private readonly ITestOutputHelper logger;

        public MessagePackReaderTests(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

        [Fact]
        public void ReadSingle_ReadIntegersOfVariousLengthsAndMagnitudes()
        {
            foreach (var (value, encoded) in IntegersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.Span[0]));
                Assert.Equal((float)(double)value, new MessagePackReader(encoded).ReadSingle());
            }
        }

        [Fact]
        public void ReadSingle_CanReadDouble()
        {
            var reader = new MessagePackReader(Encode(b => MessagePackBinary.WriteDouble(ref b, 0, 1.23)));
            Assert.Equal(1.23f, reader.ReadSingle());
        }

        private static ReadOnlyMemory<byte> Encode(Func<byte[], int> writer)
        {
            byte[] bytes = new byte[100];
            int byteCount = writer(bytes);
            return bytes.AsMemory(0, byteCount);
        }
    }
}
