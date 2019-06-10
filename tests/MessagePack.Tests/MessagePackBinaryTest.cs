using System;
using System.Linq;
using System.IO;
using Xunit;
using System.Text;
using Nerdbank.Streams;
using System.Buffers;

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

        MsgPack.MessagePackObject CreateUnpackedReference(ReadOnlySequence<byte> bytes) => CreateUnpackedReference(bytes.ToArray());

        [Fact]
        public void NilTest()
        {
            (var stream, var packer) = CreateReferencePacker();

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.WriteNil();
            writer.Flush();
            sequence.Length.Is(1);

            packer.PackNull().Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadNil().Is(Nil.Default);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).IsNil.IsTrue();
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 1)]
        public void BoolTest(bool target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            sequence.Length.Is(length);

            packer.Pack(target).Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadBoolean().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsBoolean().Is(target);
        }

        [Theory]
        [InlineData(byte.MinValue, 1)]
        [InlineData(111, 1)]
        [InlineData(136, 2)]
        [InlineData(byte.MaxValue, 2)]
        public void ByteTest(byte target, int length)
        {
            (var stream, var packer) = CreateReferencePacker();

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            sequence.Length.Is(length);

            packer.Pack(target).Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadByte().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsByte().Is(target);
        }

        public static object[][] bytesTestData = new object[][]
        {
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            sequence.Length.Is(length);

            packer.PackBinary(target).Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadBytes().ToArray().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsBinary().Is(target);
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            sequence.Length.Is(length);

            packer.Pack(target).Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadSByte().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsSByte().Is(target);
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            sequence.Length.Is(length);

            packer.Pack(target).Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadSingle().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsSingle().Is(target);
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            sequence.Length.Is(length);

            packer.Pack(target).Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadDouble().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsDouble().Is(target);
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();

            sequence.Length.Is(length);

            packer.Pack(target).Position.Is(sequence.Length);
            // stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadInt16().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsInt16().Is(target);
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            sequence.Length.Is(length);

            // bug of msgpack-cli
            if (target == 255)
            {
                packer.Pack((byte)255).Position.Is(sequence.Length);
            }
            else if (target == 50000)
            {
                packer.Pack((ushort)50000).Position.Is(sequence.Length);
            }
            else
            {
                packer.Pack(target).Position.Is(sequence.Length);
            }
            // stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadInt32().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsInt32().Is(target);
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            sequence.Length.Is(length);

            // bug of msgpack-cli
            if (target == 255)
            {
                packer.Pack((byte)255).Position.Is(sequence.Length);
            }
            else if (target == 50000)
            {
                packer.Pack((ushort)50000).Position.Is(sequence.Length);
            }
            else if (target == uint.MaxValue)
            {
                packer.Pack(uint.MaxValue).Position.Is(sequence.Length);
            }
            else
            {
                packer.Pack(target).Position.Is(sequence.Length);
            }
            // stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadInt64().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsInt64().Is(target);
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.WriteMapHeader(target);
            writer.Flush();
            sequence.Length.Is(length);

            packer.PackMapHeader((int)target).Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            // Expand sequence enough that ReadArrayHeader doesn't throw due to its security check.
            writer.Write(new byte[target * 2]);
            writer.Flush();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadMapHeader().Is((int)target);
            sequenceReader.ReadBytes(); // read the padding we added
            sequenceReader.End.IsTrue();

            var ms = new MemoryStream(sequence.AsReadOnlySequence.ToArray());
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.WriteArrayHeader(target);
            writer.Flush();
            sequence.Length.Is(length);

            packer.PackArrayHeader((int)target).Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            // Expand sequence enough that ReadArrayHeader doesn't throw due to its security check.
            writer.Write(new byte[target]);
            writer.Flush();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadArrayHeader().Is((int)target);
            sequenceReader.ReadBytes(); // read the padding we added
            sequenceReader.End.IsTrue();

            var ms = new MemoryStream(sequence.AsReadOnlySequence.ToArray());
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            sequence.Length.Is(length);

            packer.Pack(target).Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadUInt16().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsUInt16().Is(target);
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            sequence.Length.Is(length);

            packer.Pack(target).Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadUInt32().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsUInt32().Is(target);
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            sequence.Length.Is(length);

            packer.Pack(target).Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadUInt64().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsUInt64().Is(target);
        }


        public static object[][] stringTestData = new object[][]
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            var returnLength = sequence.Length;

            var referencePacked = packer.PackString(target);
            referencePacked.Position.Is(returnLength);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadString().Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsStringUtf8().Is(target);
        }

        [Theory]
        [MemberData(nameof(stringTestData))]
        public void StringSegmentTest(string target)
        {
            (var stream, var packer) = CreateReferencePacker();

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            var returnLength = sequence.Length;

            var referencePacked = packer.PackString(target);
            referencePacked.Position.Is(returnLength);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            var segment = sequenceReader.ReadStringSegment().ToArray();
            Encoding.UTF8.GetString(segment).Is(target);
            sequenceReader.End.IsTrue();

            CreateUnpackedReference(sequence).AsStringUtf8().Is(target);
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            var returnLength = sequence.Length;

            var referencePacked = packer.Pack(target);
            referencePacked.Position.Is(returnLength);
            referencePacked.Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            sequenceReader.ReadChar().Is(target);
            sequenceReader.End.IsTrue();
            ((char)CreateUnpackedReference(sequence).AsUInt16()).Is(target);
        }


        public static object[][] extTestData = new object[][]
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

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.WriteExtensionFormat(new ExtensionResult(typeCode, target));
            writer.Flush();
            var returnLength = sequence.Length;

            var referencePacked = packer.PackExtendedTypeValue((byte)typeCode, target);
            referencePacked.Position.Is(returnLength);
            referencePacked.Position.Is(sequence.Length);
            stream.ToArray().SequenceEqual(sequence.AsReadOnlySequence.ToArray()).IsTrue();

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            var ext = sequenceReader.ReadExtensionFormat();
            ext.TypeCode.Is(typeCode);
            ext.Data.ToArray().SequenceEqual(target).IsTrue();
            sequenceReader.End.IsTrue();

            var ext2 = CreateUnpackedReference(sequence).AsMessagePackExtendedTypeObject();
            ext2.TypeCode.Is((byte)ext.TypeCode);
            ext2.GetBody().SequenceEqual(ext.Data.ToArray()).IsTrue();
        }

        // FixExt4(-1) => seconds |  [1970-01-01 00:00:00 UTC, 2106-02-07 06:28:16 UTC) range
        // FixExt8(-1) => nanoseconds + seconds | [1970-01-01 00:00:00.000000000 UTC, 2514-05-30 01:53:04.000000000 UTC) range
        // Ext8(12,-1) => nanoseconds + seconds | [-584554047284-02-23 16:59:44 UTC, 584554051223-11-09 07:00:16.000000000 UTC) range
        public static object[][] dateTimeTestData = new object[][]
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
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            writer.Write(target);
            writer.Flush();
            var returnLength = sequence.Length;
            returnLength.Is(expectedLength);

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            var result = sequenceReader.ReadDateTime();
            sequenceReader.End.IsTrue();

            result.Is(target);
        }

        [Fact]
        public void IntegerRangeTest()
        {
            // Int16 can accepts UInt8
            // Int32 can accepts UInt16
            // Int64 can accepts UInt32
            {
                var small = new Sequence<byte>();
                var smallWriter = new MessagePackWriter(small);
                smallWriter.Write(byte.MaxValue);
                smallWriter.Flush();
                var smallReader = new MessagePackReader(small.AsReadOnlySequence);
                smallReader.ReadInt16().Is(byte.MaxValue);

                var target = new Sequence<byte>();
                var targetWriter = new MessagePackWriter(target);
                targetWriter.Write((short)byte.MaxValue);
                targetWriter.Flush();
                target.AsReadOnlySequence.ToArray().SequenceEqual(small.AsReadOnlySequence.ToArray()).IsTrue();
            }
            {
                var small = new Sequence<byte>();
                var smallWriter = new MessagePackWriter(small);
                smallWriter.Write(byte.MaxValue);
                smallWriter.Flush();
                var smallReader = new MessagePackReader(small.AsReadOnlySequence);
                smallReader.ReadInt32().Is(byte.MaxValue);

                var target = new Sequence<byte>();
                var targetWriter = new MessagePackWriter(target);
                targetWriter.Write((int)byte.MaxValue);
                targetWriter.Flush();
                target.AsReadOnlySequence.ToArray().SequenceEqual(small.AsReadOnlySequence.ToArray()).IsTrue();

                small.Reset();
                smallWriter = new MessagePackWriter(small);
                smallWriter.Write(ushort.MaxValue);
                smallWriter.Flush();
                smallReader = new MessagePackReader(small.AsReadOnlySequence);
                smallReader.ReadInt32().Is(ushort.MaxValue);

                target.Reset();
                targetWriter = new MessagePackWriter(target);
                targetWriter.Write((int)ushort.MaxValue);
                targetWriter.Flush();
                target.AsReadOnlySequence.ToArray().SequenceEqual(small.AsReadOnlySequence.ToArray()).IsTrue();
            }
            {
                var small = new Sequence<byte>();
                var smallWriter = new MessagePackWriter(small);
                smallWriter.Write(byte.MaxValue);
                smallWriter.Flush();
                var smallReader = new MessagePackReader(small.AsReadOnlySequence);
                smallReader.ReadInt64().Is(byte.MaxValue);

                var target = new Sequence<byte>();
                var targetWriter = new MessagePackWriter(target);
                targetWriter.Write((long)byte.MaxValue);
                targetWriter.Flush();
                target.AsReadOnlySequence.ToArray().SequenceEqual(small.AsReadOnlySequence.ToArray()).IsTrue();

                small.Reset();
                smallWriter = new MessagePackWriter(small);
                smallWriter.Write(ushort.MaxValue);
                smallWriter.Flush();
                smallReader = new MessagePackReader(small.AsReadOnlySequence);
                smallReader.ReadInt64().Is(ushort.MaxValue);

                target.Reset();
                targetWriter = new MessagePackWriter(target);
                targetWriter.Write((long)ushort.MaxValue);
                targetWriter.Flush();
                target.AsReadOnlySequence.ToArray().SequenceEqual(small.AsReadOnlySequence.ToArray()).IsTrue();

                small.Reset();
                smallWriter = new MessagePackWriter(small);
                smallWriter.Write(uint.MaxValue);
                smallWriter.Flush();
                smallReader = new MessagePackReader(small.AsReadOnlySequence);
                smallReader.ReadInt64().Is(uint.MaxValue);

                target.Reset();
                targetWriter = new MessagePackWriter(target);
                targetWriter.Write((long)uint.MaxValue);
                targetWriter.Flush();
                target.AsReadOnlySequence.ToArray().SequenceEqual(small.AsReadOnlySequence.ToArray()).IsTrue();
            }
        }
    }
}
