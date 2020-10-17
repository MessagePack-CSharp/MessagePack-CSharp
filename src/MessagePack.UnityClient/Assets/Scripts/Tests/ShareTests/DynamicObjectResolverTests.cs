// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !ENABLE_IL2CPP

using System;
using System.Runtime.Serialization;
using MessagePack.Resolvers;
using Nerdbank.Streams;
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

        [Fact]
        public void PrivateMembersInBaseClass_StandardResolverAllowPrivate()
        {
            var options = StandardResolverAllowPrivate.Options;
            PrivateMembersInBaseClass_Helper(options);
        }

        [Fact]
        public void DeserializerSetsMissingPropertiesToDefaultValue()
        {
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.WriteMapHeader(1);
            writer.Write(nameof(TwoProperties.Prop1));
            writer.Write("Set");
            writer.Flush();

            var instance = MessagePackSerializer.Deserialize<TwoProperties>(seq);
            Assert.Equal("Set", instance.Prop1);
            Assert.Null(instance.Prop2);
        }

        private static void Assert3MemberClassSerializedContent(ReadOnlyMemory<byte> msgpack)
        {
            var reader = new MessagePackReader(msgpack);
            Assert.Equal(3, reader.ReadArrayHeader());
            Assert.Equal(1, reader.ReadInt32());
            Assert.Equal(2, reader.ReadInt32());
            Assert.Equal(3, reader.ReadInt32());
        }

        private void PrivateMembersInBaseClass_Helper(MessagePackSerializerOptions options)
        {
            var obj = new DerivedClass { Name = "name", BaseClassFieldAccessor = 15, BaseClassProperty = 5 };
            var bin = MessagePackSerializer.Serialize(obj, options);
            this.logger.WriteLine(MessagePackSerializer.ConvertToJson(bin));
            var obj2 = MessagePackSerializer.Deserialize<DerivedClass>(bin, options);
            Assert.Equal(obj.BaseClassFieldAccessor, obj2.BaseClassFieldAccessor);
            Assert.Equal(obj.BaseClassProperty, obj2.BaseClassProperty);
            Assert.Equal(obj.Name, obj2.Name);
        }

        [DataContract]
        public class TwoProperties
        {
            [DataMember]
            public string Prop1 { get; set; } = "Uninitialized";

            [DataMember]
            public string Prop2 { get; set; } = "Uninitialized";
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

        [DataContract]
        public class BaseClass
        {
            [DataMember]
            private int baseClassField;

            public int BaseClassFieldAccessor
            {
                get => this.baseClassField;
                set => this.baseClassField = value;
            }

            [DataMember]
#pragma warning disable SA1300 // Element should begin with upper-case letter
            private int baseClassProperty { get; set; }
#pragma warning restore SA1300 // Element should begin with upper-case letter

            public int BaseClassProperty
            {
                get => this.baseClassProperty;
                set => this.baseClassProperty = value;
            }
        }

        [DataContract]
        public class DerivedClass : BaseClass
        {
            [DataMember]
            public string Name { get; set; }
        }
    }
}

#endif
