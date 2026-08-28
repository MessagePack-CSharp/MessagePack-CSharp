namespace SerializerFoundation;

// The IBufferWriter<byte> buffer, deliberately non-generic.
// A generic BufferWriterWriteBuffer<TBufferWriter> over a reference-type writer compiles the whole
// formatter as __Canon-shared code, turning every GetReference/Advance into a dictionary-fetched indirect call.
// And real writers (PipeWriter, ArrayBufferWriter) are classes.
// Holding the interface keeps formatter instantiations fully specialized;
// writer dispatch is confined to GetSpanSlow/Flush (once or twice per Serialize call).

// If you want to add special hooks on write, implement IWriteBuffer instead of a custom IBufferWriter.

/// <summary>
/// An <see cref="IWriteBuffer"/> that writes into an <see cref="IBufferWriter{T}"/> such as PipeWriter or ArrayBufferWriter.
/// Writes are staged in the writer's current span and committed to the writer on <see cref="Flush"/> or Dispose.
/// </summary>
public ref struct BufferWriterWriteBuffer : IWriteBuffer
{
    readonly IBufferWriter<byte> bufferWriter;
    Span<byte> buffer;
    int writtenInBuffer;
    long totalFlushed; // accumulated at Flush only, keeping Advance to a single add

    public long BytesWritten => totalFlushed + writtenInBuffer;

    [Obsolete("Use bufferWriter ctor instead.", true)]
    public BufferWriterWriteBuffer()
    {
        this.bufferWriter = default!;
    }

    /// <summary>Creates a write buffer over <paramref name="bufferWriter"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BufferWriterWriteBuffer(IBufferWriter<byte> bufferWriter)
    {
        ArgumentNullException.ThrowIfNull(bufferWriter);

        this.bufferWriter = bufferWriter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        var remaining = buffer.Length - writtenInBuffer;
        if (remaining == 0 || (uint)remaining < (uint)sizeHint)
        {
            return GetSpanSlow(sizeHint);
        }
#if !NETSTANDARD2_0
        // The JIT does not always eliminate the range check inside Slice.
        // This is a hot path and called frequently, so we avoid Slice here since the bounds are already checked by the branch above.
        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), writtenInBuffer),
            remaining);
#else
        return buffer.Slice(writtenInBuffer, remaining);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesWritten)
    {
        if ((uint)bytesWritten > (uint)(buffer.Length - writtenInBuffer))
        {
            Throws.AdvancedTooFar();
        }
        writtenInBuffer += bytesWritten;
    }

    public void Flush()
    {
        if (writtenInBuffer > 0)
        {
            bufferWriter.Advance(writtenInBuffer);
            totalFlushed += writtenInBuffer;
            writtenInBuffer = 0;
        }
        buffer = default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    Span<byte> GetSpanSlow(int sizeHint)
    {
        if (sizeHint < 0) Throws.ArgumentOutOfRange();
        Flush();
        buffer = bufferWriter.GetSpan(sizeHint);

        // validate IBufferWriter contract
        if (buffer.Length == 0 || buffer.Length < sizeHint)
        {
            Throws.InsufficientSpaceInBuffer();
        }
        return buffer;
    }

    public void Dispose()
    {
        Flush();
    }
}

/// <summary>
/// A <see cref="BufferWriterWriteBuffer"/> variant for target frameworks without <c>allows ref struct</c> support.
/// </summary>
public struct CompatibleBufferWriterWriteBuffer : IWriteBuffer
{
    readonly IBufferWriter<byte> bufferWriter;
    Memory<byte> buffer; // use Memory instead of Span to avoid ref struct
    int writtenInBuffer;
    long totalFlushed; // accumulated at Flush only, matching BufferWriterWriteBuffer

    public long BytesWritten => totalFlushed + writtenInBuffer;

    [Obsolete("Use bufferWriter ctor instead.", true)]
    public CompatibleBufferWriterWriteBuffer()
    {
        this.bufferWriter = default!;
    }

    /// <summary>Creates a write buffer over <paramref name="bufferWriter"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CompatibleBufferWriterWriteBuffer(IBufferWriter<byte> bufferWriter)
    {
        ArgumentNullException.ThrowIfNull(bufferWriter);

        this.bufferWriter = bufferWriter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        var remaining = buffer.Length - writtenInBuffer;
        if (remaining == 0 || (uint)remaining < (uint)sizeHint)
        {
            return GetSpanSlow(sizeHint);
        }
        return buffer.Span.Slice(writtenInBuffer, remaining);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesWritten)
    {
        if ((uint)bytesWritten > (uint)(buffer.Length - writtenInBuffer))
        {
            Throws.AdvancedTooFar();
        }
        writtenInBuffer += bytesWritten;
    }

    public void Flush()
    {
        if (writtenInBuffer > 0)
        {
            bufferWriter.Advance(writtenInBuffer);
            totalFlushed += writtenInBuffer;
            writtenInBuffer = 0;
        }
        buffer = default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    Span<byte> GetSpanSlow(int sizeHint)
    {
        if (sizeHint < 0) Throws.ArgumentOutOfRange();
        Flush();
        var memory = bufferWriter.GetMemory(sizeHint);

        // validate IBufferWriter contract
        if (memory.Length == 0 || memory.Length < sizeHint)
        {
            Throws.InsufficientSpaceInBuffer();
        }
        buffer = memory;
        return memory.Span;
    }

    public void Dispose()
    {
        Flush();
    }
}
