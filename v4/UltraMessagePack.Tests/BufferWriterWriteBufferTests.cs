using SerializerFoundation;
using System.Buffers;
using Xunit;

namespace UltraMessagePack.Tests;

// pins the index-representation rewrite (Advance = single add, total accumulated at Flush):
// writes cross several underlying segments (small initial ArrayBufferWriter), and
// BytesWritten must be exact both mid-write (lazy total) and after Flush
public class BufferWriterWriteBufferTests
{
    static byte[] ExpectedBytes(int count) => [.. Enumerable.Range(0, count).Select(i => (byte)i)];

    [Fact]
    public void NonGeneric_CrossesSegments_AndCountsLazily()
    {
        var inner = new ArrayBufferWriter<byte>(16);
        var b = new BufferWriterWriteBuffer(inner);

        for (int i = 0; i < 100; i++)
        {
            b.GetReference(1) = (byte)i;
            b.Advance(1);
            Assert.Equal(i + 1, b.BytesWritten); // mid-write, before any explicit Flush
        }

        b.Flush();
        Assert.Equal(100, b.BytesWritten); // unchanged across the flush boundary
        b.Dispose();

        Assert.Equal(ExpectedBytes(100), inner.WrittenSpan.ToArray());
    }

    [Fact]
    public void NonGeneric_BulkThenSmall_RespectsSizeHint()
    {
        var inner = new ArrayBufferWriter<byte>(16);
        var b = new BufferWriterWriteBuffer(inner);

        // reservation larger than the current segment forces the slow path
        var span = b.GetSpan(64);
        Assert.True(span.Length >= 64);
        for (int i = 0; i < 64; i++) span[i] = (byte)i;
        b.Advance(64);

        b.GetReference(1) = 64;
        b.Advance(1);

        b.Dispose(); // Dispose alone must flush
        Assert.Equal(ExpectedBytes(65), inner.WrittenSpan.ToArray());
    }

}
