using SerializerFoundation;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UltraMessagePack;
using Xunit;

namespace UltraMessagePack.Tests;

// the old `bool TryGetReference(int, ref byte)` shape compiled but could never deliver the
// reference (ref-reassigning a ref parameter only rebinds the callee-local); these tests
// pin the GetReferenceOrNullRef replacement to observable behavior through a real Pipe
public class AsyncBufferReferenceTests
{
    [Fact]
    public async Task WriteBuffer_GetReferenceOrNullRef_DeliversBufferReference()
    {
        var pipe = new Pipe();
        var wb = new PipeWriterAsyncWriteBuffer(pipe.Writer);

        // no buffer acquired yet: probe must report NullRef
        {
            ref byte probe = ref wb.GetReferenceOrNullRef(MessagePackPrimitives.MaxInt32Length);
            Assert.True(Unsafe.IsNullRef(ref probe));
        }

        await wb.EnsureBufferAsync(MessagePackPrimitives.MaxInt32Length, CancellationToken.None);

        {
            ref byte d = ref wb.GetReferenceOrNullRef(MessagePackPrimitives.MaxInt32Length);
            Assert.False(Unsafe.IsNullRef(ref d));
            wb.Advance(MessagePackPrimitives.UnsafeWriteInt32(ref d, 123456));
        }

        await wb.FlushAsync(CancellationToken.None);
        await pipe.Writer.CompleteAsync();

        var result = await pipe.Reader.ReadAsync();
        var actual = result.Buffer.ToArray();

        var expected = new byte[MessagePackPrimitives.MaxInt32Length];
        var expectedLength = MessagePackPrimitives.UnsafeWriteInt32(ref expected[0], 123456);
        Assert.Equal(expected.AsSpan(0, expectedLength).ToArray(), actual);
    }

    [Fact]
    public async Task ReadBuffer_GetReferenceOrNullRef_DeliversBufferReference()
    {
        var pipe = new Pipe();
        byte[] payload = [0xCD, 0x12, 0x34];
        await pipe.Writer.WriteAsync(payload);
        await pipe.Writer.CompleteAsync();

        var rb = new PipeReaderAsyncReadBuffer(pipe.Reader);

        {
            ref readonly byte probe = ref rb.GetReferenceOrNullRef(payload.Length);
            Assert.True(Unsafe.IsNullRef(in probe));
        }

        await rb.EnsureBufferAsync(payload.Length, CancellationToken.None);

        {
            ref readonly byte r = ref rb.GetReferenceOrNullRef(payload.Length);
            Assert.False(Unsafe.IsNullRef(in r));
            var view = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in r), payload.Length);
            Assert.Equal(payload, view.ToArray());
        }

        await rb.DisposeAsync();
    }
}
