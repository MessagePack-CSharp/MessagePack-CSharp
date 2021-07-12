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
            var options = MessagePackSerializerOptions.Standard.WithStringInterning(new InternedStringCollection());
            string result = StringInterningResolver.Instance.GetFormatter<string>().Deserialize(ref reader, options);
            Assert.Null(result);
        }

        [Fact]
        public void EquivalentStringsGetSharedInstance()
        {
            string originalValue1 = new string('a', 5);
            string originalValue3 = new string('b', 5);
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.Write(originalValue1);
            writer.Write(originalValue1);
            writer.Write(originalValue3);
            writer.Flush();

            var reader = new MessagePackReader(seq);
            var options = MessagePackSerializerOptions.Standard.WithStringInterning(new InternedStringCollection());
            var formatter = StringInterningResolver.Instance.GetFormatter<string>();
            string value1 = formatter.Deserialize(ref reader, options);
            string value2 = formatter.Deserialize(ref reader, options);
            string value3 = formatter.Deserialize(ref reader, options);

            Assert.Equal(originalValue1, value1);
            Assert.Equal(originalValue3, value3);

            Assert.Same(value1, value2);
        }
    }
}
