using System;
using System.Linq;
using System.IO;
using Xunit;
using System.Text;

namespace MessagePack.Tests
{
    public class MessagePackBinaryTest
    {
        (MemoryStream, MsgPack.Packer) CreateReferencePacker()
        {
            var ms = new MemoryStream();
            var packer = MsgPack.Packer.Create(ms, MsgPack.PackerCompatibilityOptions.None);
            return (ms, packer);
        }

        MsgPack.MessagePackObject CreateUnpackedReference(byte[] bytes)
        {
            var ms = new MemoryStream(bytes);
            var unpacker = MsgPack.Unpacker.Create(ms);
            unpacker.Read();
            return unpacker.LastReadData;
        }

        [Fact]
        public void NilTest()
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteNil(ref bytes, 0).Is(1);

            packer.PackNull().Position.Is(bytes.Length);
            stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadNil(bytes, 0, out readSize).Is(Nil.Default);
            readSize.Is(1);

            CreateUnpackedReference(bytes).IsNil.IsTrue();
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 1)]
        public void BoolTest(bool target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteBoolean(ref bytes, 0, target).Is(length);

            packer.Pack(target).Position.Is(bytes.Length);
            stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadBoolean(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            CreateUnpackedReference(bytes).AsBoolean().Is(target);
        }

        [Theory]
        [InlineData(byte.MinValue, 1)]
        [InlineData(111, 1)]
        [InlineData(136, 2)]
        [InlineData(byte.MaxValue, 2)]
        public void ByteTest(byte target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteByte(ref bytes, 0, target).Is(length);

            packer.Pack(target).Position.Is(bytes.Length);
            stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadByte(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            CreateUnpackedReference(bytes).AsByte().Is(target);
        }

        public static object[] bytesTestData = new object[]
        {
            new object[]{ (byte[])null, 1 },
            new object[]{ new byte[] { }, 2 },
            new object[]{ new byte[] { 1, 2, 3 }, 5 },
            new object[]{ Enumerable.Repeat((byte)100, byte.MaxValue).ToArray(), 255 + 2 },
            new object[]{ Enumerable.Repeat((byte)100, UInt16.MaxValue).ToArray(), 65535 + 3 },
            new object[]{ Enumerable.Repeat((byte)100, 99999).ToArray(), 99999 + 5 },
        };

        [Theory]
        [MemberData(nameof(bytesTestData))]
        public void BytesTest(byte[] target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteBytes(ref bytes, 0, target).Is(length);

            packer.PackBinary(target).Position.Is(bytes.Length);
            stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadBytes(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            CreateUnpackedReference(bytes).AsBinary().Is(target);
        }

        [Theory]
        [InlineData(sbyte.MinValue, 2)]
        [InlineData(-100, 2)]
        [InlineData(-33, 2)]
        [InlineData(-32, 1)]
        [InlineData(-31, 1)]
        [InlineData(-30, 1)]
        [InlineData(-1, 1)]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(126, 1)]
        [InlineData(sbyte.MaxValue, 1)]
        public void SByteTest(sbyte target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteSByte(ref bytes, 0, target).Is(length);

            packer.Pack(target).Position.Is(bytes.Length);
            stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadSByte(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            CreateUnpackedReference(bytes).AsSByte().Is(target);
        }

        [Theory]
        [InlineData(Single.MinValue, 5)]
        [InlineData(0.0f, 5)]
        [InlineData(12345.6789f, 5)]
        [InlineData(-12345.6789f, 5)]
        [InlineData(Single.MaxValue, 5)]
        [InlineData(Single.NaN, 5)]
        [InlineData(Single.PositiveInfinity, 5)]
        [InlineData(Single.NegativeInfinity, 5)]
        [InlineData(Single.Epsilon, 5)]
        public void SingleTest(Single target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteSingle(ref bytes, 0, target).Is(length);

            packer.Pack(target).Position.Is(bytes.Length);
            stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadSingle(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            CreateUnpackedReference(bytes).AsSingle().Is(target);
        }

        [Theory]
        [InlineData(Double.MinValue, 9)]
        [InlineData(0.0, 9)]
        [InlineData(12345.6789, 9)]
        [InlineData(-12345.6789, 9)]
        [InlineData(Double.MaxValue, 9)]
        [InlineData(Double.NaN, 9)]
        [InlineData(Double.PositiveInfinity, 9)]
        [InlineData(Double.NegativeInfinity, 9)]
        [InlineData(Double.Epsilon, 9)]
        public void DoubleTest(Double target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteDouble(ref bytes, 0, target).Is(length);

            packer.Pack(target).Position.Is(bytes.Length);
            stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadDouble(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            CreateUnpackedReference(bytes).AsDouble().Is(target);
        }

        [Theory]
        [InlineData(short.MinValue, 3)]
        [InlineData(-30000, 3)]
        [InlineData((short)sbyte.MinValue, 2)]
        [InlineData(-100, 2)]
        [InlineData(-33, 2)]
        [InlineData(-32, 1)]
        [InlineData(-31, 1)]
        [InlineData(-30, 1)]
        [InlineData(-1, 1)]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(126, 1)]
        [InlineData((short)sbyte.MaxValue, 1)]
        [InlineData(20000, 3)]
        [InlineData(short.MaxValue, 3)]
        public void Int16Test(short target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteInt16(ref bytes, 0, target).Is(length);

            packer.Pack(target).Position.Is(bytes.Length);
            // stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadInt16(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            CreateUnpackedReference(bytes).AsInt16().Is(target);
        }

        [Theory]
        [InlineData(int.MinValue, 5)]
        [InlineData(-50000, 5)]
        [InlineData(short.MinValue, 3)]
        [InlineData(-30000, 3)]
        [InlineData((short)sbyte.MinValue, 2)]
        [InlineData(-100, 2)]
        [InlineData(-33, 2)]
        [InlineData(-32, 1)]
        [InlineData(-31, 1)]
        [InlineData(-30, 1)]
        [InlineData(-1, 1)]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(126, 1)]
        [InlineData(sbyte.MaxValue, 1)]
        [InlineData(byte.MaxValue, 2)]
        [InlineData(20000, 3)]
        [InlineData(short.MaxValue, 3)]
        [InlineData(50000, 3)]
        [InlineData(int.MaxValue, 5)]
        public void Int32Test(int target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteInt32(ref bytes, 0, target).Is(length);

            // bug of msgpack-cli
            if (target == 255)
            {
                packer.Pack((byte)255).Position.Is(bytes.Length);
            }
            else if (target == 50000)
            {
                packer.Pack((ushort)50000).Position.Is(bytes.Length);
            }
            else
            {
                packer.Pack(target).Position.Is(bytes.Length);
            }
            // stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadInt32(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            CreateUnpackedReference(bytes).AsInt32().Is(target);
        }

        [Theory]
        [InlineData(long.MinValue, 9)]
        [InlineData(-3372036854775807, 9)]
        [InlineData(int.MinValue, 5)]
        [InlineData(-50000, 5)]
        [InlineData(short.MinValue, 3)]
        [InlineData(-30000, 3)]
        [InlineData((short)sbyte.MinValue, 2)]
        [InlineData(-100, 2)]
        [InlineData(-33, 2)]
        [InlineData(-32, 1)]
        [InlineData(-31, 1)]
        [InlineData(-30, 1)]
        [InlineData(-1, 1)]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(126, 1)]
        [InlineData(sbyte.MaxValue, 1)]
        [InlineData(byte.MaxValue, 2)]
        [InlineData(20000, 3)]
        [InlineData(short.MaxValue, 3)]
        [InlineData(50000, 3)]
        [InlineData(int.MaxValue, 5)]
        [InlineData(uint.MaxValue, 5)]
        [InlineData(3372036854775807, 9)]
        [InlineData(long.MaxValue, 9)]
        public void Int64Test(long target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteInt64(ref bytes, 0, target).Is(length);

            // bug of msgpack-cli
            if (target == 255)
            {
                packer.Pack((byte)255).Position.Is(bytes.Length);
            }
            else if (target == 50000)
            {
                packer.Pack((ushort)50000).Position.Is(bytes.Length);
            }
            else if (target == uint.MaxValue)
            {
                packer.Pack(uint.MaxValue).Position.Is(bytes.Length);
            }
            else
            {
                packer.Pack(target).Position.Is(bytes.Length);
            }
            // stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadInt64(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            CreateUnpackedReference(bytes).AsInt64().Is(target);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(14, 1)]
        [InlineData(15, 1)]
        [InlineData(16, 3)]
        [InlineData(17, 3)]
        [InlineData(18, 3)]
        [InlineData(126, 3)]
        [InlineData(byte.MaxValue, 3)]
        [InlineData(20000, 3)]
        [InlineData(ushort.MaxValue, 3)]
        [InlineData(80000, 5)]
        public void MapHeaderTest(uint target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = new byte[length + target * 2];
            MessagePackBinary.WriteMapHeader(ref bytes, 0, target).Is(length);

            packer.PackMapHeader((int)target).Position.Is(length);
            stream.ToArray().SequenceEqual(bytes.Take(length)).IsTrue();

            int readSize;
            MessagePackBinary.ReadMapHeaderRaw(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            var ms = new MemoryStream(bytes);
            var unpacker = MsgPack.Unpacker.Create(ms);
            long len;
            unpacker.ReadMapLength(out len).IsTrue();
            len.Is(target);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(14, 1)]
        [InlineData(15, 1)]
        [InlineData(16, 3)]
        [InlineData(17, 3)]
        [InlineData(18, 3)]
        [InlineData(126, 3)]
        [InlineData(byte.MaxValue, 3)]
        [InlineData(20000, 3)]
        [InlineData(ushort.MaxValue, 3)]
        [InlineData(80000, 5)]
        public void ArrayHeaderTest(uint target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = new byte[length + target];
            MessagePackBinary.WriteArrayHeader(ref bytes, 0, target).Is(length);

            packer.PackArrayHeader((int)target).Position.Is(length);
            stream.ToArray().SequenceEqual(bytes.Take(length)).IsTrue();

            int readSize;
            MessagePackBinary.ReadArrayHeaderRaw(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            var ms = new MemoryStream(bytes);
            var unpacker = MsgPack.Unpacker.Create(ms);
            long len;
            unpacker.ReadArrayLength(out len).IsTrue();
            len.Is(target);
        }



        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(126, 1)]
        [InlineData((short)sbyte.MaxValue, 1)]
        [InlineData(20000, 3)]
        [InlineData(short.MaxValue, 3)]
        [InlineData(50000, 3)]
        [InlineData(ushort.MaxValue, 3)]
        public void UInt16Test(ushort target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteUInt16(ref bytes, 0, target).Is(length);

            packer.Pack(target).Position.Is(bytes.Length);
            stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadUInt16(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            CreateUnpackedReference(bytes).AsUInt16().Is(target);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(126, 1)]
        [InlineData((short)sbyte.MaxValue, 1)]
        [InlineData(20000, 3)]
        [InlineData(short.MaxValue, 3)]
        [InlineData(50000, 3)]
        [InlineData(ushort.MaxValue, 3)]
        [InlineData(int.MaxValue, 5)]
        [InlineData(3294967295, 5)]
        [InlineData(uint.MaxValue, 5)]
        public void UInt32Test(uint target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteUInt32(ref bytes, 0, target).Is(length);

            packer.Pack(target).Position.Is(bytes.Length);
            stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadUInt32(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            CreateUnpackedReference(bytes).AsUInt32().Is(target);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(126, 1)]
        [InlineData((short)sbyte.MaxValue, 1)]
        [InlineData(20000, 3)]
        [InlineData(short.MaxValue, 3)]
        [InlineData(50000, 3)]
        [InlineData(ushort.MaxValue, 3)]
        [InlineData(int.MaxValue, 5)]
        [InlineData(3294967295, 5)]
        [InlineData(uint.MaxValue, 5)]
        [InlineData(3372036854775807, 9)]
        [InlineData(long.MaxValue, 9)]
        [InlineData(12446744073709551615, 9)]
        [InlineData(ulong.MaxValue, 9)]
        public void UInt64Test(ulong target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            MessagePackBinary.WriteUInt64(ref bytes, 0, target).Is(length);

            packer.Pack(target).Position.Is(bytes.Length);
            stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadUInt64(bytes, 0, out readSize).Is(target);
            readSize.Is(length);

            CreateUnpackedReference(bytes).AsUInt64().Is(target);
        }


        public static object[] stringTestData = new object[]
        {
            new object[]{ "a"},
            new object[]{ "abc" },
            new object[]{ "012345678901234567890123456789"}, // 30
            new object[]{ "0123456789012345678901234567890"}, // 31
            new object[]{ "01234567890123456789012345678901"}, // 32
            new object[]{ "012345678901234567890123456789012"}, // 33
            new object[]{ "0123456789012345678901234567890123"}, // 33
            new object[]{ new string('a', sbyte.MaxValue - 1) },
            new object[]{ new string('a', sbyte.MaxValue) },
            new object[]{ new string('a', sbyte.MaxValue + 1) },
            new object[]{ new string('a', byte.MaxValue - 1) },
            new object[]{ new string('a', byte.MaxValue) },
            new object[]{ new string('a', byte.MaxValue + 1) },
            new object[]{ new string('a', ushort.MaxValue - 1) },
            new object[]{ new string('a', short.MaxValue -1 ) },
            new object[]{ new string('a', short.MaxValue  ) },
            new object[]{ new string('a', short.MaxValue + 1) },
            new object[]{ new string('a', ushort.MaxValue ) },
            new object[]{ new string('a', ushort.MaxValue+1 ) },
            new object[]{ "あいうえおかきくけこさしすせおたちつてとなにぬねのわをん"}, // Japanese
        };

        [Theory]
        [MemberData(nameof(stringTestData))]
        public void StringTest(string target)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            var returnLength = MessagePackBinary.WriteString(ref bytes, 0, target);

            var referencePacked = packer.PackString(target);
            referencePacked.Position.Is(returnLength);
            stream.ToArray().SequenceEqual(bytes.Take(returnLength).ToArray()).IsTrue();

            int readSize;
            MessagePackBinary.ReadString(bytes, 0, out readSize).Is(target);
            readSize.Is(returnLength);
            CreateUnpackedReference(bytes).AsStringUtf8().Is(target);
        }

        [Theory]
        [MemberData(nameof(stringTestData))]
        public void StringSegmentTest(string target)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            var returnLength = MessagePackBinary.WriteString(ref bytes, 0, target);

            var referencePacked = packer.PackString(target);
            referencePacked.Position.Is(returnLength);
            stream.ToArray().SequenceEqual(bytes.Take(returnLength).ToArray()).IsTrue();

            int readSize;
            var segment = MessagePackBinary.ReadStringSegment(bytes, 0, out readSize);
            Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count).Is(target);
            readSize.Is(returnLength);
            CreateUnpackedReference(bytes).AsStringUtf8().Is(target);
        }

        [Theory]
        [InlineData(char.MinValue)]
        [InlineData('a')]
        [InlineData('あ')]
        [InlineData('c')]
        [InlineData(char.MaxValue)]
        public void CharTest(char target)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            var returnLength = MessagePackBinary.WriteChar(ref bytes, 0, target);

            var referencePacked = packer.Pack(target);
            referencePacked.Position.Is(returnLength);
            referencePacked.Position.Is(bytes.Length);
            stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            MessagePackBinary.ReadChar(bytes, 0, out readSize).Is(target);
            readSize.Is(returnLength);
            ((char)CreateUnpackedReference(bytes).AsUInt16()).Is(target);
        }


        public static object[] extTestData = new object[]
        {
            new object[]{ 0,  Enumerable.Repeat((byte)1, 0).ToArray() },
            new object[]{ 1,  Enumerable.Repeat((byte)1, 1).ToArray() },
            new object[]{ 2,  Enumerable.Repeat((byte)1, 2).ToArray() },
            new object[]{ 3,  Enumerable.Repeat((byte)1, 3).ToArray() },
            new object[]{ 4,  Enumerable.Repeat((byte)1, 4).ToArray() },
            new object[]{ 5,  Enumerable.Repeat((byte)1, 5).ToArray() },
            new object[]{ 6,  Enumerable.Repeat((byte)1, 6).ToArray() },
            new object[]{ 7,  Enumerable.Repeat((byte)1, 7).ToArray() },
            new object[]{ 8,  Enumerable.Repeat((byte)1, 8).ToArray() },
            new object[]{ 9,  Enumerable.Repeat((byte)1, 9).ToArray() },
            new object[]{ 10, Enumerable.Repeat((byte)1, 10).ToArray() },
            new object[]{ 11, Enumerable.Repeat((byte)1, 11).ToArray() },
            new object[]{ 12, Enumerable.Repeat((byte)1, 12).ToArray() },
            new object[]{ 13, Enumerable.Repeat((byte)1, 13).ToArray() },
            new object[]{ 14, Enumerable.Repeat((byte)1, 14).ToArray() },
            new object[]{ 15, Enumerable.Repeat((byte)1, 15).ToArray() },
            new object[]{ 16, Enumerable.Repeat((byte)1, 16).ToArray() },
            new object[]{ 17, Enumerable.Repeat((byte)1, 17).ToArray() },
            new object[]{ 29, Enumerable.Repeat((byte)1, sbyte.MaxValue - 1).ToArray() },
            new object[]{ 39, Enumerable.Repeat((byte)1, sbyte.MaxValue).ToArray() },
            new object[]{ 49, Enumerable.Repeat((byte)1, sbyte.MaxValue + 1).ToArray() },
            new object[]{ 59, Enumerable.Repeat((byte)1, byte.MaxValue - 1).ToArray() },
            new object[]{ 69, Enumerable.Repeat((byte)1, byte.MaxValue).ToArray() },
            new object[]{ 79, Enumerable.Repeat((byte)1, byte.MaxValue + 1).ToArray() },
            new object[]{ 89, Enumerable.Repeat((byte)1, short.MaxValue - 1).ToArray() },
            new object[]{ 99, Enumerable.Repeat((byte)1, short.MaxValue).ToArray() },
            new object[]{ 100, Enumerable.Repeat((byte)1, short.MaxValue + 1).ToArray() },
            new object[]{ 101, Enumerable.Repeat((byte)1, ushort.MaxValue - 1).ToArray() },
            new object[]{ 102, Enumerable.Repeat((byte)1, ushort.MaxValue).ToArray() },
            new object[]{ 103, Enumerable.Repeat((byte)1, ushort.MaxValue + 1).ToArray() },
        };

        [Theory]
        [MemberData(nameof(extTestData))]
        public void ExtTest(sbyte typeCode, byte[] target)
        {
            (var stream, var packer) = CreateReferencePacker();

            byte[] bytes = null;
            var returnLength = MessagePackBinary.WriteExtensionFormat(ref bytes, 0, typeCode, target);

            var referencePacked = packer.PackExtendedTypeValue((byte)typeCode, target);
            referencePacked.Position.Is(returnLength);
            referencePacked.Position.Is(bytes.Length);
            stream.ToArray().SequenceEqual(bytes).IsTrue();

            int readSize;
            var ext = MessagePackBinary.ReadExtensionFormat(bytes, 0, out readSize);
            ext.TypeCode.Is(typeCode);
            ext.Data.SequenceEqual(target).IsTrue();
            readSize.Is(returnLength);

            var ext2 = CreateUnpackedReference(bytes).AsMessagePackExtendedTypeObject();
            ext2.TypeCode.Is((byte)ext.TypeCode);
            ext2.GetBody().SequenceEqual(ext.Data).IsTrue();
        }

        // FixExt4(-1) => seconds |  [1970-01-01 00:00:00 UTC, 2106-02-07 06:28:16 UTC) range
        // FixExt8(-1) => nanoseconds + seconds | [1970-01-01 00:00:00.000000000 UTC, 2514-05-30 01:53:04.000000000 UTC) range
        // Ext8(12,-1) => nanoseconds + seconds | [-584554047284-02-23 16:59:44 UTC, 584554051223-11-09 07:00:16.000000000 UTC) range
        public static object[] dateTimeTestData = new object[]
        {
            new object[]{ new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6},
            new object[]{ new DateTime(2010, 12, 1, 3, 4, 57, 0, DateTimeKind.Utc), 6},
            new object[]{ new DateTime(2106, 2, 7, 6, 28, 15, 0, DateTimeKind.Utc), 6},
            new object[]{ new DateTime(2106, 2, 7, 6, 28, 16, 0, DateTimeKind.Utc), 10},
            new object[]{ new DateTime(2106, 2, 7, 6, 28, 17, 0, DateTimeKind.Utc), 10},
            new object[]{ new DateTime(2106, 2, 7, 6, 28, 16, 1, DateTimeKind.Utc), 10},
            new object[]{ new DateTime(2010, 12, 1, 3, 4, 57, 123, DateTimeKind.Utc), 10},
            new object[]{ new DateTime(2514, 5, 30, 1, 53, 4, 0, DateTimeKind.Utc).AddMilliseconds(-1), 10},
            new object[]{ new DateTime(2514, 5, 30, 1, 53, 4, 0, DateTimeKind.Utc), 15},
            new object[]{ new DateTime(2514, 5, 30, 1, 53, 4, 0, DateTimeKind.Utc).AddMilliseconds(1), 15 },
            new object[]{ new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(-1), 15},
            new object[]{ new DateTime(111, 12, 1, 3, 4, 57, 0, DateTimeKind.Utc), 15},
            new object[]{ new DateTime(111, 12, 1, 3, 4, 57, 123, DateTimeKind.Utc), 15},
            new object[]{  new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Utc), 15},
            new object[]{ new DateTime(DateTime.MaxValue.Ticks, DateTimeKind.Utc), 15},
        };

        [Theory]
        [MemberData(nameof(dateTimeTestData))]
        public void DateTimeTest(DateTime target, int expectedLength)
        {
            byte[] bytes = null;
            var returnLength = MessagePackBinary.WriteDateTime(ref bytes, 0, target);
            returnLength.Is(expectedLength);

            int readSize;
            var result = MessagePackBinary.ReadDateTime(bytes, 0, out readSize);
            readSize.Is(returnLength);

            result.Is(target);
        }

        [Fact]
        public void IntegerRangeTest()
        {
            // Int16 can accepts UInt8
            // Int32 can accepts UInt16
            // Int64 can accepts UInt32
            {
                int readSize;
                byte[] small = null;
                byte[] target = null;
                MessagePackBinary.WriteByte(ref small, 0, byte.MaxValue);
                MessagePackBinary.ReadInt16(small, 0, out readSize).Is(byte.MaxValue);
                MessagePackBinary.WriteInt16(ref target, 0, byte.MaxValue);
                target.SequenceEqual(small).IsTrue();
            }
            {
                int readSize;
                byte[] small = null;
                byte[] target = null;
                MessagePackBinary.WriteByte(ref small, 0, byte.MaxValue);
                MessagePackBinary.ReadInt32(small, 0, out readSize).Is(byte.MaxValue);
                MessagePackBinary.WriteInt32(ref target, 0, byte.MaxValue);
                target.SequenceEqual(small).IsTrue();

                small = target = null;
                MessagePackBinary.WriteUInt16(ref small, 0, ushort.MaxValue);
                MessagePackBinary.ReadInt32(small, 0, out readSize).Is(ushort.MaxValue);
                MessagePackBinary.WriteInt32(ref target, 0, ushort.MaxValue);
                target.SequenceEqual(small).IsTrue();
            }
            {
                int readSize;
                byte[] small = null;
                byte[] target = null;
                MessagePackBinary.WriteByte(ref small, 0, byte.MaxValue);
                MessagePackBinary.ReadInt64(small, 0, out readSize).Is(byte.MaxValue);
                MessagePackBinary.WriteInt64(ref target, 0, byte.MaxValue);
                target.SequenceEqual(small).IsTrue();

                small = target = null;
                MessagePackBinary.WriteUInt16(ref small, 0, ushort.MaxValue);
                MessagePackBinary.ReadInt64(small, 0, out readSize).Is(ushort.MaxValue);
                MessagePackBinary.WriteInt64(ref target, 0, ushort.MaxValue);
                target.SequenceEqual(small).IsTrue();

                small = target = null;
                MessagePackBinary.WriteUInt32(ref small, 0, uint.MaxValue);
                MessagePackBinary.ReadInt64(small, 0, out readSize).Is(uint.MaxValue);
                MessagePackBinary.WriteInt64(ref target, 0, uint.MaxValue);
                target.SequenceEqual(small).IsTrue();
            }
        }

        [Fact]
        public void ReadArrayHeader_MitigatesLargeAllocations()
        {
            byte[] bytes = null;
            int count = MessagePackBinary.WriteArrayHeader(ref bytes, 0, 9999);
            Assert.Throws<EndOfStreamException>(() => MessagePackBinary.ReadArrayHeader(bytes, 0, out int readSize));
        }

        [Fact]
        public void ReadArrayHeaderRaw_MitigatesLargeAllocations()
        {
            byte[] bytes = null;
            int count = MessagePackBinary.WriteArrayHeader(ref bytes, 0, 9999);
            Assert.Throws<EndOfStreamException>(() => MessagePackBinary.ReadArrayHeaderRaw(bytes, 0, out int readSize));
        }

        [Fact]
        public void ReadMapHeader_MitigatesLargeAllocations()
        {
            byte[] bytes = null;
            int count = MessagePackBinary.WriteMapHeader(ref bytes, 0, 9999);
            Assert.Throws<EndOfStreamException>(() => MessagePackBinary.ReadMapHeader(bytes, 0, out int readSize));
        }

        [Fact]
        public void ReadMapHeaderRaw_MitigatesLargeAllocations()
        {
            byte[] bytes = null;
            int count = MessagePackBinary.WriteMapHeader(ref bytes, 0, 9999);
            Assert.Throws<EndOfStreamException>(() => MessagePackBinary.ReadMapHeaderRaw(bytes, 0, out int readSize));
        }
    }
}
