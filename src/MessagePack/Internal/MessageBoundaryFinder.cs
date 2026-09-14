namespace MessagePack;

// The async entries' pass 1 behind one surface: the MessagePack token scanner for plain messages and envelopes that are
// MessagePack values, or the processor's own boundary walk for containers that are not (raw LZ4 / Zstandard frames).
// Offsets are buffer-relative, like the scanner's; the processor walk restarts from the message start on every read,
// which is cheap because a walk only touches block headers.
struct MessageBoundaryFinder
{
    MessagePackBoundaryScanner scanner;
    readonly MessagePackMessageProcessor? processor;
    long start;
    long consumed;
    long minimum;

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
        var message = buffer.Slice(start);
        if (processor.TryFindMessageEnd(in message, out var length))
        {
            if (length < 0 || length > message.Length)
            {
                throw new InvalidOperationException($"The message processor reported a message of {length} bytes inside a {message.Length}-byte buffer.");
            }
            consumed = start + length;
            minimum = consumed;
            return true;
        }
        // a lower bound below the bytes already examined carries no information
        minimum = start + Math.Max(length, message.Length);
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
        }
    }
}
