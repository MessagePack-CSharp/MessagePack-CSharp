// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Tests
{
    public class StandardClassLibraryFormatterTests : TestBase
    {
        private readonly ITestOutputHelper logger;

        public StandardClassLibraryFormatterTests(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

        [Fact]
        public void SystemType_Serializable()
        {
            Type type = typeof(string);
            byte[] msgpack = MessagePackSerializer.Serialize(type, MessagePackSerializerOptions.Standard);
            Type type2 = MessagePackSerializer.Deserialize<Type>(msgpack, MessagePackSerializerOptions.Standard);
            Assert.Equal(type, type2);
        }

        [Fact]
        public void SystemType_Serializable_Null()
        {
            Type type = null;
            byte[] msgpack = MessagePackSerializer.Serialize(type, MessagePackSerializerOptions.Standard);
            Type type2 = MessagePackSerializer.Deserialize<Type>(msgpack, MessagePackSerializerOptions.Standard);
            Assert.Equal(type, type2);
        }

        [Fact]
        public void DeserializeByteArrayFromFixArray()
        {
            var input = new byte[] { 0x93, 0x01, 0x02, 0x03 };
            byte[] byte_array = MessagePackSerializer.Deserialize<byte[]>(input);
            Assert.Equal(new byte[] { 1, 2, 3 }, byte_array);
        }

        [Fact]
        public void DeserializeByteArrayFromFixArray_LargerNumbers()
        {
            var input = new byte[] { 0x93, 0x01, 0x02, 0xCC, 0xD4 };
            byte[] byte_array = MessagePackSerializer.Deserialize<byte[]>(input);
            Assert.Equal(new byte[] { 1, 2, 212 }, byte_array);
        }

        [Fact]
        public void DeserializeByteArrayFromFixArray_LargerThanByte()
        {
            var input = new byte[] { 0x93, 0x01, 0x02, 0xCD, 0x08, 0x48 }; // 1, 2, 2120
            var ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<byte[]>(input));
            this.logger.WriteLine(ex.ToString());
        }

        [Fact]
        public void DeserializeByteArrayFromFixArray_ZeroLength()
        {
            var input = new byte[] { 0x90 }; // [ ]
            byte[] actual = MessagePackSerializer.Deserialize<byte[]>(input);
            Assert.Empty(actual);

            // Make sure we're optimized to reuse singleton empty arrays.
            Assert.Same(Array.Empty<byte>(), actual);
        }

        [Fact]
        public void DeserializeByteArrayFromFixArray_Array32()
        {
            var input = new byte[] { 0xDD, 0, 0, 0, 3, 1, 2, 3 };
            byte[] byte_array = MessagePackSerializer.Deserialize<byte[]>(input);
            Assert.Equal(new byte[] { 1, 2, 3 }, byte_array);
        }
    }
}
