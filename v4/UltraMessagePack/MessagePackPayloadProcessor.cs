using SerializerFoundation;

// per-TFM segment iterator: downlevel entries serialize into the plain-struct pooled
// buffer, so the abstract Wrap surface follows (multi-targeting processor implementations
// must #if the parameter type the same way; the in-repo LZ4 package is net10.0-only)
#if NET9_0_OR_GREATER
using SegmentIterator = SerializerFoundation.ArrayPoolListWriteBuffer.WrittenSegmentIterator;
#else
using SegmentIterator = SerializerFoundation.CompatibleArrayPoolListWriteBuffer.WrittenSegmentIterator;
#endif

namespace UltraMessagePack;

/// <summary>
/// Envelope hook for whole-payload transforms (compression, encryption, framing).
/// Set on <see cref="MessagePackSerializerOptions.PayloadProcessor"/>; null means the
/// entries behave exactly as before (one perfectly-predicted null check per call).
/// The core deliberately knows only this CONCEPT — concrete processors (e.g. LZ4) live
/// in external packages, yet integrations keep composing purely through options, the
/// same way v2/v3's options.Compression flowed through ASP.NET Core formatters.
/// </summary>
public abstract class MessagePackPayloadProcessor
{
    /// <summary>
    /// Serialize tail: receives the written msgpack payload as zero-copy segments and
    /// produces the final wrapped bytes. The segments are only valid during the call.
    /// </summary>
    public abstract byte[] Wrap(SegmentIterator payload, long payloadLength);

    /// <inheritdoc cref="Wrap(SegmentIterator, long)"/>
    public abstract void Wrap(SegmentIterator payload, long payloadLength, IBufferWriter<byte> output);

    /// <summary>
    /// Deserialize head: if source starts with this processor's envelope, produce the
    /// unwrapped payload (true); otherwise return false and the entry reads source as-is
    /// (v3-style transparent passthrough of uncompressed data).
    /// CONTRACT: the returned payload must NOT alias source — it owns its own (typically
    /// rented) buffers, released by <see cref="UnwrappedPayload.Dispose"/>.
    /// </summary>
    public abstract bool TryUnwrap(ReadOnlySpan<byte> source, out UnwrappedPayload payload);
}

/// <summary>
/// An unwrapped payload plus ownership of the rented buffers backing it. Disposed by the
/// entry after deserialization completes.
/// </summary>
public struct UnwrappedPayload : IDisposable
{
    ReadOnlySequence<byte> sequence;
    byte[]? rentedSingle;
    byte[]?[]? rentedMany;

    public readonly ReadOnlySequence<byte> Sequence => sequence;

    public UnwrappedPayload(ReadOnlySequence<byte> sequence, byte[]? rentedSingle = null, byte[]?[]? rentedMany = null)
    {
        this.sequence = sequence;
        this.rentedSingle = rentedSingle;
        this.rentedMany = rentedMany;
    }

    public void Dispose()
    {
        if (rentedSingle != null)
        {
            ArrayPool<byte>.Shared.Return(rentedSingle);
            rentedSingle = null;
        }
        if (rentedMany != null)
        {
            foreach (var array in rentedMany)
            {
                if (array != null)
                {
                    ArrayPool<byte>.Shared.Return(array);
                }
            }
            rentedMany = null;
        }
        sequence = default;
    }
}
