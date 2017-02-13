using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class MessagePackBinaryTest
    {
        (MsgPack.Packer, MsgPack.Unpacker) CreateReference()
        {
            var ms = new MemoryStream();
            var packer = MsgPack.Packer.Create(ms, MsgPack.PackerCompatibilityOptions.None);
            var unpacker = MsgPack.Unpacker.Create(ms);
            return (packer, unpacker);
        }

        [Fact]
        public void Binary()
        {
            (var packer, var unpacker) = CreateReference();

            byte[] bytes = null;
            MessagePackBinary.WriteNil(ref bytes, 0).Is(1);

            packer.PackNull().Position.Is(bytes.Length);
            unpacker.ReadItemData().AsBinary().SequenceEqual(bytes);

            

            //const int offset = 33;
            //var bytes = new byte[100];

            //bytes = new byte[100];
            //BinaryUtil.WriteBoolean(ref bytes, offset, true).Is(1);
            //BinaryUtil.ReadBoolean(ref bytes, offset).IsTrue();

            //bytes = new byte[100];
            //BinaryUtil.WriteByte(ref bytes, offset, (byte)234).Is(1);
            //BinaryUtil.ReadByte(ref bytes, offset).Is((byte)234);

            //bytes = new byte[100];
            //BinaryUtil.WriteChar(ref bytes, offset, 'z').Is(2);
            //BinaryUtil.ReadChar(ref bytes, offset).Is('z');

            //bytes = new byte[100];
            //var now = DateTime.Now.ToUniversalTime();
            //BinaryUtil.WriteDateTime(ref bytes, offset, now).Is(12);
            //BinaryUtil.ReadDateTime(ref bytes, offset).Is(now);

            //bytes = new byte[100];
            //BinaryUtil.WriteDecimal(ref bytes, offset, decimal.MaxValue).Is(16);
            //BinaryUtil.ReadDecimal(ref bytes, offset).Is(decimal.MaxValue);

            //bytes = new byte[100];
            //BinaryUtil.WriteDouble(ref bytes, offset, double.MaxValue).Is(8);
            //BinaryUtil.ReadDouble(ref bytes, offset).Is(double.MaxValue);

            //bytes = new byte[100];
            //BinaryUtil.WriteInt16(ref bytes, offset, short.MaxValue).Is(2);
            //BinaryUtil.ReadInt16(ref bytes, offset).Is(short.MaxValue);

            //bytes = new byte[100];
            //BinaryUtil.WriteInt32(ref bytes, offset, int.MaxValue).Is(4);
            //BinaryUtil.ReadInt32(ref bytes, offset).Is(int.MaxValue);

            //bytes = new byte[100];
            //BinaryUtil.WriteInt32Unsafe(ref bytes, offset, int.MaxValue);
            //BinaryUtil.ReadInt32(ref bytes, offset).Is(int.MaxValue);

            //bytes = new byte[100];
            //BinaryUtil.WriteInt64(ref bytes, offset, long.MaxValue).Is(8);
            //BinaryUtil.ReadInt64(ref bytes, offset).Is(long.MaxValue);

            //bytes = new byte[100];
            //BinaryUtil.WriteSByte(ref bytes, offset, sbyte.MaxValue).Is(1);
            //BinaryUtil.ReadSByte(ref bytes, offset).Is(sbyte.MaxValue);

            //bytes = new byte[100];
            //BinaryUtil.WriteSingle(ref bytes, offset, Single.MaxValue).Is(4);
            //BinaryUtil.ReadSingle(ref bytes, offset).Is(Single.MaxValue);

            //bytes = new byte[100];
            //var c = BinaryUtil.WriteString(ref bytes, offset, "あいうえおかきくけこ");
            //BinaryUtil.ReadString(ref bytes, offset, c).Is("あいうえおかきくけこ");

            //var ts = new TimeSpan(14, 213, 41241);
            //bytes = new byte[100];
            //BinaryUtil.WriteTimeSpan(ref bytes, offset, ts).Is(12);
            //BinaryUtil.ReadTimeSpan(ref bytes, offset).Is(ts);

            //bytes = new byte[100];
            //BinaryUtil.WriteUInt16(ref bytes, offset, UInt16.MaxValue).Is(2);
            //BinaryUtil.ReadUInt16(ref bytes, offset).Is(UInt16.MaxValue);

            //bytes = new byte[100];
            //BinaryUtil.WriteUInt32(ref bytes, offset, UInt32.MaxValue).Is(4);
            //BinaryUtil.ReadUInt32(ref bytes, offset).Is(UInt32.MaxValue);

            //bytes = new byte[100];
            //BinaryUtil.WriteUInt64(ref bytes, offset, UInt64.MaxValue).Is(8);
            //BinaryUtil.ReadUInt64(ref bytes, offset).Is(UInt64.MaxValue);
        }
    }
}
