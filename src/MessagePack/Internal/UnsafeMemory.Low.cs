// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        public static void WriteRaw1(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(1);

            MemoryMarshal.GetReference(dst) = MemoryMarshal.GetReference(src);

            writer.Advance(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw2(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(2);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref pDst, Unsafe.ReadUnaligned<short>(ref pSrc));

            writer.Advance(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw3(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(3);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            pDst = pSrc;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 1), Unsafe.ReadUnaligned<short>(ref Unsafe.Add(ref pSrc, 1)));

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
        public static void WriteRaw1(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(1);

            MemoryMarshal.GetReference(dst) = MemoryMarshal.GetReference(src);

            writer.Advance(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw2(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(2);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref pDst, Unsafe.ReadUnaligned<short>(ref pSrc));

            writer.Advance(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw3(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(3);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            pDst = pSrc;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 1), Unsafe.ReadUnaligned<short>(ref Unsafe.Add(ref pSrc, 1)));

            writer.Advance(3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw4(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(4);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref pDst, Unsafe.ReadUnaligned<int>(ref pSrc));

            writer.Advance(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw5(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(5);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref pDst, Unsafe.ReadUnaligned<int>(ref pSrc));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 1), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 1)));

            writer.Advance(5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw6(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(6);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref pDst, Unsafe.ReadUnaligned<int>(ref pSrc));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 2), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 2)));

            writer.Advance(6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw7(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(7);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref pDst, Unsafe.ReadUnaligned<int>(ref pSrc));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 3), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 3)));

            writer.Advance(7);
        }
    }
}
