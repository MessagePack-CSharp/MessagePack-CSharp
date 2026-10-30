using System.Diagnostics.CodeAnalysis;
using SerializerFoundation.CodeAnalysis;

namespace MessagePack;

/// <summary>
/// Reads a MessagePack message by hand, token by token, in place over its source. Each read consumes one token
/// and throws <see cref="MessagePackSerializationException"/> when the data does not match it.
/// </summary>
[NonCopyable]
public ref struct MessagePackReader : IDisposable
{
    // the read buffer of the ReadOnlySequence entry, so Deserialize<T> runs the formatters that entry already instantiates
#if NET9_0_OR_GREATER
    ReadOnlySequenceReadBuffer buffer;
#else
    CompatibleReadOnlySequenceReadBuffer buffer;
#endif

    /// <summary>Creates a reader over <paramref name="source"/>, which must stay unchanged while it is read.</summary>
    public MessagePackReader(ReadOnlyMemory<byte> source)
        : this(new ReadOnlySequence<byte>(source))
    {
    }

    /// <summary>Creates a reader over <paramref name="source"/>, which must stay unchanged while it is read.</summary>
    public MessagePackReader(scoped in ReadOnlySequence<byte> source)
    {
#if NET9_0_OR_GREATER
        buffer = new ReadOnlySequenceReadBuffer(in source);
#else
        buffer = new CompatibleReadOnlySequenceReadBuffer(in source);
#endif
    }

#if NET9_0_OR_GREATER
    // the netstandard read buffers cannot read a span in place, so span input is net9.0 and later

    /// <summary>Creates a reader over <paramref name="source"/>, which must stay unchanged while it is read.</summary>
    [OverloadResolutionPriority(1)] // an array converts to both ReadOnlySpan and ReadOnlyMemory; it is read as a span
    public MessagePackReader(ReadOnlySpan<byte> source)
    {
        buffer = new ReadOnlySequenceReadBuffer(source);
    }
#endif

    /// <summary>Gets the number of bytes read so far.</summary>
    public long BytesConsumed => buffer.BytesConsumed;

    /// <summary>Gets the number of bytes left to read.</summary>
    public long BytesRemaining => buffer.BytesRemaining;

    /// <summary>Gets whether every byte has been read.</summary>
    public bool End => buffer.BytesRemaining == 0;

    /// <summary>Gets the msgpack family of the next token without consuming it. Throws when no bytes remain.</summary>
    public MessagePackType NextMessagePackType
    {
        get
        {
            if (!buffer.TryPeek(out var code))
            {
                ThrowEndOfData();
            }
            return MessagePackCode.ToMessagePackType(code);
        }
    }

    #region Tokens

    /// <summary>Reads an int32 from any msgpack int format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32() => buffer.ReadInt32();

    /// <summary>Reads an int64 from any msgpack int format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64() => buffer.ReadInt64();

    /// <summary>Reads a uint32 from any msgpack int format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32() => buffer.ReadUInt32();

    /// <summary>Reads a uint64 from any msgpack int format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64() => buffer.ReadUInt64();

    /// <summary>Reads a byte from any msgpack int format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => buffer.ReadByte();

    /// <summary>Reads an sbyte from any msgpack int format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => buffer.ReadSByte();

    /// <summary>Reads an int16 from any msgpack int format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16() => buffer.ReadInt16();

    /// <summary>Reads a uint16 from any msgpack int format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16() => buffer.ReadUInt16();

    /// <summary>Reads a char encoded as a uint16.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char ReadChar() => buffer.ReadChar();

    /// <summary>Returns the next code byte without consuming it, or false when no bytes remain.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeek(out byte code) => buffer.TryPeek(out code);

    /// <summary>Consumes the nil and returns true when the next value is nil; otherwise consumes nothing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadNil() => buffer.TryReadNil();

    /// <summary>Reads a boolean.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBoolean() => buffer.ReadBoolean();

    /// <summary>Reads a float32. A float64 or an int format is converted.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadSingle() => buffer.ReadSingle();

    /// <summary>Reads a float64. A float32 or an int format is converted.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => buffer.ReadDouble();

    /// <summary>Reads an array header and returns the element count. A count larger than the remaining data throws.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadArrayHeader() => buffer.ReadArrayHeader();

    /// <summary>Reads a map header and returns the entry count. A count larger than the remaining data throws.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadMapHeader() => buffer.ReadMapHeader();

    /// <summary>Reads a str header and returns the payload byte count, which the caller then reads with <see cref="ReadRaw(int)"/>. A count larger than the remaining data throws.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadStringHeader() => buffer.ReadStringHeader();

    /// <summary>Reads a bin header and returns the payload byte count, which the caller then reads with <see cref="ReadRaw(int)"/>. A str header is also accepted, for data written in the old spec.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadBinHeader() => buffer.ReadBinHeader();

    /// <summary>Reads a bin, header and payload, into a new array. A str-coded payload is also accepted, for data written in the old spec.</summary>
    public byte[] ReadBinary() => buffer.ReadBinary();

    /// <summary>
    /// Consumes the ext header and returns true when the next token is an ext of <paramref name="typeCode"/>; otherwise consumes nothing.
    /// The data bytes follow for the caller to read with <see cref="ReadRaw(int)"/>.
    /// </summary>
    public bool TryReadExtHeader(sbyte typeCode, out int dataLength) => buffer.TryReadExtHeader(typeCode, out dataLength);

    /// <summary>Reads an ext header and returns its type code and data length. The data bytes follow for the caller to read with <see cref="ReadRaw(int)"/>.</summary>
    public (sbyte TypeCode, int DataLength) ReadExtHeader() => buffer.ReadExtHeader();

    /// <summary>Reads a msgpack timestamp (ext type -1) as a UTC DateTime.</summary>
    public DateTime ReadTimestamp() => buffer.ReadTimestamp();

    /// <summary>Reads a str as a string, and nil as null.</summary>
    public string? ReadString() => buffer.ReadString();

    /// <summary>Skips exactly one value including its entire subtree, without recursion, so nesting depth cannot exhaust the stack.</summary>
    public void Skip() => buffer.Skip();

    /// <summary>
    /// Reads exactly one value including its entire subtree and returns its bytes unchanged.
    /// </summary>
    public byte[] ReadRaw() => buffer.ReadRaw();

    /// <summary>Reads the next <paramref name="length"/> bytes into a new array, such as a payload after a header. A length larger than the remaining data throws.</summary>
    public byte[] ReadRaw(int length)
    {
        if ((uint)length > (ulong)buffer.BytesRemaining)
        {
            ThrowRawLengthOutOfRange(length, buffer.BytesRemaining);
        }
        var bytes = new byte[length];
        ReadRaw(bytes);
        return bytes;
    }

    /// <summary>Reads the next <c>destination.Length</c> bytes into <paramref name="destination"/>, such as a payload after a header. A destination longer than the remaining data throws.</summary>
    public void ReadRaw(scoped Span<byte> destination)
    {
        if ((ulong)destination.Length > (ulong)buffer.BytesRemaining)
        {
            ThrowRawLengthOutOfRange(destination.Length, buffer.BytesRemaining);
        }
        buffer.CopyTo(destination);
        buffer.Advance(destination.Length);
    }

    #endregion

    #region Values

    /// <summary>Reads one value through its formatter. Options with a MessageProcessor are rejected.</summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public T Deserialize<T>()
    {
        return Deserialize<T>(MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Deserialize{T}()"/>
    public T Deserialize<T>(MessagePackSerializerOptions options)
    {
#if NET9_0_OR_GREATER
        return MessagePackSerializer.Deserialize<ReadOnlySequenceReadBuffer, T>(ref buffer, options);
#else
        // netstandard has no buffer-level entry; this is what it would do over the Compatible buffer
        ArgumentNullException.ThrowIfNull(options);
        if (options.MessageProcessor != null)
        {
            MessagePackSerializer.ThrowMessageProcessorNotApplicable();
        }
        var state = new DeserializeState(options.MaxDepth, buffer.BytesRemaining);
        T value = default!;
        options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySequenceReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
        return value;
#endif
    }

    /// <summary>
    /// Reads one value of <paramref name="type"/>, the non-generic form of <see cref="Deserialize{T}()"/>.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public object? Deserialize(Type type)
    {
        return Deserialize(type, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Deserialize(Type)"/>
    public object? Deserialize(Type type, MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return MessagePackSerializer.DeserializeFromReader(type, ref this, options);
    }

    #endregion

    /// <summary>Returns the memory rented for tokens that straddled segments. A second call does nothing.</summary>
    public void Dispose()
    {
        buffer.Dispose();
        buffer = default;
    }

    [DoesNotReturn]
    static void ThrowEndOfData()
    {
        throw new MessagePackSerializationException("No bytes remain to read.");
    }

    [DoesNotReturn]
    static void ThrowRawLengthOutOfRange(int length, long bytesRemaining)
    {
        throw new MessagePackSerializationException($"A raw read of {length} bytes cannot be served by the {bytesRemaining} remaining bytes");
    }
}
