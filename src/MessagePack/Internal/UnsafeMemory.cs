// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#if !UNITY_2018_3_OR_NEWER

#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using MessagePack.Formatters;

namespace MessagePack.Internal
{
    public static partial class UnsafeMemory32
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw4(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(4);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
            }

            writer.Advance(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw5(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(5);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 1) = *(int*)(pSrc + 1);
            }

            writer.Advance(5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw6(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(6);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 2) = *(int*)(pSrc + 2);
            }

            writer.Advance(6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw7(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(7);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 3) = *(int*)(pSrc + 3);
            }

            writer.Advance(7);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw8(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(8);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
            }

            writer.Advance(8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw9(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(9);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 5) = *(int*)(pSrc + 5);
            }

            writer.Advance(9);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw10(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(10);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 6) = *(int*)(pSrc + 6);
            }

            writer.Advance(10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw11(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(11);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 7) = *(int*)(pSrc + 7);
            }

            writer.Advance(11);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw12(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(12);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
            }

            writer.Advance(12);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw13(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(13);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 9) = *(int*)(pSrc + 9);
            }

            writer.Advance(13);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw14(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(14);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 10) = *(int*)(pSrc + 10);
            }

            writer.Advance(14);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw15(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(15);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 11) = *(int*)(pSrc + 11);
            }

            writer.Advance(15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw16(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(16);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
            }

            writer.Advance(16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw17(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(17);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 13) = *(int*)(pSrc + 13);
            }

            writer.Advance(17);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw18(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(18);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 14) = *(int*)(pSrc + 14);
            }

            writer.Advance(18);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw19(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(19);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 15) = *(int*)(pSrc + 15);
            }

            writer.Advance(19);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw20(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(20);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
            }

            writer.Advance(20);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw21(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(21);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 17) = *(int*)(pSrc + 17);
            }

            writer.Advance(21);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw22(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(22);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 18) = *(int*)(pSrc + 18);
            }

            writer.Advance(22);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw23(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(23);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 19) = *(int*)(pSrc + 19);
            }

            writer.Advance(23);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw24(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(24);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
            }

            writer.Advance(24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw25(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(25);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 21) = *(int*)(pSrc + 21);
            }

            writer.Advance(25);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw26(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(26);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 22) = *(int*)(pSrc + 22);
            }

            writer.Advance(26);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw27(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(27);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 23) = *(int*)(pSrc + 23);
            }

            writer.Advance(27);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw28(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(28);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 24) = *(int*)(pSrc + 24);
            }

            writer.Advance(28);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw29(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(29);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 24) = *(int*)(pSrc + 24);
                *(int*)(pDst + 25) = *(int*)(pSrc + 25);
            }

            writer.Advance(29);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw30(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(30);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 24) = *(int*)(pSrc + 24);
                *(int*)(pDst + 26) = *(int*)(pSrc + 26);
            }

            writer.Advance(30);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw31(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(31);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 24) = *(int*)(pSrc + 24);
                *(int*)(pDst + 27) = *(int*)(pSrc + 27);
            }

            writer.Advance(31);
        }
    }

    public static partial class UnsafeMemory64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw8(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(8);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
            }

            writer.Advance(8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw9(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(9);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 1) = *(long*)(pSrc + 1);
            }

            writer.Advance(9);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw10(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(10);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 2) = *(long*)(pSrc + 2);
            }

            writer.Advance(10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw11(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(11);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 3) = *(long*)(pSrc + 3);
            }

            writer.Advance(11);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw12(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(12);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 4) = *(long*)(pSrc + 4);
            }

            writer.Advance(12);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw13(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(13);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 5) = *(long*)(pSrc + 5);
            }

            writer.Advance(13);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw14(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(14);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 6) = *(long*)(pSrc + 6);
            }

            writer.Advance(14);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw15(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(15);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 7) = *(long*)(pSrc + 7);
            }

            writer.Advance(15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw16(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(16);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
            }

            writer.Advance(16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw17(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(17);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 9) = *(long*)(pSrc + 9);
            }

            writer.Advance(17);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw18(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(18);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 10) = *(long*)(pSrc + 10);
            }

            writer.Advance(18);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw19(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(19);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 11) = *(long*)(pSrc + 11);
            }

            writer.Advance(19);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw20(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(20);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 12) = *(long*)(pSrc + 12);
            }

            writer.Advance(20);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw21(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(21);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 13) = *(long*)(pSrc + 13);
            }

            writer.Advance(21);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw22(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(22);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 14) = *(long*)(pSrc + 14);
            }

            writer.Advance(22);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw23(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(23);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 15) = *(long*)(pSrc + 15);
            }

            writer.Advance(23);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw24(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(24);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
            }

            writer.Advance(24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw25(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(25);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 17) = *(long*)(pSrc + 17);
            }

            writer.Advance(25);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw26(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(26);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 18) = *(long*)(pSrc + 18);
            }

            writer.Advance(26);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw27(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(27);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 19) = *(long*)(pSrc + 19);
            }

            writer.Advance(27);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw28(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(28);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 20) = *(long*)(pSrc + 20);
            }

            writer.Advance(28);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw29(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(29);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 21) = *(long*)(pSrc + 21);
            }

            writer.Advance(29);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw30(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(30);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 22) = *(long*)(pSrc + 22);
            }

            writer.Advance(30);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw31(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(31);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 23) = *(long*)(pSrc + 23);
            }

            writer.Advance(31);
        }
    }
}

#endif
