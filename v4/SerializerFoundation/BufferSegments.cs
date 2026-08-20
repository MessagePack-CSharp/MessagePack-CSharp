namespace SerializerFoundation;

// backing storage shared by the pooled-list write buffers (both tiers) and the
// BufferSegments view; 16 exponentially-growing segments are enough to reach Array.MaxLength

#if NET9_0_OR_GREATER

[InlineArray(16)]
internal struct PooledArrays
{
    public byte[]? value;
}

[InlineArray(17)] // scratch(1) + pooled(16)
internal struct CompletedLengths
{
    public int value;
}

#else

internal struct PooledArrays
{
    byte[]? _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15;

    public ref byte[]? this[int index]
    {
        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            switch (index)
            {
                case 0: return ref _0;
                case 1: return ref _1;
                case 2: return ref _2;
                case 3: return ref _3;
                case 4: return ref _4;
                case 5: return ref _5;
                case 6: return ref _6;
                case 7: return ref _7;
                case 8: return ref _8;
                case 9: return ref _9;
                case 10: return ref _10;
                case 11: return ref _11;
                case 12: return ref _12;
                case 13: return ref _13;
                case 14: return ref _14;
                case 15: return ref _15;
                default: Throws.ArgumentOutOfRange(); return ref _0;
            }
        }
    }
}

internal struct CompletedLengths
{
    int _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16;

    public ref int this[int index]
    {
        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            switch (index)
            {
                case 0: return ref _0;
                case 1: return ref _1;
                case 2: return ref _2;
                case 3: return ref _3;
                case 4: return ref _4;
                case 5: return ref _5;
                case 6: return ref _6;
                case 7: return ref _7;
                case 8: return ref _8;
                case 9: return ref _9;
                case 10: return ref _10;
                case 11: return ref _11;
                case 12: return ref _12;
                case 13: return ref _13;
                case 14: return ref _14;
                case 15: return ref _15;
                case 16: return ref _16;
                default: Throws.ArgumentOutOfRange(); return ref _0;
            }
        }
    }
}

#endif

// a concrete ref struct works as a parameter type on every TFM (only generic positions need `allows ref struct`),

/// <summary>
/// A borrowed, forward-only view of a written message, exposed as zero-copy segments.
/// Valid only while the source buffer is alive and unmodified.
/// Instead of foreach, you can loop with `while (segments.TryGetNext(out var segment))`.
/// </summary>
public ref struct BufferSegments
{
    // first (scratch, pre-sliced; empty means "no first segment") followed by rented
    // arrays with normalized lengths ([i + 1] = finished length of pooled segment i,
    // the last one uses currentWritten)
    readonly ReadOnlySpan<byte> first;
    readonly PooledArrays pooledArrays;
    readonly CompletedLengths completedLengths;
    readonly int pooledCount;
    readonly int currentWritten;
    readonly long length;
    int index;

    /// <summary>Total message length in bytes.</summary>
    public long Length => length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal BufferSegments(ReadOnlySpan<byte> first, scoped in PooledArrays pooledArrays, scoped in CompletedLengths completedLengths, int pooledCount, int currentWritten, long length)
    {
        this.first = first;
        this.pooledArrays = pooledArrays;
        this.completedLengths = completedLengths;
        this.pooledCount = pooledCount;
        this.currentWritten = currentWritten;
        this.length = length;
        index = -1;
    }

    /// <summary>Retrieves the next segment. Returns false when no segments remain.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetNext(out ReadOnlySpan<byte> segment)
    {
        index++;

        if (index == 0)
        {
            if (first.Length > 0)
            {
                segment = first;
                return true;
            }
            index++;
        }

        var pooledIndex = index - 1;
        if ((uint)pooledIndex < (uint)pooledCount)
        {
            var len = pooledIndex < pooledCount - 1
                ? completedLengths[pooledIndex + 1]
                : currentWritten;
            segment = pooledArrays[pooledIndex]!.AsSpan(0, len);
            return true;
        }

        segment = default;
        return false;
    }

    /// <summary>Restarts iteration from the first segment.</summary>
    public void Reset()
    {
        index = -1;
    }
}
