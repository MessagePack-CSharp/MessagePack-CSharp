// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MessagePack.Formatters;

internal static partial class ReadOnlySpanSerializeHelper
{
    internal static void Serialize(ref MessagePackWriter writer, ref readonly double input, int length)
    {
        writer.WriteArrayHeader(length);
        if (length == 0)
        {
            return;
        }

        if (!BitConverter.IsLittleEndian)
        {
            BigEndianSerialize(ref writer, ref Unsafe.AsRef(in input), length, writer.CancellationToken);
            return;
        }

        ref var inputIterator = ref Unsafe.As<double, ulong>(ref Unsafe.AsRef(in input));
        const int maxInputSize = int.MaxValue / (sizeof(double) + 1);
        if (Vector512.IsHardwareAccelerated)
        {
            for (var alignedInputLength = (nuint)(length & (~7)); alignedInputLength > 0; alignedInputLength = (nuint)(length & (~7)))
            {
                if (alignedInputLength >= maxInputSize)
                {
                    alignedInputLength = maxInputSize & (~7);
                }

                var outputLength = (int)alignedInputLength * (sizeof(double) + 1);
                var destination = writer.GetSpan(outputLength);
                ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                for (nuint inputOffset = 0, outputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector512<ulong>.Count, outputOffset += (nuint)Vector512<ulong>.Count * (sizeof(double) + 1))
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();

                    // Reorder Little Endian bytes to Big Endian.
                    var shuffled = Vector512.Shuffle(Vector512.LoadUnsafe(ref inputIterator, inputOffset).AsByte(), Vector512.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24, 39, 38, 37, 36, 35, 34, 33, 32, 47, 46, 45, 44, 43, 42, 41, 40, 55, 54, 53, 52, 51, 50, 49, 48, 63, 62, 61, 60, 59, 58, 57, 56)).AsUInt64();

                    // Write 8 Big-Endian doubles.
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.GetElement(0));
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 9), shuffled.GetElement(1));
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 18), shuffled.GetElement(2));
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 27), shuffled.GetElement(3));
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 36), shuffled.GetElement(4));
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 45), shuffled.GetElement(5));
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 54), shuffled.GetElement(6));
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 63), shuffled.GetElement(7));
                }

                writer.Advance(outputLength);
                length -= (int)alignedInputLength;
                inputIterator = ref Unsafe.Add(ref inputIterator, alignedInputLength);
            }
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            for (var alignedInputLength = (nuint)(length & (~3)); alignedInputLength > 0; alignedInputLength = (nuint)(length & (~3)))
            {
                if (alignedInputLength >= maxInputSize)
                {
                    alignedInputLength = maxInputSize & (~3);
                }

                var outputLength = (int)alignedInputLength * (sizeof(double) + 1);
                var destination = writer.GetSpan(outputLength);
                ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                for (nuint inputOffset = 0, outputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector256<ulong>.Count, outputOffset += (nuint)Vector256<ulong>.Count * (sizeof(double) + 1))
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();

                    // Reorder Little Endian bytes to Big Endian.
                    var shuffled = Vector256.Shuffle(Vector256.LoadUnsafe(ref inputIterator, inputOffset).AsByte(), Vector256.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24)).AsUInt64();

                    // Write 4 Big-Endian doubles.
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.GetElement(0));
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 9), shuffled.GetElement(1));
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 18), shuffled.GetElement(2));
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 27), shuffled.GetElement(3));
                }

                writer.Advance(outputLength);
                length -= (int)alignedInputLength;
                inputIterator = ref Unsafe.Add(ref inputIterator, alignedInputLength);
            }
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            for (var alignedInputLength = (nuint)(length & (~1)); alignedInputLength > 0; alignedInputLength = (nuint)(length & (~1)))
            {
                if (alignedInputLength >= maxInputSize)
                {
                    alignedInputLength = maxInputSize & (~1);
                }

                var outputLength = (int)alignedInputLength * (sizeof(double) + 1);
                var destination = writer.GetSpan(outputLength);
                ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                for (nuint inputOffset = 0, outputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<ulong>.Count, outputOffset += (nuint)Vector128<ulong>.Count * (sizeof(double) + 1))
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();

                    // Reorder Little Endian bytes to Big Endian.
                    var shuffled = Vector128.Shuffle(Vector128.LoadUnsafe(ref inputIterator, inputOffset).AsByte(), Vector128.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)).AsUInt64();

                    // Write 2 Big-Endian doubles.
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.GetElement(0));
                    WriteFloat64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset + 9), shuffled.GetElement(1));
                }

                writer.Advance(outputLength);
                length -= (int)alignedInputLength;
                inputIterator = ref Unsafe.Add(ref inputIterator, alignedInputLength);
            }
        }

        {
            var outputLength = length * (sizeof(double) + 1);
            var destination = writer.GetSpan(outputLength);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            for (var index = 0; index < length; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                outputIterator = ref ReverseWriteFloat64(ref outputIterator, inputIterator);
                inputIterator = ref Unsafe.Add(ref inputIterator, 1);
            }

            writer.Advance(outputLength);
        }
    }
}
