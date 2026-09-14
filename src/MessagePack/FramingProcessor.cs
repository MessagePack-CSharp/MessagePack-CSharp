using System.Buffers.Binary;

namespace MessagePack;

/// <summary>
/// Frames every message with its byte length: the message travels as one ext32 whose payload is the message verbatim,
/// so a reader finds the end of a message from the header instead of walking its tokens.
/// Set through <see cref="FramingMessagePackOptionsExtensions.WithFraming"/>.
/// </summary>
public sealed class FramingProcessor : MessagePackMessageProcessor
{
    const int HeaderLength = 6;
    const byte ExtTypeByte = unchecked((byte)ThisLibraryExtensionTypeCodes.Framing);

    /// <summary>The shared instance; the processor holds no state.</summary>
    public static FramingProcessor Instance { get; } = new FramingProcessor();

    /// <inheritdoc/>
    public override bool TryEncode(ref BufferSegments message, IBufferWriter<byte> output)
    {
        WriteHeader(output.GetSpan(HeaderLength), message.Length);
        output.Advance(HeaderLength);
        while (message.TryGetNext(out var segment))
        {
            output.Write(segment);
        }
        return true;
    }

#if NET9_0_OR_GREATER
    /// <inheritdoc/>
    public override bool TryEncode<TWriteBuffer>(ref BufferSegments message, ref TWriteBuffer output)
    {
        WriteHeader(output.GetSpan(HeaderLength), message.Length);
        output.Advance(HeaderLength);
        while (message.TryGetNext(out var segment))
        {
            output.WriteRaw(segment);
        }
        return true;
    }
#endif

    /// <inheritdoc/>
    public override bool TryDecode(ReadOnlySpan<byte> source, out DecodedMessage message)
    {
        if (!TryReadHeader(source, out var length))
        {
            message = default;
            return false;
        }
        if (length > source.Length - HeaderLength)
        {
            ThrowTruncated(length, source.Length - HeaderLength);
        }

        message = new DecodedMessage.SourceSlice(HeaderLength, length);
        return true;
    }

    /// <inheritdoc/>
    public override bool TryDecode(in ReadOnlySequence<byte> source, out DecodedMessage message)
    {
        if (source.IsSingleSegment)
        {
            return TryDecode(source.FirstSpan, out message);
        }

        // the header is recognized from a stitched prefix, so a message without envelope never pays for flattening
        Span<byte> prefix = stackalloc byte[HeaderLength];
        var prefixLength = (int)Math.Min(source.Length, HeaderLength);
        source.Slice(0, prefixLength).CopyTo(prefix);
        if (!TryReadHeader(prefix.Slice(0, prefixLength), out var length))
        {
            message = default;
            return false;
        }
        if (length > source.Length - HeaderLength)
        {
            ThrowTruncated(length, source.Length - HeaderLength);
        }
        message = new DecodedMessage.SourceSlice(HeaderLength, length);
        return true;
    }

    static void WriteHeader(Span<byte> header, long length)
    {
        if (length > uint.MaxValue)
        {
            throw new MessagePackSerializationException($"The message is {length} bytes long, more than the 4 GiB an ext32 frame can declare.");
        }

        // header is acquired from output.GetSpan(HeaderLength) so index bounds are guaranteed
        header[0] = MessagePackCode.Ext32;
        BinaryPrimitives.WriteUInt32BigEndian(header.Slice(1), (uint)length);
        header[5] = ExtTypeByte;
    }

    static bool TryReadHeader(ReadOnlySpan<byte> source, out int length)
    {
        if (source.Length < HeaderLength || source[0] != MessagePackCode.Ext32 || source[5] != ExtTypeByte)
        {
            length = 0;
            return false;
        }
        var declared = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(1));
        if (declared > int.MaxValue)
        {
            throw new MessagePackSerializationException($"The frame declares {declared} bytes, more than a single message can hold.");
        }
        length = (int)declared;
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowTruncated(int declared, long available)
    {
        throw new MessagePackSerializationException($"The frame declares {declared} bytes but only {available} follow the header.");
    }
}

/// <summary>Options extensions for <see cref="FramingProcessor"/>.</summary>
public static class FramingMessagePackOptionsExtensions
{
    /// <summary>Returns options that frame every message with its length, see <see cref="FramingProcessor"/>.</summary>
    public static MessagePackSerializerOptions WithFraming(this MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options with { MessageProcessor = FramingProcessor.Instance };
    }
}
