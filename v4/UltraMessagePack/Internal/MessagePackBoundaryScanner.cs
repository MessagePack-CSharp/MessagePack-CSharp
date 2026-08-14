namespace UltraMessagePack;

/// <summary>
/// Resumable boundary scanner for the async two-pass deserializer:
/// pass 1 walks tokens via <see cref="MessagePackPrimitives.TryReadToken"/> over the PipeReader's
/// buffered-but-unconsumed bytes to find where one top-level value ends,
/// pass 2 hands exactly that range to the synchronous parser.
/// Count-based like ReadBufferExtensions.Skip (iterative, so adversarial nesting depth cannot blow the stack),
/// but suspendable: the whole resume state is the outstanding value count plus the opaque payload bytes still to pass over,
/// so a scan can stop at the end of the buffered data and continue when more arrives without re-reading anything already scanned.
/// </summary>
internal struct MessagePackBoundaryScanner
{
    // the largest tokenSize TryReadToken can require (ext32: code + 4 length bytes + type byte)
    const int MaxTokenSize = 6;

    long remainingValues;     // msgpack values still outstanding (each container adds its children)
    long pendingPayloadBytes; // opaque bytes (str/bin/ext payloads, numeric bodies) still to pass over
    long consumed;            // offset into the buffered sequence already scanned

    /// <summary>Bytes of the buffered sequence covered by the value once complete.</summary>
    public long Consumed => consumed;

    /// <summary>
    /// Lower bound of the total bytes the value will occupy (every outstanding value is at least one byte),
    /// letting the caller reject an implausible message against the size cap before buffering it all.
    /// </summary>
    public long MinimumMessageSize => consumed + pendingPayloadBytes + remainingValues;

    /// <summary>
    /// A fresh scanner is primed to find the end of one value.
    /// </summary>
    public MessagePackBoundaryScanner()
    {
        remainingValues = 1;
    }

    /// <summary>
    /// Re-arms the scanner for the value that follows the one just found, keeping the
    /// scan position, so successive <see cref="Consumed"/> values are the boundaries of
    /// consecutive messages within the same buffered sequence.
    /// </summary>
    public void StartNextValue()
    {
        remainingValues = 1;
    }

    /// <summary>
    /// Shifts the scan origin after the caller consumed the leading
    /// <paramref name="consumedBytes"/> of the buffered sequence (PipeReader.AdvanceTo):
    /// the next TryFindEnd expects a sequence starting where those bytes ended, and any
    /// progress into a not-yet-complete value is kept.
    /// </summary>
    public void Rebase(long consumedBytes)
    {
        consumed -= consumedBytes;
    }

    /// <summary>
    /// Scans forward from the previous stop. True when the end of the value is found, at
    /// which point <see cref="Consumed"/> is its total size; false means every buffered
    /// byte was scanned and more data is required. The buffer must be the same sequence
    /// as the previous call, only grown (the PipeReader examined-but-not-consumed pattern).
    /// </summary>
    public bool TryFindEnd(in ReadOnlySequence<byte> buffer)
    {
        var totalLength = buffer.Length;
        var remaining = remainingValues;
        var pending = pendingPayloadBytes;
        var offset = consumed;

        // the value ends when its outstanding work — values still to read plus payload
        // bytes still to pass over — reaches zero; running out of buffered data breaks
        // out instead, and the epilogue reports which of the two happened
        while (remaining > 0 || pending > 0)
        {
            // opaque payload bytes: pure arithmetic, crosses segment boundaries freely
            if (pending > 0)
            {
                var take = Math.Min(pending, totalLength - offset);
                offset += take;
                pending -= take;
                if (pending > 0)
                {
                    break; // buffered data ran out inside the payload
                }
                continue; // the payload may have been the value's tail
            }

            if (offset == totalLength)
            {
                break; // buffered data ran out at a token boundary
            }

            if (!ScanTokens(buffer, totalLength, ref remaining, ref pending, ref offset))
            {
                break; // a token's length bytes are not fully buffered yet
            }
        }

        remainingValues = remaining;
        pendingPayloadBytes = pending;
        consumed = offset;
        return remaining == 0 && pending == 0;
    }

    static bool ScanTokens(in ReadOnlySequence<byte> buffer, long totalLength, ref long remaining, ref long pending, ref long offset)
    {
        Span<byte> stitch = stackalloc byte[MaxTokenSize];
        foreach (var segment in buffer.Slice(offset))
        {
            var span = segment.Span;
            var index = 0;
            while (index < span.Length)
            {
                if (pending > 0)
                {
                    var take = (int)Math.Min(pending, span.Length - index);
                    index += take;
                    pending -= take;
                    continue;
                }

                if (remaining == 0)
                {
                    offset += index; // value complete mid-span
                    return true;
                }

                var result = MessagePackPrimitives.TryReadToken(span.Slice(index), out var payloadLength, out var childValueCount, out var tokenSize);
                if (result == DecodeResult.Success)
                {
                    remaining += childValueCount - 1;
                    pending = payloadLength;
                    index += tokenSize;
                    continue;
                }
                if (result == DecodeResult.TokenMismatch)
                {
                    throw new MessagePackSerializationException("The msgpack data contains the never-used code 0xc1");
                }

                // InsufficientBuffer with the code byte present, so tokenSize is the
                // token's exact requirement: its length bytes straddle the segment
                // boundary. Stitch them through the sequence if buffered.
                var tokenStart = offset + index;
                if (totalLength - tokenStart < tokenSize)
                {
                    offset = tokenStart;
                    return false;
                }
                buffer.Slice(tokenStart, tokenSize).CopyTo(stitch);
                // cannot fail: tokenSize bytes are present and 0xc1 was excluded above
                MessagePackPrimitives.TryReadToken(stitch.Slice(0, tokenSize), out payloadLength, out childValueCount, out tokenSize);
                remaining += childValueCount - 1;
                pending = payloadLength;
                offset = tokenStart + tokenSize;
                return true;
            }

            offset += span.Length;
        }
        return true; // every buffered byte scanned
    }
}
