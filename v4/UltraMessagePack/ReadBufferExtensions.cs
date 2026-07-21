using SerializerFoundation;
using System.Runtime.CompilerServices;
using static UltraMessagePack.MessagePackPrimitives;

namespace UltraMessagePack;

// Read-side mirror of WriteBufferExtensions: throwing readers layered over the span-based
// TryRead primitives. Fast path decodes straight from the buffer's current span (for
// contiguous buffers that is everything remaining). The NoInlining slow path is driven by
// the DecodeResult category: on InsufficientBuffer it requests EXACTLY the tokenSize the
// primitive reported (GetSpan(tokenSize) — the ReadOnlySequenceReadBuffer straddle case
// stitches precisely that much) and retries; tokenSize strictly exceeds the window it was
// reported for and is bounded by BytesRemaining checks, so the loop terminates (str/bin
// take two hops: header requirement first, then header + payload). TokenMismatch throws
// immediately.
//
// Materialization order matters: extract the value (ToArray/GetString) BEFORE Advance,
// because Advance may return a stitched temp buffer to the pool and invalidate the span.
public static class ReadBufferExtensions
{
    extension<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        #region Int32, 64 / UInt32, 64

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            var r = TryReadInt32(buffer.GetSpan(), out var value, out var tokenSize);
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
            var r = TryReadInt64(buffer.GetSpan(), out var value, out var tokenSize);
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
            var r = TryReadUInt32(buffer.GetSpan(), out var value, out var tokenSize);
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
            var r = TryReadUInt64(buffer.GetSpan(), out var value, out var tokenSize);
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
            var r = TryReadByte(buffer.GetSpan(), out var value, out var tokenSize);
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
            var r = TryReadSByte(buffer.GetSpan(), out var value, out var tokenSize);
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
            var r = TryReadInt16(buffer.GetSpan(), out var value, out var tokenSize);
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
            var r = TryReadUInt16(buffer.GetSpan(), out var value, out var tokenSize);
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

        /// <summary>Consumes 1 byte and returns true iff the next value is nil; otherwise consumes nothing.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadNil()
        {
            if (MessagePackPrimitives.TryReadNil(buffer.GetSpan()))
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
            var r = TryReadBoolean(buffer.GetSpan(), out var value, out _);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(1);
                return value;
            }
            throw Unreadable("boolean", buffer.GetSpan(), r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            var r = TryReadSingle(buffer.GetSpan(), out var value, out var tokenSize);
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
            var r = TryReadDouble(buffer.GetSpan(), out var value, out var tokenSize);
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
            var r = TryReadArrayHeader(buffer.GetSpan(), out var count, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return count;
            }
            return ReadArrayHeaderSlow(ref buffer, r, tokenSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadMapHeader()
        {
            var r = TryReadMapHeader(buffer.GetSpan(), out var count, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return count;
            }
            return ReadMapHeaderSlow(ref buffer, r, tokenSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadStringHeader()
        {
            var r = TryReadStringHeader(buffer.GetSpan(), out var byteCount, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return byteCount;
            }
            return ReadStringHeaderSlow(ref buffer, r, tokenSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadBinHeader()
        {
            var r = TryReadBinHeader(buffer.GetSpan(), out var byteCount, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return byteCount;
            }
            return ReadBinHeaderSlow(ref buffer, r, tokenSize);
        }

        /// <summary>Reads a bin (header + payload) as a new array.</summary>
        public byte[] ReadBinary()
        {
            var r = TryReadBinary(buffer.GetSpan(), out var value, out var tokenSize);
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
            var r = TryReadExtHeader(buffer.GetSpan(), out var typeCode, out var dataLength, out var tokenSize);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(tokenSize);
                return (typeCode, dataLength);
            }
            return ReadExtHeaderSlow(ref buffer, r, tokenSize);
        }

        public DateTime ReadTimestamp()
        {
            var r = TryReadTimestamp(buffer.GetSpan(), out var value, out var tokenSize);
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
            var r = TryReadString(buffer.GetSpan(), out var value, out var tokenSize);
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
        /// str/bin and ext headers go through the existing stitch-aware readers; payload
        /// and fixed-size token advances are validated against BytesRemaining first, so a
        /// lying length claim (bin32 pretending 2GB) throws instead of silently skipping
        /// past the end.
        /// </summary>
        public void Skip()
        {
            long remaining = 1;
            do
            {
                remaining--;
                var span = buffer.GetSpan();
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
                            SkipPayloadChecked(ref buffer, buffer.ReadStringHeader());
                            break;
                        case MessagePackCode.Bin8:
                        case MessagePackCode.Bin16:
                        case MessagePackCode.Bin32:
                            SkipPayloadChecked(ref buffer, buffer.ReadBinHeader());
                            break;
                        case MessagePackCode.Ext8:
                        case MessagePackCode.Ext16:
                        case MessagePackCode.Ext32:
                        case MessagePackCode.FixExt1:
                        case MessagePackCode.FixExt2:
                        case MessagePackCode.FixExt4:
                        case MessagePackCode.FixExt8:
                        case MessagePackCode.FixExt16:
                            SkipPayloadChecked(ref buffer, buffer.ReadExtHeader().DataLength);
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
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        if (tokenSize > buffer.BytesRemaining)
        {
            throw Unreadable("skip", buffer.GetSpan(), DecodeResult.InsufficientBuffer);
        }
        buffer.Advance(tokenSize);
    }

    // header already consumed by a stitch-aware reader; validate and skip the payload
    static void SkipPayloadChecked<TReadBuffer>(ref TReadBuffer buffer, int byteCount)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        if (byteCount > buffer.BytesRemaining)
        {
            throw Unreadable("skip", buffer.GetSpan(), DecodeResult.InsufficientBuffer);
        }
        buffer.Advance(byteCount);
    }

    // Per-target slow retry loops over the dedicated narrow primitives (range failures
    // arrive as TokenMismatch from the primitive layer, so no target-level range logic
    // lives here anymore).

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint ReadUInt32Slow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadUInt32(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("uint32", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong ReadUInt64Slow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadUInt64(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("uint64", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte ReadByteSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadByte(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("byte", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static sbyte ReadSByteSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadSByte(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("sbyte", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short ReadInt16Slow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadInt16(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("int16", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ushort ReadUInt16Slow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadUInt16(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("uint16", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadInt32Slow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadInt32(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("int32", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ReadInt64Slow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadInt64(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("int64", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float ReadSingleSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadSingle(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("float32", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double ReadDoubleSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadDouble(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("float64", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadArrayHeaderSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadArrayHeader(buffer.GetSpan(required), out var count, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return count;
            }
            first = r;
        }
        throw Unreadable("array header", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadMapHeaderSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadMapHeader(buffer.GetSpan(required), out var count, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return count;
            }
            first = r;
        }
        throw Unreadable("map header", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadStringHeaderSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadStringHeader(buffer.GetSpan(required), out var byteCount, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return byteCount;
            }
            first = r;
        }
        throw Unreadable("str header", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadBinHeaderSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadBinHeader(buffer.GetSpan(required), out var byteCount, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return byteCount;
            }
            first = r;
        }
        throw Unreadable("bin header", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (sbyte TypeCode, int DataLength) ReadExtHeaderSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadExtHeader(buffer.GetSpan(required), out var typeCode, out var dataLength, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return (typeCode, dataLength);
            }
            first = r;
        }
        throw Unreadable("ext header", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static DateTime ReadTimestampSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadTimestamp(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required);
                return value;
            }
            first = r;
        }
        throw Unreadable("timestamp", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string? ReadStringSlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadString(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                buffer.Advance(required); // value is already a materialized string
                return value;
            }
            first = r;
        }
        throw Unreadable("string", buffer.GetSpan(), first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte[] ReadBinarySlow<TReadBuffer>(ref TReadBuffer buffer, DecodeResult first, int required)
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        while (first == DecodeResult.InsufficientBuffer && required <= buffer.BytesRemaining)
        {
            var r = TryReadBinary(buffer.GetSpan(required), out var value, out required);
            if (r == DecodeResult.Success)
            {
                var result = value.ToArray(); // before Advance: the span may alias a pooled stitch buffer
                buffer.Advance(required);
                return result;
            }
            first = r;
        }
        throw Unreadable("binary", buffer.GetSpan(), first);
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
