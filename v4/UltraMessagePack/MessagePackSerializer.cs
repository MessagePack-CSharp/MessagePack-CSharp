// TODO: high-level API is not fully implemented yet.

using System.Diagnostics.CodeAnalysis;

namespace UltraMessagePack;

public static partial class MessagePackSerializer
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
            var processor = options.MessageProcessor;
            return processor == null
                ? buffer.ToArray()
                : processor.Encode(buffer.GetWrittenSegments(), buffer.BytesWritten);
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
                var processor = options.MessageProcessor;
                if (processor == null)
                {
                    return buffer.ToArray();
                }
#if NET9_0_OR_GREATER
                return EncodeCompatible(ref buffer, processor); // TODO: avoid compatible
#else
                return processor.Encode(buffer.GetWrittenSegments(), buffer.BytesWritten);
#endif
            }
            finally
            {
                buffer.Dispose();
            }
        }
    }

#if NET9_0_OR_GREATER
    // The processor's Encode surface takes the FAST buffer's segment iterator (a per-TFM
    // alias, see MessagePackMessageProcessor), so on the modern TFM the compatibility
    // path bridges by copying the written message into a fast buffer once. Cold by construction
    // (routed graph + processor set): correctness over zero-copy.
    static byte[] EncodeCompatible(ref CompatibleArrayPoolListWriteBuffer source, MessagePackMessageProcessor processor)
    {
        var bridge = new ArrayPoolListWriteBuffer(default);
        try
        {
            CopyToBridge(ref source, ref bridge);
            return processor.Encode(bridge.GetWrittenSegments(), bridge.BytesWritten);
        }
        finally
        {
            bridge.Dispose();
        }
    }

    static void EncodeCompatible(ref CompatibleArrayPoolListWriteBuffer source, MessagePackMessageProcessor processor, IBufferWriter<byte> output)
    {
        var bridge = new ArrayPoolListWriteBuffer(default);
        try
        {
            CopyToBridge(ref source, ref bridge);
            processor.Encode(bridge.GetWrittenSegments(), bridge.BytesWritten, output);
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
        var processor = options.MessageProcessor;
        if (processor != null)
        {
            // an envelope needs the complete message before the first output byte, so
            // this path serializes into the pooled segment buffer first
            SerializeEncoded(output, value, options, processor);
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
            processor.Encode(buffer.GetWrittenSegments(), buffer.BytesWritten, output);
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
#if NET9_0_OR_GREATER
                EncodeCompatible(ref buffer, processor, output);
#else
                processor.Encode(buffer.GetWrittenSegments(), buffer.BytesWritten, output);
#endif
            }
            finally
            {
                buffer.Dispose();
            }
        }
    }

}
