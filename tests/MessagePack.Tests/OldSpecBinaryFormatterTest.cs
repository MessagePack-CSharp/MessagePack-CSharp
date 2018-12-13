using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
    public class OldSpecBinaryFormatterTest
    {
        private MessagePackSerializer serializer = new MessagePackSerializer();

        [Theory]
        [InlineData(10)] // fixstr
        [InlineData(1000)] // str 16
        [InlineData(100000)] // str 32
        public void SerializeSimpleByteArray(int arrayLength)
        {
            var sourceBytes = Enumerable.Range(0, arrayLength).Select(i => unchecked((byte)i)).ToArray(); // long byte array
            byte[] messagePackBytes = null;
            var length = OldSpecBinaryFormatter.Instance.Serialize(ref messagePackBytes, 0, sourceBytes, StandardResolver.Instance);
            Assert.NotEmpty(messagePackBytes);
            Assert.Equal(length, messagePackBytes.Length);

            var deserializedBytes = DeserializeByClassicMsgPack<byte[]>(messagePackBytes, MsgPack.Serialization.SerializationMethod.Array);
            Assert.Equal(sourceBytes, deserializedBytes);
        }

        [Fact]
        public void SerializeNil()
        {
            byte[] sourceBytes = null;
            byte[] messagePackBytes = null;
            var length = OldSpecBinaryFormatter.Instance.Serialize(ref messagePackBytes, 0, sourceBytes, StandardResolver.Instance);
            Assert.NotEmpty(messagePackBytes);
            Assert.Equal(length, messagePackBytes.Length);
            Assert.Equal(MessagePackCode.Nil, messagePackBytes[0]); 

            var deserializedBytes = DeserializeByClassicMsgPack<byte[]>(messagePackBytes, MsgPack.Serialization.SerializationMethod.Array);
            Assert.Null(deserializedBytes);
        }

        [Theory]
        [InlineData(10)] // fixstr
        [InlineData(1000)] // str 16
        [InlineData(100000)] // str 32
        public void SerializeObject(int arrayLength)
        {
            var foo = new Foo
            {
                Id = 123,
                Value = Enumerable.Range(0, arrayLength).Select(i => unchecked((byte) i)).ToArray() // long byte array
            };
            byte[] messagePackBytes = serializer.Serialize(foo);
            Assert.NotEmpty(messagePackBytes);

            var deserializedFoo = DeserializeByClassicMsgPack<Foo>(messagePackBytes, MsgPack.Serialization.SerializationMethod.Map);
            Assert.Equal(foo.Id, deserializedFoo.Id);
            Assert.Equal(foo.Value, deserializedFoo.Value);
        }

        [Theory]
        [InlineData(10)] // fixstr
        [InlineData(1000)] // str 16
        [InlineData(100000)] // str 32
        public void DeserializeSimpleByteArray(int arrayLength)
        {
            var sourceBytes = Enumerable.Range(0, arrayLength).Select(i => unchecked((byte) i)).ToArray(); // long byte array
            var messagePackBytes = SerializeByClassicMsgPack(sourceBytes, MsgPack.Serialization.SerializationMethod.Array); 

            var deserializedBytes = OldSpecBinaryFormatter.Instance.Deserialize(messagePackBytes, 0, StandardResolver.Instance, out var readSize);
            Assert.NotNull(deserializedBytes);
            Assert.Equal(sourceBytes, deserializedBytes);
        }

        [Fact]
        public void DeserializeNil()
        {
            var messagePackBytes = new byte[]{ MessagePackCode.Nil }; 

            var deserializedObj = OldSpecBinaryFormatter.Instance.Deserialize(messagePackBytes, 0, StandardResolver.Instance, out var readSize);
            Assert.Null(deserializedObj);
        }

        [Theory]
        [InlineData(10)] // fixstr
        [InlineData(1000)] // str 16
        [InlineData(100000)] // str 32
        public void DeserializeObject(int arrayLength)
        {
            var foo = new Foo
            {
                Id = 123,
                Value = Enumerable.Range(0, arrayLength).Select(i => unchecked((byte)i)).ToArray() // long byte array
            };
            var messagePackBytes = SerializeByClassicMsgPack(foo, MsgPack.Serialization.SerializationMethod.Map);

            var deserializedFoo = serializer.Deserialize<Foo>(messagePackBytes);
            Assert.NotNull(deserializedFoo);
            Assert.Equal(foo.Id, deserializedFoo.Id);
            Assert.Equal(foo.Value, deserializedFoo.Value);
        }

        private static byte[] SerializeByClassicMsgPack<T>(T obj, MsgPack.Serialization.SerializationMethod method)
        {
            var context = new MsgPack.Serialization.SerializationContext
            {
                SerializationMethod = method,
                CompatibilityOptions = { PackerCompatibilityOptions = MsgPack.PackerCompatibilityOptions.Classic }
            };

            var serializer = MsgPack.Serialization.MessagePackSerializer.Get<T>(context);
            using (var memory = new MemoryStream())
            {
                serializer.Pack(memory, obj);
                return memory.ToArray();
            }
        }

        private static T DeserializeByClassicMsgPack<T>(byte[] messagePackBytes, MsgPack.Serialization.SerializationMethod method)
        {
            var context = new MsgPack.Serialization.SerializationContext
            {
                SerializationMethod = method,
                CompatibilityOptions = { PackerCompatibilityOptions = MsgPack.PackerCompatibilityOptions.Classic }
            };

            var serializer = MsgPack.Serialization.MessagePackSerializer.Get<T>(context);
            using (var memory = new MemoryStream(messagePackBytes))
            {
                return serializer.Unpack(memory);
            }
        }

        [DataContract]
        public class Foo
        {
            [DataMember(Name = "Id")]
            public int Id { get; set; }

            [DataMember(Name = "Value")]
            [MessagePackFormatter(typeof(OldSpecBinaryFormatter))]
            public byte[] Value { get; set; }
        }
    }
}
