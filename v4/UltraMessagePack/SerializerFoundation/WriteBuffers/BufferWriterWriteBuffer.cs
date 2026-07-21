using System;

namespace SerializerFoundation;

// THE IBufferWriter<byte> buffer, deliberately non-generic. A generic
// BufferWriterWriteBuffer<TBufferWriter> over a reference-type writer compiles the whole
// formatter as __Canon-shared code, turning every GetReference/Advance into a
// dictionary-fetched indirect call (measured 14.2ns vs flat on a 2-field POCO serialize) —
// and real writers (PipeWriter, ArrayBufferWriter) are classes. Holding the interface
// keeps formatter instantiations fully specialized; writer dispatch is confined to
// GetSpanSlow/Flush (once or twice per Serialize call). The struct-writer generic variant
// was deleted with the ref-passing entry: wrap-a-struct-for-performance callers should
// implement IWriteBuffer directly, which is strictly better (fully specialized formatters,
// no wrapper layer).
//
// Buffer representation matches ArrayPoolListWriteBuffer: the full span handed out by the
// writer plus a written index. The previous window-slicing Advance carried a Slice range
// check the JIT provably cannot eliminate (DisasmProbe8 notes) plus a per-write long
// total; Advance is now a single add and the total accumulates only at Flush.
public ref struct BufferWriterWriteBuffer : IWriteBuffer
{
    readonly IBufferWriter<byte> bufferWriter;
    Span<byte> buffer;
    int writtenInBuffer;
    long totalFlushed; // accumulated at Flush only, keeping Advance to a single add

    public long BytesWritten => totalFlushed + writtenInBuffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BufferWriterWriteBuffer(IBufferWriter<byte> bufferWriter)
    {
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
        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), writtenInBuffer),
            remaining);
#else
        return buffer.Slice(writtenInBuffer, remaining);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetReference(int sizeHint = 0)
    {
        var remaining = buffer.Length - writtenInBuffer;
        if (remaining == 0 || (uint)remaining < (uint)sizeHint)
        {
            return ref MemoryMarshal.GetReference(GetSpanSlow(sizeHint));
        }
        return ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), writtenInBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytesWritten)
    {
        writtenInBuffer += bytesWritten;
    }

    public void Flush()
    {
        if (writtenInBuffer > 0)
        {
            bufferWriter.Advance(writtenInBuffer);
            totalFlushed += writtenInBuffer;
            writtenInBuffer = 0;
            // the old span is invalid once the writer has been advanced (IBufferWriter
            // contract: Advance invalidates previously returned spans); dropping it routes
            // the next Get* through GetSpanSlow
            buffer = default;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    Span<byte> GetSpanSlow(int sizeHint)
    {
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


// The GENERIC pointer variant (NonRefBufferWriterWriteBuffer<TBufferWriter>) was removed
// as strictly dominated: its class constraint made the generic __Canon-shared, zero
// specialization benefit over holding IBufferWriter<byte> directly. NOTE for the planned
// multi-targeting (`allows ref struct` #if'd out on old TFMs): old TFMs cannot pass ref
// structs through formatter generics at all, so the IBufferWriter entry there will need a
// NON-generic plain-struct variant — PointerBufferWriterWriteBuffer holding
// IBufferWriter<byte> + a PointerSpan window (same reason PointerArrayPoolListWriteBuffer
// exists). That shape was never the deleted one; add it when multi-targeting lands.
