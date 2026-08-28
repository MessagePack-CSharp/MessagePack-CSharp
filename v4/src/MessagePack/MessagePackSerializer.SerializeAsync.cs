using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace MessagePack;

// Async serialization is sync writing plus awaited flushes. PipeWriter.GetSpan/Advance
// never block, so the ordinary synchronous entry writes straight into pipe memory and
// the async layer only awaits FlushAsync at message boundaries, which is where
// backpressure and cancellation live — no scanner, no async formatters, the serialize
// side of the two-pass deserializer's conclusion. Flushes are batched: bytes ride along
// until FlushThresholdBytes is crossed (the System.Text.Json FlushThreshold analog), and
// the streaming APIs additionally flush whenever the source is about to suspend, so
// everything serialized so far reaches the reader before waiting for more input.
// Non-seekable raw Streams are handled by wrapping them with PipeWriter.Create. The
// writer is never completed here — the caller owns the PipeWriter lifetime.
public static partial class MessagePackSerializer
{
    // half of the default Pipe pause threshold (64KB), so a batch flush lands before
    // backpressure would engage on a default pipe
    const long FlushThresholdBytes = 32 * 1024;

    /// <summary>
    /// Serializes one value into the writer and flushes. The write itself is synchronous
    /// (pipe memory never blocks); the flush is where backpressure and cancellation
    /// apply. The writer is left uncompleted.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeAsync<T>(PipeWriter pipeWriter, T value, CancellationToken cancellationToken = default)
    {
        return SerializeAsync(pipeWriter, value, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeAsync{T}(PipeWriter, T, CancellationToken)"/>
    public static async Task SerializeAsync<T>(PipeWriter pipeWriter, T value, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        Serialize(pipeWriter, value, options);
        await FlushAsync(pipeWriter, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Serializes a stream of values as concatenated top-level messages (the msgpack
    /// analog of json lines; the reading counterpart is
    /// <see cref="DeserializeMessagesAsync{T}(PipeReader, CancellationToken)"/>).
    /// Flushes are batched, plus one whenever the source is about to suspend so the
    /// reader sees everything serialized so far. Returns silently when the reader
    /// completes; the writer is left uncompleted.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeMessagesAsync<T>(PipeWriter pipeWriter, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        return SerializeMessagesAsync(pipeWriter, source, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeMessagesAsync{T}(PipeWriter, IAsyncEnumerable{T}, CancellationToken)"/>
    public static async Task SerializeMessagesAsync<T>(PipeWriter pipeWriter, IAsyncEnumerable<T> source, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var enumerator = source.GetAsyncEnumerator(cancellationToken);
        await using (enumerator.ConfigureAwait(false))
        {
            while (true)
            {
                var moveNext = enumerator.MoveNextAsync();
                if (moveNext.IsCompletedSuccessfully)
                {
                    if (!moveNext.Result)
                    {
                        break;
                    }
                }
                else
                {
                    // the source is about to suspend: make everything written so far
                    // visible to the reader before waiting for more input
                    if (await FlushAsync(pipeWriter, cancellationToken).ConfigureAwait(false))
                    {
                        return;
                    }
                    if (!await moveNext.ConfigureAwait(false))
                    {
                        break;
                    }
                }

                Serialize(pipeWriter, enumerator.Current, options);
                if (ShouldFlush(pipeWriter) && await FlushAsync(pipeWriter, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
            }

            await FlushAsync(pipeWriter, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc cref="SerializeMessagesAsync{T}(PipeWriter, IAsyncEnumerable{T}, CancellationToken)"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeMessagesAsync<T>(PipeWriter pipeWriter, IEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        return SerializeMessagesAsync(pipeWriter, source, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeMessagesAsync{T}(PipeWriter, IAsyncEnumerable{T}, CancellationToken)"/>
    public static async Task SerializeMessagesAsync<T>(PipeWriter pipeWriter, IEnumerable<T> source, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        foreach (var value in source)
        {
            Serialize(pipeWriter, value, options);
            if (ShouldFlush(pipeWriter) && await FlushAsync(pipeWriter, cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        await FlushAsync(pipeWriter, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Serializes the values as one top-level MessagePack array of <paramref name="count"/>
    /// elements (the reading counterpart is
    /// <see cref="DeserializeElementsAsync{T}(PipeReader, CancellationToken)"/>).
    /// msgpack arrays are length-prefixed, so the count must be known up front and the
    /// source must yield exactly that many elements (checked); for unknown counts use
    /// <c>SerializeMessagesAsync</c>, concatenated messages need no header. Flushing and
    /// reader-completion behave as in <c>SerializeMessagesAsync</c>.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeElementsAsync<T>(PipeWriter pipeWriter, IAsyncEnumerable<T> source, long count, CancellationToken cancellationToken = default)
    {
        return SerializeElementsAsync(pipeWriter, source, count, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeElementsAsync{T}(PipeWriter, IAsyncEnumerable{T}, long, CancellationToken)"/>
    public static async Task SerializeElementsAsync<T>(PipeWriter pipeWriter, IAsyncEnumerable<T> source, long count, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        WriteArrayHeader(pipeWriter, count);
        var enumerator = source.GetAsyncEnumerator(cancellationToken);
        await using (enumerator.ConfigureAwait(false))
        {
            var produced = 0L;
            while (produced < count)
            {
                var moveNext = enumerator.MoveNextAsync();
                bool hasNext;
                if (moveNext.IsCompletedSuccessfully)
                {
                    hasNext = moveNext.Result;
                }
                else
                {
                    if (await FlushAsync(pipeWriter, cancellationToken).ConfigureAwait(false))
                    {
                        return;
                    }
                    hasNext = await moveNext.ConfigureAwait(false);
                }
                if (!hasNext)
                {
                    MessagePackSerializationException.ThrowAsyncElementsMissing(count, produced);
                }

                Serialize(pipeWriter, enumerator.Current, options);
                produced++;
                if (ShouldFlush(pipeWriter) && await FlushAsync(pipeWriter, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
            }

            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                MessagePackSerializationException.ThrowAsyncElementsExceeded(count);
            }
            await FlushAsync(pipeWriter, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc cref="SerializeElementsAsync{T}(PipeWriter, IAsyncEnumerable{T}, long, CancellationToken)"/>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static Task SerializeElementsAsync<T>(PipeWriter pipeWriter, IEnumerable<T> source, long count, CancellationToken cancellationToken = default)
    {
        return SerializeElementsAsync(pipeWriter, source, count, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="SerializeElementsAsync{T}(PipeWriter, IAsyncEnumerable{T}, long, CancellationToken)"/>
    public static async Task SerializeElementsAsync<T>(PipeWriter pipeWriter, IEnumerable<T> source, long count, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        WriteArrayHeader(pipeWriter, count);
        var produced = 0L;
        foreach (var value in source)
        {
            if (produced == count)
            {
                MessagePackSerializationException.ThrowAsyncElementsExceeded(count);
            }
            Serialize(pipeWriter, value, options);
            produced++;
            if (ShouldFlush(pipeWriter) && await FlushAsync(pipeWriter, cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }
        if (produced < count)
        {
            MessagePackSerializationException.ThrowAsyncElementsMissing(count, produced);
        }

        await FlushAsync(pipeWriter, cancellationToken).ConfigureAwait(false);
    }

    static void WriteArrayHeader(PipeWriter pipeWriter, long count)
    {
        // the array formatter family is int-bounded (a materializable payload never
        // exceeds it), so the streaming header keeps the same bound
        if (count is < 0 or > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        var span = pipeWriter.GetSpan(MessagePackPrimitives.MaxArrayHeaderLength);
        pipeWriter.Advance(MessagePackPrimitives.UnsafeWriteArrayHeader(ref span[0], (int)count));
    }

    // called right after a message was serialized. Writers that cannot report unflushed
    // bytes flush per message, the safe default. Zero unflushed bytes at this point is
    // only possible when the pipe discarded the write because its reader completed
    // (every msgpack value is at least one byte); flushing then surfaces IsCompleted and
    // stops the producer instead of serializing the rest of the stream into the void.
    static bool ShouldFlush(PipeWriter pipeWriter)
    {
        if (!pipeWriter.CanGetUnflushedBytes)
        {
            return true;
        }
        var unflushedBytes = pipeWriter.UnflushedBytes;
        return unflushedBytes >= FlushThresholdBytes || unflushedBytes == 0;
    }

    /// <summary>
    /// True when the reader completed (further writes are pointless, producers stop
    /// silently, matching System.Text.Json's PipeWriter behavior); a canceled flush
    /// throws.
    /// </summary>
    static async ValueTask<bool> FlushAsync(PipeWriter pipeWriter, CancellationToken cancellationToken)
    {
        var result = await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        if (result.IsCanceled)
        {
            throw new OperationCanceledException("The PipeWriter flush was canceled");
        }
        return result.IsCompleted;
    }
}
