using System.IO.Pipelines;

namespace SerializerFoundation;

public class PipeReaderAsyncReadBuffer : IAsyncReadBuffer
{
    PipeReader pipeReader;
    ReadOnlySequence<byte> sequence;
    PointerSpan buffer;
    MemoryHandle bufferHandle;
    byte[]? tempBuffer;
    int readInBuffer;
    long totalConsumed;

    public long BytesConsumed => totalConsumed;

    public PipeReaderAsyncReadBuffer(PipeReader pipeReader)
    {
        this.pipeReader = pipeReader;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSpan(int sizeHint, out ReadOnlySpan<byte> span)
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
    public ref readonly byte GetReferenceOrNullRef(int sizeHint)
    {
        if (buffer.Length == 0 || (uint)buffer.Length < (uint)sizeHint)
        {
            return ref Unsafe.NullRef<byte>();
        }

        return ref buffer.GetReference(); // PointerSpan: plain pointer, no Span construction
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesConsumed)
    {
        buffer = buffer.Slice(bytesConsumed);
        readInBuffer += bytesConsumed;
        totalConsumed += bytesConsumed;
    }

#if NET8_0_OR_GREATER
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
#endif
    public async ValueTask EnsureBufferAsync(int sizeHint, CancellationToken cancellationToken)
    {
        ReturnTempBuffer();

        if (readInBuffer > 0)
        {
            pipeReader.AdvanceTo(sequence.GetPosition(readInBuffer));
            readInBuffer = 0;
        }

        if (sizeHint <= 0) sizeHint = 1; // minimum 1 byte

        var readResult = await pipeReader.ReadAtLeastAsync(sizeHint, cancellationToken);
        var buffer = readResult.Buffer;

        sequence = buffer;
        var memory = buffer.First;

        if ((uint)memory.Length < (uint)sizeHint)
        {
            // ReadAtLeast may return less than minimumSize when the end of the stream is reached.
            if ((uint)buffer.Length < (uint)sizeHint)
            {
                Throws.InsufficientSpaceInBuffer();
            }

            tempBuffer = ArrayPool<byte>.Shared.Rent(sizeHint);
            buffer.Slice(0, sizeHint).CopyTo(tempBuffer);
            SetSpan(tempBuffer.AsMemory(0, sizeHint));
        }
        else
        {
            SetSpan(memory);
        }
    }

    unsafe void SetSpan(ReadOnlyMemory<byte> memory)
    {
        var handle = memory.Pin();
        this.bufferHandle.Dispose(); // unpin previous only after the new pin succeeded
        this.bufferHandle = handle;
        this.buffer = new PointerSpan((byte*)handle.Pointer, memory.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReturnTempBuffer()
    {
        if (tempBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
            tempBuffer = null;
            buffer = default;
            bufferHandle.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        ReturnTempBuffer();
        buffer = default;
        bufferHandle.Dispose();
        return default;
    }
}
