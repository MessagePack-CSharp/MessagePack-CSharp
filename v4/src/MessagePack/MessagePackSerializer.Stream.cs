using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace MessagePack;

// We provide both sync Stream and async Stream overloads.
// Sync support for Stream is important.
// Sync I/O and async I/O are different things, and sync-over-async on top of async I/O is not a substitute.

public static partial class MessagePackSerializer
{
    static readonly StreamPipeWriterOptions StreamWriterOptions = new(leaveOpen: true);
    static readonly StreamPipeReaderOptions StreamReaderOptions = new(leaveOpen: true);

    /// <summary>
    /// Serializes a value and writes the MessagePack binary to the stream synchronously.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static void Serialize<T>(Stream stream, T value)
    {
        Serialize(stream, value, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Serialize{T}(Stream, T)"/>
    [SkipLocalsInit]
    public static void Serialize<T>(Stream stream, T value, MessagePackSerializerOptions options)
    {
#if NET9_0_OR_GREATER
        if (!options.Resolver.TryGetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>(out var formatter))
        {
            SerializeStreamCompatible(stream, value, options);
            return;
        }

        Span<byte> scratch = stackalloc byte[SerializeScratchSize];
        var buffer = new ArrayPoolListWriteBuffer(scratch);
        try
        {
            var state = new SerializeState(options.MaxDepth);
            formatter.Serialize(ref buffer, ref state, value);
            var segments = buffer.GetWrittenSegments();
            WriteMessageToStream(stream, ref segments, options.MessageProcessor);
        }
        finally
        {
            buffer.Dispose();
        }

        static void SerializeStreamCompatible(Stream stream, T value, MessagePackSerializerOptions options)
#endif
        {
            var buffer = new CompatibleArrayPoolListWriteBuffer();
            try
            {
                var state = new SerializeState(options.MaxDepth);
                options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T>().Serialize(ref buffer, ref state, value);
                var segments = buffer.GetWrittenSegments();
                WriteMessageToStream(stream, ref segments, options.MessageProcessor);
            }
            finally
            {
                buffer.Dispose();
            }
        }

        static void WriteMessageToStream(Stream stream, ref BufferSegments segments, MessagePackMessageProcessor? processor)
        {
            if (processor != null && TryEncodeToArray(segments, processor, out var encoded))
            {
                stream.Write(encoded, 0, encoded.Length);
                return;
            }

            segments.Reset(); // a declined TryEncode consumed the iterator
            while (segments.TryGetNext(out var segment))
            {
#if NETSTANDARD2_0
            var rented = ArrayPool<byte>.Shared.Rent(segment.Length);
            segment.CopyTo(rented);
            stream.Write(rented, 0, segment.Length);
            ArrayPool<byte>.Shared.Return(rented);
#else
                stream.Write(segment);
#endif
            }
        }
    }

    /// <summary>
    /// Deserializes a value from the stream synchronously.
    /// The stream is read to its end, bounded by <see cref="MessagePackSerializerOptions.MaxBufferedMessageSize"/>.
    /// On a seekable stream without a MessageProcessor, the position is left just past the value
    /// so trailing data stays readable.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static T Deserialize<T>(Stream stream)
    {
        return Deserialize<T>(stream, MessagePackSerializerOptions.Default);
    }

    /// <inheritdoc cref="Deserialize{T}(Stream)"/>
    public static T Deserialize<T>(Stream stream, MessagePackSerializerOptions options)
    {
        // exposable MemoryStream: deserialize straight over its buffer — no copy, no
        // rent, no buffering cap (the data is already in memory, same as the span entry)
        if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var exposed))
        {
            return DeserializeFromMemoryStream<T>(memoryStream, exposed, options);
        }

        var rented = ReadStreamToPooled(stream, options.MaxBufferedMessageSize, out var length);
        try
        {
            var source = rented.AsSpan(0, length);
            T value = default!;

            var processor = options.MessageProcessor;
            if (processor != null && processor.TryDecode(source, out var decoded))
            {
                try
                {
                    DeserializeDecoded(ref value, in decoded, options);
                }
                finally
                {
                    decoded.Dispose();
                }
                return value; // an envelope owns the whole message: no positional rewind
            }

            var consumed = DeserializeSpanCore(ref value, source, options);
            if (stream.CanSeek && consumed < length)
            {
                stream.Seek(consumed - length, SeekOrigin.Current); // hand trailing bytes back
            }
            return value;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    static T DeserializeFromMemoryStream<T>(MemoryStream stream, ArraySegment<byte> exposed, MessagePackSerializerOptions options)
    {
        var position = checked((int)stream.Position);
        var source = exposed.AsSpan(position, (int)(stream.Length - position));
        T value = default!;

        var processor = options.MessageProcessor;
        if (processor != null && processor.TryDecode(source, out var decoded))
        {
            try
            {
                DeserializeDecoded(ref value, in decoded, options);
            }
            finally
            {
                decoded.Dispose();
            }
            stream.Position = stream.Length; // an envelope owns the whole message
            return value;
        }

        var consumed = DeserializeSpanCore(ref value, source, options);
        stream.Position = position + consumed;
        return value;
    }

    static byte[] ReadStreamToPooled(Stream stream, long maxMessageSize, out int length)
    {
        maxMessageSize = Math.Min(maxMessageSize, Array.MaxLength);

        var initialSize = 4096;
        if (stream.CanSeek)
        {
            var remaining = stream.Length - stream.Position;
            if (remaining > maxMessageSize)
            {
                MessagePackSerializationException.ThrowBufferedMessageSizeExceeded(remaining, maxMessageSize);
            }
            initialSize = (int)Math.Max(remaining, 1);
        }

        var rented = ArrayPool<byte>.Shared.Rent(initialSize);
        length = 0;
        while (true)
        {
            if (length == rented.Length)
            {
                if (length >= maxMessageSize)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                    MessagePackSerializationException.ThrowBufferedMessageSizeExceeded(length + 1, maxMessageSize);
                }
                var grown = ArrayPool<byte>.Shared.Rent((int)Math.Min((long)rented.Length * 2, Array.MaxLength));
                Array.Copy(rented, grown, length);
                ArrayPool<byte>.Shared.Return(rented);
                rented = grown;
            }

            var read = stream.Read(rented, length, rented.Length - length);
            if (read == 0)
            {
                if (length > maxMessageSize)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                    MessagePackSerializationException.ThrowBufferedMessageSizeExceeded(length, maxMessageSize);
                }
                return rented;
            }
            length += read;
        }
    }

    /// <summary>
    /// Serializes a value and writes the MessagePack binary to the stream asynchronously.
    /// The stream is flushed and left open.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
    {
        return SerializeAsync(stream, value, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeAsync{T}(Stream, T, CancellationToken)"/>
    public static async Task SerializeAsync<T>(Stream stream, T value, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var pipeWriter = PipeWriter.Create(stream, StreamWriterOptions);
        try
        {
            await SerializeAsync(pipeWriter, value, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await pipeWriter.CompleteAsync(exception).ConfigureAwait(false); // discard, don't flush garbage
            throw;
        }
        await pipeWriter.CompleteAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Deserializes a value from the stream asynchronously. The stream is left open.
    /// Bytes read ahead past the value are discarded, so use the synchronous overload
    /// on a seekable stream when trailing data matters.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static ValueTask<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        return DeserializeAsync<T>(stream, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="DeserializeAsync{T}(Stream, CancellationToken)"/>
    public static async ValueTask<T> DeserializeAsync<T>(Stream stream, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        // exposable MemoryStream: skip the pipe entirely — completes synchronously, and
        // unlike the pipe path the position lands exactly past the value, so trailing
        // data stays readable
        if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var exposed))
        {
            return DeserializeFromMemoryStream<T>(memoryStream, exposed, options);
        }

        var pipeReader = PipeReader.Create(stream, StreamReaderOptions);
        try
        {
            return await DeserializeAsync<T>(pipeReader, options, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await pipeReader.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc cref="SerializeMessagesAsync{T}(PipeWriter, IAsyncEnumerable{T}, CancellationToken)"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeMessagesAsync<T>(Stream stream, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        return SerializeMessagesAsync(stream, source, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeMessagesAsync{T}(PipeWriter, IAsyncEnumerable{T}, CancellationToken)"/>
    public static async Task SerializeMessagesAsync<T>(Stream stream, IAsyncEnumerable<T> source, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var pipeWriter = PipeWriter.Create(stream, StreamWriterOptions);
        try
        {
            await SerializeMessagesAsync(pipeWriter, source, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await pipeWriter.CompleteAsync(exception).ConfigureAwait(false);
            throw;
        }
        await pipeWriter.CompleteAsync().ConfigureAwait(false);
    }

    /// <inheritdoc cref="SerializeMessagesAsync{T}(PipeWriter, IAsyncEnumerable{T}, CancellationToken)"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeMessagesAsync<T>(Stream stream, IEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        return SerializeMessagesAsync(stream, source, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeMessagesAsync{T}(PipeWriter, IAsyncEnumerable{T}, CancellationToken)"/>
    public static async Task SerializeMessagesAsync<T>(Stream stream, IEnumerable<T> source, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var pipeWriter = PipeWriter.Create(stream, StreamWriterOptions);
        try
        {
            await SerializeMessagesAsync(pipeWriter, source, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await pipeWriter.CompleteAsync(exception).ConfigureAwait(false);
            throw;
        }
        await pipeWriter.CompleteAsync().ConfigureAwait(false);
    }

    /// <inheritdoc cref="SerializeElementsAsync{T}(PipeWriter, IAsyncEnumerable{T}, long, CancellationToken)"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeElementsAsync<T>(Stream stream, IAsyncEnumerable<T> source, long count, CancellationToken cancellationToken = default)
    {
        return SerializeElementsAsync(stream, source, count, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeElementsAsync{T}(PipeWriter, IAsyncEnumerable{T}, long, CancellationToken)"/>
    public static async Task SerializeElementsAsync<T>(Stream stream, IAsyncEnumerable<T> source, long count, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var pipeWriter = PipeWriter.Create(stream, StreamWriterOptions);
        try
        {
            await SerializeElementsAsync(pipeWriter, source, count, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await pipeWriter.CompleteAsync(exception).ConfigureAwait(false);
            throw;
        }
        await pipeWriter.CompleteAsync().ConfigureAwait(false);
    }

    /// <inheritdoc cref="SerializeElementsAsync{T}(PipeWriter, IAsyncEnumerable{T}, long, CancellationToken)"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeElementsAsync<T>(Stream stream, IEnumerable<T> source, long count, CancellationToken cancellationToken = default)
    {
        return SerializeElementsAsync(stream, source, count, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeElementsAsync{T}(PipeWriter, IAsyncEnumerable{T}, long, CancellationToken)"/>
    public static async Task SerializeElementsAsync<T>(Stream stream, IEnumerable<T> source, long count, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var pipeWriter = PipeWriter.Create(stream, StreamWriterOptions);
        try
        {
            await SerializeElementsAsync(pipeWriter, source, count, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await pipeWriter.CompleteAsync(exception).ConfigureAwait(false);
            throw;
        }
        await pipeWriter.CompleteAsync().ConfigureAwait(false);
    }

    /// <inheritdoc cref="DeserializeMessagesAsync{T}(PipeReader, CancellationToken)"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static IAsyncEnumerable<T> DeserializeMessagesAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        return DeserializeMessagesAsync<T>(stream, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="DeserializeMessagesAsync{T}(PipeReader, CancellationToken)"/>
    public static async IAsyncEnumerable<T> DeserializeMessagesAsync<T>(Stream stream, MessagePackSerializerOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pipeReader = PipeReader.Create(stream, StreamReaderOptions);
        try
        {
            await foreach (var value in DeserializeMessagesAsync<T>(pipeReader, options, cancellationToken).ConfigureAwait(false))
            {
                yield return value;
            }
        }
        finally
        {
            await pipeReader.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc cref="DeserializeElementsAsync{T}(PipeReader, CancellationToken)"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static IAsyncEnumerable<T> DeserializeElementsAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        return DeserializeElementsAsync<T>(stream, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="DeserializeElementsAsync{T}(PipeReader, CancellationToken)"/>
    public static async IAsyncEnumerable<T> DeserializeElementsAsync<T>(Stream stream, MessagePackSerializerOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pipeReader = PipeReader.Create(stream, StreamReaderOptions);
        try
        {
            await foreach (var value in DeserializeElementsAsync<T>(pipeReader, options, cancellationToken).ConfigureAwait(false))
            {
                yield return value;
            }
        }
        finally
        {
            await pipeReader.CompleteAsync().ConfigureAwait(false);
        }
    }
}
