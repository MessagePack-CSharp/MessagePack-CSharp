namespace SerializerFoundation;

public interface IAsyncReadBuffer : IAsyncDisposable
{
    bool TryGetSpan(int sizeHint, out ReadOnlySpan<byte> span);

    /// <summary>
    /// Fast-path probe: returns a reference to at least <paramref name="sizeHint"/> readable
    /// bytes, or <see cref="Unsafe.NullRef{T}"/> when the buffer must first be refilled via
    /// EnsureBufferAsync. Check with <see cref="Unsafe.IsNullRef{T}(ref readonly T)"/>; a ref
    /// obtained before EnsureBufferAsync is invalid afterwards and must be re-acquired.
    /// </summary>
    ref readonly byte GetReferenceOrNullRef(int sizeHint);
    ValueTask EnsureBufferAsync(int sizeHint, CancellationToken cancellationToken);
    void Advance(int bytesConsumed);
    long BytesConsumed { get; }
}
