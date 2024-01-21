// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MessagePack.Formatters;

public sealed partial class DoubleArrayFormatter : IMessagePackFormatter<double[]?>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref byte WriteFloat64(ref byte destination, ulong value)
    {
        destination = MessagePackCode.Float64;
        Unsafe.As<byte, ulong>(ref Unsafe.AddByteOffset(ref destination, 1)) = value;
        return ref Unsafe.AddByteOffset(ref destination, 9);
    }

    public void Serialize(ref MessagePackWriter writer, double[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        var inputLength = value.Length;
        writer.WriteArrayHeader(inputLength);
        if (inputLength == 0)
        {
            return;
        }

        var outputLength = inputLength * 9;
        var destination = writer.GetSpan(outputLength);
        ref var outputIterator = ref MemoryMarshal.GetReference(destination);
        ref var inputIterator = ref Unsafe.As<double, ulong>(ref MemoryMarshal.GetArrayDataReference(value));
        ref var inputEnd = ref Unsafe.Add(ref inputIterator, inputLength);

        if (Vector512.IsHardwareAccelerated && inputLength >= 16)
        {
            const int ShiftCount = 3;
            const int Stride = 1 << ShiftCount;

            for (ref var vectorizedEnd = ref Unsafe.Add(ref inputIterator, (inputLength >> ShiftCount) << ShiftCount);
                !Unsafe.AreSame(ref inputIterator, ref vectorizedEnd);
                inputIterator = ref Unsafe.Add(ref inputIterator, Stride))
            {
                // Fetch 8 doubles.
                var current = Vector512.LoadUnsafe(ref inputIterator).AsByte();

                // Reorder Little Endian bytes to Big Endian.
                var answer = Vector512.Shuffle(current, Vector512.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24, 39, 38, 37, 36, 35, 34, 33, 32, 47, 46, 45, 44, 43, 42, 41, 40, 55, 54, 53, 52, 51, 50, 49, 48, 63, 62, 61, 60, 59, 58, 57, 56)).AsUInt64();

                // Write 8 Big-Endian doubles.
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(0));
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(1));
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(2));
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(3));
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(4));
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(5));
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(6));
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(7));
            }
        }
        else if (Vector256.IsHardwareAccelerated && inputLength >= 8)
        {
            const int ShiftCount = 2;
            const int Stride = 1 << ShiftCount;

            for (ref var vectorizedEnd = ref Unsafe.Add(ref inputIterator, (inputLength >> ShiftCount) << ShiftCount);
                !Unsafe.AreSame(ref inputIterator, ref vectorizedEnd);
                inputIterator = ref Unsafe.Add(ref inputIterator, Stride))
            {
                // Fetch 4 doubles.
                var current = Vector256.LoadUnsafe(ref inputIterator).AsByte();

                // Reorder Little Endian bytes to Big Endian.
                var answer = Vector256.Shuffle(current, Vector256.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24)).AsUInt64();

                // Write 4 Big-Endian doubles.
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(0));
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(1));
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(2));
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(3));
            }
        }
        else if (Vector128.IsHardwareAccelerated && inputLength >= 4)
        {
            const int ShiftCount = 1;
            const int Stride = 1 << ShiftCount;

            for (ref var vectorizedEnd = ref Unsafe.Add(ref inputIterator, (inputLength >> ShiftCount) << ShiftCount);
                !Unsafe.AreSame(ref inputIterator, ref vectorizedEnd);
                inputIterator = ref Unsafe.Add(ref inputIterator, Stride))
            {
                // Fetch 2 doubles.
                var current = Vector128.LoadUnsafe(ref inputIterator).AsByte();

                // Reorder Little Endian bytes to Big Endian.
                var answer = Vector128.Shuffle(current, Vector128.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)).AsUInt64();

                // Write 2 Big-Endian doubles.
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(0));
                outputIterator = ref WriteFloat64(ref outputIterator, answer.GetElement(1));
            }
        }

        for (;
            !Unsafe.AreSame(ref inputIterator, ref inputEnd);
            inputIterator = ref Unsafe.Add(ref inputIterator, 1),
            outputIterator = ref Unsafe.AddByteOffset(ref outputIterator, 9))
        {
            var current = inputIterator;
            outputIterator = MessagePackCode.Float64;
            Unsafe.AddByteOffset(ref outputIterator, 1) = (byte)(current >> 56);
            Unsafe.AddByteOffset(ref outputIterator, 2) = (byte)(current >> 48);
            Unsafe.AddByteOffset(ref outputIterator, 3) = (byte)(current >> 40);
            Unsafe.AddByteOffset(ref outputIterator, 4) = (byte)(current >> 32);
            Unsafe.AddByteOffset(ref outputIterator, 5) = (byte)(current >> 24);
            Unsafe.AddByteOffset(ref outputIterator, 6) = (byte)(current >> 16);
            Unsafe.AddByteOffset(ref outputIterator, 7) = (byte)(current >> 8);
            Unsafe.AddByteOffset(ref outputIterator, 8) = (byte)current;
        }

        writer.Advance(outputLength);
    }
}
