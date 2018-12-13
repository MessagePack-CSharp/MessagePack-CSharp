using System;
using Xunit;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedData;

namespace MessagePack.Tests
{
    public class MessagePackBinaryStream
    {
        private MessagePackSerializer serializer = new MessagePackSerializer();
        private MessagePackSerializer.NonGeneric nonGenericSerializer = new MessagePackSerializer.NonGeneric();
        private LZ4MessagePackSerializer lz4Serializer = new LZ4MessagePackSerializer();
        private LZ4MessagePackSerializer.NonGeneric lz4nonGenericSerializer = new LZ4MessagePackSerializer.NonGeneric();

        delegate int RefAction(ref byte[] bytes, int offset);

        [Fact]
        public void Write()
        {
            void Check(Action<Stream> streamAction, RefAction bytesAction)
            {
                const int CheckOffset = 10;

                var ms = new MemoryStream();
                byte[] bytes = null;

                ms.Position = CheckOffset;
                streamAction(ms);
                var len = bytesAction(ref bytes, CheckOffset);
                MessagePackBinary.FastResize(ref bytes, CheckOffset + len);

                ms.ToArray().Is(bytes);
            }

            Check(
                      (x) => MessagePackBinary.WriteArrayHeader(x, 999),
    (ref byte[] x, int y) => MessagePackBinary.WriteArrayHeader(ref x, y, 999));

            Check(
                      (x) => MessagePackBinary.WriteArrayHeaderForceArray32Block(x, 999),
    (ref byte[] x, int y) => MessagePackBinary.WriteArrayHeaderForceArray32Block(ref x, y, 999));

            Check(
                      (x) => MessagePackBinary.WriteBoolean(x, true),
    (ref byte[] x, int y) => MessagePackBinary.WriteBoolean(ref x, y, true));

            Check(
                      (x) => MessagePackBinary.WriteByte(x, (byte)100),
    (ref byte[] x, int y) => MessagePackBinary.WriteByte(ref x, y, (byte)100));

            Check(
                      (x) => MessagePackBinary.WriteByteForceByteBlock(x, (byte)11),
    (ref byte[] x, int y) => MessagePackBinary.WriteByteForceByteBlock(ref x, y, (byte)11));

            Check(
                      (x) => MessagePackBinary.WriteBytes(x, new byte[] { 1, 10, 100 }),
    (ref byte[] x, int y) => MessagePackBinary.WriteBytes(ref x, y, new byte[] { 1, 10, 100 }));

            Check(
                      (x) => MessagePackBinary.WriteChar(x, 'z'),
    (ref byte[] x, int y) => MessagePackBinary.WriteChar(ref x, y, 'z'));

            var now = DateTime.UtcNow;
            Check(
                      (x) => MessagePackBinary.WriteDateTime(x, now),
    (ref byte[] x, int y) => MessagePackBinary.WriteDateTime(ref x, y, now));

            Check(
                      (x) => MessagePackBinary.WriteDouble(x, 10.31231f),
    (ref byte[] x, int y) => MessagePackBinary.WriteDouble(ref x, y, 10.31231f));

            Check(
                      (x) => MessagePackBinary.WriteExtensionFormat(x, 10, new byte[] { 1, 10, 100 }),
    (ref byte[] x, int y) => MessagePackBinary.WriteExtensionFormat(ref x, y, 10, new byte[] { 1, 10, 100 }));

            Check(
                      (x) => MessagePackBinary.WriteFixedArrayHeaderUnsafe(x, 'z'),
    (ref byte[] x, int y) => MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref x, y, 'z'));

            Check(
                      (x) => MessagePackBinary.WriteFixedMapHeaderUnsafe(x, 'z'),
    (ref byte[] x, int y) => MessagePackBinary.WriteFixedMapHeaderUnsafe(ref x, y, 'z'));

            Check(
                      (x) => MessagePackBinary.WriteFixedStringUnsafe(x, "aaa", Encoding.UTF8.GetByteCount("aaa")),
    (ref byte[] x, int y) => MessagePackBinary.WriteFixedStringUnsafe(ref x, y, "aaa", Encoding.UTF8.GetByteCount("aaa")));

            Check(
                      (x) => MessagePackBinary.WriteInt16(x, 321),
    (ref byte[] x, int y) => MessagePackBinary.WriteInt16(ref x, y, 321));

            Check(
                      (x) => MessagePackBinary.WriteInt16ForceInt16Block(x, 321),
    (ref byte[] x, int y) => MessagePackBinary.WriteInt16ForceInt16Block(ref x, y, 321));

            Check(
                      (x) => MessagePackBinary.WriteInt32(x, 321),
    (ref byte[] x, int y) => MessagePackBinary.WriteInt32(ref x, y, 321));

            Check(
                      (x) => MessagePackBinary.WriteInt32ForceInt32Block(x, 321),
    (ref byte[] x, int y) => MessagePackBinary.WriteInt32ForceInt32Block(ref x, y, 321));

            Check(
                      (x) => MessagePackBinary.WriteInt64(x, 321),
    (ref byte[] x, int y) => MessagePackBinary.WriteInt64(ref x, y, 321));

            Check(
                      (x) => MessagePackBinary.WriteInt64ForceInt64Block(x, 321),
    (ref byte[] x, int y) => MessagePackBinary.WriteInt64ForceInt64Block(ref x, y, 321));

            Check(
                      (x) => MessagePackBinary.WriteMapHeader(x, 321),
    (ref byte[] x, int y) => MessagePackBinary.WriteMapHeader(ref x, y, 321));

            Check(
                      (x) => MessagePackBinary.WriteMapHeaderForceMap32Block(x, 321),
    (ref byte[] x, int y) => MessagePackBinary.WriteMapHeaderForceMap32Block(ref x, y, 321));

            Check(
                      (x) => MessagePackBinary.WriteNil(x),
    (ref byte[] x, int y) => MessagePackBinary.WriteNil(ref x, y));

            Check(
                      (x) => MessagePackBinary.WritePositiveFixedIntUnsafe(x, 12),
    (ref byte[] x, int y) => MessagePackBinary.WritePositiveFixedIntUnsafe(ref x, y, 12));

            Check(
                      (x) => MessagePackBinary.WriteSByte(x, 12),
    (ref byte[] x, int y) => MessagePackBinary.WriteSByte(ref x, y, 12));

            Check(
                      (x) => MessagePackBinary.WriteSByteForceSByteBlock(x, 12),
    (ref byte[] x, int y) => MessagePackBinary.WriteSByteForceSByteBlock(ref x, y, 12));

            Check(
                      (x) => MessagePackBinary.WriteSingle(x, 123),
    (ref byte[] x, int y) => MessagePackBinary.WriteSingle(ref x, y, 123));

            Check(
                      (x) => MessagePackBinary.WriteString(x, "aaa"),
    (ref byte[] x, int y) => MessagePackBinary.WriteString(ref x, y, "aaa"));

            Check(
                      (x) => MessagePackBinary.WriteStringBytes(x, new byte[] { 1, 10 }),
    (ref byte[] x, int y) => MessagePackBinary.WriteStringBytes(ref x, y, new byte[] { 1, 10 }));

            Check(
                      (x) => MessagePackBinary.WriteStringForceStr32Block(x, "zzz"),
    (ref byte[] x, int y) => MessagePackBinary.WriteStringForceStr32Block(ref x, y, "zzz"));

            Check(
                      (x) => MessagePackBinary.WriteStringUnsafe(x, "zzz", Encoding.UTF8.GetByteCount("zzz")),
    (ref byte[] x, int y) => MessagePackBinary.WriteStringUnsafe(ref x, y, "zzz", Encoding.UTF8.GetByteCount("zzz")));

            Check(
                      (x) => MessagePackBinary.WriteUInt16(x, 31),
    (ref byte[] x, int y) => MessagePackBinary.WriteUInt16(ref x, y, 31));

            Check(
                      (x) => MessagePackBinary.WriteUInt16ForceUInt16Block(x, 32),
    (ref byte[] x, int y) => MessagePackBinary.WriteUInt16ForceUInt16Block(ref x, y, 32));

            Check(
                      (x) => MessagePackBinary.WriteUInt32(x, 11),
    (ref byte[] x, int y) => MessagePackBinary.WriteUInt32(ref x, y, 11));

            Check(
                      (x) => MessagePackBinary.WriteUInt32ForceUInt32Block(x, 11),
    (ref byte[] x, int y) => MessagePackBinary.WriteUInt32ForceUInt32Block(ref x, y, 11));

            Check(
                      (x) => MessagePackBinary.WriteUInt64(x, 11),
    (ref byte[] x, int y) => MessagePackBinary.WriteUInt64(ref x, y, 11));

            Check(
                      (x) => MessagePackBinary.WriteUInt64ForceUInt64Block(x, 11),
    (ref byte[] x, int y) => MessagePackBinary.WriteUInt64ForceUInt64Block(ref x, y, 11));
        }


        [Fact]
        public void Read()
        {
            void Check1<T, T2>(T data, T2 result, Func<Stream, T2> streamRead)
            {
                const int CheckOffset = 10;

                byte[] bytes = null;
                var len = MessagePack.Resolvers.StandardResolver.Instance.GetFormatter<T>().Serialize(ref bytes, CheckOffset, data, MessagePack.Resolvers.StandardResolver.Instance);
                MessagePackBinary.FastResize(ref bytes, CheckOffset + len);


                var ms = new MemoryStream(bytes);
                ms.Position = CheckOffset;

                streamRead(ms).Is(result);
            }

            void Check2<T>(T data, Func<Stream, T> streamRead)
            {
                const int CheckOffset = 10;

                byte[] bytes = null;
                var len = MessagePack.Resolvers.StandardResolver.Instance.GetFormatter<T>().Serialize(ref bytes, CheckOffset, data, MessagePack.Resolvers.StandardResolver.Instance);
                MessagePackBinary.FastResize(ref bytes, CheckOffset + len);


                var ms = new MemoryStream(bytes);
                ms.Position = CheckOffset;

                streamRead(ms).Is(data);
            }

            Check1(new[] { 1, 10, 100, 1000, 10000, short.MaxValue, int.MaxValue }, 7, x => MessagePackBinary.ReadArrayHeader(x));
            Check1(new[] { 1, 10, 100, 1000, 10000, short.MaxValue, int.MaxValue }, (uint)7, x => MessagePackBinary.ReadArrayHeaderRaw(x));
            Check1(Nil.Default, true, x => MessagePackBinary.IsNil((x)));
            Check2(true, x => MessagePackBinary.ReadBoolean(x));
            Check2((byte)100, x => MessagePackBinary.ReadByte(x));
            Check2(new byte[] { 1, 10, 100, 245 }, x => MessagePackBinary.ReadBytes(x));
            Check2('あ', x => MessagePackBinary.ReadChar(x));
            Check2(DateTime.UtcNow, x => MessagePackBinary.ReadDateTime(x));
            Check2(132, x => MessagePackBinary.ReadInt16(x));
            Check2(423, x => MessagePackBinary.ReadInt32(x));
            Check2(64332, x => MessagePackBinary.ReadInt64(x));
            Check2(Nil.Default, x => MessagePackBinary.ReadNil(x));
            Check2(11, x => MessagePackBinary.ReadSByte(x));
            Check2(10.31231f, x => MessagePackBinary.ReadSingle(x));
            Check2("foobar", x => MessagePackBinary.ReadString(x));
            Check2(124, x => MessagePackBinary.ReadUInt16(x));
            Check2((uint)432, x => MessagePackBinary.ReadUInt32(x));
            Check2((ulong)432, x => MessagePackBinary.ReadUInt64(x));


            Check1(new Dictionary<int, int>() { { 1, 2 } }, 1, x => MessagePackBinary.ReadMapHeader(x));
            Check1(new Dictionary<int, int>() { { 1, 2 } }, (uint)1, x => MessagePackBinary.ReadMapHeaderRaw(x));

            {
                var block = new object[] { 1, new[] { 1, 10, 100 }, 100 };
                var bytes = serializer.Serialize(block);
                var stream = new MemoryStream(bytes);
                MessagePackBinary.ReadNext(stream); // array(first)
                MessagePackBinary.ReadNext(stream); // int
                MessagePackBinary.ReadNextBlock(stream); // skip array
                MessagePackBinary.ReadInt32(stream).Is(100);
            }
            {
                var block = new object[] { 1, new Dictionary<int, int> { { 1, 10 }, { 111, 200 } }, 100 };
                var bytes = serializer.Serialize(block);
                var stream = new MemoryStream(bytes);
                MessagePackBinary.ReadNext(stream);
                MessagePackBinary.ReadNext(stream);
                MessagePackBinary.ReadNextBlock(stream);
                MessagePackBinary.ReadInt32(stream).Is(100);
            }
        }

        [Fact]
        public void Standard()
        {
            var o = new SimpleIntKeyData()
            {
                Prop1 = 100,
                Prop2 = ByteEnum.C,
                Prop3 = "abcde",
                Prop4 = new SimlpeStringKeyData
                {
                    Prop1 = 99999,
                    Prop2 = ByteEnum.E,
                    Prop3 = 3
                },
                Prop5 = new SimpleStructIntKeyData
                {
                    X = 100,
                    Y = 300,
                    BytesSpecial = new byte[] { 9, 99, 122 }
                },
                Prop6 = new SimpleStructStringKeyData
                {
                    X = 9999,
                    Y = new[] { 1, 10, 100 }
                },
                BytesSpecial = new byte[] { 1, 4, 6 }
            };


            var bytes = serializer.Serialize(o);
            var ms = new MemoryStream(bytes);

            MessagePackBinary.ReadArrayHeader(ms).Is(7);
            MessagePackBinary.ReadInt32(ms).Is(100);
            MessagePackBinary.ReadByte(ms).Is((byte)ByteEnum.C);
            MessagePackBinary.ReadString(ms).Is("abcde");

            MessagePackBinary.ReadMapHeader(ms).Is(3);
            MessagePackBinary.ReadString(ms).Is("Prop1");
            MessagePackBinary.ReadInt32(ms).Is(99999);
            MessagePackBinary.ReadString(ms).Is("Prop2");
            MessagePackBinary.ReadByte(ms).Is((byte)ByteEnum.E);
            MessagePackBinary.ReadString(ms).Is("Prop3");
            MessagePackBinary.ReadInt32(ms).Is(3);

            MessagePackBinary.ReadArrayHeader(ms).Is(3);
            MessagePackBinary.ReadInt32(ms).Is(100);
            MessagePackBinary.ReadInt32(ms).Is(300);
            MessagePackBinary.ReadBytes(ms).Is(new byte[] { 9, 99, 122 });

            MessagePackBinary.ReadMapHeader(ms).Is(2);
            MessagePackBinary.ReadString(ms).Is("key-X");
            MessagePackBinary.ReadInt32(ms).Is(9999);
            MessagePackBinary.ReadString(ms).Is("key-Y");
            MessagePackBinary.ReadArrayHeader(ms).Is(3);
            MessagePackBinary.ReadInt32(ms).Is(1);
            MessagePackBinary.ReadInt32(ms).Is(10);
            MessagePackBinary.ReadInt32(ms).Is(100);

            MessagePackBinary.ReadBytes(ms).Is(new byte[] { 1, 4, 6 });
        }

        [Fact]
        public void ReadStrictDeserialize()
        {
            var ms = new MemoryStream();
            serializer.Serialize(ms, new SimlpeStringKeyData
            {
                Prop1 = 99999,
                Prop2 = ByteEnum.E,
                Prop3 = 3
            });
            serializer.Serialize(ms, new SimpleStructStringKeyData
            {
                X = 9999,
                Y = new[] { 1, 10, 100 }
            });

            ms.Position = 0;

            var d = serializer.Deserialize<SimlpeStringKeyData>(ms, readStrict: true);
            d.Prop1.Is(99999); d.Prop2.Is(ByteEnum.E); d.Prop3.Is(3);

            var d2 = (SimpleStructStringKeyData)nonGenericSerializer.Deserialize(typeof(SimpleStructStringKeyData), ms, readStrict: true);
            d2.X.Is(9999); d2.Y.Is(new[] { 1, 10, 100 });
        }

        [Fact]
        public void ReadStrictDeserializeLZ4()
        {
            var ms = new MemoryStream();
            lz4Serializer.Serialize(ms, new SimlpeStringKeyData
            {
                Prop1 = 99999,
                Prop2 = ByteEnum.E,
                Prop3 = 3
            });
            lz4Serializer.Serialize(ms, new string('a', 100000));
            lz4Serializer.Serialize(ms, new SimpleStructStringKeyData
            {
                X = 9999,
                Y = new[] { 1, 10, 100 }
            });

            ms.Position = 0;

            var d = lz4Serializer.Deserialize<SimlpeStringKeyData>(ms, readStrict: true);
            d.Prop1.Is(99999); d.Prop2.Is(ByteEnum.E); d.Prop3.Is(3);

            var ds = lz4Serializer.Deserialize<string>(ms, readStrict: true);
            ds.Is(new string('a', 100000));

            var d2 = (SimpleStructStringKeyData)lz4nonGenericSerializer.Deserialize(typeof(SimpleStructStringKeyData), ms, readStrict: true);
            d2.X.Is(9999); d2.Y.Is(new[] { 1, 10, 100 });
        }
    }
}
