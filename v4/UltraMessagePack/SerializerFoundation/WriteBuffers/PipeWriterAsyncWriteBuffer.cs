using System.IO.Pipelines;

namespace SerializerFoundation;

public class PipeWriterAsyncWriteBuffer : IAsyncWriteBuffer
{
    PipeWriter pipeWriter;
    PointerSpan buffer;
    MemoryHandle bufferHandle;
    int writtenInBuffer;
    long totalWritten;

    public long BytesWritten => totalWritten;

    public PipeWriterAsyncWriteBuffer(PipeWriter pipeWriter)
    {
        this.pipeWriter = pipeWriter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSpan(int sizeHint, out Span<byte> span)
    {
        if (buffer.Length == 0 || (uint)buffer.Length < (uint)sizeHint)
        {
            span = default;
            return false;
        }

        span = buffer;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetReferenceOrNullRef(int sizeHint)
    {
        if (buffer.Length == 0 || (uint)buffer.Length < (uint)sizeHint)
        {
            return ref Unsafe.NullRef<byte>();
        }

        return ref buffer.GetReference(); // PointerSpan: plain pointer, no Span construction
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesWritten)
    {
        buffer = buffer.Slice(bytesWritten);
        writtenInBuffer += bytesWritten;
        totalWritten += bytesWritten;
    }

#if NET8_0_OR_GREATER
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
#endif
    public async ValueTask EnsureBufferAsync(int sizeHint, CancellationToken cancellationToken)
    {
        if (writtenInBuffer > 0)
        {
            pipeWriter.Advance(writtenInBuffer);
            writtenInBuffer = 0;
            buffer = default; // invalid once advanced; never hold a stale window across await
            await pipeWriter.FlushAsync(cancellationToken);
        }

        var memory = pipeWriter.GetMemory(sizeHint);
        unsafe
        {
            var handle = memory.Pin(); // keep pinned until next Ensure/Flush/Dispose
            bufferHandle.Dispose(); // unpin previous only after the new pin succeeded
            this.bufferHandle = handle;
            this.buffer = new PointerSpan((byte*)handle.Pointer, memory.Length);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (writtenInBuffer > 0)
            {
                pipeWriter.Advance(writtenInBuffer);
                writtenInBuffer = 0;
                buffer = default;
                await pipeWriter.FlushAsync(CancellationToken.None);
            }
        }
        finally
        {
            buffer = default;
            writtenInBuffer = 0;
            bufferHandle.Dispose();
        }
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken)
    {
        if (writtenInBuffer > 0)
        {
            pipeWriter.Advance(writtenInBuffer);
            await pipeWriter.FlushAsync(cancellationToken);
            buffer = default;
            writtenInBuffer = 0;
            bufferHandle.Dispose();
        }
    }
}
