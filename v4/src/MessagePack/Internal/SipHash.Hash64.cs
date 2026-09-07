// The production entry point: SipHash-1-3 with the two I/O-boundary transforms over the 1:1 port.
//
// Here is the reasoning for the reduced round count:
//   - SipHash is latency-bound on the SIPROUND dependency chain, so halving the rounds roughly halves the cost.
//     With hash-flooding-resistant comparers on by default, this is paid per key insert during deserialization.
//   - Precedent:
//     Rust's std HashMap has defaulted to SipHash-1-3 since 2016
//     (https://github.com/rust-lang/rust/pull/33940, discussion in https://github.com/rust-lang/rust/issues/29754).
//     rustc's StableHasher followed in 2023(https://github.com/rust-lang/rust/pull/107925);
//     Ruby switched its string hash to SipHash13 in Ruby 2.5 (https://github.com/ruby/ruby/pull/1501 , https://bugs.ruby-lang.org/issues/13017).
//     CPython switched the default str/bytes hash to SipHash13 in Python 3.11
//     (https://github.com/python/cpython/issues/73596 , https://github.com/python/cpython/pull/28752).

namespace MessagePack;

internal static partial class SipHash
{
    internal static ulong Hash64(ReadOnlySpan<byte> @in, ulong k0, ulong k1)
    {
        var v0 = 0x736f6d6570736575UL ^ k0;
        var v1 = 0x646f72616e646f6dUL ^ k1;
        var v2 = 0x6c7967656e657261UL ^ k0;
        var v3 = 0x7465646279746573UL ^ k1;

        var inlen = @in.Length;
        var end = inlen & ~7;
        var b = (ulong)inlen << 56;

        ref var r0 = ref MemoryMarshal.GetReference(@in);
        for (var i = 0; i < end; i += 8)
        {
            var m = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref r0, i));
            if (!BitConverter.IsLittleEndian)
            {
                m = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(m);
            }
            v3 ^= m;
            SIPROUND(ref v0, ref v1, ref v2, ref v3);
            v0 ^= m;
        }

        switch (inlen & 7)
        {
            case 7: b |= (ulong)Unsafe.Add(ref r0, end + 6) << 48; goto case 6;
            case 6: b |= (ulong)Unsafe.Add(ref r0, end + 5) << 40; goto case 5;
            case 5: b |= (ulong)Unsafe.Add(ref r0, end + 4) << 32; goto case 4;
            case 4: b |= (ulong)Unsafe.Add(ref r0, end + 3) << 24; goto case 3;
            case 3: b |= (ulong)Unsafe.Add(ref r0, end + 2) << 16; goto case 2;
            case 2: b |= (ulong)Unsafe.Add(ref r0, end + 1) << 8; goto case 1;
            case 1: b |= Unsafe.Add(ref r0, end); break;
            case 0: break;
        }

        v3 ^= b;
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        v0 ^= b;

        v2 ^= 0xff;
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        return v0 ^ v1 ^ v2 ^ v3;
    }

    // The little-endian value either fits the length-tagged final block outright (length < 8, where unused upper bytes
    // are masked off so garbage or sign extension in them cannot change the hash) or is exactly one compression block
    // with an empty tail (length == 8), so there is no span, no block loop and no tail switch. About 30% faster than
    // the span path per Dictionary insert. Callers pass a JIT-constant length (Unsafe.SizeOf), which folds the branch
    // and mask away into constants.

    /// <summary><see cref="Hash64"/> specialized for fixed-size keys of at most 8 bytes, bit-identical to hashing their little-endian bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Hash64Fixed(ulong littleEndianBits, int length, ulong k0, ulong k1)
    {
        var v0 = 0x736f6d6570736575UL ^ k0;
        var v1 = 0x646f72616e646f6dUL ^ k1;
        var v2 = 0x6c7967656e657261UL ^ k0;
        var v3 = 0x7465646279746573UL ^ k1;

        ulong b;
        if (length == 8)
        {
            v3 ^= littleEndianBits;
            SIPROUND(ref v0, ref v1, ref v2, ref v3);
            v0 ^= littleEndianBits;

            b = 8UL << 56;
        }
        else
        {
            // length is 0..7, so the shift count is 0..56.
            // This also removes sign extension and garbage in unused bytes.
            var payloadMask = (1UL << (length * 8)) - 1UL;
            var payload = littleEndianBits & payloadMask;

            b = ((ulong)length << 56) | payload;
        }

        v3 ^= b;
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        v0 ^= b;

        v2 ^= 0xff;
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        return v0 ^ v1 ^ v2 ^ v3;
    }

    // Two compression blocks plus the empty length-tagged tail, all in registers, with no span, no block loop and no tail switch.

    /// <summary><see cref="Hash64"/> specialized for 16-byte keys such as Guid, bit-identical to hashing their little-endian bytes (bits0 = bytes 0..7, bits1 = bytes 8..15).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Hash64Fixed16(ulong bits0, ulong bits1, ulong k0, ulong k1)
    {
        var v0 = 0x736f6d6570736575UL ^ k0;
        var v1 = 0x646f72616e646f6dUL ^ k1;
        var v2 = 0x6c7967656e657261UL ^ k0;
        var v3 = 0x7465646279746573UL ^ k1;

        v3 ^= bits0;
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        v0 ^= bits0;

        v3 ^= bits1;
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        v0 ^= bits1;

        var b = 16UL << 56;
        v3 ^= b;
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        v0 ^= b;

        v2 ^= 0xff;
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        SIPROUND(ref v0, ref v1, ref v2, ref v3);
        return v0 ^ v1 ^ v2 ^ v3;
    }
}
