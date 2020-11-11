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
