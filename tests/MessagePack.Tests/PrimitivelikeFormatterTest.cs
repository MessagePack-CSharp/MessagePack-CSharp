using MessagePack.Formatters;
using MessagePack.Resolvers;
using Nerdbank.Streams;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class PrimitivelikeResolver : IFormatterResolver
    {
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(string))
            {
                return (IMessagePackFormatter<T>)new DummyStringFormatter();
            }

            if (typeof(T) == typeof(DateTime))
            {
                return (IMessagePackFormatter<T>)new DummyDateTimeFormatter();
            }

            if (typeof(T) == typeof(byte[]))
            {
                return (IMessagePackFormatter<T>)new DummyBinaryFormatter();
            }

            return StandardResolver.Instance.GetFormatter<T>();
        }
    }

    public class DummyStringFormatter : IMessagePackFormatter<string>
    {
        public string Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            throw new NotImplementedException();
        }

        public void Serialize(ref MessagePackWriter writer, string value, IFormatterResolver formatterResolver)
        {
            throw new NotImplementedException();
        }
    }

    public class DummyDateTimeFormatter : IMessagePackFormatter<DateTime>
    {
        public DateTime Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            throw new NotImplementedException();
        }

        public void Serialize(ref MessagePackWriter writer, DateTime value, IFormatterResolver formatterResolver)
        {
            throw new NotImplementedException();
        }
    }

    public class DummyBinaryFormatter : IMessagePackFormatter<byte[]>
    {
        public byte[] Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            throw new NotImplementedException();
        }

        public void Serialize(ref MessagePackWriter writer, byte[] value, IFormatterResolver formatterResolver)
        {
            throw new NotImplementedException();
        }
    }

    [MessagePackObject]
    public class MyDateTimeResolverTest1
    {
        [Key(0)]
        public DateTime MyProperty1 { get; set; }
    }

    [MessagePackObject]
    public class MyDateTimeResolverTest2
    {
        [Key(0)]
        public string MyProperty1 { get; set; }
    }

    [MessagePackObject]
    public class MyDateTimeResolverTest3
    {
        [Key(0)]
        public byte[] MyProperty1 { get; set; }
    }

    public class PrimitivelikeFormatterTest
    {
        private MessagePackSerializer defaultSerializer = new MessagePackSerializer();

        [Fact]
        public void CanResolve()
        {
            var resolver = new PrimitivelikeResolver();

            Assert.Throws<NotImplementedException>(() =>
            {
                defaultSerializer.Serialize(new MyDateTimeResolverTest1() { MyProperty1 = DateTime.Now }, resolver);
            });

            Assert.Throws<NotImplementedException>(() =>
            {
                defaultSerializer.Serialize(new MyDateTimeResolverTest2() { MyProperty1 = "aaa" }, resolver);
            });

            Assert.Throws<NotImplementedException>(() =>
            {
                defaultSerializer.Serialize(new MyDateTimeResolverTest3() { MyProperty1 = new byte[] { 0, 1, 2 } }, resolver);
            });
        }

        [Fact]
        public void NativeDateTime()
        {
            var referenceContext = new MsgPack.Serialization.SerializationContext(MsgPack.PackerCompatibilityOptions.Classic)
            {
                DefaultDateTimeConversionMethod = MsgPack.Serialization.DateTimeConversionMethod.Native,
            };

            var now = DateTime.Now;

            var serializer = referenceContext.GetSerializer<DateTime>();

            var a = defaultSerializer.Serialize(now, NativeDateTimeResolver.Instance);
            var b = serializer.PackSingleObject(now);

            a.Is(b);

            var dt1 = defaultSerializer.Deserialize<DateTime>(a, NativeDateTimeResolver.Instance);
            var dt2 = serializer.UnpackSingleObject(b);

            dt1.Is(dt2);
        }

        [Fact]
        public void OldSpecString()
        {
            var referenceContext = new MsgPack.Serialization.SerializationContext(MsgPack.PackerCompatibilityOptions.Classic)
            {
                DefaultDateTimeConversionMethod = MsgPack.Serialization.DateTimeConversionMethod.Native,
            };

            var data = "あいうえおabcdefgかきくけこあいうえおabcdefgかきくけこあいうえおabcdefgかきくけこあいうえおabcdefgかきくけこ"; // Japanese

            var serializer = referenceContext.GetSerializer<string>();

            using (var sequence = new Sequence<byte>())
            {
                var oldSpecWriter = new MessagePackWriter(sequence) { OldSpec = true };
                defaultSerializer.Serialize(ref oldSpecWriter, data);
                oldSpecWriter.Flush();
                var a = sequence.AsReadOnlySequence.ToArray();
                var b = serializer.PackSingleObject(data);

                a.Is(b);

                var oldSpecReader = new MessagePackReader(sequence.AsReadOnlySequence);
                var r1 = defaultSerializer.Deserialize<string>(ref oldSpecReader);
                var r2 = serializer.UnpackSingleObject(b);

                r1.Is(r2);
            }
        }


        [Fact]
        public void OldSpecBinary()
        {
            var referenceContext = new MsgPack.Serialization.SerializationContext(MsgPack.PackerCompatibilityOptions.Classic)
            {
                DefaultDateTimeConversionMethod = MsgPack.Serialization.DateTimeConversionMethod.Native,
            };

            var data = Enumerable.Range(0, 10000).Select(x => (byte)1).ToArray();

            var serializer = referenceContext.GetSerializer<byte[]>();

            using (var sequence = new Sequence<byte>())
            {
                var oldSpecWriter = new MessagePackWriter(sequence) { OldSpec = true };
                defaultSerializer.Serialize(ref oldSpecWriter, data);
                oldSpecWriter.Flush();
                var a = sequence.AsReadOnlySequence.ToArray();
                var b = serializer.PackSingleObject(data);

                a.Is(b);

                var oldSpecReader = new MessagePackReader(sequence.AsReadOnlySequence);
                var r1 = defaultSerializer.Deserialize<byte[]>(ref oldSpecReader);
                var r2 = serializer.UnpackSingleObject(b);

                r1.Is(r2);
            }
        }
    }
}
