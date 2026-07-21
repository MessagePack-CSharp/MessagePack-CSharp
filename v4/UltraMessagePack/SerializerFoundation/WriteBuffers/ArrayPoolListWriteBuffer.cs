namespace SerializerFoundation;

public ref struct ArrayPoolListWriteBuffer : IWriteBuffer, IDisposable
{
    PooledArrays pooledArrays;
    CompletedLengths completedLengths; // [0] = scratch, [1..] = pooled
    int pooledCount;

    Span<byte> scratchBuffer;
    Span<byte> currentBuffer;
    int currentWritten;

    public long BytesWritten
    {
        get
        {
            long total = 0;
            for (int i = 0; i < pooledCount; i++)
            {
                total += completedLengths[i];
            }
            return total + currentWritten;
        }
    }

    [Obsolete("Use scratchBuffer ctor instead.", true)]
    public ArrayPoolListWriteBuffer()
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ArrayPoolListWriteBuffer(Span<byte> scratchBuffer)
    {
        this.scratchBuffer = scratchBuffer;
        currentBuffer = scratchBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        var remaining = currentBuffer.Length - currentWritten;
        if (remaining == 0 || (uint)remaining < (uint)sizeHint)
        {
            return GetSpanSlow(sizeHint);
        }
        else
        {
#if !NETSTANDARD2_0
            return MemoryMarshal.CreateSpan(
                ref Unsafe.Add(ref MemoryMarshal.GetReference(currentBuffer), currentWritten),
                remaining);
#else
            return currentBuffer.Slice(currentWritten, remaining);
#endif
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetReference(int sizeHint = 0)
    {
        var remaining = currentBuffer.Length - currentWritten;
        if (remaining == 0 || (uint)remaining < (uint)sizeHint)
        {
            return ref MemoryMarshal.GetReference(GetSpanSlow(sizeHint));
        }
        else
        {
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(currentBuffer), currentWritten);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    Span<byte> GetSpanSlow(int sizeHint)
    {
        if (sizeHint <= 0) sizeHint = 1;

        if (currentBuffer.Length - currentWritten < sizeHint)
        {
            // finish current segment
            completedLengths[pooledCount] = currentWritten;

            // allocate next segment
            var minSize = GetMinSegmentSize(pooledCount);
            var requiredSize = Math.Max(sizeHint, minSize);
            var newArray = ArrayPool<byte>.Shared.Rent(requiredSize);
            pooledArrays[pooledCount++] = newArray;

            currentBuffer = newArray;
            currentWritten = 0;
        }

        return currentBuffer.Slice(currentWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesWritten)
    {
        currentWritten += bytesWritten;
    }

    public byte[] ToArray()
    {
        var totalLength = checked((int)BytesWritten);
        if (totalLength == 0) return [];

        var result = GC.AllocateUninitializedArray<byte>(totalLength);
        WriteTo(result);
        return result;
    }

    public void WriteTo(Span<byte> destination)
    {
        // copy scratch buffer
        var scratchLen = pooledCount > 0 ? completedLengths[0] : currentWritten;
        if (scratchLen > 0)
        {
            scratchBuffer.Slice(0, scratchLen).CopyTo(destination);
            destination = destination.Slice(scratchLen);
        }

        // copy pooled buffers
        for (int i = 0; i < pooledCount; i++)
        {
            var len = i < pooledCount - 1 ? completedLengths[i + 1] : currentWritten;
            pooledArrays[i]!.AsSpan(0, len).CopyTo(destination);
            destination = destination.Slice(len);
        }
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
        for (int i = 0; i < pooledCount; i++)
        {
            var array = pooledArrays[i];
            if (array != null)
            {
                ArrayPool<byte>.Shared.Return(array);
                pooledArrays[i] = null;
            }
        }
        pooledCount = 0;
        currentWritten = 0;
        currentBuffer = default;
    }

    // Segment sizes grow exponentially from 64KB to 1GB.
    // Index 15 returns Array.MaxLength to handle edge cases where callers
    // request large buffers via sizeHint but only partially consume them via Advance().
    // This ensures we never exceed 16 pooled segments regardless of usage pattern.
    // Example: GetSpan(1GB) followed by Advance(1) repeated would exhaust segments
    // quickly if we continued doubling, but Array.MaxLength guarantees any sizeHint fits.
    static int GetMinSegmentSize(int index) => index switch
    {
        0 => 65_536,
        1 => 131_072,
        2 => 262_144,
        3 => 524_288,
        4 => 1_048_576,
        5 => 2_097_152,
        6 => 4_194_304,
        7 => 8_388_608,
        8 => 16_777_216,
        9 => 33_554_432,
        10 => 67_108_864,
        11 => 134_217_728,
        12 => 268_435_456,
        13 => 536_870_912,
        14 => 1_073_741_824,
        15 => Array.MaxLength,
        _ => Throws.InsufficientSpaceInBuffer<int>(),
    };

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

    [StructLayout(LayoutKind.Sequential)]
    internal struct PooledArrays
    {
        byte[]? _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15;

        public ref byte[]? this[int index]
        {
            [System.Diagnostics.CodeAnalysis.UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Add(ref _0, index);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CompletedLengths
    {
        int _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16;

        public ref int this[int index]
        {
            [System.Diagnostics.CodeAnalysis.UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Add(ref _0, index);
        }
    }

#endif

    // iterator

    public WrittenSegmentIterator GetWrittenSegments()
    {
        return new WrittenSegmentIterator(ref this);
    }

    public ref struct WrittenSegmentIterator
    {
        readonly Span<byte> scratchBuffer;
        readonly PooledArrays pooledArrays;
        readonly CompletedLengths completedLengths;
        readonly int pooledCount;
        readonly int currentWritten;
        int index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal WrittenSegmentIterator(scoped ref ArrayPoolListWriteBuffer buffer)
        {
            this.scratchBuffer = buffer.scratchBuffer;
            this.pooledArrays = buffer.pooledArrays;
            this.completedLengths = buffer.completedLengths;
            this.pooledCount = buffer.pooledCount;
            this.currentWritten = buffer.currentWritten;
            this.index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetNext(out ReadOnlySpan<byte> segment)
        {
            index++;

            if (index == 0)
            {
                var len = pooledCount > 0 ? completedLengths[0] : currentWritten;
                if (len > 0)
                {
                    segment = scratchBuffer.Slice(0, len);
                    return true;
                }
                index++;
            }

            if ((uint)(index - 1) < (uint)pooledCount)
            {
                var pooledIndex = index - 1;
                var len = pooledIndex < pooledCount - 1
                    ? completedLengths[pooledIndex + 1]
                    : currentWritten;
                segment = pooledArrays[pooledIndex]!.AsSpan(0, len);
                return true;
            }

            segment = default;
            return false;
        }

        public void Reset()
        {
            index = -1;
        }
    }
}

// compatibility fallback for TFMs without `allows ref struct`: correctness and simplicity
// over peak performance (pinning per segment etc. is accepted)
public unsafe struct PointerArrayPoolListWriteBuffer : IWriteBuffer, IDisposable
{
    PooledArrays pooledArrays;
    CompletedLengths completedLengths; // [0] = scratch, [1..] = pooled
    int pooledCount;

    PointerSpan scratchBuffer;
    PointerSpan currentBuffer;
    MemoryHandle currentBufferHandle;
    int currentWritten;

    public long BytesWritten
    {
        get
        {
            long total = 0;
            for (int i = 0; i < pooledCount; i++)
            {
                total += completedLengths[i];
            }
            return total + currentWritten;
        }
    }

    [Obsolete("Use scratchBuffer ctor instead.", true)]
    public PointerArrayPoolListWriteBuffer()
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PointerArrayPoolListWriteBuffer(byte* scratchBuffer, int length)
    {
        this.scratchBuffer = new PointerSpan(scratchBuffer, length);
        this.currentBuffer = this.scratchBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (currentBuffer.Length == 0 || (uint)currentBuffer.Length < (uint)sizeHint)
        {
            return GetSpanSlow(sizeHint);
        }

        return currentBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetReference(int sizeHint = 0)
    {
        if (currentBuffer.Length == 0 || (uint)currentBuffer.Length < (uint)sizeHint)
        {
            return ref MemoryMarshal.GetReference(GetSpanSlow(sizeHint));
        }

        return ref currentBuffer.GetReference();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    Span<byte> GetSpanSlow(int sizeHint)
    {
        if (sizeHint <= 0) sizeHint = 1;

        // window representation: Advance shrinks currentBuffer, so Length IS the remaining
        // space (unlike the ref variant's full-span + currentWritten bookkeeping)
        if (currentBuffer.Length < sizeHint)
        {
            // finish current segment
            completedLengths[pooledCount] = currentWritten;

            // allocate next segment
            var minSize = GetMinSegmentSize(pooledCount);
            var requiredSize = Math.Max(sizeHint, minSize);
            var newArray = ArrayPool<byte>.Shared.Rent(requiredSize);
            pooledArrays[pooledCount++] = newArray;

            var memory = newArray.AsMemory();
            var handle = memory.Pin();
            currentBufferHandle.Dispose(); // unpin previous only after the new pin succeeded
            currentBufferHandle = handle;
            currentBuffer = new PointerSpan((byte*)currentBufferHandle.Pointer, memory.Length);
            currentWritten = 0;
        }
        return currentBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesWritten)
    {
        currentWritten += bytesWritten;
        currentBuffer = currentBuffer.Slice(bytesWritten); // Unlike the ref version, advance the window.
    }

    public byte[] ToArray()
    {
        var totalLength = checked((int)BytesWritten);
        if (totalLength == 0) return [];

        var result = GC.AllocateUninitializedArray<byte>(totalLength);
        WriteTo(result);
        return result;
    }

    public void WriteTo(Span<byte> destination)
    {
        // copy scratch buffer
        var scratchLen = pooledCount > 0 ? completedLengths[0] : currentWritten;
        if (scratchLen > 0)
        {
            scratchBuffer.AsSpan(0, scratchLen).CopyTo(destination);
            destination = destination.Slice(scratchLen);
        }

        // copy pooled buffers
        for (int i = 0; i < pooledCount; i++)
        {
            var len = i < pooledCount - 1 ? completedLengths[i + 1] : currentWritten;
            pooledArrays[i]!.AsSpan(0, len).CopyTo(destination);
            destination = destination.Slice(len);
        }
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
        for (int i = 0; i < pooledCount; i++)
        {
            var array = pooledArrays[i];
            if (array != null)
            {
                ArrayPool<byte>.Shared.Return(array);
                pooledArrays[i] = null;
            }
        }
        pooledCount = 0;
        currentWritten = 0;
        currentBuffer = default;
        currentBufferHandle.Dispose();
    }

    // Segment sizes grow exponentially from 64KB to 1GB.
    // Index 15 returns Array.MaxLength to handle edge cases where callers
    // request large buffers via sizeHint but only partially consume them via Advance().
    // This ensures we never exceed 16 pooled segments regardless of usage pattern.
    // Example: GetSpan(1GB) followed by Advance(1) repeated would exhaust segments
    // quickly if we continued doubling, but Array.MaxLength guarantees any sizeHint fits.
    static int GetMinSegmentSize(int index) => index switch
    {
        0 => 65_536,
        1 => 131_072,
        2 => 262_144,
        3 => 524_288,
        4 => 1_048_576,
        5 => 2_097_152,
        6 => 4_194_304,
        7 => 8_388_608,
        8 => 16_777_216,
        9 => 33_554_432,
        10 => 67_108_864,
        11 => 134_217_728,
        12 => 268_435_456,
        13 => 536_870_912,
        14 => 1_073_741_824,
        15 => Array.MaxLength,
        _ => Throws.InsufficientSpaceInBuffer<int>(),
    };

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

    [StructLayout(LayoutKind.Sequential)]
    internal struct PooledArrays
    {
        byte[]? _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15;

        public ref byte[]? this[int index]
        {
            [System.Diagnostics.CodeAnalysis.UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Add(ref _0, index);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CompletedLengths
    {
        int _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16;

        public ref int this[int index]
        {
            [System.Diagnostics.CodeAnalysis.UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Add(ref _0, index);
        }
    }

#endif

    // iterator
    public WrittenSegmentIterator GetWrittenSegments()
    {
        return new WrittenSegmentIterator(ref this);
    }

    public ref struct WrittenSegmentIterator
    {
        readonly PointerSpan scratchBuffer;
        readonly PooledArrays pooledArrays;
        readonly CompletedLengths completedLengths;
        readonly int pooledCount;
        readonly int currentWritten;
        int index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal WrittenSegmentIterator(scoped ref PointerArrayPoolListWriteBuffer buffer)
        {
            this.scratchBuffer = buffer.scratchBuffer;
            this.pooledArrays = buffer.pooledArrays;
            this.completedLengths = buffer.completedLengths;
            this.pooledCount = buffer.pooledCount;
            this.currentWritten = buffer.currentWritten;
            this.index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetNext(out ReadOnlySpan<byte> segment)
        {
            index++;

            if (index == 0)
            {
                var len = pooledCount > 0 ? completedLengths[0] : currentWritten;
                if (len > 0)
                {
                    segment = scratchBuffer.AsSpan(0, len);
                    return true;
                }
                index++;
            }

            if ((uint)(index - 1) < (uint)pooledCount)
            {
                var pooledIndex = index - 1;
                var len = pooledIndex < pooledCount - 1
                    ? completedLengths[pooledIndex + 1]
                    : currentWritten;
                segment = pooledArrays[pooledIndex]!.AsSpan(0, len);
                return true;
            }

            segment = default;
            return false;
        }

        public void Reset()
        {
            index = -1;
        }
    }
}
