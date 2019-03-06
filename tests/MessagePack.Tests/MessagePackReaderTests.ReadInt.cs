// THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
// CHANGE THE .tt FILE INSTEAD.

using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace MessagePack.Tests
{
    partial class MessagePackReaderTests
    {
        private const sbyte MinNegativeFixInt = unchecked((sbyte)MessagePackCode.MinNegativeFixInt);
        private const sbyte MaxNegativeFixInt = unchecked((sbyte)MessagePackCode.MaxNegativeFixInt);

        private readonly IReadOnlyList<(BigInteger Value, ReadOnlyMemory<byte> Encoded)> IntegersOfInterest = new List<(BigInteger Value, ReadOnlyMemory<byte> Encoded)>
        {
            // * FixInt
            // ** non-boundary
            (3, Encode(b => MessagePackBinary.WriteByte(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteByteForceByteBlock(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteUInt16ForceUInt16Block(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteUInt32ForceUInt32Block(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, 3))),

            (-3, Encode(b => MessagePackBinary.WriteSByte(ref b, 0, -3))),
            (-3, Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, -3))),
            (-3, Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, -3))),
            (-3, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, -3))),
            (-3, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, -3))),

            // ** Boundary conditions
            // *** MaxFixInt
            (MessagePackCode.MaxFixInt, Encode(b => MessagePackBinary.WriteByte(ref b, 0, MessagePackCode.MaxFixInt))),
            (MessagePackCode.MaxFixInt, Encode(b => MessagePackBinary.WriteByteForceByteBlock(ref b, 0, checked((Byte)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode(b => MessagePackBinary.WriteUInt16ForceUInt16Block(ref b, 0, checked((UInt16)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode(b => MessagePackBinary.WriteUInt32ForceUInt32Block(ref b, 0, checked((UInt32)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, checked((UInt64)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, checked((SByte)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, checked((Int16)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, checked((Int32)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, checked((Int64)MessagePackCode.MaxFixInt)))),
            // *** MinFixInt
            (MessagePackCode.MinFixInt, Encode(b => MessagePackBinary.WriteByte(ref b, 0, MessagePackCode.MinFixInt))),
            (MessagePackCode.MinFixInt, Encode(b => MessagePackBinary.WriteByteForceByteBlock(ref b, 0, checked((Byte)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode(b => MessagePackBinary.WriteUInt16ForceUInt16Block(ref b, 0, checked((UInt16)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode(b => MessagePackBinary.WriteUInt32ForceUInt32Block(ref b, 0, checked((UInt32)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, checked((UInt64)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, checked((SByte)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, checked((Int16)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, checked((Int32)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, checked((Int64)MessagePackCode.MinFixInt)))),
            // *** MinNegativeFixInt
            (MinNegativeFixInt, Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, MinNegativeFixInt))),
            (MinNegativeFixInt, Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, MinNegativeFixInt))),
            (MinNegativeFixInt, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, MinNegativeFixInt))),
            (MinNegativeFixInt, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, MinNegativeFixInt))),
            // *** MaxNegativeFixInt
            (MaxNegativeFixInt, Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, MaxNegativeFixInt))),
            (MaxNegativeFixInt, Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, MaxNegativeFixInt))),
            (MaxNegativeFixInt, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, MaxNegativeFixInt))),
            (MaxNegativeFixInt, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, MaxNegativeFixInt))),

            (MessagePackCode.MaxFixInt, Encode(b => MessagePackBinary.WriteInt32(ref b, 0, MessagePackCode.MaxFixInt))),
            (MessagePackCode.MinFixInt, Encode(b => MessagePackBinary.WriteInt32(ref b, 0, MessagePackCode.MinFixInt))),
            (MaxNegativeFixInt, Encode(b => MessagePackBinary.WriteInt32(ref b, 0, MaxNegativeFixInt))),
            (MinNegativeFixInt, Encode(b => MessagePackBinary.WriteInt32(ref b, 0, MinNegativeFixInt))),

            // * Encoded as each type of at least 8 bits
            // ** Small positive value
            (3, Encode(b => MessagePackBinary.WriteByteForceByteBlock(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteUInt16ForceUInt16Block(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteUInt32ForceUInt32Block(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, 3))),
            (3, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, 3))),

            // ** Small negative value
            (-3, Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, -3))),
            (-3, Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, -3))),
            (-3, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, -3))),
            (-3, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, -3))),

            // ** Max values
            // *** Positive
            (0x0ff, Encode(b => MessagePackBinary.WriteByteForceByteBlock(ref b, 0, 255))),
            (0x0ff, Encode(b => MessagePackBinary.WriteUInt16ForceUInt16Block(ref b, 0, 255))),
            (0x0ff, Encode(b => MessagePackBinary.WriteUInt32ForceUInt32Block(ref b, 0, 255))),
            (0x0ff, Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, 255))),
            (0x0ff, Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, 255))),
            (0x0ff, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, 255))),
            (0x0ff, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, 255))),
            (0x0ffff, Encode(b => MessagePackBinary.WriteUInt16ForceUInt16Block(ref b, 0, 65535))),
            (0x0ffff, Encode(b => MessagePackBinary.WriteUInt32ForceUInt32Block(ref b, 0, 65535))),
            (0x0ffff, Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, 65535))),
            (0x0ffff, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, 65535))),
            (0x0ffff, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, 65535))),
            (0x0ffffffff, Encode(b => MessagePackBinary.WriteUInt32ForceUInt32Block(ref b, 0, 4294967295))),
            (0x0ffffffff, Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, 4294967295))),
            (0x0ffffffff, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, 4294967295))),
            (0x0ffffffffffffffff, Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, 18446744073709551615))),
            (0x7f, Encode(b => MessagePackBinary.WriteByteForceByteBlock(ref b, 0, 127))),
            (0x7f, Encode(b => MessagePackBinary.WriteUInt16ForceUInt16Block(ref b, 0, 127))),
            (0x7f, Encode(b => MessagePackBinary.WriteUInt32ForceUInt32Block(ref b, 0, 127))),
            (0x7f, Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, 127))),
            (0x7f, Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, 127))),
            (0x7f, Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, 127))),
            (0x7f, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, 127))),
            (0x7f, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, 127))),
            (0x7fff, Encode(b => MessagePackBinary.WriteUInt16ForceUInt16Block(ref b, 0, 32767))),
            (0x7fff, Encode(b => MessagePackBinary.WriteUInt32ForceUInt32Block(ref b, 0, 32767))),
            (0x7fff, Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, 32767))),
            (0x7fff, Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, 32767))),
            (0x7fff, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, 32767))),
            (0x7fff, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, 32767))),
            (0x7fffffff, Encode(b => MessagePackBinary.WriteUInt32ForceUInt32Block(ref b, 0, 2147483647))),
            (0x7fffffff, Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, 2147483647))),
            (0x7fffffff, Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, 2147483647))),
            (0x7fffffff, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, 2147483647))),
            (0x7fffffffffffffff, Encode(b => MessagePackBinary.WriteUInt64ForceUInt64Block(ref b, 0, 9223372036854775807))),
            (0x7fffffffffffffff, Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, 9223372036854775807))),
            // *** Negative
            (unchecked((SByte)0x80), Encode(b => MessagePackBinary.WriteSByteForceSByteBlock(ref b, 0, -128))),
            (unchecked((SByte)0x80), Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, -128))),
            (unchecked((SByte)0x80), Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, -128))),
            (unchecked((SByte)0x80), Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, -128))),
            (unchecked((Int16)0x8000), Encode(b => MessagePackBinary.WriteInt16ForceInt16Block(ref b, 0, -32768))),
            (unchecked((Int16)0x8000), Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, -32768))),
            (unchecked((Int16)0x8000), Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, -32768))),
            (unchecked((Int32)0x80000000), Encode(b => MessagePackBinary.WriteInt32ForceInt32Block(ref b, 0, -2147483648))),
            (unchecked((Int32)0x80000000), Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, -2147483648))),
            (unchecked((Int64)0x8000000000000000), Encode(b => MessagePackBinary.WriteInt64ForceInt64Block(ref b, 0, -9223372036854775808))),
        };

        [Fact]
        public void ReadByte_ReadVariousLengthsAndMagnitudes()
        {
            foreach (var (value, encoded) in IntegersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.Span[0]));
                if (value <= Byte.MaxValue && value >= Byte.MinValue)
                {
                    Assert.Equal(value, new MessagePackReader(encoded).ReadByte());
                }
                else
                {
                    Assert.Throws<OverflowException>(() => new MessagePackReader(encoded).ReadByte());
                }
            }
        }

        [Fact]
        public void ReadByte_ThrowsOnUnexpectedCode()
        {
            Assert.Throws<InvalidOperationException>(() => new MessagePackReader(StringEncodedAsFixStr).ReadByte());
        }

        [Fact]
        public void ReadUInt16_ReadVariousLengthsAndMagnitudes()
        {
            foreach (var (value, encoded) in IntegersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.Span[0]));
                if (value <= UInt16.MaxValue && value >= UInt16.MinValue)
                {
                    Assert.Equal(value, new MessagePackReader(encoded).ReadUInt16());
                }
                else
                {
                    Assert.Throws<OverflowException>(() => new MessagePackReader(encoded).ReadUInt16());
                }
            }
        }

        [Fact]
        public void ReadUInt16_ThrowsOnUnexpectedCode()
        {
            Assert.Throws<InvalidOperationException>(() => new MessagePackReader(StringEncodedAsFixStr).ReadUInt16());
        }

        [Fact]
        public void ReadUInt32_ReadVariousLengthsAndMagnitudes()
        {
            foreach (var (value, encoded) in IntegersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.Span[0]));
                if (value <= UInt32.MaxValue && value >= UInt32.MinValue)
                {
                    Assert.Equal(value, new MessagePackReader(encoded).ReadUInt32());
                }
                else
                {
                    Assert.Throws<OverflowException>(() => new MessagePackReader(encoded).ReadUInt32());
                }
            }
        }

        [Fact]
        public void ReadUInt32_ThrowsOnUnexpectedCode()
        {
            Assert.Throws<InvalidOperationException>(() => new MessagePackReader(StringEncodedAsFixStr).ReadUInt32());
        }

        [Fact]
        public void ReadUInt64_ReadVariousLengthsAndMagnitudes()
        {
            foreach (var (value, encoded) in IntegersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.Span[0]));
                if (value <= UInt64.MaxValue && value >= UInt64.MinValue)
                {
                    Assert.Equal(value, new MessagePackReader(encoded).ReadUInt64());
                }
                else
                {
                    Assert.Throws<OverflowException>(() => new MessagePackReader(encoded).ReadUInt64());
                }
            }
        }

        [Fact]
        public void ReadUInt64_ThrowsOnUnexpectedCode()
        {
            Assert.Throws<InvalidOperationException>(() => new MessagePackReader(StringEncodedAsFixStr).ReadUInt64());
        }

        [Fact]
        public void ReadSByte_ReadVariousLengthsAndMagnitudes()
        {
            foreach (var (value, encoded) in IntegersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.Span[0]));
                if (value <= SByte.MaxValue && value >= SByte.MinValue)
                {
                    Assert.Equal(value, new MessagePackReader(encoded).ReadSByte());
                }
                else
                {
                    Assert.Throws<OverflowException>(() => new MessagePackReader(encoded).ReadSByte());
                }
            }
        }

        [Fact]
        public void ReadSByte_ThrowsOnUnexpectedCode()
        {
            Assert.Throws<InvalidOperationException>(() => new MessagePackReader(StringEncodedAsFixStr).ReadSByte());
        }

        [Fact]
        public void ReadInt16_ReadVariousLengthsAndMagnitudes()
        {
            foreach (var (value, encoded) in IntegersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.Span[0]));
                if (value <= Int16.MaxValue && value >= Int16.MinValue)
                {
                    Assert.Equal(value, new MessagePackReader(encoded).ReadInt16());
                }
                else
                {
                    Assert.Throws<OverflowException>(() => new MessagePackReader(encoded).ReadInt16());
                }
            }
        }

        [Fact]
        public void ReadInt16_ThrowsOnUnexpectedCode()
        {
            Assert.Throws<InvalidOperationException>(() => new MessagePackReader(StringEncodedAsFixStr).ReadInt16());
        }

        [Fact]
        public void ReadInt32_ReadVariousLengthsAndMagnitudes()
        {
            foreach (var (value, encoded) in IntegersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.Span[0]));
                if (value <= Int32.MaxValue && value >= Int32.MinValue)
                {
                    Assert.Equal(value, new MessagePackReader(encoded).ReadInt32());
                }
                else
                {
                    Assert.Throws<OverflowException>(() => new MessagePackReader(encoded).ReadInt32());
                }
            }
        }

        [Fact]
        public void ReadInt32_ThrowsOnUnexpectedCode()
        {
            Assert.Throws<InvalidOperationException>(() => new MessagePackReader(StringEncodedAsFixStr).ReadInt32());
        }

        [Fact]
        public void ReadInt64_ReadVariousLengthsAndMagnitudes()
        {
            foreach (var (value, encoded) in IntegersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.Span[0]));
                if (value <= Int64.MaxValue && value >= Int64.MinValue)
                {
                    Assert.Equal(value, new MessagePackReader(encoded).ReadInt64());
                }
                else
                {
                    Assert.Throws<OverflowException>(() => new MessagePackReader(encoded).ReadInt64());
                }
            }
        }

        [Fact]
        public void ReadInt64_ThrowsOnUnexpectedCode()
        {
            Assert.Throws<InvalidOperationException>(() => new MessagePackReader(StringEncodedAsFixStr).ReadInt64());
        }
    }
}
