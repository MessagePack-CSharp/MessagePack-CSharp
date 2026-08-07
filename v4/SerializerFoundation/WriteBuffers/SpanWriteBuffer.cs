// A class for handling fixed buffers. When it is obvious that the maximum processing size is a fixed length, it allows things like writing directly to stackalloc.
// It is not normally used inside the serializer framework.

namespace SerializerFoundation;

public ref struct SpanWriteBuffer : IWriteBuffer
{
    readonly Span<byte> buffer;
    int written;

    public long BytesWritten => written;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanWriteBuffer(Span<byte> buffer)
    {
        this.buffer = buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        var remaining = buffer.Length - written;
        if (remaining == 0 || (uint)remaining < (uint)sizeHint)
        {
            if (sizeHint < 0) Throws.ArgumentOutOfRange();
            Throws.InsufficientSpaceInBuffer();
        }

#if !NETSTANDARD2_0
        // The JIT does not always eliminate the range check inside Slice.
        // This is a hot path and called frequently, so we avoid Slice here since the bounds are already checked by the branch above.
        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), written),
            remaining);
#else
        return buffer.Slice(written, remaining);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesWritten)
    {
        if ((uint)bytesWritten > (uint)(buffer.Length - written))
        {
            Throws.AdvancedTooFar();
        }
        written += bytesWritten;
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}

// compatibility fallback for Target Framework without `allows ref struct`
public unsafe struct CompatibleSpanWriteBuffer : IWriteBuffer
{
    readonly PointerSpan buffer;
    int written;

    public long BytesWritten => written;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CompatibleSpanWriteBuffer(byte* buffer, int length)
    {
        this.buffer = new PointerSpan(buffer, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        var remaining = buffer.Length - written;
        if (remaining == 0 || (uint)remaining < (uint)sizeHint)
        {
            if (sizeHint < 0) Throws.ArgumentOutOfRange();
            Throws.InsufficientSpaceInBuffer();
        }
        return buffer.AsSpan(written, remaining);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesWritten)
    {
        if ((uint)bytesWritten > (uint)(buffer.Length - written))
        {
            Throws.AdvancedTooFar();
        }
        written += bytesWritten;
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}
