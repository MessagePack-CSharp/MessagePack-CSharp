// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MessagePack.Formatters;

namespace MessagePack.Internal
{
    public static partial class UnsafeMemory32
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw4(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(4);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));

            writer.Advance(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw5(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(5);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 1), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 1)));

            writer.Advance(5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw6(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(6);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 2), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 2)));

            writer.Advance(6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw7(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(7);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 3), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 3)));

            writer.Advance(7);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw8(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(8);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));

            writer.Advance(8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw9(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(9);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 5), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 5)));

            writer.Advance(9);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw10(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(10);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 6), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 6)));

            writer.Advance(10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw11(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(11);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 7), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 7)));

            writer.Advance(11);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw12(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(12);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));

            writer.Advance(12);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw13(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(13);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 9), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 9)));

            writer.Advance(13);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw14(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(14);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 10), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 10)));

            writer.Advance(14);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw15(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(15);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 11), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 11)));

            writer.Advance(15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw16(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(16);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));

            writer.Advance(16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw17(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(17);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 13), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 13)));

            writer.Advance(17);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw18(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(18);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 14), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 14)));

            writer.Advance(18);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw19(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(19);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 15), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 15)));

            writer.Advance(19);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw20(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(20);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 16)));

            writer.Advance(20);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw21(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(21);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 17), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 17)));

            writer.Advance(21);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw22(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(22);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 18), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 18)));

            writer.Advance(22);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw23(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(23);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 19), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 19)));

            writer.Advance(23);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw24(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(24);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 20), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 20)));

            writer.Advance(24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw25(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(25);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 20), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 20)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 21), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 21)));

            writer.Advance(25);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw26(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(26);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 20), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 20)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 22), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 22)));

            writer.Advance(26);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw27(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(27);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 20), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 20)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 23), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 23)));

            writer.Advance(27);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw28(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(28);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 20), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 20)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 24), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 24)));

            writer.Advance(28);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw29(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(29);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 20), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 20)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 24), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 24)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 25), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 25)));

            writer.Advance(29);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw30(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(30);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 20), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 20)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 24), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 24)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 26), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 26)));

            writer.Advance(30);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw31(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(31);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 20), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 20)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 24), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 24)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 27), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref pSrc, 27)));

            writer.Advance(31);
        }
    }

    public static partial class UnsafeMemory64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw8(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(8);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));

            writer.Advance(8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw9(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(9);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 1), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 1)));

            writer.Advance(9);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw10(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(10);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 2), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 2)));

            writer.Advance(10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw11(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(11);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 3), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 3)));

            writer.Advance(11);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw12(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(12);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 4), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 4)));

            writer.Advance(12);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw13(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(13);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 5), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 5)));

            writer.Advance(13);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw14(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(14);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 6), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 6)));

            writer.Advance(14);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw15(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(15);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 7), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 7)));

            writer.Advance(15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw16(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(16);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));

            writer.Advance(16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw17(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(17);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 9), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 9)));

            writer.Advance(17);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw18(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(18);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 10), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 10)));

            writer.Advance(18);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw19(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(19);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 11), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 11)));

            writer.Advance(19);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw20(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(20);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 12), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 12)));

            writer.Advance(20);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw21(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(21);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 13), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 13)));

            writer.Advance(21);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw22(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(22);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 14), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 14)));

            writer.Advance(22);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw23(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(23);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 15), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 15)));

            writer.Advance(23);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw24(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(24);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 16)));

            writer.Advance(24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw25(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(25);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 17), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 17)));

            writer.Advance(25);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw26(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(26);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 18), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 18)));

            writer.Advance(26);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw27(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(27);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 19), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 19)));

            writer.Advance(27);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw28(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(28);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 20), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 20)));

            writer.Advance(28);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw29(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(29);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 21), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 21)));

            writer.Advance(29);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw30(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(30);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 22), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 22)));

            writer.Advance(30);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRaw31(ref MessagePackWriter writer, ReadOnlySpan<byte> src)
        {
            Span<byte> dst = writer.GetSpan(31);

            ref byte pSrc = ref MemoryMarshal.GetReference(src);
            ref byte pDst = ref MemoryMarshal.GetReference(dst);

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 0), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 0)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 16), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pDst, 23), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref pSrc, 23)));

            writer.Advance(31);
        }
    }
}
