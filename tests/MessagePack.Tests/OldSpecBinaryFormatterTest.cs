using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
    public class OldSpecBinaryFormatterTest
    {
        [Fact]
        public void Serialize()
        {
            var sourceBytes = Enumerable.Range(0, 10000).Select(i => unchecked((byte)i)).ToArray(); // long byte array
            byte[] messagePackBytes = null;
            var length = OldSpecBinaryFormatter.Instance.Serialize(ref messagePackBytes, 0, sourceBytes, StandardResolver.Instance);

            var deserializedBytes = DeserializeByClassicMsgPack<byte[]>(messagePackBytes);
            Assert.Equal(sourceBytes, deserializedBytes);
        }

        [Fact]
        public void SerializeNil()
        {
            byte[] sourceBytes = null;
            byte[] messagePackBytes = null;
            var length = OldSpecBinaryFormatter.Instance.Serialize(ref messagePackBytes, 0, sourceBytes, StandardResolver.Instance);
            Assert.Equal(0xc0, messagePackBytes[0]); // nil

            var deserializedBytes = DeserializeByClassicMsgPack<byte[]>(messagePackBytes);
            Assert.Null(deserializedBytes);
        }

        [Fact]
        public void Deserialize()
        {
            var sourceBytes = Enumerable.Range(0, 10000).Select(i => unchecked((byte) i)).ToArray(); // long byte array
            var messagePackBytes = SerializeByClassicMsgPack(sourceBytes); 
            Assert.Equal(0xda, messagePackBytes[0]); // str 16

            var deserializedBytes = OldSpecBinaryFormatter.Instance.Deserialize(messagePackBytes, 0, StandardResolver.Instance, out var readSize);
            Assert.Equal(sourceBytes, deserializedBytes);
        }

        [Fact]
        public void DeserializeNil()
        {
            var messagePackBytes = new byte[]{ 0xc0 }; // nil

            var deserializedObj = OldSpecBinaryFormatter.Instance.Deserialize(messagePackBytes, 0, StandardResolver.Instance, out var readSize);
            Assert.Null(deserializedObj);
        }

        private static byte[] SerializeByClassicMsgPack<T>(T obj)
        {
            var context = new MsgPack.Serialization.SerializationContext
            {
                SerializationMethod = MsgPack.Serialization.SerializationMethod.Array,
                CompatibilityOptions = { PackerCompatibilityOptions = MsgPack.PackerCompatibilityOptions.Classic }
            };

            var serializer = MsgPack.Serialization.MessagePackSerializer.Get<T>(context);
            using (var memory = new MemoryStream())
            {
                serializer.Pack(memory, obj);
                return memory.ToArray();
            }
        }

        private static T DeserializeByClassicMsgPack<T>(byte[] messagePackBytes)
        {
            var context = new MsgPack.Serialization.SerializationContext
            {
                SerializationMethod = MsgPack.Serialization.SerializationMethod.Array,
                CompatibilityOptions = { PackerCompatibilityOptions = MsgPack.PackerCompatibilityOptions.Classic }
            };

            var serializer = MsgPack.Serialization.MessagePackSerializer.Get<T>(context);
            using (var memory = new MemoryStream(messagePackBytes))
            {
                return serializer.Unpack(memory);
            }
        }
    }
}
