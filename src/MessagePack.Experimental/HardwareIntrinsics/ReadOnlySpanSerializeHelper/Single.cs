// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MessagePack.Formatters;

internal static partial class RefSerializeHelper
{
    internal static void Serialize(ref MessagePackWriter writer, ref float input, int length)
    {
        writer.WriteArrayHeader(length);
        if (length == 0)
        {
            return;
        }

        if (!BitConverter.IsLittleEndian)
        {
            BigEndianSerialize(ref writer, ref input, length, writer.CancellationToken);
            return;
        }

        ref var inputIterator = ref Unsafe.As<float, uint>(ref input);
        const int maxOutputElementSize = sizeof(float) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;
        if (Vector512.IsHardwareAccelerated)
        {
            for (var alignedInputLength = (nuint)(length & (~15)); alignedInputLength > 0; alignedInputLength = (nuint)(length & (~15)))
            {
                if (alignedInputLength >= maxInputSize)
                {
                    alignedInputLength = maxInputSize & (~15);
                }

                var outputLength = (int)alignedInputLength * maxOutputElementSize;
                var destination = writer.GetSpan(outputLength);
                ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                for (nuint inputOffset = 0, outputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector512<uint>.Count, outputOffset += (nuint)Vector512<uint>.Count * maxOutputElementSize)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();

                    // Reorder Little Endian bytes to Big Endian.
                    var shuffled = Vector512.Shuffle(Vector512.LoadUnsafe(ref inputIterator, inputOffset).AsByte(), Vector512.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12, 19, 18, 17, 16, 23, 22, 21, 20, 27, 26, 25, 24, 31, 30, 29, 28, 35, 34, 33, 32, 39, 38, 37, 36, 43, 42, 41, 40, 47, 46, 45, 44, 51, 50, 49, 48, 55, 54, 53, 52, 59, 58, 57, 56, 63, 62, 61, 60)).AsUInt32();

                    // Write 16 Big-Endian floats.
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.GetElement(0));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 5), shuffled.GetElement(1));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 10), shuffled.GetElement(2));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 15), shuffled.GetElement(3));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 20), shuffled.GetElement(4));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 25), shuffled.GetElement(5));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 30), shuffled.GetElement(6));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 35), shuffled.GetElement(7));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 40), shuffled.GetElement(8));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 45), shuffled.GetElement(9));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 50), shuffled.GetElement(10));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 55), shuffled.GetElement(11));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 60), shuffled.GetElement(12));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 65), shuffled.GetElement(13));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 70), shuffled.GetElement(14));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 75), shuffled.GetElement(15));
                }

                writer.Advance(outputLength);
                length -= (int)alignedInputLength;
                inputIterator = ref Unsafe.Add(ref inputIterator, alignedInputLength);
            }
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            for (var alignedInputLength = (nuint)(length & (~7)); alignedInputLength > 0; alignedInputLength = (nuint)(length & (~7)))
            {
                if (alignedInputLength >= maxInputSize)
                {
                    alignedInputLength = maxInputSize & (~7);
                }

                var outputLength = (int)alignedInputLength * maxOutputElementSize;
                var destination = writer.GetSpan(outputLength);
                ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                for (nuint inputOffset = 0, outputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector256<uint>.Count, outputOffset += (nuint)Vector256<uint>.Count * maxOutputElementSize)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();

                    // Reorder Little Endian bytes to Big Endian.
                    var shuffled = Vector256.Shuffle(Vector256.LoadUnsafe(ref inputIterator, inputOffset).AsByte(), Vector256.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12, 19, 18, 17, 16, 23, 22, 21, 20, 27, 26, 25, 24, 31, 30, 29, 28)).AsUInt32();

                    // Write 8 Big-Endian floats.
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.GetElement(0));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 5), shuffled.GetElement(1));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 10), shuffled.GetElement(2));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 15), shuffled.GetElement(3));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 20), shuffled.GetElement(4));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 25), shuffled.GetElement(5));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 30), shuffled.GetElement(6));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 35), shuffled.GetElement(7));
                }

                writer.Advance(outputLength);
                length -= (int)alignedInputLength;
                inputIterator = ref Unsafe.Add(ref inputIterator, alignedInputLength);
            }
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            for (var alignedInputLength = (nuint)(length & (~3)); alignedInputLength > 0; alignedInputLength = (nuint)(length & (~3)))
            {
                if (alignedInputLength >= maxInputSize)
                {
                    alignedInputLength = maxInputSize & (~3);
                }

                var outputLength = (int)alignedInputLength * maxOutputElementSize;
                var destination = writer.GetSpan(outputLength);
                ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                for (nuint inputOffset = 0, outputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<uint>.Count, outputOffset += (nuint)Vector128<uint>.Count * maxOutputElementSize)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();

                    // Reorder Little Endian bytes to Big Endian.
                    var shuffled = Vector128.Shuffle(Vector128.LoadUnsafe(ref inputIterator, inputOffset).AsByte(), Vector128.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)).AsUInt32();

                    // Write 4 Big-Endian floats.
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.GetElement(0));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 5), shuffled.GetElement(1));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 10), shuffled.GetElement(2));
                    WriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 15), shuffled.GetElement(3));
                }

                writer.Advance(outputLength);
                length -= (int)alignedInputLength;
                inputIterator = ref Unsafe.Add(ref inputIterator, alignedInputLength);
            }
        }

        while (length > 0)
        {
            var inputLength = length;
            if (inputLength > maxInputSize)
            {
                inputLength = maxInputSize;
            }

            var outputLength = inputLength * maxOutputElementSize;
            var destination = writer.GetSpan(outputLength);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            for (nuint index = 0, outputOffset = 0; index < (nuint)inputLength; index++, outputOffset += maxOutputElementSize)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                ReverseWriteFloat32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), Unsafe.Add(ref inputIterator, index));
            }

            writer.Advance(outputLength);
            length -= inputLength;
            inputIterator = ref Unsafe.Add(ref inputIterator, inputLength);
        }
    }
}
