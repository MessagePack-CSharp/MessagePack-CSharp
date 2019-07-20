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
            new MessagePackReader(MessagePackSerializer.Serialize(100, MessagePackSerializerOptions.LZ4Default)).NextMessagePackType.Is(MessagePackType.Integer);
            new MessagePackReader(MessagePackSerializer.Serialize("test", MessagePackSerializerOptions.LZ4Default)).NextMessagePackType.Is(MessagePackType.String);
            new MessagePackReader(MessagePackSerializer.Serialize(false, MessagePackSerializerOptions.LZ4Default)).NextMessagePackType.Is(MessagePackType.Boolean);
        }

        public void CompressionData()
        {
            var originalData = Enumerable.Range(1, 1000).Select(x => x).ToArray();
            var lz4Data = MessagePackSerializer.Serialize(originalData, MessagePackSerializerOptions.LZ4Default);

            var reader = new MessagePackReader(lz4Data);
            reader.NextMessagePackType.Is(MessagePackType.Extension);
            var header = reader.ReadExtensionFormatHeader();
            header.TypeCode.Is((sbyte)MessagePackSerializer.LZ4ExtensionTypeCode);

            var decompress = MessagePackSerializer.Deserialize<int[]>(lz4Data, MessagePackSerializerOptions.LZ4Default);
            decompress.IsCollection(originalData);
        }

        ////public void PrimitiveCompression()
        ////{
        ////    var data = Encoding.UTF8.GetBytes("あいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそ");
        ////    var data2 = LZ4.LZ4Codec.Encode32Unsafe(data, 0, data.Length);
        ////    var data3 = LZ4.LZ4Codec.Encode64Unsafe(data, 0, data.Length);
        ////    var bytes = new byte[LZ4.LZ4Codec.MaximumOutputLength(data.Length)];
        ////    var data4Len = LZ4.LZ4Codec.Encode64Unsafe(data, 0, data.Length, bytes, 0, bytes.Length);

        ////    var out1 = new byte[data.Length];
        ////    var out2 = new byte[data.Length];
        ////    var out3 = new byte[data.Length];

        ////    bytes = MessagePackBinary.FastCloneWithResize(bytes, data4Len);
        ////    var len1 = LZ4.LZ4Codec.Decode32Unsafe(data2, 0, data2.Length, out1, 0, out1.Length, true);
        ////    var len2 = LZ4.LZ4Codec.Decode64Unsafe(data3, 0, data3.Length, out2, 0, out2.Length, true);
        ////    var len3 = LZ4.LZ4Codec.Decode(bytes, 0, bytes.Length, out3, 0, out3.Length, true);

        ////    var str1 = Encoding.UTF8.GetString(out1);
        ////    var str2 = Encoding.UTF8.GetString(out2);
        ////    var str3 = Encoding.UTF8.GetString(out3);

        ////    str1.Is("あいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそ");
        ////    str2.Is("あいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそ");
        ////    str3.Is("あいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそあいうえおかきくけこさしすせそ");
        ////}

        ////public void PrimitiveCompression2()
        ////{
        ////    var originalData = Enumerable.Range(1, 1000).Select(x => x).ToArray();
        ////    var data = MessagePackSerializer.Serialize(originalData);

        ////    var data2 = LZ4.LZ4Codec.Encode32(data, 0, data.Length);
        ////    var data3 = LZ4.LZ4Codec.Encode64(data, 0, data.Length);
        ////    var bytes = new byte[LZ4.LZ4Codec.MaximumOutputLength(data.Length)];
        ////    var data4Len = LZ4.LZ4Codec.Encode64(data, 0, data.Length, bytes, 0, bytes.Length);

        ////    var out1 = new byte[data.Length];
        ////    var out2 = new byte[data.Length];
        ////    var out3 = new byte[data.Length];

        ////    bytes = MessagePackBinary.FastCloneWithResize(bytes, data4Len);
        ////    var len1 = LZ4.LZ4Codec.Decode32(data2, 0, data2.Length, out1, 0, out1.Length, true);
        ////    var len2 = LZ4.LZ4Codec.Decode64(data3, 0, data3.Length, out2, 0, out2.Length, true);
        ////    var len3 = LZ4.LZ4Codec.Decode(bytes, 0, bytes.Length, out3, 0, out3.Length, true);

        ////    MessagePackSerializer.Deserialize<int[]>(out1).IsCollection(originalData);
        ////    MessagePackSerializer.Deserialize<int[]>(out2).IsCollection(originalData);
        ////    MessagePackSerializer.Deserialize<int[]>(out3).IsCollection(originalData);
        ////}
    }
}
