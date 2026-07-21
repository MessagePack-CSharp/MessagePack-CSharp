namespace SerializerFoundation;

public interface IAsyncWriteBuffer : IAsyncDisposable
{
    bool TryGetSpan(int sizeHint, out Span<byte> span);

    /// <summary>
    /// Fast-path probe: returns a reference to at least <paramref name="sizeHint"/> writable
    /// bytes, or <see cref="Unsafe.NullRef{T}"/> when the buffer must first be refilled via
    /// EnsureBufferAsync. Check with <see cref="Unsafe.IsNullRef{T}(ref readonly T)"/>; a ref
    /// obtained before EnsureBufferAsync is invalid afterwards and must be re-acquired.
    /// (A `bool Try(ref byte)` shape cannot work — ref-reassigning a ref parameter only
    /// rebinds the callee-local; only a ref return reaches the caller.)
    /// </summary>
    ref byte GetReferenceOrNullRef(int sizeHint);
    ValueTask EnsureBufferAsync(int sizeHint, CancellationToken cancellationToken);
    void Advance(int bytesWritten);
    long BytesWritten { get; }
    ValueTask FlushAsync(CancellationToken cancellationToken);
}
