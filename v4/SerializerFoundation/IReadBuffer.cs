namespace SerializerFoundation;

/// <summary>
/// Source that serializers read from.
/// Inspect bytes through <see cref="GetCurrentSpan"/> or <see cref="TryGetSpan"/>, then consume them with <see cref="Advance"/>.
/// </summary>
public interface IReadBuffer : IDisposable
{
    /// <summary>Total number of bytes consumed so far.</summary>
    long BytesConsumed { get; }

    /// <summary>Number of unread bytes left in the data.</summary>
    long BytesRemaining { get; }

    /// <summary>
    /// Returns the unread portion of the current contiguous window, with no size requirement.
    /// May be empty when the data is exhausted. Never throws.
    /// </summary>
    ReadOnlySpan<byte> GetCurrentSpan();

    /// <summary>
    /// Returns a contiguous window of at least <paramref name="sizeHint"/> bytes, copying across segment seams as needed,
    /// or false when fewer bytes remain in the data.
    /// A <paramref name="sizeHint"/> of 0 always succeeds and may return an empty span.
    /// A negative <paramref name="sizeHint"/> is a caller bug and throws <see cref="ArgumentOutOfRangeException"/>.
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
