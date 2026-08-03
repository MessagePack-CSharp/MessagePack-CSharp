using SerializerFoundation;
using System.Diagnostics.CodeAnalysis;

#if NET9_0_OR_GREATER
using DefaultWriteBuffer = SerializerFoundation.ArrayPoolListWriteBuffer;
using SpanReadBuffer = SerializerFoundation.ReadOnlySpanReadBuffer;
using SequenceReadBuffer = SerializerFoundation.ReadOnlySequenceReadBuffer;
using WriterWriteBuffer = SerializerFoundation.BufferWriterWriteBuffer;
#else
using DefaultWriteBuffer = SerializerFoundation.CompatibleArrayPoolListWriteBuffer;
using SpanReadBuffer = SerializerFoundation.UnsafeReadOnlySpanReadBuffer;
using SequenceReadBuffer = SerializerFoundation.CompatibleReadOnlySequenceReadBuffer;
using WriterWriteBuffer = SerializerFoundation.CompatibleBufferWriterWriteBuffer;
#endif

namespace UltraMessagePack;

public static class MessagePackSerializer
{
    const int SerializeScratchSize = 1024;
    const int DeserializeScratchSize = 64;

    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
    public static byte[] Serialize<T>(T value)
    {
        return Serialize(value, MessagePackSerializerOptions.Default);
    }

    [SkipLocalsInit]
    public static byte[] Serialize<T>(T value, MessagePackSerializerOptions options)
    {
#if NET9_0_OR_GREATER
        Span<byte> scratch = stackalloc byte[SerializeScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
#else
        var buffer = new CompatibleArrayPoolListWriteBuffer();
#endif
        try
        {
            var state = new SerializeState();
            options.Resolver.GetFormatter<DefaultWriteBuffer, SpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
            var processor = options.PayloadProcessor;
            return processor == null
                ? buffer.ToArray()
                : processor.Wrap(buffer.GetWrittenSegments(), buffer.BytesWritten);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
    public static void Serialize<T>(IBufferWriter<byte> output, T value)
        => Serialize(output, value, MessagePackSerializerOptions.Default);

    // Takes the interface directly, not a `ref TBufferWriter` generic: real writers
    // (PipeWriter, ArrayBufferWriter) are classes, for which a generic TBufferWriter is
    // __Canon-shared anyway (zero specialization benefit) while forcing awkward ref
    // passing (no readonly fields/properties at call sites). The buffer touches the
    // writer only on segment refill/Flush, so interface dispatch is confined to that
    // cold path. Anyone wrapping custom state in a struct for performance should
    // implement IWriteBuffer instead — that puts them on the fully-specialized
    // formatter path, strictly better than a wrapped struct writer.
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

        var buffer = new WriterWriteBuffer(output);
        try
        {
            var state = new SerializeState();
            options.Resolver.GetFormatter<WriterWriteBuffer, SpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
            buffer.Flush();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [SkipLocalsInit]
    static void SerializeWrapped<T>(IBufferWriter<byte> output, T value, MessagePackSerializerOptions options, MessagePackPayloadProcessor processor)
    {
#if NET9_0_OR_GREATER
        Span<byte> scratch = stackalloc byte[SerializeScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
#else
        var buffer = new CompatibleArrayPoolListWriteBuffer();
#endif
        try
        {
            var state = new SerializeState();
            options.Resolver.GetFormatter<DefaultWriteBuffer, SpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
            processor.Wrap(buffer.GetWrittenSegments(), buffer.BytesWritten, output);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
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
    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
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
        var buffer = new ReadOnlySpanReadBuffer(source);
        try
        {
            var state = new DeserializeState();
            options.Resolver.GetFormatter<DefaultWriteBuffer, SpanReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
        }
        finally
        {
            buffer.Dispose();
        }
#else
        // UnsafeReadBuffer reads through a fixed view of the caller's span; the whole
        // deserialization runs inside the fixed scope (empty source pins to null, which
        // PointerSpan represents as an empty window)
        fixed (byte* pointer = source)
        {
            var buffer = new UnsafeReadOnlySpanReadBuffer(pointer, source.Length);
            try
            {
                var state = new DeserializeState();
                options.Resolver.GetFormatter<DefaultWriteBuffer, SpanReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
            }
            finally
            {
                buffer.Dispose();
            }
        }
#endif
    }

    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
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
    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
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
        Span<byte> scratch = stackalloc byte[DeserializeScratchSize];
        var buffer = new ReadOnlySequenceReadBuffer(source, scratch);
#else
        // the fallback tier windows each segment as ReadOnlyMemory (pin-free) and
        // stitches through its rented temp; no caller scratch involved
        var buffer = new CompatibleReadOnlySequenceReadBuffer(in source);
#endif
        try
        {
            var state = new DeserializeState();
            options.Resolver.GetFormatter<DefaultWriteBuffer, SequenceReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
        }
        finally
        {
            buffer.Dispose();
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
