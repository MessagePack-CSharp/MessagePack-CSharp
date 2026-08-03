namespace SerializerFoundation;

// TODO: this define is temporaly, not fixed yet.

public interface IAsyncReadBuffer : IAsyncDisposable
{
    bool TryGetSpan(int sizeHint, out ReadOnlySpan<byte> span);

    // TODO: It is not decided yet whether this definition will be used
    ref readonly byte GetReferenceOrNullRef(int sizeHint);

    ValueTask EnsureBufferAsync(int sizeHint, CancellationToken cancellationToken);
    void Advance(int bytesConsumed);
    long BytesConsumed { get; }
}
