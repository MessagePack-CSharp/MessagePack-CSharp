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

    /// <summary>
    /// Highly tuned method for writing raw bytes to a <see cref="MessagePackWriter"/>.
    /// </summary>
    /// <remarks>
    /// The methods on this class are <em>not</em> safe, in that they use pointer arithmetic
    /// and assume that the caller has provided a <see cref="ReadOnlySpan{Byte}"/> with a length
    /// of at least the number of bytes being written. The caller must ensure that this is the case.
    /// </remarks>
    public static partial class UnsafeMemory32
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw1(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(1);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(byte*)pDst = *(byte*)pSrc;
            }

            writer.Advance(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw2(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(2);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(short*)pDst = *(short*)pSrc;
            }

            writer.Advance(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw3(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(3);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(byte*)pDst = *(byte*)pSrc;
                *(short*)(pDst + 1) = *(short*)(pSrc + 1);
            }

            writer.Advance(3);
        }
    }

    /// <summary>
    /// Highly tuned method for writing raw bytes to a <see cref="MessagePackWriter"/>.
    /// </summary>
    /// <remarks>
    /// The methods on this class are <em>not</em> safe, in that they use pointer arithmetic
    /// and assume that the caller has provided a <see cref="ReadOnlySpan{Byte}"/> with a length
    /// of at least the number of bytes being written. The caller must ensure that this is the case.
    /// </remarks>
    public static partial class UnsafeMemory64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw1(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(1);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(byte*)pDst = *(byte*)pSrc;
            }

            writer.Advance(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw2(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(2);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(short*)pDst = *(short*)pSrc;
            }

            writer.Advance(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRaw3(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(3);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                *(byte*)pDst = *(byte*)pSrc;
                *(short*)(pDst + 1) = *(short*)(pSrc + 1);
            }

            writer.Advance(3);
        }

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
    }
}

#endif
