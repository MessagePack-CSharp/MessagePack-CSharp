// TODO: high-level API is not fully implemented yet.

using System.Diagnostics.CodeAnalysis;

namespace UltraMessagePack;

public static class MessagePackSerializer
{
    const int SerializeScratchSize = 1024;
    const int DeserializeScratchSize = 64;

    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    public static byte[] Serialize<T>(T value)
    {
        return Serialize(value, MessagePackSerializerOptions.Default);
    }

    [SkipLocalsInit]
    public static byte[] Serialize<T>(T value, MessagePackSerializerOptions options)
    {
#if NET9_0_OR_GREATER
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
            var processor = options.PayloadProcessor;
            return processor == null
                ? buffer.ToArray()
                : processor.Wrap(buffer.GetWrittenSegments(), buffer.BytesWritten);
        }
        finally
        {
            buffer.Dispose();
        }

        static byte[] SerializeCompatible(T value, MessagePackSerializerOptions options)
#endif
        {
            var buffer = new CompatibleArrayPoolListWriteBuffer();
            try
            {
                var state = new SerializeState(options.MaxDepth);
                options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
                var processor = options.PayloadProcessor;
                if (processor == null)
                {
                    return buffer.ToArray();
                }
#if NET9_0_OR_GREATER
                return WrapCompatible(ref buffer, processor); // TODO: avoid compatible
#else
                return processor.Wrap(buffer.GetWrittenSegments(), buffer.BytesWritten);
#endif
            }
            finally
            {
                buffer.Dispose();
            }
        }
    }

#if NET9_0_OR_GREATER
    // The processor's Wrap surface takes the FAST buffer's segment iterator (a per-TFM
    // alias, see MessagePackPayloadProcessor), so on the modern TFM the compatibility
    // path bridges by copying the payload into a fast buffer once. Cold by construction
    // (routed graph + processor set): correctness over zero-copy.
    static byte[] WrapCompatible(ref CompatibleArrayPoolListWriteBuffer source, MessagePackPayloadProcessor processor)
    {
        var bridge = new ArrayPoolListWriteBuffer(default);
        try
        {
            CopyToBridge(ref source, ref bridge);
            return processor.Wrap(bridge.GetWrittenSegments(), bridge.BytesWritten);
        }
        finally
        {
            bridge.Dispose();
        }
    }

    static void WrapCompatible(ref CompatibleArrayPoolListWriteBuffer source, MessagePackPayloadProcessor processor, IBufferWriter<byte> output)
    {
        var bridge = new ArrayPoolListWriteBuffer(default);
        try
        {
            CopyToBridge(ref source, ref bridge);
            processor.Wrap(bridge.GetWrittenSegments(), bridge.BytesWritten, output);
        }
        finally
        {
            bridge.Dispose();
        }
    }

    static void CopyToBridge(ref CompatibleArrayPoolListWriteBuffer source, ref ArrayPoolListWriteBuffer bridge)
    {
        var segments = source.GetWrittenSegments();
        while (segments.TryGetNext(out var segment))
        {
            segment.CopyTo(bridge.GetSpan(segment.Length));
            bridge.Advance(segment.Length);
        }
    }
#endif

    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    public static void Serialize<T>(IBufferWriter<byte> output, T value)
    {
        Serialize(output, value, MessagePackSerializerOptions.Default);
    }

    public static void Serialize<T>(IBufferWriter<byte> output, T value, MessagePackSerializerOptions options)
    {
        var processor = options.PayloadProcessor;
        if (processor != null)
        {
            // an envelope needs the complete payload before the first output byte, so
            // this path serializes into the pooled segment buffer first
            SerializeWrapped(output, value, options, processor);
            return;
        }

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

    [SkipLocalsInit]
    static void SerializeWrapped<T>(IBufferWriter<byte> output, T value, MessagePackSerializerOptions options, MessagePackPayloadProcessor processor)
    {
#if NET9_0_OR_GREATER
        if (!options.Resolver.TryGetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>(out var formatter))
        {
            SerializeWrappedCompatible(output, value, options, processor);
            return;
        }

        Span<byte> scratch = stackalloc byte[SerializeScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            var state = new SerializeState(options.MaxDepth);
            formatter.Serialize(ref buffer, ref state, value);
            processor.Wrap(buffer.GetWrittenSegments(), buffer.BytesWritten, output);
        }
        finally
        {
            buffer.Dispose();
        }

        static void SerializeWrappedCompatible(IBufferWriter<byte> output, T value, MessagePackSerializerOptions options, MessagePackPayloadProcessor processor)
#endif
        {
            var buffer = new CompatibleArrayPoolListWriteBuffer();
            try
            {
                var state = new SerializeState(options.MaxDepth);
                options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
#if NET9_0_OR_GREATER
                WrapCompatible(ref buffer, processor, output);
#else
                processor.Wrap(buffer.GetWrittenSegments(), buffer.BytesWritten, output);
#endif
            }
            finally
            {
                buffer.Dispose();
            }
        }
    }

    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(ReadOnlySpan<byte> source)
        => Deserialize<T>(source, MessagePackSerializerOptions.Default);

    public static T Deserialize<T>(ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
    {
        T result = default!;
        Deserialize(ref result, source, options);
        return result;
    }

    /// <summary>
    /// Populate overload: deserializes into an existing instance (formatters treat a
    /// non-null ref as reuse), eliminating the result allocation for pooled objects.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    public static void Deserialize<T>(ref T value, ReadOnlySpan<byte> source)
        => Deserialize(ref value, source, MessagePackSerializerOptions.Default);

    /// <inheritdoc cref="Deserialize{T}(ref T, ReadOnlySpan{byte})"/>
    public static void Deserialize<T>(ref T value, ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
    {
        var processor = options.PayloadProcessor;
        if (processor != null && processor.TryUnwrap(source, out var payload))
        {
            // TryUnwrap == false falls through: unwrapped input is read as-is (v3-style
            // transparent passthrough)
            try
            {
                DeserializeUnwrapped(ref value, in payload, options);
            }
            finally
            {
                payload.Dispose();
            }
            return;
        }

        DeserializeSpanCore(ref value, source, options);
    }

    static unsafe void DeserializeSpanCore<T>(ref T value, ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
    {
#if NET9_0_OR_GREATER
        if (!options.Resolver.TryGetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>(out var formatter))
        {
            DeserializeSpanCompatible(ref value, source, options);
            return;
        }

        var buffer = new ReadOnlySpanReadBuffer(source);
        try
        {
            var state = new DeserializeState(options.MaxDepth);
            formatter.Deserialize(ref buffer, ref state, ref value);
        }
        finally
        {
            buffer.Dispose();
        }

        static void DeserializeSpanCompatible(ref T value, ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
#endif
        {
            // the Compatible read buffer reads through a fixed view of the caller's span;
            // the whole deserialization runs inside the fixed scope (empty source pins to
            // null, which PointerSpan represents as an empty window)
            fixed (byte* pointer = source)
            {
                var buffer = new CompatibleReadOnlySpanReadBuffer(pointer, source.Length);
                try
                {
                    var state = new DeserializeState(options.MaxDepth);
                    options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
                }
                finally
                {
                    buffer.Dispose();
                }
            }
        }
    }

    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    public static T Deserialize<T>(in ReadOnlySequence<byte> source)
        => Deserialize<T>(source, MessagePackSerializerOptions.Default);

    public static T Deserialize<T>(in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
    {
        T result = default!;
        Deserialize(ref result, source, options);
        return result;
    }

    /// <summary>
    /// Populate overload: deserializes into an existing instance (formatters treat a
    /// non-null ref as reuse), eliminating the result allocation for pooled objects.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    public static void Deserialize<T>(ref T value, in ReadOnlySequence<byte> source)
        => Deserialize(ref value, source, MessagePackSerializerOptions.Default);

    /// <inheritdoc cref="Deserialize{T}(ref T, in ReadOnlySequence{byte})"/>
    public static void Deserialize<T>(ref T value, in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
    {
        var processor = options.PayloadProcessor;
        if (processor != null && TryUnwrapSequence(processor, source, out var payload))
        {
            try
            {
                DeserializeUnwrapped(ref value, in payload, options);
            }
            finally
            {
                payload.Dispose();
            }
            return;
        }

        DeserializeSequenceCore(ref value, source, options);
    }

    [SkipLocalsInit]
    static void DeserializeSequenceCore<T>(ref T value, in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
    {
#if NET9_0_OR_GREATER
        if (!options.Resolver.TryGetFormatter<ArrayPoolListWriteBuffer, ReadOnlySequenceReadBuffer, T>(out var formatter))
        {
            DeserializeSequenceCompatible(ref value, in source, options);
            return;
        }

        Span<byte> scratch = stackalloc byte[DeserializeScratchSize];
        var buffer = new ReadOnlySequenceReadBuffer(source, scratch);
        try
        {
            var state = new DeserializeState(options.MaxDepth);
            formatter.Deserialize(ref buffer, ref state, ref value);
        }
        finally
        {
            buffer.Dispose();
        }

        static void DeserializeSequenceCompatible(ref T value, in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
#endif
        {
            // the Compatible tier windows each segment as ReadOnlyMemory (pin-free) and
            // stitches through its rented temp; no caller scratch involved
            var buffer = new CompatibleReadOnlySequenceReadBuffer(in source);
            try
            {
                var state = new DeserializeState(options.MaxDepth);
                options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySequenceReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
            }
            finally
            {
                buffer.Dispose();
            }
        }
    }

    static void DeserializeUnwrapped<T>(ref T value, in UnwrappedPayload payload, MessagePackSerializerOptions options)
    {
        // an unwrapped payload never re-enters TryUnwrap (no nested envelopes); route to
        // the plain cores, taking the cheaper span path when it is contiguous
        var sequence = payload.Sequence;
        if (sequence.IsSingleSegment)
        {
            DeserializeSpanCore(ref value, sequence.FirstSpan, options);
        }
        else
        {
            DeserializeSequenceCore(ref value, sequence, options);
        }
    }

    static bool TryUnwrapSequence(MessagePackPayloadProcessor processor, in ReadOnlySequence<byte> source, out UnwrappedPayload payload)
    {
        if (source.IsSingleSegment)
        {
            return processor.TryUnwrap(source.FirstSpan, out payload);
        }

        // multi-segment input: the envelope check needs contiguous bytes, so flatten into
        // a rented buffer for the duration of TryUnwrap (the payload contract forbids
        // aliasing the source, so the flat copy can be returned immediately after)
        var length = checked((int)source.Length);
        var flat = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            source.CopyTo(flat);
            return processor.TryUnwrap(flat.AsSpan(0, length), out payload);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(flat);
        }
    }
}
