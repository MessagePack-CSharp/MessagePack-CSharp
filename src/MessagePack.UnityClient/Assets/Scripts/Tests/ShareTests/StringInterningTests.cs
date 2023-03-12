// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;
using MessagePack.Resolvers;
using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests
{
    public class StringInterningTests
    {
        [Fact]
        public void NullString()
        {
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.WriteNil();
            writer.Flush();

            var reader = new MessagePackReader(seq);
            string result = StandardResolver.Instance.GetFormatter<string>().Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            Assert.Null(result);
        }

        [Fact]
        public void EmptyString()
        {
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.Write(string.Empty);
            writer.Flush();

            var reader = new MessagePackReader(seq);
            string result = StandardResolver.Instance.GetFormatter<string>().Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            Assert.Same(string.Empty, result);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(1024 * 1024)]
        public void EquivalentStringsGetSharedInstance(int length)
        {
            string originalValue1 = new string('a', length);
            string originalValue3 = new string('b', length);
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.Write(originalValue1);
            writer.Write(originalValue1);
            writer.Write(originalValue3);
            writer.Flush();

            var reader = new MessagePackReader(seq);
            var formatter = new StringInterningFormatter();
            string value1 = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            string value2 = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            string value3 = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);

            Assert.Equal(originalValue1, value1);
            Assert.Equal(originalValue3, value3);

            Assert.Same(value1, value2);
        }

        [Fact]
        public void StringMemberInterning()
        {
            ClassOfStrings before = new ClassOfStrings { InternedString = "abc", OrdinaryString = "def", ObjectString = "obj" };
            ClassOfStrings after1 = MessagePackSerializer.Deserialize<ClassOfStrings>(MessagePackSerializer.Serialize(before, MessagePackSerializerOptions.Standard), MessagePackSerializerOptions.Standard);
            ClassOfStrings after2 = MessagePackSerializer.Deserialize<ClassOfStrings>(MessagePackSerializer.Serialize(before, MessagePackSerializerOptions.Standard), MessagePackSerializerOptions.Standard);
            Assert.Equal(after1.InternedString, after2.InternedString);
            Assert.Equal(after1.OrdinaryString, after2.OrdinaryString);
            Assert.Equal(after1.ObjectString, after2.ObjectString);

            Assert.Same(after1.InternedString, after2.InternedString);
            Assert.NotSame(after1.OrdinaryString, after2.OrdinaryString);
            Assert.NotSame(after1.ObjectString, after2.ObjectString);
        }

        [Fact]
        public void StringMemberInterning_CustomResolver()
        {
            var options = MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(
                    new IMessagePackFormatter[] { new StringInterningFormatter() },
                    new IFormatterResolver[] { StandardResolver.Instance }));

            ClassOfStrings before = new ClassOfStrings { InternedString = "abc", OrdinaryString = "def", ObjectString = "obj" };
            ClassOfStrings after1 = MessagePackSerializer.Deserialize<ClassOfStrings>(MessagePackSerializer.Serialize(before, options), options);
            ClassOfStrings after2 = MessagePackSerializer.Deserialize<ClassOfStrings>(MessagePackSerializer.Serialize(before, options), options);
            Assert.Equal(after1.InternedString, after2.InternedString);
            Assert.Equal(after1.OrdinaryString, after2.OrdinaryString);
            Assert.Equal(after1.ObjectString, after2.ObjectString);

            Assert.Same(after1.InternedString, after2.InternedString);
            Assert.Same(after1.OrdinaryString, after2.OrdinaryString);
            Assert.Same(after1.ObjectString, after2.ObjectString);
        }

        [MessagePackObject]
        public class ClassOfStrings
        {
            [Key(0)]
            [MessagePackFormatter(typeof(StringInterningFormatter))]
            public string InternedString { get; set; }

            [Key(1)]
            public string OrdinaryString { get; set; }

            [Key(2)]
            public object ObjectString { get; set; }
        }
    }
}
