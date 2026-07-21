namespace SerializerFoundation;

// Index representation matching ReadOnlySpanReadBuffer: currentSpan is the FULL current
// window (a whole segment, or a stitched temp buffer) and index counts the bytes
// consumed inside it since the last fold. The fold operation is UNIFORM for both window
// kinds — a stitched temp is a copy of the sequence front, so sequence.Slice(index)
// repositions correctly either way — which is what lets the Advance fast path drop both
// the window Slice (an uneliminable range check + 2-field span update) and the
// tempBuffer test, collapsing to a single add. The single-segment sequence (the dominant
// real-world case) then runs the exact ReadOnlySpanReadBuffer hot shape.
// Slicing a ReadOnlySequence is slow, so it happens only at window boundaries.
public ref struct ReadOnlySequenceReadBuffer : IReadBuffer
{
    ReadOnlySequence<byte> sequence;  // positioned at the START of the current window
    ReadOnlySpan<byte> currentSpan;   // full current window (segment or stitched temp)
    // Stitch destinations, in preference order: caller-provided scratch (stackalloc'd at
    // the serializer entry, mirroring Serialize) for small windows — the fixed-size
    // tokens need at most 15 bytes, so numeric straddles never touch the pool — and a
    // RETAINED rented buffer for large ones (str/bin payloads), swapped only when it
    // must grow and returned only at Dispose. The previous per-straddle Rent/Return
    // pair cost more than the stitch copy itself.
    readonly Span<byte> scratch;
    byte[]? tempBuffer;
    int index;                        // consumed within currentSpan since the last fold
    long foldedConsumed;              // consumption already applied to sequence (slow paths only)
    readonly long length;

    public long BytesConsumed => foldedConsumed + index;
    public long BytesRemaining => length - foldedConsumed - index;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySequenceReadBuffer(in ReadOnlySequence<byte> sequence)
        : this(in sequence, default)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySequenceReadBuffer(in ReadOnlySequence<byte> sequence, Span<byte> scratch)
    {
        this.sequence = sequence;
        this.currentSpan = sequence.FirstSpan;
        this.scratch = scratch;
        this.length = sequence.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetSpan(int sizeHint = 0)
    {
        // SIGNED compares are load-bearing: Advance may legally skip PAST the current
        // window (e.g. skipping an ext payload), leaving remaining negative — the
        // unsigned idiom would read that as huge and take the fast path with a negative
        // window length
        var remaining = currentSpan.Length - index;
        if (remaining <= 0 || remaining < sizeHint)
        {
            return GetSpanSlow(sizeHint);
        }

#if !NETSTANDARD2_0
        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSpan), index),
            remaining);
#else
        return currentSpan.Slice(index, remaining);
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    ReadOnlySpan<byte> GetSpanSlow(int sizeHint)
    {
        // Fold the fast-path consumption into the sequence — uniform for a partially
        // consumed segment, an active stitched window (scratch or temp copies the
        // sequence front, so index maps 1:1 either way), AND an index past the window
        // end (a skip: those bytes exist in the sequence, Slice just crosses segments).
        if (index != 0)
        {
            foldedConsumed += index;
            sequence = sequence.Slice(index);
            index = 0;
        }
        currentSpan = sequence.FirstSpan;

        // move to next segment
        if (currentSpan.Length == 0)
        {
            if (sequence.Length == 0)
            {
                // exhausted: a plain GetSpan() returns an empty window (the primitives
                // report InsufficientBuffer, so end-of-buffer surfaces as the same
                // domain exception as any other truncation); a positive requirement
                // still throws here
                if (sizeHint > 0)
                {
                    Throws.InsufficientSpaceInBuffer();
                }
                return default;
            }
            currentSpan = sequence.FirstSpan;
        }

        if (sizeHint <= 0) sizeHint = 1; // minimum 1 byte

        // still not enough: stitch a contiguous window. A stitched window is dead the
        // moment the caller comes back here (values are materialized before Advance),
        // so scratch and the retained temp may be freely overwritten/reused.
        if ((uint)currentSpan.Length < (uint)sizeHint)
        {
            if ((uint)sequence.Length < (uint)sizeHint)
            {
                Throws.InsufficientSpaceInBuffer();
            }

            Span<byte> window;
            if (sizeHint <= scratch.Length)
            {
                window = scratch.Slice(0, sizeHint);
            }
            else
            {
                if (tempBuffer == null || tempBuffer.Length < sizeHint)
                {
                    ReturnTempBuffer();
                    tempBuffer = ArrayPool<byte>.Shared.Rent(sizeHint);
                }
                window = tempBuffer.AsSpan(0, sizeHint);
            }
            sequence.Slice(0, sizeHint).CopyTo(window);
            currentSpan = window;
        }

        return currentSpan;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesConsumed)
    {
        // index may legally run past the current window (skip semantics, e.g. an ext
        // payload); the next GetSpan detects it via the signed remaining and folds
        index += bytesConsumed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReturnTempBuffer()
    {
        if (tempBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
            tempBuffer = null;
        }
    }

    public void Dispose()
    {
        ReturnTempBuffer();
        currentSpan = default;
    }
}

// same index representation as ReadOnlySequenceReadBuffer, just over a PointerSpan
// (fallback tier for TFMs without `allows ref struct`; structural mirror, not perf-tuned)
public unsafe struct PointerReadOnlySequenceReadBuffer : IReadBuffer
{
    ReadOnlySequence<byte> sequence;  // positioned at the START of the current window
    PointerSpan currentSpan;          // full current window (segment or stitched temp)
    MemoryHandle currentSpanHandle;
    byte[]? tempBuffer;
    int index;                        // consumed within currentSpan since the last fold
    long foldedConsumed;              // consumption already applied to sequence (slow paths only)
    readonly long length;

    public long BytesConsumed => foldedConsumed + index;
    public long BytesRemaining => length - foldedConsumed - index;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PointerReadOnlySequenceReadBuffer(in ReadOnlySequence<byte> sequence)
    {
        this.sequence = sequence;
        SetSpan(sequence.First);
        this.length = sequence.Length;
    }

    void SetSpan(ReadOnlyMemory<byte> memory)
    {
        this.currentSpanHandle.Dispose(); // unpin previous
        var handle = memory.Pin();
        this.currentSpanHandle = handle;
        this.currentSpan = new PointerSpan((byte*)handle.Pointer, memory.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetSpan(int sizeHint = 0)
    {
        var remaining = currentSpan.Length - index;
        if (remaining <= 0 || remaining < sizeHint) // signed on purpose, see ReadOnlySequenceReadBuffer.GetSpan
        {
            return GetSpanSlow(sizeHint);
        }

        return currentSpan.AsSpan(index, remaining);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    ReadOnlySpan<byte> GetSpanSlow(int sizeHint)
    {
        // fold the fast-path consumption into the sequence (uniform for segment and
        // stitched windows, see ReadOnlySequenceReadBuffer)
        if (index != 0)
        {
            foldedConsumed += index;
            sequence = sequence.Slice(index);
            index = 0;
        }
        SetSpan(sequence.First);

        // move to next segment
        if (currentSpan.Length == 0)
        {
            if (sequence.Length == 0)
            {
                // exhausted: empty for a plain GetSpan(), see ReadOnlySequenceReadBuffer
                if (sizeHint > 0)
                {
                    Throws.InsufficientSpaceInBuffer();
                }
                return default;
            }
            SetSpan(sequence.First);
        }

        if (sizeHint <= 0) sizeHint = 1; // minimum 1 byte

        // still not enough: stitch into the RETAINED temp (rented once, grown on
        // demand, returned at Dispose — no scratch tier here, fallback path)
        if ((uint)currentSpan.Length < (uint)sizeHint)
        {
            if ((uint)sequence.Length < (uint)sizeHint)
            {
                Throws.InsufficientSpaceInBuffer();
            }

            if (tempBuffer == null || tempBuffer.Length < sizeHint)
            {
                ReturnTempBuffer();
                tempBuffer = ArrayPool<byte>.Shared.Rent(sizeHint);
            }
            sequence.Slice(0, sizeHint).CopyTo(tempBuffer);
            SetSpan(tempBuffer.AsMemory(0, sizeHint));
        }

        return currentSpan;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesConsumed)
    {
        index += bytesConsumed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReturnTempBuffer()
    {
        // the pin handle is owned by SetSpan/Dispose, not by the temp's lifetime
        if (tempBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
            tempBuffer = null;
        }
    }

    public void Dispose()
    {
        ReturnTempBuffer();
        currentSpan = default;
        currentSpanHandle.Dispose();
    }
}
