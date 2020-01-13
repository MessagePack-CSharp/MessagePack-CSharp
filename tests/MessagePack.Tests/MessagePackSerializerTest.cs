using MessagePack.Resolvers;
using SharedData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class MessagePackSerializerTest
    {
        T ConvertNonGeneric<T>(T obj)
        {
            var t = obj.GetType();
            return (T)MessagePackSerializer.NonGeneric.Deserialize(t, MessagePackSerializer.NonGeneric.Serialize(t, obj));
        }

        [Fact]
        public void NonGeneric()
        {
            var data = new FirstSimpleData { Prop1 = 9, Prop2 = "hoge", Prop3 = 999 };
            var t = typeof(FirstSimpleData);
            var ms = new MemoryStream();

            var data1 = MessagePackSerializer.NonGeneric.Deserialize(t, MessagePackSerializer.NonGeneric.Serialize(t, data)) as FirstSimpleData;
            var data2 = MessagePackSerializer.NonGeneric.Deserialize(t, MessagePackSerializer.NonGeneric.Serialize(t, data, StandardResolver.Instance)) as FirstSimpleData;

            MessagePackSerializer.NonGeneric.Serialize(t, ms, data);
            ms.Position = 0;
            var data3 = MessagePackSerializer.NonGeneric.Deserialize(t, ms) as FirstSimpleData;

            ms = new MemoryStream();
            MessagePackSerializer.NonGeneric.Serialize(t, ms, data, StandardResolver.Instance);
            ms.Position = 0;
            var data4 = MessagePackSerializer.NonGeneric.Deserialize(t, ms, StandardResolver.Instance) as FirstSimpleData;

            new[] { data1.Prop1, data2.Prop1, data3.Prop1, data4.Prop1 }.Distinct().Is(data.Prop1);
            new[] { data1.Prop2, data2.Prop2, data3.Prop2, data4.Prop2 }.Distinct().Is(data.Prop2);
            new[] { data1.Prop3, data2.Prop3, data3.Prop3, data4.Prop3 }.Distinct().Is(data.Prop3);
        }

        [Fact]
        public void StreamAPI()
        {
            var originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();

            var ms = new MemoryStream();
            MessagePackSerializer.NonGeneric.Serialize(typeof(FirstSimpleData[]), ms, originalData);

            var normal = MessagePackSerializer.NonGeneric.Serialize(typeof(FirstSimpleData[]), originalData);

            ms.Position = 0;

            normal.SequenceEqual(ms.ToArray()).IsTrue();

            var decompress1 = MessagePackSerializer.NonGeneric.Deserialize(typeof(FirstSimpleData[]), ms.ToArray());
            var decompress2 = MessagePackSerializer.NonGeneric.Deserialize(typeof(FirstSimpleData[]), normal);
            var decompress3 = MessagePackSerializer.NonGeneric.Deserialize(typeof(FirstSimpleData[]), ms);
            ms.Position = 0;
            var onmore = new NonMemoryStream(ms);
            var decompress4 = MessagePackSerializer.NonGeneric.Deserialize(typeof(FirstSimpleData[]), onmore);

            decompress1.IsStructuralEqual(originalData);
            decompress2.IsStructuralEqual(originalData);
            decompress3.IsStructuralEqual(originalData);
            decompress4.IsStructuralEqual(originalData);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StackDepthCheck_Arrays(bool convertToJson)
        {
            var (buffer, depthExceeded) = StackDepthCheck_Helper((ref byte[] b, int offset) => MessagePackBinary.WriteArrayHeader(ref b, offset, 1));

            if (convertToJson)
            {
                AssertConvertToJsonRecursionCheckThrows(buffer, depthExceeded);
            }
            else
            {
                AssertDeserializationRecursionCheckThrows(buffer, depthExceeded);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StackDepthCheck_Maps(bool convertToJson)
        {
            var (sequence, options) = StackDepthCheck_Helper((ref byte[] b, int offset) =>
            {
                int written = MessagePackBinary.WriteMapHeader(ref b, offset, 1);
                written += MessagePackBinary.WriteByte(ref b, offset + written, 1);
                return written;
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
            byte[] msgpack = MessagePackSerializer.Serialize(graph);

            MessagePackSecurity.Active = MessagePackSecurity.UntrustedData.WithMaximumObjectGraphDepth(3);
            try
            {
                MessagePackSerializer.Deserialize<RecursiveObjectGraph>(msgpack);

                MessagePackSecurity.Active = MessagePackSecurity.UntrustedData.WithMaximumObjectGraphDepth(2);
                Assert.Throws<InsufficientExecutionStackException>(() => MessagePackSerializer.Deserialize<RecursiveObjectGraph>(msgpack));
            }
            finally
            {
                MessagePackSecurity.Active = MessagePackSecurity.TrustedData;
            }
        }

        private delegate int WriterHelper(ref byte[] buffer, int offset);

        private static (byte[] buffer, int depthExceeded) StackDepthCheck_Helper(WriterHelper recursiveWriteOperation)
        {
            const int maxDepth = 3;

            var buffer = new byte[20];
            int offset = 0;
            for (int i = 0; i <= maxDepth; i++)
            {
                offset += recursiveWriteOperation(ref buffer, offset);
            }

            offset += MessagePackBinary.WriteByte(ref buffer, offset, 1);
            Array.Resize(ref buffer, offset);

            return (buffer, maxDepth);
        }

        private static void AssertDeserializationRecursionCheckThrows(byte[] buffer, int depthExceeded)
        {
            MessagePackSecurity.Active = MessagePackSecurity.UntrustedData.WithMaximumObjectGraphDepth(depthExceeded);
            try
            {
                Assert.Throws<InsufficientExecutionStackException>(() => MessagePackSerializer.Deserialize<object>(buffer));
            }
            finally
            {
                MessagePackSecurity.Active = MessagePackSecurity.TrustedData;
            }
        }

        private static void AssertConvertToJsonRecursionCheckThrows(byte[] buffer, int depthExceeded)
        {
            MessagePackSecurity.Active = MessagePackSecurity.UntrustedData.WithMaximumObjectGraphDepth(depthExceeded);
            try
            {
                Assert.Throws<InsufficientExecutionStackException>(() => MessagePackSerializer.ToJson(buffer));
            }
            finally
            {
                MessagePackSecurity.Active = MessagePackSecurity.TrustedData;
            }
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

    class NonMemoryStream : Stream
    {
        readonly MemoryStream stream;

        public NonMemoryStream(MemoryStream stream)
        {
            this.stream = stream;
        }

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => stream.Length;

        public override long Position { get => stream.Position; set => stream.Position = value; }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }
    }
}
