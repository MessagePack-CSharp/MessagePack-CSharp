namespace SerializerFoundation;

// TODO: this define is temporaly, not fixed yet.

public interface IAsyncWriteBuffer : IAsyncDisposable
{
    bool TryGetSpan(int sizeHint, out Span<byte> span);

    // TODO: It is not decided yet whether this definition will be used
    ref byte GetReferenceOrNullRef(int sizeHint);

    ValueTask EnsureBufferAsync(int sizeHint, CancellationToken cancellationToken);
    void Advance(int bytesWritten);
    long BytesWritten { get; }
    ValueTask FlushAsync(CancellationToken cancellationToken);
}
