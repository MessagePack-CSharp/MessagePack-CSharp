namespace MessagePack;

// The async entries' pass 1 behind one surface: the MessagePack token scanner for plain messages and envelopes that are
// MessagePack values, or the processor's own boundary walk for containers that are not (raw LZ4 / Zstandard frames).
// Offsets are buffer-relative, like the scanner's. The processor is handed its own walk position back on every read
// of the same message (message-relative, so a Rebase does not touch it), so a frame processor resumes where it
// stopped instead of re-walking the block headers it has already seen.
struct MessageBoundaryFinder
{
    MessagePackBoundaryScanner scanner;
    readonly MessagePackMessageProcessor? processor;
    long start;
    long consumed;
    long minimum;
    long position;
    // where `start` is in the buffer the last TryFindEnd saw, so the next message of the same buffer is sliced from
    // there instead of from the buffer's first segment (messages x segments otherwise); dropped by Rebase, after
    // which the buffer is a new one and start is at or near its beginning
    SequencePosition startPosition;
    SequencePosition nextStartPosition;
    bool hasStartPosition;

    public MessageBoundaryFinder(MessagePackMessageProcessor? processor)
    {
        scanner = new MessagePackBoundaryScanner();
        this.processor = processor is { DefinesMessageBoundaries: true } ? processor : null;
    }

    /// <summary>Buffer offset just past the message found by the last successful <see cref="TryFindEnd"/>.</summary>
    public long Consumed => processor == null ? scanner.Consumed : consumed;

    /// <summary>The smallest buffer offset the current message can end at, for the size cap before more bytes are waited for.</summary>
    public long MinimumMessageSize => processor == null ? scanner.MinimumMessageSize : minimum;

    public bool TryFindEnd(in ReadOnlySequence<byte> buffer)
    {
        if (processor == null)
        {
            return scanner.TryFindEnd(in buffer);
        }
        var message = hasStartPosition ? buffer.Slice(startPosition) : buffer.Slice(start);
        if (processor.TryFindMessageEnd(in message, ref position, out var length))
        {
            if (length < 0 || length > message.Length)
            {
                throw new InvalidOperationException($"The message processor reported a message of {length} bytes inside a {message.Length}-byte buffer.");
            }
            consumed = start + length;
            minimum = consumed;
            nextStartPosition = message.GetPosition(length);
            return true;
        }
        // a lower bound below the bytes already examined carries no information; saturated, since a processor may
        // report a length it read off the wire (start + long.MaxValue would wrap negative and pass every size cap)
        var bound = Math.Max(length, message.Length);
        minimum = bound > long.MaxValue - start ? long.MaxValue : start + bound;
        return false;
    }

    public void StartNextValue()
    {
        if (processor == null)
        {
            scanner.StartNextValue();
        }
        else
        {
            start = consumed;
            position = 0;
            startPosition = nextStartPosition;
            hasStartPosition = true;
        }
    }

    public void Rebase(long consumedBytes)
    {
        if (processor == null)
        {
            scanner.Rebase(consumedBytes);
        }
        else
        {
            start -= consumedBytes;
            consumed -= consumedBytes;
            minimum -= consumedBytes;
            hasStartPosition = false;
        }
    }
}
