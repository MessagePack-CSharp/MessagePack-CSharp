// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable SA1300 // Element should begin with uppercase letter
#pragma warning disable SA1303 // Const field names should begin with uppercase letter
#pragma warning disable SA1307 // (public) field names should begin with uppercase letter

namespace MessagePack.Internal
{
    internal static class FarmHash
    {
        #region Hash32

        // entry point of 32bit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe uint Hash32(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length <= 4)
            {
                return Hash32Len0to4(bytes);
            }

            fixed (byte* p = bytes)
            {
                return Hash32(p, (uint)bytes.Length);
            }
        }

        // port of farmhash.cc, 32bit only

        // Magic numbers for 32-bit hashing.  Copied from Murmur3.
        private const uint c1 = 0xcc9e2d51;
        private const uint c2 = 0x1b873593;

        private static unsafe uint Fetch32(byte* p)
        {
            return *(uint*)p;
        }

        private static uint Rotate32(uint val, int shift)
        {
            return shift == 0 ? val : ((val >> shift) | (val << (32 - shift)));
        }

        // A 32-bit to 32-bit integer hash copied from Murmur3.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint fmix(uint h)
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
        private static uint Mur(uint a, uint h)
        {
            unchecked
            {
                // Helper from Murmur3 for combining two 32-bit values.
                a *= c1;
                a = Rotate32(a, 17);
                a *= c2;
                h ^= a;
                h = Rotate32(h, 19);
                return (h * 5) + 0xe6546b64;
            }
        }

        // 0-4
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint Hash32Len0to4(ReadOnlySpan<byte> s)
        {
            unchecked
            {
                uint b = 0;
                uint c = 9;
                for (int i = 0; i < s.Length; i++)
                {
                    b = (b * c1) + s[i];
                    c ^= b;
                }

                return fmix(Mur(b, Mur((uint)s.Length, c)));
            }
        }

        // 5-12
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint Hash32Len5to12(byte* s, uint len)
        {
            unchecked
            {
                uint a = len, b = len * 5, c = 9, d = b;
                a += Fetch32(s);
                b += Fetch32(s + len - 4);
                c += Fetch32(s + ((len >> 1) & 4));
                return fmix(Mur(c, Mur(b, Mur(a, d))));
            }
        }

        // 13-24
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint Hash32Len13to24(byte* s, uint len)
        {
            unchecked
            {
                uint a = Fetch32(s - 4 + (len >> 1));
                uint b = Fetch32(s + 4);
                uint c = Fetch32(s + len - 8);
                uint d = Fetch32(s + (len >> 1));
                uint e = Fetch32(s);
                uint f = Fetch32(s + len - 4);
                uint h = (d * c1) + len;
                a = Rotate32(a, 12) + f;
                h = Mur(c, h) + a;
                a = Rotate32(a, 3) + c;
                h = Mur(e, h) + a;
                a = Rotate32(a + f, 12) + d;
                h = Mur(b, h) + a;
                return fmix(h);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint Hash32(byte* s, uint len)
        {
            if (len <= 24)
            {
                return len <= 12 ? Hash32Len5to12(s, len) : Hash32Len13to24(s, len);
            }

            unchecked
            {
                // len > 24
                uint h = len, g = c1 * len, f = g;
                uint a0 = Rotate32(Fetch32(s + len - 4) * c1, 17) * c2;
                uint a1 = Rotate32(Fetch32(s + len - 8) * c1, 17) * c2;
                uint a2 = Rotate32(Fetch32(s + len - 16) * c1, 17) * c2;
                uint a3 = Rotate32(Fetch32(s + len - 12) * c1, 17) * c2;
                uint a4 = Rotate32(Fetch32(s + len - 20) * c1, 17) * c2;
                h ^= a0;
                h = Rotate32(h, 19);
                h = (h * 5) + 0xe6546b64;
                h ^= a2;
                h = Rotate32(h, 19);
                h = (h * 5) + 0xe6546b64;
                g ^= a1;
                g = Rotate32(g, 19);
                g = (g * 5) + 0xe6546b64;
                g ^= a3;
                g = Rotate32(g, 19);
                g = (g * 5) + 0xe6546b64;
                f += a4;
                f = Rotate32(f, 19) + 113;
                uint iters = (len - 1) / 20;
                do
                {
                    uint a = Fetch32(s);
                    uint b = Fetch32(s + 4);
                    uint c = Fetch32(s + 8);
                    uint d = Fetch32(s + 12);
                    uint e = Fetch32(s + 16);
                    h += a;
                    g += b;
                    f += c;
                    h = Mur(d, h) + e;
                    g = Mur(c, g) + a;
                    f = Mur(b + (e * c1), f) + d;
                    f += g;
                    g += f;
                    s += 20;
                }
                while (--iters != 0);
                g = Rotate32(g, 11) * c1;
                g = Rotate32(g, 17) * c1;
                f = Rotate32(f, 11) * c1;
                f = Rotate32(f, 17) * c1;
                h = Rotate32(h + g, 19);
                h = (h * 5) + 0xe6546b64;
                h = Rotate32(h, 17) * c1;
                h = Rotate32(h + f, 19);
                h = (h * 5) + 0xe6546b64;
                h = Rotate32(h, 17) * c1;
                return h;
            }
        }

        #endregion

        #region hash64

        // entry point
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ulong Hash64(ReadOnlySpan<byte> bytes)
        {
            fixed (byte* p = bytes)
            {
                return Hash64(p, (uint)bytes.Length);
            }
        }

        /* port from farmhash.cc */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void swap(ref ulong x, ref ulong z)
        {
            var temp = z;
            z = x;
            x = temp;
        }

        // Some primes between 2^63 and 2^64 for various uses.
        private const ulong k0 = 0xc3a5c85c97cb3127UL;
        private const ulong k1 = 0xb492b66fbe98f273UL;
        private const ulong k2 = 0x9ae16a3b2f90404fUL;

        private static unsafe ulong Fetch64(byte* p)
        {
            return *(ulong*)p;
        }

        private static ulong Rotate64(ulong val, int shift)
        {
            return shift == 0 ? val : (val >> shift) | (val << (64 - shift));
        }

        // farmhashna.cc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ShiftMix(ulong val)
        {
            return val ^ (val >> 47);
        }

        // farmhashna.cc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong HashLen16(ulong u, ulong v, ulong mul)
        {
            unchecked
            {
                // Murmur-inspired hashing.
                ulong a = (u ^ v) * mul;
                a ^= a >> 47;
                ulong b = (v ^ a) * mul;
                b ^= b >> 47;
                b *= mul;
                return b;
            }
        }

        // farmhashxo.cc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong Hash64(byte* s, uint len)
        {
            if (len <= 16)
            {
                // farmhashna::
                return HashLen0to16(s, len);
            }

            if (len <= 32)
            {
                // farmhashna::
                return HashLen17to32(s, len);
            }

            if (len <= 64)
            {
                return HashLen33to64(s, len);
            }

            if (len <= 96)
            {
                return HashLen65to96(s, len);
            }

            if (len <= 256)
            {
                // farmhashna::
                return Hash64NA(s, len);
            }

            // farmhashuo::
            return Hash64UO(s, len);
        }

        // 0-16 farmhashna.cc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong HashLen0to16(byte* s, uint len)
        {
            unchecked
            {
                if (len >= 8)
                {
                    ulong mul = k2 + (len * 2);
                    ulong a = Fetch64(s) + k2;
                    ulong b = Fetch64(s + len - 8);
                    ulong c = (Rotate64(b, 37) * mul) + a;
                    ulong d = (Rotate64(a, 25) + b) * mul;
                    return HashLen16(c, d, mul);
                }

                if (len >= 4)
                {
                    ulong mul = k2 + (len * 2);
                    ulong a = Fetch32(s);
                    return HashLen16(len + (a << 3), Fetch32(s + len - 4), mul);
                }

                if (len > 0)
                {
                    ushort a = s[0];
                    ushort b = s[len >> 1];
                    ushort c = s[len - 1];
                    uint y = a + ((uint)b << 8);
                    uint z = len + ((uint)c << 2);
                    return ShiftMix(y * k2 ^ z * k0) * k2;
                }

                return k2;
            }
        }

        // 17-32 farmhashna.cc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong HashLen17to32(byte* s, uint len)
        {
            unchecked
            {
                ulong mul = k2 + (len * 2);
                ulong a = Fetch64(s) * k1;
                ulong b = Fetch64(s + 8);
                ulong c = Fetch64(s + len - 8) * mul;
                ulong d = Fetch64(s + len - 16) * k2;
                return HashLen16(
                    Rotate64(a + b, 43) + Rotate64(c, 30) + d,
                    a + Rotate64(b + k2, 18) + c,
                    mul);
            }
        }

        // farmhashxo.cc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong H32(byte* s, uint len, ulong mul, ulong seed0 = 0, ulong seed1 = 0)
        {
            unchecked
            {
                ulong a = Fetch64(s) * k1;
                ulong b = Fetch64(s + 8);
                ulong c = Fetch64(s + len - 8) * mul;
                ulong d = Fetch64(s + len - 16) * k2;
                ulong u = Rotate64(a + b, 43) + Rotate64(c, 30) + d + seed0;
                ulong v = a + Rotate64(b + k2, 18) + c + seed1;
                a = ShiftMix((u ^ v) * mul);
                b = ShiftMix((v ^ a) * mul);
                return b;
            }
        }

        // 33-64 farmhashxo.cc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong HashLen33to64(byte* s, uint len)
        {
            const ulong mul0 = k2 - 30;

            unchecked
            {
                ulong mul1 = k2 - 30 + (2 * len);
                ulong h0 = H32(s, 32, mul0);
                ulong h1 = H32(s + len - 32, 32, mul1);
                return ((h1 * mul1) + h0) * mul1;
            }
        }

        // 65-96 farmhashxo.cc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong HashLen65to96(byte* s, uint len)
        {
            const ulong mul0 = k2 - 114;

            unchecked
            {
                ulong mul1 = k2 - 114 + (2 * len);
                ulong h0 = H32(s, 32, mul0);
                ulong h1 = H32(s + 32, 32, mul1);
                ulong h2 = H32(s + len - 32, 32, mul1, h0, h1);
                return ((h2 * 9) + (h0 >> 17) + (h1 >> 21)) * mul1;
            }
        }

        // farmhashna.cc
        // Return a 16-byte hash for 48 bytes.  Quick and dirty.
        // Callers do best to use "random-looking" values for a and b.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WeakHashLen32WithSeeds(ulong w, ulong x, ulong y, ulong z, ulong a, ulong b, out ulong first, out ulong second)
        {
            unchecked
            {
                a += w;
                b = Rotate64(b + a + z, 21);
                ulong c = a;
                a += x;
                a += y;
                b += Rotate64(a, 44);
                first = a + z;
                second = b + c;
            }
        }

        // farmhashna.cc
        // Return a 16-byte hash for s[0] ... s[31], a, and b.  Quick and dirty.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WeakHashLen32WithSeeds(byte* s, ulong a, ulong b, out ulong first, out ulong second)
        {
            WeakHashLen32WithSeeds(
                Fetch64(s),
                Fetch64(s + 8),
                Fetch64(s + 16),
                Fetch64(s + 24),
                a,
                b,
                out first,
                out second);
        }

        // na(97-256) farmhashna.cc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong Hash64NA(byte* s, uint len)
        {
            const ulong seed = 81;

            unchecked
            {
                // For strings over 64 bytes we loop.  Internal state consists of
                // 56 bytes: v, w, x, y, and z.
                ulong x = seed;
                ulong y = (seed * k1) + 113;
                ulong z = ShiftMix((y * k2) + 113) * k2;
                ulong v_first = 0;
                ulong v_second = 0;
                ulong w_first = 0;
                ulong w_second = 0;
                x = (x * k2) + Fetch64(s);

                // Set end so that after the loop we have 1 to 64 bytes left to process.
                byte* end = s + ((len - 1) / 64 * 64);
                byte* last64 = end + ((len - 1) & 63) - 63;

                do
                {
                    x = Rotate64(x + y + v_first + Fetch64(s + 8), 37) * k1;
                    y = Rotate64(y + v_second + Fetch64(s + 48), 42) * k1;
                    x ^= w_second;
                    y += v_first + Fetch64(s + 40);
                    z = Rotate64(z + w_first, 33) * k1;
                    WeakHashLen32WithSeeds(s, v_second * k1, x + w_first, out v_first, out v_second);
                    WeakHashLen32WithSeeds(s + 32, z + w_second, y + Fetch64(s + 16), out w_first, out w_second);
                    swap(ref z, ref x);
                    s += 64;
                }
                while (s != end);
                ulong mul = k1 + ((z & 0xff) << 1);

                // Make s point to the last 64 bytes of input.
                s = last64;
                w_first += (len - 1) & 63;
                v_first += w_first;
                w_first += v_first;
                x = Rotate64(x + y + v_first + Fetch64(s + 8), 37) * mul;
                y = Rotate64(y + v_second + Fetch64(s + 48), 42) * mul;
                x ^= w_second * 9;
                y += (v_first * 9) + Fetch64(s + 40);
                z = Rotate64(z + w_first, 33) * mul;
                WeakHashLen32WithSeeds(s, v_second * mul, x + w_first, out v_first, out v_second);
                WeakHashLen32WithSeeds(s + 32, z + w_second, y + Fetch64(s + 16), out w_first, out w_second);
                swap(ref z, ref x);
                return HashLen16(HashLen16(v_first, w_first, mul) + (ShiftMix(y) * k0) + z, HashLen16(v_second, w_second, mul) + x, mul);
            }
        }

        // farmhashuo.cc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong H(ulong x, ulong y, ulong mul, int r)
        {
            unchecked
            {
                ulong a = (x ^ y) * mul;
                a ^= a >> 47;
                ulong b = (y ^ a) * mul;
                return Rotate64(b, r) * mul;
            }
        }

        // uo(257-) farmhashuo.cc, Hash64WithSeeds
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong Hash64UO(byte* s, uint len)
        {
            const ulong seed0 = 81;
            const ulong seed1 = 0;

            unchecked
            {
                // For strings over 64 bytes we loop.  Internal state consists of
                // 64 bytes: u, v, w, x, y, and z.
                ulong x = seed0;
                ulong y = (seed1 * k2) + 113;
                ulong z = ShiftMix(y * k2) * k2;
                ulong v_first = seed0;
                ulong v_second = seed1;
                ulong w_first = 0;
                ulong w_second = 0;
                ulong u = x - z;
                x *= k2;
                ulong mul = k2 + (u & 0x82);

                // Set end so that after the loop we have 1 to 64 bytes left to process.
                byte* end = s + ((len - 1) / 64 * 64);
                byte* last64 = end + ((len - 1) & 63) - 63;

                do
                {
                    ulong a0 = Fetch64(s);
                    ulong a1 = Fetch64(s + 8);
                    ulong a2 = Fetch64(s + 16);
                    ulong a3 = Fetch64(s + 24);
                    ulong a4 = Fetch64(s + 32);
                    ulong a5 = Fetch64(s + 40);
                    ulong a6 = Fetch64(s + 48);
                    ulong a7 = Fetch64(s + 56);
                    x += a0 + a1;
                    y += a2;
                    z += a3;
                    v_first += a4;
                    v_second += a5 + a1;
                    w_first += a6;
                    w_second += a7;

                    x = Rotate64(x, 26);
                    x *= 9;
                    y = Rotate64(y, 29);
                    z *= mul;
                    v_first = Rotate64(v_first, 33);
                    v_second = Rotate64(v_second, 30);
                    w_first ^= x;
                    w_first *= 9;
                    z = Rotate64(z, 32);
                    z += w_second;
                    w_second += z;
                    z *= 9;
                    swap(ref u, ref y);

                    z += a0 + a6;
                    v_first += a2;
                    v_second += a3;
                    w_first += a4;
                    w_second += a5 + a6;
                    x += a1;
                    y += a7;

                    y += v_first;
                    v_first += x - y;
                    v_second += w_first;
                    w_first += v_second;
                    w_second += x - y;
                    x += w_second;
                    w_second = Rotate64(w_second, 34);
                    swap(ref u, ref z);
                    s += 64;
                }
                while (s != end);

                // Make s point to the last 64 bytes of input.
                s = last64;
                u *= 9;
                v_second = Rotate64(v_second, 28);
                v_first = Rotate64(v_first, 20);
                w_first += (len - 1) & 63;
                u += y;
                y += u;
                x = Rotate64(y - x + v_first + Fetch64(s + 8), 37) * mul;
                y = Rotate64(y ^ v_second ^ Fetch64(s + 48), 42) * mul;
                x ^= w_second * 9;
                y += v_first + Fetch64(s + 40);
                z = Rotate64(z + w_first, 33) * mul;
                WeakHashLen32WithSeeds(s, v_second * mul, x + w_first, out v_first, out v_second);
                WeakHashLen32WithSeeds(s + 32, z + w_second, y + Fetch64(s + 16), out w_first, out w_second);
                return H(HashLen16(v_first + x, w_first ^ y, mul) + z - u, H(v_second + y, w_second + z, k2, 30) ^ x, k2, 31);
            }
        }

        #endregion
    }
}
