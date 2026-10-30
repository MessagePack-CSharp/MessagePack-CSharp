using System.Diagnostics.CodeAnalysis;
using SerializerFoundation.CodeAnalysis;

namespace MessagePack;

/// <summary>
/// Writes a MessagePack message by hand, token by token, into pooled memory. <see cref="ToArray"/> and the <c>WriteTo</c>
/// overloads copy the bytes out, and only <see cref="Dispose"/> returns the memory.
/// </summary>
[NonCopyable]
public ref struct MessagePackWriter : IDisposable
{
    // the write buffer of the byte[] entry, so Serialize<T> runs the formatters that entry already instantiates
#if NET9_0_OR_GREATER
    ArrayPoolListWriteBuffer buffer;
#else
    CompatibleArrayPoolListWriteBuffer buffer;
#endif

    /// <summary>Gets the number of bytes written so far.</summary>
    public long BytesWritten => buffer.BytesWritten;

    #region Tokens

    /// <summary>Writes an int32 in the smallest msgpack format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt32(int value) => buffer.WriteInt32(value);

    /// <summary>Writes an int64 in the smallest msgpack format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt64(long value) => buffer.WriteInt64(value);

    /// <summary>Writes a uint32 in the smallest msgpack format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt32(uint value) => buffer.WriteUInt32(value);

    /// <summary>Writes a uint64 in the smallest msgpack format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt64(ulong value) => buffer.WriteUInt64(value);

    /// <summary>Writes a byte in the smallest msgpack format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value) => buffer.WriteByte(value);

    /// <summary>Writes an sbyte in the smallest msgpack format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value) => buffer.WriteSByte(value);

    /// <summary>Writes an int16 in the smallest msgpack format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt16(short value) => buffer.WriteInt16(value);

    /// <summary>Writes a uint16 in the smallest msgpack format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16(ushort value) => buffer.WriteUInt16(value);

    /// <summary>Writes a char as a uint16 in the smallest msgpack format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteChar(char value) => buffer.WriteChar(value);

    /// <summary>Writes value in the int8 format regardless of its size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteForcedInt8(sbyte value) => buffer.WriteForcedInt8(value);

    /// <summary>Writes value in the uint8 format regardless of its size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteForcedUInt8(byte value) => buffer.WriteForcedUInt8(value);

    /// <summary>Writes value in the int16 format regardless of its size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteForcedInt16(short value) => buffer.WriteForcedInt16(value);

    /// <summary>Writes value in the uint16 format regardless of its size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteForcedUInt16(ushort value) => buffer.WriteForcedUInt16(value);

    /// <summary>Writes value in the int32 format regardless of its size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteForcedInt32(int value) => buffer.WriteForcedInt32(value);

    /// <summary>Writes value in the uint32 format regardless of its size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteForcedUInt32(uint value) => buffer.WriteForcedUInt32(value);

    /// <summary>Writes value in the int64 format regardless of its size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteForcedInt64(long value) => buffer.WriteForcedInt64(value);

    /// <summary>Writes value in the uint64 format regardless of its size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteForcedUInt64(ulong value) => buffer.WriteForcedUInt64(value);

    /// <summary>Writes nil.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNil() => buffer.WriteNil();

    /// <summary>Writes a boolean.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBoolean(bool value) => buffer.WriteBoolean(value);

    /// <summary>Writes a float32.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSingle(float value) => buffer.WriteSingle(value);

    /// <summary>Writes a float64.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble(double value) => buffer.WriteDouble(value);

    /// <summary>Writes a string as a str in the smallest format, or nil when null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string? value) => buffer.WriteString(value);

    /// <summary>Writes a str from already-encoded UTF-8 bytes, copied as they are. The caller guarantees valid UTF-8.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(scoped ReadOnlySpan<byte> utf8Value) => buffer.WriteString(utf8Value);

    /// <summary>Writes a fixarray header. count must be 0..15.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFixArrayHeader([ConstantExpected(Min = 0, Max = 15)] int count) => buffer.WriteFixArrayHeader(count);

    /// <summary>Writes an array header in the smallest format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteArrayHeader(int count) => buffer.WriteArrayHeader(count);

    /// <summary>Writes a fixmap header. count must be 0..15.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFixMapHeader([ConstantExpected(Min = 0, Max = 15)] int count) => buffer.WriteFixMapHeader(count);

    /// <summary>Writes a map header in the smallest format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteMapHeader(int count) => buffer.WriteMapHeader(count);

    /// <summary>
    /// Writes a bin header, as bin8, bin16 or bin32 for byteCount.
    /// The caller then writes exactly byteCount payload bytes through <see cref="WriteRaw(ReadOnlySpan{byte})"/>.
    /// Use <see cref="WriteBinary(ReadOnlySpan{byte})"/> when the payload is already a single span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBinHeader(int byteCount) => buffer.WriteBinHeader(byteCount);

    /// <summary>Writes a bin, header and payload.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBinary(scoped ReadOnlySpan<byte> value) => buffer.WriteBinary(value);

    /// <summary>Writes a bin, header and payload, from a sequence whose segments are copied one by one without stitching.</summary>
    public void WriteBinary(in ReadOnlySequence<byte> value) => buffer.WriteBinary(value);

    /// <summary>
    /// Writes an ext header, as fixext when dataLength is exactly 1, 2, 4, 8 or 16 and as ext8, ext16 or ext32 otherwise.
    /// The caller then writes exactly dataLength payload bytes through <see cref="WriteRaw(ReadOnlySpan{byte})"/>. dataLength must be non-negative.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteExtHeader(sbyte typeCode, int dataLength) => buffer.WriteExtHeader(typeCode, dataLength);

    /// <summary>Writes a msgpack timestamp (ext type -1) in the smallest form.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteTimestamp(DateTime value) => buffer.WriteTimestamp(value);

    /// <summary>
    /// Copies value into the output without interpreting it.
    /// value must be a complete MessagePack value, such as the bytes returned by ReadRaw, or the payload promised by a preceding header.
    /// Nothing is validated. An empty span writes nothing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteRaw(scoped ReadOnlySpan<byte> value) => buffer.WriteRaw(value);

    /// <summary>
    /// The <see cref="ReadOnlySequence{T}"/> form of <see cref="WriteRaw(ReadOnlySpan{byte})"/>, copying each segment in turn.
    /// The same contract applies to the sequence as a whole.
    /// </summary>
    public void WriteRaw(in ReadOnlySequence<byte> value) => buffer.WriteRaw(value);

    #endregion

    #region Values

    /// <summary>Writes <paramref name="value"/> through its formatter. Options with a MessageProcessor are rejected.</summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public void Serialize<T>(T value)
    {
        Serialize(value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize{T}(T)"/>
    public void Serialize<T>(T value, MessagePackSerializerOptions options)
    {
#if NET9_0_OR_GREATER
        MessagePackSerializer.Serialize(ref buffer, value, options);
#else
        // netstandard has no buffer-level entry; this is what it would do over the Compatible buffer
        ArgumentNullException.ThrowIfNull(options);
        if (options.MessageProcessor != null)
        {
            MessagePackSerializer.ThrowMessageProcessorNotApplicable();
        }
        var state = new SerializeState(options.MaxDepth);
        options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
#endif
    }

    /// <summary>
    /// Writes <paramref name="value"/> as <paramref name="type"/>, the non-generic form of <see cref="Serialize{T}(T)"/>.
    /// The value must be an instance of that type.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public void Serialize(Type type, object? value)
    {
        Serialize(type, value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize(Type, object?)"/>
    public void Serialize(Type type, object? value, MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        MessagePackSerializer.SerializeToWriter(type, ref this, value, options);
    }

    #endregion

    #region Output

    /// <summary>Copies the bytes written so far into a new array.</summary>
    public byte[] ToArray() => buffer.ToArray();

    /// <summary>
    /// Copies the bytes written so far to the start of <paramref name="destination"/>, which must be at least
    /// <see cref="BytesWritten"/> bytes long.
    /// </summary>
    public void WriteTo(scoped Span<byte> destination)
    {
        if (destination.Length < BytesWritten)
        {
            ThrowDestinationTooShort(destination.Length, BytesWritten);
        }
        buffer.WriteTo(destination);
    }

    [DoesNotReturn]
    static void ThrowDestinationTooShort(int length, long bytesWritten)
    {
        throw new ArgumentException($"The destination has {length} bytes, fewer than the {bytesWritten} bytes written.", "destination");
    }

    /// <summary>Copies the bytes written so far to <paramref name="destination"/>.</summary>
    public void WriteTo(IBufferWriter<byte> destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var segments = buffer.GetWrittenSegments();
        while (segments.TryGetNext(out var segment))
        {
            destination.Write(segment);
        }
    }

    /// <summary>Writes the bytes written so far to <paramref name="destination"/>.</summary>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var segments = buffer.GetWrittenSegments();
        while (segments.TryGetNext(out var segment))
        {
#if NETSTANDARD2_0
            var rented = ArrayPool<byte>.Shared.Rent(segment.Length);
            try
            {
                segment.CopyTo(rented);
                destination.Write(rented, 0, segment.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
#else
            destination.Write(segment);
#endif
        }
    }

    /// <summary>Returns the pooled memory. A second call does nothing.</summary>
    public void Dispose()
    {
        buffer.Dispose();
        buffer = default;
    }

    #endregion
}
