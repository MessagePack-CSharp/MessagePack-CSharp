namespace SerializerFoundation;

public interface IReadBuffer : IDisposable
{
    /// <summary>
    /// Returns a contiguous window to read from.
    /// With a positive <paramref name="sizeHint"/> the returned span has at least that
    /// many bytes, or the method throws. With no <paramref name="sizeHint"/> (or 0) the
    /// remaining window is returned and MAY BE EMPTY when the buffer is exhausted — the
    /// read primitives report InsufficientBuffer with their exact requirement on an
    /// empty window, which lets the extension layer surface end-of-buffer as the same
    /// domain exception as any other truncation instead of a foundation throw.
    /// (No ref-returning accessor on the read side: a ref cannot express emptiness, and
    /// the sole consumer would have been an unused Peek helper — removed as YAGNI. The
    /// write side keeps GetReference, where capacity is guaranteed by contract.)
    /// </summary>
    ReadOnlySpan<byte> GetSpan(int sizeHint = 0);

    void Advance(int bytesConsumed);
    long BytesConsumed { get; }
    long BytesRemaining { get; }
}
