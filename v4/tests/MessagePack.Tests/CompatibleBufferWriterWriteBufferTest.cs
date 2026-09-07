using System.Buffers;
using SerializerFoundation;

namespace MessagePack.Tests;

// The fallback tier is the downlevel serializer's buffer set, but the types are plain
// structs that run on net10.0 too — so CompatibleBufferWriterWriteBuffer (pin-free
// Memory<byte> window, dropped on every Flush) gets real runtime coverage here instead
// of only compile coverage from the netstandard builds.
public class CompatibleBufferWriterWriteBufferTest
{
    [Fact]
    public void WritesAcrossWindowRefills()
    {
        var output = new ArrayBufferWriter<byte>(16);
        var buffer = new CompatibleBufferWriterWriteBuffer(output);
        var expected = new byte[1000];
        new Random(42).NextBytes(expected);

        var written = 0;
        while (written < expected.Length)
        {
            // prime-sized chunks against a 16-byte initial window force repeated
            // GetSpanSlow refills (flush + fresh GetMemory window)
            var chunk = Math.Min(7, expected.Length - written);
            var span = buffer.GetSpan(chunk);
            expected.AsSpan(written, chunk).CopyTo(span);
            buffer.Advance(chunk);
            written += chunk;
        }

        Assert.Equal(expected.Length, buffer.BytesWritten);
        buffer.Dispose();
        Assert.Equal(expected, output.WrittenSpan.ToArray());
        Assert.Equal(expected.Length, buffer.BytesWritten);
    }

    [Fact]
    public void GetReferencePathAndIdempotentDispose()
    {
        var output = new ArrayBufferWriter<byte>(8);
        var buffer = new CompatibleBufferWriterWriteBuffer(output);
        for (var i = 0; i < 100; i++)
        {
            ref var reference = ref buffer.GetReference(1);
            reference = (byte)i;
            buffer.Advance(1);
        }
        buffer.Dispose();
        buffer.Dispose();

        Assert.Equal(Enumerable.Range(0, 100).Select(static x => (byte)x).ToArray(), output.WrittenSpan.ToArray());
    }
}
