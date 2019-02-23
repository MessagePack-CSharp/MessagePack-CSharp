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
    }
}
