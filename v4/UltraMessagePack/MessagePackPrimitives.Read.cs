using System.Buffers.Binary;
using System.Text;
using System.Text.Unicode;

namespace UltraMessagePack;

// Three-category decode contract (MessagePack-CSharp v3 minus its redundant members;
// measured zero-cost vs the old bool in TryReadInt32Benchmark — Success == 0 keeps the
// caller's check a single test). The only category that changes control flow is
// InsufficientBuffer (sequence readers stitch and retry, async readers Ensure and retry);
// everything else is fatal, so "wrong token" and "well-formed value that doesn't fit the
// target" deliberately share TokenMismatch. Folding out-of-range into the enum also keeps
// the Try layer TOTAL: MessagePack-CSharp v3 / Nerdbank.MessagePack have no out-of-range
// category and their TryRead itself escapes OverflowException (a checked cast) on e.g.
// int64(0xd3) holding 2^31 read as an int32 target — verified 2026-07 on 3.1.8 / 1.2.36;
// wide-encoding ACCEPTANCE when the value fits matches all three libraries. Ours never
// throws from TryRead; throwing is deferred to the Read* extensions (MessagePackSerializationException).
//
// tokenSize (name shared with MessagePack-CSharp v3 / Nerdbank):
//   Success            -> the token's length: advance by it
//   InsufficientBuffer -> the total bytes this token requires (>= 1): fetch that much and
//                         retry. Exact, not worst-case — an async Ensure(tokenSize) never
//                         over-waits past a stream boundary, and str/bin report
//                         header+payload so one fetch completes the token. May grow across
//                         retries (header first, then header+payload) but strictly exceeds
//                         the window it was reported for, so retry loops terminate.
//   TokenMismatch      -> 0 (skip support intentionally not provided)
public enum DecodeResult
{
    Success = 0,
    TokenMismatch = 1,
    InsufficientBuffer = 2,
}

// Read primitives. TryReadInt32/TryReadInt64 are ported from the DisassembleBenchmark
// loop's converged winners. Shape: fixint fast branch (constant tokenSize — no
// consumed->next-address chain link for the dominant class) + unconditional payload load
// with an 8-entry format table + arithmetic sign extension for the rest. int64(d3)/
// uint64(cf) wider-than-needed encodings are legal msgpack and handled on a cold path.
// Everything else below is a plain compare cascade — a functional baseline in TryWrite
// style, and the candidate pool for future loop rounds.
//
// Contract: on non-Success, value is unspecified; tokenSize semantics per DecodeResult.
public static partial class MessagePackPrimitives
{
    #region unchecked big-endian loads

    // Unchecked big-endian loads for sites already dominated by an explicit Length
    // check. The checked BinaryPrimitives.ReadXxxBigEndian(span) forms keep their
    // internal length check — the JIT cannot propagate the caller's guard through Slice
    // (asm-verified in TryReadSingle: cmp+jl+throw block plus a forced stack frame
    // survive) — and the cold cascades are NOT rare paths: every message's trailing
    // fields hit them, since the remaining window tightens toward the buffer's end.
    // Each helper is exactly one unaligned load + bswap (movbe where available).

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ushort UnsafeReadUInt16BigEndian(ReadOnlySpan<byte> source, int offset)
        => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), offset)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint UnsafeReadUInt32BigEndian(ReadOnlySpan<byte> source, int offset)
        => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), offset)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ulong UnsafeReadUInt64BigEndian(ReadOnlySpan<byte> source, int offset)
        => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), offset)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static short UnsafeReadInt16BigEndian(ReadOnlySpan<byte> source, int offset)
        => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<short>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), offset)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int UnsafeReadInt32BigEndian(ReadOnlySpan<byte> source, int offset)
        => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), offset)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long UnsafeReadInt64BigEndian(ReadOnlySpan<byte> source, int offset)
        => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), offset)));

    #endregion

    #region ReadInt32, 64

    // Int32/Int64 read tables: 8 entries indexed by code - 0xcc (uint8 cc .. int64 d3).
    // The fixint fast path in front structurally removes fixint, and the range check
    // rejects every other code, so no total-domain table is needed. (The original Int32
    // design was a 256-entry 1KB table indexed by the raw code byte, with 128 fixint rows
    // and a codeMask field. Measured equal on TryReadInt32RematchBenchmark.Ultra —
    // FieldCycle 2.05->2.09, FixPos 0.60->0.59, Int32 2.60->2.66, Mixed 3.27->3.24 ns,
    // all inside the 10% ShortRun band — so the compact form was adopted for the 32x
    // smaller D-cache footprint at the cost of one predicted fatal-path branch.)
    // entry: len | rawShift<<4 | ext<<10 | isUInt64<<16 (64-bit table only)
    //   len      - token length in bytes ('entry & 0xf'; 9 marks Int32's nine-byte cold path)
    //   rawShift - right-aligns the payload inside the unconditional big-endian load
    //              (4-byte load for Int32: {24,16,0}; 8-byte load for Int64: {56,48,32,0})
    //   ext      - sign-extension shift pair: (v << ext) >> ext
    //   isUInt64 - marks the uint64(cf) format; the Int64 target reads it as "must decode
    //              non-negative" (sign-alias guard), the UInt64 target as "always valid"
    const uint R32U1 = 2 | (24u << 4);                 // uint8
    const uint R32U2 = 3 | (16u << 4);                 // uint16
    const uint R32U4 = 5 | (0u << 4);                  // uint32
    const uint R32W8 = 9;                              // uint64/int64: nine-byte cold path
    const uint R32I1 = 2 | (24u << 4) | (56u << 10);   // int8
    const uint R32I2 = 3 | (16u << 4) | (48u << 10);   // int16
    const uint R32I4 = 5 | (0u << 4) | (32u << 10);    // int32

    static ReadOnlySpan<uint> Int32ReadTable => // indexed by code - 0xcc, 8 entries (cc..d3)
    [
        R32U1, R32U2, R32U4, R32W8, R32I1, R32I2, R32I4, R32W8,
    ];

    const uint R64U1 = 2 | (56u << 4);                 // uint8
    const uint R64U2 = 3 | (48u << 4);                 // uint16
    const uint R64U4 = 5 | (32u << 4);                 // uint32
    const uint R64U8 = 9 | (0u << 4) | (1u << 16);     // uint64: isUInt64 flag
    const uint R64I1 = 2 | (56u << 4) | (56u << 10);   // int8
    const uint R64I2 = 3 | (48u << 4) | (48u << 10);   // int16
    const uint R64I4 = 5 | (32u << 4) | (32u << 10);   // int32
    const uint R64I8 = 9 | (0u << 4);                  // int64

    static ReadOnlySpan<uint> Int64ReadTable => // indexed by code - 0xcc, 8 entries (cc..d3)
    [
        R64U1, R64U2, R64U4, R64U8, R64I1, R64I2, R64I4, R64I8,
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadInt32(ReadOnlySpan<byte> source, out int value, out int tokenSize)
    {
        // fixint fast path: one predicted branch, and tokenSize is a
        // CONSTANT — the consumed->next-address chain vanishes for the dominant class.
        // Measured end-to-end: 0.43x on fixint-heavy streams, 0.81x at 50/50, 1.03x (noise) on adversarial mixes;
        // micro: harmless when fixint never occurs (predicted not-taken).
        // Needs only 1 byte, so it also serves buffer tails ahead of the >= 5 gate.
        if (!source.IsEmpty)
        {
            byte code0 = source[0];

            // The (byte) cast is load-bearing: it wraps the negative-fixint codes
            // 0xe0-0xff (+32 -> 256..287, mod 256 -> 0..31) down next to the positive
            // ones (32..159) so one unsigned compare covers both — (uint) would reject
            // them. Same bias+wrap idiom as the write side's (uint)(value + 32) <= 159,
            // but wrapping mod 2^8 over the code byte instead of mod 2^32 over the value.
            if ((byte)(code0 + 32) <= 159)
            {
                value = unchecked((sbyte)code0);
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }

        if (source.Length >= 5)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            byte code = s;
            uint fmt = (uint)(code - MessagePackCode.UInt8);
            if (fmt > 7) // not cc..d3 (fixint was handled above): fatal, predicted not-taken
            {
                value = 0;
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Int32ReadTable), (int)fmt);

            if ((e & 0xf) == 9) // compare length
            {
                // this is rare-path so branch-prediction does not miss here
                return TryReadInt32NineByteToken(source, out value, out tokenSize);
            }

            // unconditional 4-byte big-endian payload load, right-aligned per the entry
            uint p = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
            uint sel = p >> (int)((e >> 4) & 0x3f);
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            int len = (int)(e & 0xf);
            value = (int)v;
            // uint32 above int.MaxValue is the only reachable fit failure. Asm-verified
            // (FullOpts probe, 2026-07): the fit check is a sete and tokenSize a cmov, so
            // the consumed->next-address chain stays branchless; the DecodeResult return
            // itself may compile to a predicted branch (off the chain, harmless)
            bool ok = v == value;
            tokenSize = ok ? len : 0;
            return ok ? DecodeResult.Success : DecodeResult.TokenMismatch;
        }
        else
        {
            // Careful byte-by-byte path for source shorter than the unconditional-load window.
            // Reached only with Length < 5, so any Success from the int64 cascade came from a
            // <= 3-byte format — those always fit int32, and the only fit-breaking format
            // (uint32, 5 bytes) cannot reach Success here; no narrowing check needed. The
            // 5- and 9-byte formats deliberately report InsufficientBuffer with their exact
            // requirement, NOT TokenMismatch: a wide-encoded value may well fit int32 once
            // the rest arrives, and sequence/async readers rely on the Ensure-and-retry
            // contract at segment boundaries.
            var r = TryReadInt64ShortBuffer(source, out long v, out tokenSize);
            value = (int)v;
            return r;
        }

        // Cold path for the 9-byte formats int64(d3)/uint64(cf), narrowed to int32.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static DecodeResult TryReadInt32NineByteToken(ReadOnlySpan<byte> source, out int value, out int tokenSize)
        {
            if (source.Length >= 9)
            {
                byte code = MemoryMarshal.GetReference(source);
                long v = unchecked((long)UnsafeReadUInt64BigEndian(source, 1));
                value = (int)v;
                // two conditions: uint64 must be non-negative as long (reinterpreting
                // 0xFFFF.. as -1 would slip through the fit check), and the value must
                // survive the int32 narrowing
                if (((code == MessagePackCode.Int64) | (v >= 0)) & (v == value))
                {
                    tokenSize = 9;
                    return DecodeResult.Success;
                }
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            value = 0;
            tokenSize = 9;
            return DecodeResult.InsufficientBuffer;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadInt64(ReadOnlySpan<byte> source, out long value, out int tokenSize)
    {
        if (!source.IsEmpty)
        {
            byte code0 = source[0];

            // fixint; (byte) wrap is load-bearing, see TryReadInt32
            if ((byte)(code0 + 32) <= 159)
            {
                value = unchecked((sbyte)code0);
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }

        if (source.Length >= 9)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            byte code = s;
            uint fmt = (uint)(code - MessagePackCode.UInt8);
            if (fmt > 7) // not cc..d3
            {
                value = 0;
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Int64ReadTable), (int)fmt);
            ulong p = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s, 1)));
            ulong sel = p >> (int)((e >> 4) & 0x3f);
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            int len = (int)(e & 0xf);
            value = v;
            // the isUInt64 flag catches uint64 payloads above long.MaxValue; same cmov/sete
            // codegen as TryReadInt32 (verified there)
            bool ok = ((e >> 16) == 0) | (v >= 0);
            tokenSize = ok ? len : 0;
            return ok ? DecodeResult.Success : DecodeResult.TokenMismatch;
        }
        return TryReadInt64ShortBuffer(source, out value, out tokenSize);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static DecodeResult TryReadInt64ShortBuffer(ReadOnlySpan<byte> source, out long value, out int tokenSize)
    {
        value = 0;
        if (source.IsEmpty)
        {
            tokenSize = 1;
            return DecodeResult.InsufficientBuffer;
        }

        byte code = source[0];
        switch (code)
        {
            case MessagePackCode.UInt8:
                tokenSize = 2;
                if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                value = source[1];
                return DecodeResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                value = UnsafeReadUInt16BigEndian(source, 1);
                return DecodeResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                value = UnsafeReadUInt32BigEndian(source, 1);
                return DecodeResult.Success;
            case MessagePackCode.UInt64:
                {
                    tokenSize = 9;
                    if (source.Length < 9) return DecodeResult.InsufficientBuffer;
                    ulong raw = UnsafeReadUInt64BigEndian(source, 1);
                    if (raw > long.MaxValue)
                    {
                        tokenSize = 0;
                        return DecodeResult.TokenMismatch;
                    }
                    value = unchecked((long)raw);
                    return DecodeResult.Success;
                }
            case MessagePackCode.Int8:
                tokenSize = 2;
                if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                value = unchecked((sbyte)source[1]);
                return DecodeResult.Success;
            case MessagePackCode.Int16:
                tokenSize = 3;
                if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                value = UnsafeReadInt16BigEndian(source, 1);
                return DecodeResult.Success;
            case MessagePackCode.Int32:
                tokenSize = 5;
                if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                value = UnsafeReadInt32BigEndian(source, 1);
                return DecodeResult.Success;
            case MessagePackCode.Int64:
                tokenSize = 9;
                if (source.Length < 9) return DecodeResult.InsufficientBuffer;
                value = UnsafeReadInt64BigEndian(source, 1);
                return DecodeResult.Success;
            default:
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
        }
    }

    #endregion

    #region ReadUInt32, 64

    /// <summary>Reads a uint32 from any msgpack int format. Out-of-range values report TokenMismatch.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadUInt32(ReadOnlySpan<byte> source, out uint value, out int tokenSize)
        => TryReadUnsignedCore(source, uint.MaxValue, out value, out tokenSize);

    // Every unsigned target from uint32 down is the SAME gate-5 table body — NOT a
    // narrowing wrapper over TryReadUInt64, whose >= 9 gate would drop complete tokens
    // in 5..8-byte windows (buffer tails, exact-requirement sequence windows) to the
    // cascade. The range folds into ONE unsigned compare: (ulong)v <= max rejects
    // negatives (they alias to huge ulongs) and overflow together. Callers pass constant
    // bounds into the AggressiveInlining core, so each wrapper specializes into a
    // dedicated body — no shared-code dispatch survives to runtime.
    // (Why not gate 3 for byte/uint16? wide acceptance: uint32/int32-encoded values that
    // fit the target are legal, and decoding a 5-byte token inline needs the 4-byte
    // load, hence Length >= 5. The 9-byte formats stay on a cold path as usual.)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadUnsignedCore(ReadOnlySpan<byte> source, ulong max, out uint value, out int tokenSize)
    {
        // positive-fixint fast path (127 fits every unsigned target here); negative
        // fixint falls through and the range check rejects it for free
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if (code0 <= MessagePackCode.MaxFixInt)
            {
                value = code0;
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }

        if (source.Length >= 5)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            byte code = s;
            uint fmt = (uint)(code - MessagePackCode.UInt8);
            if (fmt > 7) // not cc..d3: fatal, predicted not-taken
            {
                value = 0;
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Int32ReadTable), (int)fmt);
            if ((e & 0xf) == 9) return NineByteToken(source, max, out value, out tokenSize);

            uint p = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
            uint sel = p >> (int)((e >> 4) & 0x3f);
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            int len = (int)(e & 0xf);
            value = unchecked((uint)v);
            bool ok = (ulong)v <= max;
            tokenSize = ok ? len : 0;
            return ok ? DecodeResult.Success : DecodeResult.TokenMismatch;
        }

        // Length < 5: Success from the shared cascade comes from <= 3-byte formats
        // (<= 65535) — that can still exceed a byte target, so fit-check it; the 5- and
        // 9-byte formats report their exact requirement as InsufficientBuffer
        var r = TryReadUInt64ShortBuffer(source, out ulong wide, out tokenSize);
        value = unchecked((uint)wide);
        if (r == DecodeResult.Success && wide > max)
        {
            value = 0;
            tokenSize = 0;
            return DecodeResult.TokenMismatch;
        }
        return r;

        // cold path for the 9-byte formats: raw <= max covers both — cf's raw bits are
        // the value, and d3's negative values alias to huge ulongs
        [MethodImpl(MethodImplOptions.NoInlining)]
        static DecodeResult NineByteToken(ReadOnlySpan<byte> source, ulong max, out uint value, out int tokenSize)
        {
            if (source.Length >= 9)
            {
                ulong raw = UnsafeReadUInt64BigEndian(source, 1);
                value = unchecked((uint)raw);
                bool ok = raw <= max;
                tokenSize = ok ? 9 : 0;
                return ok ? DecodeResult.Success : DecodeResult.TokenMismatch;
            }
            value = 0;
            tokenSize = 9;
            return DecodeResult.InsufficientBuffer;
        }
    }

    // Signed mirror of TryReadUnsignedCore for sbyte/int16: the range check biases into
    // one unsigned compare, (ulong)(v - min) <= (ulong)(max - min) — any v outside
    // [min, max] lands above the tiny range, wraparound included.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadSignedCore(ReadOnlySpan<byte> source, long min, long max, out int value, out int tokenSize)
    {
        // full fixint fast path — every fixint value [-32, 127] fits every signed target
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((byte)(code0 + 32) <= 159) // fixint; (byte) wrap is load-bearing, see TryReadInt32
            {
                value = unchecked((sbyte)code0);
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }

        if (source.Length >= 5)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            byte code = s;
            uint fmt = (uint)(code - MessagePackCode.UInt8);
            if (fmt > 7) // not cc..d3: fatal, predicted not-taken
            {
                value = 0;
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Int32ReadTable), (int)fmt);
            if ((e & 0xf) == 9) return NineByteToken(source, min, max, out value, out tokenSize);

            uint p = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
            uint sel = p >> (int)((e >> 4) & 0x3f);
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            int len = (int)(e & 0xf);
            value = unchecked((int)v);
            bool ok = (ulong)(v - min) <= (ulong)(max - min);
            tokenSize = ok ? len : 0;
            return ok ? DecodeResult.Success : DecodeResult.TokenMismatch;
        }

        // Length < 5: Success from the shared cascade comes from <= 3-byte formats
        // ([-32768, 65535]) — can exceed either bound of a narrow target, so fit-check
        var r = TryReadInt64ShortBuffer(source, out long wideV, out tokenSize);
        value = unchecked((int)wideV);
        if (r == DecodeResult.Success && (ulong)(wideV - min) > (ulong)(max - min))
        {
            value = 0;
            tokenSize = 0;
            return DecodeResult.TokenMismatch;
        }
        return r;

        // cold path for the 9-byte formats: the bias check also rejects cf values above
        // long.MaxValue (they alias negative)
        [MethodImpl(MethodImplOptions.NoInlining)]
        static DecodeResult NineByteToken(ReadOnlySpan<byte> source, long min, long max, out int value, out int tokenSize)
        {
            if (source.Length >= 9)
            {
                long v = unchecked((long)UnsafeReadUInt64BigEndian(source, 1));
                value = unchecked((int)v);
                bool ok = (ulong)(v - min) <= (ulong)(max - min);
                tokenSize = ok ? 9 : 0;
                return ok ? DecodeResult.Success : DecodeResult.TokenMismatch;
            }
            value = 0;
            tokenSize = 9;
            return DecodeResult.InsufficientBuffer;
        }
    }

    /// <summary>Reads a uint64 from any msgpack int format. Negative values report TokenMismatch.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadUInt64(ReadOnlySpan<byte> source, out ulong value, out int tokenSize)
    {
        // positive-fixint fast path (constant tokenSize, same rationale as TryReadInt32).
        // Negative fixint needs no arm here: it falls through and the range check below
        // rejects it for free — a negative value never fits an unsigned target.
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if (code0 <= MessagePackCode.MaxFixInt)
            {
                value = code0;
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }

        if (source.Length >= 9)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            byte code = s;
            uint fmt = (uint)(code - MessagePackCode.UInt8);
            if (fmt > 7) // not cc..d3: fatal, predicted not-taken
            {
                value = 0;
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            // Int64ReadTable reused wholesale — only the sign predicate flips. The bit16
            // flag marks uint64(cf): always valid for THIS target (its raw bits are the
            // value), while every other format must decode non-negative. The Int64 target
            // reads the same flag the other way around ("cf must be non-negative").
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Int64ReadTable), (int)fmt);
            ulong p = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s, 1)));
            ulong sel = p >> (int)((e >> 4) & 0x3f);
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            int len = (int)(e & 0xf);
            value = unchecked((ulong)v);
            bool ok = ((e >> 16) != 0) | (v >= 0);
            tokenSize = ok ? len : 0;
            return ok ? DecodeResult.Success : DecodeResult.TokenMismatch;
        }

        return TryReadUInt64ShortBuffer(source, out value, out tokenSize);
    }

    // careful byte-by-byte path for source shorter than the unconditional-load window
    [MethodImpl(MethodImplOptions.NoInlining)]
    static DecodeResult TryReadUInt64ShortBuffer(ReadOnlySpan<byte> source, out ulong value, out int tokenSize)
    {
        value = 0;
        if (source.IsEmpty)
        {
            tokenSize = 1;
            return DecodeResult.InsufficientBuffer;
        }
        byte code = source[0];
        if (code <= MessagePackCode.MaxFixInt) // positive fixint
        {
            value = code;
            tokenSize = 1;
            return DecodeResult.Success;
        }
        if (code >= MessagePackCode.MinNegativeFixInt) // negative fixint never fits
        {
            tokenSize = 0;
            return DecodeResult.TokenMismatch;
        }
        switch (code)
        {
            case MessagePackCode.UInt8:
                tokenSize = 2;
                if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                value = source[1];
                return DecodeResult.Success;
            case MessagePackCode.UInt16:
                tokenSize = 3;
                if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                value = UnsafeReadUInt16BigEndian(source, 1);
                return DecodeResult.Success;
            case MessagePackCode.UInt32:
                tokenSize = 5;
                if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                value = UnsafeReadUInt32BigEndian(source, 1);
                return DecodeResult.Success;
            case MessagePackCode.UInt64:
                tokenSize = 9;
                if (source.Length < 9) return DecodeResult.InsufficientBuffer;
                value = UnsafeReadUInt64BigEndian(source, 1);
                return DecodeResult.Success;
            case MessagePackCode.Int8:
                {
                    tokenSize = 2;
                    if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                    long v = unchecked((sbyte)source[1]);
                    if (v < 0)
                    {
                        tokenSize = 0;
                        return DecodeResult.TokenMismatch;
                    }
                    value = unchecked((ulong)v);
                    return DecodeResult.Success;
                }
            case MessagePackCode.Int16:
                {
                    tokenSize = 3;
                    if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                    long v = UnsafeReadInt16BigEndian(source, 1);
                    if (v < 0)
                    {
                        tokenSize = 0;
                        return DecodeResult.TokenMismatch;
                    }
                    value = unchecked((ulong)v);
                    return DecodeResult.Success;
                }
            case MessagePackCode.Int32:
                {
                    tokenSize = 5;
                    if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                    long v = UnsafeReadInt32BigEndian(source, 1);
                    if (v < 0)
                    {
                        tokenSize = 0;
                        return DecodeResult.TokenMismatch;
                    }
                    value = unchecked((ulong)v);
                    return DecodeResult.Success;
                }
            case MessagePackCode.Int64:
                {
                    tokenSize = 9;
                    if (source.Length < 9) return DecodeResult.InsufficientBuffer;
                    long v = UnsafeReadInt64BigEndian(source, 1);
                    if (v < 0)
                    {
                        tokenSize = 0;
                        return DecodeResult.TokenMismatch;
                    }
                    value = unchecked((ulong)v);
                    return DecodeResult.Success;
                }
            default:
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
        }
    }

    #endregion

    #region ReadInt8, 16

    // Natural-width tiers. The gate-5 cores above "round up" the narrow targets: a
    // 2..4-byte window holding a COMPLETE uint8/int16 token would drop to the cascade —
    // and that is not a rare tail, a trailing byte field sees remaining == 2 on every
    // message. So each narrow target decides its NATURAL encoding domain (everything a
    // minimal writer can emit for its value range) inline under the smallest possible
    // gate, and defers only the wide compat encodings and true tails to a NoInlining
    // bridge into the gate-5 cores.

    /// <summary>Reads a byte from any msgpack int format. Out-of-range values report TokenMismatch.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadByte(ReadOnlySpan<byte> source, out byte value, out int tokenSize)
    {
        // natural domain: fixint + uint8, both fit unconditionally — no range check at all
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if (code0 <= MessagePackCode.MaxFixInt)
            {
                value = code0;
                tokenSize = 1;
                return DecodeResult.Success;
            }
            if (code0 == MessagePackCode.UInt8 && source.Length >= 2)
            {
                value = source[1];
                tokenSize = 2;
                return DecodeResult.Success;
            }
        }
        return Rare(source, out value, out tokenSize);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static DecodeResult Rare(ReadOnlySpan<byte> source, out byte value, out int tokenSize)
        {
            var r = TryReadUnsignedCore(source, byte.MaxValue, out uint v, out tokenSize);
            value = unchecked((byte)v);
            return r;
        }
    }

    /// <summary>Reads an sbyte from any msgpack int format. Out-of-range values report TokenMismatch.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadSByte(ReadOnlySpan<byte> source, out sbyte value, out int tokenSize)
    {
        // natural domain: fixint + int8, both fit unconditionally
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((byte)(code0 + 32) <= 159) // fixint; (byte) wrap is load-bearing, see TryReadInt32
            {
                value = unchecked((sbyte)code0);
                tokenSize = 1;
                return DecodeResult.Success;
            }
            if (code0 == MessagePackCode.Int8 && source.Length >= 2)
            {
                value = unchecked((sbyte)source[1]);
                tokenSize = 2;
                return DecodeResult.Success;
            }
        }
        return Rare(source, out value, out tokenSize);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static DecodeResult Rare(ReadOnlySpan<byte> source, out sbyte value, out int tokenSize)
        {
            var r = TryReadSignedCore(source, sbyte.MinValue, sbyte.MaxValue, out int v, out tokenSize);
            value = unchecked((sbyte)v);
            return r;
        }
    }

    // 16-bit natural tier: the minimal encodings for the whole ushort/short domain are
    // cc/cd/d0/d1 (+fixint, handled in front), so a 2-byte unconditional load and this
    // mini-table decode everything a minimal writer can emit under a Length >= 3 gate.
    // Zero entries (ce/cf/d2/d3) are legal wide encodings — decided cold in the bridge.
    // entry: len | rawShift<<4 | ext<<10 (same field meanings as the 8-entry tables)
    const uint N16U1 = 2 | (8u << 4);                  // uint8
    const uint N16U2 = 3 | (0u << 4);                  // uint16
    const uint N16I1 = 2 | (8u << 4) | (56u << 10);    // int8
    const uint N16I2 = 3 | (0u << 4) | (48u << 10);    // int16
    const uint N16W_ = 0;                              // uint32/uint64/int32/int64: bridge

    static ReadOnlySpan<uint> Narrow16ReadTable => // indexed by code - 0xcc, 8 entries (cc..d3)
    [
        N16U1, N16U2, N16W_, N16W_, N16I1, N16I2, N16W_, N16W_,
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static DecodeResult TryReadNarrow16Core(ReadOnlySpan<byte> source, long min, long max, out int value, out int tokenSize)
    {
        // fixint fast path: ONE validity compare and an unconditional Success — tokenSize
        // must stay a CONSTANT here. (A bias-checked cmov variant made consumed
        // data-dependent and cost 4x on fixint streams, measured.) The min == 0 selector
        // is folded by the JIT per specialization: unsigned targets accept only positive
        // fixint (negative falls through to the range check, which rejects it for free);
        // signed targets accept every fixint — [-32, 127] fits them all.
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if (min == 0 ? code0 <= MessagePackCode.MaxFixInt : (byte)(code0 + 32) <= 159)
            {
                value = unchecked((sbyte)code0);
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }

        if (source.Length >= 3)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            byte code = s;
            uint fmt = (uint)(code - MessagePackCode.UInt8);
            if (fmt > 7) // not cc..d3: fatal, predicted not-taken
            {
                value = 0;
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Narrow16ReadTable), (int)fmt);
            if (e == 0) return Bridge(source, min, max, out value, out tokenSize);

            uint p = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref s, 1)));
            uint sel = p >> (int)((e >> 4) & 0x3f);
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            int len = (int)(e & 0xf);
            value = unchecked((int)v);
            bool ok = (ulong)(v - min) <= (ulong)(max - min);
            tokenSize = ok ? len : 0;
            return ok ? DecodeResult.Success : DecodeResult.TokenMismatch;
        }

        return Bridge(source, min, max, out value, out tokenSize);

        // Wide encodings and short windows decide cold in the gate-5 signed core. Safe
        // for the ushort bounds too: the ONLY place TryReadSignedCore skips the bias
        // check is its fixint fast path, and fixint never reaches this bridge (the fast
        // path above consumes it for any non-empty source).
        [MethodImpl(MethodImplOptions.NoInlining)]
        static DecodeResult Bridge(ReadOnlySpan<byte> source, long min, long max, out int value, out int tokenSize)
            => TryReadSignedCore(source, min, max, out value, out tokenSize);
    }

    /// <summary>Reads an int16 from any msgpack int format. Out-of-range values report TokenMismatch.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadInt16(ReadOnlySpan<byte> source, out short value, out int tokenSize)
    {
        var r = TryReadNarrow16Core(source, short.MinValue, short.MaxValue, out int v, out tokenSize);
        value = unchecked((short)v);
        return r;
    }

    /// <summary>Reads a uint16 from any msgpack int format. Out-of-range values report TokenMismatch.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadUInt16(ReadOnlySpan<byte> source, out ushort value, out int tokenSize)
    {
        var r = TryReadNarrow16Core(source, ushort.MinValue, ushort.MaxValue, out int v, out tokenSize);
        value = unchecked((ushort)v);
        return r;
    }

    /// <summary>Reads a char (encoded as uint16, MessagePack-CSharp compatible).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadChar(ReadOnlySpan<byte> source, out char value, out int tokenSize)
    {
        var r = TryReadUInt16(source, out ushort v, out tokenSize);
        value = (char)v;
        return r;
    }

    #endregion

    #region fixed-length(Nil, Boolean, Single, Double)

    /// <summary>
    /// True iff the next value is nil (which is always exactly 1 byte, hence no
    /// tokenSize out). Peek-style bool on purpose: false only means "not nil here,
    /// read the real type", never an error, so the DecodeResult contract does not apply —
    /// an empty source answers false and defers error reporting to the typed read that
    /// follows.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadNil(ReadOnlySpan<byte> source)
    {
        // source[0] after the IsEmpty guard is bounds-check-free, asm-verified.
        return !source.IsEmpty && source[0] == MessagePackCode.Nil;
    }

    /// <summary>Reads a boolean (0xc2/0xc3).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadBoolean(ReadOnlySpan<byte> source, out bool value, out int tokenSize)
    {
        if (source.IsEmpty)
        {
            value = false;
            tokenSize = 1;
            return DecodeResult.InsufficientBuffer;
        }

        // False=0xc2 / True=0xc3: one biased compare validates, the low bit is the value.
        // The only branch here depends on VALIDITY, not on the value — on well-formed data it
        // always takes the same direction, so it predicts ~100% regardless of the true/false
        // mix; the value itself is derived without a branch (d != 0 -> setne). A switch/if on
        // the code byte branches on the value itself and mispredicts on unpredictable bools
        // (up to 50% for random data — measured 3.8x slower; predictable data ties). Removing
        // this last branch too is NOT better: tokenSize then becomes data-dependent and joins
        // the consumed->next-address serial chain (measured 2.4x slower on every distribution).
        uint d = (uint)source[0] - MessagePackCode.False;
        if (d <= 1)
        {
            value = d != 0;
            tokenSize = 1;
            return DecodeResult.Success;
        }

        value = false;
        tokenSize = 0;
        return DecodeResult.TokenMismatch;
    }

    /// <summary>Reads a float32; also accepts float64 (narrowing cast) and any int format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadSingle(ReadOnlySpan<byte> source, out float value, out int tokenSize)
    {
        // The Float32 arm IS the whole hot path: unlike int, float has no size-minimized
        // encoding variety — a conforming writer emits 0xca for every float32 value. So we
        // bet on it up front, TryReadInt32-gate style: one length check covers the whole
        // 5-byte token and the arm runs with no per-arm guard. Two always-predicted
        // branches (length, mono-class code compare), constant tokenSize (no consumed->
        // next-address chain link), branchless value load — the shape TryReadBoolean/
        // TryReadInt32 converged on. Everything else, including the truncated-Float32
        // tail, lives in the NoInlining slow half, keeping the inlined footprint at
        // formatter callsites tiny.
        if (source.Length >= 5 && source[0] == MessagePackCode.Float32)
        {
            // NOT BinaryPrimitives.ReadSingleBigEndian(source.Slice(1)) — that compiles
            // byte-identically to the Slice+ReadUInt32 pair, and in BOTH the >= 5 gate
            // fails to propagate through Slice for the JIT: the primitive's internal
            // length check survives as cmp+jl+throw block and forces a stack frame
            // (probe asm 93B). The unchecked read below is what the gate already paid
            // for: 54B, frameless, one movbe load straight off the code byte.
            value = BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), 1))));
            tokenSize = 5;
            return DecodeResult.Success;
        }
        return TryReadSingleSlow(source, out value, out tokenSize);

        // COMPAT arms, not perf paths: data written as double/int read into a float field
        // (schema drift, foreign writers). Even then each field's encoding class is stable,
        // so the cascade's branches still predict; a 256-entry table would only trade a
        // predicted branch for a data-dependent tokenSize (measured 2.4x worse in the
        // boolean round) and is deliberately not used here. Also catches the hot gate's
        // truncated-Float32 tail (Length < 5) to report the exact tokenSize.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static DecodeResult TryReadSingleSlow(ReadOnlySpan<byte> source, out float value, out int tokenSize)
        {
            if (!source.IsEmpty)
            {
                byte code = source[0];
                if (code == MessagePackCode.Float32)
                {
                    value = default;
                    tokenSize = 5;
                    if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                    value = BitConverter.UInt32BitsToSingle(UnsafeReadUInt32BigEndian(source, 1));
                    return DecodeResult.Success;
                }

                if (code == MessagePackCode.Float64)
                {
                    value = default;
                    tokenSize = 9;
                    if (source.Length < 9) return DecodeResult.InsufficientBuffer;
                    value = (float)BitConverter.UInt64BitsToDouble(UnsafeReadUInt64BigEndian(source, 1));
                    return DecodeResult.Success;
                }

                // uint64 above long.MaxValue: TryReadInt64 folds it to TokenMismatch, but as a
                // float source it is a valid magnitude — handle before falling through
                if (code == MessagePackCode.UInt64)
                {
                    value = default;
                    tokenSize = 9;
                    if (source.Length < 9) return DecodeResult.InsufficientBuffer;
                    value = UnsafeReadUInt64BigEndian(source, 1);
                    return DecodeResult.Success;
                }
            }

            var r = TryReadInt64(source, out long l, out tokenSize);
            value = l;
            return r;
        }
    }

    /// <summary>Reads a float64; also accepts float32 (widening, exact) and any int format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadDouble(ReadOnlySpan<byte> source, out double value, out int tokenSize)
    {
        // Same hot-path bet as TryReadSingle (see its comment) with the gate at 9:
        // Float64 (0xcb) is the sole encoding a double-writing producer emits.
        if (source.Length >= 9 && source[0] == MessagePackCode.Float64)
        {
            // unchecked read for the same reason as TryReadSingle: the checked
            // BinaryPrimitives path keeps its internal length check despite the gate
            value = BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), 1))));
            tokenSize = 9;
            return DecodeResult.Success;
        }
        return TryReadDoubleSlow(source, out value, out tokenSize);

        // compat arms + the hot gate's truncated-Float64 tail; see TryReadSingleSlow
        [MethodImpl(MethodImplOptions.NoInlining)]
        static DecodeResult TryReadDoubleSlow(ReadOnlySpan<byte> source, out double value, out int tokenSize)
        {
            if (!source.IsEmpty)
            {
                byte code = source[0];
                if (code == MessagePackCode.Float64)
                {
                    value = default;
                    tokenSize = 9;
                    if (source.Length < 9) return DecodeResult.InsufficientBuffer;
                    value = BitConverter.UInt64BitsToDouble(UnsafeReadUInt64BigEndian(source, 1));
                    return DecodeResult.Success;
                }

                if (code == MessagePackCode.Float32)
                {
                    value = default;
                    tokenSize = 5;
                    if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                    value = BitConverter.UInt32BitsToSingle(UnsafeReadUInt32BigEndian(source, 1));
                    return DecodeResult.Success;
                }

                if (code == MessagePackCode.UInt64)
                {
                    value = default;
                    tokenSize = 9;
                    if (source.Length < 9) return DecodeResult.InsufficientBuffer;
                    value = UnsafeReadUInt64BigEndian(source, 1);
                    return DecodeResult.Success;
                }
            }

            var r = TryReadInt64(source, out long l, out tokenSize);
            value = l;
            return r;
        }
    }

    #endregion

    #region headers(array, map, string, bin), binary

    /// <summary>Reads an array header (fixarray/array16/array32). A count above int.MaxValue reports TokenMismatch.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadArrayHeader(ReadOnlySpan<byte> source, out int count, out int tokenSize)
    {
        // fixarray fast path inline (the dominant class: POCO field counts, small
        // collections — one predicted mask+compare, constant tokenSize, branchless
        // count); array16/32 and every failure decide cold, keeping the inlined
        // footprint at formatter callsites minimal. Measured NEUTRAL on flat-POCO e2e
        // (one header per message amortizes; the call hid under OoO) — kept for shape
        // consistency with the value readers and for nested collections, where headers
        // are per-ELEMENT and the call-to-work ratio matches the byte-reader round's
        // winning conditions.
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((code0 & 0b1111_0000) == MessagePackCode.MinFixArray) // fixarray 1001_xxxx (0x90..0x9f)
            {
                count = code0 & 0b0000_1111; // xxxx = count
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }
        return TryReadArrayHeaderSlow(source, out count, out tokenSize);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static DecodeResult TryReadArrayHeaderSlow(ReadOnlySpan<byte> source, out int count, out int tokenSize)
        {
            count = 0;
            if (source.IsEmpty)
            {
                tokenSize = 1;
                return DecodeResult.InsufficientBuffer;
            }
            byte code = source[0];
            if ((code & 0b1111_0000) == MessagePackCode.MinFixArray) // fixarray 1001_xxxx (0x90..0x9f)
            {
                count = code & 0b0000_1111; // xxxx = count
                tokenSize = 1;
                return DecodeResult.Success;
            }
            switch (code)
            {
                case MessagePackCode.Array16:
                    tokenSize = 3;
                    if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                    count = UnsafeReadUInt16BigEndian(source, 1);
                    return DecodeResult.Success;
                case MessagePackCode.Array32:
                    {
                        tokenSize = 5;
                        if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                        uint c = UnsafeReadUInt32BigEndian(source, 1);
                        // NOT malformed data: a u32 count is legal msgpack, it just exceeds
                        // .NET's int indexing limit — nothing on this platform could
                        // materialize it. Per the contract, "well-formed but does not fit
                        // the declared target (int count)" folds into TokenMismatch, same
                        // as cf(2^63)->Int64. MessagePack-CSharp v3 / Nerdbank instead type
                        // the primitive `out uint` and let their reader layer throw
                        // OverflowException on the cast — that only relocates the same
                        // fatal outcome while spreading the fit check to every caller.
                        // (Applies equally to the Map32/Str32/Bin32 twins below.)
                        if (c > int.MaxValue)
                        {
                            tokenSize = 0;
                            return DecodeResult.TokenMismatch;
                        }
                        count = unchecked((int)c);
                        return DecodeResult.Success;
                    }
                default:
                    tokenSize = 0;
                    return DecodeResult.TokenMismatch;
            }
        }
    }

    /// <summary>Reads a map header (fixmap/map16/map32). A count above int.MaxValue reports TokenMismatch.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadMapHeader(ReadOnlySpan<byte> source, out int count, out int tokenSize)
    {
        // fixmap fast path inline, everything else cold — see TryReadArrayHeader
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((code0 & 0b1111_0000) == MessagePackCode.MinFixMap) // fixmap 1000_xxxx (0x80..0x8f)
            {
                count = code0 & 0b0000_1111; // xxxx = count
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }
        return TryReadMapHeaderSlow(source, out count, out tokenSize);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static DecodeResult TryReadMapHeaderSlow(ReadOnlySpan<byte> source, out int count, out int tokenSize)
        {
            count = 0;
            if (source.IsEmpty)
            {
                tokenSize = 1;
                return DecodeResult.InsufficientBuffer;
            }
            byte code = source[0];
            if ((code & 0b1111_0000) == MessagePackCode.MinFixMap) // fixmap 1000_xxxx (0x80..0x8f)
            {
                count = code & 0b0000_1111; // xxxx = count
                tokenSize = 1;
                return DecodeResult.Success;
            }
            switch (code)
            {
                case MessagePackCode.Map16:
                    tokenSize = 3;
                    if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                    count = UnsafeReadUInt16BigEndian(source, 1);
                    return DecodeResult.Success;
                case MessagePackCode.Map32:
                    {
                        tokenSize = 5;
                        if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                        uint c = UnsafeReadUInt32BigEndian(source, 1);
                        if (c > int.MaxValue) // legal msgpack, exceeds the int target — see Array32
                        {
                            tokenSize = 0;
                            return DecodeResult.TokenMismatch;
                        }
                        count = unchecked((int)c);
                        return DecodeResult.Success;
                    }
                default:
                    tokenSize = 0;
                    return DecodeResult.TokenMismatch;
            }
        }
    }

    /// <summary>Reads a str header (fixstr/str8/str16/str32). A byte count above int.MaxValue reports TokenMismatch.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadStringHeader(ReadOnlySpan<byte> source, out int byteCount, out int tokenSize)
    {
        // fixstr fast path inline, everything else cold — see TryReadArrayHeader
        if (!source.IsEmpty)
        {
            byte code0 = source[0];
            if ((code0 & 0b1110_0000) == MessagePackCode.MinFixStr) // fixstr 101x_xxxx (0xa0..0xbf)
            {
                byteCount = code0 & 0b0001_1111; // x_xxxx = byte count (5 bits, 0..31)
                tokenSize = 1;
                return DecodeResult.Success;
            }
        }
        return TryReadStringHeaderSlow(source, out byteCount, out tokenSize);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static DecodeResult TryReadStringHeaderSlow(ReadOnlySpan<byte> source, out int byteCount, out int tokenSize)
        {
            byteCount = 0;
            if (source.IsEmpty)
            {
                tokenSize = 1;
                return DecodeResult.InsufficientBuffer;
            }
            byte code = source[0];
            if ((code & 0b1110_0000) == MessagePackCode.MinFixStr) // fixstr 101x_xxxx (0xa0..0xbf)
            {
                byteCount = code & 0b0001_1111; // x_xxxx = byte count (5 bits, 0..31)
                tokenSize = 1;
                return DecodeResult.Success;
            }
            switch (code)
            {
                case MessagePackCode.Str8:
                    tokenSize = 2;
                    if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                    byteCount = source[1];
                    return DecodeResult.Success;
                case MessagePackCode.Str16:
                    tokenSize = 3;
                    if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                    byteCount = UnsafeReadUInt16BigEndian(source, 1);
                    return DecodeResult.Success;
                case MessagePackCode.Str32:
                    {
                        tokenSize = 5;
                        if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                        uint c = UnsafeReadUInt32BigEndian(source, 1);
                        if (c > int.MaxValue) // legal msgpack, exceeds the int target — see Array32
                        {
                            tokenSize = 0;
                            return DecodeResult.TokenMismatch;
                        }
                        byteCount = unchecked((int)c);
                        return DecodeResult.Success;
                    }
                default:
                    tokenSize = 0;
                    return DecodeResult.TokenMismatch;
            }
        }
    }

    /// <summary>Reads a bin header (bin8/bin16/bin32). A byte count above int.MaxValue reports TokenMismatch.</summary>
    public static DecodeResult TryReadBinHeader(ReadOnlySpan<byte> source, out int byteCount, out int tokenSize)
    {
        byteCount = 0;
        if (source.IsEmpty)
        {
            tokenSize = 1;
            return DecodeResult.InsufficientBuffer;
        }
        switch (source[0])
        {
            case MessagePackCode.Bin8:
                tokenSize = 2;
                if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                byteCount = source[1];
                return DecodeResult.Success;
            case MessagePackCode.Bin16:
                tokenSize = 3;
                if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                byteCount = UnsafeReadUInt16BigEndian(source, 1);
                return DecodeResult.Success;
            case MessagePackCode.Bin32:
                {
                    tokenSize = 5;
                    if (source.Length < 5) return DecodeResult.InsufficientBuffer;
                    uint c = UnsafeReadUInt32BigEndian(source, 1);
                    if (c > int.MaxValue) // legal msgpack, exceeds the int target — see Array32
                    {
                        tokenSize = 0;
                        return DecodeResult.TokenMismatch;
                    }
                    byteCount = unchecked((int)c);
                    return DecodeResult.Success;
                }
            default:
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
        }
    }

    /// <summary>Reads a bin payload as a slice of source (no copy). On InsufficientBuffer, tokenSize reports header + payload so one fetch completes the token.</summary>
    public static DecodeResult TryReadBinary(ReadOnlySpan<byte> source, out ReadOnlySpan<byte> value, out int tokenSize)
    {
        value = default;
        var r = TryReadBinHeader(source, out int byteCount, out int headerSize);
        if (r != DecodeResult.Success)
        {
            tokenSize = headerSize; // header's own requirement (or 0 on mismatch)
            return r;
        }
        // header + payload must stay an int: a total above int.MaxValue fits no span on
        // this platform (same fold as count > int.MaxValue), and letting it wrap would
        // hand a NEGATIVE tokenSize to sequence retry loops — an infinite loop on
        // adversarial headers
        if (byteCount > int.MaxValue - headerSize)
        {
            tokenSize = 0;
            return DecodeResult.TokenMismatch;
        }
        tokenSize = headerSize + byteCount;
        if (source.Length - headerSize < byteCount)
        {
            return DecodeResult.InsufficientBuffer;
        }
        value = source.Slice(headerSize, byteCount);
        return DecodeResult.Success;
    }

    #endregion

    #region Ext, Timestamp

    /// <summary>Reads an ext header (fixext1/2/4/8/16 or ext8/16/32). A data length above int.MaxValue reports TokenMismatch.</summary>
    public static DecodeResult TryReadExtHeader(ReadOnlySpan<byte> source, out sbyte typeCode, out int dataLength, out int tokenSize)
    {
        typeCode = 0;
        dataLength = 0;
        if (source.IsEmpty)
        {
            tokenSize = 1;
            return DecodeResult.InsufficientBuffer;
        }
        byte code = source[0];
        switch (code)
        {
            case MessagePackCode.FixExt1:
            case MessagePackCode.FixExt2:
            case MessagePackCode.FixExt4:
            case MessagePackCode.FixExt8:
            case MessagePackCode.FixExt16:
                tokenSize = 2;
                if (source.Length < 2) return DecodeResult.InsufficientBuffer;
                typeCode = unchecked((sbyte)source[1]);
                dataLength = 1 << (code - MessagePackCode.FixExt1);
                return DecodeResult.Success;
            case MessagePackCode.Ext8:
                tokenSize = 3;
                if (source.Length < 3) return DecodeResult.InsufficientBuffer;
                dataLength = source[1];
                typeCode = unchecked((sbyte)source[2]);
                return DecodeResult.Success;
            case MessagePackCode.Ext16:
                tokenSize = 4;
                if (source.Length < 4) return DecodeResult.InsufficientBuffer;
                dataLength = UnsafeReadUInt16BigEndian(source, 1);
                typeCode = unchecked((sbyte)source[3]);
                return DecodeResult.Success;
            case MessagePackCode.Ext32:
                {
                    tokenSize = 6;
                    if (source.Length < 6) return DecodeResult.InsufficientBuffer;
                    uint len = UnsafeReadUInt32BigEndian(source, 1);
                    if (len > int.MaxValue)
                    {
                        tokenSize = 0;
                        return DecodeResult.TokenMismatch;
                    }
                    dataLength = unchecked((int)len);
                    typeCode = unchecked((sbyte)source[5]);
                    return DecodeResult.Success;
                }
            default:
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
        }
    }

    // seconds range representable by DateTime (0001-01-01 .. 9999-12-31 23:59:59), relative to unix epoch
    const long MinTimestampSeconds = -BclSecondsAtUnixEpoch;
    const long MaxTimestampSeconds = 253402300799; // DateTime.MaxValue.Ticks / TicksPerSecond - BclSecondsAtUnixEpoch

    /// <summary>
    /// Reads a msgpack timestamp (ext type -1, 32/64/96-bit form) as a UTC DateTime.
    /// A well-formed ext of another type, or a timestamp outside DateTime's range,
    /// reports TokenMismatch. On InsufficientBuffer, tokenSize reports header + payload.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadTimestamp(ReadOnlySpan<byte> source, out DateTime value, out int tokenSize)
    {
        // ts64/ts32 fast paths: real timestamps are fixed byte patterns ([d7 ff|8B] /
        // [d6 ff|4B]), so ONE 16-bit load compares the format code and the ext type (-1)
        // together — no ext-header call, no dataLength switch. The generic range checks
        // exist only for ts96 generality: ts64's unsigned 34-bit seconds top out in year
        // 2514 and ts32's u32 seconds in 2106, both inside DateTime's range, so the only
        // data check left is ts64's nanoseconds < 1e9 (ts32 needs none). ts96, short
        // windows and mismatches decide cold in Rare (the previous full implementation).
        if (source.Length >= 6)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            ushort head = Unsafe.ReadUnaligned<ushort>(ref s);
            ushort ts64Head = BitConverter.IsLittleEndian ? (ushort)0xffd7 : (ushort)0xd7ff; // FixExt8, type -1
            ushort ts32Head = BitConverter.IsLittleEndian ? (ushort)0xffd6 : (ushort)0xd6ff; // FixExt4, type -1
            if (head == ts64Head && source.Length >= 10)
            {
                // timestamp 64: [nanoseconds in 30-bit | seconds in 34-bit]
                ulong data64 = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s, 2)));
                long nanoseconds = (long)(data64 >> 34);
                long seconds = (long)(data64 & 0x3_FFFF_FFFF);
                if ((ulong)nanoseconds < 1_000_000_000)
                {
                    value = new DateTime((seconds + BclSecondsAtUnixEpoch) * TimeSpan.TicksPerSecond + nanoseconds / NanosecondsPerTick, DateTimeKind.Utc);
                    tokenSize = 10;
                    return DecodeResult.Success;
                }
                value = default;
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            if (head == ts32Head)
            {
                // timestamp 32: seconds u32, no sub-second part — nothing to validate
                uint secs = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 2)));
                value = new DateTime((secs + BclSecondsAtUnixEpoch) * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
                tokenSize = 6;
                return DecodeResult.Success;
            }
        }
        return TryReadTimestampSlow(source, out value, out tokenSize);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static DecodeResult TryReadTimestampSlow(ReadOnlySpan<byte> source, out DateTime value, out int tokenSize)
        {
            value = default;
            var r = TryReadExtHeader(source, out sbyte typeCode, out int dataLength, out int headerSize);
            if (r != DecodeResult.Success)
            {
                tokenSize = headerSize; // header's own requirement (or 0 on mismatch)
                return r;
            }
            // A wrong ext type or a non-timestamp length is fatal regardless of
            // truncation — decide BEFORE reporting Insufficient (no pointless fetches
            // for data we could never accept). This also caps tokenSize at 6 + 12,
            // killing the headerSize + dataLength int overflow a malicious ext32
            // length would otherwise cause (negative tokenSize would hang sequence
            // retry loops).
            if (typeCode != MessagePackCode.TimestampExtensionTypeCode
                || (dataLength != 4 && dataLength != 8 && dataLength != 12))
            {
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            tokenSize = headerSize + dataLength;
            if (source.Length - headerSize < dataLength) return DecodeResult.InsufficientBuffer;
            var data = source.Slice(headerSize, dataLength);
            long seconds;
            long nanoseconds;
            switch (dataLength)
            {
                case 4: // timestamp 32: seconds u32
                    seconds = UnsafeReadUInt32BigEndian(data, 0);
                    nanoseconds = 0;
                    break;
                case 8: // timestamp 64: [nanoseconds in 30-bit | seconds in 34-bit]
                    {
                        ulong data64 = UnsafeReadUInt64BigEndian(data, 0);
                        nanoseconds = (long)(data64 >> 34);
                        seconds = (long)(data64 & 0x3_FFFF_FFFF);
                        break;
                    }
                case 12: // timestamp 96: nanoseconds u32 + seconds i64
                    nanoseconds = UnsafeReadUInt32BigEndian(data, 0);
                    seconds = UnsafeReadInt64BigEndian(data, 4);
                    break;
                default: // ext(-1) with a non-timestamp length
                    tokenSize = 0;
                    return DecodeResult.TokenMismatch;
            }
            // range checks: the biased-unsigned compare also catches the (seconds + bias) wraparound
            if ((ulong)(seconds - MinTimestampSeconds) > (ulong)(MaxTimestampSeconds - MinTimestampSeconds)
                || (ulong)nanoseconds >= 1_000_000_000)
            {
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            long ticks = (seconds + BclSecondsAtUnixEpoch) * TimeSpan.TicksPerSecond + nanoseconds / NanosecondsPerTick;
            if ((ulong)ticks > (ulong)DateTime.MaxValue.Ticks) // sub-second part can push past MaxValue at MaxTimestampSeconds
            {
                tokenSize = 0;
                return DecodeResult.TokenMismatch;
            }
            value = new DateTime(ticks, DateTimeKind.Utc);
            return DecodeResult.Success;
        }
    }

    #endregion

    #region String

    /// <summary>Reads a str as a string; nil reads as null (mirror of UnsafeWriteString(string?)). On InsufficientBuffer, tokenSize reports header + payload so one fetch completes the token.</summary>
    public static DecodeResult TryReadString(ReadOnlySpan<byte> source, out string? value, out int tokenSize)
    {
        value = null;
        if (!source.IsEmpty && source[0] == MessagePackCode.Nil)
        {
            tokenSize = 1;
            return DecodeResult.Success;
        }
        var r = TryReadStringHeader(source, out int byteCount, out int headerSize);
        if (r != DecodeResult.Success)
        {
            tokenSize = headerSize; // header's own requirement (or 0 on mismatch)
            return r;
        }
        if (byteCount > int.MaxValue - headerSize) // overflow guard, see TryReadBinary
        {
            tokenSize = 0;
            return DecodeResult.TokenMismatch;
        }
        tokenSize = headerSize + byteCount;
        if (source.Length - headerSize < byteCount)
        {
            return DecodeResult.InsufficientBuffer;
        }

        value = Encoding.UTF8.GetString(source.Slice(headerSize, byteCount));
        return DecodeResult.Success;
    }

    /// <summary>Reads a str payload as a UTF-8 slice of source (no copy, no nil handling; mirror of UnsafeWriteString(ReadOnlySpan&lt;byte&gt;)). On InsufficientBuffer, tokenSize reports header + payload.</summary>
    public static DecodeResult TryReadStringSpan(ReadOnlySpan<byte> source, out ReadOnlySpan<byte> utf8Value, out int tokenSize)
    {
        utf8Value = default;
        var r = TryReadStringHeader(source, out int byteCount, out int headerSize);
        if (r != DecodeResult.Success)
        {
            tokenSize = headerSize; // header's own requirement (or 0 on mismatch)
            return r;
        }
        if (byteCount > int.MaxValue - headerSize) // overflow guard, see TryReadBinary
        {
            tokenSize = 0;
            return DecodeResult.TokenMismatch;
        }
        tokenSize = headerSize + byteCount;
        if (source.Length - headerSize < byteCount)
        {
            return DecodeResult.InsufficientBuffer;
        }
        utf8Value = source.Slice(headerSize, byteCount);
        return DecodeResult.Success;
    }

    #endregion
}
