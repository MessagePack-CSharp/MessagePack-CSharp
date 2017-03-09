using System;
using RuntimeUnitTestToolkit;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessagePack.UnityClient.Tests
{
    public class LZ4Test
    {
        public void TestSmall()
        {
            MessagePackBinary.GetMessagePackType(LZ4MessagePackSerializer.Serialize(100), 0).Is(MessagePackType.Integer);
            MessagePackBinary.GetMessagePackType(LZ4MessagePackSerializer.Serialize("test"), 0).Is(MessagePackType.String);
            MessagePackBinary.GetMessagePackType(LZ4MessagePackSerializer.Serialize(false), 0).Is(MessagePackType.Boolean);
        }

        public void CompressionData()
        {
            var originalData = Enumerable.Range(1, 1000).Select(x => x).ToArray();
            var lz4Data = LZ4MessagePackSerializer.Serialize(originalData);

            MessagePackBinary.GetMessagePackType(lz4Data, 0).Is(MessagePackType.Extension);
            int r;
            var header = MessagePackBinary.ReadExtensionFormatHeader(lz4Data, 0, out r);
            header.TypeCode.Is((sbyte)LZ4MessagePackSerializer.ExtensionTypeCode);

            var decompress = LZ4MessagePackSerializer.Deserialize<int[]>(lz4Data);
            decompress.IsCollection(originalData);


        }

        public void PrimitiveCompression()
        {
            var data = Encoding.UTF8.GetBytes("あいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそ");
            var data2 = LZ4.LZ4Codec.Encode32(data, 0, data.Length);
            var data3 = LZ4.LZ4Codec.Encode64(data, 0, data.Length);
            var bytes = new byte[LZ4.LZ4Codec.MaximumOutputLength(data.Length)];
            var data4Len = LZ4.LZ4Codec.Encode64(data, 0, data.Length, bytes, 0, bytes.Length);

            var out1 = new byte[data.Length];
            var out2 = new byte[data.Length];
            var out3 = new byte[data.Length];

            bytes = MessagePackBinary.FastCloneWithResize(bytes, data4Len);
            var len1 = LZ4.LZ4Codec.Decode32(data2, 0, data2.Length, out1, 0, out1.Length, true);
            var len2 = LZ4.LZ4Codec.Decode64(data3, 0, data3.Length, out2, 0, out2.Length, true);
            var len3 = LZ4.LZ4Codec.Decode(bytes, 0, bytes.Length, out3, 0, out3.Length, true);

            var str1 = Encoding.UTF8.GetString(out1);
            var str2 = Encoding.UTF8.GetString(out2);
            var str3 = Encoding.UTF8.GetString(out3);

            str1.Is("あいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそ");
            str2.Is("あいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそ");
            str3.Is("あいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそ");
        }

        public void PrimitiveCompression2()
        {
            var originalData = Enumerable.Range(1, 1000).Select(x => x).ToArray();
            var data = MessagePackSerializer.Serialize(originalData);

            var data2 = LZ4.LZ4Codec.Encode32(data, 0, data.Length);
            var data3 = LZ4.LZ4Codec.Encode64(data, 0, data.Length);
            var bytes = new byte[LZ4.LZ4Codec.MaximumOutputLength(data.Length)];
            var data4Len = LZ4.LZ4Codec.Encode64(data, 0, data.Length, bytes, 0, bytes.Length);

            var out1 = new byte[data.Length];
            var out2 = new byte[data.Length];
            var out3 = new byte[data.Length];

            bytes = MessagePackBinary.FastCloneWithResize(bytes, data4Len);
            var len1 = LZ4.LZ4Codec.Decode32(data2, 0, data2.Length, out1, 0, out1.Length, true);
            var len2 = LZ4.LZ4Codec.Decode64(data3, 0, data3.Length, out2, 0, out2.Length, true);
            var len3 = LZ4.LZ4Codec.Decode(bytes, 0, bytes.Length, out3, 0, out3.Length, true);

            MessagePackSerializer.Deserialize<int[]>(out1).IsCollection(originalData);
            MessagePackSerializer.Deserialize<int[]>(out2).IsCollection(originalData);
            MessagePackSerializer.Deserialize<int[]>(out3).IsCollection(originalData);
        }
    }
}
