namespace SerializerFoundation;

/// <summary>
/// An <see cref="IReadBuffer"/> over a single contiguous block of memory.
/// The standard entry point for reading from a byte array or span.
/// </summary>
public ref struct ReadOnlySpanReadBuffer : IReadBuffer
{
    readonly ReadOnlySpan<byte> buffer;
    int consumed;

    public long BytesConsumed => consumed;
    public long BytesRemaining => buffer.Length - consumed;

    /// <summary>Creates a read buffer over <paramref name="buffer"/>.</summary>
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

    // always contiguous, so this is the one copy the destination inherently needs.
    // Span.Length is never negative, so a plain compare covers the guard.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Span<byte> destination)
    {
        if (destination.Length > buffer.Length - consumed)
        {
            Throws.InsufficientDataInBuffer();
        }
        buffer.Slice(consumed, destination.Length).CopyTo(destination);
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

/// <summary>
/// A <see cref="ReadOnlySpanReadBuffer"/> variant over pointer memory for target frameworks without <c>allows ref struct</c> support.
/// </summary>
public unsafe struct CompatibleReadOnlySpanReadBuffer : IReadBuffer
{
    readonly PointerSpan buffer;
    int consumed;

    public long BytesConsumed => consumed;
    public long BytesRemaining => buffer.Length - consumed;

    /// <summary>
    /// Creates a read buffer over <paramref name="length"/> bytes starting at <paramref name="buffer"/>.
    /// The memory must stay valid and pinned while in use.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CompatibleReadOnlySpanReadBuffer(byte* buffer, int length)
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

    // always contiguous, see ReadOnlySpanReadBuffer.CopyTo
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Span<byte> destination)
    {
        if (destination.Length > buffer.Length - consumed)
        {
            Throws.InsufficientDataInBuffer();
        }
        buffer.AsSpan(consumed, destination.Length).CopyTo(destination);
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
