using MessagePack.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class UnsafeMemoryTest
    {
        private MessagePackSerializer serializer = new MessagePackSerializer();

        delegate void WriteDelegate(ref MessagePackWriter writer, ReadOnlySpan<byte> ys);

        [Theory]
        [InlineData('a', 1)]
        [InlineData('b', 10)]
        [InlineData('c', 100)]
        [InlineData('d', 1000)]
        [InlineData('e', 10000)]
        [InlineData('f', 100000)]
        public void GetEncodedStringBytes(char c, int count)
        {
            var s = new string(c, count);
            var bin1 = CodeGenHelpers.GetEncodedStringBytes(s);
            var bin2 = serializer.Serialize(s);
            var bin3Writer = new MessagePackWriter();
            bin3Writer.WriteRaw(bin1);
            bin3Writer.Flush();

            MessagePack.Internal.ByteArrayComparer.Equals(bin1, bin2).IsTrue();
            MessagePack.Internal.ByteArrayComparer.Equals(bin1, CodeGenHelpers.GetSpanFromSequence(bin3Writer.WrittenBytes)).IsTrue();
        }

        [Fact]
        public void WriteRaw()
        {
            // x86
            for (int i = 1; i <= MessagePackRange.MaxFixStringLength; i++)
            {
                var src = Enumerable.Range(0, i).Select(x => (byte)x).ToArray();
                var dstWriter = new MessagePackWriter();
                ((typeof(UnsafeMemory32).GetMethod("WriteRaw" + i)).CreateDelegate(typeof(WriteDelegate)) as WriteDelegate).Invoke(ref dstWriter, src);
                dstWriter.Flush();
                dstWriter.WrittenBytes.Length.Is(i);
                MessagePack.Internal.ByteArrayComparer.Equals(src, CodeGenHelpers.GetSpanFromSequence(dstWriter.WrittenBytes)).IsTrue();
            }
            // x64
            for (int i = 1; i <= MessagePackRange.MaxFixStringLength; i++)
            {
                var src = Enumerable.Range(0, i).Select(x => (byte)x).ToArray();
                var dstWriter = new MessagePackWriter();
                ((typeof(UnsafeMemory64).GetMethod("WriteRaw" + i)).CreateDelegate(typeof(WriteDelegate)) as WriteDelegate).Invoke(ref dstWriter, src);
                dstWriter.Flush();
                dstWriter.WrittenBytes.Length.Is(i);
                MessagePack.Internal.ByteArrayComparer.Equals(src, CodeGenHelpers.GetSpanFromSequence(dstWriter.WrittenBytes)).IsTrue();
            }
        }
    }
}
