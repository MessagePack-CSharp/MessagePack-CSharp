// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Resolvers;
using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests
{
    public class StringInterningResolverTests
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
            var formatter = StandardResolver.Instance.GetFormatter<string>();
            string value1 = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            string value2 = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            string value3 = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);

            Assert.Equal(originalValue1, value1);
            Assert.Equal(originalValue3, value3);

            Assert.Same(value1, value2);
        }
    }
}
