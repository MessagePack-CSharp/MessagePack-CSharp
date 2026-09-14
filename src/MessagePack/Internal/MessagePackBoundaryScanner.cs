#if NET9_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace MessagePack;

/// <summary>
/// Resumable boundary scanner for the async two-pass deserializer.
/// Pass 1 walks tokens over the PipeReader's buffered bytes to find where one top-level value ends, and pass 2 hands exactly that range to the synchronous parser.
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
        var remaining = remainingValues;
        var pending = pendingPayloadBytes;

        if (buffer.IsSingleSegment)
        {
            // the common case (a message inside one pipe segment): one span, no segment enumerator,
            // and nothing to stitch, a header running past the end is simply more data needed
            var index = (int)consumed;
            var status = ScanSpan(buffer.FirstSpan, ref remaining, ref pending, ref index, out _);
            remainingValues = remaining;
            pendingPayloadBytes = pending;
            consumed = index;
            return status == ScanStatus.Done;
        }

        var offset = consumed;
        var found = ScanSegments(in buffer, ref remaining, ref pending, ref offset);
        remainingValues = remaining;
        pendingPayloadBytes = pending;
        consumed = offset;
        return found;
    }

    // The multi-segment walk: ScanSpan per segment, and a multi-byte header whose length bytes
    // straddle a segment boundary is stitched through the sequence when they are all buffered.
    static bool ScanSegments(in ReadOnlySequence<byte> buffer, ref long remaining, ref long pending, ref long offset)
    {
        var totalLength = buffer.Length;
        Span<byte> stitch = stackalloc byte[MaxTokenSize];
        while (true)
        {
            if (offset == totalLength)
            {
                return remaining == 0 && pending == 0;
            }
            var straddled = false;
            foreach (var segment in buffer.Slice(offset))
            {
                var index = 0;
                var status = ScanSpan(segment.Span, ref remaining, ref pending, ref index, out var need);
                offset += index;
                if (status == ScanStatus.Done)
                {
                    return true;
                }
                if (status == ScanStatus.Straddle)
                {
                    if (totalLength - offset < need)
                    {
                        return false; // the header's length bytes are not buffered yet
                    }
                    buffer.Slice(offset, need).CopyTo(stitch);
                    // cannot fail: need bytes are present and 0xc1 was excluded by ScanSpan
                    MessagePackPrimitives.TryReadToken(stitch.Slice(0, need), out var payloadLength, out var childValueCount, out var tokenSize);
                    remaining += childValueCount - 1;
                    pending = payloadLength;
                    offset += tokenSize;
                    straddled = true;
                    break; // re-slice from the new offset: the payload (if any) is skipped by the next ScanSpan
                }
            }
            if (!straddled)
            {
                return remaining == 0 && pending == 0;
            }
        }
    }

    enum ScanStatus
    {
        Done,      // the value ended inside the span, index is its end
        Exhausted, // every byte of the span was scanned, more data needed (pending may be > 0)
        Straddle,  // a multi-byte header starts at index but its length bytes are not all in the span; need is its size
    }

    // The hot loop over one span. Classification is a jump table on the top three bits of the code byte
    // (positive fixint x4, fixmap/fixarray, fixstr, 0xc0-0xdf, negative fixint) with the one-byte tokens handled
    // in place, their payload skipped without leaving the loop when it is buffered, and the end-of-value test in each
    // branch right after the decrement, so the loop head is a single compare. Only the multi-byte headers
    // (str8+/bin/ext/array16+/map16+) and the numeric bodies go through TryReadToken.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ScanStatus ScanSpan(ReadOnlySpan<byte> span, ref long remaining, ref long pending, ref int index, out int need)
    {
        need = 0;
        var length = span.Length;
        var i = index;
        var rem = remaining;
        var pend = pending;
        ref var start = ref MemoryMarshal.GetReference(span);

        // a payload left over from the previous span
        if (pend > 0)
        {
            var take = (int)Math.Min(pend, length - i);
            i += take;
            pend -= take;
            if (pend > 0)
            {
                goto exhausted;
            }
            if (rem == 0)
            {
                goto done; // the payload was the value's tail
            }
        }

        while (i < length)
        {
            var code = Unsafe.Add(ref start, i);
            switch (code >> 5)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 7: // positive/negative fixint
                    i++;
                    rem--;
#if NET9_0_OR_GREATER
                    // bulk fixint skip: 32 (or 16) code bytes at once while they are all fixints (sbyte >= -32)
                    if (Vector256.IsHardwareAccelerated)
                    {
                        while (rem >= 32 && length - i >= 32)
                        {
                            var v = Vector256.LoadUnsafe(ref start, (nuint)i).AsSByte();
                            if (!Vector256.GreaterThanAll(v, Vector256.Create((sbyte)-33)))
                            {
                                break;
                            }
                            i += 32;
                            rem -= 32;
                        }
                    }
                    else if (Vector128.IsHardwareAccelerated)
                    {
                        // unmeasured (no ARM box here), same shape at half width
                        while (rem >= 16 && length - i >= 16)
                        {
                            var v = Vector128.LoadUnsafe(ref start, (nuint)i).AsSByte();
                            if (!Vector128.GreaterThanAll(v, Vector128.Create((sbyte)-33)))
                            {
                                break;
                            }
                            i += 16;
                            rem -= 16;
                        }
                    }
#endif
                    if (rem == 0) goto done;
                    continue;
                case 4: // fixmap 0x80-0x8f (entries count twice), fixarray 0x90-0x9f
                {
                    i++;
                    var count = code & 0x0f;
                    rem += ((code & 0x10) != 0 ? count : count << 1) - 1;
                    if (rem == 0) goto done;
                    continue;
                }
                case 5: // fixstr, payload skipped in place when buffered
                {
                    var payload = code & 0x1f;
                    i++;
                    rem--;
                    var available = length - i;
                    if (available >= payload)
                    {
                        i += payload;
                        if (rem == 0) goto done;
                        continue;
                    }
                    pend = payload - available;
                    i = length;
                    goto exhausted;
                }
                default: // 0xc0-0xdf
                {
                    if (code == MessagePackCode.Nil || (byte)(code - MessagePackCode.False) < 2)
                    {
                        i++;
                        rem--;
                        if (rem == 0) goto done;
                        continue;
                    }

                    // numeric bodies, str8+/bin/ext headers, array16+/map16+, and 0xc1
                    var result = MessagePackPrimitives.TryReadToken(span.Slice(i), out var payloadLength, out var childValueCount, out var tokenSize);
                    if (result == DecodeResult.Success)
                    {
                        i += tokenSize;
                        rem += childValueCount - 1;
                        if (payloadLength > 0)
                        {
                            var available = length - i;
                            if (available >= payloadLength)
                            {
                                i += (int)payloadLength;
                            }
                            else
                            {
                                pend = payloadLength - available;
                                i = length;
                                goto exhausted;
                            }
                        }
                        if (rem == 0) goto done;
                        continue;
                    }
                    if (result == DecodeResult.TokenMismatch)
                    {
                        throw new MessagePackSerializationException("The msgpack data contains the never-used code 0xc1");
                    }

                    // InsufficientBuffer with the code byte present, so tokenSize is the token's exact requirement
                    // and its length bytes run past this span
                    index = i;
                    remaining = rem;
                    pending = pend;
                    need = tokenSize;
                    return ScanStatus.Straddle;
                }
            }
        }

    exhausted:
        index = i;
        remaining = rem;
        pending = pend;
        return ScanStatus.Exhausted;

    done:
        index = i;
        remaining = 0;
        pending = 0;
        return ScanStatus.Done;
    }
}
