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
            var segments = new TestSequenceSegment<T>[segmentContents.Length];
            TestSequenceSegment<T> last = null;
            for (var i = 0; i < segmentContents.Length; i++)
            {
                last = segments[i] = new TestSequenceSegment<T>(segmentContents[i], last);
            }

            return new ReadOnlySequence<T>(segments[0], 0, last, last.Memory.Length);
        }

        class TestSequenceSegment<T> : ReadOnlySequenceSegment<T>
        {
            public TestSequenceSegment(T[] data, TestSequenceSegment<T> prev = null)
            {
                Memory = new Memory<T>(data);

                if (prev != null)
                {
                    prev.Next = this;
                    RunningIndex = prev.RunningIndex + prev.Memory.Length;
                }
            }
        }
    }
}
