// Element-run cores for the primitive collection.
// IElementCodec is mimic of static abstract members for netstandard compatibility.

// Apply SIMD where it is applicable
// For the rest, and for the fallback paths, we still acquire the buffer in batches
// so that GetSpan/Advance calls are batched, which is faster than calling Write/Read per element
// During Serialize, the batch buffer acquisition is not for the full amount but is split
// by SerializeRegionElements, to avoid requesting an excessively large buffer

#if NET9_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace MessagePack.Formatters;

internal interface IElementCodec<T> where T : unmanaged
{
    void WriteElements<TWriteBuffer>(ref TWriteBuffer buffer, ReadOnlySpan<T> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        ;

    void ReadElements<TReadBuffer>(ref TReadBuffer buffer, Span<T> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        ;
}

#region varint

#if NET9_0_OR_GREATER
// Shuffle tables for the wide (multi-byte token) SIMD tiers. Shape tables shared by every
// family of the same token width carry a Token{3,5,9} prefix (token = [code][BE payload],
// 3/5/9 bytes total); serialize tables are Weave*, decode tables are Decode*. Code
// scatter/OR tables bake in a specific code byte, so they carry the owning element type.
file static class WeaveTables
{
    // ---- int32 wide tiers (5-byte tokens [0xce|0xd2][4B BE], 16-token block = 80B) ----

    // Per-dword byte reversal: turns each little-endian int32 into big-endian byte
    // order in place (dword t's bytes 4t..4t+3 become [b3 b2 b1 b0]).
    public static readonly Vector512<byte> Token5PayloadReverse512 = Vector512.Create(
        (byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12,
        19, 18, 17, 16, 23, 22, 21, 20, 27, 26, 25, 24, 31, 30, 29, 28,
        35, 34, 33, 32, 39, 38, 37, 36, 43, 42, 41, 40, 47, 46, 45, 44,
        51, 50, 49, 48, 55, 54, 53, 52, 59, 58, 57, 56, 63, 62, 61, 60);

    // Serialize weave, output bytes 0..63 of the token run. First source = Token5PayloadReverse512'd
    // payloads (token t's BE bytes at 4t..4t+3), second source = code dwords (token t's
    // code byte at 4t, so index 64+4t). Each row below is one token [code | b3 b2 b1 b0];
    // token 12 is cut off after 4 bytes — Token5WeaveShiftedIndices512 finishes it.
    public static readonly Vector512<byte> Token5WeaveIndices512 = Vector512.Create(
        (byte)64, 0, 1, 2, 3,    //  t0
        68, 4, 5, 6, 7,          //  t1
        72, 8, 9, 10, 11,        //  t2
        76, 12, 13, 14, 15,      //  t3
        80, 16, 17, 18, 19,      //  t4
        84, 20, 21, 22, 23,      //  t5
        88, 24, 25, 26, 27,      //  t6
        92, 28, 29, 30, 31,      //  t7
        96, 32, 33, 34, 35,      //  t8
        100, 36, 37, 38, 39,     //  t9
        104, 40, 41, 42, 43,     // t10
        108, 44, 45, 46, 47,     // t11
        112, 48, 49, 50);        // t12 (b0 completed by Token5WeaveShiftedIndices512)

    // Serialize weave, output bytes 16..79, stored at offset +16 (a full-width store in
    // place of a masked tail; entries for bytes 16..63 produce the same values as
    // Token5WeaveIndices512, so the 48-byte overlap is written twice with identical data).
    public static readonly Vector512<byte> Token5WeaveShiftedIndices512 = Vector512.Create(
        (byte)12, 13, 14, 15,    //  t3 payload again (out bytes 16..19)
        80, 16, 17, 18, 19,      //  t4
        84, 20, 21, 22, 23,      //  t5
        88, 24, 25, 26, 27,      //  t6
        92, 28, 29, 30, 31,      //  t7
        96, 32, 33, 34, 35,      //  t8
        100, 36, 37, 38, 39,     //  t9
        104, 40, 41, 42, 43,     // t10
        108, 44, 45, 46, 47,     // t11
        112, 48, 49, 50, 51,     // t12
        116, 52, 53, 54, 55,     // t13
        120, 56, 57, 58, 59,     // t14
        124, 60, 61, 62, 63);    // t15

    // Decode gather. Sources are two OVERLAPPING window loads: first = window bytes
    // 0..63, second = window bytes 16..79 — so window byte p is index p when p < 64
    // and index 64 + (p - 16) = p + 48 when p >= 64. Output dword t = token t's payload
    // (window bytes 5t+1..5t+4) reversed into little-endian: [5t+4, 5t+3, 5t+2, 5t+1].
    public static readonly Vector512<byte> Token5DecodeValues512 = Vector512.Create(
        (byte)4, 3, 2, 1,        //  t0 <- window 4..1
        9, 8, 7, 6,              //  t1 <- window 9..6
        14, 13, 12, 11,          //  t2
        19, 18, 17, 16,          //  t3
        24, 23, 22, 21,          //  t4
        29, 28, 27, 26,          //  t5
        34, 33, 32, 31,          //  t6
        39, 38, 37, 36,          //  t7
        44, 43, 42, 41,          //  t8
        49, 48, 47, 46,          //  t9
        54, 53, 52, 51,          // t10
        59, 58, 57, 56,          // t11
        112, 63, 62, 61,         // t12 <- window 64 (=112), 63..61: straddles the sources
        117, 116, 115, 114,      // t13 <- window 69..66
        122, 121, 120, 119,      // t14 <- window 74..71
        127, 126, 125, 124);     // t15 <- window 79..76

    // Decode validation gather: token t's code byte (window byte 5t) replicated across
    // all four bytes of output dword t, so whole-dword compares against 0xd2d2d2d2 /
    // 0xcececece classify every lane at once.
    public static readonly Vector512<byte> Token5DecodeCodes512 = Vector512.Create(
        (byte)0, 0, 0, 0,        //  t0 <- window 0
        5, 5, 5, 5,              //  t1 <- window 5
        10, 10, 10, 10,          //  t2
        15, 15, 15, 15,          //  t3
        20, 20, 20, 20,          //  t4
        25, 25, 25, 25,          //  t5
        30, 30, 30, 30,          //  t6
        35, 35, 35, 35,          //  t7
        40, 40, 40, 40,          //  t8
        45, 45, 45, 45,          //  t9
        50, 50, 50, 50,          // t10
        55, 55, 55, 55,          // t11
        60, 60, 60, 60,          // t12
        113, 113, 113, 113,      // t13 <- window 65 (second source)
        118, 118, 118, 118,      // t14 <- window 70
        123, 123, 123, 123);     // t15 <- window 75

    // 256/128 serialize tiers (non-VBMI hardware). The payload shuffles and decode
    // gathers are shared with the float tier (identical token shape),
    // only the code handling differs. IntCode256 scatters the sign-selected code dwords
    // (0xce ^ (sign & 0x1c), low byte at in-lane byte 4k, computed on the SAME
    // vpermq-arranged vector as the payload shuffle: lane 0 = elements 0-3, lane 1 =
    // elements 2-5) into token positions 5t. UIntCode256 is the constant-code OR mask
    // for the uint32 weave, where every token is 0xce.
    const byte B = byte.MaxValue; // pshufb-zeroed slot (bit 7 set)
    const byte U = 0xce;          // MessagePackCode.UInt32

    public static readonly Vector256<byte> IntCode256 = Vector256.Create(
        0, B, B, B, B, 4, B, B, B, B, 8, B, B, B, B, 12,
        B, B, B, B, 8, B, B, B, B, 12, B, B, B, B, B, B);
    public static readonly Vector128<byte> IntCode128 = Vector128.Create(
        0, B, B, B, B, 4, B, B, B, B, 8, B, B, B, B, B);
    public static readonly Vector256<byte> UIntCode256 = Vector256.Create(
        U, 0, 0, 0, 0, U, 0, 0, 0, 0, U, 0, 0, 0, 0, U,
        0, 0, 0, 0, U, 0, 0, 0, 0, U, 0, 0, 0, 0, 0, 0);
    public static readonly Vector128<byte> UIntCode128 = Vector128.Create(
        U, 0, 0, 0, 0, U, 0, 0, 0, 0, U, 0, 0, 0, 0, 0);

    // ---- int16 wide tiers (3-byte tokens [0xcd|0xd1][2B BE], 16-token block = 48B) ----

    // 512 serialize: one vpermi2b builds the whole 48-byte run. Source 1 = 16 shorts
    // (LE words at bytes 2t..2t+1), source 2 = the code words (low byte at 2t, index
    // 64 + 2t; for uint16 the source is a 0xcd-filled vector, any index works). Token
    // t = [64+2t, 2t+1, 2t]; the result is stored split 32 + 16 so the garbage upper
    // 16 bytes never touch the buffer.
    public static readonly Vector512<byte> Token3WeaveIndices512 = Vector512.Create(
        (byte)64, 1, 0, 66, 3, 2, 68, 5, 4, 70, 7, 6,
        72, 9, 8, 74, 11, 10, 76, 13, 12, 78, 15, 14,
        80, 17, 16, 82, 19, 18, 84, 21, 20, 86, 23, 22,
        88, 25, 24, 90, 27, 26, 92, 29, 28, 94, 31, 30,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    // 256 serialize (input vpermq'd to lanes [e0-7][e4-11]): payloads of tokens 0-4
    // from lane 0 plus token 5's code at byte 15, token 5's payload and 6-9's tokens
    // from lane 1; the code scatter reads the sign-selected code words' low bytes.
    public static readonly Vector256<byte> Token3WeavePayload256 = Vector256.Create(
        B, 1, 0, B, 3, 2, B, 5, 4, B, 7, 6, B, 9, 8, B,
        3, 2, B, 5, 4, B, 7, 6, B, 9, 8, B, 11, 10, B, B);
    public static readonly Vector256<byte> ShortCode256 = Vector256.Create(
        0, B, B, 2, B, B, 4, B, B, 6, B, B, 8, B, B, 10,
        B, B, 4, B, B, 6, B, B, 8, B, B, 10, B, B, B, B);

    // 128 serialize: 5 tokens from elements 0-4 of an 8-short load (Lo) or elements
    // 2-6 (Hi; used with a load at element 8 to reach elements 10-14 without reading
    // past the block). Each store's 16th byte is the next window's code slot.
    public static readonly Vector128<byte> Token3WeavePayload128 = Vector128.Create(
        B, 1, 0, B, 3, 2, B, 5, 4, B, 7, 6, B, 9, 8, B);
    public static readonly Vector128<byte> ShortCode128 = Vector128.Create(
        0, B, B, 2, B, B, 4, B, B, 6, B, B, 8, B, B, B);
    public static readonly Vector128<byte> Token3WeavePayloadHigh128 = Vector128.Create(
        B, 5, 4, B, 7, 6, B, 9, 8, B, 11, 10, B, 13, 12, B);
    public static readonly Vector128<byte> ShortCodeHigh128 = Vector128.Create(
        4, B, B, 6, B, B, 8, B, B, 10, B, B, 12, B, B, B);

    // uint16 constant-code OR masks (0xcd in every code slot; one 128 table serves the
    // Lo and Hi windows, whose code positions coincide)
    const byte S = 0xcd; // MessagePackCode.UInt16
    public static readonly Vector256<byte> UShortCode256 = Vector256.Create(
        S, 0, 0, S, 0, 0, S, 0, 0, S, 0, 0, S, 0, 0, S,
        0, 0, S, 0, 0, S, 0, 0, S, 0, 0, S, 0, 0, 0, 0);
    public static readonly Vector128<byte> UShortCode128 = Vector128.Create(
        S, 0, 0, S, 0, 0, S, 0, 0, S, 0, 0, S, 0, 0, 0);

    // 512 decode: single-source gathers over a 64B window; word t = window bytes
    // [3t+2, 3t+1] (values) or [3t, 3t] (codes). Only the lower 32 output bytes are
    // meaningful and validation runs on the 256-bit halves.
    public static readonly Vector512<byte> Token3DecodeValues512 = Vector512.Create(
        (byte)2, 1, 5, 4, 8, 7, 11, 10, 14, 13, 17, 16, 20, 19, 23, 22,
        26, 25, 29, 28, 32, 31, 35, 34, 38, 37, 41, 40, 44, 43, 47, 46,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public static readonly Vector512<byte> Token3DecodeCodes512 = Vector512.Create(
        (byte)0, 0, 3, 3, 6, 6, 9, 9, 12, 12, 15, 15, 18, 18, 21, 21,
        24, 24, 27, 27, 30, 30, 33, 33, 36, 36, 39, 39, 42, 42, 45, 45,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    // 256/128 decode: 10 tokens per 30B window (lane 0 decodes tokens 0-4, lane 1
    // tokens 5-9; token 5's payload sits fully inside lane 1) or 5 tokens per 15B.
    // Z-zeroed store tails heal under the next store or the scalar tail.
    const byte Z = 0x80;
    public static readonly Vector256<byte> Token3DecodeShuffle256 = Vector256.Create(
        (byte)2, 1, 5, 4, 8, 7, 11, 10, 14, 13, Z, Z, Z, Z, Z, Z,
        1, 0, 4, 3, 7, 6, 10, 9, 13, 12, Z, Z, Z, Z, Z, Z);
    public static readonly Vector128<byte> Token3DecodeShuffle128 = Vector128.Create(
        (byte)2, 1, 5, 4, 8, 7, 11, 10, 14, 13, Z, Z, Z, Z, Z, Z);

    // ---- int64 wide tiers (9-byte tokens [0xcf|0xd3][8B BE], 8-token block = 72B) ----

    // 512 serialize: tokens 0-6 in one vpermi2b (source 2 = the code qwords, low byte
    // at 8t; a 0xcf fill for uint64), then tokens 1-7 from the same source stored at
    // offset +9, rewriting tokens 1-6 with identical bytes. The second store overhangs
    // the 72-byte run by 1 byte (region reservation slack).
    public static readonly Vector512<byte> Token9WeaveIndices512 = Vector512.Create(
        (byte)64, 7, 6, 5, 4, 3, 2, 1, 0,
        72, 15, 14, 13, 12, 11, 10, 9, 8,
        80, 23, 22, 21, 20, 19, 18, 17, 16,
        88, 31, 30, 29, 28, 27, 26, 25, 24,
        96, 39, 38, 37, 36, 35, 34, 33, 32,
        104, 47, 46, 45, 44, 43, 42, 41, 40,
        112, 55, 54, 53, 52, 51, 50, 49, 48,
        0);
    public static readonly Vector512<byte> Token9WeaveShiftedIndices512 = Vector512.Create(
        (byte)72, 15, 14, 13, 12, 11, 10, 9, 8,
        80, 23, 22, 21, 20, 19, 18, 17, 16,
        88, 31, 30, 29, 28, 27, 26, 25, 24,
        96, 39, 38, 37, 36, 35, 34, 33, 32,
        104, 47, 46, 45, 44, 43, 42, 41, 40,
        112, 55, 54, 53, 52, 51, 50, 49, 48,
        120, 63, 62, 61, 60, 59, 58, 57, 56,
        0);

    // 256 serialize code scatter: the double weave layout (vpermq lanes [e0,e1][e1,e2])
    // with codes at positions 0 and 9 from lane 0 and 18 from lane 1 (element 2's low
    // byte at in-lane 8); uint64 ORs the constant variant instead.
    public static readonly Vector256<byte> LongCode256 = Vector256.Create(
        0, B, B, B, B, B, B, B, B, 8, B, B, B, B, B, B,
        B, B, 8, B, B, B, B, B, B, B, B, B, B, B, B, B);
    const byte L = 0xcf; // MessagePackCode.UInt64
    public static readonly Vector256<byte> ULongCode256 = Vector256.Create(
        L, 0, 0, 0, 0, 0, 0, 0, 0, L, 0, 0, 0, 0, 0, 0,
        0, 0, L, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    // ---- float32/float64 tiers (5-byte [0xca] / 9-byte [0xcb] tokens) ----

    // Rearrange float(4)/double(8) into msgpack's token-prefixed float32(5)/float64(9)
    // and reorder the values into big-endian.
    // The 512 tables put C in the token slots. C selects byte 0 of vpermi2b's SECOND source
    // (bit 6 of the index picks the source), which the caller passes as a vector filled with
    // the code byte, so a single two-source permute emits codes and payloads together.
    // The 256/128 tables put B in the token slots, which pshufb zeroes, and OR the code in after
    // (their shuffles are single-source, so the code cannot ride along the same way).
    // For 512 float only, the leading token is written outside, so the indices are shifted

    const byte C = 64;            // code slot, byte 0 of the code-fill second source of vpermi2b

    public static readonly Vector512<byte> FloatWeaveIndices512 = Vector512.Create(
        (byte)3, 2, 1, 0, C, 7, 6, 5, 4, C, 11, 10, 9, 8, C, 15, 14, 13, 12, C,
        19, 18, 17, 16, C, 23, 22, 21, 20, C, 27, 26, 25, 24, C, 31, 30, 29, 28, C,
        35, 34, 33, 32, C, 39, 38, 37, 36, C, 43, 42, 41, 40, C, 47, 46, 45, 44, C,
        51, 50, 49, 48);
    public static readonly Vector512<byte> DoubleWeaveIndices512 = Vector512.Create(
        C, 7, 6, 5, 4, 3, 2, 1, 0, C, 15, 14, 13, 12, 11, 10, 9, 8,
        C, 23, 22, 21, 20, 19, 18, 17, 16, C, 31, 30, 29, 28, 27, 26, 25, 24,
        C, 39, 38, 37, 36, 35, 34, 33, 32, C, 47, 46, 45, 44, 43, 42, 41, 40,
        C, 55, 54, 53, 52, 51, 50, 49, 48, C);

    // AVX2 tier (vpshufb is in-lane, so Permute4x64(0b10_01_01_00) first duplicates the
    // middle qwords: float lanes hold [f0..f3][f2..f5], double lanes [d0,d1][d1,d2]).
    // Float: 6 tokens per 32B store, 30B advance (2B overhang). Double: 3 tokens, 27B
    // advance (5B overhang). The loop guards require 8/4 readable but consume 6/3, so the
    // scalar tail always covers the overhang bytes within the region reservation.
    public static readonly Vector256<byte> Token5WeaveIndices256 = Vector256.Create(
        B, 3, 2, 1, 0, B, 7, 6, 5, 4, B, 11, 10, 9, 8, B,
        7, 6, 5, 4, B, 11, 10, 9, 8, B, 15, 14, 13, 12, B, B);
    public static readonly Vector256<byte> FloatCode256 = Vector256.Create(
        MessagePackCode.Float32, 0, 0, 0, 0, MessagePackCode.Float32, 0, 0, 0, 0,
        MessagePackCode.Float32, 0, 0, 0, 0, MessagePackCode.Float32, 0, 0, 0, 0,
        MessagePackCode.Float32, 0, 0, 0, 0, MessagePackCode.Float32, 0, 0, 0, 0, 0, 0);
    public static readonly Vector256<byte> Token9WeaveIndices256 = Vector256.Create(
        B, 7, 6, 5, 4, 3, 2, 1, 0, B, 15, 14, 13, 12, 11, 10,
        1, 0, B, 15, 14, 13, 12, 11, 10, 9, 8, B, B, B, B, B);
    public static readonly Vector256<byte> DoubleCode256 = Vector256.Create(
        MessagePackCode.Float64, 0, 0, 0, 0, 0, 0, 0, 0, MessagePackCode.Float64,
        0, 0, 0, 0, 0, 0, 0, 0, MessagePackCode.Float64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    // Vector128 tier. Ssse3.Shuffle zeroes lanes whose index has the high bit set; the
    // portable fallback (NEON tbl also zeroes, but stay explicit) shuffles with in-range
    // indices and applies the mask. Float: 3 tokens per 16B store, 15B advance (1B
    // overhang). Double: 2 tokens, exact 18B (code and the last payload byte go scalar).
    public static readonly Vector128<byte> Token5WeaveIndices128 = Vector128.Create(
        B, 3, 2, 1, 0, B, 7, 6, 5, 4, B, 11, 10, 9, 8, B);
    public static readonly Vector128<byte> Token5WeaveIndices128Portable = Vector128.Create(
        (byte)0, 3, 2, 1, 0, 0, 7, 6, 5, 4, 0, 11, 10, 9, 8, 0);
    public static readonly Vector128<byte> Token5WeaveMask128 = Vector128.Create(
        0, B, B, B, B, 0, B, B, B, B, 0, B, B, B, B, 0);
    public static readonly Vector128<byte> FloatCode128 = Vector128.Create(
        MessagePackCode.Float32, 0, 0, 0, 0, MessagePackCode.Float32, 0, 0, 0, 0,
        MessagePackCode.Float32, 0, 0, 0, 0, 0);
    public static readonly Vector128<byte> Token9WeaveIndices128 = Vector128.Create(
        7, 6, 5, 4, 3, 2, 1, 0, B, 15, 14, 13, 12, 11, 10, 9);
    public static readonly Vector128<byte> Token9WeaveIndices128Portable = Vector128.Create(
        (byte)7, 6, 5, 4, 3, 2, 1, 0, 0, 15, 14, 13, 12, 11, 10, 9);
    public static readonly Vector128<byte> Token9WeaveMask128 = Vector128.Create(
        B, B, B, B, B, B, B, B, 0, B, B, B, B, B, B, B);
    public static readonly Vector128<byte> DoubleCode128 = Vector128.Create(
        0, 0, 0, 0, 0, 0, 0, 0, MessagePackCode.Float64, 0, 0, 0, 0, 0, 0, 0);

    // Decode gather for float64 (single-source vpermb over a 64B window = 7 tokens + 1
    // slack byte). Qword t = window bytes [9t+8 .. 9t+1] (BE payload reversed to LE);
    // lane 7 is a garbage qword stored into the destination and decoded again by the
    // next iteration or the scalar tail. The validation gather replicates each token's
    // code byte across its qword; lane 7 replicates token0's code, so the all-0xcb
    // compare stays consistent. (float32 decode reuses the shared Token5 decode
    // gathers: the token shape [code][4B BE] is identical.)
    public static readonly Vector512<byte> Token9DecodeValues512 = Vector512.Create(
        (byte)8, 7, 6, 5, 4, 3, 2, 1,
        17, 16, 15, 14, 13, 12, 11, 10,
        26, 25, 24, 23, 22, 21, 20, 19,
        35, 34, 33, 32, 31, 30, 29, 28,
        44, 43, 42, 41, 40, 39, 38, 37,
        53, 52, 51, 50, 49, 48, 47, 46,
        62, 61, 60, 59, 58, 57, 56, 55,
        0, 0, 0, 0, 0, 0, 0, 0);
    public static readonly Vector512<byte> Token9DecodeCodes512 = Vector512.Create(
        (byte)0, 0, 0, 0, 0, 0, 0, 0,
        9, 9, 9, 9, 9, 9, 9, 9,
        18, 18, 18, 18, 18, 18, 18, 18,
        27, 27, 27, 27, 27, 27, 27, 27,
        36, 36, 36, 36, 36, 36, 36, 36,
        45, 45, 45, 45, 45, 45, 45, 45,
        54, 54, 54, 54, 54, 54, 54, 54,
        0, 0, 0, 0, 0, 0, 0, 0);

    // Decode shuffles for the non-VBMI tiers (Z = pshufb zeroing index; the zeroed or
    // junk tail bytes of every store are rewritten by the next store or the scalar
    // tail, so the portable variants may substitute index 0). Float 256: lane 0
    // reverses tokens 0-2 (window bytes 1-14), lane 1 the payloads of tokens 3-5
    // (bytes 16-29; token 3's code sits exactly on the lane boundary at byte 15).
    // Double 256: A covers tokens 0 (lane 0) and 2 (lane 1); B covers token 1, whose
    // payload straddles the lane boundary, out of a vpermq'd q1q2 lane.

    public static readonly Vector256<byte> Token5DecodeShuffle256 = Vector256.Create(
        (byte)4, 3, 2, 1, 9, 8, 7, 6, 14, 13, 12, 11, Z, Z, Z, Z,
        3, 2, 1, 0, 8, 7, 6, 5, 13, 12, 11, 10, Z, Z, Z, Z);
    public static readonly Vector256<byte> Token9DecodeShuffleA256 = Vector256.Create(
        (byte)8, 7, 6, 5, 4, 3, 2, 1, Z, Z, Z, Z, Z, Z, Z, Z,
        10, 9, 8, 7, 6, 5, 4, 3, Z, Z, Z, Z, Z, Z, Z, Z);
    public static readonly Vector256<byte> Token9DecodeShuffleB256 = Vector256.Create(
        Z, Z, Z, Z, Z, Z, Z, Z, (byte)9, 8, 7, 6, 5, 4, 3, 2,
        Z, Z, Z, Z, Z, Z, Z, Z, Z, Z, Z, Z, Z, Z, Z, Z);
    public static readonly Vector128<byte> Token5DecodeShuffle128 = Vector128.Create(
        (byte)4, 3, 2, 1, 9, 8, 7, 6, 14, 13, 12, 11, Z, Z, Z, Z);
    public static readonly Vector128<byte> Token5DecodeShuffle128Portable = Vector128.Create(
        (byte)4, 3, 2, 1, 9, 8, 7, 6, 14, 13, 12, 11, 0, 0, 0, 0);
    public static readonly Vector128<byte> Token9DecodeShuffle128 = Vector128.Create(
        (byte)8, 7, 6, 5, 4, 3, 2, 1, Z, Z, Z, Z, Z, Z, Z, Z);
    public static readonly Vector128<byte> Token9DecodeShuffle128Portable = Vector128.Create(
        (byte)8, 7, 6, 5, 4, 3, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0);
}
#endif

/// <summary>
/// int32 element core.
/// </summary>
internal readonly struct Int32ElementCodec : IElementCodec<int>
{
    // Serialize reserves worst case (5 bytes/element) for a region up front and emits into
    // it with a register-accumulated offset, one Advance per region. Everything inside the
    // region loop is pure register arithmetic — per-element buffer calls pay byref
    // field-reload traffic (index/capacity/pointer re-read from memory each iteration;
    // large structs passed by ref don't get promoted), measured ~2x the cost of the store
    // itself. The cap bounds the reservation for huge arrays (4096 * 5 = 20KB).
    internal const int SerializeRegionElements = 4096;

    public void WriteElements<TWriteBuffer>(ref TWriteBuffer buffer, ReadOnlySpan<int> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref int src = ref MemoryMarshal.GetReference(source);
        int length = source.Length;

        int i = 0;
        while (i < length)
        {
            int regionEnd = i + Math.Min(length - i, SerializeRegionElements);
            // +2: the AVX2 wide weave's last 32B store overhangs the 80-byte token run
            // by 2 bytes, which lands past the worst case when the block ends the region
            ref byte d = ref buffer.GetReference(((regionEnd - i) * MessagePackPrimitives.MaxInt32Length) + 2);
            int written = 0;
#if NET9_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && Avx512F.IsSupported)
            {
                while (regionEnd - i >= 16)
                {
                    // superlane: 16 elements all in fixint range [-32, 127] become one
                    // truncating narrow int32 -> int8 (vpmovdb), one byte per element
                    var v = Vector512.LoadUnsafe(ref src, (nuint)i);
                    var biased = (v + Vector512.Create(32)).AsUInt32();
                    if (Vector512.LessThanOrEqualAll(biased, Vector512.Create(159u)))
                    {
                        Avx512F.ConvertToVector128SByte(v).AsByte().StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    else if (Vector512.GreaterThanOrEqualAll((v + Vector512.Create(32768)).AsUInt32(), Vector512.Create(98304u)))
                    {
                        // wide superlane: all 16 need 5-byte tokens (v < -32768 or v > 65535,
                        // i.e. biased v+32768 outside [0, 98304)). Realistic arrays are
                        // magnitude-homogeneous, so all-wide runs are the common complement
                        // of all-fixint runs.
                        if (Avx512Vbmi.IsSupported)
                        {
                            // code = 0xd2 (int32) for negative, 0xce (uint32) otherwise,
                            // picked branchlessly via the sign mask (0xce ^ 0x1c = 0xd2);
                            // two vpermi2b weave code bytes + byte-reversed payloads into
                            // the 80-byte token run (stores overlap by 16 bytes, same data)
                            var be = Vector512.Shuffle(v.AsByte(), WeaveTables.Token5PayloadReverse512);
                            var codes = (Vector512.Create(0x000000ce) ^ (Vector512.ShiftRightArithmetic(v, 31) & Vector512.Create(0x0000001c))).AsByte();
                            Avx512Vbmi.PermuteVar64x8x2(be, WeaveTables.Token5WeaveIndices512, codes).StoreUnsafe(ref Unsafe.Add(ref d, written));
                            Avx512Vbmi.PermuteVar64x8x2(be, WeaveTables.Token5WeaveShiftedIndices512, codes).StoreUnsafe(ref Unsafe.Add(ref d, written + 16));
                        }
                        else
                        {
                            EmitWide16(ref d, written, ref src, i);
                        }
                        written += 80;
                        i += 16;
                    }
                    else
                    {
                        // mixed 16: scalar classify chain, then re-probe the superlanes
                        int end = i + 16;
                        for (; i < end; i++)
                        {
                            written += MessagePackPrimitives.UnsafeWriteInt32(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                // AVX2 tier (current consumer Intel): same shape, two 256-bit probes
                while (regionEnd - i >= 16)
                {
                    var a = Vector256.LoadUnsafe(ref src, (nuint)i);
                    var b = Vector256.LoadUnsafe(ref src, (nuint)(i + 8));
                    var loFix = (a + Vector256.Create(32)).AsUInt32();
                    var hiFix = (b + Vector256.Create(32)).AsUInt32();
                    if (Vector256.LessThanOrEqualAll(Vector256.Max(loFix, hiFix), Vector256.Create(159u)))
                    {
                        // values verified in [-32,127]: truncating and saturating narrows
                        // agree; portable Narrow fixes up AVX2's per-lane pack order
                        var packed = Vector256.Narrow(a, b);
                        Vector256.Narrow(packed, packed).GetLower().AsByte().StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    else if (Vector256.GreaterThanOrEqualAll(
                        Vector256.Min((a + Vector256.Create(32768)).AsUInt32(), (b + Vector256.Create(32768)).AsUInt32()),
                        Vector256.Create(98304u)))
                    {
                        if (Avx2.IsSupported)
                        {
                            // 16 tokens in three overlapping 6-token weave windows
                            // (measured 9.1x over EmitWide16): t0-5, t6-11, t10-15. The
                            // middle store's 2 garbage bytes are rewritten by the last
                            // store; the shifted vpermq (lane 0 = elements 10-13, lane 1
                            // = 12-15) lets the same shuffle tables serve the last window,
                            // rewriting tokens 10-11 with identical bytes. The last store
                            // overhangs the run by 2 bytes (covered by reservation slack).
                            var p0 = Avx2.Permute4x64(a.AsUInt64(), 0b10_01_01_00).AsInt32();
                            var p1 = Avx2.Permute4x64(Vector256.LoadUnsafe(ref src, (nuint)(i + 4)).AsUInt64(), 0b11_10_10_01).AsInt32();
                            var p2 = Avx2.Permute4x64(b.AsUInt64(), 0b11_10_10_01).AsInt32();
                            WeaveWide6(p0).StoreUnsafe(ref Unsafe.Add(ref d, written));
                            WeaveWide6(p1).StoreUnsafe(ref Unsafe.Add(ref d, written + 30));
                            WeaveWide6(p2).StoreUnsafe(ref Unsafe.Add(ref d, written + 50));
                        }
                        else
                        {
                            EmitWide16(ref d, written, ref src, i);
                        }
                        written += 80;
                        i += 16;
                    }
                    else
                    {
                        int end = i + 16;
                        for (; i < end; i++)
                        {
                            written += MessagePackPrimitives.UnsafeWriteInt32(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                // portable tier (ARM NEON / SSE2): same shape, 4x128-bit probe
                var bias = Vector128.Create(32);
                var limit = Vector128.Create(159u);
                while (regionEnd - i >= 16)
                {
                    var a = Vector128.LoadUnsafe(ref src, (nuint)i);
                    var b = Vector128.LoadUnsafe(ref src, (nuint)(i + 4));
                    var c = Vector128.LoadUnsafe(ref src, (nuint)(i + 8));
                    var e = Vector128.LoadUnsafe(ref src, (nuint)(i + 12));
                    var max = Vector128.Max(
                        Vector128.Max((a + bias).AsUInt32(), (b + bias).AsUInt32()),
                        Vector128.Max((c + bias).AsUInt32(), (e + bias).AsUInt32()));
                    if (Vector128.LessThanOrEqualAll(max, limit))
                    {
                        // values verified in [-32,127]: truncating and saturating narrows
                        // agree, so platform lowering (xtn vs pack) doesn't matter
                        var packed = Vector128.Narrow(Vector128.Narrow(a, b), Vector128.Narrow(c, e));
                        packed.AsByte().StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    else
                    {
                        var wideBias = Vector128.Create(32768);
                        var min = Vector128.Min(
                            Vector128.Min((a + wideBias).AsUInt32(), (b + wideBias).AsUInt32()),
                            Vector128.Min((c + wideBias).AsUInt32(), (e + wideBias).AsUInt32()));
                        if (Vector128.GreaterThanOrEqualAll(min, Vector128.Create(98304u)))
                        {
                            if (Ssse3.IsSupported)
                            {
                                // five 3-token weave windows plus one scalar token
                                // (measured 3.6x over EmitWide16): each store's 1
                                // overhang byte is the next window's code slot, the
                                // fifth store's garbage byte at written + 75 is
                                // rewritten by token 15's scalar code. Exact 80 bytes.
                                for (int t = 0; t < 5; t++)
                                {
                                    var v = Vector128.LoadUnsafe(ref src, (nuint)(i + (t * 3)));
                                    var codes = (Vector128.Create(0x000000ce) ^ (Vector128.ShiftRightArithmetic(v, 31) & Vector128.Create(0x0000001c))).AsByte();
                                    (Ssse3.Shuffle(v.AsByte(), WeaveTables.Token5WeaveIndices128) | Ssse3.Shuffle(codes, WeaveTables.IntCode128))
                                        .StoreUnsafe(ref Unsafe.Add(ref d, written + (t * 15)));
                                }
                                int v15 = Unsafe.Add(ref src, i + 15);
                                Unsafe.Add(ref d, written + 75) = (byte)(MessagePackCode.UInt32 ^ ((v15 >> 31) & 0x1c));
                                Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + 76), MessagePackEndian.ToBigEndian((uint)v15));
                            }
                            else
                            {
                                EmitWide16(ref d, written, ref src, i);
                            }
                            written += 80;
                            i += 16;
                        }
                        else
                        {
                            int end = i + 16;
                            for (; i < end; i++)
                            {
                                written += MessagePackPrimitives.UnsafeWriteInt32(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                            }
                        }
                    }
                }
            }
#endif
            // non-SIMD hardware bulk, and the sub-16 tail everywhere
            for (; i < regionEnd; i++)
            {
                written += MessagePackPrimitives.UnsafeWriteInt32(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
            }
            buffer.Advance(written);
        }
    }

    /// <summary>
    /// Wide-run emit for tiers without vpermi2b: all 16 elements are verified 5-byte
    /// tokens, so the stride is CONSTANT — every offset is written + 5t, which removes
    /// both the classify chain and the offset serial dependency; plain scalar stores
    /// run at full ILP. No shuffle hardware needed.
    /// </summary>
    static void EmitWide16(ref byte d, int written, ref int src, int i)
    {
        for (int t = 0; t < 16; t++)
        {
            int v = Unsafe.Add(ref src, i + t);
            // 0xce ^ 0x1c = 0xd2: sign-mask xor selects int32 vs uint32 with pure ALU.
            // A ternary here compiles to a branch or a cmov at the JIT's (PGO's)
            // discretion, and the branch flavor eats a ~50% mispredict per element on
            // random-sign data — measured 4.3 vs 0.8 ns/elem for the same loop
            Unsafe.Add(ref d, written + (t * 5)) = (byte)(MessagePackCode.UInt32 ^ ((v >> 31) & 0x1c));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + (t * 5) + 1), MessagePackEndian.ToBigEndian((uint)v));
        }
    }

#if NET9_0_OR_GREATER
    /// <summary>
    /// 6 tokens from a vpermq-arranged vector (lane 0 = elements 0-3, lane 1 = 2-5):
    /// the shared float payload shuffle reverses into the payload slots, and a second
    /// shuffle scatters the sign-selected code dwords into the code slots.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector256<byte> WeaveWide6(Vector256<int> permuted)
    {
        var codes = (Vector256.Create(0x000000ce) ^ (Vector256.ShiftRightArithmetic(permuted, 31) & Vector256.Create(0x0000001c))).AsByte();
        return Avx2.Shuffle(permuted.AsByte(), WeaveTables.Token5WeaveIndices256) | Avx2.Shuffle(codes, WeaveTables.IntCode256);
    }
#endif

    public void ReadElements<TReadBuffer>(ref TReadBuffer buffer, Span<int> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref int dst = ref MemoryMarshal.GetReference(destination);
        int count = destination.Length;

        int i = 0;
#if NET9_0_OR_GREATER
        if (Vector128.IsHardwareAccelerated)
        {
            while (count - i >= 16)
            {
                // fixint elements are exactly 1 byte, so 16 elements = the next 16 bytes
                // of the window. A window shorter than 16 (buffer tail or sequence
                // segment boundary) takes the scalar chunk, whose per-element reader
                // stitches across the boundary; SIMD resumes on the next window.
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 16)
                {
                    var codes = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    // all 16 codes fixint? (b + 0x20) wraps 0xe0-0xff to 0x00-0x1f
                    var biased = codes + Vector128.Create((byte)0x20);
                    if (Vector128.LessThanOrEqualAll(biased, Vector128.Create((byte)0x9f)))
                    {
                        if (Vector512.IsHardwareAccelerated && Avx512F.IsSupported)
                        {
                            // sign-extend 16 bytes to 16 ints in one instruction (vpmovsxbd)
                            Avx512F.ConvertToVector512Int32(codes.AsSByte()).StoreUnsafe(ref dst, (nuint)i);
                        }
                        else if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
                        {
                            // AVX2 tier: vpmovsxbw then two vpmovsxwd
                            var s = Avx2.ConvertToVector256Int16(codes.AsSByte());
                            Avx2.ConvertToVector256Int32(s.GetLower()).StoreUnsafe(ref dst, (nuint)i);
                            Avx2.ConvertToVector256Int32(s.GetUpper()).StoreUnsafe(ref dst, (nuint)(i + 8));
                        }
                        else
                        {
                            // portable tier: widen sbyte -> short -> int (NEON: sshll/sshll2)
                            var (lo, hi) = Vector128.Widen(codes.AsSByte());
                            var (i0, i1) = Vector128.Widen(lo);
                            var (i2, i3) = Vector128.Widen(hi);
                            i0.StoreUnsafe(ref dst, (nuint)i);
                            i1.StoreUnsafe(ref dst, (nuint)(i + 4));
                            i2.StoreUnsafe(ref dst, (nuint)(i + 8));
                            i3.StoreUnsafe(ref dst, (nuint)(i + 12));
                        }
                        buffer.Advance(16);
                        i += 16;
                        continue;
                    }

                    // wide superlane: 16 tokens of exactly [0xd2|0xce][4B BE] = the next
                    // 80 bytes, fixed stride. Any gate failure (other codes, non-minimal
                    // widths mixed in, a uint32 above int.MaxValue) falls to the scalar
                    // reader, which keeps the exact exception behavior and accepts
                    // non-minimal encodings as before.
                    if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported && window.Length >= 80)
                    {
                        // one vpermi2b gathers the payloads already byte-swapped into LE
                        // dwords, another replicates each code byte across its dword for
                        // vectorized validation
                        ref byte w0 = ref MemoryMarshal.GetReference(window);
                        var lo512 = Vector512.LoadUnsafe(ref w0);
                        var hi512 = Vector512.LoadUnsafe(ref Unsafe.Add(ref w0, 16));
                        var vals = Avx512Vbmi.PermuteVar64x8x2(lo512, WeaveTables.Token5DecodeValues512, hi512).AsInt32();
                        var codes4 = Avx512Vbmi.PermuteVar64x8x2(lo512, WeaveTables.Token5DecodeCodes512, hi512).AsInt32();
                        var isInt32 = Vector512.Equals(codes4, Vector512.Create(unchecked((int)0xd2d2d2d2)));
                        var isUInt32 = Vector512.Equals(codes4, Vector512.Create(unchecked((int)0xcececece)));
                        if (Vector512.EqualsAll(isInt32 | isUInt32, Vector512<int>.AllBitsSet)
                            && Vector512.GreaterThanOrEqualAll(isUInt32 & vals, Vector512<int>.Zero))
                        {
                            vals.StoreUnsafe(ref dst, (nuint)i);
                            buffer.Advance(80);
                            i += 16;
                            continue;
                        }
                    }
                    else if (Avx2.IsSupported && window.Length >= 32)
                    {
                        // 6 tokens per 32B window (measured 4.7x over TryDecodeWide16):
                        // code validity is (0xce | 0xd2) at the six code positions; the
                        // uint-overflow test shifts the 0xce positions onto the following
                        // payload byte's movemask bit, since a 0xce token with its
                        // payload MSB set must reach the scalar reader to throw. The
                        // stores' garbage tails land in dst slots decoded by the next
                        // iteration or the scalar tail.
                        var w = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        uint ce = Vector256.Equals(w, Vector256.Create(MessagePackCode.UInt32)).ExtractMostSignificantBits();
                        uint d2 = Vector256.Equals(w, Vector256.Create(MessagePackCode.Int32)).ExtractMostSignificantBits();
                        uint msb = w.ExtractMostSignificantBits();
                        if (((ce | d2) & 0x2108421u) == 0x2108421u && (((ce & 0x2108421u) << 1) & msb) == 0)
                        {
                            var shuffled = Avx2.Shuffle(w, WeaveTables.Token5DecodeShuffle256);
                            shuffled.GetLower().StoreUnsafe(ref Unsafe.As<int, byte>(ref dst), (nuint)i * 4);
                            shuffled.GetUpper().StoreUnsafe(ref Unsafe.As<int, byte>(ref dst), ((nuint)i * 4) + 12);
                            buffer.Advance(30);
                            i += 6;
                            continue;
                        }
                    }
                    else if (Ssse3.IsSupported && window.Length >= 16)
                    {
                        // 3 tokens per 16B window, same gate at 128-bit width
                        var w = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        uint ce = Vector128.Equals(w, Vector128.Create(MessagePackCode.UInt32)).ExtractMostSignificantBits();
                        uint d2 = Vector128.Equals(w, Vector128.Create(MessagePackCode.Int32)).ExtractMostSignificantBits();
                        uint msb = w.ExtractMostSignificantBits();
                        if (((ce | d2) & 0x421u) == 0x421u && (((ce & 0x421u) << 1) & msb) == 0)
                        {
                            Ssse3.Shuffle(w, WeaveTables.Token5DecodeShuffle128)
                                .StoreUnsafe(ref Unsafe.As<int, byte>(ref dst), (nuint)i * 4);
                            buffer.Advance(15);
                            i += 3;
                            continue;
                        }
                    }
                    else if (window.Length >= 80
                        && TryDecodeWide16(ref MemoryMarshal.GetReference(window), ref dst, i))
                    {
                        buffer.Advance(80);
                        i += 16;
                        continue;
                    }
                }

                // mixed chunk or short window: scalar for these 16, then try SIMD again
                int end = i + 16;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadInt32();
                }
            }
        }
#endif
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadInt32();
        }
    }

    /// <summary>
    /// Wide-run decode for tiers without vpermi2b: constant-stride code probe plus
    /// constant-stride byte-swapped payload loads, validation accumulated branchlessly
    /// (every lane int32/uint32, no uint32 above int.MaxValue — an oversized uint32
    /// must reach the scalar reader to throw). Values are stored before validation —
    /// on a failed gate the caller's scalar reader re-reads the unadvanced buffer and
    /// overwrites (or throws), so partial writes are unobservable.
    /// </summary>
    static bool TryDecodeWide16(ref byte w0, ref int dst, int i)
    {
        // On mixed data this gate fails constantly, so misfires must be cheap: probing
        // just the first two codes kills ~94% of them for two loads (a full 16-code
        // pre-pass made mixed cheap but taxed the all-wide success path ~40%; a single
        // fused pass taxed mixed 1.5x — this is the measured middle).
        if (!IsWideCode(Unsafe.Add(ref w0, 0)) || !IsWideCode(Unsafe.Add(ref w0, 5)))
        {
            return false;
        }
        bool ok = true;
        bool uintOverflow = false;
        for (int t = 0; t < 16; t++)
        {
            byte c = Unsafe.Add(ref w0, t * 5);
            int v = (int)MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref w0, (t * 5) + 1)));
            Unsafe.Add(ref dst, i + t) = v;
            ok &= (c == MessagePackCode.Int32) | (c == MessagePackCode.UInt32);
            uintOverflow |= (c == MessagePackCode.UInt32) & (v < 0);
        }
        return ok && !uintOverflow;

        static bool IsWideCode(byte c) => (c == MessagePackCode.Int32) | (c == MessagePackCode.UInt32);
    }
}

/// <summary>
/// sbyte element core. A fixint token is exactly the value's own two's-complement byte,
/// so a 16-lane run with no element in [-128, -33] copies verbatim in BOTH directions —
/// the gate is the same biased-range compare as the int32 fixint superlane. Runs
/// containing int8 tokens (0xd0, 2 bytes) fall to the scalar ladder.
/// </summary>
internal readonly struct SByteElementCodec : IElementCodec<sbyte>
{
    internal const int SerializeRegionElements = 8192; // * 2B = 16KB

    public void WriteElements<TWriteBuffer>(ref TWriteBuffer buffer, ReadOnlySpan<sbyte> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref sbyte src = ref MemoryMarshal.GetReference(source);
        int length = source.Length;

        int i = 0;
        while (i < length)
        {
            int regionEnd = i + Math.Min(length - i, SerializeRegionElements);
            ref byte d = ref buffer.GetReference((regionEnd - i) * MessagePackPrimitives.MaxInt8Length);
            int written = 0;
#if NET9_0_OR_GREATER
            if (Vector128.IsHardwareAccelerated)
            {
                // width cascade: probe the widest all-fixint run first; a mixed wide chunk
                // falls to the next width, which localizes the stragglers, and finally to
                // the scalar-16 chunk. The gate is the same at every width: (b + 0x20)
                // wraps negative fixints to 0x00-0x1f, leaves 0x00-0x7f at 0x20-0x9f, and
                // pushes int8 territory above 0x9f; a passing run stores verbatim.
                while (regionEnd - i >= 16)
                {
                    if (Vector512.IsHardwareAccelerated && regionEnd - i >= 64)
                    {
                        var v64 = Vector512.LoadUnsafe(ref Unsafe.As<sbyte, byte>(ref src), (nuint)i);
                        if (Vector512.LessThanOrEqualAll(v64 + Vector512.Create((byte)0x20), Vector512.Create((byte)0x9f)))
                        {
                            v64.StoreUnsafe(ref Unsafe.Add(ref d, written));
                            written += 64;
                            i += 64;
                            continue;
                        }
                    }
                    if (Vector256.IsHardwareAccelerated && regionEnd - i >= 32)
                    {
                        var v32 = Vector256.LoadUnsafe(ref Unsafe.As<sbyte, byte>(ref src), (nuint)i);
                        if (Vector256.LessThanOrEqualAll(v32 + Vector256.Create((byte)0x20), Vector256.Create((byte)0x9f)))
                        {
                            v32.StoreUnsafe(ref Unsafe.Add(ref d, written));
                            written += 32;
                            i += 32;
                            continue;
                        }
                    }
                    var v = Vector128.LoadUnsafe(ref Unsafe.As<sbyte, byte>(ref src), (nuint)i);
                    var biased = v + Vector128.Create((byte)0x20);
                    if (Vector128.LessThanOrEqualAll(biased, Vector128.Create((byte)0x9f)))
                    {
                        v.StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    else
                    {
                        // int8 stragglers mixed in: scalar for these 16, then re-probe
                        int end = i + 16;
                        for (; i < end; i++)
                        {
                            written += MessagePackPrimitives.UnsafeWriteSByte(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
#endif
            for (; i < regionEnd; i++)
            {
                written += MessagePackPrimitives.UnsafeWriteSByte(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
            }
            buffer.Advance(written);
        }
    }

    public void ReadElements<TReadBuffer>(ref TReadBuffer buffer, Span<sbyte> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref sbyte dst = ref MemoryMarshal.GetReference(destination);
        int count = destination.Length;

        int i = 0;
#if NET9_0_OR_GREATER
        if (Vector128.IsHardwareAccelerated)
        {
            // same width cascade as the serialize side: widest all-fixint window first
            while (count - i >= 16)
            {
                var window = buffer.GetCurrentSpan();
                if (Vector512.IsHardwareAccelerated && count - i >= 64 && window.Length >= 64)
                {
                    var codes64 = Vector512.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if (Vector512.LessThanOrEqualAll(codes64 + Vector512.Create((byte)0x20), Vector512.Create((byte)0x9f)))
                    {
                        codes64.StoreUnsafe(ref Unsafe.As<sbyte, byte>(ref dst), (nuint)i);
                        buffer.Advance(64);
                        i += 64;
                        continue;
                    }
                }
                if (Vector256.IsHardwareAccelerated && count - i >= 32 && window.Length >= 32)
                {
                    var codes32 = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if (Vector256.LessThanOrEqualAll(codes32 + Vector256.Create((byte)0x20), Vector256.Create((byte)0x9f)))
                    {
                        codes32.StoreUnsafe(ref Unsafe.As<sbyte, byte>(ref dst), (nuint)i);
                        buffer.Advance(32);
                        i += 32;
                        continue;
                    }
                }
                if (window.Length >= 16)
                {
                    var codes = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    var biased = codes + Vector128.Create((byte)0x20);
                    if (Vector128.LessThanOrEqualAll(biased, Vector128.Create((byte)0x9f)))
                    {
                        // 16 fixint codes ARE the 16 sbyte values
                        codes.StoreUnsafe(ref Unsafe.As<sbyte, byte>(ref dst), (nuint)i);
                        buffer.Advance(16);
                        i += 16;
                        continue;
                    }
                }
                // mixed chunk or short window: scalar for these 16, then try SIMD again
                int end = i + 16;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadSByte();
                }
            }
        }
#endif
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadSByte();
        }
    }
}

/// <summary>
/// byte element core (uint8 tokens). byte[] itself is bin-format, so this core serves only the
/// byte-backed enum collections. A positive fixint token is the value byte itself, so a 16-lane
/// run with every element &lt;= 0x7f copies verbatim in BOTH directions; runs containing uint8
/// tokens (0xcc, 2 bytes) fall to the scalar ladder.
/// </summary>
internal readonly struct ByteElementCodec : IElementCodec<byte>
{
    internal const int SerializeRegionElements = 8192; // * 2B = 16KB

    public void WriteElements<TWriteBuffer>(ref TWriteBuffer buffer, ReadOnlySpan<byte> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref byte src = ref MemoryMarshal.GetReference(source);
        int length = source.Length;

        int i = 0;
        while (i < length)
        {
            int regionEnd = i + Math.Min(length - i, SerializeRegionElements);
            ref byte d = ref buffer.GetReference((regionEnd - i) * MessagePackPrimitives.MaxUInt8Length);
            int written = 0;
#if NET9_0_OR_GREATER
            if (Vector128.IsHardwareAccelerated)
            {
                // same width cascade as SByteElementCodec; the gate is simpler because the
                // fixint window is the unsigned range 0x00-0x7f: a passing run stores verbatim
                while (regionEnd - i >= 16)
                {
                    if (Vector512.IsHardwareAccelerated && regionEnd - i >= 64)
                    {
                        var v64 = Vector512.LoadUnsafe(ref src, (nuint)i);
                        if (Vector512.LessThanOrEqualAll(v64, Vector512.Create((byte)0x7f)))
                        {
                            v64.StoreUnsafe(ref Unsafe.Add(ref d, written));
                            written += 64;
                            i += 64;
                            continue;
                        }
                    }
                    if (Vector256.IsHardwareAccelerated && regionEnd - i >= 32)
                    {
                        var v32 = Vector256.LoadUnsafe(ref src, (nuint)i);
                        if (Vector256.LessThanOrEqualAll(v32, Vector256.Create((byte)0x7f)))
                        {
                            v32.StoreUnsafe(ref Unsafe.Add(ref d, written));
                            written += 32;
                            i += 32;
                            continue;
                        }
                    }
                    var v = Vector128.LoadUnsafe(ref src, (nuint)i);
                    if (Vector128.LessThanOrEqualAll(v, Vector128.Create((byte)0x7f)))
                    {
                        v.StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    else
                    {
                        // uint8 stragglers mixed in: scalar for these 16, then re-probe
                        int end = i + 16;
                        for (; i < end; i++)
                        {
                            written += MessagePackPrimitives.UnsafeWriteByte(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
#endif
            for (; i < regionEnd; i++)
            {
                written += MessagePackPrimitives.UnsafeWriteByte(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
            }
            buffer.Advance(written);
        }
    }

    public void ReadElements<TReadBuffer>(ref TReadBuffer buffer, Span<byte> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref byte dst = ref MemoryMarshal.GetReference(destination);
        int count = destination.Length;

        int i = 0;
#if NET9_0_OR_GREATER
        if (Vector128.IsHardwareAccelerated)
        {
            // widest all-positive-fixint window first; 16 fixint codes ARE the 16 byte values
            while (count - i >= 16)
            {
                var window = buffer.GetCurrentSpan();
                if (Vector512.IsHardwareAccelerated && count - i >= 64 && window.Length >= 64)
                {
                    var codes64 = Vector512.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if (Vector512.LessThanOrEqualAll(codes64, Vector512.Create((byte)0x7f)))
                    {
                        codes64.StoreUnsafe(ref dst, (nuint)i);
                        buffer.Advance(64);
                        i += 64;
                        continue;
                    }
                }
                if (Vector256.IsHardwareAccelerated && count - i >= 32 && window.Length >= 32)
                {
                    var codes32 = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if (Vector256.LessThanOrEqualAll(codes32, Vector256.Create((byte)0x7f)))
                    {
                        codes32.StoreUnsafe(ref dst, (nuint)i);
                        buffer.Advance(32);
                        i += 32;
                        continue;
                    }
                }
                if (window.Length >= 16)
                {
                    var codes = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if (Vector128.LessThanOrEqualAll(codes, Vector128.Create((byte)0x7f)))
                    {
                        codes.StoreUnsafe(ref dst, (nuint)i);
                        buffer.Advance(16);
                        i += 16;
                        continue;
                    }
                }
                // mixed chunk or short window: scalar for these 16, then try SIMD again
                int end = i + 16;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadByte();
                }
            }
        }
#endif
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadByte();
        }
    }
}

/// <summary>int16 element core: region-reserved scalar ladder emit (see <see cref="Int32ElementCodec"/> for the region rationale).</summary>
internal readonly struct Int16ElementCodec : IElementCodec<short>
{
    internal const int SerializeRegionElements = 4096; // * 3B = 12KB

    public void WriteElements<TWriteBuffer>(ref TWriteBuffer buffer, ReadOnlySpan<short> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref short src = ref MemoryMarshal.GetReference(source);
        int length = source.Length;

        int i = 0;
        while (i < length)
        {
            int regionEnd = i + Math.Min(length - i, SerializeRegionElements);
            ref byte d = ref buffer.GetReference((regionEnd - i) * MessagePackPrimitives.MaxInt16Length);
            int written = 0;
#if NET9_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                while (i + 16 <= regionEnd)
                {
                    var v = Vector256.LoadUnsafe(ref src, (nuint)i);
                    // all 16 in fixint range [-32, 127]?
                    if (Vector256.LessThanOrEqualAll((v + Vector256.Create((short)32)).AsUInt16(), Vector256.Create((ushort)159)))
                    {
                        // truncating narrow agrees with the values (verified in range)
                        Vector128.Narrow(v.GetLower(), v.GetUpper()).AsByte().StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    // all 16 outside [-128, 255], i.e. every token is 3B (int16/uint16)?
                    else if (Vector256.GreaterThanOrEqualAll((v + Vector256.Create((short)128)).AsUInt16(), Vector256.Create((ushort)384)))
                    {
                        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
                        {
                            // one vpermi2b builds the whole 48-byte run (measured 17x
                            // over EmitWide16), stored split 32 + 16 to stay exact
                            var codes = Vector256.Create((short)0x00cd) ^ (Vector256.ShiftRightArithmetic(v, 15) & Vector256.Create((short)0x001c));
                            var r = Avx512Vbmi.PermuteVar64x8x2(v.AsByte().ToVector512(), WeaveTables.Token3WeaveIndices512, codes.AsByte().ToVector512());
                            r.GetLower().StoreUnsafe(ref Unsafe.Add(ref d, written));
                            r.GetUpper().GetLower().StoreUnsafe(ref Unsafe.Add(ref d, written + 32));
                        }
                        else if (Avx2.IsSupported)
                        {
                            // tokens 0-9 from one vpermq'd window, 10-14 from a half
                            // window at element 8, token 15 scalar (heals byte 45)
                            var p = Avx2.Permute4x64(v.AsUInt64(), 0b10_01_01_00).AsInt16();
                            var cp = (Vector256.Create((short)0x00cd) ^ (Vector256.ShiftRightArithmetic(p, 15) & Vector256.Create((short)0x001c))).AsByte();
                            (Avx2.Shuffle(p.AsByte(), WeaveTables.Token3WeavePayload256) | Avx2.Shuffle(cp, WeaveTables.ShortCode256))
                                .StoreUnsafe(ref Unsafe.Add(ref d, written));
                            var h = Vector128.LoadUnsafe(ref src, (nuint)(i + 8));
                            var ch = (Vector128.Create((short)0x00cd) ^ (Vector128.ShiftRightArithmetic(h, 15) & Vector128.Create((short)0x001c))).AsByte();
                            (Ssse3.Shuffle(h.AsByte(), WeaveTables.Token3WeavePayloadHigh128) | Ssse3.Shuffle(ch, WeaveTables.ShortCodeHigh128))
                                .StoreUnsafe(ref Unsafe.Add(ref d, written + 30));
                            short v15 = Unsafe.Add(ref src, i + 15);
                            Unsafe.Add(ref d, written + 45) = (byte)(0xcd ^ ((v15 >> 15) & 0x1c));
                            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + 46), MessagePackEndian.ToBigEndian((ushort)v15));
                        }
                        else
                        {
                            EmitWide16(ref d, written, ref src, i);
                        }
                        written += 48;
                        i += 16;
                    }
                    else
                    {
                        // mixed 16: scalar classify chain, then re-probe the superlanes
                        int end = i + 16;
                        for (; i < end; i++)
                        {
                            written += MessagePackPrimitives.UnsafeWriteInt16(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                while (i + 16 <= regionEnd)
                {
                    var lo = Vector128.LoadUnsafe(ref src, (nuint)i);
                    var hi = Vector128.LoadUnsafe(ref src, (nuint)(i + 8));
                    var fixMax = Vector128.Max((lo + Vector128.Create((short)32)).AsUInt16(), (hi + Vector128.Create((short)32)).AsUInt16());
                    if (Vector128.LessThanOrEqualAll(fixMax, Vector128.Create((ushort)159)))
                    {
                        Vector128.Narrow(lo, hi).AsByte().StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    else
                    {
                        var wideMin = Vector128.Min((lo + Vector128.Create((short)128)).AsUInt16(), (hi + Vector128.Create((short)128)).AsUInt16());
                        if (Vector128.GreaterThanOrEqualAll(wideMin, Vector128.Create((ushort)384)))
                        {
                            if (Ssse3.IsSupported)
                            {
                                // three 5-token windows (elements 0-4, 5-9, 10-14; the
                                // third reuses the probe's hi load with the Hi tables)
                                // plus token 15 scalar, exact 48 bytes
                                var b2 = Vector128.LoadUnsafe(ref src, (nuint)(i + 5));
                                var ca = (Vector128.Create((short)0x00cd) ^ (Vector128.ShiftRightArithmetic(lo, 15) & Vector128.Create((short)0x001c))).AsByte();
                                var cb = (Vector128.Create((short)0x00cd) ^ (Vector128.ShiftRightArithmetic(b2, 15) & Vector128.Create((short)0x001c))).AsByte();
                                var cc = (Vector128.Create((short)0x00cd) ^ (Vector128.ShiftRightArithmetic(hi, 15) & Vector128.Create((short)0x001c))).AsByte();
                                (Ssse3.Shuffle(lo.AsByte(), WeaveTables.Token3WeavePayload128) | Ssse3.Shuffle(ca, WeaveTables.ShortCode128))
                                    .StoreUnsafe(ref Unsafe.Add(ref d, written));
                                (Ssse3.Shuffle(b2.AsByte(), WeaveTables.Token3WeavePayload128) | Ssse3.Shuffle(cb, WeaveTables.ShortCode128))
                                    .StoreUnsafe(ref Unsafe.Add(ref d, written + 15));
                                (Ssse3.Shuffle(hi.AsByte(), WeaveTables.Token3WeavePayloadHigh128) | Ssse3.Shuffle(cc, WeaveTables.ShortCodeHigh128))
                                    .StoreUnsafe(ref Unsafe.Add(ref d, written + 30));
                                short v15 = Unsafe.Add(ref src, i + 15);
                                Unsafe.Add(ref d, written + 45) = (byte)(0xcd ^ ((v15 >> 15) & 0x1c));
                                Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + 46), MessagePackEndian.ToBigEndian((ushort)v15));
                            }
                            else
                            {
                                EmitWide16(ref d, written, ref src, i);
                            }
                            written += 48;
                            i += 16;
                        }
                        else
                        {
                            int end = i + 16;
                            for (; i < end; i++)
                            {
                                written += MessagePackPrimitives.UnsafeWriteInt16(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                            }
                        }
                    }
                }
            }
#endif
            for (; i < regionEnd; i++)
            {
                written += MessagePackPrimitives.UnsafeWriteInt16(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
            }
            buffer.Advance(written);
        }
    }

#if NET9_0_OR_GREATER
    static void EmitWide16(ref byte d, int written, ref short src, int i)
    {
        for (int t = 0; t < 16; t++)
        {
            short val = Unsafe.Add(ref src, i + t);
            Unsafe.Add(ref d, written + (t * 3)) = (byte)(0xcd ^ ((val >> 15) & 0x1c));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + (t * 3) + 1), MessagePackEndian.ToBigEndian((ushort)val));
        }
    }

    // Values are stored before validation, same contract as the int32 wide decode: on a
    // failed gate the scalar reader re-reads the unadvanced buffer. A uint16 payload
    // above short.MaxValue must reach the scalar reader to throw, so it fails the gate.
    static bool TryDecodeWide16(ref byte w0, ref short dst, int i)
    {
        if (!IsWide(Unsafe.Add(ref w0, 0)) || !IsWide(Unsafe.Add(ref w0, 3)))
        {
            return false;
        }
        bool ok = true;
        bool overflow = false;
        for (int t = 0; t < 16; t++)
        {
            byte c = Unsafe.Add(ref w0, t * 3);
            ushort raw = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref w0, (t * 3) + 1)));
            Unsafe.Add(ref dst, i + t) = unchecked((short)raw);
            ok &= (c == 0xd1) | (c == 0xcd);
            overflow |= (c == 0xcd) & (raw > 0x7FFF);
        }
        return ok && !overflow;

        static bool IsWide(byte c) => (c == 0xd1) | (c == 0xcd);
    }
#endif

    public void ReadElements<TReadBuffer>(ref TReadBuffer buffer, Span<short> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref short dst = ref MemoryMarshal.GetReference(destination);
        int count = destination.Length;

        int i = 0;
#if NET9_0_OR_GREATER
        if (Vector128.IsHardwareAccelerated)
        {
            while (i + 16 <= count)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 16)
                {
                    var codes = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if (Vector128.LessThanOrEqualAll(codes + Vector128.Create((byte)0x20), Vector128.Create((byte)0x9f)))
                    {
                        // sign-extend 16 fixint bytes to 16 shorts
                        var (lo, hi) = Vector128.Widen(codes.AsSByte());
                        lo.StoreUnsafe(ref dst, (nuint)i);
                        hi.StoreUnsafe(ref dst, (nuint)(i + 8));
                        buffer.Advance(16);
                        i += 16;
                        continue;
                    }
                    if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported && window.Length >= 64)
                    {
                        // 16 tokens gathered by two vpermb (values and replicated codes,
                        // lower halves only; measured 14x over TryDecodeWide16). The
                        // load reads 64B but consumes 48; shorter windows fall through
                        // to the narrower weaves below.
                        var w = Vector512.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        var vals = Avx512Vbmi.PermuteVar64x8(w, WeaveTables.Token3DecodeValues512).GetLower().AsInt16();
                        var codes16 = Avx512Vbmi.PermuteVar64x8(w, WeaveTables.Token3DecodeCodes512).GetLower().AsUInt16();
                        var isCd = Vector256.Equals(codes16, Vector256.Create((ushort)0xcdcd));
                        var isD1 = Vector256.Equals(codes16, Vector256.Create((ushort)0xd1d1));
                        if (Vector256.EqualsAll(isCd | isD1, Vector256<ushort>.AllBitsSet)
                            && Vector256.GreaterThanOrEqualAll(isCd.AsInt16() & vals, Vector256<short>.Zero))
                        {
                            vals.StoreUnsafe(ref dst, (nuint)i);
                            buffer.Advance(48);
                            i += 16;
                            continue;
                        }
                    }
                    else if (Avx2.IsSupported && window.Length >= 32)
                    {
                        // 10 tokens per 30B window; codes every 3 bytes (mask
                        // 0x9249249), uint16 overflow via the 0xcd mask shifted onto
                        // the first payload byte's movemask bit
                        var w = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        uint cd = Vector256.Equals(w, Vector256.Create((byte)0xcd)).ExtractMostSignificantBits();
                        uint d1 = Vector256.Equals(w, Vector256.Create((byte)0xd1)).ExtractMostSignificantBits();
                        uint msb = w.ExtractMostSignificantBits();
                        if (((cd | d1) & 0x9249249u) == 0x9249249u && (((cd & 0x9249249u) << 1) & msb) == 0)
                        {
                            var shuffled = Avx2.Shuffle(w, WeaveTables.Token3DecodeShuffle256);
                            shuffled.GetLower().StoreUnsafe(ref Unsafe.As<short, byte>(ref dst), (nuint)i * 2);
                            shuffled.GetUpper().StoreUnsafe(ref Unsafe.As<short, byte>(ref dst), ((nuint)i * 2) + 10);
                            buffer.Advance(30);
                            i += 10;
                            continue;
                        }
                    }
                    else if (Ssse3.IsSupported && window.Length >= 16)
                    {
                        var w = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        uint cd = Vector128.Equals(w, Vector128.Create((byte)0xcd)).ExtractMostSignificantBits();
                        uint d1 = Vector128.Equals(w, Vector128.Create((byte)0xd1)).ExtractMostSignificantBits();
                        uint msb = w.ExtractMostSignificantBits();
                        if (((cd | d1) & 0x1249u) == 0x1249u && (((cd & 0x1249u) << 1) & msb) == 0)
                        {
                            Ssse3.Shuffle(w, WeaveTables.Token3DecodeShuffle128).StoreUnsafe(ref Unsafe.As<short, byte>(ref dst), (nuint)i * 2);
                            buffer.Advance(15);
                            i += 5;
                            continue;
                        }
                    }
                    else if (window.Length >= 48 && TryDecodeWide16(ref MemoryMarshal.GetReference(window), ref dst, i))
                    {
                        buffer.Advance(48);
                        i += 16;
                        continue;
                    }
                }
                // mixed chunk or short window: scalar for these 16, then try SIMD again
                int end = i + 16;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadInt16();
                }
            }
        }
#endif
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadInt16();
        }
    }
}

/// <summary>uint16 element core: the <see cref="Int16ElementCodec"/> superlanes with
/// unsigned zones (fixint = 0..127, wide = above 255) and a constant 0xcd wide code
/// (the weaves fill or OR a constant code source, the decode gates validate a single
/// pattern with no overflow test).</summary>
internal readonly struct UInt16ElementCodec : IElementCodec<ushort>
{
    internal const int SerializeRegionElements = 4096; // * 3B = 12KB

    public void WriteElements<TWriteBuffer>(ref TWriteBuffer buffer, ReadOnlySpan<ushort> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref ushort src = ref MemoryMarshal.GetReference(source);
        int length = source.Length;

        int i = 0;
        while (i < length)
        {
            int regionEnd = i + Math.Min(length - i, SerializeRegionElements);
            ref byte d = ref buffer.GetReference((regionEnd - i) * MessagePackPrimitives.MaxUInt16Length);
            int written = 0;
#if NET9_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                while (i + 16 <= regionEnd)
                {
                    var v = Vector256.LoadUnsafe(ref src, (nuint)i);
                    if (Vector256.LessThanOrEqualAll(v, Vector256.Create((ushort)127)))
                    {
                        Vector128.Narrow(v.GetLower(), v.GetUpper()).StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    else if (Vector256.GreaterThanOrEqualAll(v, Vector256.Create((ushort)256)))
                    {
                        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
                        {
                            // the int16 weave with a 0xcd-filled code source
                            var r = Avx512Vbmi.PermuteVar64x8x2(v.AsByte().ToVector512(), WeaveTables.Token3WeaveIndices512, Vector512.Create((byte)0xcd));
                            r.GetLower().StoreUnsafe(ref Unsafe.Add(ref d, written));
                            r.GetUpper().GetLower().StoreUnsafe(ref Unsafe.Add(ref d, written + 32));
                        }
                        else if (Avx2.IsSupported)
                        {
                            var p = Avx2.Permute4x64(v.AsUInt64(), 0b10_01_01_00).AsByte();
                            (Avx2.Shuffle(p, WeaveTables.Token3WeavePayload256) | WeaveTables.UShortCode256)
                                .StoreUnsafe(ref Unsafe.Add(ref d, written));
                            var h = Vector128.LoadUnsafe(ref src, (nuint)(i + 8));
                            (Ssse3.Shuffle(h.AsByte(), WeaveTables.Token3WeavePayloadHigh128) | WeaveTables.UShortCode128)
                                .StoreUnsafe(ref Unsafe.Add(ref d, written + 30));
                            Unsafe.Add(ref d, written + 45) = 0xcd;
                            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + 46), MessagePackEndian.ToBigEndian(Unsafe.Add(ref src, i + 15)));
                        }
                        else
                        {
                            EmitWide16(ref d, written, ref src, i);
                        }
                        written += 48;
                        i += 16;
                    }
                    else
                    {
                        int end = i + 16;
                        for (; i < end; i++)
                        {
                            written += MessagePackPrimitives.UnsafeWriteUInt16(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                while (i + 16 <= regionEnd)
                {
                    var lo = Vector128.LoadUnsafe(ref src, (nuint)i);
                    var hi = Vector128.LoadUnsafe(ref src, (nuint)(i + 8));
                    if (Vector128.LessThanOrEqualAll(Vector128.Max(lo, hi), Vector128.Create((ushort)127)))
                    {
                        Vector128.Narrow(lo, hi).StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    else if (Vector128.GreaterThanOrEqualAll(Vector128.Min(lo, hi), Vector128.Create((ushort)256)))
                    {
                        if (Ssse3.IsSupported)
                        {
                            var b2 = Vector128.LoadUnsafe(ref src, (nuint)(i + 5));
                            (Ssse3.Shuffle(lo.AsByte(), WeaveTables.Token3WeavePayload128) | WeaveTables.UShortCode128)
                                .StoreUnsafe(ref Unsafe.Add(ref d, written));
                            (Ssse3.Shuffle(b2.AsByte(), WeaveTables.Token3WeavePayload128) | WeaveTables.UShortCode128)
                                .StoreUnsafe(ref Unsafe.Add(ref d, written + 15));
                            (Ssse3.Shuffle(hi.AsByte(), WeaveTables.Token3WeavePayloadHigh128) | WeaveTables.UShortCode128)
                                .StoreUnsafe(ref Unsafe.Add(ref d, written + 30));
                            Unsafe.Add(ref d, written + 45) = 0xcd;
                            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + 46), MessagePackEndian.ToBigEndian(Unsafe.Add(ref src, i + 15)));
                        }
                        else
                        {
                            EmitWide16(ref d, written, ref src, i);
                        }
                        written += 48;
                        i += 16;
                    }
                    else
                    {
                        int end = i + 16;
                        for (; i < end; i++)
                        {
                            written += MessagePackPrimitives.UnsafeWriteUInt16(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
#endif
            for (; i < regionEnd; i++)
            {
                written += MessagePackPrimitives.UnsafeWriteUInt16(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
            }
            buffer.Advance(written);
        }
    }

#if NET9_0_OR_GREATER
    static void EmitWide16(ref byte d, int written, ref ushort src, int i)
    {
        for (int t = 0; t < 16; t++)
        {
            Unsafe.Add(ref d, written + (t * 3)) = 0xcd;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + (t * 3) + 1), MessagePackEndian.ToBigEndian(Unsafe.Add(ref src, i + t)));
        }
    }

    // every uint16 payload fits ushort, so unlike the signed variant there is no
    // overflow flag; only the code bytes are validated
    static bool TryDecodeWide16(ref byte w0, ref ushort dst, int i)
    {
        if (Unsafe.Add(ref w0, 0) != 0xcd || Unsafe.Add(ref w0, 3) != 0xcd)
        {
            return false;
        }
        bool ok = true;
        for (int t = 0; t < 16; t++)
        {
            ok &= Unsafe.Add(ref w0, t * 3) == 0xcd;
            Unsafe.Add(ref dst, i + t) = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref w0, (t * 3) + 1)));
        }
        return ok;
    }
#endif

    public void ReadElements<TReadBuffer>(ref TReadBuffer buffer, Span<ushort> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref ushort dst = ref MemoryMarshal.GetReference(destination);
        int count = destination.Length;

        int i = 0;
#if NET9_0_OR_GREATER
        if (Vector128.IsHardwareAccelerated)
        {
            while (i + 16 <= count)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 16)
                {
                    var codes = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    // positive fixint only: negative fixint is out of range for ushort
                    // and must reach the scalar reader to throw
                    if (Vector128.LessThanOrEqualAll(codes, Vector128.Create((byte)0x7f)))
                    {
                        var (lo, hi) = Vector128.Widen(codes);
                        lo.StoreUnsafe(ref dst, (nuint)i);
                        hi.StoreUnsafe(ref dst, (nuint)(i + 8));
                        buffer.Advance(16);
                        i += 16;
                        continue;
                    }
                    if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported && window.Length >= 64)
                    {
                        // the int16 gather with a single 0xcd code pattern and no
                        // overflow test (every uint16 payload fits)
                        var w = Vector512.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        var codes16 = Avx512Vbmi.PermuteVar64x8(w, WeaveTables.Token3DecodeCodes512).GetLower().AsUInt16();
                        if (Vector256.EqualsAll(codes16, Vector256.Create((ushort)0xcdcd)))
                        {
                            Avx512Vbmi.PermuteVar64x8(w, WeaveTables.Token3DecodeValues512).GetLower().AsUInt16()
                                .StoreUnsafe(ref dst, (nuint)i);
                            buffer.Advance(48);
                            i += 16;
                            continue;
                        }
                    }
                    else if (Avx2.IsSupported && window.Length >= 32)
                    {
                        var w = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        if ((Vector256.Equals(w, Vector256.Create((byte)0xcd)).ExtractMostSignificantBits() & 0x9249249u) == 0x9249249u)
                        {
                            var shuffled = Avx2.Shuffle(w, WeaveTables.Token3DecodeShuffle256);
                            shuffled.GetLower().StoreUnsafe(ref Unsafe.As<ushort, byte>(ref dst), (nuint)i * 2);
                            shuffled.GetUpper().StoreUnsafe(ref Unsafe.As<ushort, byte>(ref dst), ((nuint)i * 2) + 10);
                            buffer.Advance(30);
                            i += 10;
                            continue;
                        }
                    }
                    else if (Ssse3.IsSupported && window.Length >= 16)
                    {
                        var w = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        if ((Vector128.Equals(w, Vector128.Create((byte)0xcd)).ExtractMostSignificantBits() & 0x1249u) == 0x1249u)
                        {
                            Ssse3.Shuffle(w, WeaveTables.Token3DecodeShuffle128).StoreUnsafe(ref Unsafe.As<ushort, byte>(ref dst), (nuint)i * 2);
                            buffer.Advance(15);
                            i += 5;
                            continue;
                        }
                    }
                    else if (window.Length >= 48 && TryDecodeWide16(ref MemoryMarshal.GetReference(window), ref dst, i))
                    {
                        buffer.Advance(48);
                        i += 16;
                        continue;
                    }
                }
                int end = i + 16;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadUInt16();
                }
            }
        }
#endif
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadUInt16();
        }
    }
}

/// <summary>uint32 element core: the <see cref="Int32ElementCodec"/> superlanes with
/// unsigned zones (fixint = 0..127, wide = above 65535). The wide code is a constant
/// 0xce, so the VBMI weave reuses the shared <see cref="WeaveTables"/> with a
/// code-filled second source, the AVX2/SSSE3 weaves OR a constant code vector, and the
/// decode gates validate a single code pattern.</summary>
internal readonly struct UInt32ElementCodec : IElementCodec<uint>
{
    internal const int SerializeRegionElements = 4096; // * 5B = 20KB

    public void WriteElements<TWriteBuffer>(ref TWriteBuffer buffer, ReadOnlySpan<uint> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref uint src = ref MemoryMarshal.GetReference(source);
        int length = source.Length;

        int i = 0;
        while (i < length)
        {
            int regionEnd = i + Math.Min(length - i, SerializeRegionElements);
            // +2: same reservation slack as Int32ElementCodec, for the AVX2 wide weave's
            // 2-byte store overhang at the region end
            ref byte d = ref buffer.GetReference(((regionEnd - i) * MessagePackPrimitives.MaxUInt32Length) + 2);
            int written = 0;
#if NET9_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && Avx512F.IsSupported)
            {
                while (i + 16 <= regionEnd)
                {
                    var v = Vector512.LoadUnsafe(ref src, (nuint)i);
                    if (Vector512.LessThanOrEqualAll(v, Vector512.Create(127u)))
                    {
                        Avx512F.ConvertToVector128SByte(v.AsInt32()).AsByte().StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    else if (Vector512.GreaterThanOrEqualAll(v, Vector512.Create(65536u)))
                    {
                        if (Avx512Vbmi.IsSupported)
                        {
                            var be = Vector512.Shuffle(v.AsByte(), WeaveTables.Token5PayloadReverse512);
                            var codes = Vector512.Create(0x000000ce).AsByte();
                            Avx512Vbmi.PermuteVar64x8x2(be, WeaveTables.Token5WeaveIndices512, codes).StoreUnsafe(ref Unsafe.Add(ref d, written));
                            Avx512Vbmi.PermuteVar64x8x2(be, WeaveTables.Token5WeaveShiftedIndices512, codes).StoreUnsafe(ref Unsafe.Add(ref d, written + 16));
                        }
                        else
                        {
                            EmitWide16(ref d, written, ref src, i);
                        }
                        written += 80;
                        i += 16;
                    }
                    else
                    {
                        int end = i + 16;
                        for (; i < end; i++)
                        {
                            written += MessagePackPrimitives.UnsafeWriteUInt32(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                while (i + 16 <= regionEnd)
                {
                    var a = Vector256.LoadUnsafe(ref src, (nuint)i);
                    var b = Vector256.LoadUnsafe(ref src, (nuint)(i + 8));
                    if (Vector256.LessThanOrEqualAll(Vector256.Max(a, b), Vector256.Create(127u)))
                    {
                        var packed = Vector256.Narrow(a, b);
                        Vector256.Narrow(packed, packed).GetLower().StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    else if (Vector256.GreaterThanOrEqualAll(Vector256.Min(a, b), Vector256.Create(65536u)))
                    {
                        if (Avx2.IsSupported)
                        {
                            // the int32 wide weave with a constant-code OR (every token
                            // is 0xce, so no sign select and no second shuffle)
                            var p0 = Avx2.Permute4x64(a.AsUInt64(), 0b10_01_01_00).AsByte();
                            var p1 = Avx2.Permute4x64(Vector256.LoadUnsafe(ref src, (nuint)(i + 4)).AsUInt64(), 0b11_10_10_01).AsByte();
                            var p2 = Avx2.Permute4x64(b.AsUInt64(), 0b11_10_10_01).AsByte();
                            (Avx2.Shuffle(p0, WeaveTables.Token5WeaveIndices256) | WeaveTables.UIntCode256).StoreUnsafe(ref Unsafe.Add(ref d, written));
                            (Avx2.Shuffle(p1, WeaveTables.Token5WeaveIndices256) | WeaveTables.UIntCode256).StoreUnsafe(ref Unsafe.Add(ref d, written + 30));
                            (Avx2.Shuffle(p2, WeaveTables.Token5WeaveIndices256) | WeaveTables.UIntCode256).StoreUnsafe(ref Unsafe.Add(ref d, written + 50));
                        }
                        else
                        {
                            EmitWide16(ref d, written, ref src, i);
                        }
                        written += 80;
                        i += 16;
                    }
                    else
                    {
                        int end = i + 16;
                        for (; i < end; i++)
                        {
                            written += MessagePackPrimitives.UnsafeWriteUInt32(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                while (i + 16 <= regionEnd)
                {
                    var a = Vector128.LoadUnsafe(ref src, (nuint)i);
                    var b = Vector128.LoadUnsafe(ref src, (nuint)(i + 4));
                    var c = Vector128.LoadUnsafe(ref src, (nuint)(i + 8));
                    var e = Vector128.LoadUnsafe(ref src, (nuint)(i + 12));
                    var max = Vector128.Max(Vector128.Max(a, b), Vector128.Max(c, e));
                    if (Vector128.LessThanOrEqualAll(max, Vector128.Create(127u)))
                    {
                        Vector128.Narrow(Vector128.Narrow(a, b), Vector128.Narrow(c, e)).StoreUnsafe(ref Unsafe.Add(ref d, written));
                        written += 16;
                        i += 16;
                    }
                    else
                    {
                        var min = Vector128.Min(Vector128.Min(a, b), Vector128.Min(c, e));
                        if (Vector128.GreaterThanOrEqualAll(min, Vector128.Create(65536u)))
                        {
                            if (Ssse3.IsSupported)
                            {
                                // five 3-token weave windows plus one scalar token,
                                // constant-code OR (see the int32 128 wide weave)
                                for (int t = 0; t < 5; t++)
                                {
                                    var v = Vector128.LoadUnsafe(ref src, (nuint)(i + (t * 3)));
                                    (Ssse3.Shuffle(v.AsByte(), WeaveTables.Token5WeaveIndices128) | WeaveTables.UIntCode128)
                                        .StoreUnsafe(ref Unsafe.Add(ref d, written + (t * 15)));
                                }
                                Unsafe.Add(ref d, written + 75) = MessagePackCode.UInt32;
                                Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + 76), MessagePackEndian.ToBigEndian(Unsafe.Add(ref src, i + 15)));
                            }
                            else
                            {
                                EmitWide16(ref d, written, ref src, i);
                            }
                            written += 80;
                            i += 16;
                        }
                        else
                        {
                            int end = i + 16;
                            for (; i < end; i++)
                            {
                                written += MessagePackPrimitives.UnsafeWriteUInt32(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                            }
                        }
                    }
                }
            }
#endif
            for (; i < regionEnd; i++)
            {
                written += MessagePackPrimitives.UnsafeWriteUInt32(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
            }
            buffer.Advance(written);
        }
    }

#if NET9_0_OR_GREATER
    static void EmitWide16(ref byte d, int written, ref uint src, int i)
    {
        for (int t = 0; t < 16; t++)
        {
            Unsafe.Add(ref d, written + (t * 5)) = 0xce;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + (t * 5) + 1), MessagePackEndian.ToBigEndian(Unsafe.Add(ref src, i + t)));
        }
    }

    // every uint32 payload fits uint, so only the code bytes are validated
    static bool TryDecodeWide16(ref byte w0, ref uint dst, int i)
    {
        if (Unsafe.Add(ref w0, 0) != 0xce || Unsafe.Add(ref w0, 5) != 0xce)
        {
            return false;
        }
        bool ok = true;
        for (int t = 0; t < 16; t++)
        {
            ok &= Unsafe.Add(ref w0, t * 5) == 0xce;
            Unsafe.Add(ref dst, i + t) = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref w0, (t * 5) + 1)));
        }
        return ok;
    }
#endif

    public void ReadElements<TReadBuffer>(ref TReadBuffer buffer, Span<uint> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref uint dst = ref MemoryMarshal.GetReference(destination);
        int count = destination.Length;

        int i = 0;
#if NET9_0_OR_GREATER
        if (Vector128.IsHardwareAccelerated)
        {
            while (i + 16 <= count)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 16)
                {
                    var codes = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    // positive fixint only: negative fixint must reach the scalar reader to throw
                    if (Vector128.LessThanOrEqualAll(codes, Vector128.Create((byte)0x7f)))
                    {
                        if (Vector512.IsHardwareAccelerated && Avx512F.IsSupported)
                        {
                            // zero-extend 16 bytes to 16 uints (vpmovzxbd)
                            Avx512F.ConvertToVector512Int32(codes).AsUInt32().StoreUnsafe(ref dst, (nuint)i);
                        }
                        else
                        {
                            var (lo, hi) = Vector128.Widen(codes);
                            var (u0, u1) = Vector128.Widen(lo);
                            var (u2, u3) = Vector128.Widen(hi);
                            u0.StoreUnsafe(ref dst, (nuint)i);
                            u1.StoreUnsafe(ref dst, (nuint)(i + 4));
                            u2.StoreUnsafe(ref dst, (nuint)(i + 8));
                            u3.StoreUnsafe(ref dst, (nuint)(i + 12));
                        }
                        buffer.Advance(16);
                        i += 16;
                        continue;
                    }

                    if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported && window.Length >= 80)
                    {
                        // the int32 wide gather with a single constant code compare
                        ref byte w0 = ref MemoryMarshal.GetReference(window);
                        var lo512 = Vector512.LoadUnsafe(ref w0);
                        var hi512 = Vector512.LoadUnsafe(ref Unsafe.Add(ref w0, 16));
                        var codes4 = Avx512Vbmi.PermuteVar64x8x2(lo512, WeaveTables.Token5DecodeCodes512, hi512).AsInt32();
                        if (Vector512.EqualsAll(codes4, Vector512.Create(unchecked((int)0xcececece))))
                        {
                            Avx512Vbmi.PermuteVar64x8x2(lo512, WeaveTables.Token5DecodeValues512, hi512).AsUInt32()
                                .StoreUnsafe(ref dst, (nuint)i);
                            buffer.Advance(80);
                            i += 16;
                            continue;
                        }
                    }
                    else if (Avx2.IsSupported && window.Length >= 32)
                    {
                        // 6 tokens per 32B window: a single constant-code gate (every
                        // valid token is 0xce; 0xd2 or anything else goes scalar). No
                        // overflow test, every uint32 payload fits.
                        var w = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        if ((Vector256.Equals(w, Vector256.Create(MessagePackCode.UInt32)).ExtractMostSignificantBits() & 0x2108421u) == 0x2108421u)
                        {
                            var shuffled = Avx2.Shuffle(w, WeaveTables.Token5DecodeShuffle256);
                            shuffled.GetLower().StoreUnsafe(ref Unsafe.As<uint, byte>(ref dst), (nuint)i * 4);
                            shuffled.GetUpper().StoreUnsafe(ref Unsafe.As<uint, byte>(ref dst), ((nuint)i * 4) + 12);
                            buffer.Advance(30);
                            i += 6;
                            continue;
                        }
                    }
                    else if (Ssse3.IsSupported && window.Length >= 16)
                    {
                        var w = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        if ((Vector128.Equals(w, Vector128.Create(MessagePackCode.UInt32)).ExtractMostSignificantBits() & 0x421u) == 0x421u)
                        {
                            Ssse3.Shuffle(w, WeaveTables.Token5DecodeShuffle128)
                                .StoreUnsafe(ref Unsafe.As<uint, byte>(ref dst), (nuint)i * 4);
                            buffer.Advance(15);
                            i += 3;
                            continue;
                        }
                    }
                    else if (window.Length >= 80 && TryDecodeWide16(ref MemoryMarshal.GetReference(window), ref dst, i))
                    {
                        buffer.Advance(80);
                        i += 16;
                        continue;
                    }
                }
                int end = i + 16;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadUInt32();
                }
            }
        }
#endif
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadUInt32();
        }
    }
}

/// <summary>int64 element core: 8-token homogeneous superlanes. Fixint narrows 8 longs
/// to 8 bytes; the wide superlane covers values outside [int.MinValue, uint.MaxValue]
/// (9B tokens, sign-mask code select 0xcf ^ 0x1c = 0xd3) and weaves via the double
/// tables: overlapping vpermi2b pair on VBMI, 3+3+pair shuffle windows on AVX2,
/// constant-stride scalar elsewhere. The 128 tier is
/// deliberately absent: gating 8 longs there costs four loads for two lanes each,
/// which does not carry the pattern.</summary>
internal readonly struct Int64ElementCodec : IElementCodec<long>
{
    internal const int SerializeRegionElements = 2048; // * 9B = 18KB

    public void WriteElements<TWriteBuffer>(ref TWriteBuffer buffer, ReadOnlySpan<long> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref long src = ref MemoryMarshal.GetReference(source);
        int length = source.Length;

        int i = 0;
        while (i < length)
        {
            int regionEnd = i + Math.Min(length - i, SerializeRegionElements);
            // +1: the VBMI wide weave's second store overhangs the 72-byte token run by
            // 1 byte, which lands past the worst case when the block ends the region
            ref byte d = ref buffer.GetReference(((regionEnd - i) * MessagePackPrimitives.MaxInt64Length) + 1);
            int written = 0;
#if NET9_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && Avx512F.IsSupported)
            {
                while (i + 8 <= regionEnd)
                {
                    var v = Vector512.LoadUnsafe(ref src, (nuint)i);
                    if (Vector512.LessThanOrEqualAll((v + Vector512.Create(32L)).AsUInt64(), Vector512.Create(159UL)))
                    {
                        // truncating narrow 8 longs -> 8 bytes (vpmovqb)
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written), Avx512F.ConvertToVector128SByte(v).AsUInt64().ToScalar());
                        written += 8;
                        i += 8;
                    }
                    // all 8 outside [int.Min, uint.Max], i.e. every token is 9B?
                    else if (Vector512.GreaterThanOrEqualAll((v + Vector512.Create(2147483648L)).AsUInt64(), Vector512.Create(6442450944UL)))
                    {
                        if (Avx512Vbmi.IsSupported)
                        {
                            // two overlapping vpermi2b weaves (tokens 0-6, then 1-7
                            // from the same source at +9, rewriting 1-6 identically;
                            // measured 6.3x over EmitWide8). The second store overhangs
                            // the run by 1 byte, covered by the reservation slack.
                            var codes = (Vector512.Create(0x00000000000000cfL) ^ (Vector512.ShiftRightArithmetic(v, 63) & Vector512.Create(0x000000000000001cL))).AsByte();
                            Avx512Vbmi.PermuteVar64x8x2(v.AsByte(), WeaveTables.Token9WeaveIndices512, codes)
                                .StoreUnsafe(ref Unsafe.Add(ref d, written));
                            Avx512Vbmi.PermuteVar64x8x2(v.AsByte(), WeaveTables.Token9WeaveShiftedIndices512, codes)
                                .StoreUnsafe(ref Unsafe.Add(ref d, written + 9));
                        }
                        else
                        {
                            EmitWide8(ref d, written, ref src, i);
                        }
                        written += 72;
                        i += 8;
                    }
                    else
                    {
                        int end = i + 8;
                        for (; i < end; i++)
                        {
                            written += MessagePackPrimitives.UnsafeWriteInt64(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                while (i + 8 <= regionEnd)
                {
                    var a = Vector256.LoadUnsafe(ref src, (nuint)i);
                    var b = Vector256.LoadUnsafe(ref src, (nuint)(i + 4));
                    var fixMax = Vector256.Max((a + Vector256.Create(32L)).AsUInt64(), (b + Vector256.Create(32L)).AsUInt64());
                    if (Vector256.LessThanOrEqualAll(fixMax, Vector256.Create(159UL)))
                    {
                        for (int t = 0; t < 8; t++)
                        {
                            Unsafe.Add(ref d, written + t) = (byte)Unsafe.Add(ref src, i + t);
                        }
                        written += 8;
                        i += 8;
                    }
                    else
                    {
                        var wideMin = Vector256.Min((a + Vector256.Create(2147483648L)).AsUInt64(), (b + Vector256.Create(2147483648L)).AsUInt64());
                        if (Vector256.GreaterThanOrEqualAll(wideMin, Vector256.Create(6442450944UL)))
                        {
                            if (Avx2.IsSupported)
                            {
                                // the double 256 weave with a sign-selected code
                                // scatter: tokens 0-2 and 3-5 as 3-token windows, 6-7
                                // as the exact-18B pair (its vector heals the second
                                // window's 5 garbage bytes; codes and the last payload
                                // byte go scalar)
                                WeaveWide3(ref d, written, a);
                                WeaveWide3(ref d, written + 27, Vector256.LoadUnsafe(ref src, (nuint)(i + 3)));
                                var pair = b.GetUpper().AsByte();
                                Ssse3.Shuffle(pair, WeaveTables.Token9WeaveIndices128).StoreUnsafe(ref Unsafe.Add(ref d, written + 55));
                                long v6 = Unsafe.Add(ref src, i + 6);
                                long v7 = Unsafe.Add(ref src, i + 7);
                                Unsafe.Add(ref d, written + 54) = (byte)(0xcf ^ (int)((v6 >> 63) & 0x1c));
                                Unsafe.Add(ref d, written + 63) = (byte)(0xcf ^ (int)((v7 >> 63) & 0x1c));
                                Unsafe.Add(ref d, written + 71) = (byte)v7;
                            }
                            else
                            {
                                EmitWide8(ref d, written, ref src, i);
                            }
                            written += 72;
                            i += 8;
                        }
                        else
                        {
                            int end = i + 8;
                            for (; i < end; i++)
                            {
                                written += MessagePackPrimitives.UnsafeWriteInt64(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                            }
                        }
                    }
                }
            }
#endif
            for (; i < regionEnd; i++)
            {
                written += MessagePackPrimitives.UnsafeWriteInt64(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
            }
            buffer.Advance(written);
        }
    }

#if NET9_0_OR_GREATER
    static void EmitWide8(ref byte d, int written, ref long src, int i)
    {
        for (int t = 0; t < 8; t++)
        {
            long val = Unsafe.Add(ref src, i + t);
            Unsafe.Add(ref d, written + (t * 9)) = (byte)(0xcf ^ (int)((val >> 63) & 0x1c));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + (t * 9) + 1), MessagePackEndian.ToBigEndian((ulong)val));
        }
    }

    /// <summary>
    /// 3 tokens from 4 loaded longs: the double 256 weave layout (vpermq lanes
    /// [e0,e1][e1,e2]) with a code scatter for the sign-selected code instead of the
    /// constant OR. The 32B store's 5 garbage bytes heal under the following store.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WeaveWide3(ref byte d, int at, Vector256<long> loaded)
    {
        var p = Avx2.Permute4x64(loaded.AsUInt64(), 0b10_01_01_00).AsInt64();
        var cp = (Vector256.Create(0x00000000000000cfL) ^ (Vector256.LessThan(p, Vector256<long>.Zero) & Vector256.Create(0x000000000000001cL))).AsByte();
        (Avx2.Shuffle(p.AsByte(), WeaveTables.Token9WeaveIndices256) | Avx2.Shuffle(cp, WeaveTables.LongCode256))
            .StoreUnsafe(ref Unsafe.Add(ref d, at));
    }

    // a uint64 payload above long.MaxValue must reach the scalar reader to throw
    static bool TryDecodeWide8(ref byte w0, ref long dst, int i)
    {
        if (!IsWide(Unsafe.Add(ref w0, 0)) || !IsWide(Unsafe.Add(ref w0, 9)))
        {
            return false;
        }
        bool ok = true;
        bool overflow = false;
        for (int t = 0; t < 8; t++)
        {
            byte c = Unsafe.Add(ref w0, t * 9);
            ulong raw = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref w0, (t * 9) + 1)));
            Unsafe.Add(ref dst, i + t) = unchecked((long)raw);
            ok &= (c == 0xd3) | (c == 0xcf);
            overflow |= (c == 0xcf) & ((long)raw < 0);
        }
        return ok && !overflow;

        static bool IsWide(byte c) => (c == 0xd3) | (c == 0xcf);
    }
#endif

    public void ReadElements<TReadBuffer>(ref TReadBuffer buffer, Span<long> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref long dst = ref MemoryMarshal.GetReference(destination);
        int count = destination.Length;

        int i = 0;
#if NET9_0_OR_GREATER
        if (Vector128.IsHardwareAccelerated)
        {
            while (i + 8 <= count)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 16)
                {
                    // the probe loads 16 window bytes but only the lower 8 lanes gate
                    var codes = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if ((Vector128.LessThanOrEqual(codes + Vector128.Create((byte)0x20), Vector128.Create((byte)0x9f)).ExtractMostSignificantBits() & 0xFF) == 0xFF)
                    {
                        if (Vector512.IsHardwareAccelerated && Avx512F.IsSupported)
                        {
                            // sign-extend 8 fixint bytes to 8 longs (vpmovsxbq)
                            Avx512F.ConvertToVector512Int64(codes.AsSByte()).StoreUnsafe(ref dst, (nuint)i);
                        }
                        else
                        {
                            ref byte w0 = ref MemoryMarshal.GetReference(window);
                            for (int t = 0; t < 8; t++)
                            {
                                Unsafe.Add(ref dst, i + t) = unchecked((sbyte)Unsafe.Add(ref w0, t));
                            }
                        }
                        buffer.Advance(8);
                        i += 8;
                        continue;
                    }
                    if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported && window.Length >= 64)
                    {
                        // the double gather tables apply as-is (identical token shape);
                        // validation masks to lanes 0-6, lane 7 is the garbage lane
                        // whose dst slot is re-decoded by the next iteration or the
                        // scalar tail. Measured 5x over TryDecodeWide8.
                        var w = Vector512.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        var vals = Avx512Vbmi.PermuteVar64x8(w, WeaveTables.Token9DecodeValues512).AsInt64();
                        var codes8 = Avx512Vbmi.PermuteVar64x8(w, WeaveTables.Token9DecodeCodes512).AsInt64();
                        ulong cf = Vector512.Equals(codes8, Vector512.Create(unchecked((long)0xCFCFCFCFCFCFCFCF))).ExtractMostSignificantBits();
                        ulong d3 = Vector512.Equals(codes8, Vector512.Create(unchecked((long)0xD3D3D3D3D3D3D3D3))).ExtractMostSignificantBits();
                        ulong neg = vals.ExtractMostSignificantBits();
                        if (((cf | d3) & 0x7FUL) == 0x7FUL && (cf & neg & 0x7FUL) == 0)
                        {
                            vals.StoreUnsafe(ref dst, (nuint)i);
                            buffer.Advance(63);
                            i += 7;
                            continue;
                        }
                    }
                    else if (Avx2.IsSupported && window.Length >= 32)
                    {
                        // 3 tokens per 32B window, the double 256 gather with the
                        // (0xcf | 0xd3) gate and the uint64-overflow movemask shift
                        var w = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        uint cf = Vector256.Equals(w, Vector256.Create((byte)0xcf)).ExtractMostSignificantBits();
                        uint d3 = Vector256.Equals(w, Vector256.Create((byte)0xd3)).ExtractMostSignificantBits();
                        uint msb = w.ExtractMostSignificantBits();
                        if (((cf | d3) & 0x40201u) == 0x40201u && (((cf & 0x40201u) << 1) & msb) == 0)
                        {
                            var vB = Avx2.Permute4x64(w.AsUInt64(), 0b00_00_10_01).AsByte();
                            (Avx2.Shuffle(w, WeaveTables.Token9DecodeShuffleA256) | Avx2.Shuffle(vB, WeaveTables.Token9DecodeShuffleB256))
                                .StoreUnsafe(ref Unsafe.As<long, byte>(ref dst), (nuint)i * 8);
                            buffer.Advance(27);
                            i += 3;
                            continue;
                        }
                    }
                    else if (window.Length >= 72 && TryDecodeWide8(ref MemoryMarshal.GetReference(window), ref dst, i))
                    {
                        // 128-bit weave deliberately absent: measured a wash against
                        // this constant-stride loop (the per-token scalar gate eats
                        // the byte-reverse gain; see LadderWideWeaveBenchmark)
                        buffer.Advance(72);
                        i += 8;
                        continue;
                    }
                }
                int end = i + 8;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadInt64();
                }
            }
        }
#endif
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadInt64();
        }
    }
}

/// <summary>uint64 element core: the <see cref="Int64ElementCodec"/> superlanes with
/// unsigned zones (fixint = 0..127, wide = above uint.MaxValue) and a constant 0xcf
/// wide code (the weaves fill or OR a constant code source, the decode gates validate
/// a single pattern with no overflow test).</summary>
internal readonly struct UInt64ElementCodec : IElementCodec<ulong>
{
    internal const int SerializeRegionElements = 2048; // * 9B = 18KB

    public void WriteElements<TWriteBuffer>(ref TWriteBuffer buffer, ReadOnlySpan<ulong> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref ulong src = ref MemoryMarshal.GetReference(source);
        int length = source.Length;

        int i = 0;
        while (i < length)
        {
            int regionEnd = i + Math.Min(length - i, SerializeRegionElements);
            // +1: same reservation slack as Int64ElementCodec, for the VBMI wide
            // weave's 1-byte store overhang at the region end
            ref byte d = ref buffer.GetReference(((regionEnd - i) * MessagePackPrimitives.MaxUInt64Length) + 1);
            int written = 0;
#if NET9_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && Avx512F.IsSupported)
            {
                while (i + 8 <= regionEnd)
                {
                    var v = Vector512.LoadUnsafe(ref src, (nuint)i);
                    if (Vector512.LessThanOrEqualAll(v, Vector512.Create(127UL)))
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written), Avx512F.ConvertToVector128SByte(v.AsInt64()).AsUInt64().ToScalar());
                        written += 8;
                        i += 8;
                    }
                    else if (Vector512.GreaterThanOrEqualAll(v, Vector512.Create(4294967296UL)))
                    {
                        if (Avx512Vbmi.IsSupported)
                        {
                            // the int64 overlap weave with a 0xcf-filled code source
                            var codeFill = Vector512.Create((byte)0xcf);
                            Avx512Vbmi.PermuteVar64x8x2(v.AsByte(), WeaveTables.Token9WeaveIndices512, codeFill)
                                .StoreUnsafe(ref Unsafe.Add(ref d, written));
                            Avx512Vbmi.PermuteVar64x8x2(v.AsByte(), WeaveTables.Token9WeaveShiftedIndices512, codeFill)
                                .StoreUnsafe(ref Unsafe.Add(ref d, written + 9));
                        }
                        else
                        {
                            EmitWide8(ref d, written, ref src, i);
                        }
                        written += 72;
                        i += 8;
                    }
                    else
                    {
                        int end = i + 8;
                        for (; i < end; i++)
                        {
                            written += MessagePackPrimitives.UnsafeWriteUInt64(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                while (i + 8 <= regionEnd)
                {
                    var a = Vector256.LoadUnsafe(ref src, (nuint)i);
                    var b = Vector256.LoadUnsafe(ref src, (nuint)(i + 4));
                    if (Vector256.LessThanOrEqualAll(Vector256.Max(a, b), Vector256.Create(127UL)))
                    {
                        for (int t = 0; t < 8; t++)
                        {
                            Unsafe.Add(ref d, written + t) = (byte)Unsafe.Add(ref src, i + t);
                        }
                        written += 8;
                        i += 8;
                    }
                    else if (Vector256.GreaterThanOrEqualAll(Vector256.Min(a, b), Vector256.Create(4294967296UL)))
                    {
                        if (Avx2.IsSupported)
                        {
                            // the int64 256 weave with the constant-code OR
                            WeaveWide3(ref d, written, a);
                            WeaveWide3(ref d, written + 27, Vector256.LoadUnsafe(ref src, (nuint)(i + 3)));
                            var pair = b.GetUpper().AsByte();
                            Ssse3.Shuffle(pair, WeaveTables.Token9WeaveIndices128).StoreUnsafe(ref Unsafe.Add(ref d, written + 55));
                            Unsafe.Add(ref d, written + 54) = 0xcf;
                            Unsafe.Add(ref d, written + 63) = 0xcf;
                            Unsafe.Add(ref d, written + 71) = (byte)Unsafe.Add(ref src, i + 7);
                        }
                        else
                        {
                            EmitWide8(ref d, written, ref src, i);
                        }
                        written += 72;
                        i += 8;
                    }
                    else
                    {
                        int end = i + 8;
                        for (; i < end; i++)
                        {
                            written += MessagePackPrimitives.UnsafeWriteUInt64(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
                        }
                    }
                }
            }
#endif
            for (; i < regionEnd; i++)
            {
                written += MessagePackPrimitives.UnsafeWriteUInt64(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
            }
            buffer.Advance(written);
        }
    }

#if NET9_0_OR_GREATER
    static void EmitWide8(ref byte d, int written, ref ulong src, int i)
    {
        for (int t = 0; t < 8; t++)
        {
            Unsafe.Add(ref d, written + (t * 9)) = 0xcf;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + (t * 9) + 1), MessagePackEndian.ToBigEndian(Unsafe.Add(ref src, i + t)));
        }
    }

    /// <summary>the int64 3-token weave with the constant-code OR (every token is 0xcf)</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WeaveWide3(ref byte d, int at, Vector256<ulong> loaded)
    {
        var p = Avx2.Permute4x64(loaded, 0b10_01_01_00).AsByte();
        (Avx2.Shuffle(p, WeaveTables.Token9WeaveIndices256) | WeaveTables.ULongCode256).StoreUnsafe(ref Unsafe.Add(ref d, at));
    }

    // every uint64 payload fits ulong, so only the code bytes are validated; int64
    // tokens (0xd3, valid for non-negative values) fall to the scalar reader
    static bool TryDecodeWide8(ref byte w0, ref ulong dst, int i)
    {
        if (Unsafe.Add(ref w0, 0) != 0xcf || Unsafe.Add(ref w0, 9) != 0xcf)
        {
            return false;
        }
        bool ok = true;
        for (int t = 0; t < 8; t++)
        {
            ok &= Unsafe.Add(ref w0, t * 9) == 0xcf;
            Unsafe.Add(ref dst, i + t) = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref w0, (t * 9) + 1)));
        }
        return ok;
    }
#endif

    public void ReadElements<TReadBuffer>(ref TReadBuffer buffer, Span<ulong> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref ulong dst = ref MemoryMarshal.GetReference(destination);
        int count = destination.Length;

        int i = 0;
#if NET9_0_OR_GREATER
        if (Vector128.IsHardwareAccelerated)
        {
            while (i + 8 <= count)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 16)
                {
                    // positive fixint only, lower 8 lanes (the load needs 16B of window)
                    var codes = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if ((Vector128.LessThanOrEqual(codes, Vector128.Create((byte)0x7f)).ExtractMostSignificantBits() & 0xFF) == 0xFF)
                    {
                        if (Vector512.IsHardwareAccelerated && Avx512F.IsSupported)
                        {
                            // zero-extend 8 fixint bytes to 8 ulongs (vpmovzxbq)
                            Avx512F.ConvertToVector512Int64(codes).AsUInt64().StoreUnsafe(ref dst, (nuint)i);
                        }
                        else
                        {
                            ref byte w0 = ref MemoryMarshal.GetReference(window);
                            for (int t = 0; t < 8; t++)
                            {
                                Unsafe.Add(ref dst, i + t) = Unsafe.Add(ref w0, t);
                            }
                        }
                        buffer.Advance(8);
                        i += 8;
                        continue;
                    }
                    if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported && window.Length >= 64)
                    {
                        // the int64 gather with a single 0xcf code pattern and no
                        // overflow test (every uint64 payload fits); lanes 0-6 valid
                        var w = Vector512.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        var codes8 = Avx512Vbmi.PermuteVar64x8(w, WeaveTables.Token9DecodeCodes512).AsInt64();
                        ulong cf = Vector512.Equals(codes8, Vector512.Create(unchecked((long)0xCFCFCFCFCFCFCFCF))).ExtractMostSignificantBits();
                        if ((cf & 0x7FUL) == 0x7FUL)
                        {
                            Avx512Vbmi.PermuteVar64x8(w, WeaveTables.Token9DecodeValues512).AsUInt64()
                                .StoreUnsafe(ref dst, (nuint)i);
                            buffer.Advance(63);
                            i += 7;
                            continue;
                        }
                    }
                    else if (Avx2.IsSupported && window.Length >= 32)
                    {
                        var w = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                        if ((Vector256.Equals(w, Vector256.Create((byte)0xcf)).ExtractMostSignificantBits() & 0x40201u) == 0x40201u)
                        {
                            var vB = Avx2.Permute4x64(w.AsUInt64(), 0b00_00_10_01).AsByte();
                            (Avx2.Shuffle(w, WeaveTables.Token9DecodeShuffleA256) | Avx2.Shuffle(vB, WeaveTables.Token9DecodeShuffleB256))
                                .StoreUnsafe(ref Unsafe.As<ulong, byte>(ref dst), (nuint)i * 8);
                            buffer.Advance(27);
                            i += 3;
                            continue;
                        }
                    }
                    else if (window.Length >= 72 && TryDecodeWide8(ref MemoryMarshal.GetReference(window), ref dst, i))
                    {
                        buffer.Advance(72);
                        i += 8;
                        continue;
                    }
                }
                int end = i + 8;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadUInt64();
                }
            }
        }
#endif
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadUInt64();
        }
    }
}

#endregion

#region float

/// <summary>
/// float32 element core.
/// </summary>
internal readonly struct SingleElementCodec : IElementCodec<float>
{
    internal const int SerializeRegionElements = 4096; // * 5B = 20KB

    public void WriteElements<TWriteBuffer>(ref TWriteBuffer buffer, ReadOnlySpan<float> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref float src = ref MemoryMarshal.GetReference(source);
        int length = source.Length;

        int i = 0;
        while (i < length)
        {
            int regionEnd = i + Math.Min(length - i, SerializeRegionElements);
            ref byte d = ref buffer.GetReference((regionEnd - i) * MessagePackPrimitives.MaxFloat32Length);
            int written = 0;
#if NET9_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
            {
                // the guard needs 16 readable input floats but consumes 13, so the store
                // (1 scalar lead + 64B vector) always stays within the region reservation
                var codeFill = Vector512.Create(MessagePackCode.Float32);
                for (; regionEnd - i >= 16; i += 13, written += 65)
                {
                    Unsafe.Add(ref d, written) = MessagePackCode.Float32;
                    var loaded = Vector512.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();

                    // float(4) -> msgpack float32 [code][big-endian float](5), one two-source
                    // permute per 13 tokens. Payload indices gather from loaded, the C slots
                    // gather the code byte from codeFill, so the block leaves the permute finished
                    Avx512Vbmi.PermuteVar64x8x2(loaded, WeaveTables.FloatWeaveIndices512, codeFill)
                        .StoreUnsafe(ref Unsafe.Add(ref d, written + 1));
                }
            }
            else if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
            {
                // 6 tokens per 32B store, 30B advance, unrolled x2 to amortize the loop overhead.
                // The first store's 2 overhang bytes are immediately overwritten by the second
                // store. The pair guard needs 14 readable floats because the second load starts
                // at i + 6 and reads 8; the single-width loop then mops up 8..13 remaining.
                for (; regionEnd - i >= 14; i += 12, written += 60)
                {
                    var a = Vector256.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                    var b = Vector256.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)(i + 6)).AsByte();
                    // vpermq imm8, 2 bits per output qword read from the LSB, so q3..q0 = src q[2,1,1,0]
                    var permutedA = Avx2.Permute4x64(a.AsUInt64(), 0b10_01_01_00).AsByte();
                    var permutedB = Avx2.Permute4x64(b.AsUInt64(), 0b10_01_01_00).AsByte();
                    (Avx2.Shuffle(permutedA, WeaveTables.Token5WeaveIndices256) | WeaveTables.FloatCode256).StoreUnsafe(ref Unsafe.Add(ref d, written));
                    (Avx2.Shuffle(permutedB, WeaveTables.Token5WeaveIndices256) | WeaveTables.FloatCode256).StoreUnsafe(ref Unsafe.Add(ref d, written + 30));
                }
                for (; regionEnd - i >= 8; i += 6, written += 30)
                {
                    var loaded = Vector256.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                    var permuted = Avx2.Permute4x64(loaded.AsUInt64(), 0b10_01_01_00).AsByte();
                    (Avx2.Shuffle(permuted, WeaveTables.Token5WeaveIndices256) | WeaveTables.FloatCode256).StoreUnsafe(ref Unsafe.Add(ref d, written));
                }
            }
            // the portable shuffle tables index in-lane bytes assuming little-endian
            // element layout, so this tier (reachable off x86, unlike the Avx tiers)
            // needs the endian gate; the JIT constant folds it away on little-endian
            else if (Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian)
            {
                // 3 tokens per 16B store, 15B advance (1B overhang). Unrolled x2 (measured
                // 9-35% faster): store 1's overhang byte at written + 15 is immediately
                // rewritten by store 2; guard 8 consume 6, so a following iteration or the
                // remainder/scalar tail always rewrites store 2's overhang in-region.
                for (; regionEnd - i >= 8; i += 6, written += 30)
                {
                    var a = Vector128.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                    var b = Vector128.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)(i + 3)).AsByte();
                    var shuffledA = Ssse3.IsSupported
                        ? Ssse3.Shuffle(a, WeaveTables.Token5WeaveIndices128)
                        : Vector128.Shuffle(a, WeaveTables.Token5WeaveIndices128Portable) & WeaveTables.Token5WeaveMask128;
                    var shuffledB = Ssse3.IsSupported
                        ? Ssse3.Shuffle(b, WeaveTables.Token5WeaveIndices128)
                        : Vector128.Shuffle(b, WeaveTables.Token5WeaveIndices128Portable) & WeaveTables.Token5WeaveMask128;
                    (shuffledA | WeaveTables.FloatCode128).StoreUnsafe(ref Unsafe.Add(ref d, written));
                    (shuffledB | WeaveTables.FloatCode128).StoreUnsafe(ref Unsafe.Add(ref d, written + 15));
                }
                for (; regionEnd - i >= 4; i += 3, written += 15)
                {
                    var loaded = Vector128.LoadUnsafe(ref Unsafe.As<float, int>(ref src), (nuint)i).AsByte();
                    var shuffled = Ssse3.IsSupported
                        ? Ssse3.Shuffle(loaded, WeaveTables.Token5WeaveIndices128)
                        : Vector128.Shuffle(loaded, WeaveTables.Token5WeaveIndices128Portable) & WeaveTables.Token5WeaveMask128;
                    (shuffled | WeaveTables.FloatCode128).StoreUnsafe(ref Unsafe.Add(ref d, written));
                }
            }
#endif
            for (; i < regionEnd; i++)
            {
                written += MessagePackPrimitives.UnsafeWriteSingle(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
            }
            buffer.Advance(written);
        }
    }

    public void ReadElements<TReadBuffer>(ref TReadBuffer buffer, Span<float> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref float dst = ref MemoryMarshal.GetReference(destination);
        int count = destination.Length;

        int i = 0;
#if NET9_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            while (count - i >= 16)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 16 * 5)
                {
                    // the same two-source gather as the int32 wide decode (the token
                    // shape [code][4B BE] is identical); validation is one 0xca compare
                    ref byte w0 = ref MemoryMarshal.GetReference(window);
                    var lo512 = Vector512.LoadUnsafe(ref w0);
                    var hi512 = Vector512.LoadUnsafe(ref Unsafe.Add(ref w0, 16));
                    var codes4 = Avx512Vbmi.PermuteVar64x8x2(lo512, WeaveTables.Token5DecodeCodes512, hi512).AsInt32();
                    if (Vector512.EqualsAll(codes4, Vector512.Create(unchecked((int)0xcacacaca))))
                    {
                        Avx512Vbmi.PermuteVar64x8x2(lo512, WeaveTables.Token5DecodeValues512, hi512).AsInt32()
                            .StoreUnsafe(ref Unsafe.As<float, int>(ref dst), (nuint)i);
                        buffer.Advance(16 * 5);
                        i += 16;
                        continue;
                    }
                }
                int end = i + 16;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadSingle();
                }
            }
        }
        else if (Avx2.IsSupported)
        {
            // 6 tokens per 32B window. The two 16B halves store 12B apart; their
            // garbage tails and the dst slot past the 6 decoded floats are rewritten by
            // the next store or the scalar tail (the dst guard keeps them in bounds).
            while (count - i >= 8)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 32)
                {
                    var w = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if ((Vector256.Equals(w, Vector256.Create(MessagePackCode.Float32)).ExtractMostSignificantBits() & 0x2108421u) == 0x2108421u)
                    {
                        var shuffled = Avx2.Shuffle(w, WeaveTables.Token5DecodeShuffle256);
                        shuffled.GetLower().StoreUnsafe(ref Unsafe.As<float, byte>(ref dst), (nuint)i * 4);
                        shuffled.GetUpper().StoreUnsafe(ref Unsafe.As<float, byte>(ref dst), ((nuint)i * 4) + 12);
                        buffer.Advance(30);
                        i += 6;
                        continue;
                    }
                }
                int end = Math.Min(i + 6, count);
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadSingle();
                }
            }
        }
        else if (Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian)
        {
            // 3 tokens per 16B window, same self-healing store contract (the endian
            // gate covers the portable shuffle's little-endian lane assumption)
            while (count - i >= 4)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 16)
                {
                    var w = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if ((Vector128.Equals(w, Vector128.Create(MessagePackCode.Float32)).ExtractMostSignificantBits() & 0x421u) == 0x421u)
                    {
                        var shuffled = Ssse3.IsSupported
                            ? Ssse3.Shuffle(w, WeaveTables.Token5DecodeShuffle128)
                            : Vector128.Shuffle(w, WeaveTables.Token5DecodeShuffle128Portable);
                        shuffled.StoreUnsafe(ref Unsafe.As<float, byte>(ref dst), (nuint)i * 4);
                        buffer.Advance(15);
                        i += 3;
                        continue;
                    }
                }
                int end = Math.Min(i + 3, count);
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadSingle();
                }
            }
        }
        else
#endif
        {
            while (count - i >= 16)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 16 * 5 && TryDecodeFloat16(ref MemoryMarshal.GetReference(window), ref dst, i))
                {
                    buffer.Advance(16 * 5);
                    i += 16;
                    continue;
                }
                int end = i + 16;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadSingle();
                }
            }
        }
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadSingle();
        }
    }

    // Values are stored before validation — on a failed gate the caller's scalar reader
    // re-reads the unadvanced buffer and overwrites (or throws), same contract as the
    // int32 wide decode. The two-code probe keeps mixed-window misfires cheap.
    static bool TryDecodeFloat16(ref byte w0, ref float dst, int i)
    {
        if (Unsafe.Add(ref w0, 0) != MessagePackCode.Float32 || Unsafe.Add(ref w0, 5) != MessagePackCode.Float32)
        {
            return false;
        }
        bool ok = true;
        for (int t = 0; t < 16; t++)
        {
            ok &= Unsafe.Add(ref w0, t * 5) == MessagePackCode.Float32;
            var bits = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref w0, (t * 5) + 1)));
            Unsafe.Add(ref dst, i + t) = BitConverter.UInt32BitsToSingle(bits);
        }
        return ok;
    }
}

/// <summary>float64 element core: same strategy as <see cref="SingleElementCodec"/> with 9-byte tokens.</summary>
internal readonly struct DoubleElementCodec : IElementCodec<double>
{
    internal const int SerializeRegionElements = 2048; // * 9B = 18KB

    public void WriteElements<TWriteBuffer>(ref TWriteBuffer buffer, ReadOnlySpan<double> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref double src = ref MemoryMarshal.GetReference(source);
        int length = source.Length;

        int i = 0;
        while (i < length)
        {
            int regionEnd = i + Math.Min(length - i, SerializeRegionElements);
            ref byte d = ref buffer.GetReference((regionEnd - i) * MessagePackPrimitives.MaxFloat64Length);
            int written = 0;
#if NET9_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
            {
                // 7 tokens per 64B store, 63B advance: the 64th byte lands in the next
                // token's code slot. The guard needs 8 readable but consumes 7, so at
                // least one in-region token always follows and rewrites that byte
                // (idempotently: it is the same constant code); the reservation covers
                // it because >= 1 remaining token means >= 9 reserved bytes ahead.
                var codeFill = Vector512.Create(MessagePackCode.Float64);
                for (; regionEnd - i >= 8; i += 7, written += 63)
                {
                    var loaded = Vector512.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)i).AsByte();
                    Avx512Vbmi.PermuteVar64x8x2(loaded, WeaveTables.DoubleWeaveIndices512, codeFill)
                        .StoreUnsafe(ref Unsafe.Add(ref d, written));
                }
            }
            else if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
            {
                // 3 tokens per 32B store, 27B advance (5B overhang). Unrolled x2 (measured
                // 10-24% faster): store 1's overhang at written + 27 is immediately rewritten
                // by store 2; the guard consumes 6 but requires 8, so a following iteration or
                // the remainder/scalar tail always rewrites store 2's overhang in-region.
                for (; regionEnd - i >= 8; i += 6, written += 54)
                {
                    var a = Vector256.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)i);
                    var b = Vector256.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)(i + 3));
                    // vpermq imm8, 2 bits per output qword read from the LSB, so q3..q0 = src q[2,1,1,0]
                    var permutedA = Avx2.Permute4x64(a.AsUInt64(), 0b10_01_01_00).AsByte();
                    var permutedB = Avx2.Permute4x64(b.AsUInt64(), 0b10_01_01_00).AsByte();
                    (Avx2.Shuffle(permutedA, WeaveTables.Token9WeaveIndices256) | WeaveTables.DoubleCode256).StoreUnsafe(ref Unsafe.Add(ref d, written));
                    (Avx2.Shuffle(permutedB, WeaveTables.Token9WeaveIndices256) | WeaveTables.DoubleCode256).StoreUnsafe(ref Unsafe.Add(ref d, written + 27));
                }
                for (; regionEnd - i >= 4; i += 3, written += 27)
                {
                    var loaded = Vector256.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)i);
                    var permuted = Avx2.Permute4x64(loaded.AsUInt64(), 0b10_01_01_00).AsByte();
                    (Avx2.Shuffle(permuted, WeaveTables.Token9WeaveIndices256) | WeaveTables.DoubleCode256).StoreUnsafe(ref Unsafe.Add(ref d, written));
                }
            }
            else if (Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian)
            {
                // 2 tokens per exact 18B: token codes and token1's last payload byte go
                // scalar (the 16B vector covers [p0(8B)][code1][p1 upper 7B]). The last
                // byte MUST be read from element i + 1, not a fixed offset; a constant
                // offset would corrupt every iteration after the first.
                for (; regionEnd - i >= 2; i += 2, written += 18)
                {
                    Unsafe.Add(ref d, written) = MessagePackCode.Float64;
                    Unsafe.Add(ref d, written + 17) = Unsafe.As<double, byte>(ref Unsafe.Add(ref src, i + 1));
                    var loaded = Vector128.LoadUnsafe(ref Unsafe.As<double, long>(ref src), (nuint)i).AsByte();
                    var shuffled = Ssse3.IsSupported
                        ? Ssse3.Shuffle(loaded, WeaveTables.Token9WeaveIndices128)
                        : Vector128.Shuffle(loaded, WeaveTables.Token9WeaveIndices128Portable) & WeaveTables.Token9WeaveMask128;
                    (shuffled | WeaveTables.DoubleCode128).StoreUnsafe(ref Unsafe.Add(ref d, written + 1));
                }
            }
#endif
            for (; i < regionEnd; i++)
            {
                written += MessagePackPrimitives.UnsafeWriteDouble(ref Unsafe.Add(ref d, written), Unsafe.Add(ref src, i));
            }
            buffer.Advance(written);
        }
    }

    public void ReadElements<TReadBuffer>(ref TReadBuffer buffer, Span<double> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref double dst = ref MemoryMarshal.GetReference(destination);
        int count = destination.Length;

        int i = 0;
#if NET9_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
        {
            // single-source gather: 7 tokens from a 64B window (see Token9DecodeValues512).
            // Lane 7 stores a garbage qword into dst[i + 7] (the count - i >= 8 guard
            // keeps it in bounds); that slot is decoded again by the next iteration or
            // the scalar tail, so the garbage is never observable.
            while (count - i >= 8)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 64)
                {
                    var w = Vector512.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    var codes = Avx512Vbmi.PermuteVar64x8(w, WeaveTables.Token9DecodeCodes512).AsInt64();
                    if (Vector512.EqualsAll(codes, Vector512.Create(unchecked((long)0xCBCBCBCBCBCBCBCB))))
                    {
                        Avx512Vbmi.PermuteVar64x8(w, WeaveTables.Token9DecodeValues512).AsInt64()
                            .StoreUnsafe(ref Unsafe.As<double, long>(ref dst), (nuint)i);
                        buffer.Advance(7 * 9);
                        i += 7;
                        continue;
                    }
                }
                int end = Math.Min(i + 8, count);
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadDouble();
                }
            }
        }
        else if (Avx2.IsSupported)
        {
            // 3 tokens per 32B window (measured 3.1x over the fused-scalar decode).
            // Token 1's payload straddles the lane boundary, so a vpermq feeds a second
            // shuffle source; the two shuffles OR into one 24B-effective store whose
            // 8B garbage tail is rewritten by the next store or the scalar tail.
            while (count - i >= 4)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 32)
                {
                    var w = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if ((Vector256.Equals(w, Vector256.Create(MessagePackCode.Float64)).ExtractMostSignificantBits() & 0x40201u) == 0x40201u)
                    {
                        var vB = Avx2.Permute4x64(w.AsUInt64(), 0b00_00_10_01).AsByte();
                        (Avx2.Shuffle(w, WeaveTables.Token9DecodeShuffleA256) | Avx2.Shuffle(vB, WeaveTables.Token9DecodeShuffleB256))
                            .StoreUnsafe(ref Unsafe.As<double, byte>(ref dst), (nuint)i * 8);
                        buffer.Advance(27);
                        i += 3;
                        continue;
                    }
                }
                int end = Math.Min(i + 3, count);
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadDouble();
                }
            }
        }
        else if (Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian)
        {
            // 1 token per 16B window, still 1.35x over the scalar reader: the vector
            // byte-reverses the payload and the store's 8B garbage tail is rewritten by
            // the next store or the scalar tail (guarded by count - i >= 2)
            while (count - i >= 2)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 16 && window[0] == MessagePackCode.Float64)
                {
                    var w = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    var shuffled = Ssse3.IsSupported
                        ? Ssse3.Shuffle(w, WeaveTables.Token9DecodeShuffle128)
                        : Vector128.Shuffle(w, WeaveTables.Token9DecodeShuffle128Portable);
                    shuffled.StoreUnsafe(ref Unsafe.As<double, byte>(ref dst), (nuint)i * 8);
                    buffer.Advance(9);
                    i += 1;
                    continue;
                }
                Unsafe.Add(ref dst, i) = buffer.ReadDouble();
                i += 1;
            }
        }
        else
#endif
        {
            while (count - i >= 16)
            {
                var window = buffer.GetCurrentSpan();
                if (window.Length >= 16 * 9 && TryDecodeDouble16(ref MemoryMarshal.GetReference(window), ref dst, i))
                {
                    buffer.Advance(16 * 9);
                    i += 16;
                    continue;
                }
                int end = i + 16;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadDouble();
                }
            }
        }
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadDouble();
        }
    }

    static bool TryDecodeDouble16(ref byte w0, ref double dst, int i)
    {
        if (Unsafe.Add(ref w0, 0) != MessagePackCode.Float64 || Unsafe.Add(ref w0, 9) != MessagePackCode.Float64)
        {
            return false;
        }
        bool ok = true;
        for (int t = 0; t < 16; t++)
        {
            ok &= Unsafe.Add(ref w0, t * 9) == MessagePackCode.Float64;
            var bits = MessagePackEndian.FromBigEndian(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref w0, (t * 9) + 1)));
            Unsafe.Add(ref dst, i + t) = BitConverter.UInt64BitsToDouble(bits);
        }
        return ok;
    }
}

#endregion

#region bool

/// <summary>
/// bool element core: tokens are exactly one byte (0xc2/0xc3)
/// so both directions are a straight 16-lane compare/select.
/// no classify, no stride bookkeeping.
/// </summary>
internal readonly struct BooleanElementCodec : IElementCodec<bool>
{
    internal const int SerializeRegionElements = 16384; // * 1B = 16KB

    public void WriteElements<TWriteBuffer>(ref TWriteBuffer buffer, ReadOnlySpan<bool> source)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref bool src = ref MemoryMarshal.GetReference(source);
        int length = source.Length;

        // Every token is exactly 1 byte, so element index and output offset are always 1:1.
        // That makes the overlapped-tail trick safe: the last full vector is redone ending
        // exactly at the region boundary, rewriting identical bytes, and no scalar tail remains.
        int i = 0;
        while (i < length)
        {
            int regionStart = i;
            int regionEnd = i + Math.Min(length - i, SerializeRegionElements);
            ref byte d = ref buffer.GetReference(regionEnd - i);
#if NET9_0_OR_GREATER
            if (Vector128.IsHardwareAccelerated && regionEnd - regionStart >= 16)
            {
                // Min(v, 1) => 0(false) = 0, others(true) is normalize, any non-zero value becomes 1, and 0 stays 0
                // + MessagePackCode.False(194) => 0(false) = 194(MessagePackCode.False), 1(true) = 195(MessagePackCode.True)
                if (Vector512.IsHardwareAccelerated)
                {
                    for (; regionEnd - i >= 64; i += 64)
                    {
                        var v = Vector512.LoadUnsafe(ref Unsafe.As<bool, byte>(ref src), (nuint)i);
                        (Vector512.Min(v, Vector512.Create((byte)1)) + Vector512.Create(MessagePackCode.False))
                            .StoreUnsafe(ref Unsafe.Add(ref d, i - regionStart));
                    }
                }
                if (Vector256.IsHardwareAccelerated)
                {
                    for (; regionEnd - i >= 32; i += 32)
                    {
                        var v = Vector256.LoadUnsafe(ref Unsafe.As<bool, byte>(ref src), (nuint)i);
                        (Vector256.Min(v, Vector256.Create((byte)1)) + Vector256.Create(MessagePackCode.False))
                            .StoreUnsafe(ref Unsafe.Add(ref d, i - regionStart));
                    }
                }
                for (; regionEnd - i >= 16; i += 16)
                {
                    var v = Vector128.LoadUnsafe(ref Unsafe.As<bool, byte>(ref src), (nuint)i);
                    (Vector128.Min(v, Vector128.Create((byte)1)) + Vector128.Create(MessagePackCode.False))
                        .StoreUnsafe(ref Unsafe.Add(ref d, i - regionStart));
                }

                if (i < regionEnd)
                {
                    // overlapped tail: redo the last 16 elements of the region, rewriting
                    // identical bytes, so no scalar tail remains
                    var v = Vector128.LoadUnsafe(ref Unsafe.As<bool, byte>(ref src), (nuint)(regionEnd - 16));
                    (Vector128.Min(v, Vector128.Create((byte)1)) + Vector128.Create(MessagePackCode.False))
                        .StoreUnsafe(ref Unsafe.Add(ref d, regionEnd - 16 - regionStart));
                    i = regionEnd;
                }
            }
            else
#endif
            {
                for (; i < regionEnd; i++)
                {
                    MessagePackPrimitives.UnsafeWriteBoolean(ref Unsafe.Add(ref d, i - regionStart), Unsafe.Add(ref src, i));
                }
            }
            buffer.Advance(regionEnd - regionStart);
        }
    }

    public void ReadElements<TReadBuffer>(ref TReadBuffer buffer, Span<bool> destination)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ref bool dst = ref MemoryMarshal.GetReference(destination);
        int count = destination.Length;

        int i = 0;
#if NET9_0_OR_GREATER
        if (Vector128.IsHardwareAccelerated)
        {
            while (count - i >= 16)
            {
                // First, verify that every lane in the window is a bool token.
                // (v & 0xFE) folds the two codes onto one, since 0xc2(False) & 0xfe = 0xc2 and 0xc3(True) & 0xfe = 0xc2,
                // so a single EqualsAll against 0xc2 validates the whole window and any other code fails it.
                // A failed gate (a foreign token, or a window shorter than the vector) falls back to the scalar
                // ReadBoolean for these elements, which decodes the valid prefix and throws at the exact offending token.
                // The failed probe stored and advanced nothing, so the scalar reader sees an untouched buffer.

                // False and True differ only in the lowest bit (1100_0010, 1100_0011),
                // so (v & 1) converts a validated lane straight into the canonical 0(false) and 1(true).

                var window = buffer.GetCurrentSpan();
                if (Vector512.IsHardwareAccelerated && count - i >= 64 && window.Length >= 64)
                {
                    var v64 = Vector512.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if (Vector512.EqualsAll(v64 & Vector512.Create((byte)0xFE), Vector512.Create(MessagePackCode.False)))
                    {
                        (v64 & Vector512.Create((byte)1)).StoreUnsafe(ref Unsafe.As<bool, byte>(ref dst), (nuint)i);
                        buffer.Advance(64);
                        i += 64;
                        continue;
                    }
                }
                if (Vector256.IsHardwareAccelerated && count - i >= 32 && window.Length >= 32)
                {
                    var v32 = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if (Vector256.EqualsAll(v32 & Vector256.Create((byte)0xFE), Vector256.Create(MessagePackCode.False)))
                    {
                        (v32 & Vector256.Create((byte)1)).StoreUnsafe(ref Unsafe.As<bool, byte>(ref dst), (nuint)i);
                        buffer.Advance(32);
                        i += 32;
                        continue;
                    }
                }
                if (window.Length >= 16)
                {
                    var v = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(window));
                    if (Vector128.EqualsAll(v & Vector128.Create((byte)0xFE), Vector128.Create(MessagePackCode.False)))
                    {
                        (v & Vector128.Create((byte)1)).StoreUnsafe(ref Unsafe.As<bool, byte>(ref dst), (nuint)i);
                        buffer.Advance(16);
                        i += 16;
                        continue;
                    }
                }
                int end = i + 16;
                for (; i < end; i++)
                {
                    Unsafe.Add(ref dst, i) = buffer.ReadBoolean();
                }
            }
        }
#endif
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadBoolean();
        }
    }
}

#endregion