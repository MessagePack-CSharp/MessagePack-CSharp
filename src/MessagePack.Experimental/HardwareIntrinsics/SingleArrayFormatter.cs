// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MessagePack.Formatters;

public sealed partial class SingleArrayFormatter : IMessagePackFormatter<float[]?>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref byte WriteFloat32(ref byte destination, uint value)
    {
        destination = MessagePackCode.Float32;
        Unsafe.As<byte, uint>(ref Unsafe.AddByteOffset(ref destination, 1)) = value;
        return ref Unsafe.AddByteOffset(ref destination, 5);
    }

    public void Serialize(ref MessagePackWriter writer, float[]? value, MessagePackSerializerOptions options)
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

        var outputLength = inputLength * 5;
        var destination = writer.GetSpan(outputLength);
        ref var outputIterator = ref MemoryMarshal.GetReference(destination);
        ref var inputIterator = ref Unsafe.As<float, uint>(ref MemoryMarshal.GetArrayDataReference(value));
        ref var inputEnd = ref Unsafe.Add(ref inputIterator, inputLength);

        if (Vector512.IsHardwareAccelerated && inputLength >= 32)
        {
            const int ShiftCount = 4;
            const int Stride = 1 << ShiftCount;

            for (ref var vectorizedEnd = ref Unsafe.Add(ref inputIterator, (inputLength >> ShiftCount) << ShiftCount);
                !Unsafe.AreSame(ref inputIterator, ref vectorizedEnd);
                inputIterator = ref Unsafe.Add(ref inputIterator, Stride))
            {
                // Fetch 16 floats.
                var current = Vector512.LoadUnsafe(ref inputIterator).AsByte();

                // Reorder Little Endian bytes to Big Endian.
                var answer = Vector512.Shuffle(current, Vector512.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12, 19, 18, 17, 16, 23, 22, 21, 20, 27, 26, 25, 24, 31, 30, 29, 28, 35, 34, 33, 32, 39, 38, 37, 36, 43, 42, 41, 40, 47, 46, 45, 44, 51, 50, 49, 48, 55, 54, 53, 52, 59, 58, 57, 56, 63, 62, 61, 60)).AsUInt32();

                // Write 16 Big-Endian floats.
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(0));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(1));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(2));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(3));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(4));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(5));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(6));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(7));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(8));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(9));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(10));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(11));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(12));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(13));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(14));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(15));
            }
        }
        else if (Vector256.IsHardwareAccelerated && inputLength >= 16)
        {
            const int ShiftCount = 3;
            const int Stride = 1 << ShiftCount;

            for (ref var vectorizedEnd = ref Unsafe.Add(ref inputIterator, (inputLength >> ShiftCount) << ShiftCount);
                !Unsafe.AreSame(ref inputIterator, ref vectorizedEnd);
                inputIterator = ref Unsafe.Add(ref inputIterator, Stride))
            {
                // Fetch 8 floats.
                var current = Vector256.LoadUnsafe(ref inputIterator).AsByte();

                // Reorder Little Endian bytes to Big Endian.
                var answer = Vector256.Shuffle(current, Vector256.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12, 19, 18, 17, 16, 23, 22, 21, 20, 27, 26, 25, 24, 31, 30, 29, 28)).AsUInt32();

                // Write 8 Big-Endian floats.
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(0));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(1));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(2));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(3));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(4));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(5));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(6));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(7));
            }
        }
        else if (Vector128.IsHardwareAccelerated && inputLength >= 8)
        {
            const int ShiftCount = 2;
            const int Stride = 1 << ShiftCount;

            for (ref var vectorizedEnd = ref Unsafe.Add(ref inputIterator, (inputLength >> ShiftCount) << ShiftCount);
                !Unsafe.AreSame(ref inputIterator, ref vectorizedEnd);
                inputIterator = ref Unsafe.Add(ref inputIterator, Stride))
            {
                // Fetch 4 floats.
                var current = Vector128.LoadUnsafe(ref inputIterator).AsByte();

                // Reorder Little Endian bytes to Big Endian.
                var answer = Vector128.Shuffle(current, Vector128.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)).AsUInt32();

                // Write 4 Big-Endian floats.
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(0));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(1));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(2));
                outputIterator = ref WriteFloat32(ref outputIterator, answer.GetElement(3));
            }
        }

        for (;
            !Unsafe.AreSame(ref inputIterator, ref inputEnd);
            inputIterator = ref Unsafe.Add(ref inputIterator, 1),
            outputIterator = ref Unsafe.AddByteOffset(ref outputIterator, 5))
        {
            var current = inputIterator;
            outputIterator = MessagePackCode.Float32;
            Unsafe.AddByteOffset(ref outputIterator, 1) = (byte)(current >> 24);
            Unsafe.AddByteOffset(ref outputIterator, 2) = (byte)(current >> 16);
            Unsafe.AddByteOffset(ref outputIterator, 3) = (byte)(current >> 8);
            Unsafe.AddByteOffset(ref outputIterator, 4) = (byte)current;
        }

        writer.Advance(outputLength);
    }
}
