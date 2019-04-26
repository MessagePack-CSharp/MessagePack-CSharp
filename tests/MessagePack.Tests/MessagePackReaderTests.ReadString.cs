using System;
using System.Buffers;
using Xunit;

namespace MessagePack.Tests
{
    partial class MessagePackReaderTests
    {
        [Fact]
        public void ReadString_HandlesSingleSegment()
        {
            var seq = BuildSequence(new[] {
                (byte)(MessagePackCode.MinFixStr + 2),
                (byte)'A', (byte)'B' });

            var reader = new MessagePackReader(seq);
            var result = reader.ReadString();
            Assert.Equal("AB", result);
        }

        [Fact]
        public void ReadString_HandlesMultipleSegments()
        {
            var seq = BuildSequence(
                new[] { (byte)(MessagePackCode.MinFixStr + 2), (byte)'A' },
                new[] { (byte)'B' });

            var reader = new MessagePackReader(seq);
            var result = reader.ReadString();
            Assert.Equal("AB", result);
        }

        ReadOnlySequence<T> BuildSequence<T>(params T[][] segmentContents)
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
                Memory = memory;
            }

            public BufferSegment<T> Append(ReadOnlyMemory<T> memory)
            {
                var segment = new BufferSegment<T>(memory)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };
                Next = segment;
                return segment;
            }
        }
    }
}
