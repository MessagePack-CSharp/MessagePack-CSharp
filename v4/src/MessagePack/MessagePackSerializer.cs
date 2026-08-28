using System.Diagnostics.CodeAnalysis;

namespace MessagePack;

public static partial class MessagePackSerializer
{
    const int SerializeScratchSize = 1024;

    /// <summary>
    /// Serializes a value and returns the MessagePack binary as a new byte array.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static byte[] Serialize<T>(T value)
    {
        return Serialize(value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize{T}(T)"/>
    [SkipLocalsInit]
    public static byte[] Serialize<T>(T value, MessagePackSerializerOptions options)
    {
        // `Serialize(typeof(X), null)` binds here (T = Type, null -> options)
        ArgumentNullException.ThrowIfNull(options);

#if NET9_0_OR_GREATER
        // When external formatters built only for netstandard2.0/2.1 are included, the compatible path is used.
        // Normally, this path is almost never taken.
        if (!options.Resolver.TryGetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>(out var formatter))
        {
            return SerializeCompatible(value, options);
        }

        Span<byte> scratch = stackalloc byte[SerializeScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            var state = new SerializeState(options.MaxDepth);
            formatter.Serialize(ref buffer, ref state, value);
            var processor = options.MessageProcessor;
            if (processor != null && TryEncodeToArray(buffer.GetWrittenSegments(), processor, out var encoded))
            {
                return encoded;
            }
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }

        // netstandard only exists to support the downlevel compatible formatter path
        static byte[] SerializeCompatible(T value, MessagePackSerializerOptions options)
#endif
        {
            var buffer = new CompatibleArrayPoolListWriteBuffer();
            try
            {
                var state = new SerializeState(options.MaxDepth);
                options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
                var processor = options.MessageProcessor;
                if (processor != null && TryEncodeToArray(buffer.GetWrittenSegments(), processor, out var encoded))
                {
                    return encoded;
                }
                return buffer.ToArray();
            }
            finally
            {
                buffer.Dispose();
            }
        }
    }

    /// <summary>
    /// Serializes a value and writes the MessagePack binary to the buffer writer.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static void Serialize<T>(IBufferWriter<byte> output, T value)
    {
        Serialize(output, value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize{T}(IBufferWriter{byte}, T)"/>
    public static void Serialize<T>(IBufferWriter<byte> output, T value, MessagePackSerializerOptions options)
    {
        var processor = options.MessageProcessor;
        if (processor != null)
        {
            // an envelope needs the complete message before the first output byte,
            // so this path serializes into the pooled segment buffer first
            SerializeEncoded(output, value, options, processor);
            return;
        }

        // otherwise serialize to output directly

#if NET9_0_OR_GREATER
        if (!options.Resolver.TryGetFormatter<BufferWriterWriteBuffer, ReadOnlySpanReadBuffer, T>(out var formatter))
        {
            SerializeCompatible(output, value, options);
            return;
        }

        var buffer = new BufferWriterWriteBuffer(output);
        try
        {
            var state = new SerializeState(options.MaxDepth);
            formatter.Serialize(ref buffer, ref state, value);
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }

        static void SerializeCompatible(IBufferWriter<byte> output, T value, MessagePackSerializerOptions options)
#endif
        {
            var buffer = new CompatibleBufferWriterWriteBuffer(output);
            try
            {
                var state = new SerializeState(options.MaxDepth);
                options.Resolver.GetFormatter<CompatibleBufferWriterWriteBuffer, CompatibleReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
                buffer.Flush();
            }
            finally
            {
                buffer.Dispose();
            }
        }
    }

#if NET9_0_OR_GREATER

    /// <summary>
    /// Serializes a value directly into a write buffer without flushing.
    /// This is the low-level entry for embedding MessagePack inside another protocol.
    /// Options carrying a MessageProcessor are rejected because the processor
    /// needs the complete message, which this entry never sees.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static void Serialize<TWriteBuffer, T>(ref TWriteBuffer buffer, T value)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    {
        Serialize(ref buffer, value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize{TWriteBuffer, T}(ref TWriteBuffer, T)"/>
    public static void Serialize<TWriteBuffer, T>(ref TWriteBuffer buffer, T value, MessagePackSerializerOptions options)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    {
        if (options.MessageProcessor != null)
        {
            ThrowMessageProcessorNotApplicable();
        }
        var state = new SerializeState(options.MaxDepth);
        options.Resolver.GetFormatter<TWriteBuffer, ReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowMessageProcessorNotApplicable()
    {
        throw new ArgumentException("The buffer-level entry cannot apply the options' MessageProcessor; the processor needs the complete message, which this entry never sees. Use a whole-message entry such as byte[] or IBufferWriter<byte>, or pass options without a MessageProcessor.", "options");
    }

#endif

    [SkipLocalsInit]
    static void SerializeEncoded<T>(IBufferWriter<byte> output, T value, MessagePackSerializerOptions options, MessagePackMessageProcessor processor)
    {
#if NET9_0_OR_GREATER
        if (!options.Resolver.TryGetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>(out var formatter))
        {
            SerializeEncodedCompatible(output, value, options, processor);
            return;
        }

        Span<byte> scratch = stackalloc byte[SerializeScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            var state = new SerializeState(options.MaxDepth);
            formatter.Serialize(ref buffer, ref state, value);
            var segments = buffer.GetWrittenSegments();
            if (!processor.TryEncode(ref segments, output))
            {
                WritePassthrough(ref segments, output);
            }
        }
        finally
        {
            buffer.Dispose();
        }

        static void SerializeEncodedCompatible(IBufferWriter<byte> output, T value, MessagePackSerializerOptions options, MessagePackMessageProcessor processor)
#endif
        {
            var buffer = new CompatibleArrayPoolListWriteBuffer();
            try
            {
                var state = new SerializeState(options.MaxDepth);
                options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
                var segments = buffer.GetWrittenSegments();
                if (!processor.TryEncode(ref segments, output))
                {
                    WritePassthrough(ref segments, output);
                }
            }
            finally
            {
                buffer.Dispose();
            }
        }

        static void WritePassthrough(ref BufferSegments message, IBufferWriter<byte> output)
        {
            message.Reset(); // the declined TryEncode consumed the iterator
            while (message.TryGetNext(out var segment))
            {
                output.Write(segment);
            }
        }
    }

    [SkipLocalsInit]
    static bool TryEncodeToArray(scoped BufferSegments message, MessagePackMessageProcessor processor, [NotNullWhen(true)] out byte[]? encoded)
    {
        // envelope staging for the byte[] entry: the encoded size is unknown upfront, so the processor writes into a staging buffer.
        // and the exact-size copy happens once at the end; false (passthrough) falls back to the caller's optimal raw path

#if NET9_0_OR_GREATER
        Span<byte> scratch = stackalloc byte[SerializeScratchSize];
        var writeBuffer = new SerializerFoundation.ArrayPoolListWriteBuffer(scratch);
        try
        {
            if (!processor.TryEncode(ref message, ref writeBuffer))
            {
                encoded = null;
                return false;
            }

            encoded = writeBuffer.ToArray();
            return true;
        }
        finally
        {
            writeBuffer.Dispose();
        }
#else
        var staging = ArrayPoolListWriteBufferCache.Rent();
        try
        {
            if (!processor.TryEncode(ref message, staging))
            {
                encoded = null;
                return false;
            }
            encoded = ArrayPoolListWriteBufferCache.AsBuffer(staging).ToArray();
            return true;
        }
        finally
        {
            ArrayPoolListWriteBufferCache.Return(staging);
        }
#endif
    }
}
