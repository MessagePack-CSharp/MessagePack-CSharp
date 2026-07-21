using SerializerFoundation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace UltraMessagePack.Formatters;

/// <summary>
/// Specialized int[] formatter. The per-element decode loop is latency-bound on the
/// "code byte -> length -> next message address" serial chain (~15 cycles/element,
/// measured); the only way past it is to stop being element-at-a-time. Runs of fixint
/// elements (the dominant case in real payloads) have a known element size of 1 byte,
/// so 16 elements are classified with one SIMD compare and converted with one
/// vpmovsxbd / vpmovdb; the homogeneous complement — all-wide runs, 5-byte tokens — is
/// likewise fixed-stride and gets wide superlanes in both directions (vpermi2b weave
/// on AVX-512VBMI; constant-stride scalar EmitWide16 / TryDecodeWide16 elsewhere). Mixed
/// serialize 16s go through the scalar classify chain into the
/// region reservation — measured against a kinds-style vectorized multi-class emit
/// (MessagePack-CSharp's LittleEndianSerialize strategy, both switch and branchless
/// variants) and the scalar chain was equal or faster in every cell at N=1000 AND
/// N=100_000, so the vector classification was dropped (see Int32ArrayFormatterBenchmark).
/// On deserialize, windows shorter than 16 bytes (sequence segment boundaries) fall to
/// the scalar reader, which absorbs the stitch, then the SIMD loop resumes.
/// </summary>
// vpermi2b index tables for the wide superlanes (16 tokens x 5 bytes = an 80-byte run).
// Entry j says where OUTPUT byte j comes from: values 0..63 pick byte p of the FIRST
// source register, values 64..127 pick byte (value - 64) of the SECOND source
// (bit 6 of a vpermi2b index selects the register).
file static class WideLaneTables
{
    // Per-dword byte reversal: turns each little-endian int32 into big-endian byte
    // order in place (dword t's bytes 4t..4t+3 become [b3 b2 b1 b0]).
    public static readonly Vector512<byte> ByteReverse = Vector512.Create(
        (byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12,
        19, 18, 17, 16, 23, 22, 21, 20, 27, 26, 25, 24, 31, 30, 29, 28,
        35, 34, 33, 32, 39, 38, 37, 36, 43, 42, 41, 40, 47, 46, 45, 44,
        51, 50, 49, 48, 55, 54, 53, 52, 59, 58, 57, 56, 63, 62, 61, 60);

    // Serialize weave, output bytes 0..63 of the token run. First source = ByteReverse'd
    // payloads (token t's BE bytes at 4t..4t+3), second source = code dwords (token t's
    // code byte at 4t, so index 64+4t). Each row below is one token [code | b3 b2 b1 b0];
    // token 12 is cut off after 4 bytes — Weave16 finishes it.
    public static readonly Vector512<byte> Weave0 = Vector512.Create(
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
        112, 48, 49, 50);        // t12 (b0 completed by Weave16)

    // Serialize weave, output bytes 16..79, stored at offset +16 (a full-width store in
    // place of a masked tail; entries for bytes 16..63 produce the same values as
    // Weave0, so the 48-byte overlap is written twice with identical data).
    public static readonly Vector512<byte> Weave16 = Vector512.Create(
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
    public static readonly Vector512<byte> DecodeValues = Vector512.Create(
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
    public static readonly Vector512<byte> DecodeCodes = Vector512.Create(
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
}

public sealed class Int32ArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int[]?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    // Serialize reserves worst case (5 bytes/element) for a region up front and emits into
    // it with a register-accumulated offset, one Advance per region. Everything inside the
    // region loop is pure register arithmetic — per-element buffer calls pay byref
    // field-reload traffic (index/capacity/pointer re-read from memory each iteration;
    // large structs passed by ref don't get promoted), measured ~2x the cost of the store
    // itself. The cap bounds the reservation for huge arrays (4096 * 5 = 20KB).
    const int SerializeRegionElements = 4096;

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref int[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        int length = value.Length;
        buffer.WriteArrayHeader(length);
        ref int src = ref MemoryMarshal.GetArrayDataReference(value);
        int i = 0;
        while (i < length)
        {
            int regionEnd = i + Math.Min(length - i, SerializeRegionElements);
            ref byte d = ref buffer.GetReference((regionEnd - i) * MessagePackPrimitives.MaxInt32Length);
            int written = 0;
            if (Vector512.IsHardwareAccelerated && Avx512F.IsSupported)
            {
                while (i + 16 <= regionEnd)
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
                            var be = Vector512.Shuffle(v.AsByte(), WideLaneTables.ByteReverse);
                            var codes = (Vector512.Create(0x000000ce) ^ (Vector512.ShiftRightArithmetic(v, 31) & Vector512.Create(0x0000001c))).AsByte();
                            Avx512Vbmi.PermuteVar64x8x2(be, WideLaneTables.Weave0, codes).StoreUnsafe(ref Unsafe.Add(ref d, written));
                            Avx512Vbmi.PermuteVar64x8x2(be, WideLaneTables.Weave16, codes).StoreUnsafe(ref Unsafe.Add(ref d, written + 16));
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
                while (i + 16 <= regionEnd)
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
                        EmitWide16(ref d, written, ref src, i);
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
                while (i + 16 <= regionEnd)
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
                            EmitWide16(ref d, written, ref src, i);
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
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, written + (t * 5) + 1), System.Buffers.Binary.BinaryPrimitives.ReverseEndianness((uint)v));
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
            int v = (int)System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref w0, (t * 5) + 1)));
            Unsafe.Add(ref dst, i + t) = v;
            ok &= (c == MessagePackCode.Int32) | (c == MessagePackCode.UInt32);
            uintOverflow |= (c == MessagePackCode.UInt32) & (v < 0);
        }
        return ok && !uintOverflow;

        static bool IsWideCode(byte c) => (c == MessagePackCode.Int32) | (c == MessagePackCode.UInt32);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        int count = buffer.ReadArrayHeader();
        // Populate contract: reuse the incoming array only on an exact length match;
        // a fresh array may be uninitialized because every element is written below
        var result = (value != null && value.Length == count) ? value : GC.AllocateUninitializedArray<int>(count);
        ref int dst = ref MemoryMarshal.GetArrayDataReference(result);
        int i = 0;
        if (Vector128.IsHardwareAccelerated)
        {
            while (i + 16 <= count)
            {
                // fixint elements are exactly 1 byte, so 16 elements = the next 16 bytes
                // of the window. A window shorter than 16 (buffer tail or sequence
                // segment boundary) takes the scalar chunk, whose per-element reader
                // stitches across the boundary; SIMD resumes on the next window.
                var window = buffer.GetSpan();
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
                        var vals = Avx512Vbmi.PermuteVar64x8x2(lo512, WideLaneTables.DecodeValues, hi512).AsInt32();
                        var codes4 = Avx512Vbmi.PermuteVar64x8x2(lo512, WideLaneTables.DecodeCodes, hi512).AsInt32();
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
        for (; i < count; i++)
        {
            Unsafe.Add(ref dst, i) = buffer.ReadInt32();
        }
        value = result;
    }
}
