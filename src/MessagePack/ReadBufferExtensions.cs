using static MessagePack.MessagePackPrimitives;

namespace MessagePack;

// Read-side mirror of WriteBufferExtensions: throwing readers layered over the span-based TryRead primitives.
// Fast path decodes straight from the buffer's current span (for contiguous buffers that is everything remaining).
// The NoInlining slow path is driven by the DecodeResult category.
// InsufficientBuffer it demands exactly the tokenSize the primitive reported via TryGetSpan (the ReadOnlySequenceReadBuffer straddle case stitches precisely that much) and retries.
// TryGetSpan returning false means genuine truncation and exits the loop into the domain exception, the foundation never throws for it.
// tokenSize strictly exceeds the window it was reported for, so the loop terminates (str takes two hops: header requirement first, then header + payload).
// TokenMismatch throws immediately.
//
// Materialization order matters: extract the value (GetString) before Advance,
// because Advance may return a stitched temp buffer to the pool and invalidate the span.
//
// Payloads whose destination is already contiguous do not go through a window at all.
// ReadBinary reads only the header that way and then hands its result array to IReadBuffer.CopyTo,
// which copies out of the segments once instead of stitching first.

/// <summary>
/// Reads single MessagePack tokens from any <see cref="IReadBuffer"/>.
/// Each call consumes exactly one token and throws <see cref="MessagePackSerializationException"/> on a mismatch, an out-of-range value or truncated data.
/// </summary>
public static class ReadBufferExtensions
{
    // see PresizeCapacity: 8 keeps every type whose in-memory size is at most 8x its wire size (reference-type elements
    // behind a 1-byte nil, long behind a 1-byte fixint) on the one-allocation path
    const long PresizeBytesPerUnreadByte = 8;

    /// <summary>
    /// Gets the capacity to pre-size a collection of <typeparamref name="T"/> with, for a header that declared <paramref name="count"/> elements with <paramref name="bytesRemaining"/> bytes left to read.
    /// The header guards bound the count by the bytes of the message, but the allocation is the count times the element size, so a
    /// header can still claim far more memory than its bytes justify. The capacity is capped at 8 times the unread bytes; a formatter
    /// that gets less than <paramref name="count"/> back grows its collection while reading, so the allocation follows the bytes
    /// actually decoded. Data whose in-memory size is at most 8 times its wire size always gets <paramref name="count"/> back.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PresizeCapacity<T>(int count, long bytesRemaining)
    {
        var budget = PresizeBytesPerUnreadByte * bytesRemaining;
        return (long)count * Unsafe.SizeOf<T>() <= budget ? count : (int)(budget / Unsafe.SizeOf<T>());
    }

    // a Dictionary or HashSet entry carries, besides the key and value, a hash code and a chain link (8 bytes) and a
    // bucket slot (4 bytes): sized by sizeof(T) alone, a hash container overshot the 8x cap by that much per entry
    const int HashEntryOverhead = 12;

    /// <summary>
    /// <see cref="PresizeCapacity{T}"/> for a hash-based container (<c>Dictionary</c>, <c>HashSet</c> and the types built on them), whose entries cost
    /// about 12 bytes beyond <typeparamref name="T"/> (the <c>KeyValuePair</c> for a dictionary) for the hash code, the chain link and the bucket slot.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PresizeHashCapacity<T>(int count, long bytesRemaining)
    {
        var budget = PresizeBytesPerUnreadByte * bytesRemaining;
        var entrySize = Unsafe.SizeOf<T>() + HashEntryOverhead;
        return (long)count * entrySize <= budget ? count : (int)(budget / entrySize);
    }

    extension<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        #region Int32, 64 / UInt32, 64

        /// <summary>Reads an int32 from any msgpack int format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            var r = TryReadInt32(buffer.GetUnreadSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadInt32Slow(ref buffer, r, tokenSize);
        }

        /// <summary>Reads an int64 from any msgpack int format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            var r = TryReadInt64(buffer.GetUnreadSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadInt64Slow(ref buffer, r, tokenSize);
        }

        /// <summary>Reads a uint32 from any msgpack int format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            var r = TryReadUInt32(buffer.GetUnreadSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadUInt32Slow(ref buffer, r, tokenSize);
        }

        /// <summary>Reads a uint64 from any msgpack int format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            var r = TryReadUInt64(buffer.GetUnreadSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadUInt64Slow(ref buffer, r, tokenSize);
        }

        #endregion

        #region Int8, 16

        /// <summary>Reads a byte from any msgpack int format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            var r = TryReadByte(buffer.GetUnreadSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadByteSlow(ref buffer, r, tokenSize);
        }

        /// <summary>Reads an sbyte from any msgpack int format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte()
        {
            var r = TryReadSByte(buffer.GetUnreadSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadSByteSlow(ref buffer, r, tokenSize);
        }

        /// <summary>Reads an int16 from any msgpack int format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            var r = TryReadInt16(buffer.GetUnreadSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadInt16Slow(ref buffer, r, tokenSize);
        }

        /// <summary>Reads a uint16 from any msgpack int format.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            var r = TryReadUInt16(buffer.GetUnreadSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadUInt16Slow(ref buffer, r, tokenSize);
        }

        /// <summary>Reads a char encoded as a uint16.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char ReadChar() => (char)ReadUInt16(ref buffer);

        #endregion

        #region Nil, Boolean, Single, Double

        /// <summary>Returns the next code byte without consuming it, or false when no bytes remain.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out byte code)
        {
            // Relies on the buffer guarantee that the current span is non-empty whenever bytes remain (the same single-byte guarantee TryReadNil builds on).
            var span = buffer.GetUnreadSpan();
            if (span.Length > 0)
            {
                code = span[0];
                return true;
            }
            code = 0;
            return false;
        }

        /// <summary>Consumes the nil and returns true when the next value is nil; otherwise consumes nothing.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadNil()
        {
            if (MessagePackPrimitives.TryReadNil(buffer.GetUnreadSpan()))
            {
                buffer.Advance(1); // nil is always exactly 1 byte
                return true;
            }
            return false;
        }

        /// <summary>Reads a boolean.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
        {
            // single-byte format: never straddles a segment, no stitched retry needed
            var r = TryReadBoolean(buffer.GetUnreadSpan(), out var value, out _);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(1);
                return value;
            }
            throw Unreadable("boolean", buffer.GetUnreadSpan(), r);
        }

        /// <summary>Reads a float32. A float64 or an int format is converted.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            var r = TryReadSingle(buffer.GetUnreadSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadSingleSlow(ref buffer, r, tokenSize);
        }

        /// <summary>Reads a float64. A float32 or an int format is converted.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            var r = TryReadDouble(buffer.GetUnreadSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadDoubleSlow(ref buffer, r, tokenSize);
        }

        #endregion

        #region Nullable primitives

        // Nil-or-value in one call: what a Nullable<primitive> member reads (the source generator emits these for its
        // direct tier). Each is exactly `TryReadNil() ? null : ReadX()`: the same values, the same bytes consumed,
        // the same exception for anything else (NullableReaderTests holds every one of them to that).
        //
        // Every reader here returns through ONE expression of the `hasValue ? value : default(T?)` form. An inlined
        // method that returns Nullable<T> from several return points gets its result in one stack temp, written as a
        // byte (HasValue) and the value and read back as one wider load; the two narrow stores cannot forward to it,
        // and every call pays a store-forwarding stall (NullableReadBenchmark: 1.4-2.6x slower than the pair it
        // replaced, with fewer instructions). The single expression keeps HasValue and the value in two registers.
        //
        // The decoding is in the ReadXOrNil helpers below the extension block, one per wire tier.

        /// <summary>Reads an int32 from any msgpack int format, or null when the next value is nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int? ReadNullableInt32() => ReadInt32OrNil(ref buffer, out var value) ? value : default(int?);

        /// <summary>Reads a uint32 from any msgpack int format, or null when the next value is nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint? ReadNullableUInt32() => ReadUInt32OrNil(ref buffer, out var value) ? value : default(uint?);

        /// <summary>Reads an int64 from any msgpack int format, or null when the next value is nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long? ReadNullableInt64() => ReadInt64OrNil(ref buffer, out var value) ? value : default(long?);

        /// <summary>Reads a uint64 from any msgpack int format, or null when the next value is nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong? ReadNullableUInt64() => ReadUInt64OrNil(ref buffer, out var value) ? value : default(ulong?);

        /// <summary>Reads an int16 from any msgpack int format, or null when the next value is nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short? ReadNullableInt16() => ReadNarrow16OrNil(ref buffer, short.MinValue, short.MaxValue, out var value) ? (short)value : default(short?);

        /// <summary>Reads a uint16 from any msgpack int format, or null when the next value is nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort? ReadNullableUInt16() => ReadNarrow16OrNil(ref buffer, ushort.MinValue, ushort.MaxValue, out var value) ? (ushort)value : default(ushort?);

        /// <summary>Reads a byte from any msgpack int format, or null when the next value is nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte? ReadNullableByte() => ReadByteOrNil(ref buffer, out var value) ? value : default(byte?);

        /// <summary>Reads an sbyte from any msgpack int format, or null when the next value is nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte? ReadNullableSByte() => ReadSByteOrNil(ref buffer, out var value) ? value : default(sbyte?);

        /// <summary>Reads a boolean, or null when the next value is nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool? ReadNullableBoolean() => ReadBooleanOrNil(ref buffer, out var value) ? value : default(bool?);

        /// <summary>Reads a char (a uint16 on the wire), or null when the next value is nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char? ReadNullableChar() => ReadNarrow16OrNil(ref buffer, ushort.MinValue, ushort.MaxValue, out var value) ? (char)value : default(char?);

        /// <summary>Reads a float32 (a float64 or an int format is converted), or null when the next value is nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float? ReadNullableSingle() => ReadSingleOrNil(ref buffer, out var value) ? value : default(float?);

        /// <summary>Reads a float64 (a float32 or an int format is converted), or null when the next value is nil.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double? ReadNullableDouble() => ReadDoubleOrNil(ref buffer, out var value) ? value : default(double?);

        #endregion

        #region headers(array, map, string, bin), binary

        /// <summary>
        /// Reads an array header and returns the element count. A count larger than the remaining data throws.
        /// Formatters use <see cref="ReadArrayHeader{TReadBuffer}(ref TReadBuffer, ref DeserializeState)"/>, which also charges the message-wide declared-element budget.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadArrayHeader()
        {
            var r = TryReadArrayHeader(buffer.GetUnreadSpan(), out var count, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
            }
            else
            {
                count = ReadArrayHeaderSlow(ref buffer, r, tokenSize);
            }

            // Allocation-bomb guard: formatters preallocate from this count,
            // and every msgpack element occupies at least one byte, so a count exceeding the remaining payload is
            // provably a lie, reject it before anyone allocates.
            if ((uint)count > (ulong)buffer.BytesRemaining)
            {
                MessagePackSerializationException.ThrowImplausibleCollectionHeader("array", count, buffer.BytesRemaining);
            }
            return count;
        }

        /// <summary>
        /// Reads a map header and returns the entry count. A count larger than the remaining data throws.
        /// Formatters use <see cref="ReadMapHeader{TReadBuffer}(ref TReadBuffer, ref DeserializeState)"/>, which also charges the message-wide declared-element budget.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadMapHeader()
        {
            var r = TryReadMapHeader(buffer.GetUnreadSpan(), out var count, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
            }
            else
            {
                count = ReadMapHeaderSlow(ref buffer, r, tokenSize);
            }

            // same guard as ReadArrayHeader; a map pair needs at least two bytes
            if (2L * (uint)count > buffer.BytesRemaining)
            {
                MessagePackSerializationException.ThrowImplausibleCollectionHeader("map", count, buffer.BytesRemaining);
            }
            return count;
        }

        /// <summary>
        /// Reads an array header and returns the element count, charging it against the declared-element budget of <paramref name="state"/>.
        /// Formatters read their headers through this overload: a count larger than the remaining data throws, and so does a
        /// count that, together with the counts every enclosing container has already declared, exceeds the bytes of the message.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadArrayHeader(ref DeserializeState state)
        {
            var count = buffer.ReadArrayHeader();
            state.ChargeDeclaredElements("array", count, (uint)count);
            return count;
        }

        /// <summary>
        /// Reads a map header and returns the entry count, charging two elements per entry against the declared-element budget of <paramref name="state"/>.
        /// Formatters read their headers through this overload: a count larger than the remaining data throws, and so does a
        /// count that, together with the counts every enclosing container has already declared, exceeds the bytes of the message.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadMapHeader(ref DeserializeState state)
        {
            var count = buffer.ReadMapHeader();
            state.ChargeDeclaredElements("map", count, 2L * (uint)count);
            return count;
        }

        /// <summary>Reads a str header and returns the payload byte count, which the caller then reads. A count larger than the remaining data throws.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadStringHeader()
        {
            var r = TryReadStringHeader(buffer.GetUnreadSpan(), out var byteCount, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
            }
            else
            {
                byteCount = ReadStringHeaderSlow(ref buffer, r, tokenSize);
            }

            // Allocation-bomb guard, exact for str/bin/ext (unlike array/map's lower bound): the payload itself must
            // occupy byteCount of the remaining bytes, so a larger claim is provably a lie,
            // reject it before the caller allocates from it.
            if ((uint)byteCount > (ulong)buffer.BytesRemaining)
            {
                MessagePackSerializationException.ThrowImplausiblePayloadHeader("str", byteCount, buffer.BytesRemaining);
            }
            return byteCount;
        }

        /// <summary>Reads a bin header and returns the payload byte count, which the caller then reads. A str header is also accepted, for data written in the old spec.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadBinHeader()
        {
            var r = TryReadBinHeader(buffer.GetUnreadSpan(), out var byteCount, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
            }
            else
            {
                byteCount = ReadBinHeaderSlow(ref buffer, r, tokenSize);
            }

            // same exact guard as ReadStringHeader
            if ((uint)byteCount > (ulong)buffer.BytesRemaining)
            {
                MessagePackSerializationException.ThrowImplausiblePayloadHeader("bin", byteCount, buffer.BytesRemaining);
            }
            return byteCount;
        }

        /// <summary>Reads a bin, header and payload, into a new array. A str-coded payload is also accepted, for data written in the old spec.</summary>
        public byte[] ReadBinary()
        {
            // Header first, then copy the payload straight into the result.
            // The header read already owns the straddle retry, the old-spec str fallback and the guard proving
            // byteCount <= BytesRemaining, so nothing is left for a payload slow path: CopyTo serves a straddling
            // payload out of the segments in one copy, where decoding the whole token from a window would stitch into
            // the pooled temp and then copy again. A multi-megabyte bin therefore never grows the stitch buffer.
            var byteCount = buffer.ReadBinHeader();
            if (byteCount == 0)
            {
                return Array.Empty<byte>(); // what ReadOnlySpan.ToArray() returned here before
            }

            var result = GC.AllocateUninitializedArray<byte>(byteCount); // CopyTo writes every byte
            buffer.CopyTo(result);
            buffer.Advance(byteCount);
            return result;
        }

        #endregion

        #region Ext, Timestamp

        // An ext lead byte alone does not identify the extension (fixext4 is also timestamp32),
        // so formatters claiming an ext code dispatch through this.
        /// <summary>
        /// Consumes the ext header and returns true when the next token is an ext of <paramref name="typeCode"/>; otherwise consumes nothing.
        /// The data bytes follow for the caller to read.
        /// </summary>
        public bool TryReadExtHeader(sbyte typeCode, out int dataLength)
        {
            var span = buffer.GetUnreadSpan();
            var r = MessagePackPrimitives.TryReadExtHeader(span, out var code, out dataLength, out var tokenSize);
            while (r == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(tokenSize, out span))
            {
                r = MessagePackPrimitives.TryReadExtHeader(span, out code, out dataLength, out tokenSize);
            }
            if (r != DecodeResult.Success || code != typeCode)
            {
                dataLength = 0;
                return false;
            }
            buffer.Advance(tokenSize);

            // same exact guard as ReadExtHeader: once the code matched, the token is ours,
            // and a length past the buffer is a malformed header rather than a miss
            if ((uint)dataLength > (ulong)buffer.BytesRemaining)
            {
                MessagePackSerializationException.ThrowImplausiblePayloadHeader("ext", dataLength, buffer.BytesRemaining);
            }
            return true;
        }

        /// <summary>Reads an ext header and returns its type code and data length. The data bytes follow for the caller to read.</summary>
        public (sbyte TypeCode, int DataLength) ReadExtHeader()
        {
            var r = MessagePackPrimitives.TryReadExtHeader(buffer.GetUnreadSpan(), out var typeCode, out var dataLength, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
            }
            else
            {
                (typeCode, dataLength) = ReadExtHeaderSlow(ref buffer, r, tokenSize);
            }

            // same exact guard as ReadStringHeader
            if ((uint)dataLength > (ulong)buffer.BytesRemaining)
            {
                MessagePackSerializationException.ThrowImplausiblePayloadHeader("ext", dataLength, buffer.BytesRemaining);
            }
            return (typeCode, dataLength);
        }

        /// <summary>Reads a msgpack timestamp (ext type -1) as a UTC DateTime.</summary>
        public DateTime ReadTimestamp()
        {
            var r = TryReadTimestamp(buffer.GetUnreadSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadTimestampSlow(ref buffer, r, tokenSize);
        }

        #endregion

        #region String

        /// <summary>Reads a str as a string, and nil as null.</summary>
        public string? ReadString()
        {
            var r = TryReadString(buffer.GetUnreadSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize); // value is already a materialized string
                return value;
            }
            return ReadStringSlow(ref buffer, r, tokenSize);
        }

        #endregion

        #region Skip

        // Iterative count-based walk, where each container adds its children to the outstanding count.
        // Container, str/bin and ext headers go through the existing stitch-aware readers,
        // whose allocation-bomb guards already reject a payload-length claim exceeding the remaining data (bin32pretending 2GB throws at the header),
        // so their payload advances are always in bounds; only the fixed-size token advances are validated here.

        /// <summary>Skips exactly one value including its entire subtree, without recursion, so nesting depth cannot exhaust the stack.</summary>
        public void Skip()
        {
            long remaining = 1;
            do
            {
                remaining--;
                var span = buffer.GetUnreadSpan();
                if (span.IsEmpty)
                {
                    ThrowTruncatedSkip(ref buffer);
                }
                var code = span[0];
                if (code <= MessagePackCode.MaxFixInt || code >= MessagePackCode.MinNegativeFixInt)
                {
                    buffer.Advance(1); // positive/negative fixint
                }
                else if (code < MessagePackCode.MinFixArray)
                {
                    remaining += 2L * (code & 0b0000_1111); // fixmap
                    buffer.Advance(1);
                }
                else if (code < MessagePackCode.MinFixStr)
                {
                    remaining += code & 0b0000_1111; // fixarray
                    buffer.Advance(1);
                }
                else if (code < MessagePackCode.Nil)
                {
                    SkipPayload(ref buffer, 1 + (code & 0b0001_1111)); // fixstr: header + inline length
                }
                else
                {
                    switch (code)
                    {
                        case MessagePackCode.Nil:
                        case MessagePackCode.False:
                        case MessagePackCode.True:
                            buffer.Advance(1);
                            break;
                        case MessagePackCode.UInt8:
                        case MessagePackCode.Int8:
                            SkipPayload(ref buffer, 2);
                            break;
                        case MessagePackCode.UInt16:
                        case MessagePackCode.Int16:
                            SkipPayload(ref buffer, 3);
                            break;
                        case MessagePackCode.UInt32:
                        case MessagePackCode.Int32:
                        case MessagePackCode.Float32:
                            SkipPayload(ref buffer, 5);
                            break;
                        case MessagePackCode.UInt64:
                        case MessagePackCode.Int64:
                        case MessagePackCode.Float64:
                            SkipPayload(ref buffer, 9);
                            break;
                        case MessagePackCode.Str8:
                        case MessagePackCode.Str16:
                        case MessagePackCode.Str32:
                            buffer.Advance(buffer.ReadStringHeader()); // header guard bounds the payload
                            break;
                        case MessagePackCode.Bin8:
                        case MessagePackCode.Bin16:
                        case MessagePackCode.Bin32:
                            buffer.Advance(buffer.ReadBinHeader());
                            break;
                        case MessagePackCode.Ext8:
                        case MessagePackCode.Ext16:
                        case MessagePackCode.Ext32:
                        case MessagePackCode.FixExt1:
                        case MessagePackCode.FixExt2:
                        case MessagePackCode.FixExt4:
                        case MessagePackCode.FixExt8:
                        case MessagePackCode.FixExt16:
                            buffer.Advance(buffer.ReadExtHeader().DataLength);
                            break;
                        case MessagePackCode.Array16:
                        case MessagePackCode.Array32:
                            remaining += (uint)buffer.ReadArrayHeader();
                            break;
                        case MessagePackCode.Map16:
                        case MessagePackCode.Map32:
                            remaining += 2L * (uint)buffer.ReadMapHeader();
                            break;
                        default: // 0xc1 (never used)
                            throw Unreadable("skip", span, DecodeResult.TokenMismatch);
                    }
                }
            } while (remaining > 0);
        }

        #endregion

        #region ReadRaw

        // Skip's capturing twin. The walk measures the value against a contiguous window first (escalating through
        // TryGetSpan when the value crosses a segment seam), then copies once and advances once,
        // so header widths and extension payloads survive byte-for-byte.
        // MessagePackUnknownMembers is captured this way.

        /// <summary>
        /// Reads exactly one value including its entire subtree and returns its bytes unchanged.
        /// </summary>
        public byte[] ReadRaw() => buffer.ReadRaw(rejectCircularReferences: false);

        // The MessagePackUnknownMembers capture: a back-reference (ext 97) inside a captured value points at an
        // instance defined elsewhere in the payload it came from. Replayed into another serialization it would resolve
        // against that serialization's ids, to a different instance, silently when the types happen to agree, so the
        // capture refuses it. (A definition envelope inside the packet is left alone: replayed, its id either stays
        // unique or the reader throws on the duplicate, never a silent rebinding.)
        internal byte[] ReadRaw(bool rejectCircularReferences)
        {
            var span = buffer.GetUnreadSpan();
            long length;
            while (!TryMeasureValue(span, buffer.BytesRemaining, rejectCircularReferences, out length))
            {
                if (span.Length >= buffer.BytesRemaining)
                {
                    throw Unreadable("raw value", span, DecodeResult.InsufficientBuffer);
                }
                // grow the window; TryMeasureValue proved every payload claim so far fits BytesRemaining,
                // so escalation never stitches toward a lie
                var hint = (int)Math.Min(int.MaxValue, Math.Min(buffer.BytesRemaining, Math.Max(2L * span.Length, 512L)));
                if (hint <= span.Length || !buffer.TryGetSpan(hint, out span))
                {
                    throw Unreadable("raw value", span, DecodeResult.InsufficientBuffer);
                }
            }
            if (length > int.MaxValue)
            {
                throw new MessagePackSerializationException($"A single {length}-byte value cannot be captured into a byte array.");
            }
            var raw = new byte[(int)length];
            buffer.CopyTo(raw);
            buffer.Advance((int)length);
            return raw;
        }

        #endregion
    }

    // fixed-size token: the whole token must exist even though we don't decode it
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void SkipPayload<TReadBuffer>(ref TReadBuffer buffer, int tokenSize)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (tokenSize > buffer.BytesRemaining)
        {
            ThrowTruncatedSkip(ref buffer);
        }
        buffer.Advance(tokenSize);
    }

    static Exception ThrowTruncatedSkip<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        throw Unreadable("skip", buffer.GetUnreadSpan(), DecodeResult.InsufficientBuffer);
    }

    // ---- ReadXOrNil: nil-or-value through ONE window on the buffer -----------------------------------------------
    // The decoding behind the ReadNullableX readers. Each returns true with the value or false for nil, consumed
    // either way, and is a Read: anything else throws as ReadX does.
    //
    // `TryReadNil() ? null : ReadX()` opens the window twice and has ReadX report through (value, tokenSize,
    // DecodeResult), which the inlined caller rebuilds and tests again: 86 instructions for one uint16-coded int?
    // member. Here the code byte is loaded once and each case the wire is expected to carry is a branch of its own
    // that advances and returns: the fixint, the nil, then the type's natural tier under the same window gate its
    // TryReadX uses (so a short token near the end of the data, or in a small segment, stays on the fast path exactly
    // as it does there). Nothing is passed through out parameters to a call.
    //
    // Whatever a helper does not take (a wider encoding than the natural tier, a value out of range, a wrong token,
    // a window shorter than the gate) goes to ReadX itself, out of line, with nothing consumed, so those cases
    // behave exactly as they always did. That call never has a nil to find: a nil is one byte, the current span is
    // non-empty whenever bytes remain, so a helper has already taken it; with no bytes left ReadX reports the end of
    // the data as the pair would.
    //
    // Measured against the pair (sandbox NullableReadBenchmark / NullableReaderPairBenchmark, a dense loop of nullable
    // members): 14 to 27 instructions fewer per member, 16-25% of the loop; time 0.70 on fixint int32, 0.85-0.93
    // elsewhere, parity on random byte? where mispredicts dominate. On the Answer graph (61 int? / bool? members):
    // -7.5% instructions, about -5% time.

    // int32: fixint, nil, cc..d3 through Int32ReadTable under the 5-byte gate (the nine-byte formats go to ReadInt32)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool ReadInt32OrNil<TReadBuffer>(ref TReadBuffer buffer, out int value)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            uint code = s;
            if ((byte)(code + 32) <= 159) // fixint; the (byte) wrap is load-bearing, see TryReadInt32
            {
                buffer.Advance(1);
                value = unchecked((sbyte)code);
                return true;
            }
            if (code == MessagePackCode.Nil)
            {
                buffer.Advance(1);
                value = 0;
                return false;
            }
            uint format = code - MessagePackCode.UInt8;
            if (format <= 7 && span.Length >= 5) // the unconditional 4-byte payload load stays inside the window
            {
                uint entry = Unsafe.Add(ref MemoryMarshal.GetReference(Int32ReadTable), (int)format);
                int length = (int)(entry & 0xf);
                if (length != 9)
                {
                    uint payload = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
                    uint selected = payload >> (int)((entry >> 4) & 0x1f);
                    int extend = (int)(entry >> 10);
                    long wide = ((long)selected << extend) >> extend;
                    if (wide == (int)wide) // a uint32 above int.MaxValue does not fit: ReadInt32 reports it
                    {
                        buffer.Advance(length);
                        value = (int)wide;
                        return true;
                    }
                }
            }
        }
        value = ReadInt32OutOfLine(ref buffer);
        return true;
    }

    // uint32: the int32 tier with the unsigned range. Only the positive fixint is taken up front; a negative one is
    // not cc..d3 and reaches ReadUInt32, which rejects it.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool ReadUInt32OrNil<TReadBuffer>(ref TReadBuffer buffer, out uint value)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            uint code = s;
            if (code <= MessagePackCode.MaxFixInt)
            {
                buffer.Advance(1);
                value = code;
                return true;
            }
            if (code == MessagePackCode.Nil)
            {
                buffer.Advance(1);
                value = 0;
                return false;
            }
            uint format = code - MessagePackCode.UInt8;
            if (format <= 7 && span.Length >= 5)
            {
                uint entry = Unsafe.Add(ref MemoryMarshal.GetReference(Int32ReadTable), (int)format);
                int length = (int)(entry & 0xf);
                if (length != 9)
                {
                    uint payload = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
                    uint selected = payload >> (int)((entry >> 4) & 0x1f);
                    int extend = (int)(entry >> 10);
                    long wide = ((long)selected << extend) >> extend;
                    if ((ulong)wide <= uint.MaxValue) // a negative int8 / int16 / int32 does not fit
                    {
                        buffer.Advance(length);
                        value = (uint)wide;
                        return true;
                    }
                }
            }
        }
        value = ReadUInt32OutOfLine(ref buffer);
        return true;
    }

    // int64: fixint, nil, cc..d3 through Int64ReadTable under the 9-byte gate. A uint64 above long.MaxValue does not
    // fit (the entry's flag marks cf: it has to decode non-negative).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool ReadInt64OrNil<TReadBuffer>(ref TReadBuffer buffer, out long value)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            uint code = s;
            if ((byte)(code + 32) <= 159) // fixint
            {
                buffer.Advance(1);
                value = unchecked((sbyte)code);
                return true;
            }
            if (code == MessagePackCode.Nil)
            {
                buffer.Advance(1);
                value = 0;
                return false;
            }
            uint format = code - MessagePackCode.UInt8;
            if (format <= 7 && span.Length >= 9) // the unconditional 8-byte payload load stays inside the window
            {
                uint entry = Unsafe.Add(ref MemoryMarshal.GetReference(Int64ReadTable), (int)format);
                ulong payload = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s, 1)));
                ulong selected = payload >> (int)((entry >> 4) & 0x3f);
                int extend = (int)((entry >> 10) & 0x3f);
                long wide = ((long)selected << extend) >> extend;
                if (((entry >> 16) == 0) | (wide >= 0))
                {
                    buffer.Advance((int)(entry & 0xf));
                    value = wide;
                    return true;
                }
            }
        }
        value = ReadInt64OutOfLine(ref buffer);
        return true;
    }

    // uint64: the int64 tier with the sign predicate flipped, as in TryReadUInt64 (cf is always valid, every other
    // format has to decode non-negative)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool ReadUInt64OrNil<TReadBuffer>(ref TReadBuffer buffer, out ulong value)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            uint code = s;
            if (code <= MessagePackCode.MaxFixInt)
            {
                buffer.Advance(1);
                value = code;
                return true;
            }
            if (code == MessagePackCode.Nil)
            {
                buffer.Advance(1);
                value = 0;
                return false;
            }
            uint format = code - MessagePackCode.UInt8;
            if (format <= 7 && span.Length >= 9)
            {
                uint entry = Unsafe.Add(ref MemoryMarshal.GetReference(Int64ReadTable), (int)format);
                ulong payload = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s, 1)));
                ulong selected = payload >> (int)((entry >> 4) & 0x3f);
                int extend = (int)((entry >> 10) & 0x3f);
                long wide = ((long)selected << extend) >> extend;
                if (((entry >> 16) != 0) | (wide >= 0))
                {
                    buffer.Advance((int)(entry & 0xf));
                    value = unchecked((ulong)wide);
                    return true;
                }
            }
        }
        value = ReadUInt64OutOfLine(ref buffer);
        return true;
    }

    // int16 / uint16 / char: the 16-bit natural tier of TryReadNarrow16Core (cc / cd / d0 / d1 through
    // Narrow16ReadTable under the 3-byte gate; a zero entry is a wide encoding, decided by ReadX). min and max are
    // constants at every call site and fold: min == 0 selects the positive fixint and the unsigned reader.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool ReadNarrow16OrNil<TReadBuffer>(ref TReadBuffer buffer, long min, long max, out int value)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            uint code = s;
            if (min == 0 ? code <= MessagePackCode.MaxFixInt : (byte)(code + 32) <= 159)
            {
                buffer.Advance(1);
                value = unchecked((sbyte)code);
                return true;
            }
            if (code == MessagePackCode.Nil)
            {
                buffer.Advance(1);
                value = 0;
                return false;
            }
            uint format = code - MessagePackCode.UInt8;
            if (format <= 7 && span.Length >= 3) // the unconditional 2-byte payload load stays inside the window
            {
                uint entry = Unsafe.Add(ref MemoryMarshal.GetReference(Narrow16ReadTable), (int)format);
                if (entry != 0)
                {
                    uint payload = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref s, 1)));
                    uint selected = payload >> (int)((entry >> 4) & 0x1f);
                    int extend = (int)(entry >> 10);
                    long wide = ((long)selected << extend) >> extend;
                    if ((ulong)(wide - min) <= (ulong)(max - min))
                    {
                        buffer.Advance((int)(entry & 0xf));
                        value = (int)wide;
                        return true;
                    }
                }
            }
        }
        value = min == 0 ? ReadUInt16OutOfLine(ref buffer) : ReadInt16OutOfLine(ref buffer);
        return true;
    }

    // byte: its natural domain is the positive fixint and uint8, as in TryReadByte
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool ReadByteOrNil<TReadBuffer>(ref TReadBuffer buffer, out byte value)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            uint code = s;
            if (code <= MessagePackCode.MaxFixInt)
            {
                buffer.Advance(1);
                value = (byte)code;
                return true;
            }
            if (code == MessagePackCode.Nil)
            {
                buffer.Advance(1);
                value = 0;
                return false;
            }
            if (code == MessagePackCode.UInt8 && span.Length >= 2)
            {
                value = Unsafe.Add(ref s, 1);
                buffer.Advance(2);
                return true;
            }
        }
        value = ReadByteOutOfLine(ref buffer);
        return true;
    }

    // sbyte: its natural domain is the fixint and int8, as in TryReadSByte
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool ReadSByteOrNil<TReadBuffer>(ref TReadBuffer buffer, out sbyte value)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            uint code = s;
            if ((byte)(code + 32) <= 159) // fixint
            {
                buffer.Advance(1);
                value = unchecked((sbyte)code);
                return true;
            }
            if (code == MessagePackCode.Nil)
            {
                buffer.Advance(1);
                value = 0;
                return false;
            }
            if (code == MessagePackCode.Int8 && span.Length >= 2)
            {
                value = unchecked((sbyte)Unsafe.Add(ref s, 1));
                buffer.Advance(2);
                return true;
            }
        }
        value = ReadSByteOutOfLine(ref buffer);
        return true;
    }

    // boolean: the value comes from the biased compare without a branch on it, as in TryReadBoolean
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool ReadBooleanOrNil<TReadBuffer>(ref TReadBuffer buffer, out bool value)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty)
        {
            uint code = span[0];
            uint d = code - MessagePackCode.False;
            if (d <= 1)
            {
                buffer.Advance(1);
                value = d != 0;
                return true;
            }
            if (code == MessagePackCode.Nil)
            {
                buffer.Advance(1);
                value = false;
                return false;
            }
        }
        value = ReadBooleanOutOfLine(ref buffer);
        return true;
    }

    // float32: ca is the only code a float-writing producer emits (TryReadSingle's bet); the conversions from a
    // float64 or an int format are ReadSingle's
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool ReadSingleOrNil<TReadBuffer>(ref TReadBuffer buffer, out float value)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            uint code = s;
            if (code == MessagePackCode.Float32 && span.Length >= 5)
            {
                value = BitConverter.UInt32BitsToSingle(MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1))));
                buffer.Advance(5);
                return true;
            }
            if (code == MessagePackCode.Nil)
            {
                buffer.Advance(1);
                value = 0;
                return false;
            }
        }
        value = ReadSingleOutOfLine(ref buffer);
        return true;
    }

    // float64: cb, the same bet as ReadSingleOrNil with the gate at 9
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool ReadDoubleOrNil<TReadBuffer>(ref TReadBuffer buffer, out double value)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var span = buffer.GetUnreadSpan();
        if (!span.IsEmpty)
        {
            ref byte s = ref MemoryMarshal.GetReference(span);
            uint code = s;
            if (code == MessagePackCode.Float64 && span.Length >= 9)
            {
                value = BitConverter.UInt64BitsToDouble(MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s, 1))));
                buffer.Advance(9);
                return true;
            }
            if (code == MessagePackCode.Nil)
            {
                buffer.Advance(1);
                value = 0;
                return false;
            }
        }
        value = ReadDoubleOutOfLine(ref buffer);
        return true;
    }

    // ReadX kept out of line for the ReadXOrNil helpers: one call instead of its whole body inlined a second time
    // into every member read. The result comes back in a register (an out parameter would pin the caller's local to
    // the stack on the hot path as well).

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadInt32OutOfLine<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return buffer.ReadInt32();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint ReadUInt32OutOfLine<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return buffer.ReadUInt32();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ReadInt64OutOfLine<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return buffer.ReadInt64();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong ReadUInt64OutOfLine<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return buffer.ReadUInt64();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short ReadInt16OutOfLine<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return buffer.ReadInt16();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ushort ReadUInt16OutOfLine<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return buffer.ReadUInt16();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte ReadByteOutOfLine<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return buffer.ReadByte();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte ReadSByteOutOfLine<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return buffer.ReadSByte();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ReadBooleanOutOfLine<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return buffer.ReadBoolean();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float ReadSingleOutOfLine<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return buffer.ReadSingle();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double ReadDoubleOutOfLine<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return buffer.ReadDouble();
    }

    // Per-target slow retry loops over the dedicated narrow primitives (range failures arrive as TokenMismatch from the
    // primitive layer, so no target-level range logic lives here anymore).

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint ReadUInt32Slow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadUInt32(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("uint32", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong ReadUInt64Slow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadUInt64(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("uint64", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte ReadByteSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadByte(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("byte", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte ReadSByteSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadSByte(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("sbyte", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short ReadInt16Slow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadInt16(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("int16", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ushort ReadUInt16Slow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadUInt16(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("uint16", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadInt32Slow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadInt32(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("int32", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ReadInt64Slow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadInt64(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("int64", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float ReadSingleSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadSingle(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("float32", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double ReadDoubleSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadDouble(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("float64", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadArrayHeaderSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadArrayHeader(window, out var count, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return count;
            }
            first = r;
        }
        throw Unreadable("array header", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadMapHeaderSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadMapHeader(window, out var count, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return count;
            }
            first = r;
        }
        throw Unreadable("map header", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadStringHeaderSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadStringHeader(window, out var byteCount, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return byteCount;
            }
            first = r;
        }
        throw Unreadable("str header", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadBinHeaderSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadBinHeader(window, out var byteCount, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return byteCount;
            }
            first = r;
        }
        if (first == DecodeResult.TokenMismatch)
        {
            // old-spec (pre-2013) msgpack encodes binary with raw (= today's str) headers;
            // reads accept both specs like v3, so retry the token as a str header
            first = TryReadStringHeader(buffer.GetUnreadSpan(), out var byteCount, out required);
            if (first == DecodeResult.Success)
            {
                buffer.Advance(required);
                return byteCount;
            }
            while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
            {
                var r = TryReadStringHeader(window, out byteCount, out required);
                if (r == DecodeResult.Success)
                {
                    buffer.Advance(required);
                    return byteCount;
                }
                first = r;
            }
        }
        throw Unreadable("bin header", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (sbyte TypeCode, int DataLength) ReadExtHeaderSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = MessagePackPrimitives.TryReadExtHeader(window, out var typeCode, out var dataLength, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return (typeCode, dataLength);
            }
            first = r;
        }
        throw Unreadable("ext header", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static DateTime ReadTimestampSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadTimestamp(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("timestamp", buffer.GetUnreadSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string? ReadStringSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadString(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required); // value is already a materialized string
                return value;
            }
            first = r;
        }
        throw Unreadable("string", buffer.GetUnreadSpan(), first);
    }

    // Skip's structural walk, consuming nothing: token sizes and payload claims accumulate into the value's total length.
    // False = the window ended mid-value (the caller escalates the window);
    // a payload claim that cannot fit the buffer's remaining bytes throws here,
    // so window escalation only ever chases real data.
    static bool TryMeasureValue(ReadOnlySpan<byte> window, long bytesRemaining, bool rejectCircularReferences, out long length)
    {
        long offset = 0;
        long remaining = 1;
        do
        {
            var result = TryReadToken(window.Slice((int)offset), out var payloadLength, out var childValueCount, out var tokenSize);
            if (result == DecodeResult.TokenMismatch)
            {
                throw Unreadable("raw value", window.Slice((int)offset), result);
            }
            if (result == DecodeResult.InsufficientBuffer)
            {
                length = 0;
                return false;
            }
            if (rejectCircularReferences && IsCircularReferenceToken(window.Slice((int)offset, tokenSize)))
            {
                MessagePackSerializationException.ThrowCircularReferenceInCapturedValue();
            }
            if (payloadLength > bytesRemaining - offset - tokenSize)
            {
                MessagePackSerializationException.ThrowImplausiblePayloadHeader("captured value", (int)payloadLength, bytesRemaining - offset - tokenSize);
            }
            offset += tokenSize + payloadLength;
            remaining += childValueCount - 1;
            if (offset > window.Length)
            {
                length = 0;
                return false;
            }
        } while (remaining > 0);
        length = offset;
        return true;
    }

    // the token (code byte, length fields, type byte; TryReadToken proved it complete) of a back-reference: an ext of
    // this library's circular-reference type code, in any of the ext header widths
    static bool IsCircularReferenceToken(ReadOnlySpan<byte> token)
    {
        var code = token[0];
        var isExt = (code >= MessagePackCode.FixExt1 && code <= MessagePackCode.FixExt16) || (code >= MessagePackCode.Ext8 && code <= MessagePackCode.Ext32);
        return isExt && token[token.Length - 1] == unchecked((byte)ThisLibraryExtensionTypeCodes.CircularReference);
    }

    // exception factory so the throw statement stays in the (cold) caller and the JIT sees it as unreachable-hot;
    // the DecodeResult picks the message. Success only arrives here from the narrow-target readers,
    // where it means "decoded fine but out of target range"
    [MethodImpl(MethodImplOptions.NoInlining)]
    static MessagePackSerializationException Unreadable(string target, ReadOnlySpan<byte> source, DecodeResult result)
    {
        byte code = source.IsEmpty ? (byte)0 : source[0];
        return result switch
        {
            DecodeResult.Success => new MessagePackSerializationException($"MessagePack value (code 0x{code:x2}) does not fit in {target}."),
            DecodeResult.InsufficientBuffer => new MessagePackSerializationException($"Truncated MessagePack data reading {target} (code 0x{code:x2})."),
            _ => new MessagePackSerializationException($"Unexpected or out-of-range MessagePack value (code 0x{code:x2}) reading {target}."),
        };
    }
}
