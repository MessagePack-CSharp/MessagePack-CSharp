namespace SerializerFoundation;

// Unlike SpanWriteBuffer on the write side, this is mainly used as the entry point for accepting byte[]
// The implementation itself is almost the same

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
    public ReadOnlySpan<byte> GetCurrentSpan()
    {
        // Advance bounds consumed by the whole buffer,
        // so remaining is never negative and the offset needs no clamp
        var remaining = buffer.Length - consumed;
#if !NETSTANDARD2_0
        // The JIT does not always eliminate the range check inside Slice.
        // This is a hot path and called frequently, so we avoid Slice here since the bounds are guaranteed by the Advance invariant.
        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), consumed),
            remaining);
#else
        return buffer.Slice(consumed, remaining);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSpan(int sizeHint, out ReadOnlySpan<byte> span)
    {
        var remaining = buffer.Length - consumed;
        if ((uint)remaining < (uint)sizeHint)
        {
            if (sizeHint < 0) Throws.ArgumentOutOfRange();
            span = default;
            return false;
        }

        span = GetCurrentSpan();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesConsumed)
    {
        if ((uint)bytesConsumed > (uint)(buffer.Length - consumed))
        {
            Throws.AdvancedTooFar();
        }
        consumed += bytesConsumed;
    }

    public void Dispose()
    {
    }
}

// compatibility fallback for Target Framework without `allows ref struct`
public unsafe struct UnsafeReadOnlySpanReadBuffer : IReadBuffer
{
    readonly PointerSpan buffer;
    int consumed;

    public long BytesConsumed => consumed;
    public long BytesRemaining => buffer.Length - consumed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeReadOnlySpanReadBuffer(byte* buffer, int length)
    {
        this.buffer = new(buffer, length);
    }

    // invariant-backed window, see ReadOnlySpanReadBuffer.GetCurrentSpan
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetCurrentSpan()
    {
        return buffer.AsSpan(consumed, buffer.Length - consumed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSpan(int sizeHint, out ReadOnlySpan<byte> span)
    {
        var remaining = buffer.Length - consumed;
        if ((uint)remaining < (uint)sizeHint)
        {
            if (sizeHint < 0) Throws.ArgumentOutOfRange();
            span = default;
            return false;
        }

        span = buffer.AsSpan(consumed, remaining);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesConsumed)
    {
        if ((uint)bytesConsumed > (uint)(buffer.Length - consumed))
        {
            Throws.AdvancedTooFar();
        }
        consumed += bytesConsumed;
    }

    public void Dispose()
    {
    }
}
