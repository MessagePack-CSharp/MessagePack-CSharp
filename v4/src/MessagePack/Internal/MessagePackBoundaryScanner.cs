#if NET9_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace MessagePack;

/// <summary>
/// Resumable boundary scanner for the async two-pass deserializer.
/// Pass 1 walks tokens through <see cref="MessagePackPrimitives.TryReadToken"/> over the PipeReader's buffered bytes to find where one top-level value ends, and pass 2 hands exactly that range to the synchronous parser.
/// The walk is count-based like Skip, so nesting depth cannot exhaust the stack, and suspendable. The whole resume state is the outstanding value count plus the payload bytes still to pass over,
/// so a scan can stop at the end of the buffered data and continue when more arrives without re-reading anything.
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

    /// <summary>A fresh scanner is primed to find the end of one value.</summary>
    public MessagePackBoundaryScanner()
    {
        remainingValues = 1;
    }

    /// <summary>
    /// Re-arms the scanner for the value that follows the one just found, keeping the scan position,
    /// so successive <see cref="Consumed"/> values are the boundaries of consecutive messages within the same buffered sequence.
    /// </summary>
    public void StartNextValue()
    {
        remainingValues = 1;
    }

    /// <summary>
    /// Shifts the scan origin after the caller consumed the leading <paramref name="consumedBytes"/> of the buffered sequence.
    /// The next TryFindEnd expects a sequence starting where those bytes ended, and any progress into an incomplete value is kept.
    /// </summary>
    public void Rebase(long consumedBytes)
    {
        consumed -= consumedBytes;
    }

    /// <summary>
    /// Scans forward from the previous stop. Returns true when the end of the value is found, at which point <see cref="Consumed"/> is its total size,
    /// and false when every buffered byte was scanned and more data is required.
    /// The buffer must be the same sequence as in the previous call, only grown.
    /// </summary>
    public bool TryFindEnd(in ReadOnlySequence<byte> buffer)
    {
        var totalLength = buffer.Length;
        var remaining = remainingValues;
        var pending = pendingPayloadBytes;
        var offset = consumed;

        // The value ends when its outstanding work (values still to read plus payload bytes still to pass over)
        // reaches zero. Running out of buffered data breaks out instead, and the epilogue reports which of the two happened.
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

#if NET9_0_OR_GREATER
                    // Bulk fixint skip
                    if (Vector256.IsHardwareAccelerated)
                    {
                        if (remaining >= 32 && (payloadLength | childValueCount) == 0 && tokenSize == 1)
                        {
                            while (remaining >= 32 && span.Length - index >= 32)
                            {
                                var v = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(span), (nuint)index).AsSByte();
                                if (!Vector256.GreaterThanAll(v, Vector256.Create((sbyte)-33)))
                                {
                                    break;
                                }
                                index += 32;
                                remaining -= 32;
                            }
                        }
                    }
                    else if (Vector128.IsHardwareAccelerated)
                    {
                        // unmeasured (no ARM box here), same shape at half width
                        if (remaining >= 16 && (payloadLength | childValueCount) == 0 && tokenSize == 1)
                        {
                            while (remaining >= 16 && span.Length - index >= 16)
                            {
                                var v = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(span), (nuint)index).AsSByte();
                                if (!Vector128.GreaterThanAll(v, Vector128.Create((sbyte)-33)))
                                {
                                    break;
                                }
                                index += 16;
                                remaining -= 16;
                            }
                        }
                    }
#endif
                    continue;
                }
                if (result == DecodeResult.TokenMismatch)
                {
                    throw new MessagePackSerializationException("The msgpack data contains the never-used code 0xc1");
                }

                // InsufficientBuffer with the code byte present, so tokenSize is the token's exact requirement and its
                // length bytes straddle the segment boundary. Stitch them through the sequence if buffered.
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
