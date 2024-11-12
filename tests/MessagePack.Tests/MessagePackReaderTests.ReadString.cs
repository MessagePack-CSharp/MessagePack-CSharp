// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using Xunit;

namespace MessagePack.Tests
{
    public partial class MessagePackReaderTests
    {
        [Fact]
        public void ReadString_HandlesSingleSegment()
        {
            ReadOnlySequence<byte> seq = this.BuildSequence(new[]
            {
                (byte)(MessagePackCode.MinFixStr + 2),
                (byte)'A', (byte)'B',
            });

            var reader = new MessagePackReader(seq);
            var result = reader.ReadString();
            Assert.Equal("AB", result);
        }

        [Fact]
        public void ReadString_HandlesMultipleSegments()
        {
            ReadOnlySequence<byte> seq = this.BuildSequence(
                new[] { (byte)(MessagePackCode.MinFixStr + 2), (byte)'A' },
                new[] { (byte)'B' });

            var reader = new MessagePackReader(seq);
            var result = reader.ReadString();
            Assert.Equal("AB", result);
        }

        private ReadOnlySequence<T> BuildSequence<T>(params T[][] segmentContents)
        {
            if (segmentContents.Length == 1)
            {
                return new ReadOnlySequence<T>(segmentContents[0].AsMemory());
            }

            var bufferSegment = new BufferSegment<T>(segmentContents[0].AsMemory());
            BufferSegment<T> last = default;
            for (var i = 1; i < segmentContents.Length; i++)
            {
                last = bufferSegment.Append(segmentContents[i]);
            }

            return new ReadOnlySequence<T>(bufferSegment, 0, last, last.Memory.Length);
        }

        internal class BufferSegment<T> : ReadOnlySequenceSegment<T>
        {
            public BufferSegment(ReadOnlyMemory<T> memory)
            {
                this.Memory = memory;
            }

            public BufferSegment<T> Append(ReadOnlyMemory<T> memory)
            {
                var segment = new BufferSegment<T>(memory)
                {
                    RunningIndex = this.RunningIndex + this.Memory.Length,
                };
                this.Next = segment;
                return segment;
            }
        }
    }
}
