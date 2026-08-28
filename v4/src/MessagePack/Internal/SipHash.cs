/*
   SipHash reference C implementation — 1:1 C# port of siphash.c
   https://github.com/veorq/SipHash

   Original copyright:

   Copyright (c) 2012-2022 Jean-Philippe Aumasson
   <jeanphilippe.aumasson@gmail.com>
   Copyright (c) 2012-2014 Daniel J. Bernstein <djb@cr.yp.to>

   To the extent possible under law, the author(s) have dedicated all copyright
   and related and neighboring rights to this software to the public domain
   worldwide. This software is distributed without any warranty.

   You should have received a copy of the CC0 Public Domain Dedication along
   with
   this software. If not, see
   <http://creativecommons.org/publicdomain/zero/1.0/>.

   Port notes: the C macros (ROTL / U8TO64_LE / U64TO8_LE / SIPROUND) become
   AggressiveInlining static methods, the FALLTHRU switch becomes goto case,
   pointers become spans + offsets. The DEBUG_SIPHASH TRACE machinery is
   omitted. Everything else, including variable names and statement order,
   follows the original line by line.
 */

using System.Diagnostics;

namespace MessagePack;

internal static partial class SipHash
{
    /* default: SipHash-2-4 */
    const int cROUNDS = 2;
    const int dROUNDS = 4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ulong ROTL(ulong x, int b) => (x << b) | (x >> (64 - b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void U32TO8_LE(Span<byte> p, uint v)
    {
        p[0] = (byte)v;
        p[1] = (byte)(v >> 8);
        p[2] = (byte)(v >> 16);
        p[3] = (byte)(v >> 24);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void U64TO8_LE(Span<byte> p, ulong v)
    {
        U32TO8_LE(p, (uint)v);
        U32TO8_LE(p.Slice(4), (uint)(v >> 32));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ulong U8TO64_LE(ReadOnlySpan<byte> p)
    {
        return ((ulong)p[0]) | ((ulong)p[1] << 8) |
               ((ulong)p[2] << 16) | ((ulong)p[3] << 24) |
               ((ulong)p[4] << 32) | ((ulong)p[5] << 40) |
               ((ulong)p[6] << 48) | ((ulong)p[7] << 56);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void SIPROUND(ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3)
    {
        unchecked
        {
            v0 += v1;
            v1 = ROTL(v1, 13);
            v1 ^= v0;
            v0 = ROTL(v0, 32);
            v2 += v3;
            v3 = ROTL(v3, 16);
            v3 ^= v2;
            v0 += v3;
            v3 = ROTL(v3, 21);
            v3 ^= v0;
            v2 += v1;
            v1 = ROTL(v1, 17);
            v1 ^= v2;
            v2 = ROTL(v2, 32);
        }
    }

    /*
        Computes a SipHash value
        in: input data (read-only)
        k: the key data (read-only), must be 16 bytes
        out: output data (write-only), length must be 8 or 16
    */
    internal static int Siphash(ReadOnlySpan<byte> @in, ReadOnlySpan<byte> k, Span<byte> @out)
    {
        int inlen = @in.Length;
        int outlen = @out.Length;

        Debug.Assert((outlen == 8) || (outlen == 16));
        ulong v0 = 0x736f6d6570736575UL;
        ulong v1 = 0x646f72616e646f6dUL;
        ulong v2 = 0x6c7967656e657261UL;
        ulong v3 = 0x7465646279746573UL;
        ulong k0 = U8TO64_LE(k);
        ulong k1 = U8TO64_LE(k.Slice(8));
        ulong m;
        int i;
        int end = inlen - (inlen % sizeof(ulong));
        int left = inlen & 7;
        ulong b = ((ulong)inlen) << 56;
        v3 ^= k1;
        v2 ^= k0;
        v1 ^= k1;
        v0 ^= k0;

        if (outlen == 16)
            v1 ^= 0xee;

        for (int ni = 0; ni != end; ni += 8)
        {
            m = U8TO64_LE(@in.Slice(ni));
            v3 ^= m;

            for (i = 0; i < cROUNDS; ++i)
                SIPROUND(ref v0, ref v1, ref v2, ref v3);

            v0 ^= m;
        }

        switch (left)
        {
            case 7:
                b |= ((ulong)@in[end + 6]) << 48;
                goto case 6;
            case 6:
                b |= ((ulong)@in[end + 5]) << 40;
                goto case 5;
            case 5:
                b |= ((ulong)@in[end + 4]) << 32;
                goto case 4;
            case 4:
                b |= ((ulong)@in[end + 3]) << 24;
                goto case 3;
            case 3:
                b |= ((ulong)@in[end + 2]) << 16;
                goto case 2;
            case 2:
                b |= ((ulong)@in[end + 1]) << 8;
                goto case 1;
            case 1:
                b |= ((ulong)@in[end + 0]);
                break;
            case 0:
                break;
        }

        v3 ^= b;

        for (i = 0; i < cROUNDS; ++i)
            SIPROUND(ref v0, ref v1, ref v2, ref v3);

        v0 ^= b;

        if (outlen == 16)
            v2 ^= 0xee;
        else
            v2 ^= 0xff;

        for (i = 0; i < dROUNDS; ++i)
            SIPROUND(ref v0, ref v1, ref v2, ref v3);

        b = v0 ^ v1 ^ v2 ^ v3;
        U64TO8_LE(@out, b);

        if (outlen == 8)
            return 0;

        v1 ^= 0xdd;

        for (i = 0; i < dROUNDS; ++i)
            SIPROUND(ref v0, ref v1, ref v2, ref v3);

        b = v0 ^ v1 ^ v2 ^ v3;
        U64TO8_LE(@out.Slice(8), b);

        return 0;
    }
}
