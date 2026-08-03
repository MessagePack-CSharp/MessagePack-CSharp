namespace SerializerFoundation;

public interface IWriteBuffer : IDisposable
{
    /// <summary>
    /// Returns a Span to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
    /// If no <paramref name="sizeHint"/> is provided (or it's equal to 0), some non-empty buffer is returned.
    /// A negative <paramref name="sizeHint"/> throws <see cref="ArgumentOutOfRangeException"/>: it is
    /// always a caller bug (typically an overflowed size computation) and is never clamped — a
    /// clamped small window would convert the upstream overflow into an unchecked overrun.
    /// Implementations keep this check off the hot path: the unsigned capacity guard already
    /// routes every negative value to the slow/throw path.
    /// </summary>
    Span<byte> GetSpan(int sizeHint = 0);

    /// <summary>
    /// Commits <paramref name="bytesWritten"/> bytes written into the window obtained from
    /// <see cref="GetSpan"/>. Throws <see cref="InvalidOperationException"/> when
    /// <paramref name="bytesWritten"/> is negative or exceeds the remaining window, so the
    /// bookkeeping can never leave the buffer bounds. It CANNOT detect advancing by a
    /// reservation instead of the actual written count (the gap would ship uninitialized
    /// bytes) — always pass the actual number of bytes written (the Unsafe* writers return it).
    /// </summary>
    void Advance(int bytesWritten);

    long BytesWritten { get; }

    void Flush();
}

public static class WriteBufferExtensions
{
    extension<TWriteBuffer>(ref TWriteBuffer buffer)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        /// <summary>
        /// Returns a reference to write to with at least the requested length (specified by
        /// <paramref name="sizeHint"/>) of writable bytes behind it. Derived from
        /// <see cref="IWriteBuffer.GetSpan"/>: the JIT dead-codes the unused span length, so
        /// this is codegen-equivalent to a dedicated interface member
        /// (GetReferenceVsGetSpanBenchmark — identical hot-loop asm on net10 x64).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte GetReference(int sizeHint = 0)
        {
            return ref MemoryMarshal.GetReference(buffer.GetSpan(sizeHint));
        }
    }
}
