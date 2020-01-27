// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests
{
    public class MessagePackStreamReaderTests : TestBase
    {
        private readonly ReadOnlySequence<byte> twoMessages;
        private readonly IReadOnlyList<SequencePosition> messagePositions;

        public MessagePackStreamReaderTests()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            var positions = new List<SequencePosition>();

            // First message
            writer.Write(5);
            writer.Flush();
            positions.Add(sequence.AsReadOnlySequence.End);

            // Second message is more interesting.
            writer.WriteArrayHeader(2);
            writer.Write("Hi");
            writer.Write("There");
            writer.Flush();
            positions.Add(sequence.AsReadOnlySequence.End);

            this.twoMessages = sequence.AsReadOnlySequence;
            this.messagePositions = positions;
        }

        [Fact]
        public void Ctor_NullStream()
        {
            Assert.Throws<ArgumentNullException>(() => new MessagePackStreamReader(null));
        }

        [Fact]
        public void RemainingBytes_BeforeReading()
        {
            using (var reader = new MessagePackStreamReader(new MemoryStream()))
            {
                Assert.True(reader.RemainingBytes.IsEmpty);
            }
        }

        [Fact]
        public async Task StreamEndsWithNoMessage()
        {
            using (var reader = new MessagePackStreamReader(new MemoryStream()))
            {
                Assert.Null(await reader.ReadAsync(this.TimeoutToken));
                Assert.True(reader.RemainingBytes.IsEmpty);
            }
        }

        [Fact]
        public async Task StreamEndsCleanlyAfterMessage()
        {
            var oneMessage = this.twoMessages.Slice(0, this.messagePositions[0]).ToArray();
            using (var reader = new MessagePackStreamReader(new MemoryStream(oneMessage)))
            {
                var message1 = await reader.ReadAsync(this.TimeoutToken);
                Assert.NotNull(message1);
                Assert.Equal(oneMessage, message1.Value.ToArray());

                Assert.True(reader.RemainingBytes.IsEmpty);
                Assert.Null(await reader.ReadAsync(this.TimeoutToken));
                Assert.True(reader.RemainingBytes.IsEmpty);
            }
        }

        [Fact]
        public async Task TwoMessagesInSingleRead()
        {
            using (var reader = new MessagePackStreamReader(new MemoryStream(this.twoMessages.ToArray())))
            {
                var message1 = await reader.ReadAsync(this.TimeoutToken);
                Assert.NotNull(message1);
                Assert.Equal(this.twoMessages.Slice(0, this.messagePositions[0]).ToArray(), message1.Value.ToArray());

                var message2 = await reader.ReadAsync(this.TimeoutToken);
                Assert.NotNull(message2);
                Assert.Equal(this.twoMessages.Slice(this.messagePositions[0], this.messagePositions[1]).ToArray(), message2.Value.ToArray());

                Assert.Null(await reader.ReadAsync(this.TimeoutToken));
            }
        }

        [Fact]
        public async Task StreamEndsWithNoMessageAndExtraBytes()
        {
            // We'll include the start of the second message since it is multi-byte.
            var partialMessage = this.twoMessages.Slice(messagePositions[0], 1).ToArray();
            using (var reader = new MessagePackStreamReader(new MemoryStream(partialMessage)))
            {
                Assert.Null(await reader.ReadAsync(this.TimeoutToken));
                Assert.Equal(partialMessage, reader.RemainingBytes.ToArray());
            }
        }

        [Fact]
        public async Task StreamEndsAfterMessageWithExtraBytes()
        {
            // Include the first message and one more byte
            var partialMessage = this.twoMessages.Slice(0, 2).ToArray();
            using (var reader = new MessagePackStreamReader(new MemoryStream(partialMessage)))
            {
                var firstMessage = await reader.ReadAsync(this.TimeoutToken);
                Assert.NotNull(firstMessage);
                Assert.Equal(partialMessage.Take(1), firstMessage.Value.ToArray());

                Assert.Equal(partialMessage.Skip(1), reader.RemainingBytes.ToArray());
                Assert.Null(await reader.ReadAsync(this.TimeoutToken));
                Assert.Equal(partialMessage.Skip(1), reader.RemainingBytes.ToArray());
            }
        }

        [Fact]
        public async Task ReadAsync_EveryTruncationPositionPossible()
        {
            using (var reader = new MessagePackStreamReader(new OneByteAtATimeStream(this.twoMessages)))
            {
                Assert.True((await reader.ReadAsync(this.TimeoutToken)).HasValue);
                Assert.True((await reader.ReadAsync(this.TimeoutToken)).HasValue);
                Assert.False((await reader.ReadAsync(this.TimeoutToken)).HasValue);
            }
        }

        [Fact]
        public void DisposeClosesStream()
        {
            var ms = new MemoryStream();
            new MessagePackStreamReader(ms).Dispose();
            Assert.False(ms.CanSeek);
        }

        [Fact]
        public void DisposeDoesNotCloseStream_IfAskedNotTo()
        {
            var ms = new MemoryStream();
            new MessagePackStreamReader(ms, leaveOpen: true).Dispose();
            Assert.True(ms.CanSeek);
        }
    }
}
