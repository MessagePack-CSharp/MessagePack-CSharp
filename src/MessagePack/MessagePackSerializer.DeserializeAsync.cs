using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace MessagePack;

// Async deserialization uses a two-pass approach.
// Pass 1 reads the message boundary, and pass 2 synchronously deserializes the pinned buffer.
// True async, where we check and refill the buffer at each message element,
// has too much performance impact (even if .NET 11's async runtime may improve this somewhat).
// Adding complex code that checks buffer sizes and switches back and forth with sync just to cover that performance loss is wasted effort.
// Allocating a buffer and processing it synchronously in one go is the foundation of performance,
// and serializers are well suited to this kind of processing in the first place.
// This is because a message boundary is a clear block, and a single block is never extremely large.
// Skip processing is lighter than an actual Read, so the performance concern about being two-pass mostly does not apply,
// and at least it is better than true async.
// PipeReader (consumed, examined) is well suited to this kind of processing,
// and by delegating the complex buffer management to PipeReader,
// the scanner code that reads the actual message boundaries stayed small.

public static partial class MessagePackSerializer
{
    /// <summary>
    /// Deserializes one value from <paramref name="pipeReader"/>.
    /// The value is buffered completely, up to <see cref="MessagePackSerializerOptions.MaxBufferedMessageSize"/>, then parsed. Bytes after it stay unconsumed.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static ValueTask<T> DeserializeAsync<T>(PipeReader pipeReader, CancellationToken cancellationToken = default)
    {
        return DeserializeAsync<T>(pipeReader, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="DeserializeAsync{T}(PipeReader, CancellationToken)"/>
    public static async ValueTask<T> DeserializeAsync<T>(PipeReader pipeReader, MessagePackSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var maxMessageSize = options.MaxBufferedMessageSize;
        var finder = new MessageBoundaryFinder(options.MessageProcessor);
        while (true)
        {
            var result = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;
            if (result.IsCanceled)
            {
                // the reader is abandoned mid-read (Complete is legal from here), same as the truncated/size-exceeded throws below
                throw new OperationCanceledException("The PipeReader read was canceled");
            }

            if (result.IsCompleted)
            {
                if (buffer.IsEmpty)
                {
                    pipeReader.AdvanceTo(buffer.End);
                    MessagePackSerializationException.ThrowAsyncMessageMissing();
                }

                // single-pass fast path.
                // Declined when an envelope may be present (the decode entry point owns that parse) or the buffered data exceeds the cap.
                if (options.MessageProcessor == null && buffer.Length <= maxMessageSize)
                {
                    T value = default!;
                    var state = new DeserializeState(options.MaxDepth, buffer.Length);
                    long bytesConsumed;
                    try
                    {
                        bytesConsumed = buffer.IsSingleSegment
                            ? DeserializeSpanCore(ref value, buffer.FirstSpan, options, ref state)
                            : DeserializeSequenceCore(ref value, in buffer, options, ref state);
                    }
                    catch
                    {
                        // the read must be released even when the parse throws (the reader is otherwise stuck in
                        // "reading in progress"): the failed message is consumed when its boundary is known, as on the
                        // framed path, and left in place otherwise (a boundary scan that throws on the malformed data
                        // changes nothing: the parse error is the one to report)
                        var consumed = buffer.Start;
                        try
                        {
                            if (finder.TryFindEnd(buffer))
                            {
                                consumed = buffer.GetPosition(finder.Consumed);
                            }
                        }
                        catch
                        {
                        }
                        pipeReader.AdvanceTo(consumed, buffer.End);
                        throw;
                    }
                    pipeReader.AdvanceTo(buffer.GetPosition(bytesConsumed));
                    return value;
                }
            }

            if (finder.TryFindEnd(buffer))
            {
                if (finder.Consumed > maxMessageSize)
                {
                    MessagePackSerializationException.ThrowBufferedMessageSizeExceeded(finder.Consumed, maxMessageSize);
                }

                var message = buffer.Slice(0, finder.Consumed);
                T value = default!;
                try
                {
                    // pass 2: the slice is exactly one value, so the sync entry applies as-is.
                    Deserialize(in message, ref value, options);
                }
                finally
                {
                    // the boundary is known even when the parse throws
                    pipeReader.AdvanceTo(message.End);
                }
                return value;
            }

            // reject an implausible value before buffering it (headers claiming huge payloads/counts push the lower bound over the cap immediately)
            if (finder.MinimumMessageSize > maxMessageSize)
            {
                MessagePackSerializationException.ThrowBufferedMessageSizeExceeded(finder.MinimumMessageSize, maxMessageSize);
            }

            if (result.IsCompleted)
            {
                MessagePackSerializationException.ThrowAsyncMessageTruncated();
            }

            pipeReader.AdvanceTo(buffer.Start, buffer.End); // consumed nothing, examined everything
        }
    }

    /// <summary>
    /// Deserializes concatenated top-level values until the reader completes, the MessagePack counterpart of JSON Lines.
    /// Each message is buffered completely, then parsed, so memory is bounded per message rather than per stream.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static IAsyncEnumerable<T> DeserializeMessagesAsync<T>(PipeReader pipeReader, CancellationToken cancellationToken = default)
    {
        return DeserializeMessagesAsync<T>(pipeReader, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="DeserializeMessagesAsync{T}(PipeReader, CancellationToken)"/>
    public static async IAsyncEnumerable<T> DeserializeMessagesAsync<T>(PipeReader pipeReader, MessagePackSerializerOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var maxMessageSize = options.MaxBufferedMessageSize;
        var finder = new MessageBoundaryFinder(options.MessageProcessor);
        // Every yield sits between a ReadAsync and its AdvanceTo. Leaving the iterator there (the consumer breaks
        // out of its await foreach, or a size/truncation check throws) must still hand the read back, or the
        // reader's next ReadAsync fails with "reading is already in progress": the finally advances through the
        // messages already yielded, so the rest stays for the next reader.
        var reading = false;
        var consumed = 0L;
        ReadOnlySequence<byte> buffer = default;
        try
        {
            while (true)
            {
                var result = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                buffer = result.Buffer;
                reading = true;
                consumed = 0;
                if (result.IsCanceled)
                {
                    throw new OperationCanceledException("The PipeReader read was canceled");
                }

                // completed reader: single-pass drain, the parser finds each boundary itself
                if (result.IsCompleted && options.MessageProcessor == null && buffer.Length <= maxMessageSize)
                {
                    var position = 0L;
                    while (position < buffer.Length)
                    {
                        // the token is otherwise only observed by ReadAsync: a consumer that cancels between elements
                        // must not receive the rest of an already-buffered batch
                        cancellationToken.ThrowIfCancellationRequested();
                        T value = default!;
                        var tail = buffer.Slice(position);
                        var state = new DeserializeState(options.MaxDepth, tail.Length); // every message its own state
                        position += tail.IsSingleSegment
                            ? DeserializeSpanCore(ref value, tail.FirstSpan, options, ref state)
                            : DeserializeSequenceCore(ref value, in tail, options, ref state);
                        consumed = position;
                        yield return value;
                    }
                    reading = false;
                    pipeReader.AdvanceTo(buffer.End);
                    yield break;
                }

                var batchStart = 0L;
                while (finder.TryFindEnd(buffer))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var messageEnd = finder.Consumed;
                    if (messageEnd - batchStart > maxMessageSize)
                    {
                        MessagePackSerializationException.ThrowBufferedMessageSizeExceeded(messageEnd - batchStart, maxMessageSize);
                    }

                    var message = buffer.Slice(batchStart, messageEnd - batchStart);
                    T value = default!;
                    try
                    {
                        Deserialize(in message, ref value, options);
                    }
                    catch
                    {
                        // the boundary is known even when the parse throws: consume through it
                        reading = false;
                        pipeReader.AdvanceTo(buffer.GetPosition(messageEnd));
                        throw;
                    }
                    finder.StartNextValue();
                    batchStart = messageEnd;
                    consumed = batchStart;
                    yield return value;
                }

                if (finder.MinimumMessageSize - batchStart > maxMessageSize)
                {
                    MessagePackSerializationException.ThrowBufferedMessageSizeExceeded(finder.MinimumMessageSize - batchStart, maxMessageSize);
                }

                if (result.IsCompleted)
                {
                    if (buffer.Length == batchStart)
                    {
                        reading = false;
                        pipeReader.AdvanceTo(buffer.End);
                        yield break; // clean end-of-stream at a message boundary
                    }
                    MessagePackSerializationException.ThrowAsyncMessageTruncated();
                }

                reading = false;
                pipeReader.AdvanceTo(buffer.GetPosition(batchStart), buffer.End);
                finder.Rebase(batchStart);
            }
        }
        finally
        {
            if (reading)
            {
                pipeReader.AdvanceTo(buffer.GetPosition(consumed));
            }
        }
    }

    /// <summary>
    /// Deserializes the elements of one top-level array one at a time, buffering a single element at most.
    /// A nil in place of the array yields nothing. Bytes after the array stay unconsumed.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
    public static IAsyncEnumerable<T> DeserializeElementsAsync<T>(PipeReader pipeReader, CancellationToken cancellationToken = default)
    {
        return DeserializeElementsAsync<T>(pipeReader, MessagePackSerializerOptions.Default, cancellationToken);
    }

    /// <inheritdoc cref="DeserializeElementsAsync{T}(PipeReader, CancellationToken)"/>
    public static async IAsyncEnumerable<T> DeserializeElementsAsync<T>(PipeReader pipeReader, MessagePackSerializerOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (options.MessageProcessor is { DefinesMessageBoundaries: true })
        {
            // the array header and its elements are MessagePack tokens on the pipe; inside a container envelope they are not visible
            throw new NotSupportedException("DeserializeElementsAsync reads the elements of a MessagePack array straight off the pipe, which a container-format MessageProcessor hides; read the whole message instead.");
        }
        var count = await ReadArrayHeaderAsync(pipeReader, cancellationToken).ConfigureAwait(false);
        if (count == 0)
        {
            yield break;
        }

        // batched like DeserializeMessagesAsync, plus: stops after count elements and leaves anything past the array unconsumed
        var maxMessageSize = options.MaxBufferedMessageSize;
        var finder = new MessageBoundaryFinder(options.MessageProcessor);
        // one state for the whole array, as Deserialize<T[]> has: the elements are read in turn through it, Reset for
        // each element's bytes, and the circular-reference identity table spans them, so the array wire of Serialize
        // reads back element by element (and an element stream reads back as one array)
        var state = new DeserializeState(options.MaxDepth, 0);
        var produced = 0L;
        // the same early-exit guard as DeserializeMessagesAsync: a broken-out enumeration leaves the read
        // advanced through the elements already yielded
        var reading = false;
        var consumed = 0L;
        ReadOnlySequence<byte> buffer = default;
        try
        {
            while (true)
            {
                var result = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                buffer = result.Buffer;
                reading = true;
                consumed = 0;
                if (result.IsCanceled)
                {
                    throw new OperationCanceledException("The PipeReader read was canceled");
                }

                if (result.IsCompleted && options.MessageProcessor == null && buffer.Length <= maxMessageSize)
                {
                    var position = 0L;
                    while (produced < count)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (position >= buffer.Length)
                        {
                            MessagePackSerializationException.ThrowAsyncArrayTruncated(count, produced);
                        }
                        T value = default!;
                        var tail = buffer.Slice(position);
                        state.Reset(options.MaxDepth, tail.Length);
                        position += tail.IsSingleSegment
                            ? DeserializeSpanCore(ref value, tail.FirstSpan, options, ref state)
                            : DeserializeSequenceCore(ref value, in tail, options, ref state);
                        produced++;
                        consumed = position;
                        yield return value;
                    }
                    reading = false;
                    pipeReader.AdvanceTo(buffer.GetPosition(position)); // trailing data stays
                    yield break;
                }

                var batchStart = 0L;
                while (produced < count && finder.TryFindEnd(buffer))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var elementEnd = finder.Consumed;
                    if (elementEnd - batchStart > maxMessageSize)
                    {
                        MessagePackSerializationException.ThrowBufferedMessageSizeExceeded(elementEnd - batchStart, maxMessageSize);
                    }

                    var element = buffer.Slice(batchStart, elementEnd - batchStart);
                    T value = default!;
                    try
                    {
                        DeserializeElement(in element, ref value, options, ref state);
                    }
                    catch
                    {
                        reading = false;
                        pipeReader.AdvanceTo(buffer.GetPosition(elementEnd));
                        throw;
                    }
                    finder.StartNextValue();
                    batchStart = elementEnd;
                    produced++;
                    consumed = batchStart;
                    yield return value;
                }

                if (produced == count)
                {
                    reading = false;
                    pipeReader.AdvanceTo(buffer.GetPosition(batchStart)); // trailing data stays
                    yield break;
                }

                if (finder.MinimumMessageSize - batchStart > maxMessageSize)
                {
                    MessagePackSerializationException.ThrowBufferedMessageSizeExceeded(finder.MinimumMessageSize - batchStart, maxMessageSize);
                }

                if (result.IsCompleted)
                {
                    if (buffer.Length == batchStart)
                    {
                        MessagePackSerializationException.ThrowAsyncArrayTruncated(count, produced);
                    }
                    MessagePackSerializationException.ThrowAsyncMessageTruncated();
                }

                reading = false;
                pipeReader.AdvanceTo(buffer.GetPosition(batchStart), buffer.End);
                finder.Rebase(batchStart);
            }
        }
        finally
        {
            if (reading)
            {
                pipeReader.AdvanceTo(buffer.GetPosition(consumed));
            }
        }
    }

    // one element of an element stream: the array's state is Reset for the element's own bytes (the decoded ones
    // when an envelope processor wraps each element) and keeps the circular-reference identity table across elements
    static void DeserializeElement<T>(in ReadOnlySequence<byte> element, ref T value, MessagePackSerializerOptions options, ref DeserializeState state)
    {
        var processor = options.MessageProcessor;
        if (processor != null && processor.TryDecode(in element, out var decoded))
        {
            try
            {
                switch (decoded)
                {
                    case DecodedMessage.SourceSlice slice:
                        CheckSourceSlice(slice, element.Length);
                        var sliced = element.Slice(slice.Offset, slice.Length);
                        state.Reset(options.MaxDepth, sliced.Length);
                        DeserializeSequenceOrSpan(ref value, in sliced, options, ref state);
                        break;
                    case DecodedMessage.Buffer buffer:
                        state.Reset(options.MaxDepth, buffer.Sequence.Length);
                        DeserializeSequenceOrSpan(ref value, buffer.Sequence, options, ref state);
                        break;
                    default:
                        throw new InvalidOperationException("The message processor returned an empty DecodedMessage.");
                }
            }
            finally
            {
                decoded.Dispose();
            }
            return;
        }

        state.Reset(options.MaxDepth, element.Length);
        DeserializeSequenceOrSpan(ref value, in element, options, ref state);
    }

    static async ValueTask<long> ReadArrayHeaderAsync(PipeReader pipeReader, CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;
            if (result.IsCanceled)
            {
                throw new OperationCanceledException("The PipeReader read was canceled");
            }

            long count;
            int headerSize;
            bool found;
            try
            {
                found = TryReadArrayHeader(in buffer, out count, out headerSize);
                if (!found && result.IsCompleted)
                {
                    MessagePackSerializationException.ThrowAsyncMessageTruncated();
                }
            }
            catch
            {
                // a value that is not an array, or a truncated header: the read is released with nothing consumed and
                // nothing examined, so the caller's next read returns the same data at once instead of waiting for more
                pipeReader.AdvanceTo(buffer.Start);
                throw;
            }
            if (found)
            {
                pipeReader.AdvanceTo(buffer.GetPosition(headerSize));
                return count;
            }

            pipeReader.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    static bool TryReadArrayHeader(in ReadOnlySequence<byte> buffer, out long count, out int headerSize)
    {
        count = 0;
        headerSize = 0;
        if (buffer.IsEmpty)
        {
            return false;
        }

        Span<byte> header = stackalloc byte[5];
        var available = (int)Math.Min(buffer.Length, 5);
        buffer.Slice(0, available).CopyTo(header);
        var code = header[0];
        if ((code & 0xF0) == MessagePackCode.MinFixArray)
        {
            count = code & 0b0000_1111;
            headerSize = 1;
            return true;
        }
        switch (code)
        {
            case MessagePackCode.Nil: // nil in place of the array: zero elements
                headerSize = 1;
                return true;
            case MessagePackCode.Array16:
                if (available < 3)
                {
                    return false;
                }
                count = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(1));
                headerSize = 3;
                return true;
            case MessagePackCode.Array32:
                if (available < 5)
                {
                    return false;
                }
                count = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(1));
                headerSize = 5;
                return true;
            default:
                throw new MessagePackSerializationException($"DeserializeElementsAsync expects a top-level array but found code 0x{code:x2}");
        }
    }
}
