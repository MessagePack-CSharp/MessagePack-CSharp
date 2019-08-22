// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace MessagePack.Tests
{
    public partial class MessagePackReaderTests
    {
        private const sbyte MinNegativeFixInt = unchecked((sbyte)MessagePackCode.MinNegativeFixInt);
        private const sbyte MaxNegativeFixInt = unchecked((sbyte)MessagePackCode.MaxNegativeFixInt);

        private readonly IReadOnlyList<(BigInteger Value, ReadOnlySequence<byte> Encoded)> integersOfInterest = new List<(BigInteger Value, ReadOnlySequence<byte> Encoded)>
        {
            // * FixInt
            // ** non-boundary
            (3, Encode((ref MessagePackWriter w) => w.WriteByte(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteByte(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteUInt16(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteUInt32(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteUInt64(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteSByte(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteInt16(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteInt32(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteInt64(3))),

            (-3, Encode((ref MessagePackWriter w) => w.WriteSByte(-3))),
            (-3, Encode((ref MessagePackWriter w) => w.WriteSByte(-3))),
            (-3, Encode((ref MessagePackWriter w) => w.WriteInt16(-3))),
            (-3, Encode((ref MessagePackWriter w) => w.WriteInt32(-3))),
            (-3, Encode((ref MessagePackWriter w) => w.WriteInt64(-3))),

            // ** Boundary conditions
            /* MaxFixInt */
            (MessagePackCode.MaxFixInt, Encode((ref MessagePackWriter w) => w.WriteByte(MessagePackCode.MaxFixInt))),
            (MessagePackCode.MaxFixInt, Encode((ref MessagePackWriter w) => w.WriteByte(checked((Byte)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode((ref MessagePackWriter w) => w.WriteUInt16(checked((UInt16)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode((ref MessagePackWriter w) => w.WriteUInt32(checked((UInt32)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode((ref MessagePackWriter w) => w.WriteUInt64(checked((UInt64)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode((ref MessagePackWriter w) => w.WriteSByte(checked((SByte)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode((ref MessagePackWriter w) => w.WriteInt16(checked((Int16)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode((ref MessagePackWriter w) => w.WriteInt32(checked((Int32)MessagePackCode.MaxFixInt)))),
            (MessagePackCode.MaxFixInt, Encode((ref MessagePackWriter w) => w.WriteInt64(checked((Int64)MessagePackCode.MaxFixInt)))),
            /* MinFixInt */
            (MessagePackCode.MinFixInt, Encode((ref MessagePackWriter w) => w.WriteByte(MessagePackCode.MinFixInt))),
            (MessagePackCode.MinFixInt, Encode((ref MessagePackWriter w) => w.WriteByte(checked((Byte)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode((ref MessagePackWriter w) => w.WriteUInt16(checked((UInt16)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode((ref MessagePackWriter w) => w.WriteUInt32(checked((UInt32)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode((ref MessagePackWriter w) => w.WriteUInt64(checked((UInt64)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode((ref MessagePackWriter w) => w.WriteSByte(checked((SByte)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode((ref MessagePackWriter w) => w.WriteInt16(checked((Int16)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode((ref MessagePackWriter w) => w.WriteInt32(checked((Int32)MessagePackCode.MinFixInt)))),
            (MessagePackCode.MinFixInt, Encode((ref MessagePackWriter w) => w.WriteInt64(checked((Int64)MessagePackCode.MinFixInt)))),
            /* MinNegativeFixInt */
            (MinNegativeFixInt, Encode((ref MessagePackWriter w) => w.WriteSByte(MinNegativeFixInt))),
            (MinNegativeFixInt, Encode((ref MessagePackWriter w) => w.WriteInt16(MinNegativeFixInt))),
            (MinNegativeFixInt, Encode((ref MessagePackWriter w) => w.WriteInt32(MinNegativeFixInt))),
            (MinNegativeFixInt, Encode((ref MessagePackWriter w) => w.WriteInt64(MinNegativeFixInt))),
            /* MaxNegativeFixInt */
            (MaxNegativeFixInt, Encode((ref MessagePackWriter w) => w.WriteSByte(MaxNegativeFixInt))),
            (MaxNegativeFixInt, Encode((ref MessagePackWriter w) => w.WriteInt16(MaxNegativeFixInt))),
            (MaxNegativeFixInt, Encode((ref MessagePackWriter w) => w.WriteInt32(MaxNegativeFixInt))),
            (MaxNegativeFixInt, Encode((ref MessagePackWriter w) => w.WriteInt64(MaxNegativeFixInt))),

            (MessagePackCode.MaxFixInt, Encode((ref MessagePackWriter w) => w.WriteInt32(MessagePackCode.MaxFixInt))),
            (MessagePackCode.MinFixInt, Encode((ref MessagePackWriter w) => w.WriteInt32(MessagePackCode.MinFixInt))),
            (MaxNegativeFixInt, Encode((ref MessagePackWriter w) => w.WriteInt32(MaxNegativeFixInt))),
            (MinNegativeFixInt, Encode((ref MessagePackWriter w) => w.WriteInt32(MinNegativeFixInt))),

            // * Encoded as each type of at least 8 bits
            // ** Small positive value
            (3, Encode((ref MessagePackWriter w) => w.WriteByte(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteUInt16(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteUInt32(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteUInt64(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteSByte(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteInt16(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteInt32(3))),
            (3, Encode((ref MessagePackWriter w) => w.WriteInt64(3))),

            // ** Small negative value
            (-3, Encode((ref MessagePackWriter w) => w.WriteSByte(-3))),
            (-3, Encode((ref MessagePackWriter w) => w.WriteInt16(-3))),
            (-3, Encode((ref MessagePackWriter w) => w.WriteInt32(-3))),
            (-3, Encode((ref MessagePackWriter w) => w.WriteInt64(-3))),

            // ** Max values
            /* Positive */
            (0x0ff, Encode((ref MessagePackWriter w) => w.WriteByte(255))),
            (0x0ff, Encode((ref MessagePackWriter w) => w.WriteUInt16(255))),
            (0x0ff, Encode((ref MessagePackWriter w) => w.WriteUInt32(255))),
            (0x0ff, Encode((ref MessagePackWriter w) => w.WriteUInt64(255))),
            (0x0ff, Encode((ref MessagePackWriter w) => w.WriteInt16(255))),
            (0x0ff, Encode((ref MessagePackWriter w) => w.WriteInt32(255))),
            (0x0ff, Encode((ref MessagePackWriter w) => w.WriteInt64(255))),
            (0x0ffff, Encode((ref MessagePackWriter w) => w.WriteUInt16(65535))),
            (0x0ffff, Encode((ref MessagePackWriter w) => w.WriteUInt32(65535))),
            (0x0ffff, Encode((ref MessagePackWriter w) => w.WriteUInt64(65535))),
            (0x0ffff, Encode((ref MessagePackWriter w) => w.WriteInt32(65535))),
            (0x0ffff, Encode((ref MessagePackWriter w) => w.WriteInt64(65535))),
            (0x0ffffffff, Encode((ref MessagePackWriter w) => w.WriteUInt32(4294967295))),
            (0x0ffffffff, Encode((ref MessagePackWriter w) => w.WriteUInt64(4294967295))),
            (0x0ffffffff, Encode((ref MessagePackWriter w) => w.WriteInt64(4294967295))),
            (0x0ffffffffffffffff, Encode((ref MessagePackWriter w) => w.WriteUInt64(18446744073709551615))),
            (0x7f, Encode((ref MessagePackWriter w) => w.WriteByte(127))),
            (0x7f, Encode((ref MessagePackWriter w) => w.WriteUInt16(127))),
            (0x7f, Encode((ref MessagePackWriter w) => w.WriteUInt32(127))),
            (0x7f, Encode((ref MessagePackWriter w) => w.WriteUInt64(127))),
            (0x7f, Encode((ref MessagePackWriter w) => w.WriteSByte(127))),
            (0x7f, Encode((ref MessagePackWriter w) => w.WriteInt16(127))),
            (0x7f, Encode((ref MessagePackWriter w) => w.WriteInt32(127))),
            (0x7f, Encode((ref MessagePackWriter w) => w.WriteInt64(127))),
            (0x7fff, Encode((ref MessagePackWriter w) => w.WriteUInt16(32767))),
            (0x7fff, Encode((ref MessagePackWriter w) => w.WriteUInt32(32767))),
            (0x7fff, Encode((ref MessagePackWriter w) => w.WriteUInt64(32767))),
            (0x7fff, Encode((ref MessagePackWriter w) => w.WriteInt16(32767))),
            (0x7fff, Encode((ref MessagePackWriter w) => w.WriteInt32(32767))),
            (0x7fff, Encode((ref MessagePackWriter w) => w.WriteInt64(32767))),
            (0x7fffffff, Encode((ref MessagePackWriter w) => w.WriteUInt32(2147483647))),
            (0x7fffffff, Encode((ref MessagePackWriter w) => w.WriteUInt64(2147483647))),
            (0x7fffffff, Encode((ref MessagePackWriter w) => w.WriteInt32(2147483647))),
            (0x7fffffff, Encode((ref MessagePackWriter w) => w.WriteInt64(2147483647))),
            (0x7fffffffffffffff, Encode((ref MessagePackWriter w) => w.WriteUInt64(9223372036854775807))),
            (0x7fffffffffffffff, Encode((ref MessagePackWriter w) => w.WriteInt64(9223372036854775807))),
            /* Negative */
            (unchecked((SByte)0x80), Encode((ref MessagePackWriter w) => w.WriteSByte(-128))),
            (unchecked((SByte)0x80), Encode((ref MessagePackWriter w) => w.WriteInt16(-128))),
            (unchecked((SByte)0x80), Encode((ref MessagePackWriter w) => w.WriteInt32(-128))),
            (unchecked((SByte)0x80), Encode((ref MessagePackWriter w) => w.WriteInt64(-128))),
            (unchecked((Int16)0x8000), Encode((ref MessagePackWriter w) => w.WriteInt16(-32768))),
            (unchecked((Int16)0x8000), Encode((ref MessagePackWriter w) => w.WriteInt32(-32768))),
            (unchecked((Int16)0x8000), Encode((ref MessagePackWriter w) => w.WriteInt64(-32768))),
            (unchecked((Int32)0x80000000), Encode((ref MessagePackWriter w) => w.WriteInt32(-2147483648))),
            (unchecked((Int32)0x80000000), Encode((ref MessagePackWriter w) => w.WriteInt64(-2147483648))),
            (unchecked((Int64)0x8000000000000000), Encode((ref MessagePackWriter w) => w.WriteInt64(-9223372036854775808))),
        };

        [Fact]
        public void ReadByte_ReadVariousLengthsAndMagnitudes()
        {
            foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
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
            foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
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
            foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
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
            foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
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
            foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
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
            foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
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
            foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
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
            foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
            {
                this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
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
