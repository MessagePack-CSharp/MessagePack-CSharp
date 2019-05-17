﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Nerdbank.Streams;
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
            var messagePackBytes = new Sequence<byte>();
            var messagePackBytesWriter = new MessagePackWriter(messagePackBytes) { OldSpec = true };
            serializer.Serialize(ref messagePackBytesWriter, sourceBytes);
            messagePackBytesWriter.Flush();
            Assert.NotEqual(0, messagePackBytes.Length);

            var deserializedBytes = DeserializeByClassicMsgPack<byte[]>(messagePackBytes.AsReadOnlySequence.ToArray(), MsgPack.Serialization.SerializationMethod.Array);
            Assert.Equal(sourceBytes, deserializedBytes);
        }

        [Fact]
        public void SerializeNil()
        {
            byte[] sourceBytes = null;
            var messagePackBytes = new Sequence<byte>();
            var messagePackBytesWriter = new MessagePackWriter(messagePackBytes) { OldSpec = true };
            serializer.Serialize(ref messagePackBytesWriter, sourceBytes, StandardResolver.Instance);
            messagePackBytesWriter.Flush();
            Assert.Equal(1, messagePackBytes.Length);
            Assert.Equal(MessagePackCode.Nil, messagePackBytes.AsReadOnlySequence.First.Span[0]); 

            var deserializedBytes = DeserializeByClassicMsgPack<byte[]>(messagePackBytes.AsReadOnlySequence.ToArray(), MsgPack.Serialization.SerializationMethod.Array);
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
            var messagePackBytesReader = new MessagePackReader(messagePackBytes);
            var deserializedBytes = serializer.Deserialize<byte[]>(ref messagePackBytesReader);
            Assert.NotNull(deserializedBytes);
            Assert.Equal(sourceBytes, deserializedBytes);
        }

        [Fact]
        public void DeserializeNil()
        {
            var messagePackReader = new MessagePackReader(new byte[] { MessagePackCode.Nil });

            var deserializedObj = serializer.Deserialize<byte[]>(ref messagePackReader);
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
            var oldSpecReader = new MessagePackReader(messagePackBytes);
            var deserializedFoo = serializer.Deserialize<Foo>(ref oldSpecReader);
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
            public byte[] Value { get; set; }
        }
    }
}
