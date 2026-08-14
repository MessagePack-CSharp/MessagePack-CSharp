// TODO: this API is not finished yet.

using SerializerFoundation;

// per-TFM segment iterator: downlevel entries serialize into the plain-struct pooled
// buffer, so the abstract Encode surface follows (multi-targeting processor implementations
// must #if the parameter type the same way; the in-repo LZ4 package is net10.0-only)
#if NET9_0_OR_GREATER
using SegmentIterator = SerializerFoundation.ArrayPoolListWriteBuffer.WrittenSegmentIterator;
#else
using SegmentIterator = SerializerFoundation.CompatibleArrayPoolListWriteBuffer.WrittenSegmentIterator;
#endif

namespace UltraMessagePack;

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
    /// Serialize tail: receives the written msgpack message as zero-copy segments and
    /// produces the final encoded bytes. The segments are only valid during the call.
    /// </summary>
    public abstract byte[] Encode(SegmentIterator message, long messageLength);

    /// <inheritdoc cref="Encode(SegmentIterator, long)"/>
    public abstract void Encode(SegmentIterator message, long messageLength, IBufferWriter<byte> output);

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
