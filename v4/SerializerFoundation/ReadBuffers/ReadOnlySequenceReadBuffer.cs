namespace SerializerFoundation;

// This type is frequently used because PipeReader and others return ReadOnlySequence<byte>.
// Using Slice on ReadOnlySequence<byte> directly is slow, so the structure is designed
// to prefer operating on a series of chunks (ReadOnlySpan<byte>).
// Only at seams where the buffer space is not contiguous, we copy into a temporary
// buffer to create a contiguous buffer.
//
// Terminology used throughout:
//   STITCH: copy bytes that straddle a segment seam into contiguous storage (scratch or the retained temp) and serve that copy as the window.
//   COMMIT: apply the locally accumulated `currentConsumed` back into `sequence` via the expensive ReadOnlySequence.Slice (mirrors PipeReader.AdvanceTo).

public ref struct ReadOnlySequenceReadBuffer : IReadBuffer
{
    readonly long length; // original sequence length

    ReadOnlySequence<byte> sequence;  // positioned at the START of the current window
    ReadOnlySpan<byte> currentSpan;   // full current window (segment or stitched temp)

    // Stitch destinations, in preference order: caller-provided scratch (stackalloc'd at
    // the serializer entry, mirroring Serialize) for small windows — the fixed-size
    // tokens need at most 15 bytes, so numeric straddles never touch the pool — and a
    // RETAINED rented buffer for large ones (str/bin payloads), swapped only when it
    // must grow and returned only at Dispose.
    readonly Span<byte> scratch;
    byte[]? tempBuffer;

    long currentConsumed;    // consumed within currentSpan since the last commit
    long committedConsumed;  // consumption already committed into sequence

    public long BytesConsumed => committedConsumed + currentConsumed;
    public long BytesRemaining => length - committedConsumed - currentConsumed;

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
    public ReadOnlySpan<byte> GetCurrentSpan()
    {
        var remaining = currentSpan.Length - currentConsumed;
        if (remaining <= 0)
        {
            // fully consumed current-span, so commit and reposition onto the next non-empty segment (or empty window if exhausted)
            return CommitAndReposition();
        }

#if !NETSTANDARD2_0
        // The JIT does not always eliminate the range check inside Slice.
        // This is a hot path and called frequently, so we avoid Slice here since the bounds are guaranteed by the Advance invariant.
        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSpan), (nint)currentConsumed),
            (int)remaining);
#else
        return currentSpan.Slice((int)currentConsumed, (int)remaining);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSpan(int sizeHint, out ReadOnlySpan<byte> span)
    {
        var remaining = currentSpan.Length - currentConsumed;
        if (remaining <= 0 || remaining < sizeHint)
        {
            // temps, not the caller's out: passing `span` by address to the NoInlining
            // slow path would address-expose the inlined caller's local and pin the
            // span to a stack slot on the fast path too (see the slow-call note at
            // UltraMessagePack's MessagePackPrimitives.TryReadInt32)
            var slowFilled = TryGetSpanSlow(sizeHint, out var slowSpan);
            span = slowSpan;
            return slowFilled;
        }

#if !NETSTANDARD2_0
        span = MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSpan), (nint)currentConsumed),
            (int)remaining);
#else
        span = currentSpan.Slice((int)currentConsumed, (int)remaining);
#endif
        return true;
    }

    // Commit the fast-path consumption into the sequence, then reposition onto the
    // next non-empty segment. Shared by both slow paths; returns the new whole-segment
    // window, empty only when the data itself is exhausted.
    [MethodImpl(MethodImplOptions.NoInlining)]
    ReadOnlySpan<byte> CommitAndReposition()
    {
        if (currentConsumed != 0)
        {
            committedConsumed += currentConsumed;
            sequence = sequence.Slice(currentConsumed); // change the sequence to the unconsumed portion
            currentConsumed = 0;
        }

        // currentSpan is now exhausted, so the next segment is the new window
        currentSpan = sequence.FirstSpan;

        if (currentSpan.Length == 0 && sequence.Length != 0)
        {
            // Empty segment but data remains: walk to the next non-empty segment and
            // serve it WHOLE — returning an empty window here would be misread as
            // exhaustion. Empty segments contribute no bytes, so dropping them moves
            // no consumption bookkeeping.
            // same walk as BCL SequenceReader.GetNextSpan
            var segmentStart = sequence.Start;
            var position = segmentStart;
            while (sequence.TryGet(ref position, out var memory, advance: true))
            {
                if (memory.Length > 0)
                {
                    sequence = sequence.Slice(segmentStart); // set position
                    break;
                }
                segmentStart = position;
            }
            currentSpan = sequence.FirstSpan;
        }

        return currentSpan;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    bool TryGetSpanSlow(int sizeHint, out ReadOnlySpan<byte> span)
    {
        // argument error before any state mutation (the commit below moves state)
        if (sizeHint < 0) Throws.ArgumentOutOfRange();

        var window = CommitAndReposition();
        if (window.Length >= sizeHint)
        {
            // currentSpan satisfies sizeHint
            span = window;
            return true;
        }

        // shortage against the whole DATA: the caller owns how truncation surfaces.
        // plain signed compare: sequence.Length is a LONG, so the unsigned-fold idiom
        // would truncate it mod 2^32 and misreport shortage on multi-GB sequences;
        // both operands are already non-negative here (sizeHint checked above)
        if (sequence.Length < sizeHint)
        {
            span = default;
            return false;
        }

        // enough data, just not contiguous: stitch a contiguous window.
        // A stitched window is dead the moment the caller comes back here (values are materialized before Advance),
        // so scratch and the retained temp may be freely overwritten/reused.
        Span<byte> stitched;
        if (sizeHint <= scratch.Length)
        {
            stitched = scratch.Slice(0, sizeHint);
        }
        else
        {
            if (tempBuffer == null || tempBuffer.Length < sizeHint)
            {
                ReturnTempBuffer();
                tempBuffer = ArrayPool<byte>.Shared.Rent(sizeHint);
            }
            stitched = tempBuffer.AsSpan(0, sizeHint);
        }
        sequence.Slice(0, sizeHint).CopyTo(stitched);
        currentSpan = stitched;
        span = currentSpan;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesConsumed)
    {
        // currentConsumed may legally run past the current WINDOW (skip semantics: a skipped payload
        // crosses segments; the next GetSpan detects it via the signed remaining and commits)
        // but never past the end of the DATA: one unsigned compare against the total
        // remaining rejects negative and past-the-end alike — the write-side Advance shape
        // with the bound widened from the window to the sequence — maintaining
        // 0 <= committedConsumed + currentConsumed <= length (which also keeps the long currentConsumed finite).
        // Truncation is detected by the extension layer BEFORE advancing (skips validate
        // against BytesRemaining), so it still surfaces there as the domain exception.
        if ((ulong)(uint)bytesConsumed > (ulong)(length - committedConsumed - currentConsumed))
        {
            Throws.AdvancedTooFar();
        }
        currentConsumed += bytesConsumed;
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

// same window/currentConsumed representation as ReadOnlySequenceReadBuffer,
// just over a ReadOnlyMemory window unwrapped to a span per access.
public struct CompatibleReadOnlySequenceReadBuffer : IReadBuffer
{
    readonly long length; // original sequence length

    ReadOnlySequence<byte> sequence;  // positioned at the START of the current window
    ReadOnlyMemory<byte> currentMemory;   // full current window (segment or stitched temp)

    byte[]? tempBuffer;

    long currentConsumed;    // consumed within currentMemory since the last commit
    long committedConsumed;  // consumption already committed into sequence

    public long BytesConsumed => committedConsumed + currentConsumed;
    public long BytesRemaining => length - committedConsumed - currentConsumed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CompatibleReadOnlySequenceReadBuffer(in ReadOnlySequence<byte> sequence)
    {
        this.sequence = sequence;
        this.currentMemory = sequence.First;
        this.length = sequence.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetCurrentSpan()
    {
        var remaining = currentMemory.Length - currentConsumed;
        if (remaining <= 0)
        {
            // fully consumed current-span, so commit and reposition onto the next non-empty segment (or empty window if exhausted)
            return CommitAndReposition().Span;
        }

        return currentMemory.Span.Slice((int)currentConsumed, (int)remaining);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSpan(int sizeHint, out ReadOnlySpan<byte> span)
    {
        var remaining = currentMemory.Length - currentConsumed;
        if (remaining <= 0 || remaining < sizeHint)
        {
            // temps, not the caller's out: passing `span` by address to the NoInlining
            // slow path would address-expose the inlined caller's local and pin the
            // span to a stack slot on the fast path too (see the slow-call note at
            // UltraMessagePack's MessagePackPrimitives.TryReadInt32)
            var slowFilled = TryGetSpanSlow(sizeHint, out var slowSpan);
            span = slowSpan;
            return slowFilled;
        }

        span = currentMemory.Span.Slice((int)currentConsumed, (int)remaining);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    ReadOnlyMemory<byte> CommitAndReposition()
    {
        if (currentConsumed != 0)
        {
            committedConsumed += currentConsumed;
            sequence = sequence.Slice(currentConsumed);
            currentConsumed = 0;
        }

        currentMemory = sequence.First;

        if (currentMemory.Length == 0 && sequence.Length != 0)
        {
            var segmentStart = sequence.Start;
            var position = segmentStart;
            while (sequence.TryGet(ref position, out var memory, advance: true))
            {
                if (memory.Length > 0)
                {
                    sequence = sequence.Slice(segmentStart);
                    break;
                }
                segmentStart = position;
            }
            currentMemory = sequence.First;
        }

        return currentMemory;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    bool TryGetSpanSlow(int sizeHint, out ReadOnlySpan<byte> span)
    {
        // argument error before any state mutation (the commit below moves state)
        if (sizeHint < 0) Throws.ArgumentOutOfRange();

        var window = CommitAndReposition();
        if (window.Length >= sizeHint)
        {
            // currentMemory satisfies sizeHint
            span = window.Span;
            return true;
        }

        // shortage against the whole DATA: the caller owns how truncation surfaces.
        // plain signed compare: sequence.Length is a LONG, so the unsigned-fold idiom
        // would truncate it mod 2^32 and misreport shortage on multi-GB sequences;
        // both operands are already non-negative here (sizeHint checked above)
        if (sequence.Length < sizeHint)
        {
            span = default;
            return false;
        }

        // enough data, just not contiguous: stitch into the RETAINED temp
        // no scratch tier here, fallback path
        if (tempBuffer == null || tempBuffer.Length < sizeHint)
        {
            ReturnTempBuffer();
            tempBuffer = ArrayPool<byte>.Shared.Rent(sizeHint);
        }
        sequence.Slice(0, sizeHint).CopyTo(tempBuffer);
        currentMemory = tempBuffer.AsMemory(0, sizeHint);
        span = currentMemory.Span;
        return true;
    }

    // one unsigned compare against the total remaining, see ReadOnlySequenceReadBuffer.Advance
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesConsumed)
    {
        if ((ulong)(uint)bytesConsumed > (ulong)(length - committedConsumed - currentConsumed))
        {
            Throws.AdvancedTooFar();
        }
        currentConsumed += bytesConsumed;
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
        currentMemory = default;
    }
}
