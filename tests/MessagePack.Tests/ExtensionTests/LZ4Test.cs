using SharedData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests.ExtensionTests
{

    public class LZ4Test
    {
        T Convert<T>(T value)
        {
            var resolver = new WithImmutableDefaultResolver();
            return LZ4MessagePackSerializer.Deserialize<T>(LZ4MessagePackSerializer.Serialize(value, resolver), resolver);
        }


        [Fact]
        public void TestSmall()
        {
            // small size binary don't use LZ4 Encode
            MessagePackBinary.GetMessagePackType(LZ4MessagePackSerializer.Serialize(100), 0).Is(MessagePackType.Integer);
            MessagePackBinary.GetMessagePackType(LZ4MessagePackSerializer.Serialize("test"), 0).Is(MessagePackType.String);
            MessagePackBinary.GetMessagePackType(LZ4MessagePackSerializer.Serialize(false), 0).Is(MessagePackType.Boolean);
        }

        [Fact]
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

        [Fact]
        public void NonGenericAPI()
        {
            var originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();

            var lz4Data = LZ4MessagePackSerializer.NonGeneric.Serialize(typeof(FirstSimpleData[]), originalData);

            MessagePackBinary.GetMessagePackType(lz4Data, 0).Is(MessagePackType.Extension);
            int r;
            var header = MessagePackBinary.ReadExtensionFormatHeader(lz4Data, 0, out r);
            header.TypeCode.Is((sbyte)LZ4MessagePackSerializer.ExtensionTypeCode);

            var decompress = LZ4MessagePackSerializer.NonGeneric.Deserialize(typeof(FirstSimpleData[]), lz4Data);

            decompress.IsStructuralEqual(originalData);
        }

        [Fact]
        public void StreamAPI()
        {
            var originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();

            var ms = new MemoryStream();
            LZ4MessagePackSerializer.NonGeneric.Serialize(typeof(FirstSimpleData[]), ms, originalData);

            var lz4normal = LZ4MessagePackSerializer.NonGeneric.Serialize(typeof(FirstSimpleData[]), originalData);

            ms.Position = 0;

            lz4normal.SequenceEqual(ms.ToArray()).IsTrue();

            var decompress1 = LZ4MessagePackSerializer.NonGeneric.Deserialize(typeof(FirstSimpleData[]), ms.ToArray());
            var decompress2 = LZ4MessagePackSerializer.NonGeneric.Deserialize(typeof(FirstSimpleData[]), lz4normal);
            var decompress3 = LZ4MessagePackSerializer.NonGeneric.Deserialize(typeof(FirstSimpleData[]), ms);

            decompress1.IsStructuralEqual(originalData);
            decompress2.IsStructuralEqual(originalData);
            decompress3.IsStructuralEqual(originalData);
        }
    }
}
