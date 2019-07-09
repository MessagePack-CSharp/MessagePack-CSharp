using MessagePack.Resolvers;
using SharedData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            return (T)MessagePackSerializer.Deserialize(t, MessagePackSerializer.Serialize(t, obj));
        }

        [Fact]
        public void NonGeneric()
        {
            var data = new FirstSimpleData { Prop1 = 9, Prop2 = "hoge", Prop3 = 999 };
            var t = typeof(FirstSimpleData);
            var ms = new MemoryStream();

            var data1 = MessagePackSerializer.Deserialize(t, MessagePackSerializer.Serialize(t, data)) as FirstSimpleData;
            var data2 = MessagePackSerializer.Deserialize(t, MessagePackSerializer.Serialize(t, data, StandardResolver.Options)) as FirstSimpleData;

            MessagePackSerializer.Serialize(t, ms, data);
            ms.Position = 0;
            var data3 = MessagePackSerializer.Deserialize(t, ms) as FirstSimpleData;

            ms = new MemoryStream();
            MessagePackSerializer.Serialize(t, ms, data, StandardResolver.Options);
            ms.Position = 0;
            var data4 = MessagePackSerializer.Deserialize(t, ms, StandardResolver.Options) as FirstSimpleData;

            new[] { data1.Prop1, data2.Prop1, data3.Prop1, data4.Prop1 }.Distinct().Is(data.Prop1);
            new[] { data1.Prop2, data2.Prop2, data3.Prop2, data4.Prop2 }.Distinct().Is(data.Prop2);
            new[] { data1.Prop3, data2.Prop3, data3.Prop3, data4.Prop3 }.Distinct().Is(data.Prop3);
        }

        [Fact]
        public void StreamAPI()
        {
            var originalData = Enumerable.Range(1, 100).Select(x => new FirstSimpleData { Prop1 = x * x, Prop2 = "hoge", Prop3 = x }).ToArray();

            var ms = new MemoryStream();
            MessagePackSerializer.Serialize(typeof(FirstSimpleData[]), ms, originalData);

            var normal = MessagePackSerializer.Serialize(typeof(FirstSimpleData[]), originalData);

            ms.Position = 0;

            normal.SequenceEqual(ms.ToArray()).IsTrue();

            var decompress1 = MessagePackSerializer.Deserialize(typeof(FirstSimpleData[]), ms.ToArray());
            var decompress2 = MessagePackSerializer.Deserialize(typeof(FirstSimpleData[]), normal);
            var decompress3 = MessagePackSerializer.Deserialize(typeof(FirstSimpleData[]), ms);
            ms.Position = 0;
            var onmore = new NonMemoryStream(ms);
            var decompress4 = MessagePackSerializer.Deserialize(typeof(FirstSimpleData[]), onmore);

            decompress1.IsStructuralEqual(originalData);
            decompress2.IsStructuralEqual(originalData);
            decompress3.IsStructuralEqual(originalData);
            decompress4.IsStructuralEqual(originalData);
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
