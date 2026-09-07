namespace MessagePack;

/// <summary>
/// Transforms whole messages on their way in and out, for compression, encryption or framing.
/// Set it on <see cref="MessagePackSerializerOptions.MessageProcessor"/>; null leaves messages untouched.
/// </summary>
public abstract class MessagePackMessageProcessor
{
    // Contract details for implementers that do not belong in the summary. The segments are only valid during
    // the call, and the call consumes the iterator (take struct copies for extra passes); a caller reusing it
    // afterwards Resets it first. BufferSegments is TFM-invariant, so implementations multi-target without #if.

    /// <summary>
    /// Wraps a serialized message in this processor's envelope and writes it to <paramref name="output"/>.
    /// Return false to have the message written as is, for example when it is below a compression threshold.
    /// On false nothing may have been committed to <paramref name="output"/>, because for streaming entries it is the final destination.
    /// </summary>
    public abstract bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output);

#if NET9_0_OR_GREATER
    // Conceptually abstract. It is virtual only because an abstract member present on this TFM alone would make
    // processors compiled against the downlevel TFMs fail to load on the modern runtime; the bridge body
    // (interface overload into a staging buffer, one extra copy) keeps those working instead.

    /// <summary>
    /// Same as <see cref="TryEncode(ref BufferSegments, IBufferWriter{byte})"/>, writing straight into the target buffer.
    /// Override it; the default goes through a staging buffer and costs one extra copy.
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
    /// Unwraps a message that starts with this processor's envelope.
    /// Return false when <paramref name="source"/> carries no envelope, and it is read as is.
    /// The returned message must not alias <paramref name="source"/>; it owns its own buffers, released by <see cref="DecodedMessage.Dispose"/>.
    /// </summary>
    public abstract bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message);

    /// <inheritdoc cref="TryDecode(ReadOnlySpan{byte}, out DecodedMessage)"/>
    /// <remarks>Identify a message without envelope from a small prefix, so that passthrough input never pays for flattening the whole sequence.</remarks>
    public abstract bool TryDecode(in ReadOnlySequence<byte> source, out DecodedMessage message);
}

/// <summary>
/// A decoded message together with the owner that releases the memory behind it.
/// Disposed by the serializer after deserialization; a null owner means there is nothing to release.
/// </summary>
public readonly struct DecodedMessage : IDisposable
{
    readonly ReadOnlySequence<byte> sequence;
    readonly IDisposable? owner;

    /// <summary>The decoded message bytes.</summary>
    public ReadOnlySequence<byte> Sequence => sequence;

    /// <summary>Wraps <paramref name="sequence"/>, with <paramref name="owner"/> releasing its memory on dispose.</summary>
    public DecodedMessage(ReadOnlySequence<byte> sequence, IDisposable? owner = null)
    {
        this.sequence = sequence;
        this.owner = owner;
    }

    // Copies share the owner, so surviving double-dispose is the owner's job. Release exactly once
    // (e.g. null out the pooled references on the first call).
    /// <summary>Releases the memory behind <see cref="Sequence"/>.</summary>
    public void Dispose() => owner?.Dispose();
}
