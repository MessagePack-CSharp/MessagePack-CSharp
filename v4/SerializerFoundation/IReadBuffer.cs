namespace SerializerFoundation;

public interface IReadBuffer : IDisposable
{
    /// <summary>
    /// Returns the unread portion of the current contiguous window WITHOUT any size
    /// requirement. MAY BE EMPTY when the buffer is exhausted — the read primitives
    /// report InsufficientBuffer with their exact requirement on an empty window, which
    /// lets the extension layer surface end-of-buffer as the same domain exception as
    /// any other truncation instead of a foundation throw. Never throws.
    /// Logically non-consuming, but NOT necessarily side-effect free: when the current
    /// window is exhausted, multi-segment implementations commit and reposition their
    /// INTERNAL windowing to the next segment before answering (an idempotent
    /// normalization — BytesConsumed/BytesRemaining are unchanged). This is why it is a
    /// method, not a property.
    /// (No ref-returning accessor on the read side: a ref cannot express emptiness. The
    /// write side's GetReference is an extension over GetSpan, where capacity is
    /// guaranteed by contract.)
    /// </summary>
    ReadOnlySpan<byte> GetCurrentSpan();

    /// <summary>
    /// Returns a contiguous window of at least <paramref name="sizeHint"/> bytes
    /// (multi-segment implementations stitch across seams as needed), or false when
    /// fewer bytes than that remain in the data — the CALLER decides how truncation
    /// surfaces (the read layer throws its domain exception), so the foundation never
    /// throws for it. A <paramref name="sizeHint"/> of 0 is vacuously satisfied: always
    /// true, with a possibly-EMPTY span (equivalent to <see cref="GetCurrentSpan"/>) —
    /// this is load-bearing for zero-length payloads, e.g. an empty string key at the
    /// very end of the data must yield an empty key, not a truncation error. A negative
    /// <paramref name="sizeHint"/> throws <see cref="ArgumentOutOfRangeException"/>: it
    /// is a caller bug (typically an overflowed size computation) and must not be
    /// misdiagnosed as truncated input.
    /// </summary>
    bool TryGetSpan(int sizeHint, out ReadOnlySpan<byte> span);

    /// <summary>
    /// Consumes <paramref name="bytesConsumed"/> bytes. One unsigned compare — the
    /// write side's Advance shape — rejects a negative value and advancing past the end
    /// of the DATA alike with <see cref="InvalidOperationException"/>, maintaining
    /// 0 &lt;= consumed &lt;= data length. Multi-window implementations still allow
    /// advancing past the current WINDOW (skip semantics: a skipped payload crosses
    /// segments, and the next GetSpan repositions). Truncation of malformed input is
    /// detected by the read layer BEFORE it advances (skips validate against
    /// <see cref="BytesRemaining"/>), so it surfaces as the domain exception — this
    /// guard only ever fires on a caller contract bug.
    /// </summary>
    void Advance(int bytesConsumed);

    // TODO: need more API like TryGetSequence(out ReadOnlySequence) for deserialize large str/bin

    long BytesConsumed { get; }
    long BytesRemaining { get; }
}
