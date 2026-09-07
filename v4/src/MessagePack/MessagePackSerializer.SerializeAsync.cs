using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace MessagePack;

// Async serialization is sync writing plus awaited flushes. PipeWriter.GetSpan/Advance never block, so the ordinary
// synchronous entry writes straight into pipe memory and the async layer only awaits FlushAsync at message boundaries,
// which is where backpressure and cancellation live. No scanner, no async formatters, the serialize side of the two-pass
// deserializer's conclusion. Flushes are batched. Bytes ride along until FlushThresholdBytes is crossed (the
// System.Text.Json FlushThreshold analog), and the streaming APIs additionally flush whenever the source is about to
// suspend, so everything serialized so far reaches the reader before waiting for more input.
// Non-seekable raw Streams are handled by wrapping them with PipeWriter.Create. The writer is never completed here,
// because the caller owns the PipeWriter lifetime.
public static partial class MessagePackSerializer
{
    // half of the default Pipe pause threshold (64KB), so a batch flush lands before backpressure would engage on a default pipe
    const long FlushThresholdBytes = 32 * 1024;

    /// <summary>
    /// Serializes one value into <paramref name="pipeWriter"/> and flushes.
    /// Writing is synchronous, since pipe memory never blocks; the flush is where backpressure and cancellation apply. The writer is left uncompleted.
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
    /// Serializes the values as concatenated top-level messages, the MessagePack counterpart of JSON Lines, read back by <see cref="DeserializeMessagesAsync{T}(PipeReader, CancellationToken)"/>.
    /// Flushes are batched, with an extra flush whenever the source is about to suspend so the reader sees everything serialized so far.
    /// Returns silently when the reader completes. The writer is left uncompleted.
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
                    // the source is about to suspend, so make everything written so far visible to the reader before
                    // waiting for more input
                    var hasNext = await FlushWhilePendingAsync(pipeWriter, moveNext, cancellationToken).ConfigureAwait(false);
                    if (hasNext is null)
                    {
                        return;
                    }
                    if (!hasNext.Value)
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
    /// Serializes the values as one top-level array of <paramref name="count"/> elements, read back by <see cref="DeserializeElementsAsync{T}(PipeReader, CancellationToken)"/>.
    /// MessagePack arrays carry their length up front, so the source must yield exactly <paramref name="count"/> elements; for an unknown count use <c>SerializeMessagesAsync</c>.
    /// Flushing and reader completion behave as there.
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
                    var next = await FlushWhilePendingAsync(pipeWriter, moveNext, cancellationToken).ConfigureAwait(false);
                    if (next is null)
                    {
                        return;
                    }
                    hasNext = next.Value;
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

            // the excess probe is one more MoveNextAsync, with the same flush-before-suspend as the loop: the
            // elements already written must reach the reader before this waits on the source's end
            var probe = enumerator.MoveNextAsync();
            bool excess;
            if (probe.IsCompletedSuccessfully)
            {
                excess = probe.Result;
            }
            else
            {
                var next = await FlushWhilePendingAsync(pipeWriter, probe, cancellationToken).ConfigureAwait(false);
                if (next is null)
                {
                    return;
                }
                excess = next.Value;
            }
            if (excess)
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
        // the array formatter family is int-bounded (a materializable payload never exceeds it), so the streaming
        // header keeps the same bound
        if (count is < 0 or > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        var span = pipeWriter.GetSpan(MessagePackPrimitives.MaxArrayHeaderLength);
        pipeWriter.Advance(MessagePackPrimitives.UnsafeWriteArrayHeader(ref span[0], (int)count));
    }

    // Called right after a message was serialized. Writers that cannot report unflushed bytes flush per message, the
    // safe default. Zero unflushed bytes at this point is only possible when the pipe discarded the write because its
    // reader completed (every msgpack value is at least one byte); flushing then surfaces IsCompleted and stops the
    // producer instead of serializing the rest of the stream into the void.
    static bool ShouldFlush(PipeWriter pipeWriter)
    {
        if (!pipeWriter.CanGetUnflushedBytes)
        {
            return true;
        }
        var unflushedBytes = pipeWriter.UnflushedBytes;
        return unflushedBytes >= FlushThresholdBytes || unflushedBytes == 0;
    }

    // True when the reader completed (further writes are pointless, so producers stop silently, matching
    // System.Text.Json's PipeWriter behavior). A canceled flush throws.
    // The flush-before-suspend, issued while the source's MoveNextAsync is in flight. Whatever the flush's outcome
    // (reader gone, canceled, faulted), that MoveNextAsync is settled before control can reach the enumerator's
    // DisposeAsync: an async iterator refuses DisposeAsync mid-MoveNext with NotSupportedException, which would
    // otherwise replace the silent return or the flush's own exception. Returns null when the reader completed
    // (the caller returns silently), else the MoveNextAsync result.
    static async ValueTask<bool?> FlushWhilePendingAsync(PipeWriter pipeWriter, ValueTask<bool> moveNext, CancellationToken cancellationToken)
    {
        bool readerCompleted;
        try
        {
            readerCompleted = await FlushAsync(pipeWriter, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            try
            {
                await moveNext.ConfigureAwait(false);
            }
            catch
            {
                // the flush's exception is the one to report
            }
            throw;
        }
        var hasNext = await moveNext.ConfigureAwait(false);
        return readerCompleted ? null : hasNext;
    }

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
