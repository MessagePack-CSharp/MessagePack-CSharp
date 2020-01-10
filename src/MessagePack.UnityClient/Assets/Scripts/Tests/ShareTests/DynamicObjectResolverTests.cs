// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !ENABLE_IL2CPP

using System;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Tests
{
    public class DynamicObjectResolverTests
    {
        private readonly ITestOutputHelper logger;

        public DynamicObjectResolverTests(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

        [Fact]
        public void SerializeTypeWithReadOnlyField()
        {
            Assert3MemberClassSerializedContent(MessagePackSerializer.Serialize(new TestMessageWithReadOnlyField()));
        }

        [Fact]
        public void SerializeTypeWithReadOnlyProperty()
        {
            Assert3MemberClassSerializedContent(MessagePackSerializer.Serialize(new TestMessageWithReadOnlyProperty()));
        }

        [Fact]
        public void SerializeTypeWithReadWriteFields()
        {
            Assert3MemberClassSerializedContent(MessagePackSerializer.Serialize(new TestMessageWithReadWriteFields()));
        }

        private static void Assert3MemberClassSerializedContent(ReadOnlyMemory<byte> msgpack)
        {
            var reader = new MessagePackReader(msgpack);
            Assert.Equal(3, reader.ReadArrayHeader());
            Assert.Equal(1, reader.ReadInt32());
            Assert.Equal(2, reader.ReadInt32());
            Assert.Equal(3, reader.ReadInt32());
        }

        [MessagePackObject]
        public class TestMessageWithReadOnlyField
        {
            [Key(0)]
            public int Field1 = 1;

            [Key(1)]
            public readonly int Field2 = 2;

            [Key(2)]
            public int Field3 = 3;
        }

        [MessagePackObject]
        public class TestMessageWithReadWriteFields
        {
            [Key(0)]
            public int Property1 = 1;

            [Key(1)]
            public int Property2 = 2;

            [Key(2)]
            public int Property3 = 3;
        }

        [MessagePackObject]
        public class TestMessageWithReadOnlyProperty
        {
            [Key(0)]
            public int Property1 { get; set; } = 1;

            [Key(1)]
            public int Property2 => 2;

            [Key(2)]
            public int Property3 { get; set; } = 3;
        }
    }
}

#endif
