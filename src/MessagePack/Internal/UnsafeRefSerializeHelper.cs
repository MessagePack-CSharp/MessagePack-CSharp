// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*
 * The reason why serialize method signature is void Serialize(ref MessagePackWriter writer, ref T input, int length)
 * 1. Vector128.Load method requires only ref T or T*, not ReadOnlySpan<T>.
 * 2. For the purpose of getting ref T, MemoryMarshal.GetArrayDataReference is performant than ReadOnlySpan<T> conversion + MemoryMarshal.GetReference.
 * 3. Unity IL2CPP does not treat ReadOnlySpan<T> as first-class-citizen.
 *
 * When .NET team introduces the overload method for ReadOnlySpan<T>, the signature can be changed.
 */

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace MessagePack.Internal;

internal static class UnsafeRefSerializeHelper
{
    /// <summary>Unsafe serialize method without parameter checks nor cancellation.</summary>
    /// <param name="writer">MessagePackWriter.</param>
    /// <param name="input">Must not be null reference.</param>
    /// <param name="length">Must be greater than 0.</param>
    internal static void Serialize(ref MessagePackWriter writer, ref bool input, int length)
    {
#if NET8_0_OR_GREATER
        unchecked
        {
            if (Vector128.IsHardwareAccelerated)
            {
                const int mask = ~15;
                var alignedInputLength = (nuint)(uint)(length & mask);
                if (alignedInputLength > 0)
                {
                    var destination = writer.GetSpan((int)alignedInputLength);
                    ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                    for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<byte>.Count)
                    {
                        // Load 16 bools.
                        var loaded = Vector128.LoadUnsafe(ref Unsafe.As<bool, sbyte>(ref input), inputOffset);

                        // true is not 0, but false is always 0 in C#. true -> 0. false -> -1.
                        var falses = Vector128.Equals(loaded, Vector128<sbyte>.Zero);

                        // MessagePackCode.True is 0xc3(-61). MessagePackCode.False is 0xc2(-62).
                        var results = Vector128.Create((sbyte)MessagePackCode.True) + falses;

                        // Store 16 values.
                        results.AsByte().StoreUnsafe(ref outputIterator, inputOffset);
                    }

                    writer.Advance((int)alignedInputLength);
                    length -= (int)alignedInputLength;
                    input = ref Unsafe.Add(ref input, alignedInputLength);
                }
            }
        }
#endif
        {
            var destination = writer.GetSpan(length);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            for (nuint index = 0; index < (nuint)length; index++)
            {
                Unsafe.Add(ref outputIterator, index) = Unsafe.Add(ref input, index) ? MessagePackCode.True : MessagePackCode.False;
            }
        }

        writer.Advance(length);
    }

    /// <summary>Unsafe serialize method without parameter checks nor cancellation.</summary>
    /// <param name="writer">MessagePackWriter.</param>
    /// <param name="input">Must not be null reference.</param>
    /// <param name="length">Must be greater than 0.</param>
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
        for (int i = 0; i < length; i++)
        {
            writer.Write(Unsafe.Add(ref input, i));
        }
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref sbyte input, int length)
    {
        const int maxOutputElementSize = sizeof(sbyte) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

#if NET8_0_OR_GREATER
        unchecked
        {
            if (Vector256.IsHardwareAccelerated)
            {
                const int mask = ~31;
                for (var alignedInputLength = (nuint)(length & mask); alignedInputLength > 0; alignedInputLength = (nuint)(length & mask))
                {
                    if (alignedInputLength >= maxInputSize)
                    {
                        alignedInputLength = maxInputSize & mask;
                    }

                    var destination = writer.GetSpan((int)alignedInputLength * maxOutputElementSize);
                    ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                    nuint outputOffset = 0;
                    for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector256<sbyte>.Count)
                    {
                        var loaded = Vector256.LoadUnsafe(ref input, inputOffset);

                        // Less than MinFixNegativeInt value requires 2 byte.
                        var lessThanMinFixNegativeInt = Vector256.ExtractMostSignificantBits(Vector256.LessThan(loaded, Vector256.Create((sbyte)MessagePackRange.MinFixNegativeInt)));
                        if (lessThanMinFixNegativeInt == 0)
                        {
                            loaded.AsByte().StoreUnsafe(ref Unsafe.Add(ref outputIterator, outputOffset));
                            outputOffset += (nuint)Vector256<sbyte>.Count;
                        }
                        else
                        {
                            for (var i = 0; i < Vector256<sbyte>.Count; i++, lessThanMinFixNegativeInt >>= 1)
                            {
                                var inputValue = Unsafe.Add(ref input, inputOffset + (nuint)i);
                                if ((lessThanMinFixNegativeInt & 1) == 1)
                                {
                                    Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Int8;
                                    Unsafe.Add(ref outputIterator, outputOffset + 1) = (byte)inputValue;
                                    outputOffset += 2;
                                }
                                else
                                {
                                    Unsafe.Add(ref outputIterator, outputOffset) = (byte)inputValue;
                                    outputOffset += 1;
                                }
                            }
                        }
                    }

                    writer.Advance((int)outputOffset);
                    length -= (int)alignedInputLength;
                    input = ref Unsafe.Add(ref input, alignedInputLength);
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                const int mask = ~15;
                for (var alignedInputLength = (nuint)(length & mask); alignedInputLength > 0; alignedInputLength = (nuint)(length & mask))
                {
                    if (alignedInputLength >= maxInputSize)
                    {
                        alignedInputLength = maxInputSize & mask;
                    }

                    var destination = writer.GetSpan((int)alignedInputLength * maxOutputElementSize);
                    ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                    nuint outputOffset = 0;
                    for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<sbyte>.Count)
                    {
                        var loaded = Vector128.LoadUnsafe(ref input, inputOffset);

                        // Less than MinFixNegativeInt value requires 2 byte.
                        var lessThanMinFixNegativeInt = Vector128.ExtractMostSignificantBits(Vector128.LessThan(loaded, Vector128.Create((sbyte)MessagePackRange.MinFixNegativeInt)));
                        if (lessThanMinFixNegativeInt == 0)
                        {
                            loaded.AsByte().StoreUnsafe(ref Unsafe.Add(ref outputIterator, outputOffset));
                            outputOffset += (nuint)Vector128<sbyte>.Count;
                        }
                        else
                        {
                            for (var i = 0; i < Vector128<sbyte>.Count; i++, lessThanMinFixNegativeInt >>= 1)
                            {
                                var inputValue = Unsafe.Add(ref input, inputOffset + (nuint)i);
                                if ((lessThanMinFixNegativeInt & 1) == 1)
                                {
                                    Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Int8;
                                    Unsafe.Add(ref outputIterator, outputOffset + 1) = (byte)inputValue;
                                    outputOffset += 2;
                                }
                                else
                                {
                                    Unsafe.Add(ref outputIterator, outputOffset) = (byte)inputValue;
                                    outputOffset += 1;
                                }
                            }
                        }
                    }

                    writer.Advance((int)outputOffset);
                    length -= (int)alignedInputLength;
                    input = ref Unsafe.Add(ref input, alignedInputLength);
                }
            }
        }
#endif

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
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    /// <summary>Unsafe serialize method without parameter checks nor cancellation.</summary>
    /// <param name="writer">MessagePackWriter.</param>
    /// <param name="input">Must not be null reference.</param>
    /// <param name="length">Must be greater than 0.</param>
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
        for (int i = 0; i < length; i++)
        {
            writer.Write(Unsafe.Add(ref input, i));
        }
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref short input, int length)
    {
        const int maxOutputElementSize = sizeof(short) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

#if NET8_0_OR_GREATER
        unchecked
        {
            if (Vector128.IsHardwareAccelerated)
            {
                const int mask = ~7;
                for (var alignedInputLength = (nuint)(length & mask); alignedInputLength > 0; alignedInputLength = (nuint)(length & mask))
                {
                    if (alignedInputLength >= maxInputSize)
                    {
                        alignedInputLength = maxInputSize & mask;
                    }

                    var destination = writer.GetSpan((int)alignedInputLength * maxOutputElementSize);
                    ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                    nuint outputOffset = 0;
                    for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<short>.Count)
                    {
                        var loaded = Vector128.LoadUnsafe(ref input, inputOffset);

                        // Less than sbyte.MinValue value requires 3 byte.
                        var gteSByteMinValue = Vector128.GreaterThanOrEqual(loaded, Vector128.Create((short)sbyte.MinValue));

                        // Less than MinFixNegativeInt value requires 2 byte.
                        var gteMinFixNegativeInt = Vector128.GreaterThanOrEqual(loaded, Vector128.Create((short)MessagePackRange.MinFixNegativeInt));

                        // GreaterThan MaxFixPositiveInt value requires 2 byte.
                        var gtMaxFixPositiveInt = Vector128.GreaterThan(loaded, Vector128.Create((short)MessagePackRange.MaxFixPositiveInt));

                        // GreaterThan byte.MaxValue value requires 3 byte.
                        var gtByteMaxValue = Vector128.GreaterThan(loaded, Vector128.Create((short)byte.MaxValue));

                        // 0 -> Int16, 3byte
                        // 1 -> Int8, 2byte
                        // 2 -> None, 1byte
                        // 3 -> UInt8, 2byte
                        // 4 -> UInt16, 3byte
                        var kinds = -(gteSByteMinValue + gteMinFixNegativeInt + gtMaxFixPositiveInt + gtByteMaxValue);
                        if (kinds == Vector128.Create((short)2))
                        {
                            // Reorder Big-Endian
                            var shuffled = Vector128.Shuffle(loaded.AsByte(), Vector128.Create((byte)0, 3, 5, 7, 9, 11, 13, 15, 0, 0, 0, 0, 0, 0, 0, 0)).AsUInt64().GetElement(0);
                            Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset), shuffled);
                            outputOffset += sizeof(ulong);
                        }
                        else
                        {
                            // Reorder Big-Endian
                            var shuffled = Vector128.Shuffle(loaded.AsByte(), Vector128.Create((byte)1, 0, 3, 2, 5, 4, 7, 6, 9, 8, 11, 10, 13, 12, 15, 14)).AsInt16();

                            for (var i = 0; i < Vector128<short>.Count; i++)
                            {
                                switch (kinds.GetElement(i))
                                {
                                    case 0:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Int16;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.GetElement(i));
                                        outputOffset += 3;
                                        break;
                                    case 1:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Int8;
                                        Unsafe.Add(ref outputIterator, outputOffset + 1) = shuffled.AsByte().GetElement((i * 2) + 1);
                                        outputOffset += 2;
                                        break;
                                    case 2:
                                        Unsafe.Add(ref outputIterator, outputOffset) = shuffled.AsByte().GetElement((i * 2) + 1);
                                        outputOffset += 1;
                                        break;
                                    case 3:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt8;
                                        Unsafe.Add(ref outputIterator, outputOffset + 1) = shuffled.AsByte().GetElement((i * 2) + 1);
                                        outputOffset += 2;
                                        break;
                                    default:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt16;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.GetElement(i));
                                        outputOffset += 3;
                                        break;
                                }
                            }
                        }
                    }

                    writer.Advance((int)outputOffset);
                    length -= (int)alignedInputLength;
                    input = ref Unsafe.Add(ref input, alignedInputLength);
                }
            }
        }
#endif

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
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    /// <summary>Unsafe serialize method without parameter checks nor cancellation.</summary>
    /// <param name="writer">MessagePackWriter.</param>
    /// <param name="input">Must not be null reference.</param>
    /// <param name="length">Must be greater than 0.</param>
    internal static void Serialize(ref MessagePackWriter writer, ref char input, int length)
    {
        Serialize(ref writer, ref Unsafe.As<char, ushort>(ref input), length);
    }

    /// <summary>Unsafe serialize method without parameter checks nor cancellation.</summary>
    /// <param name="writer">MessagePackWriter.</param>
    /// <param name="input">Must not be null reference.</param>
    /// <param name="length">Must be greater than 0.</param>
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
        for (int i = 0; i < length; i++)
        {
            writer.Write(Unsafe.Add(ref input, i));
        }
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref ushort input, int length)
    {
        const int maxOutputElementSize = sizeof(ushort) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

#if NET8_0_OR_GREATER
        unchecked
        {
            if (Vector128.IsHardwareAccelerated)
            {
                const int mask = ~7;
                for (var alignedInputLength = (nuint)(length & mask); alignedInputLength > 0; alignedInputLength = (nuint)(length & mask))
                {
                    if (alignedInputLength >= maxInputSize)
                    {
                        alignedInputLength = maxInputSize & mask;
                    }

                    var destination = writer.GetSpan((int)alignedInputLength * (sizeof(short) + 1));
                    ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                    nuint outputOffset = 0;
                    for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<short>.Count)
                    {
                        var loaded = Vector128.LoadUnsafe(ref input, inputOffset).AsInt16();

                        // LessThan 0 means ushort max range and requires 3 byte.
                        var gte0 = Vector128.GreaterThanOrEqual(loaded, Vector128<short>.Zero);

                        // GreaterThan MaxFixPositiveInt value requires 2 byte.
                        var gtMaxFixPositiveInt = Vector128.GreaterThan(loaded, Vector128.Create((short)MessagePackRange.MaxFixPositiveInt));

                        // GreaterThan byte.MaxValue value requires 3 byte.
                        var gtByteMaxValue = Vector128.GreaterThan(loaded, Vector128.Create((short)byte.MaxValue));

                        // -1 -> UInt16, 3byte
                        // +0 -> None, 1byte
                        // +1 -> UInt8, 2byte
                        // +2 -> UInt16, 3byte
                        // Vector128<short>.AllBitsSet means -1.
                        var kinds = Vector128<short>.AllBitsSet - gte0 - gtMaxFixPositiveInt - gtByteMaxValue;
                        if (kinds == Vector128<short>.Zero)
                        {
                            // Reorder Big-Endian
                            var shuffled = Vector128.Shuffle(loaded.AsByte(), Vector128.Create((byte)0, 3, 5, 7, 9, 11, 13, 15, 0, 0, 0, 0, 0, 0, 0, 0)).AsUInt64().GetElement(0);
                            Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset), shuffled);
                            outputOffset += sizeof(ulong);
                        }
                        else
                        {
                            // Reorder Big-Endian
                            var shuffled = Vector128.Shuffle(loaded.AsByte(), Vector128.Create((byte)1, 0, 3, 2, 5, 4, 7, 6, 9, 8, 11, 10, 13, 12, 15, 14));
                            for (var i = 0; i < Vector128<short>.Count; i++)
                            {
                                switch (kinds.GetElement(i))
                                {
                                    case 0:
                                        Unsafe.Add(ref outputIterator, outputOffset) = shuffled.GetElement((i * 2) + 1);
                                        outputOffset += 1;
                                        break;
                                    case 1:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt8;
                                        Unsafe.Add(ref outputIterator, outputOffset + 1) = shuffled.GetElement((i * 2) + 1);
                                        outputOffset += 2;
                                        break;
                                    default:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt16;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.AsUInt16().GetElement(i));
                                        outputOffset += 3;
                                        break;
                                }
                            }
                        }
                    }

                    writer.Advance((int)outputOffset);
                    length -= (int)alignedInputLength;
                    input = ref Unsafe.Add(ref input, alignedInputLength);
                }
            }
        }
#endif

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
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    /// <summary>Unsafe serialize method without parameter checks nor cancellation.</summary>
    /// <param name="writer">MessagePackWriter.</param>
    /// <param name="input">Must not be null reference.</param>
    /// <param name="length">Must be greater than 0.</param>
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
        for (int i = 0; i < length; i++)
        {
            writer.Write(Unsafe.Add(ref input, i));
        }
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref int input, int length)
    {
        const int maxOutputElementSize = sizeof(int) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

#if NET8_0_OR_GREATER
        unchecked
        {
            if (Vector128.IsHardwareAccelerated)
            {
                const int mask = ~3;
                for (var alignedInputLength = (nuint)(length & mask); alignedInputLength > 0; alignedInputLength = (nuint)(length & mask))
                {
                    if (alignedInputLength >= maxInputSize)
                    {
                        alignedInputLength = maxInputSize & mask;
                    }

                    var destination = writer.GetSpan((int)alignedInputLength * maxOutputElementSize);
                    ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                    nuint outputOffset = 0;
                    for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<int>.Count)
                    {
                        var loaded = Vector128.LoadUnsafe(ref input, inputOffset);

                        // Less than short.MinValue value requires 5 byte.
                        var gteInt16MinValue = Vector128.GreaterThanOrEqual(loaded, Vector128.Create((int)short.MinValue));

                        // Less than sbyte.MinValue value requires 3 byte.
                        var gteSByteMinValue = Vector128.GreaterThanOrEqual(loaded, Vector128.Create((int)sbyte.MinValue));

                        // Less than MinFixNegativeInt value requires 2 byte.
                        var gteMinFixNegativeInt = Vector128.GreaterThanOrEqual(loaded, Vector128.Create(MessagePackRange.MinFixNegativeInt));

                        // GreaterThan MaxFixPositiveInt value requires 2 byte.
                        var gtMaxFixPositiveInt = Vector128.GreaterThan(loaded, Vector128.Create(MessagePackRange.MaxFixPositiveInt));

                        // GreaterThan byte.MaxValue value requires 3 byte.
                        var gtByteMaxValue = Vector128.GreaterThan(loaded, Vector128.Create((int)byte.MaxValue));

                        // GreaterThan ushort.MaxValue value requires 5 byte.
                        var gtUInt16MaxValue = Vector128.GreaterThan(loaded, Vector128.Create((int)ushort.MaxValue));

                        // 0 -> Int32,  5byte
                        // 1 -> Int16,  3byte
                        // 2 -> Int8,   2byte
                        // 3 -> None,   1byte
                        // 4 -> UInt8,  2byte
                        // 5 -> UInt16, 3byte
                        // 6 -> UInt32, 5byte
                        var kinds = -(gteInt16MinValue + gteSByteMinValue + gteMinFixNegativeInt + gtMaxFixPositiveInt + gtByteMaxValue + gtUInt16MaxValue);
                        if (kinds == Vector128.Create(3))
                        {
                            // Reorder Big-Endian and Narrowing
                            var shuffled = Vector128.Shuffle(loaded.AsByte(), Vector128.Create((byte)0, 4, 8, 12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)).AsUInt32().GetElement(0);
                            Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset), shuffled);
                            outputOffset += sizeof(uint);
                        }
                        else
                        {
                            // Reorder Big-Endian
                            var shuffled = Vector128.Shuffle(loaded.AsByte(), Vector128.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)).AsInt32();
                            for (var i = 0; i < Vector128<int>.Count; i++)
                            {
                                switch (kinds.GetElement(i))
                                {
                                    case 0:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Int32;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.GetElement(i));
                                        outputOffset += 5;
                                        break;
                                    case 1:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Int16;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.AsInt16().GetElement((i * 2) + 1));
                                        outputOffset += 3;
                                        break;
                                    case 2:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Int8;
                                        Unsafe.Add(ref outputIterator, outputOffset + 1) = shuffled.AsByte().GetElement((i * 4) + 3);
                                        outputOffset += 2;
                                        break;
                                    case 3:
                                        Unsafe.Add(ref outputIterator, outputOffset) = shuffled.AsByte().GetElement((i * 4) + 3);
                                        outputOffset += 1;
                                        break;
                                    case 4:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt8;
                                        Unsafe.Add(ref outputIterator, outputOffset + 1) = shuffled.AsByte().GetElement((i * 4) + 3);
                                        outputOffset += 2;
                                        break;
                                    case 5:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt16;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.AsUInt16().GetElement((i * 2) + 1));
                                        outputOffset += 3;
                                        break;
                                    default:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt32;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.GetElement(i));
                                        outputOffset += 5;
                                        break;
                                }
                            }
                        }
                    }

                    writer.Advance((int)outputOffset);
                    length -= (int)alignedInputLength;
                    input = ref Unsafe.Add(ref input, alignedInputLength);
                }
            }
        }
#endif

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
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    /// <summary>Unsafe serialize method without parameter checks nor cancellation.</summary>
    /// <param name="writer">MessagePackWriter.</param>
    /// <param name="input">Must not be null reference.</param>
    /// <param name="length">Must be greater than 0.</param>
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
        for (int i = 0; i < length; i++)
        {
            writer.Write(Unsafe.Add(ref input, i));
        }
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref uint input, int length)
    {
        const int maxOutputElementSize = sizeof(uint) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

#if NET8_0_OR_GREATER
        unchecked
        {
            if (Vector128.IsHardwareAccelerated)
            {
                const int mask = ~3;
                for (var alignedInputLength = (nuint)(length & mask); alignedInputLength > 0; alignedInputLength = (nuint)(length & mask))
                {
                    if (alignedInputLength >= maxInputSize)
                    {
                        alignedInputLength = maxInputSize & mask;
                    }

                    var destination = writer.GetSpan((int)alignedInputLength * maxOutputElementSize);
                    ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                    nuint outputOffset = 0;
                    for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<uint>.Count)
                    {
                        var loaded = Vector128.LoadUnsafe(ref input, inputOffset).AsInt32();

                        // LessThan 0 means ushort max range and requires 5 byte.
                        var gte0 = Vector128.GreaterThanOrEqual(loaded, Vector128<int>.Zero);

                        // GreaterThan MaxFixPositiveInt value requires 2 byte.
                        var gtMaxFixPositiveInt = Vector128.GreaterThan(loaded, Vector128.Create(MessagePackRange.MaxFixPositiveInt));

                        // GreaterThan byte.MaxValue value requires 3 byte.
                        var gtByteMaxValue = Vector128.GreaterThan(loaded, Vector128.Create((int)byte.MaxValue));

                        // GreaterThan ushort.MaxValue value requires 5 byte.
                        var gtUInt16MaxValue = Vector128.GreaterThan(loaded, Vector128.Create((int)ushort.MaxValue));

                        // -1 -> UInt32, 5byte
                        // +0 -> None, 1byte
                        // +1 -> UInt8, 2byte
                        // +2 -> UInt16, 3byte
                        // +3 -> UInt32, 5byte
                        var kinds = Vector128<int>.AllBitsSet - gte0 - gtMaxFixPositiveInt - gtByteMaxValue - gtUInt16MaxValue;
                        if (kinds == Vector128<int>.Zero)
                        {
                            // Reorder Big-Endian and Narrowing
                            var shuffled = Vector128.Shuffle(loaded.AsByte(), Vector128.Create((byte)0, 4, 8, 12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)).AsUInt32().GetElement(0);
                            Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset), shuffled);
                            outputOffset += sizeof(uint);
                        }
                        else
                        {
                            // Reorder Big-Endian
                            var shuffled = Vector128.Shuffle(loaded.AsByte(), Vector128.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12));
                            for (var i = 0; i < Vector128<uint>.Count; i++)
                            {
                                switch (kinds.GetElement(i))
                                {
                                    case 0:
                                        Unsafe.Add(ref outputIterator, outputOffset) = shuffled.GetElement((i * 4) + 3);
                                        outputOffset += 1;
                                        break;
                                    case 1:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt8;
                                        Unsafe.Add(ref outputIterator, outputOffset + 1) = shuffled.GetElement((i * 4) + 3);
                                        outputOffset += 2;
                                        break;
                                    case 2:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt16;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.AsUInt16().GetElement((i * 2) + 1));
                                        outputOffset += 3;
                                        break;
                                    default:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt32;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.AsUInt32().GetElement(i));
                                        outputOffset += 5;
                                        break;
                                }
                            }
                        }
                    }

                    writer.Advance((int)outputOffset);
                    length -= (int)alignedInputLength;
                    input = ref Unsafe.Add(ref input, alignedInputLength);
                }
            }
        }
#endif

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
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    /// <summary>Unsafe serialize method without parameter checks nor cancellation.</summary>
    /// <param name="writer">MessagePackWriter.</param>
    /// <param name="input">Must not be null reference.</param>
    /// <param name="length">Must be greater than 0.</param>
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
        for (int i = 0; i < length; i++)
        {
            writer.Write(Unsafe.Add(ref input, i));
        }
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref long input, int length)
    {
        const int maxOutputElementSize = sizeof(long) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

#if NET8_0_OR_GREATER
        unchecked
        {
            if (Vector128.IsHardwareAccelerated)
            {
                const int mask = ~1;
                for (var alignedInputLength = (nuint)(length & mask); alignedInputLength > 0; alignedInputLength = (nuint)(length & mask))
                {
                    if (alignedInputLength >= maxInputSize)
                    {
                        alignedInputLength = maxInputSize & mask;
                    }

                    var destination = writer.GetSpan((int)alignedInputLength * maxOutputElementSize);
                    ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                    nuint outputOffset = 0;
                    for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<long>.Count)
                    {
                        var loaded = Vector128.LoadUnsafe(ref input, inputOffset);

                        // Less than int.MinValue value requires 9 byte.
                        var gteInt32MinValue = Vector128.GreaterThanOrEqual(loaded, Vector128.Create((long)int.MinValue));

                        // Less than short.MinValue value requires 5 byte.
                        var gteInt16MinValue = Vector128.GreaterThanOrEqual(loaded, Vector128.Create((long)short.MinValue));

                        // Less than sbyte.MinValue value requires 3 byte.
                        var gteSByteMinValue = Vector128.GreaterThanOrEqual(loaded, Vector128.Create((long)sbyte.MinValue));

                        // Less than MinFixNegativeInt value requires 2 byte.
                        var gteMinFixNegativeInt = Vector128.GreaterThanOrEqual(loaded, Vector128.Create((long)MessagePackRange.MinFixNegativeInt));

                        // GreaterThan MaxFixPositiveInt value requires 2 byte.
                        var gtMaxFixPositiveInt = Vector128.GreaterThan(loaded, Vector128.Create((long)MessagePackRange.MaxFixPositiveInt));

                        // GreaterThan byte.MaxValue value requires 3 byte.
                        var gtByteMaxValue = Vector128.GreaterThan(loaded, Vector128.Create((long)byte.MaxValue));

                        // GreaterThan ushort.MaxValue value requires 5 byte.
                        var gtUInt16MaxValue = Vector128.GreaterThan(loaded, Vector128.Create((long)ushort.MaxValue));

                        // GreaterThan uint.MaxValue value requires 5 byte.
                        var gtUInt32MaxValue = Vector128.GreaterThan(loaded, Vector128.Create((long)uint.MaxValue));

                        // 0 -> Int64,  9byte
                        // 1 -> Int32,  5byte
                        // 2 -> Int16,  3byte
                        // 3 -> Int8,   2byte
                        // 4 -> None,   1byte
                        // 5 -> UInt8,  2byte
                        // 6 -> UInt16, 3byte
                        // 7 -> UInt32, 5byte
                        // 8 -> UInt64, 9byte
                        var kinds = -(gteInt32MinValue + gteInt16MinValue + gteSByteMinValue + gteMinFixNegativeInt + gtMaxFixPositiveInt + gtByteMaxValue + gtUInt16MaxValue + gtUInt32MaxValue);
                        if (kinds == Vector128.Create(4L))
                        {
                            // Reorder Big-Endian and Narrowing
                            var shuffled = Vector128.Shuffle(loaded.AsByte(), Vector128.Create((byte)0, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)).AsUInt16().GetElement(0);
                            Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset), shuffled);
                            outputOffset += sizeof(ushort);
                        }
                        else
                        {
                            // Reorder Big-Endian
                            var shuffled = Vector128.Shuffle(loaded.AsByte(), Vector128.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)).AsInt64();
                            for (var i = 0; i < Vector128<long>.Count; i++)
                            {
                                switch (kinds.GetElement(i))
                                {
                                    case 0:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Int64;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.GetElement(i));
                                        outputOffset += 9;
                                        break;
                                    case 1:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Int32;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.AsInt32().GetElement((i * 2) + 1));
                                        outputOffset += 5;
                                        break;
                                    case 2:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Int16;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.AsInt16().GetElement((i * 4) + 3));
                                        outputOffset += 3;
                                        break;
                                    case 3:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Int8;
                                        Unsafe.Add(ref outputIterator, outputOffset + 1) = shuffled.AsByte().GetElement((i * 8) + 7);
                                        outputOffset += 2;
                                        break;
                                    case 4:
                                        Unsafe.Add(ref outputIterator, outputOffset) = shuffled.AsByte().GetElement((i * 8) + 7);
                                        outputOffset += 1;
                                        break;
                                    case 5:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt8;
                                        Unsafe.Add(ref outputIterator, outputOffset + 1) = shuffled.AsByte().GetElement((i * 8) + 7);
                                        outputOffset += 2;
                                        break;
                                    case 6:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt16;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.AsUInt16().GetElement((i * 4) + 3));
                                        outputOffset += 3;
                                        break;
                                    case 7:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt32;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.AsUInt32().GetElement((i * 2) + 1));
                                        outputOffset += 5;
                                        break;
                                    default:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt64;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.GetElement(i));
                                        outputOffset += 9;
                                        break;
                                }
                            }
                        }
                    }

                    writer.Advance((int)outputOffset);
                    length -= (int)alignedInputLength;
                    input = ref Unsafe.Add(ref input, alignedInputLength);
                }
            }
        }
#endif

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
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    /// <summary>Unsafe serialize method without parameter checks nor cancellation.</summary>
    /// <param name="writer">MessagePackWriter.</param>
    /// <param name="input">Must not be null reference.</param>
    /// <param name="length">Must be greater than 0.</param>
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
        for (int i = 0; i < length; i++)
        {
            writer.Write(Unsafe.Add(ref input, i));
        }
    }

    private static void LittleEndianSerialize(ref MessagePackWriter writer, ref ulong input, int length)
    {
        const int maxOutputElementSize = sizeof(ulong) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;

#if NET8_0_OR_GREATER
        unchecked
        {
            if (Vector128.IsHardwareAccelerated)
            {
                const int mask = ~1;
                for (var alignedInputLength = (nuint)(length & mask); alignedInputLength > 0; alignedInputLength = (nuint)(length & mask))
                {
                    if (alignedInputLength >= maxInputSize)
                    {
                        alignedInputLength = maxInputSize & mask;
                    }

                    var destination = writer.GetSpan((int)alignedInputLength * maxOutputElementSize);
                    ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                    nuint outputOffset = 0;
                    for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<ulong>.Count)
                    {
                        var loaded = Vector128.LoadUnsafe(ref input, inputOffset).AsInt64();

                        // LessThan 0 means ushort max range and requires 5 byte.
                        var gte0 = Vector128.GreaterThanOrEqual(loaded, Vector128<long>.Zero);

                        // GreaterThan MaxFixPositiveInt value requires 2 byte.
                        var gtMaxFixPositiveInt = Vector128.GreaterThan(loaded, Vector128.Create((long)MessagePackRange.MaxFixPositiveInt));

                        // GreaterThan byte.MaxValue value requires 3 byte.
                        var gtByteMaxValue = Vector128.GreaterThan(loaded, Vector128.Create((long)byte.MaxValue));

                        // GreaterThan ushort.MaxValue value requires 5 byte.
                        var gtUInt16MaxValue = Vector128.GreaterThan(loaded, Vector128.Create((long)ushort.MaxValue));

                        // GreaterThan ushort.MaxValue value requires 5 byte.
                        var gtUInt32MaxValue = Vector128.GreaterThan(loaded, Vector128.Create((long)uint.MaxValue));

                        // -1 -> UInt64, 9byte
                        // +0 -> None, 1byte
                        // +1 -> UInt8, 2byte
                        // +2 -> UInt16, 3byte
                        // +3 -> UInt32, 5byte
                        // +4 -> UInt64, 9byte
                        var kinds = Vector128<long>.AllBitsSet - gte0 - gtMaxFixPositiveInt - gtByteMaxValue - gtUInt16MaxValue - gtUInt32MaxValue;
                        if (kinds == Vector128<long>.Zero)
                        {
                            // Reorder Big-Endian and Narrowing
                            var shuffled = Vector128.Shuffle(loaded.AsByte(), Vector128.Create((byte)0, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)).AsUInt16().GetElement(0);
                            Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset), shuffled);
                            outputOffset += sizeof(ushort);
                        }
                        else
                        {
                            // Reorder Big-Endian
                            var shuffled = Vector128.Shuffle(loaded.AsByte(), Vector128.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8));
                            for (var i = 0; i < Vector128<long>.Count; i++)
                            {
                                switch (kinds.GetElement(i))
                                {
                                    case 0:
                                        Unsafe.Add(ref outputIterator, outputOffset) = shuffled.GetElement((i * 8) + 7);
                                        outputOffset += 1;
                                        break;
                                    case 1:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt8;
                                        Unsafe.Add(ref outputIterator, outputOffset + 1) = shuffled.GetElement((i * 8) + 7);
                                        outputOffset += 2;
                                        break;
                                    case 2:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt16;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.AsUInt16().GetElement((i * 4) + 3));
                                        outputOffset += 3;
                                        break;
                                    case 3:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt32;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.AsUInt32().GetElement((i * 2) + 1));
                                        outputOffset += 5;
                                        break;
                                    default:
                                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.UInt64;
                                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.AsUInt64().GetElement(i));
                                        outputOffset += 9;
                                        break;
                                }
                            }
                        }
                    }

                    writer.Advance((int)outputOffset);
                    length -= (int)alignedInputLength;
                    input = ref Unsafe.Add(ref input, alignedInputLength);
                }
            }
        }
#endif

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
                outputOffset += ReverseWriteUnknown(ref Unsafe.Add(ref outputIterator, outputOffset), Unsafe.Add(ref input, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            input = ref Unsafe.Add(ref input, inputLength);
        }
    }

    /// <summary>Unsafe serialize method without parameter checks nor cancellation.</summary>
    /// <param name="writer">MessagePackWriter.</param>
    /// <param name="input">Must not be null reference.</param>
    /// <param name="length">Must be greater than 0.</param>
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

#if NET8_0_OR_GREATER
        unchecked
        {
            if (Vector256.IsHardwareAccelerated)
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
                        // Reorder Little Endian bytes to Big Endian.
                        var shuffled = Vector256.Shuffle(Vector256.LoadUnsafe(ref inputIterator, inputOffset).AsByte(), Vector256.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12, 19, 18, 17, 16, 23, 22, 21, 20, 27, 26, 25, 24, 31, 30, 29, 28)).AsUInt32();

                        // Write 8 Big-Endian floats.
                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Float32;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.GetElement(0));
                        Unsafe.Add(ref outputIterator, outputOffset + 5) = MessagePackCode.Float32;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 6), shuffled.GetElement(0));
                        Unsafe.Add(ref outputIterator, outputOffset + 10) = MessagePackCode.Float32;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 11), shuffled.GetElement(0));
                        Unsafe.Add(ref outputIterator, outputOffset + 15) = MessagePackCode.Float32;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 16), shuffled.GetElement(0));
                        Unsafe.Add(ref outputIterator, outputOffset + 20) = MessagePackCode.Float32;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 21), shuffled.GetElement(0));
                        Unsafe.Add(ref outputIterator, outputOffset + 25) = MessagePackCode.Float32;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 26), shuffled.GetElement(0));
                        Unsafe.Add(ref outputIterator, outputOffset + 30) = MessagePackCode.Float32;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 31), shuffled.GetElement(0));
                        Unsafe.Add(ref outputIterator, outputOffset + 35) = MessagePackCode.Float32;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 35), shuffled.GetElement(0));
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
                        // Reorder Little Endian bytes to Big Endian.
                        var shuffled = Vector128.Shuffle(Vector128.LoadUnsafe(ref inputIterator, inputOffset).AsByte(), Vector128.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)).AsUInt32();

                        // Write 4 Big-Endian floats.
                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Float32;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.GetElement(0));
                        Unsafe.Add(ref outputIterator, outputOffset + 5) = MessagePackCode.Float32;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 6), shuffled.GetElement(0));
                        Unsafe.Add(ref outputIterator, outputOffset + 10) = MessagePackCode.Float32;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 11), shuffled.GetElement(0));
                        Unsafe.Add(ref outputIterator, outputOffset + 15) = MessagePackCode.Float32;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 16), shuffled.GetElement(0));
                    }

                    writer.Advance(outputLength);
                    length -= (int)alignedInputLength;
                    inputIterator = ref Unsafe.Add(ref inputIterator, alignedInputLength);
                }
            }
        }
#endif

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
                Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Float32;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), BinaryPrimitives.ReverseEndianness(Unsafe.Add(ref inputIterator, index)));
            }

            writer.Advance(outputLength);
            length -= inputLength;
            inputIterator = ref Unsafe.Add(ref inputIterator, inputLength);
        }
    }

    /// <summary>Unsafe serialize method without parameter checks nor cancellation.</summary>
    /// <param name="writer">MessagePackWriter.</param>
    /// <param name="input">Must not be null reference.</param>
    /// <param name="length">Must be greater than 0.</param>
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

#if NET8_0_OR_GREATER
        unchecked
        {
            if (Vector256.IsHardwareAccelerated)
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
                    for (nuint inputOffset = 0, outputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector256<ulong>.Count, outputOffset += (nuint)Vector256<ulong>.Count * maxOutputElementSize)
                    {
                        // Reorder Little Endian bytes to Big Endian.
                        var shuffled = Vector256.Shuffle(Vector256.LoadUnsafe(ref inputIterator, inputOffset).AsByte(), Vector256.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24)).AsUInt64();

                        // Write 4 Big-Endian doubles.
                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Float64;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.GetElement(0));
                        Unsafe.Add(ref outputIterator, outputOffset + 9) = MessagePackCode.Float64;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 10), shuffled.GetElement(1));
                        Unsafe.Add(ref outputIterator, outputOffset + 18) = MessagePackCode.Float64;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 19), shuffled.GetElement(2));
                        Unsafe.Add(ref outputIterator, outputOffset + 27) = MessagePackCode.Float64;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 28), shuffled.GetElement(3));
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

                    var outputLength = (int)alignedInputLength * maxOutputElementSize;
                    var destination = writer.GetSpan(outputLength);
                    ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                    for (nuint inputOffset = 0, outputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<ulong>.Count, outputOffset += (nuint)Vector128<ulong>.Count * maxOutputElementSize)
                    {
                        // Reorder Little Endian bytes to Big Endian.
                        var shuffled = Vector128.Shuffle(Vector128.LoadUnsafe(ref inputIterator, inputOffset).AsByte(), Vector128.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)).AsUInt64();

                        // Write 2 Big-Endian doubles.
                        Unsafe.Add(ref outputIterator, outputOffset) = MessagePackCode.Float64;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 1), shuffled.GetElement(0));
                        Unsafe.Add(ref outputIterator, outputOffset + 9) = MessagePackCode.Float64;
                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputIterator, outputOffset + 10), shuffled.GetElement(1));
                    }

                    writer.Advance(outputLength);
                    length -= (int)alignedInputLength;
                    inputIterator = ref Unsafe.Add(ref inputIterator, alignedInputLength);
                }
            }
        }
#endif

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
