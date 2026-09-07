// Uses a pooling strategy close to dotnet/runtime's SegmentedArrayBuilder.
// Unlike that internal type, however, this is exposed as public API with a long code path,
// which raises the risk of a double-Return caused by copying the struct.
// For that reason, we bundle an Analyzer(NonCopyableBufferAnalyzer) that reports an error when a struct implementing IReadBuffer/IWriteBuffer is copied, to prevent this.
// I hope C# will support ownership as a language feature in the future. https://github.com/dotnet/csharplang/pull/10296

namespace SerializerFoundation;

/// <summary>
/// An <see cref="IWriteBuffer"/> that stages the message in a caller-provided scratch span first,
/// then in a chain of arrays rented from <see cref="ArrayPool{T}"/>.
/// Dispose returns the rented arrays and must be called exactly once.
/// </summary>
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

    /// <summary>
    /// Creates a write buffer that fills <paramref name="scratchBuffer"/> (typically stackalloc memory) before renting from the pool.
    /// </summary>
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
            // The JIT does not always eliminate the range check inside Slice.
            // This is a hot path and called frequently, so we avoid Slice here since the bounds are already checked by the branch above.
            return MemoryMarshal.CreateSpan(
                ref Unsafe.Add(ref MemoryMarshal.GetReference(currentBuffer), currentWritten),
                remaining);
#else
            return currentBuffer.Slice(currentWritten, remaining);
#endif
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    Span<byte> GetSpanSlow(int sizeHint)
    {
        if (sizeHint < 0) Throws.ArgumentOutOfRange();
        if (sizeHint == 0) sizeHint = 1;

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
        if ((uint)bytesWritten > (uint)(currentBuffer.Length - currentWritten))
        {
            Throws.AdvancedTooFar();
        }
        currentWritten += bytesWritten;
    }

    /// <summary>Copies the written message into a new array.</summary>
    public byte[] ToArray()
    {
        var totalLength = checked((int)BytesWritten);
        if (totalLength == 0) return [];

        var result = GC.AllocateUninitializedArray<byte>(totalLength);
        WriteTo(result);
        return result;
    }

    /// <summary>
    /// Copies the written message into <paramref name="destination"/>, which must be at least <see cref="BytesWritten"/> bytes long.
    /// </summary>
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

    /// <summary>Borrowed zero-copy view of the written message; valid until the next write or Dispose.</summary>
    public BufferSegments GetWrittenSegments()
    {
        var firstLength = pooledCount > 0 ? completedLengths[0] : currentWritten;
        return new BufferSegments(scratchBuffer.Slice(0, firstLength), in pooledArrays, in completedLengths, pooledCount, currentWritten, BytesWritten);
    }
}

// Boxed instances double as the interface-shaped staging buffer (interface calls mutate the box in place).
// However, that needs to be handled carefully.
// (The Analyzer will warn about this, so only disable it if you understand what it means)

/// <summary>
/// An <see cref="ArrayPoolListWriteBuffer"/> variant for target frameworks without <c>allows ref struct</c> support.
/// </summary>
public struct CompatibleArrayPoolListWriteBuffer : IWriteBuffer, IDisposable, IBufferWriter<byte>
{
    PooledArrays pooledArrays;
    CompletedLengths completedLengths; // [i] = finished length of pooled segment i
    int pooledCount;

    byte[]? currentArray; // == pooledArrays[pooledCount - 1] once the first segment is rented
    int currentWritten;

    public long BytesWritten
    {
        get
        {
            long total = 0;
            for (int i = 0; i < pooledCount - 1; i++)
            {
                total += completedLengths[i];
            }
            return total + currentWritten;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        var array = currentArray;
        if (array != null)
        {
            var remaining = array.Length - currentWritten;
            if (remaining != 0 && (uint)remaining >= (uint)sizeHint)
            {
                return array.AsSpan(currentWritten);
            }
        }
        return GetSpanSlow(sizeHint);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    Span<byte> GetSpanSlow(int sizeHint)
    {
        if (sizeHint < 0) Throws.ArgumentOutOfRange();
        if (sizeHint == 0) sizeHint = 1;

        var array = currentArray;
        if (array == null || array.Length - currentWritten < sizeHint)
        {
            // finish current segment
            if (pooledCount > 0)
            {
                completedLengths[pooledCount - 1] = currentWritten;
            }

            // allocate next segment
            var minSize = GetMinSegmentSize(pooledCount);
            var requiredSize = Math.Max(sizeHint, minSize);
            array = ArrayPool<byte>.Shared.Rent(requiredSize);
            pooledArrays[pooledCount++] = array;
            currentArray = array;
            currentWritten = 0;
        }
        return array.AsSpan(currentWritten);
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        GetSpan(sizeHint); // ensures capacity in currentArray
        return currentArray!.AsMemory(currentWritten);
    }

    // same guard as the ref variant, maintaining 0 <= currentWritten <= capacity;
    // before the first rent the capacity is 0, so any nonzero Advance throws
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesWritten)
    {
        var capacity = currentArray is null ? 0 : currentArray.Length;
        if ((uint)bytesWritten > (uint)(capacity - currentWritten))
        {
            Throws.AdvancedTooFar();
        }
        currentWritten += bytesWritten;
    }

    /// <summary>Copies the written message into a new array.</summary>
    public byte[] ToArray()
    {
        var totalLength = checked((int)BytesWritten);
        if (totalLength == 0) return [];

        var result = GC.AllocateUninitializedArray<byte>(totalLength);
        WriteTo(result);
        return result;
    }

    /// <summary>
    /// Copies the written message into <paramref name="destination"/>, which must be at least <see cref="BytesWritten"/> bytes long.
    /// </summary>
    public void WriteTo(Span<byte> destination)
    {
        for (int i = 0; i < pooledCount; i++)
        {
            var len = i < pooledCount - 1 ? completedLengths[i] : currentWritten;
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
        currentArray = null;
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

    // iterator

    /// <summary>Borrowed zero-copy view of the written message; valid until the next write or Dispose.</summary>
    public BufferSegments GetWrittenSegments()
    {
        // normalize this variant's lengths ([i] = pooled segment i) to the shared
        // scratch-first layout ([i + 1] = pooled segment i) expected by BufferSegments
        var normalized = default(CompletedLengths);
        for (int i = 0; i < pooledCount - 1; i++)
        {
            normalized[i + 1] = completedLengths[i];
        }
        return new BufferSegments(default, in pooledArrays, in normalized, pooledCount, currentWritten, BytesWritten);
    }
}
