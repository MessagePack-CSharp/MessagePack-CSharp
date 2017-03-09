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

            decompress.Is(originalData);


        }

        public void PrimitiveCompression()
        {
            var originalData = Enumerable.Range(1, 1000).Select(x => x).ToArray();
            var data = MessagePackSerializer.Serialize(originalData);
            var data2 = LZ4.LZ4Codec.Encode32(data, 0, data.Length);
            var data3 = LZ4.LZ4Codec.Encode64(data, 0, data.Length);

        }
    }
}
