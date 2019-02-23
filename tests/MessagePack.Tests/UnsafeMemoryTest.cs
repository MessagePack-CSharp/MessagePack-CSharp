using MessagePack.Internal;
using System;
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

        delegate int WriteDelegate(ref byte[] xs, int offset, byte[] ys);

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
            var bin1 = MessagePackBinary.GetEncodedStringBytes(s);
            var bin2 = serializer.Serialize(s);
            byte[] bin3 = null;
            var size = MessagePackBinary.WriteRaw(ref bin3, 0, bin1);
            MessagePackBinary.FastResize(ref bin3, size);

            MessagePack.Internal.ByteArrayComparer.Equals(bin1, bin2).IsTrue();
            MessagePack.Internal.ByteArrayComparer.Equals(bin1, bin3).IsTrue();
        }

        [Fact]
        public void WriteRaw()
        {
            // x86
            for (int i = 1; i <= MessagePackRange.MaxFixStringLength; i++)
            {
                var src = Enumerable.Range(0, i).Select(x => (byte)x).ToArray();
                byte[] dst = null;
                var len = ((typeof(UnsafeMemory32).GetMethod("WriteRaw" + i)).CreateDelegate(typeof(WriteDelegate)) as WriteDelegate).Invoke(ref dst, 0, src);
                len.Is(i);
                MessagePackBinary.FastResize(ref dst, len);
                MessagePack.Internal.ByteArrayComparer.Equals(src, dst).IsTrue();
            }
            // x64
            for (int i = 1; i <= MessagePackRange.MaxFixStringLength; i++)
            {
                var src = Enumerable.Range(0, i).Select(x => (byte)x).ToArray();
                byte[] dst = null;
                var len = ((typeof(UnsafeMemory64).GetMethod("WriteRaw" + i)).CreateDelegate(typeof(WriteDelegate)) as WriteDelegate).Invoke(ref dst, 0, src);
                len.Is(i);
                MessagePackBinary.FastResize(ref dst, len);
                MessagePack.Internal.ByteArrayComparer.Equals(src, dst).IsTrue();
            }
            // x86, offset
            for (int i = 1; i <= MessagePackRange.MaxFixStringLength; i++)
            {
                var src = Enumerable.Range(0, i).Select(x => (byte)x).ToArray();
                byte[] dst = new byte[3];
                var len = ((typeof(UnsafeMemory32).GetMethod("WriteRaw" + i)).CreateDelegate(typeof(WriteDelegate)) as WriteDelegate).Invoke(ref dst, 3, src);
                len.Is(i);
                dst = dst.Skip(3).Take(len).ToArray();
                MessagePack.Internal.ByteArrayComparer.Equals(src, dst).IsTrue();
            }
            // x64, offset
            for (int i = 1; i <= MessagePackRange.MaxFixStringLength; i++)
            {
                var src = Enumerable.Range(0, i).Select(x => (byte)x).ToArray();
                byte[] dst = new byte[3];
                var len = ((typeof(UnsafeMemory64).GetMethod("WriteRaw" + i)).CreateDelegate(typeof(WriteDelegate)) as WriteDelegate).Invoke(ref dst, 3, src);
                len.Is(i);
                dst = dst.Skip(3).Take(len).ToArray();
                MessagePack.Internal.ByteArrayComparer.Equals(src, dst).IsTrue();
            }
        }
    }
}
