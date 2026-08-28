// TODO: this API is not finished yet.

using SerializerFoundation;

namespace MessagePack;

/// <summary>
/// Envelope hook for whole-message transforms (compression, encryption, framing).
/// Set on <see cref="MessagePackSerializerOptions.MessageProcessor"/>; null means the
/// entries behave exactly as before (one perfectly-predicted null check per call).
/// The core deliberately knows only this CONCEPT — concrete processors (e.g. LZ4) live
/// in external packages, yet integrations keep composing purely through options, the
/// same way v2/v3's options.Compression flowed through ASP.NET Core formatters.
/// </summary>
public abstract class MessagePackMessageProcessor
{
    /// <summary>
    /// Serialize tail, the mirror of TryDecode's transparent passthrough: wrap the written
    /// msgpack message (zero-copy segments, <see cref="BufferSegments.Length"/> carries the
    /// total size) in this processor's envelope and return true, or return false and the
    /// entry writes the raw message itself (envelope not worth it: below a compression
    /// threshold, incompressible, ...). The segments are only valid during the call.
    /// The call consumes the iterator (take struct copies for extra passes); a caller
    /// reusing it afterwards Resets it first.
    /// CONTRACT: false means output was NOT advanced — bytes obtained from GetSpan without
    /// Advance are fine, committed bytes are not, because on the streaming entries output
    /// is the final destination. BufferSegments is TFM-invariant, so implementations
    /// multi-target without #if.
    /// </summary>
    public abstract bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output);

#if NET
    /// <summary>
    /// Modern path: encode into buffers borrowed directly from the target write buffer.
    /// Conceptually abstract — implementations are expected to override this. It is
    /// virtual only because an abstract member present on this TFM alone would make
    /// processors compiled against the downlevel TFMs fail to LOAD on the modern runtime;
    /// the bridge body (interface overload into a staging buffer, one extra copy) keeps
    /// those working instead. Same false-means-passthrough contract.
    /// </summary>
    [SerializerFoundation.CodeAnalysis.RequireOverride] // SF003: compiled-against-modern processors must override
    public virtual bool TryEncode<TWriteBuffer>(ref BufferSegments message, ref TWriteBuffer output)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    {
        var staging = ArrayPoolListWriteBufferCache.Rent();
        try
        {
            if (!TryEncode(ref message, staging))
            {
                return false;
            }
            var encoded = ArrayPoolListWriteBufferCache.AsBuffer(staging).GetWrittenSegments();
            while (encoded.TryGetNext(out var segment))
            {
                segment.CopyTo(output.GetSpan(segment.Length));
                output.Advance(segment.Length);
            }
            return true;
        }
        finally
        {
            ArrayPoolListWriteBufferCache.Return(staging);
        }
    }
#endif

    /// <summary>
    /// Deserialize head: if source starts with this processor's envelope, produce the
    /// decoded message (true); otherwise return false and the entry reads source as-is
    /// (v3-style transparent passthrough of uncompressed data).
    /// CONTRACT: the returned message must NOT alias source — it owns its own (typically
    /// rented) buffers, released by <see cref="DecodedMessage.Dispose"/>.
    /// </summary>
    public abstract bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message);

    /// <inheritdoc cref="TryDecode(ReadOnlySpan{byte}, out DecodedMessage)"/>
    /// <remarks>
    /// Multi-segment handling is the implementation's job; identify a non-envelope from a
    /// small stitched prefix so passthrough input never pays a whole-message flatten.
    /// </remarks>
    public abstract bool TryDecode(in ReadOnlySequence<byte> source, out DecodedMessage message);
}

/// <summary>
/// A decoded message plus the owner that releases the memory backing it. Disposed by the
/// entry after deserialization completes; the producer defines the release policy (pool
/// return, native free, ...) through <c>owner</c>, and null means nothing to release.
/// </summary>
public readonly struct DecodedMessage : IDisposable
{
    readonly ReadOnlySequence<byte> sequence;
    readonly IDisposable? owner;

    public ReadOnlySequence<byte> Sequence => sequence;

    public DecodedMessage(ReadOnlySequence<byte> sequence, IDisposable? owner = null)
    {
        this.sequence = sequence;
        this.owner = owner;
    }

    // copies share the owner, so surviving double-dispose is the owner's job: release
    // exactly once (e.g. null out the pooled references on the first call)
    public void Dispose() => owner?.Dispose();
}
