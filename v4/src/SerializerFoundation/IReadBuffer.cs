namespace SerializerFoundation;

/// <summary>
/// Source that serializers read from.
/// Inspect bytes through <see cref="GetCurrentSpan"/> or <see cref="TryGetSpan"/>, then consume them with <see cref="Advance"/>.
/// A span obtained from either stays valid until the next call to <see cref="GetCurrentSpan"/> or <see cref="TryGetSpan"/>,
/// or until <see cref="IDisposable.Dispose"/>: a later window may be served from temporary storage that the next request
/// overwrites or returns to a pool. Anything that may read from the buffer, such as a nested formatter, can request a window,
/// so re-request the span after such calls instead of holding on to it.
/// <see cref="Advance"/> and <see cref="CopyTo"/> do not invalidate a held span, although the bytes that Advance consumed are no longer unread data.
/// </summary>
public interface IReadBuffer : IDisposable
{
    /// <summary>Total number of bytes consumed so far.</summary>
    long BytesConsumed { get; }

    /// <summary>Number of unread bytes left in the data.</summary>
    long BytesRemaining { get; }

    /// <summary>
    /// Returns the unread portion of the current contiguous window, with no size requirement.
    /// Empty only when the data is exhausted: while <see cref="BytesRemaining"/> is positive the span holds at least one byte,
    /// so a segmented implementation repositions onto the next non-empty segment instead of returning an empty window.
    /// Never throws.
    /// The span is valid until the next <see cref="GetCurrentSpan"/> or <see cref="TryGetSpan"/> call, or Dispose.
    /// </summary>
    ReadOnlySpan<byte> GetCurrentSpan();

    /// <summary>
    /// Returns a contiguous window of at least <paramref name="sizeHint"/> bytes, copying across segment seams as needed,
    /// or false when fewer bytes remain in the data.
    /// A <paramref name="sizeHint"/> of 0 always succeeds and may return an empty span.
    /// A negative <paramref name="sizeHint"/> throws <see cref="ArgumentOutOfRangeException"/>.
    /// The span is valid until the next <see cref="GetCurrentSpan"/> or <see cref="TryGetSpan"/> call, or Dispose;
    /// a window copied across a seam lives in temporary storage that the next request reuses.
    /// </summary>
    bool TryGetSpan(int sizeHint, out ReadOnlySpan<byte> span);

    /// <summary>
    /// Consumes <paramref name="bytesConsumed"/> bytes.
    /// Throws <see cref="InvalidOperationException"/> when the value is negative or exceeds <see cref="BytesRemaining"/>.
    /// </summary>
    void Advance(int bytesConsumed);

    /// <summary>
    /// Copies the next <paramref name="destination"/>.Length bytes into <paramref name="destination"/> without consuming them;
    /// the caller advances afterwards.
    /// Multi-segment implementations copy straight out of their segments without building a contiguous window first.
    /// Throws <see cref="InvalidOperationException"/> when <paramref name="destination"/> is longer than <see cref="BytesRemaining"/>.
    /// </summary>
    void CopyTo(Span<byte> destination);
}
