// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace MessagePack.Tests
{
    public class StandardClassLibraryFormatterTests : TestBase
    {
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
    }
}
