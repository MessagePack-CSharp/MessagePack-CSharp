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

    public static partial class UnsafeMemory32
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw1(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            MemoryMarshal.GetReference(dst) = MemoryMarshal.GetReference(src);

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw2(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref pDst, Unsafe.ReadUnaligned<short>(ref pSrc));

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw3(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            pDst = pSrc;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 1), Unsafe.ReadUnaligned<short>(ref Unsafe.Add(ref pSrc, 1)));

            writer.Advance(src.Length);
        }
    }

    public static partial class UnsafeMemory64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw1(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            MemoryMarshal.GetReference(dst) = MemoryMarshal.GetReference(src);

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw2(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref pDst, Unsafe.ReadUnaligned<short>(ref pSrc));

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw3(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            pDst = pSrc;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 1), Unsafe.ReadUnaligned<short>(ref Unsafe.Add(ref pSrc, 1)));

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw4(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref pDst, Unsafe.ReadUnaligned<int>(ref pSrc));

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw5(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref pDst, Unsafe.ReadUnaligned<int>(ref pSrc));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 1), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 1)));

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw6(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref pDst, Unsafe.ReadUnaligned<int>(ref pSrc));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 2), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 2)));

            writer.Advance(src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw7(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(src.Length);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref pDst, Unsafe.ReadUnaligned<int>(ref pSrc));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 3), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 3)));

            writer.Advance(src.Length);
        }
    }
}
