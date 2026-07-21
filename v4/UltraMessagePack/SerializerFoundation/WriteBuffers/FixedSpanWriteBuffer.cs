namespace SerializerFoundation;

// non ref-struct variation -> FixedPointerWriteBuffer

// Index representation matching the other write buffers: the full destination span plus a
// written index; Advance is a single add (the previous window-slicing Advance carried a
// Slice range check the JIT provably cannot eliminate)
public ref struct FixedSpanWriteBuffer : IWriteBuffer
{
    readonly Span<byte> buffer;
    int written;

    public long BytesWritten => written;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FixedSpanWriteBuffer(Span<byte> buffer)
    {
        this.buffer = buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        var remaining = buffer.Length - written;
        if (remaining == 0 || (uint)remaining < (uint)sizeHint)
        {
            Throws.InsufficientSpaceInBuffer();
        }

#if !NETSTANDARD2_0
        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), written),
            remaining);
#else
        return buffer.Slice(written, remaining);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetReference(int sizeHint = 0)
    {
        var remaining = buffer.Length - written;
        if (remaining == 0 || (uint)remaining < (uint)sizeHint)
        {
            Throws.InsufficientSpaceInBuffer();
        }

        return ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), written);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesWritten)
    {
        written += bytesWritten;
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}

// same index representation as FixedSpanWriteBuffer, just over a PointerSpan
public unsafe struct FixedPointerWriteBuffer : IWriteBuffer
{
    readonly PointerSpan buffer;
    int written;

    public long BytesWritten => written;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FixedPointerWriteBuffer(byte* buffer, int length)
    {
        this.buffer = new PointerSpan(buffer, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        var remaining = buffer.Length - written;
        if (remaining == 0 || (uint)remaining < (uint)sizeHint)
        {
            Throws.InsufficientSpaceInBuffer();
        }
        return buffer.AsSpan(written, remaining);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetReference(int sizeHint = 0)
    {
        var remaining = buffer.Length - written;
        if (remaining == 0 || (uint)remaining < (uint)sizeHint)
        {
            Throws.InsufficientSpaceInBuffer();
        }
        return ref Unsafe.Add(ref buffer.GetReference(), written);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesWritten)
    {
        written += bytesWritten;
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}
