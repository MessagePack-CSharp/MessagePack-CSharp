// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests
{
    public class InternedStringCollectionTests
    {
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
            var collection = new InternedStringCollection();
            string value1 = collection.GetString(ref reader);
            string value2 = collection.GetString(ref reader);
            string value3 = collection.GetString(ref reader);

            Assert.Equal(originalValue1, value1);
            Assert.Equal(originalValue3, value3);

            Assert.Same(value1, value2);
        }
    }
}
