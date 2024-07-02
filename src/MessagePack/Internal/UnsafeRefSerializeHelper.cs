// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MessagePack.Internal;

internal static class UnsafeRefSerializeHelper
{
    internal static void Serialize(ref MessagePackWriter writer, ref bool input, int length)
    {
        {
            var destination = writer.GetSpan(length);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            for (nuint index = 0; index < (nuint)length; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                Unsafe.Add(ref outputIterator, index) = Unsafe.Add(ref input, index) ? MessagePackCode.True : MessagePackCode.False;
            }
        }

        writer.Advance(length);
    }

    internal static void Serialize(ref MessagePackWriter writer, ref sbyte input, int length)
    {
        if (BitConverter.IsLittleEndian)
        {
            LittleEndianSerialize(ref writer, ref input, length);
        }
        else
        {
            BigEndianSerialize(ref writer, ref input, length);
        }
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref sbyte input, int length)
    {
        var i = 0;
        do
        {
            writer.CancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref sbyte input, int length)
    {
        const int maxOutputElementSize = sizeof(sbyte) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

        while (length > 0)
        {
            var inputLength = length;
            if (inputLength > maxInputSize)
            {
                inputLength = maxInputSize;
            }

            var destination = writer.GetSpan(inputLength * maxOutputElementSize);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            nuint outputOffset = 0;
            for (nuint index = 0; index < (nuint)inputLength; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    internal static void Serialize(ref MessagePackWriter writer, ref short input, int length)
    {
        if (BitConverter.IsLittleEndian)
        {
            LittleEndianSerialize(ref writer, ref input, length);
        }
        else
        {
            BigEndianSerialize(ref writer, ref input, length);
        }
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref short input, int length)
    {
        var i = 0;
        do
        {
            writer.CancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref short input, int length)
    {
        const int maxOutputElementSize = sizeof(short) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

        while (length > 0)
        {
            var inputLength = length;
            if (inputLength > maxInputSize)
            {
                inputLength = maxInputSize;
            }

            var destination = writer.GetSpan(inputLength * maxOutputElementSize);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            nuint outputOffset = 0;
            for (nuint index = 0; index < (nuint)inputLength; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    internal static void Serialize(ref MessagePackWriter writer, ref char input, int length)
    {
        Serialize(ref writer, ref Unsafe.As<char, ushort>(ref input), length);
    }

    internal static void Serialize(ref MessagePackWriter writer, ref ushort input, int length)
    {
        if (BitConverter.IsLittleEndian)
        {
            LittleEndianSerialize(ref writer, ref input, length);
        }
        else
        {
            BigEndianSerialize(ref writer, ref input, length);
        }
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref ushort input, int length)
    {
        var i = 0;
        do
        {
            writer.CancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref ushort input, int length)
    {
        const int maxOutputElementSize = sizeof(ushort) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

        while (length > 0)
        {
            var inputLength = length;
            if (inputLength > maxInputSize)
            {
                inputLength = maxInputSize;
            }

            var destination = writer.GetSpan(inputLength * maxOutputElementSize);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            nuint outputOffset = 0;
            for (nuint index = 0; index < (nuint)inputLength; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    internal static void Serialize(ref MessagePackWriter writer, ref int input, int length)
    {
        if (BitConverter.IsLittleEndian)
        {
            LittleEndianSerialize(ref writer, ref input, length);
        }
        else
        {
            BigEndianSerialize(ref writer, ref input, length);
        }
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref int input, int length)
    {
        var i = 0;
        do
        {
            writer.CancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref int input, int length)
    {
        const int maxOutputElementSize = sizeof(int) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

        while (length > 0)
        {
            var inputLength = length;
            if (inputLength > maxInputSize)
            {
                inputLength = maxInputSize;
            }

            var destination = writer.GetSpan(inputLength * maxOutputElementSize);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            nuint outputOffset = 0;
            for (nuint index = 0; index < (nuint)inputLength; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    internal static void Serialize(ref MessagePackWriter writer, ref uint input, int length)
    {
        if (BitConverter.IsLittleEndian)
        {
            LittleEndianSerialize(ref writer, ref input, length);
        }
        else
        {
            BigEndianSerialize(ref writer, ref input, length);
        }
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref uint input, int length)
    {
        var i = 0;
        do
        {
            writer.CancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref uint input, int length)
    {
        const int maxOutputElementSize = sizeof(uint) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

        while (length > 0)
        {
            var inputLength = length;
            if (inputLength > maxInputSize)
            {
                inputLength = maxInputSize;
            }

            var destination = writer.GetSpan(inputLength * maxOutputElementSize);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            nuint outputOffset = 0;
            for (nuint index = 0; index < (nuint)inputLength; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    internal static void Serialize(ref MessagePackWriter writer, ref long input, int length)
    {
        if (BitConverter.IsLittleEndian)
        {
            LittleEndianSerialize(ref writer, ref input, length);
        }
        else
        {
            BigEndianSerialize(ref writer, ref input, length);
        }
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref long input, int length)
    {
        var i = 0;
        do
        {
            writer.CancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref long input, int length)
    {
        const int maxOutputElementSize = sizeof(long) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

        while (length > 0)
        {
            var inputLength = length;
            if (inputLength > maxInputSize)
            {
                inputLength = maxInputSize;
            }

            var destination = writer.GetSpan(inputLength * maxOutputElementSize);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            nuint outputOffset = 0;
            for (nuint index = 0; index < (nuint)inputLength; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    internal static void Serialize(ref MessagePackWriter writer, ref ulong input, int length)
    {
        if (BitConverter.IsLittleEndian)
        {
            LittleEndianSerialize(ref writer, ref input, length);
        }
        else
        {
            BigEndianSerialize(ref writer, ref input, length);
        }
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref ulong input, int length)
    {
        var i = 0;
        do
        {
            writer.CancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref ulong input, int length)
    {
        const int maxOutputElementSize = sizeof(ulong) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

        while (length > 0)
        {
            var inputLength = length;
            if (inputLength > maxInputSize)
            {
                inputLength = maxInputSize;
            }

            var destination = writer.GetSpan(inputLength * maxOutputElementSize);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            nuint outputOffset = 0;
            for (nuint index = 0; index < (nuint)inputLength; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    internal static void Serialize(ref MessagePackWriter writer, ref float input, int length)
    {
        if (BitConverter.IsLittleEndian)
        {
            LittleEndianSerialize(ref writer, ref input, length);
        }
        else
        {
            BigEndianSerialize(ref writer, ref input, length);
        }
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref float input, int length)
    {
        ref var inputIterator = ref Unsafe.As<float, uint>(ref input);
        const int maxOutputElementSize = sizeof(float) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

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
                Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Float32;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), Unsafe.Add(ref inputIterator, index));
            }

            writer.Advance(outputLength);
            length -= inputLength;
            inputIterator = ref Unsafe.Add(ref inputIterator, inputLength);
        }
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref float input, int length)
    {
        ref var inputIterator = ref Unsafe.As<float, uint>(ref input);
        const int maxOutputElementSize = sizeof(float) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

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
                Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Float32;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), BinaryPrimitives.ReverseEndianness(Unsafe.Add(ref inputIterator, index)));
            }

            writer.Advance(outputLength);
            length -= inputLength;
            inputIterator = ref Unsafe.Add(ref inputIterator, inputLength);
        }
    }

    internal static void Serialize(ref MessagePackWriter writer, ref double input, int length)
    {
        if (BitConverter.IsLittleEndian)
        {
            LittleEndianSerialize(ref writer, ref input, length);
        }
        else
        {
            BigEndianSerialize(ref writer, ref input, length);
        }
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref double input, int length)
    {
        ref var inputIterator = ref Unsafe.As<double, ulong>(ref input);
        const int maxOutputElementSize = sizeof(double) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

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
                Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Float64;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), Unsafe.Add(ref inputIterator, index));
            }

            writer.Advance(outputLength);
            length -= inputLength;
            inputIterator = ref Unsafe.Add(ref inputIterator, inputLength);
        }
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref double input, int length)
    {
        ref var inputIterator = ref Unsafe.As<double, ulong>(ref input);
        const int maxOutputElementSize = sizeof(double) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

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
                Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Float64;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), BinaryPrimitives.ReverseEndianness(Unsafe.Add(ref inputIterator, index)));
            }

            writer.Advance(outputLength);
            length -= inputLength;
            inputIterator = ref Unsafe.Add(ref inputIterator, inputLength);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, byte value)
    {
        if (value <= MessagePackRange.MaxFixPositiveInt)
        {
            destination = value;
            return 1;
        }
        else
        {
            destination = MessagePackCode.UInt8;
            Unsafe.Add(ref destination, 1) = value;
            return 2;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, sbyte value)
    {
        if (value < MessagePackRange.MinFixNegativeInt)
        {
            destination = MessagePackCode.Int8;
            Unsafe.Add(ref destination, 1) = unchecked((byte)value);
            return 2;
        }
        else
        {
            destination = unchecked((byte)value);
            return 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, ushort value)
    {
        if (value <= byte.MaxValue)
        {
            return ReverseWriteUnknown(ref destination, unchecked((byte)value));
        }
        else
        {
            destination = MessagePackCode.UInt16;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(value));
            return 3;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, short value)
    {
        if (value < 0)
        {
            if (value >= sbyte.MinValue)
            {
                return ReverseWriteUnknown(ref destination, unchecked((sbyte)value));
            }
            else
            {
                destination = MessagePackCode.Int16;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(value));
                return 3;
            }
        }
        else
        {
            return ReverseWriteUnknown(ref destination, unchecked((ushort)value));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, uint value)
    {
        if (value <= ushort.MaxValue)
        {
            return ReverseWriteUnknown(ref destination, unchecked((ushort)value));
        }
        else
        {
            destination = MessagePackCode.UInt32;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(value));
            return 5;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, int value)
    {
        if (value < 0)
        {
            if (value >= short.MinValue)
            {
                return ReverseWriteUnknown(ref destination, unchecked((short)value));
            }
            else
            {
                destination = MessagePackCode.Int32;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(value));
                return 5;
            }
        }
        else
        {
            return ReverseWriteUnknown(ref destination, unchecked((uint)value));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, ulong value)
    {
        if (value <= uint.MaxValue)
        {
            return ReverseWriteUnknown(ref destination, unchecked((uint)value));
        }
        else
        {
            destination = MessagePackCode.UInt64;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(value));
            return 9;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReverseWriteUnknown(ref byte destination, long value)
    {
        if (value < 0)
        {
            if (value >= int.MinValue)
            {
                return ReverseWriteUnknown(ref destination, unchecked((int)value));
            }
            else
            {
                destination = MessagePackCode.Int64;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness(value));
                return 9;
            }
        }
        else
        {
            return ReverseWriteUnknown(ref destination, unchecked((ulong)value));
        }
    }
}
