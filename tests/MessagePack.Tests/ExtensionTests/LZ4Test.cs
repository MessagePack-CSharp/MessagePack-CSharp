using SharedData;
using System;
using System.Buffers;
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
            PeekMessagePackType(lz4Serializer.Serialize(100)).Is(MessagePackType.Integer);
            PeekMessagePackType(lz4Serializer.Serialize("test")).Is(MessagePackType.String);
            PeekMessagePackType(lz4Serializer.Serialize(false)).Is(MessagePackType.Boolean);
        }

        [Fact]
        public void CompressionData()
        {
            var originalData = Enumerable.Range(1, 1000).Select(x => x).ToArray();

            var lz4Data = lz4Serializer.Serialize(originalData);

            PeekMessagePackType(lz4Data).Is(MessagePackType.Extension);
            var lz4DataReader = new MessagePackReader(lz4Data);
            var header = lz4DataReader.ReadExtensionFormatHeader();
            header.TypeCode.Is(LZ4MessagePackSerializer.ExtensionTypeCode);

            var decompress = lz4Serializer.Deserialize<int[]>(lz4Data);

            decompress.Is(originalData);
        }

        [Fact]
        public void NonGenericAPI()
        {
            var originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();

            var lz4Data = lz4NonGenericSerializer.Serialize(typeof(FirstSimpleData[]), originalData);

            PeekMessagePackType(lz4Data).Is(MessagePackType.Extension);
            var lz4DataReader = new MessagePackReader(lz4Data);
            var header = lz4DataReader.ReadExtensionFormatHeader();
            header.TypeCode.Is(LZ4MessagePackSerializer.ExtensionTypeCode);

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

        private static MessagePackType PeekMessagePackType(byte[] msgpackBuffer)
        {
            var reader = new MessagePackReader(new ReadOnlySequence<byte>(msgpackBuffer));
            return reader.NextMessagePackType;
        }

        [Fact]
        public void BigList()
        {
            int capacity = 21974;
            List<long> list = new List<long>(capacity);
            for (long i = 0; i < capacity; i++)
                list.Add(i);
            var data = lz4Serializer.Serialize(list);
            data.Length.IsNot(11);

            var testList = lz4Serializer.Deserialize<List<long>>(data);
            testList.Count.Is(list.Count);
        }
    }
}
