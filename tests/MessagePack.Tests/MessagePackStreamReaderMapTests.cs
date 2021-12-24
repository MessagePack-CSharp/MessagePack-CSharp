// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests
{
    public class MessagePackStreamReaderMapTests : TestBase
    {
        private static readonly IReadOnlyList<object> MapContent = new object[] { (byte)5, "2", "23", "234", "2345" };
        private readonly ReadOnlySequence<byte> arraySequence;

        public MessagePackStreamReaderMapTests()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);

            writer.WriteMapHeader(MapContent.Count);

            for (int i = 0; i < MapContent.Count; i++)
            {
                MessagePackSerializer.Serialize(ref writer, MapContent[i]);
                writer.Flush();
            }

            this.arraySequence = sequence;
        }

        [Fact]
        public async Task ReadMapHeader()
        {
            var reader = new MessagePackStreamReader(this.arraySequence.AsStream());
            var count = await reader.ReadMapHeaderAsync(this.TimeoutToken);
            Assert.Equal(MapContent.Count, count);
            for (var i = 0; i < count; i++)
            {
                var elementSequence = await reader.ReadAsync(this.TimeoutToken);
                Assert.Equal(MapContent[i], MessagePackSerializer.Deserialize<object>(elementSequence.Value));
            }
        }
    }
}
