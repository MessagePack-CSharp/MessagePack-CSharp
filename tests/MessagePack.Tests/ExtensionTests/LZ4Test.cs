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
        private MessagePackSerializer serializer = new MessagePackSerializer();
        private LZ4MessagePackSerializer lz4Serializer = new LZ4MessagePackSerializer();
        private LZ4MessagePackSerializer.NonGeneric lz4NonGenericSerializer = new LZ4MessagePackSerializer.NonGeneric();

        T Convert<T>(T value)
        {
            var resolver = new WithImmutableDefaultResolver();
            return lz4Serializer.Deserialize<T>(lz4Serializer.Serialize(value, resolver), resolver);
        }


        [Fact]
        public void TestSmall()
        {
            // small size binary don't use LZ4 Encode
            MessagePackBinary.GetMessagePackType(lz4Serializer.Serialize(100), 0).Is(MessagePackType.Integer);
            MessagePackBinary.GetMessagePackType(lz4Serializer.Serialize("test"), 0).Is(MessagePackType.String);
            MessagePackBinary.GetMessagePackType(lz4Serializer.Serialize(false), 0).Is(MessagePackType.Boolean);
        }

        [Fact]
        public void CompressionData()
        {
            var originalData = Enumerable.Range(1, 1000).Select(x => x).ToArray();

            var lz4Data = lz4Serializer.Serialize(originalData);

            MessagePackBinary.GetMessagePackType(lz4Data, 0).Is(MessagePackType.Extension);
            int r;
            var header = MessagePackBinary.ReadExtensionFormatHeader(lz4Data, 0, out r);
            header.TypeCode.Is((sbyte)LZ4MessagePackSerializer.ExtensionTypeCode);

            var decompress = lz4Serializer.Deserialize<int[]>(lz4Data);

            decompress.Is(originalData);
        }

        [Fact]
        public void NonGenericAPI()
        {
            var originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();

            var lz4Data = lz4NonGenericSerializer.Serialize(typeof(FirstSimpleData[]), originalData);

            MessagePackBinary.GetMessagePackType(lz4Data, 0).Is(MessagePackType.Extension);
            int r;
            var header = MessagePackBinary.ReadExtensionFormatHeader(lz4Data, 0, out r);
            header.TypeCode.Is((sbyte)LZ4MessagePackSerializer.ExtensionTypeCode);

            var decompress = lz4NonGenericSerializer.Deserialize(typeof(FirstSimpleData[]), lz4Data);

            decompress.IsStructuralEqual(originalData);
        }

        [Fact]
        public void StreamAPI()
        {
            var originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();

            var ms = new MemoryStream();
            lz4NonGenericSerializer.Serialize(typeof(FirstSimpleData[]), ms, originalData);

            var lz4normal = lz4NonGenericSerializer.Serialize(typeof(FirstSimpleData[]), originalData);

            ms.Position = 0;

            lz4normal.SequenceEqual(ms.ToArray()).IsTrue();

            var decompress1 = lz4NonGenericSerializer.Deserialize(typeof(FirstSimpleData[]), ms.ToArray());
            var decompress2 = lz4NonGenericSerializer.Deserialize(typeof(FirstSimpleData[]), lz4normal);
            var decompress3 = lz4NonGenericSerializer.Deserialize(typeof(FirstSimpleData[]), ms);

            decompress1.IsStructuralEqual(originalData);
            decompress2.IsStructuralEqual(originalData);
            decompress3.IsStructuralEqual(originalData);
        }

        [Fact]
        public void ArraySegmentAPI()
        {
            var originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();

            var ms = new MemoryStream();
            lz4NonGenericSerializer.Serialize(typeof(FirstSimpleData[]), ms, originalData);
            ms.Position = 0;

            var lz4normal = lz4Serializer.Serialize(originalData);

            var paddingOffset = 10;
            var paddedLz4Normal = new byte[lz4normal.Length + paddingOffset + paddingOffset];
            Array.Copy(lz4normal,0, paddedLz4Normal, paddingOffset, lz4normal.Length);

            var decompress1 = lz4NonGenericSerializer.Deserialize(typeof(FirstSimpleData[]), ms.ToArray());
            var decompress2 = lz4NonGenericSerializer.Deserialize(typeof(FirstSimpleData[]), lz4normal);
            var decompress3 = lz4NonGenericSerializer.Deserialize(typeof(FirstSimpleData[]), ms);
            var decompress4 = lz4Serializer.Deserialize<FirstSimpleData[]>(lz4normal);
            var decompress5 = lz4Serializer.Deserialize<FirstSimpleData[]>(new ArraySegment<byte>(lz4normal));
            var decompress6 = lz4Serializer.Deserialize<FirstSimpleData[]>(new ArraySegment<byte>(paddedLz4Normal, paddingOffset, lz4normal.Length));

            decompress1.IsStructuralEqual(originalData);
            decompress2.IsStructuralEqual(originalData);
            decompress3.IsStructuralEqual(originalData);
            decompress4.IsStructuralEqual(originalData);
            decompress5.IsStructuralEqual(originalData);
            decompress6.IsStructuralEqual(originalData);
        }

        [Fact]
        public void SerializeToBlock()
        {
            var originalData = Enumerable.Range(1, 1000).Select(x => x).ToArray();

            byte[] bytes = null;

            var len = lz4Serializer.SerializeToBlock(ref bytes, 0, originalData, lz4Serializer.DefaultResolver);
            var lz4Data = lz4Serializer.Serialize(originalData);

            len.Is(lz4Data.Length);

            for (int i = 0; i < len; i++)
            {
                if (bytes[i] != lz4Data[i]) throw new AssertException("not same");
            }
        }

        [Fact]
        public void Decode()
        {
            var originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();
            var simple = lz4Serializer.Serialize(100);
            var complex = lz4Serializer.Serialize(originalData);

            var msgpack1 = LZ4MessagePackSerializer.Decode(simple);
            var msgpack2 = LZ4MessagePackSerializer.Decode(complex);

            serializer.Deserialize<int>(msgpack1).Is(100);
            serializer.Deserialize<FirstSimpleData[]>(msgpack2).IsStructuralEqual(originalData);
        }
    }
}
