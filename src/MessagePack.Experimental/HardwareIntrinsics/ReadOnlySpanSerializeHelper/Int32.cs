// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MessagePack.Formatters;

internal static partial class RefSerializeHelper
{
    internal static void Serialize(ref MessagePackWriter writer, ref int input, int length)
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

        ref var inputIterator = ref input;
        const int maxOutputElementSize = sizeof(int) + 1;
        const int maxInputSize = int.MaxValue / maxOutputElementSize;
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
                    writer.CancellationToken.ThrowIfCancellationRequested();

                    var loaded = Vector128.LoadUnsafe(ref inputIterator, inputOffset);

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
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled);
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
                                    WriteInt32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.GetElement(i));
                                    outputOffset += 5;
                                    break;
                                case 1:
                                    WriteInt16(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.AsInt16().GetElement((i * 2) + 1));
                                    outputOffset += 3;
                                    break;
                                case 2:
                                    WriteInt8(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.AsSByte().GetElement((i * 4) + 3));
                                    outputOffset += 2;
                                    break;
                                case 3:
                                    Unsafe.AddByteOffset(ref outputIterator, outputOffset) = shuffled.AsByte().GetElement((i * 4) + 3);
                                    outputOffset += 1;
                                    break;
                                case 4:
                                    WriteUInt8(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.AsByte().GetElement((i * 4) + 3));
                                    outputOffset += 2;
                                    break;
                                case 5:
                                    WriteUInt16(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.AsUInt16().GetElement((i * 2) + 1));
                                    outputOffset += 3;
                                    break;
                                default:
                                    WriteUInt32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.AsUInt32().GetElement(i));
                                    outputOffset += 5;
                                    break;
                            }
                        }
                    }
                }

                writer.Advance((int)outputOffset);
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

            var destination = writer.GetSpan(inputLength * maxOutputElementSize);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            nuint outputOffset = 0;
            for (nuint index = 0; index < (nuint)inputLength; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                outputOffset += ReverseWriteUnknown(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), Unsafe.Add(ref inputIterator, index));
            }

            writer.Advance((int)outputOffset);
            length -= inputLength;
            inputIterator = ref Unsafe.Add(ref inputIterator, inputLength);
        }
    }
}
