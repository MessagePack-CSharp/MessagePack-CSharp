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
    public class MessagePackStreamReaderArrayTests : TestBase
    {
        private static readonly IReadOnlyList<object> ArrayContent = new object[] { (byte)5, "2", "23", "234", "2345" };
        private readonly ReadOnlySequence<byte> arraySequence;

        public MessagePackStreamReaderArrayTests()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);

            writer.WriteArrayHeader(ArrayContent.Count);

            for (int i = 0; i < ArrayContent.Count; i++)
            {
                MessagePackSerializer.Serialize(ref writer, ArrayContent[i]);
                writer.Flush();
            }

            this.arraySequence = sequence;
        }

        [Fact]
        public async Task EnumerateArrayElements_AllAtOnce()
        {
            var reader = new MessagePackStreamReader(this.arraySequence.AsStream());
            int messageCounter = 0;
            await foreach (ReadOnlySequence<byte> elementSequence in reader.ReadArrayAsync(this.TimeoutToken))
            {
                Assert.Equal(ArrayContent[messageCounter++], MessagePackSerializer.Deserialize<object>(elementSequence));
            }
        }

        [Fact]
        public async Task EnumerateArrayElements_OneByteAtATime()
        {
            var reader = new MessagePackStreamReader(new OneByteAtATimeStream(this.arraySequence));
            int messageCounter = 0;
            await foreach (ReadOnlySequence<byte> elementSequence in reader.ReadArrayAsync(this.TimeoutToken))
            {
                Assert.Equal(ArrayContent[messageCounter++], MessagePackSerializer.Deserialize<object>(elementSequence));
            }
        }
    }
}
