// TODO: There is room for optimization in String and Bin(get ReadOnlySequence)

using SerializerFoundation;
using System.Runtime.CompilerServices;
using static UltraMessagePack.MessagePackPrimitives;

namespace UltraMessagePack;

// Read-side mirror of WriteBufferExtensions: throwing readers layered over the span-based
// TryRead primitives. Fast path decodes straight from the buffer's current span (for
// contiguous buffers that is everything remaining). The NoInlining slow path is driven by
// the DecodeResult category: on InsufficientBuffer it demands EXACTLY the tokenSize the
// primitive reported via TryGetSpan (the ReadOnlySequenceReadBuffer straddle case
// stitches precisely that much) and retries; TryGetSpan returning false means genuine
// truncation and exits the loop into the domain exception — the foundation never throws
// for it. tokenSize strictly exceeds the window it was reported for, so the loop
// terminates (str/bin take two hops: header requirement first, then header + payload).
// TokenMismatch throws immediately.
//
// Materialization order matters: extract the value (ToArray/GetString) BEFORE Advance,
// because Advance may return a stitched temp buffer to the pool and invalidate the span.
public static class ReadBufferExtensions
{
    extension<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        #region Int32, 64 / UInt32, 64

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            var r = TryReadInt32(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadInt32Slow(ref buffer, r, tokenSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            var r = TryReadInt64(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadInt64Slow(ref buffer, r, tokenSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            var r = TryReadUInt32(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadUInt32Slow(ref buffer, r, tokenSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            var r = TryReadUInt64(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadUInt64Slow(ref buffer, r, tokenSize);
        }

        #endregion

        #region Int8, 16

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            var r = TryReadByte(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadByteSlow(ref buffer, r, tokenSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte()
        {
            var r = TryReadSByte(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadSByteSlow(ref buffer, r, tokenSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            var r = TryReadInt16(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadInt16Slow(ref buffer, r, tokenSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            var r = TryReadUInt16(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadUInt16Slow(ref buffer, r, tokenSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char ReadChar() => (char)ReadUInt16(ref buffer);

        #endregion

        #region Nil, Boolean, Single, Double

        /// <summary>Reads the next code byte without consuming anything; false iff no bytes
        /// remain. Relies on the buffer guarantee that the current span is non-empty whenever
        /// bytes remain (the same single-byte guarantee <see cref="TryReadNil"/> builds on),
        /// so a segment boundary can never produce a false negative.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out byte code)
        {
            var span = buffer.GetCurrentSpan();
            if (span.Length > 0)
            {
                code = span[0];
                return true;
            }
            code = 0;
            return false;
        }

        /// <summary>Consumes 1 byte and returns true iff the next value is nil; otherwise consumes nothing.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadNil()
        {
            if (MessagePackPrimitives.TryReadNil(buffer.GetCurrentSpan()))
            {
                buffer.Advance(1); // nil is always exactly 1 byte
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
        {
            // single-byte format: never straddles a segment, no stitched retry needed
            var r = TryReadBoolean(buffer.GetCurrentSpan(), out var value, out _);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(1);
                return value;
            }
            throw Unreadable("boolean", buffer.GetCurrentSpan(), r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            var r = TryReadSingle(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadSingleSlow(ref buffer, r, tokenSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            var r = TryReadDouble(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadDoubleSlow(ref buffer, r, tokenSize);
        }

        #endregion

        #region headers(array, map, string, bin), binary

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadArrayHeader()
        {
            var r = TryReadArrayHeader(buffer.GetCurrentSpan(), out var count, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
            }
            else
            {
                count = ReadArrayHeaderSlow(ref buffer, r, tokenSize);
            }

            // Allocation-bomb guard: formatters preallocate from this count, and every
            // msgpack element occupies at least one byte, so a count exceeding the
            // remaining payload is provably a lie — reject it BEFORE anyone allocates.
            if ((uint)count > (ulong)buffer.BytesRemaining)
            {
                MessagePackSerializationException.ThrowImplausibleCollectionHeader("array", count, buffer.BytesRemaining);
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadMapHeader()
        {
            var r = TryReadMapHeader(buffer.GetCurrentSpan(), out var count, out var tokenSize);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadStringHeader()
        {
            var r = TryReadStringHeader(buffer.GetCurrentSpan(), out var byteCount, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
            }
            else
            {
                byteCount = ReadStringHeaderSlow(ref buffer, r, tokenSize);
            }

            // Allocation-bomb guard, EXACT for str/bin/ext (unlike array/map's lower
            // bound): the payload itself must occupy byteCount of the remaining bytes,
            // so a larger claim is provably a lie — reject it BEFORE the caller
            // allocates from it.
            if ((uint)byteCount > (ulong)buffer.BytesRemaining)
            {
                MessagePackSerializationException.ThrowImplausiblePayloadHeader("str", byteCount, buffer.BytesRemaining);
            }
            return byteCount;
        }

        /// <summary>Reads a bin header; str headers are also accepted (old-spec raw compatibility).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadBinHeader()
        {
            var r = TryReadBinHeader(buffer.GetCurrentSpan(), out var byteCount, out var tokenSize);
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

        /// <summary>Reads a bin (header + payload) as a new array; str-coded payloads are also accepted (old-spec raw compatibility, as in v3).</summary>
        public byte[] ReadBinary()
        {
            var r = TryReadBinary(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                var result = value.ToArray(); // before Advance: the span may alias a pooled stitch buffer
                buffer.Advance(tokenSize);
                return result;
            }
            return ReadBinarySlow(ref buffer, r, tokenSize);
        }

        #endregion

        #region Ext, Timestamp

        public (sbyte TypeCode, int DataLength) ReadExtHeader()
        {
            var r = TryReadExtHeader(buffer.GetCurrentSpan(), out var typeCode, out var dataLength, out var tokenSize);
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

        public DateTime ReadTimestamp()
        {
            var r = TryReadTimestamp(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return value;
            }
            return ReadTimestampSlow(ref buffer, r, tokenSize);
        }

        #endregion

        #region String

        /// <summary>Reads a str as a string; nil reads as null (mirror of WriteString(string?)).</summary>
        public string? ReadString()
        {
            var r = TryReadString(buffer.GetCurrentSpan(), out var value, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize); // value is already a materialized string
                return value;
            }
            return ReadStringSlow(ref buffer, r, tokenSize);
        }

        #endregion

        #region Skip

        /// <summary>
        /// Skips exactly one msgpack value including its entire subtree. Iterative
        /// count-based walk (no recursion, so adversarial nesting depth cannot blow the
        /// stack): each container adds its children to the outstanding count. Container,
        /// str/bin and ext headers go through the existing stitch-aware readers, whose
        /// allocation-bomb guards already reject a payload-length claim exceeding the
        /// remaining data (bin32 pretending 2GB throws at the header), so their payload
        /// advances are always in bounds; only the fixed-size token advances are
        /// validated here.
        /// </summary>
        public void Skip()
        {
            long remaining = 1;
            do
            {
                remaining--;
                var span = buffer.GetCurrentSpan();
                if (span.IsEmpty)
                {
                    throw Unreadable("skip", span, DecodeResult.InsufficientBuffer);
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
    }

    // fixed-size token: the whole token must exist even though we don't decode it
    static void SkipPayload<TReadBuffer>(ref TReadBuffer buffer, int tokenSize)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (tokenSize > buffer.BytesRemaining)
        {
            throw Unreadable("skip", buffer.GetCurrentSpan(), DecodeResult.InsufficientBuffer);
        }
        buffer.Advance(tokenSize);
    }

    // Per-target slow retry loops over the dedicated narrow primitives (range failures
    // arrive as TokenMismatch from the primitive layer, so no target-level range logic
    // lives here anymore).

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
        throw Unreadable("uint32", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("uint64", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("byte", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("sbyte", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("int16", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("uint16", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("int32", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("int64", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("float32", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("float64", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("array header", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("map header", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("str header", buffer.GetCurrentSpan(), first);
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
            first = TryReadStringHeader(buffer.GetCurrentSpan(), out var byteCount, out required);
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
        throw Unreadable("bin header", buffer.GetCurrentSpan(), first);
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
            var r = TryReadExtHeader(window, out var typeCode, out var dataLength, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return (typeCode, dataLength);
            }
            first = r;
        }
        throw Unreadable("ext header", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("timestamp", buffer.GetCurrentSpan(), first);
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
        throw Unreadable("string", buffer.GetCurrentSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte[] ReadBinarySlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
        {
            var r = TryReadBinary(window, out var value, out required);
            if (r == DecodeResult.Success)
            {
                var result = value.ToArray(); // before Advance: the span may alias a pooled stitch buffer
                buffer.Advance(required);
                return result;
            }
            first = r;
        }
        if (first == DecodeResult.TokenMismatch)
        {
            // old-spec (pre-2013) msgpack encodes binary with raw (= today's str) headers;
            // reads accept both specs like v3, so retry the token as a str payload
            first = TryReadStringSpan(buffer.GetCurrentSpan(), out var strValue, out required);
            if (first == DecodeResult.Success)
            {
                var result = strValue.ToArray(); // before Advance: the span may alias a pooled stitch buffer
                buffer.Advance(required);
                return result;
            }
            while (first == DecodeResult.InsufficientBuffer && buffer.TryGetSpan(required, out var window))
            {
                var r = TryReadStringSpan(window, out strValue, out required);
                if (r == DecodeResult.Success)
                {
                    var result = strValue.ToArray(); // before Advance: the span may alias a pooled stitch buffer
                    buffer.Advance(required);
                    return result;
                }
                first = r;
            }
        }
        throw Unreadable("binary", buffer.GetCurrentSpan(), first);
    }

    // exception factory so the throw statement stays in the (cold) caller and the JIT sees
    // it as unreachable-hot; the DecodeResult picks the message. Success only arrives here
    // from the narrow-target readers, where it means "decoded fine but out of target range"
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
