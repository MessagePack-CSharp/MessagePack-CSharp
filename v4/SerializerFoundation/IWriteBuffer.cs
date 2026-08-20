namespace SerializerFoundation;

/// <summary>
/// Destination that serializers write to.
/// Request a window with <see cref="GetSpan"/>, write into it, then commit with <see cref="Advance"/>.
/// </summary>
public interface IWriteBuffer : IDisposable
{
    /// <summary>Total number of bytes written so far.</summary>
    long BytesWritten { get; }

    /// <summary>
    /// Returns a writable span of at least <paramref name="sizeHint"/> bytes.
    /// When <paramref name="sizeHint"/> is 0, some non-empty span is returned.
    /// A negative <paramref name="sizeHint"/> is a caller bug and throws <see cref="ArgumentOutOfRangeException"/> instead of being clamped.
    /// </summary>
    Span<byte> GetSpan(int sizeHint = 0);

    /// <summary>
    /// Commits <paramref name="bytesWritten"/> bytes written into the span obtained from <see cref="GetSpan"/>.
    /// Throws <see cref="InvalidOperationException"/> when the value is negative or exceeds the remaining span.
    /// Always pass the number of bytes actually written, never an upfront reservation.
    /// </summary>
    void Advance(int bytesWritten);

    /// <summary>Pushes buffered bytes through to the underlying destination, when one exists.</summary>
    void Flush();
}

/// <summary>Extension methods for <see cref="IWriteBuffer"/> implementations.</summary>
public static class WriteBufferExtensions
{
    extension<TWriteBuffer>(ref TWriteBuffer buffer)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        /// <summary>
        /// Returns a reference to writable memory with at least <paramref name="sizeHint"/> bytes behind it.
        /// A reference-typed shortcut for <see cref="IWriteBuffer.GetSpan"/> with the same contract.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte GetReference(int sizeHint = 0)
        {
            return ref MemoryMarshal.GetReference(buffer.GetSpan(sizeHint));
        }
    }
}
