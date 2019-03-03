using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests
{
    public class MessagePackReaderTests
    {
        private const sbyte ByteNegativeValue = -3;
        private const byte BytePositiveValue = 3;
        private static readonly ReadOnlyMemory<byte> BytePositiveEncodedAsUInt64 = Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, BytePositiveValue));
        private static readonly ReadOnlyMemory<byte> BytePositiveEncodedAsUInt32 = Encode(b => MessagePackBinary.WriteUInt32ForceUInt32Block(ref b, 0, BytePositiveValue));
        private static readonly ReadOnlyMemory<byte> BytePositiveEncodedAsUInt16 = Encode(b => MessagePackBinary.WriteUInt16ForceUInt16Block(ref b, 0, BytePositiveValue));
        private static readonly ReadOnlyMemory<byte> BytePositiveEncodedAsUInt8 = Encode(b => MessagePackBinary.WriteByteForceByteBlock(ref b, 0, BytePositiveValue));
        private static readonly ReadOnlyMemory<byte> BytePositiveEncodedAsFixInt = Encode(b => MessagePackBinary.WriteByte(ref b, 0, BytePositiveValue));
        private static readonly ReadOnlyMemory<byte> BytePositiveEncodedAsInt64 = Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, BytePositiveValue));
        private static readonly ReadOnlyMemory<byte> BytePositiveEncodedAsInt32 = Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, BytePositiveValue));
        private static readonly ReadOnlyMemory<byte> BytePositiveEncodedAsInt16 = Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, BytePositiveValue));
        private static readonly ReadOnlyMemory<byte> BytePositiveEncodedAsInt8 = Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, (sbyte)BytePositiveValue));
        private static readonly ReadOnlyMemory<byte> ByteNegativeEncodedAsInt64 = Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, ByteNegativeValue));
        private static readonly ReadOnlyMemory<byte> ByteNegativeEncodedAsInt32 = Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, ByteNegativeValue));
        private static readonly ReadOnlyMemory<byte> ByteNegativeEncodedAsInt16 = Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, ByteNegativeValue));
        private static readonly ReadOnlyMemory<byte> ByteNegativeEncodedAsInt8 = Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, ByteNegativeValue));
        private static readonly ReadOnlyMemory<byte> ByteNegativeEncodedAsFixInt = Encode(b => MessagePackBinary.WriteSByte(ref b, 0, ByteNegativeValue));
        private static readonly ReadOnlyMemory<byte> StringEncodedAsFixStr = Encode(b => MessagePackBinary.WriteString(ref b, 0, "hi"));

        [Fact]
        public void ReadByte_CanReadAllLengths()
        {
            Assert.Equal(BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsInt64).ReadByte());
            Assert.Equal(BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsInt32).ReadByte());
            Assert.Equal(BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsInt16).ReadByte());
            Assert.Equal(BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsInt8).ReadByte());
            Assert.Equal(BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsFixInt).ReadByte());
            Assert.Equal(BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsUInt64).ReadByte());
            Assert.Equal(BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsUInt32).ReadByte());
            Assert.Equal(BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsUInt16).ReadByte());
            Assert.Equal(BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsUInt8).ReadByte());
        }

        [Fact]
        public void ReadByte_ThrowsOverflowException()
        {
            // Negatives
            Assert.Throws<OverflowException>(() => new MessagePackReader(ByteNegativeEncodedAsFixInt).ReadByte());
            Assert.Throws<OverflowException>(() => new MessagePackReader(ByteNegativeEncodedAsInt8).ReadByte());
            Assert.Throws<OverflowException>(() => new MessagePackReader(ByteNegativeEncodedAsInt16).ReadByte());
            Assert.Throws<OverflowException>(() => new MessagePackReader(ByteNegativeEncodedAsInt32).ReadByte());
            Assert.Throws<OverflowException>(() => new MessagePackReader(ByteNegativeEncodedAsInt64).ReadByte());

            // Positives that exceed max value
            Assert.Throws<OverflowException>(() => new MessagePackReader(Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, byte.MaxValue + 1))).ReadByte());
            Assert.Throws<OverflowException>(() => new MessagePackReader(Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, byte.MaxValue + 1))).ReadByte());
            Assert.Throws<OverflowException>(() => new MessagePackReader(Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, byte.MaxValue + 1))).ReadByte());
        }

        [Fact]
        public void ReadByte_ThrowsOnUnexpectedCode()
        {
            Assert.Throws<InvalidOperationException>(() => new MessagePackReader(StringEncodedAsFixStr).ReadByte());
        }

        [Fact]
        public void ReadSByte_CanReadLargerLengths()
        {
            Assert.Equal(ByteNegativeValue, new MessagePackReader(ByteNegativeEncodedAsInt64).ReadSByte());
            Assert.Equal(ByteNegativeValue, new MessagePackReader(ByteNegativeEncodedAsInt32).ReadSByte());
            Assert.Equal(ByteNegativeValue, new MessagePackReader(ByteNegativeEncodedAsInt16).ReadSByte());
            Assert.Equal(ByteNegativeValue, new MessagePackReader(ByteNegativeEncodedAsInt8).ReadSByte());
            Assert.Equal(ByteNegativeValue, new MessagePackReader(ByteNegativeEncodedAsFixInt).ReadSByte());
            Assert.Equal((sbyte)BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsInt64).ReadSByte());
            Assert.Equal((sbyte)BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsInt32).ReadSByte());
            Assert.Equal((sbyte)BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsInt16).ReadSByte());
            Assert.Equal((sbyte)BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsInt8).ReadSByte());
            Assert.Equal((sbyte)BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsFixInt).ReadSByte());
            Assert.Equal((sbyte)BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsUInt64).ReadSByte());
            Assert.Equal((sbyte)BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsUInt32).ReadSByte());
            Assert.Equal((sbyte)BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsUInt16).ReadSByte());
            Assert.Equal((sbyte)BytePositiveValue, new MessagePackReader(BytePositiveEncodedAsUInt8).ReadSByte());
        }

        [Fact]
        public void ReadSByte_CanReadNegativeOwnSize()
        {
            Assert.Equal(ByteNegativeValue, new MessagePackReader(Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, ByteNegativeValue))).ReadSByte());
        }

        [Fact]
        public void ReadSByte_ThrowsOverflowException()
        {
            // Negatives that exceed min value
            Assert.Throws<OverflowException>(() => new MessagePackReader(Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, sbyte.MinValue - 1))).ReadSByte());
            Assert.Throws<OverflowException>(() => new MessagePackReader(Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, sbyte.MinValue - 1))).ReadSByte());
            Assert.Throws<OverflowException>(() => new MessagePackReader(Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, sbyte.MinValue - 1))).ReadSByte());

            // Positives that exceed max value
            Assert.Throws<OverflowException>(() => new MessagePackReader(Encode(b => MessagePackBinary.WriteByteForceByteBlock(ref b, 0, sbyte.MaxValue + 1))).ReadSByte());
            Assert.Throws<OverflowException>(() => new MessagePackReader(Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, sbyte.MaxValue + 1))).ReadSByte());
            Assert.Throws<OverflowException>(() => new MessagePackReader(Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, sbyte.MaxValue + 1))).ReadSByte());
            Assert.Throws<OverflowException>(() => new MessagePackReader(Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, sbyte.MaxValue + 1))).ReadSByte());
        }

        [Fact]
        public void ReadSByte_ThrowsOnUnexpectedCode()
        {
            Assert.Throws<InvalidOperationException>(() => new MessagePackReader(StringEncodedAsFixStr).ReadSByte());
        }

        private static ReadOnlyMemory<byte> Encode(Func<byte[], int> writer)
        {
            byte[] bytes = new byte[100];
            int byteCount = writer(bytes);
            return bytes.AsMemory(0, byteCount);
        }
    }
}
