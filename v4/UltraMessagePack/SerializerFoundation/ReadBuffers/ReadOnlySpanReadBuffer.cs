namespace SerializerFoundation;

// non ref-struct variation -> PointerReadBuffer

// Index representation matching the write buffers: the full source span plus a consumed
// index. Advance is a single add — the previous window-slicing Advance carried a Slice
// range check the JIT provably cannot eliminate plus a redundant length field
// (buffer.Length was always length - consumed).
public ref struct ReadOnlySpanReadBuffer : IReadBuffer
{
    readonly ReadOnlySpan<byte> buffer;
    int consumed;

    public long BytesConsumed => consumed;
    public long BytesRemaining => buffer.Length - consumed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpanReadBuffer(ReadOnlySpan<byte> buffer)
    {
        this.buffer = buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetSpan(int sizeHint = 0)
    {
        // sizeHint 0 (constant-folded per callsite) compiles to just the clamp: an
        // exhausted buffer returns an empty window and the primitives report
        // InsufficientBuffer — end-of-buffer then surfaces as the same domain exception
        // as any other truncation. The Min clamp (a cmov) is load-bearing: Advance has
        // skip semantics and may legally run past the end on truncated input (e.g. a
        // blind ext-payload skip); clamping the OFFSET keeps both the byref (at most
        // one-past-end, the GC-legal limit) and the length (never negative) in range.
        var offset = Math.Min(consumed, buffer.Length);
        var remaining = buffer.Length - offset;
        if ((uint)remaining < (uint)sizeHint)
        {
            Throws.InsufficientSpaceInBuffer();
        }

#if !NETSTANDARD2_0
        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), offset),
            remaining);
#else
        return buffer.Slice(offset, remaining);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesConsumed)
    {
        consumed += bytesConsumed;
    }

    public void Dispose()
    {
    }
}

// same index representation as ReadOnlySpanReadBuffer, just over a PointerSpan
public unsafe struct PointerReadBuffer : IReadBuffer
{
    readonly PointerSpan buffer;
    int consumed;

    public long BytesConsumed => consumed;
    public long BytesRemaining => buffer.Length - consumed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PointerReadBuffer(byte* buffer, int length)
    {
        this.buffer = new(buffer, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetSpan(int sizeHint = 0)
    {
        // allow-empty + skip-safe offset clamp, see ReadOnlySpanReadBuffer.GetSpan
        var offset = Math.Min(consumed, buffer.Length);
        var remaining = buffer.Length - offset;
        if ((uint)remaining < (uint)sizeHint)
        {
            Throws.InsufficientSpaceInBuffer();
        }

        return buffer.AsSpan(offset, remaining);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesConsumed)
    {
        consumed += bytesConsumed;
    }

    public void Dispose()
    {
    }
}
