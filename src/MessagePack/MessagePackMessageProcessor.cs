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
    /// The returned message either owns its own buffers (<see cref="DecodedMessage.Buffer"/>, released by <see cref="DecodedMessage.Dispose"/>)
    /// or names a slice of <paramref name="source"/> (<see cref="DecodedMessage.SourceSlice"/>), which the serializer reads in place before it returns.
    /// </summary>
    public abstract bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message);

    /// <inheritdoc cref="TryDecode(ReadOnlySpan{byte}, out DecodedMessage)"/>
    /// <remarks>Identify a message without envelope from a small prefix, so that passthrough input never pays for flattening the whole sequence.</remarks>
    public abstract bool TryDecode(in ReadOnlySequence<byte> source, out DecodedMessage message);

    /// <summary>
    /// True when this processor's envelope is not a MessagePack value (a raw compression container, for example), so the
    /// async entries find message boundaries through <see cref="TryFindMessageEnd"/> instead of scanning MessagePack tokens.
    /// Such a processor never passes messages through: every message on the wire is its container.
    /// </summary>
    public virtual bool DefinesMessageBoundaries => false;

    /// <summary>
    /// For a processor with <see cref="DefinesMessageBoundaries"/>: the length of the message that starts <paramref name="buffer"/>.
    /// Returns false when more bytes are needed, with <paramref name="length"/> the smallest length the message can still have,
    /// which the async entries check against <see cref="MessagePackSerializerOptions.MaxBufferedMessageSize"/> before waiting.
    /// Malformed data throws <see cref="MessagePackSerializationException"/>.
    /// </summary>
    public virtual bool TryFindMessageEnd(in ReadOnlySequence<byte> buffer, out long length)
    {
        throw new NotSupportedException("This processor's messages are MessagePack values; the serializer finds their boundaries itself.");
    }
}

/// <summary>
/// A decoded message, one of two cases: <see cref="Buffer"/>, the message as its own bytes with the owner that releases
/// them, or <see cref="SourceSlice"/>, the message as a slice of the source the processor was given, read in place.
/// Disposed by the serializer after deserialization.
/// </summary>
[Union]
public readonly struct DecodedMessage : IUnion, IDisposable
{
    readonly Buffer buffer;
    readonly SourceSlice slice;

    readonly Case held;

    enum Case : byte
    {
        None,
        Buffer,
        SourceSlice,
    }

    /// <summary>The message as its own bytes; <paramref name="Owner"/> releases them on dispose, null when there is nothing to release.</summary>
    public readonly record struct Buffer(ReadOnlySequence<byte> Sequence, IDisposable? Owner);

    /// <summary>
    /// The message as a slice of the source the processor was given, read in place with nothing to release;
    /// for envelopes that carry the message verbatim.
    /// </summary>
    public readonly struct SourceSlice
    {
        /// <summary>The slice <paramref name="length"/> bytes from <paramref name="offset"/> into the source.</summary>
        public SourceSlice(int offset, int length)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            Offset = offset;
            Length = length;
        }

        /// <summary>Offset of the message inside the source.</summary>
        public int Offset { get; }

        /// <summary>Length of the message inside the source.</summary>
        public int Length { get; }
    }

    /// <summary>The <see cref="Buffer"/> case.</summary>
    public DecodedMessage(Buffer value)
    {
        buffer = value;
        held = Case.Buffer;
    }

    /// <summary>The <see cref="SourceSlice"/> case.</summary>
    public DecodedMessage(SourceSlice value)
    {
        slice = value;
        held = Case.SourceSlice;
    }

    /// <summary>The <see cref="Buffer"/> case: wraps <paramref name="sequence"/>, with <paramref name="owner"/> releasing its memory on dispose.</summary>
    public DecodedMessage(ReadOnlySequence<byte> sequence, IDisposable? owner = null)
        : this(new Buffer(sequence, owner))
    {
    }

    /// <summary>The <see cref="SourceSlice"/> case. The message as a slice of the source the processor was given.</summary>
    public DecodedMessage(int offset, int length)
        : this(new SourceSlice(offset, length))
    {

    }

    public static implicit operator DecodedMessage(Buffer value) => new(value);

    public static implicit operator DecodedMessage(SourceSlice value) => new(value);

    /// <summary>Reads the <see cref="Buffer"/> case without boxing.</summary>
    public bool TryGetValue(out Buffer value)
    {
        value = buffer;
        return held == Case.Buffer;
    }

    /// <summary>Reads the <see cref="SourceSlice"/> case without boxing.</summary>
    public bool TryGetValue(out SourceSlice value)
    {
        value = slice;
        return held == Case.SourceSlice;
    }

    /// <summary>The held case, boxed; null for a default instance.</summary>
    public object? Value => held switch
    {
        Case.Buffer => buffer,
        Case.SourceSlice => slice,
        _ => null,
    };

    // Copies share the owner, so surviving double-dispose is the owner's job. Release exactly once
    // (e.g. null out the pooled references on the first call).
    /// <summary>Releases the memory behind a <see cref="Buffer"/> message.</summary>
    public void Dispose()
    {
        if (held == Case.Buffer)
        {
            buffer.Owner?.Dispose();
        }
    }
}
