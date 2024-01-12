// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY_2018_3_OR_NEWER

#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Runtime.CompilerServices;

namespace MessagePack.Internal
{
    /* for string key property name write optimization. */

    public static class UnsafeMemory
    {
        public static readonly bool Is32Bit = IntPtr.Size == 4;
    }

    public static partial class UnsafeMemory32
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw1(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(byte*)pDst = *(byte*)pSrc;
            }

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw2(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(short*)pDst = *(short*)pSrc;
            }

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw3(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(byte*)pDst = *(byte*)pSrc;
                *(short*)(pDst + 1) = *(short*)(pSrc + 1);
            }

            writer.Advance(src.Length);
        }
    }

    public static partial class UnsafeMemory64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw1(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(byte*)pDst = *(byte*)pSrc;
            }

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw2(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(short*)pDst = *(short*)pSrc;
            }

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw3(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(byte*)pDst = *(byte*)pSrc;
                *(short*)(pDst + 1) = *(short*)(pSrc + 1);
            }

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw4(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
            }

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw5(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 1) = *(int*)(pSrc + 1);
            }

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw6(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 2) = *(int*)(pSrc + 2);
            }

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw7(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 3) = *(int*)(pSrc + 3);
            }

            writer.Advance(src.Length);
        }
    }
}

#endif
