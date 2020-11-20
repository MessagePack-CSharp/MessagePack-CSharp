// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Resolvers;
using Nerdbank.Streams;
using SharedData;
using Xunit;

namespace MessagePack.GeneratedCode.Tests
{
    public class MissingPropertiesTest
    {
        private readonly MessagePackSerializerOptions options;

        public MissingPropertiesTest()
        {
            var resolver = CompositeResolver.Create(GeneratedResolver.Instance, StandardResolver.Instance);
            options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        [Fact]
        public void IgnoreSerializationWhenNullTypeTest_BothNull()
        {
            var value = new IgnoreSerializationWhenNullType { MyProperty1 = null, MyProperty2 = null, MyProperty3 = 114 };
            var reader = new MessagePackReader(MessagePackSerializer.Serialize(value, options));
            Assert.Equal(1, reader.ReadMapHeader());
            Assert.Equal("MyProp3", reader.ReadString());
            Assert.Equal(value.MyProperty3, reader.ReadInt64());
        }

        [Fact]
        public void IgnoreSerializationWhenNullTypeTest_Prop1Null()
        {
            var value = new IgnoreSerializationWhenNullType { MyProperty1 = null, MyProperty2 = "null", MyProperty3 = 514 };
            var reader = new MessagePackReader(MessagePackSerializer.Serialize(value, options));
            Assert.Equal(2, reader.ReadMapHeader());
            for (var i = 0; i < 2; i++)
            {
                var prop = reader.ReadString();
                switch (prop)
                {
                    case "MyProp2":
                        Assert.Equal(value.MyProperty2, reader.ReadString());
                        break;
                    case "MyProp3":
                        Assert.Equal(value.MyProperty3, reader.ReadInt64());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void IgnoreSerializationWhenNullTypeTest_Prop2Null()
        {
            var value = new IgnoreSerializationWhenNullType { MyProperty1 = "null", MyProperty2 = null, MyProperty3 = 1919 };
            var reader = new MessagePackReader(MessagePackSerializer.Serialize(value, options));
            Assert.Equal(2, reader.ReadMapHeader());
            for (var i = 0; i < 2; i++)
            {
                var prop = reader.ReadString();
                switch (prop)
                {
                    case "MyProp1":
                        Assert.Equal(value.MyProperty1, reader.ReadString());
                        break;
                    case "MyProp3":
                        Assert.Equal(value.MyProperty3, reader.ReadInt64());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void IgnoreSerializationWhenNullTypeTest_NotNull()
        {
            var value = new IgnoreSerializationWhenNullType { MyProperty1 = "null", MyProperty2 = "ref", MyProperty3 = 1919 };
            var reader = new MessagePackReader(MessagePackSerializer.Serialize(value, options));
            Assert.Equal(3, reader.ReadMapHeader());
            for (var i = 0; i < 3; i++)
            {
                var prop = reader.ReadString();
                switch (prop)
                {
                    case "MyProp1":
                        Assert.Equal(value.MyProperty1, reader.ReadString());
                        break;
                    case "MyProp2":
                        Assert.Equal(value.MyProperty2, reader.ReadString());
                        break;
                    case "MyProp3":
                        Assert.Equal(value.MyProperty3, reader.ReadInt64());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void DefaultValueStringKeyClassWithoutExplicitConstructorTest()
        {
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.WriteMapHeader(0);
            writer.Flush();

            var instance = MessagePackSerializer.Deserialize<DefaultValueStringKeyClassWithoutExplicitConstructor>(seq, options);
            Assert.Equal(DefaultValueStringKeyClassWithoutExplicitConstructor.Prop1Constant, instance.Prop1);
            Assert.Equal(DefaultValueStringKeyClassWithoutExplicitConstructor.Prop2Constant, instance.Prop2);
        }

        [Fact]
        public void DefaultValueStringKeyClassWithExplicitConstructorTest()
        {
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.WriteMapHeader(1);
            writer.Write(nameof(DefaultValueStringKeyClassWithExplicitConstructor.Prop1));
            writer.Write(-1);
            writer.Flush();

            var instance = MessagePackSerializer.Deserialize<DefaultValueStringKeyClassWithExplicitConstructor>(seq, options);
            Assert.Equal(-1, instance.Prop1);
            Assert.Equal(DefaultValueStringKeyClassWithExplicitConstructor.Prop2Constant, instance.Prop2);
        }

        [Fact]
        public void DefaultValueStringKeyStructWithExplicitConstructorTest()
        {
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.WriteMapHeader(1);
            writer.Write(nameof(DefaultValueStringKeyStructWithExplicitConstructor.Prop1));
            writer.Write(-1);
            writer.Flush();

            var instance = MessagePackSerializer.Deserialize<DefaultValueStringKeyStructWithExplicitConstructor>(seq, options);
            Assert.Equal(-1, instance.Prop1);
            Assert.Equal(DefaultValueStringKeyStructWithExplicitConstructor.Prop2Constant, instance.Prop2);
        }

        [Fact]
        public void DefaultValueIntKeyClassWithoutExplicitConstructorTest()
        {
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.WriteArrayHeader(0);
            writer.Flush();

            var instance = MessagePackSerializer.Deserialize<DefaultValueIntKeyClassWithoutExplicitConstructor>(seq, options);
            Assert.Equal(DefaultValueIntKeyClassWithoutExplicitConstructor.Prop1Constant, instance.Prop1);
            Assert.Equal(DefaultValueIntKeyClassWithoutExplicitConstructor.Prop2Constant, instance.Prop2);
        }

        [Fact]
        public void DefaultValueIntKeyClassWithExplicitConstructorTest()
        {
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.WriteArrayHeader(1);
            writer.Write(-1);
            writer.Flush();

            var instance = MessagePackSerializer.Deserialize<DefaultValueIntKeyClassWithExplicitConstructor>(seq, options);
            Assert.Equal(-1, instance.Prop1);
            Assert.Equal(DefaultValueIntKeyClassWithExplicitConstructor.Prop2Constant, instance.Prop2);
        }

        [Fact]
        public void DefaultValueIntKeyStructWithExplicitConstructorTest()
        {
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.WriteArrayHeader(1);
            writer.Write(-1);
            writer.Flush();

            var instance = MessagePackSerializer.Deserialize<DefaultValueIntKeyStructWithExplicitConstructor>(seq, options);
            Assert.Equal(-1, instance.Prop1);
            Assert.Equal(DefaultValueIntKeyStructWithExplicitConstructor.Prop2Constant, instance.Prop2);
        }
    }
}
