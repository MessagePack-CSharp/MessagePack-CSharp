// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Resolvers;
using Microsoft;
using MsgPack.Serialization;
using Nerdbank.Streams;
using SharedData;
using Xunit;

namespace MessagePack.Tests
{
    public class MessagePackSerializerTest
    {
        [Fact]
        public void NonGeneric()
        {
            var data = new FirstSimpleData { Prop1 = 9, Prop2 = "hoge", Prop3 = 999 };
            Type t = typeof(FirstSimpleData);
            var ms = new MemoryStream();
            var writerBytes = new Sequence<byte>();
            var writer = new MessagePackWriter(writerBytes);

            var data1 = MessagePackSerializer.Deserialize(t, MessagePackSerializer.Serialize(t, data)) as FirstSimpleData;
            var data2 = MessagePackSerializer.Deserialize(t, MessagePackSerializer.Serialize(t, data, MessagePackSerializer.DefaultOptions)) as FirstSimpleData;

            MessagePackSerializer.Serialize(t, ms, data);
            ms.Position = 0;
            var data3 = MessagePackSerializer.Deserialize(t, ms) as FirstSimpleData;

            ms = new MemoryStream();
            MessagePackSerializer.Serialize(t, ms, data, MessagePackSerializer.DefaultOptions);
            ms.Position = 0;
            var data4 = MessagePackSerializer.Deserialize(t, ms, MessagePackSerializer.DefaultOptions) as FirstSimpleData;

#if ENABLE_IL2CPP
            var data5 = data4;
#else
            MessagePackSerializer.Serialize(t, ref writer, data, MessagePackSerializer.DefaultOptions);
            writer.Flush();
            var reader = new MessagePackReader(writerBytes.AsReadOnlySequence);
            var data5 = MessagePackSerializer.Deserialize(t, ref reader, MessagePackSerializer.DefaultOptions) as FirstSimpleData;
#endif

            new[] { data1.Prop1, data2.Prop1, data3.Prop1, data4.Prop1, data5.Prop1 }.Distinct().Is(data.Prop1);
            new[] { data1.Prop2, data2.Prop2, data3.Prop2, data4.Prop2, data5.Prop2 }.Distinct().Is(data.Prop2);
            new[] { data1.Prop3, data2.Prop3, data3.Prop3, data4.Prop3, data5.Prop3 }.Distinct().Is(data.Prop3);
        }

        [Fact]
        public void NonGeneric_BufferWriter_ReadOnlySequence()
        {
            var data = new FirstSimpleData { Prop1 = 9, Prop2 = "hoge", Prop3 = 999 };
            Type t = typeof(FirstSimpleData);
            var ms = new MemoryStream();
            var writerBytes = new Sequence<byte>();

            var data1 = MessagePackSerializer.Deserialize(t, MessagePackSerializer.Serialize(t, data)) as FirstSimpleData;
            var data2 = MessagePackSerializer.Deserialize(t, MessagePackSerializer.Serialize(t, data, MessagePackSerializer.DefaultOptions)) as FirstSimpleData;

            MessagePackSerializer.Serialize(t, ms, data);
            ms.Position = 0;
            var data3 = MessagePackSerializer.Deserialize(t, ms) as FirstSimpleData;

            ms = new MemoryStream();
            MessagePackSerializer.Serialize(t, ms, data, MessagePackSerializer.DefaultOptions);
            ms.Position = 0;
            var data4 = MessagePackSerializer.Deserialize(t, ms, MessagePackSerializer.DefaultOptions) as FirstSimpleData;

            MessagePackSerializer.Serialize(t, writerBytes, data, MessagePackSerializer.DefaultOptions);
            var data5 = MessagePackSerializer.Deserialize(t, writerBytes.AsReadOnlySequence, MessagePackSerializer.DefaultOptions) as FirstSimpleData;

            new[] { data1.Prop1, data2.Prop1, data3.Prop1, data4.Prop1, data5.Prop1 }.Distinct().Is(data.Prop1);
            new[] { data1.Prop2, data2.Prop2, data3.Prop2, data4.Prop2, data5.Prop2 }.Distinct().Is(data.Prop2);
            new[] { data1.Prop3, data2.Prop3, data3.Prop3, data4.Prop3, data5.Prop3 }.Distinct().Is(data.Prop3);
        }

#if !UNITY_2018_3_OR_NEWER

        /* Unity's NUnit currently no supported Task test. */

        [Fact]
        public async Task NonGeneric_Async()
        {
            var data = new FirstSimpleData { Prop1 = 9, Prop2 = "hoge", Prop3 = 999 };
            Type t = typeof(FirstSimpleData);
            var ms = new MemoryStream();

            await MessagePackSerializer.SerializeAsync(t, ms, data);
            ms.Position = 0;
            var data2 = (FirstSimpleData)await MessagePackSerializer.DeserializeAsync(t, ms);

            Assert.Equal(data, data2);
        }

#endif

        [Fact]
        public void StreamAPI()
        {
            FirstSimpleData[] originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();

            var ms = new MemoryStream();
            MessagePackSerializer.Serialize(ms, originalData); // serialize to stream

            var normal = MessagePackSerializer.Serialize(originalData);

            ms.Position = 0;

            normal.SequenceEqual(ms.ToArray()).IsTrue();

            var decompress1 = MessagePackSerializer.Deserialize<FirstSimpleData[]>(ms.ToArray());
            var decompress2 = MessagePackSerializer.Deserialize<FirstSimpleData[]>(normal);
            var decompress3 = MessagePackSerializer.Deserialize<FirstSimpleData[]>(ms);
            ms.Position = 0;
            var onmore = new StreamWrapper(ms);
            var decompress4 = MessagePackSerializer.Deserialize<FirstSimpleData[]>(onmore);

            decompress1.IsStructuralEqual(originalData);
            decompress2.IsStructuralEqual(originalData);
            decompress3.IsStructuralEqual(originalData);
            decompress4.IsStructuralEqual(originalData);
        }

        [Fact]
        public void SerializeAndDeserialize_MultipleValues_NonSeekableStream()
        {
            var ms = new MemoryStream();
            Stream stream = new StreamWrapper(ms, canSeek: false);

            MessagePackSerializer.Serialize(stream, 1);
            MessagePackSerializer.Serialize(stream, 2);

            ms.Position = 0;
            Assert.Equal(1, MessagePackSerializer.Deserialize<int>(stream));
            var ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<int>(stream));
            Assert.IsType<EndOfStreamException>(ex.InnerException);
        }

        [Fact]
        public async Task SerializeAndDeserializeAsync_MultipleValues_NonSeekableStream()
        {
            var ms = new MemoryStream();
            Stream stream = new StreamWrapper(ms, canSeek: false);

            await MessagePackSerializer.SerializeAsync(stream, 1);
            await MessagePackSerializer.SerializeAsync(stream, 2);

            ms.Position = 0;
            Assert.Equal(1, await MessagePackSerializer.DeserializeAsync<int>(stream));
            var ex = await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await MessagePackSerializer.DeserializeAsync<int>(stream));
            Assert.IsType<EndOfStreamException>(ex.InnerException);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SerializeAndDeserialize_MultipleValues_SeekableStream(bool useMemoryStream)
        {
            Stream stream = new MemoryStream();
            if (!useMemoryStream)
            {
                // Hide the MemoryStream so MessagePack doesn't treat it specially.
                stream = new StreamWrapper(stream);
            }

            MessagePackSerializer.Serialize(stream, 1);
            MessagePackSerializer.Serialize(stream, 2);
            MessagePackSerializer.Serialize(stream, 3);

            stream.Position = 0;
            Assert.Equal(1, MessagePackSerializer.Deserialize<int>(stream));
            Assert.Equal(2, MessagePackSerializer.Deserialize<int>(stream));
            Assert.Equal(3, MessagePackSerializer.Deserialize<int>(stream));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SerializeAndDeserializeAsync_MultipleValues_SeekableStream(bool useMemoryStream)
        {
            Stream stream = new MemoryStream();
            if (!useMemoryStream)
            {
                // Hide the MemoryStream so MessagePack doesn't treat it specially.
                stream = new StreamWrapper(stream);
            }

            await MessagePackSerializer.SerializeAsync(stream, 1);
            await MessagePackSerializer.SerializeAsync(stream, 2);
            await MessagePackSerializer.SerializeAsync(stream, 3);

            stream.Position = 0;
            Assert.Equal(1, await MessagePackSerializer.DeserializeAsync<int>(stream));
            Assert.Equal(2, await MessagePackSerializer.DeserializeAsync<int>(stream));
            Assert.Equal(3, await MessagePackSerializer.DeserializeAsync<int>(stream));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StackDepthCheck_Arrays(bool convertToJson)
        {
            var (sequence, options) = StackDepthCheck_Helper((ref MessagePackWriter writer) => writer.WriteArrayHeader(1));
            if (convertToJson)
            {
                AssertConvertToJsonRecursionCheckThrows(sequence, options);
            }
            else
            {
                AssertDeserializationRecursionCheckThrows(sequence, options);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StackDepthCheck_Maps(bool convertToJson)
        {
            var (sequence, options) = StackDepthCheck_Helper((ref MessagePackWriter writer) =>
            {
                writer.WriteMapHeader(1);
                writer.Write(1);
            });
            if (convertToJson)
            {
                AssertConvertToJsonRecursionCheckThrows(sequence, options);
            }
            else
            {
                AssertDeserializationRecursionCheckThrows(sequence, options);
            }
        }

#if !ENABLE_IL2CPP

        [Fact]
        public void StackDepthCheck_DynamicObjectResolver()
        {
            var graph = new RecursiveObjectGraph
            {
                Child1 = new RecursiveObjectGraph
                {
                    Child1 = new RecursiveObjectGraph { },
                },
                Child2 = new RecursiveObjectGraph
                {
                    Child1 = new RecursiveObjectGraph { },
                },
                Child3 = new RecursiveObjectGraph
                {
                    Child1 = new RecursiveObjectGraph { },
                },
            };
            byte[] msgpack = MessagePackSerializer.Serialize(graph, MessagePackSerializerOptions.Standard);

            var options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData.WithMaximumObjectGraphDepth(3));
            MessagePackSerializer.Deserialize<RecursiveObjectGraph>(msgpack, options);

            options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData.WithMaximumObjectGraphDepth(2));
            var ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<RecursiveObjectGraph>(msgpack, options));
            Assert.IsType<InsufficientExecutionStackException>(ex.InnerException);
        }

#endif

        private delegate void WriterHelper(ref MessagePackWriter writer);

        private static (ReadOnlySequence<byte> Sequence, MessagePackSerializerOptions Options) StackDepthCheck_Helper(WriterHelper recursiveWriteOperation)
        {
            const int maxDepth = 3;
            var options = MessagePackSerializerOptions.Standard
                .WithSecurity(MessagePackSecurity.UntrustedData.WithMaximumObjectGraphDepth(maxDepth));

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            for (int i = 0; i <= maxDepth; i++)
            {
                recursiveWriteOperation(ref writer);
            }

            writer.Write(1);
            writer.Flush();

            return (sequence.AsReadOnlySequence, options);
        }

        private static void AssertDeserializationRecursionCheckThrows(ReadOnlySequence<byte> sequence, MessagePackSerializerOptions options)
        {
            var ex = Assert.Throws<MessagePackSerializationException>(() =>
            {
                var reader = new MessagePackReader(sequence);
                return MessagePackSerializer.Deserialize<object>(ref reader, options);
            });
            Assert.IsType<InsufficientExecutionStackException>(ex.InnerException);
        }

        private static void AssertConvertToJsonRecursionCheckThrows(ReadOnlySequence<byte> sequence, MessagePackSerializerOptions options)
        {
            var ex = Assert.Throws<MessagePackSerializationException>(() =>
            {
                var reader = new MessagePackReader(sequence);
                MessagePackSerializer.ConvertToJson(ref reader, new StringWriter(), options);
            });
            Assert.IsType<InsufficientExecutionStackException>(ex.InnerException);
        }

        [DataContract]
        public class RecursiveObjectGraph
        {
            [DataMember]
            public RecursiveObjectGraph Child1 { get; set; }

            [DataMember]
            public RecursiveObjectGraph Child2 { get; set; }

            [DataMember]
            public RecursiveObjectGraph Child3 { get; set; }
        }
    }

    internal class StreamWrapper : Stream
    {
        private readonly Stream stream;
        private readonly bool canSeek;

        internal StreamWrapper(Stream stream)
        {
            Requires.NotNull(stream, nameof(stream));
            this.stream = stream;
            this.canSeek = stream.CanSeek;
        }

        internal StreamWrapper(Stream stream, bool canSeek)
        {
            Requires.NotNull(stream, nameof(stream));
            Requires.Argument(stream.CanSeek || !CanSeek, nameof(canSeek), "Cannot emulate seek on a non-seekable underlying stream.");
            this.stream = stream;
            this.canSeek = canSeek;
        }

        public override bool CanRead => this.stream.CanRead;

        public override bool CanSeek => this.canSeek;

        public override bool CanWrite => this.stream.CanWrite;

        public override long Length => this.stream.Length;

        public override long Position
        {
            get => this.stream.Position;
            set
            {
                if (!this.canSeek)
                {
                    throw new NotSupportedException();
                }

                this.stream.Position = value;
            }
        }

        public override void Flush()
        {
            this.stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!this.canSeek)
            {
                throw new NotSupportedException();
            }

            return this.stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.stream.Write(buffer, offset, count);
        }
    }
}
