// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable SA1300 // Element should begin with uppercase letter

namespace MessagePack.Tests
{
    public class LZ4Test
    {
#if !UNITY_2018_3_OR_NEWER

        private readonly ITestOutputHelper logger;

        public LZ4Test(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

#endif

        [Theory]
        [InlineData(MessagePackCompression.Lz4Block)]
        [InlineData(MessagePackCompression.Lz4BlockArray)]
        [Trait("CWE", "409")]
        public void Lz4RejectsDeclaredOutputOverMaximumBeforeAllocating(MessagePackCompression compression)
        {
            byte[] payload = compression == MessagePackCompression.Lz4Block
                ? new byte[] { 0xC7, 0x06, 0x63, 0xD2, 0x7F, 0xFF, 0xFF, 0xFF, 0x00 }
                : new byte[] { 0x92, 0xC7, 0x05, 0x62, 0xD2, 0x7F, 0xFF, 0xFF, 0xFF, 0xC4, 0x01, 0x00 };
            var arrayPool = new ThrowingArrayPool(1024);
            var options = MessagePackSerializerOptions.Standard
                .WithCompression(compression)
                .WithSecurity(MessagePackSecurity.UntrustedData)
                .WithPool(new SequencePool(1, arrayPool));

            MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<object>(payload, options));

            Assert.Contains("exceeds the configured maximum", FlattenMessages(ex));
            Assert.Null(arrayPool.LargestRequestedLength);
        }

        [Fact]
        [Trait("CWE", "409")]
        public void Lz4BlockArrayRejectsTotalDeclaredOutputOverMaximum()
        {
            byte[] payload =
            {
                0x93,
                0xC7, 0x02, 0x62, 0x04, 0x04,
                0xC4, 0x05, 0x40, 0x20, 0x20, 0x20, 0x20,
                0xC4, 0x05, 0x40, 0x20, 0x20, 0x20, 0x20,
            };
            var options = MessagePackSerializerOptions.Standard
                .WithCompression(MessagePackCompression.Lz4BlockArray)
                .WithSecurity(MessagePackSecurity.UntrustedData.WithMaximumDecompressedSize(7));

            MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<object>(payload, options));

            Assert.Contains("exceeds the configured maximum", FlattenMessages(ex));
        }

        [Theory]
        [InlineData(MessagePackCompression.Lz4Block)]
        [InlineData(MessagePackCompression.Lz4BlockArray)]
        [Trait("CWE", "409")]
        public void Lz4AllowsHighlyCompressiblePayloadWithinMaximum(MessagePackCompression compression)
        {
            string data = new string(' ', 100_000);
            var options = MessagePackSerializerOptions.Standard
                .WithCompression(compression)
                .WithSecurity(MessagePackSecurity.UntrustedData.WithMaximumDecompressedSize(200_000));

            byte[] payload = MessagePackSerializer.Serialize(data, options);
            string actual = MessagePackSerializer.Deserialize<string>(payload, options);

            Assert.Equal(data, actual);
        }

        [Fact]
        public void Lz4Compress()
        {
            Execute(1);
            Execute(10);
            Execute(100);
            Execute(1000);
            Execute(10000);
        }

        [Fact]
        [Trait("CWE", "125")]
        public void Lz4BlockRejectsTruncatedLiteralRun()
        {
            const int extensionByteCount = 1024;
            int uncompressedLength = 15 + (255 * extensionByteCount) + 16;

            byte[] sizeHeader =
            {
                0xCE,
                (byte)(uncompressedLength >> 24),
                (byte)(uncompressedLength >> 16),
                (byte)(uncompressedLength >> 8),
                (byte)uncompressedLength,
            };

            byte[] lz4 = new byte[1 + extensionByteCount];
            lz4[0] = 0xF0;
            for (int i = 1; i < lz4.Length; i++)
            {
                lz4[i] = 0xFF;
            }

            int bodyLength = sizeHeader.Length + lz4.Length;
            byte[] payload = new byte[6 + bodyLength];
            int offset = 0;
            payload[offset++] = 0xC9;
            payload[offset++] = (byte)(bodyLength >> 24);
            payload[offset++] = (byte)(bodyLength >> 16);
            payload[offset++] = (byte)(bodyLength >> 8);
            payload[offset++] = (byte)bodyLength;
            payload[offset++] = 99;
            System.Array.Copy(sizeHeader, 0, payload, offset, sizeHeader.Length);
            offset += sizeHeader.Length;
            System.Array.Copy(lz4, 0, payload, offset, lz4.Length);

            var options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);

            Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<object>(payload, options));
        }

        private void Execute(int count)
        {
            // Large
            {
                var data = Enumerable.Range(1, count)
                    .Select(x => new SharedData.SimpleStringKeyData
                    {
                        Prop1 = x,
                        Prop2 = SharedData.ByteEnum.A,
                        Prop3 = unchecked(x * x),
                    })
                    .ToArray();

                var lz4Option = MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4Block);
                var lz4Contiguous = MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4BlockArray);

                // check bin1, 2, 3 size...
                var bin2 = MessagePackSerializer.Serialize(data, lz4Option);
                var bin3 = MessagePackSerializer.Serialize(data, lz4Contiguous);

#if !UNITY_2018_3_OR_NEWER
                var bin1 = MessagePackSerializer.Serialize(data, MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.None));
                logger.WriteLine("Len:" + count + " NoneSize:" + bin1.Length);
                logger.WriteLine("Len:" + count + " Lz4BlockSize:" + bin2.Length);
                logger.WriteLine("Len:" + count + " Lz4ContiguousBlockSize:" + bin3.Length);
#endif

                var d1 = MessagePackSerializer.Deserialize<SharedData.SimpleStringKeyData[]>(bin2, lz4Option);
                var d2 = MessagePackSerializer.Deserialize<SharedData.SimpleStringKeyData[]>(bin2, lz4Contiguous);
                var d3 = MessagePackSerializer.Deserialize<SharedData.SimpleStringKeyData[]>(bin3, lz4Option);
                var d4 = MessagePackSerializer.Deserialize<SharedData.SimpleStringKeyData[]>(bin3, lz4Contiguous);

                SequenceStructuralEqual(d1, data);
                SequenceStructuralEqual(d2, data);
                SequenceStructuralEqual(d3, data);
                SequenceStructuralEqual(d4, data);
            }
        }

        private static void SequenceStructuralEqual(SharedData.SimpleStringKeyData[] actual, SharedData.SimpleStringKeyData[] expected)
        {
            actual.Length.Is(expected.Length);

            for (int i = 0; i < actual.Length; i++)
            {
                actual[i].Prop1.Is(expected[i].Prop1);
                actual[i].Prop2.Is(expected[i].Prop2);
                actual[i].Prop3.Is(expected[i].Prop3);
            }
        }

        private static string FlattenMessages(System.Exception ex)
        {
            return ex.InnerException is null ? ex.Message : ex.Message + " " + FlattenMessages(ex.InnerException);
        }

        private class ThrowingArrayPool : ArrayPool<byte>
        {
            private readonly int maximumLength;

            internal ThrowingArrayPool(int maximumLength)
            {
                this.maximumLength = maximumLength;
            }

            internal int? LargestRequestedLength { get; private set; }

            public override byte[] Rent(int minimumLength)
            {
                this.LargestRequestedLength = this.LargestRequestedLength.HasValue ? System.Math.Max(this.LargestRequestedLength.Value, minimumLength) : minimumLength;
                if (minimumLength > this.maximumLength)
                {
                    throw new System.InvalidOperationException("Unexpected decompression allocation request: " + minimumLength);
                }

                return new byte[minimumLength];
            }

            public override void Return(byte[] array, bool clearArray = false)
            {
            }
        }
    }
}
