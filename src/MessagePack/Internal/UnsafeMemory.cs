// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

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
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 4).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw5(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 5).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 1) = *(int*)(pSrc + 1);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw6(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 6).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 2) = *(int*)(pSrc + 2);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw7(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 7).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 3) = *(int*)(pSrc + 3);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw8(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 8).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw9(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 9).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 5) = *(int*)(pSrc + 5);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw10(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 10).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 6) = *(int*)(pSrc + 6);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw11(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 11).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 7) = *(int*)(pSrc + 7);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw12(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 12).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw13(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 13).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 9) = *(int*)(pSrc + 9);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw14(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 14).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 10) = *(int*)(pSrc + 10);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw15(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 15).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 11) = *(int*)(pSrc + 11);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw16(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 16).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw17(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 17).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 13) = *(int*)(pSrc + 13);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw18(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 18).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 14) = *(int*)(pSrc + 14);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw19(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 19).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 15) = *(int*)(pSrc + 15);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw20(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 20).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw21(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 21).CopyTo(dst);
#else
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
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw22(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 22).CopyTo(dst);
#else
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
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw23(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 23).CopyTo(dst);
#else
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
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw24(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 24).CopyTo(dst);
#else
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
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw25(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 25).CopyTo(dst);
#else
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
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw26(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 26).CopyTo(dst);
#else
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
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw27(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 27).CopyTo(dst);
#else
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
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw28(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 28).CopyTo(dst);
#else
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
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw29(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 29).CopyTo(dst);
#else
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
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw30(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 30).CopyTo(dst);
#else
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
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw31(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 31).CopyTo(dst);
#else
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
#endif
            writer.Advance(src.Length);
        }
    }

    public static partial class UnsafeMemory64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw8(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 8).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw9(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 9).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 1) = *(long*)(pSrc + 1);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw10(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 10).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 2) = *(long*)(pSrc + 2);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw11(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 11).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 3) = *(long*)(pSrc + 3);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw12(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 12).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 4) = *(long*)(pSrc + 4);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw13(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 13).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 5) = *(long*)(pSrc + 5);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw14(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 14).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 6) = *(long*)(pSrc + 6);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw15(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 15).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 7) = *(long*)(pSrc + 7);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw16(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 16).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw17(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 17).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 9) = *(long*)(pSrc + 9);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw18(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 18).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 10) = *(long*)(pSrc + 10);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw19(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 19).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 11) = *(long*)(pSrc + 11);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw20(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 20).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 12) = *(long*)(pSrc + 12);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw21(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 21).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 13) = *(long*)(pSrc + 13);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw22(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 22).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 14) = *(long*)(pSrc + 14);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw23(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 23).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 15) = *(long*)(pSrc + 15);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw24(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 24).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw25(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 25).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 17) = *(long*)(pSrc + 17);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw26(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 26).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 18) = *(long*)(pSrc + 18);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw27(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 27).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 19) = *(long*)(pSrc + 19);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw28(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 28).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 20) = *(long*)(pSrc + 20);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw29(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 29).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 21) = *(long*)(pSrc + 21);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw30(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 30).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 22) = *(long*)(pSrc + 22);
            }
#endif
            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw31(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

#if NET7_0_OR_GREATER
            src.Slice(0, 31).CopyTo(dst);
#else
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 23) = *(long*)(pSrc + 23);
            }
#endif
            writer.Advance(src.Length);
        }
    }
}
