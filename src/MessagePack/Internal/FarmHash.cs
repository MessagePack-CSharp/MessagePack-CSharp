#if NETSTANDARD1_4

using System.Runtime.CompilerServices;

namespace MessagePack.Internal
{
    // Port of the google/farmhash https://github.com/google/farmhash

    internal static class FarmHash
    {
        // entry point 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe uint Hash32(byte[] bytes, int offset, int count)
        {
            if (count <= 4)
            {
                return Hash32Len0to4(bytes, offset, (uint)count);
            }

            fixed (byte* p = bytes)
            {
                return Hash32(p + offset, (uint)count);
            }
        }

        // port of farmhash.cc, 32bit only

        // Magic numbers for 32-bit hashing.  Copied from Murmur3.
        const uint c1 = 0xcc9e2d51;
        const uint c2 = 0x1b873593;
        
        // Fetch32
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe uint Fetch(byte* p)
        {
            return *(uint*)p;
        }

        // BasicRotate32
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Rotate(uint val, int shift)
        {
            unchecked
            {
                return shift == 0 ? val : ((val >> shift) | (val << (32 - shift)));
            }
        }

        // A 32-bit to 32-bit integer hash copied from Murmur3.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint fmix(uint h)
        {
            unchecked
            {
                h ^= h >> 16;
                h *= 0x85ebca6b;
                h ^= h >> 13;
                h *= 0xc2b2ae35;
                h ^= h >> 16;
                return h;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Mur(uint a, uint h)
        {
            unchecked
            {
                // Helper from Murmur3 for combining two 32-bit values.
                a *= c1;
                a = Rotate(a, 17);
                a *= c2;
                h ^= a;
                h = Rotate(h, 19);
                return h * 5 + 0xe6546b64;
            }
        }

        // 0-4
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe uint Hash32Len0to4(byte[] s, int offset, uint len)
        {
            unchecked
            {
                uint b = 0;
                uint c = 9;
                var max = offset + len;
                for (int i = offset; i < max; i++)
                {
                    b = b * c1 + s[i];
                    c ^= b;
                }
                return fmix(Mur(b, Mur(len, c)));
            }
        }

        // 5-12
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe uint Hash32Len5to12(byte* s, uint len)
        {
            unchecked
            {
                uint a = len, b = len * 5, c = 9, d = b;
                a += Fetch(s);
                b += Fetch(s + len - 4);
                c += Fetch(s + ((len >> 1) & 4));
                return fmix(Mur(c, Mur(b, Mur(a, d))));
            }
        }

        // 13-24
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe uint Hash32Len13to24(byte* s, uint len)
        {
            unchecked
            {
                uint a = Fetch(s - 4 + (len >> 1));
                uint b = Fetch(s + 4);
                uint c = Fetch(s + len - 8);
                uint d = Fetch(s + (len >> 1));
                uint e = Fetch(s);
                uint f = Fetch(s + len - 4);
                uint h = d * c1 + len;
                a = Rotate(a, 12) + f;
                h = Mur(c, h) + a;
                a = Rotate(a, 3) + c;
                h = Mur(e, h) + a;
                a = Rotate(a + f, 12) + d;
                h = Mur(b, h) + a;
                return fmix(h);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe uint Hash32(byte* s, uint len)
        {
            if (len <= 24)
            {
                return len <= 12 ? Hash32Len5to12(s, len) : Hash32Len13to24(s, len);
            }

            unchecked
            {
                // len > 24
                uint h = len, g = c1 * len, f = g;
                uint a0 = Rotate(Fetch(s + len - 4) * c1, 17) * c2;
                uint a1 = Rotate(Fetch(s + len - 8) * c1, 17) * c2;
                uint a2 = Rotate(Fetch(s + len - 16) * c1, 17) * c2;
                uint a3 = Rotate(Fetch(s + len - 12) * c1, 17) * c2;
                uint a4 = Rotate(Fetch(s + len - 20) * c1, 17) * c2;
                h ^= a0;
                h = Rotate(h, 19);
                h = h * 5 + 0xe6546b64;
                h ^= a2;
                h = Rotate(h, 19);
                h = h * 5 + 0xe6546b64;
                g ^= a1;
                g = Rotate(g, 19);
                g = g * 5 + 0xe6546b64;
                g ^= a3;
                g = Rotate(g, 19);
                g = g * 5 + 0xe6546b64;
                f += a4;
                f = Rotate(f, 19) + 113;
                uint iters = (len - 1) / 20;
                do
                {
                    uint a = Fetch(s);
                    uint b = Fetch(s + 4);
                    uint c = Fetch(s + 8);
                    uint d = Fetch(s + 12);
                    uint e = Fetch(s + 16);
                    h += a;
                    g += b;
                    f += c;
                    h = Mur(d, h) + e;
                    g = Mur(c, g) + a;
                    f = Mur(b + e * c1, f) + d;
                    f += g;
                    g += f;
                    s += 20;
                } while (--iters != 0);
                g = Rotate(g, 11) * c1;
                g = Rotate(g, 17) * c1;
                f = Rotate(f, 11) * c1;
                f = Rotate(f, 17) * c1;
                h = Rotate(h + g, 19);
                h = h * 5 + 0xe6546b64;
                h = Rotate(h, 17) * c1;
                h = Rotate(h + f, 19);
                h = h * 5 + 0xe6546b64;
                h = Rotate(h, 17) * c1;
                return h;
            }
        }
    }
}

#endif