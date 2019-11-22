// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedData;
using Xunit;

namespace MessagePack.Tests.ExtensionTests
{
    public class LZ4Test
    {
        private T Convert<T>(T value)
        {
            MessagePackSerializerOptions options = MessagePackSerializerOptions.LZ4Standard.WithResolver(new WithImmutableDefaultResolver());
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, options), options);
        }

        [Fact]
        public void TestSmall()
        {
            // small size binary don't use LZ4 Encode
            PeekMessagePackType(MessagePackSerializer.Serialize(100, MessagePackSerializerOptions.LZ4Standard)).Is(MessagePackType.Integer);
            PeekMessagePackType(MessagePackSerializer.Serialize("test", MessagePackSerializerOptions.LZ4Standard)).Is(MessagePackType.String);
            PeekMessagePackType(MessagePackSerializer.Serialize(false, MessagePackSerializerOptions.LZ4Standard)).Is(MessagePackType.Boolean);
        }

        [Fact]
        public void CompressionData()
        {
            var originalData = Enumerable.Range(1, 1000).Select(x => x).ToArray();

            var lz4Data = MessagePackSerializer.Serialize(originalData, MessagePackSerializerOptions.LZ4Standard);

            PeekMessagePackType(lz4Data).Is(MessagePackType.Extension);
            var lz4DataReader = new MessagePackReader(lz4Data);
            ExtensionHeader header = lz4DataReader.ReadExtensionFormatHeader();
            header.TypeCode.Is(ThisLibraryExtensionTypeCodes.LZ4);

            var decompress = MessagePackSerializer.Deserialize<int[]>(lz4Data, MessagePackSerializerOptions.LZ4Standard);

            decompress.Is(originalData);
        }

        [Fact]
        public void NonGenericAPI()
        {
            FirstSimpleData[] originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();

            var lz4Data = MessagePackSerializer.Serialize(typeof(FirstSimpleData[]), originalData, MessagePackSerializerOptions.LZ4Standard);

            PeekMessagePackType(lz4Data).Is(MessagePackType.Extension);
            var lz4DataReader = new MessagePackReader(lz4Data);
            ExtensionHeader header = lz4DataReader.ReadExtensionFormatHeader();
            header.TypeCode.Is(ThisLibraryExtensionTypeCodes.LZ4);

            var decompress = MessagePackSerializer.Deserialize(typeof(FirstSimpleData[]), lz4Data, MessagePackSerializerOptions.LZ4Standard);

            decompress.IsStructuralEqual(originalData);
        }

        [Fact]
        public void StreamAPI()
        {
            FirstSimpleData[] originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();

            var ms = new MemoryStream();
            MessagePackSerializer.Serialize(typeof(FirstSimpleData[]), ms, originalData, MessagePackSerializerOptions.LZ4Standard);

            var lz4normal = MessagePackSerializer.Serialize(typeof(FirstSimpleData[]), originalData, MessagePackSerializerOptions.LZ4Standard);

            ms.Position = 0;

            lz4normal.SequenceEqual(ms.ToArray()).IsTrue();

            var decompress1 = MessagePackSerializer.Deserialize(typeof(FirstSimpleData[]), ms.ToArray(), MessagePackSerializerOptions.LZ4Standard);
            var decompress2 = MessagePackSerializer.Deserialize(typeof(FirstSimpleData[]), lz4normal, MessagePackSerializerOptions.LZ4Standard);
            var decompress3 = MessagePackSerializer.Deserialize(typeof(FirstSimpleData[]), ms, MessagePackSerializerOptions.LZ4Standard);

            decompress1.IsStructuralEqual(originalData);
            decompress2.IsStructuralEqual(originalData);
            decompress3.IsStructuralEqual(originalData);
        }

        [Fact]
        public void ArraySegmentAPI()
        {
            FirstSimpleData[] originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();

            var ms = new MemoryStream();
            MessagePackSerializer.Serialize(typeof(FirstSimpleData[]), ms, originalData, MessagePackSerializerOptions.LZ4Standard);
            ms.Position = 0;

            var lz4normal = MessagePackSerializer.Serialize(originalData, MessagePackSerializerOptions.LZ4Standard);

            var paddingOffset = 10;
            var paddedLz4Normal = new byte[lz4normal.Length + paddingOffset + paddingOffset];
            Array.Copy(lz4normal, 0, paddedLz4Normal, paddingOffset, lz4normal.Length);

            var decompress1 = MessagePackSerializer.Deserialize(typeof(FirstSimpleData[]), ms.ToArray(), MessagePackSerializerOptions.LZ4Standard);
            var decompress2 = MessagePackSerializer.Deserialize(typeof(FirstSimpleData[]), lz4normal, MessagePackSerializerOptions.LZ4Standard);
            var decompress3 = MessagePackSerializer.Deserialize(typeof(FirstSimpleData[]), ms, MessagePackSerializerOptions.LZ4Standard);
            FirstSimpleData[] decompress4 = MessagePackSerializer.Deserialize<FirstSimpleData[]>(lz4normal, MessagePackSerializerOptions.LZ4Standard);
            FirstSimpleData[] decompress5 = MessagePackSerializer.Deserialize<FirstSimpleData[]>(new ArraySegment<byte>(lz4normal), MessagePackSerializerOptions.LZ4Standard);
            FirstSimpleData[] decompress6 = MessagePackSerializer.Deserialize<FirstSimpleData[]>(new ArraySegment<byte>(paddedLz4Normal, paddingOffset, lz4normal.Length), MessagePackSerializerOptions.LZ4Standard);

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
            {
                list.Add(i);
            }

            var data = MessagePackSerializer.Serialize(list, MessagePackSerializerOptions.LZ4Standard);
            data.Length.IsNot(11);

            List<long> testList = MessagePackSerializer.Deserialize<List<long>>(data, MessagePackSerializerOptions.LZ4Standard);
            testList.Count.Is(list.Count);
        }
    }
}
