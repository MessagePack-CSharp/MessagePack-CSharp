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

        private static ReadOnlyMemory<byte> Encode(Func<byte[], int> writer)
        {
            byte[] bytes = new byte[100];
            int byteCount = writer(bytes);
            return bytes.AsMemory(0, byteCount);
        }
    }
}
