// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MessagePack.Formatters;

internal static partial class RefSerializeHelper
{
    internal static void Serialize(ref MessagePackWriter writer, ref ulong input, int length)
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
        const int maxInputSize = int.MaxValue / (sizeof(ulong) + 1);
        if (Vector128.IsHardwareAccelerated)
        {
            const int mask = ~1;
            for (var alignedInputLength = (nuint)(length & mask); alignedInputLength > 0; alignedInputLength = (nuint)(length & mask))
            {
                if (alignedInputLength >= maxInputSize)
                {
                    alignedInputLength = maxInputSize & mask;
                }

                var destination = writer.GetSpan((int)alignedInputLength * (sizeof(ulong) + 1));
                ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                nuint outputOffset = 0;
                for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<ulong>.Count)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();

                    var loaded = Vector128.LoadUnsafe(ref inputIterator, inputOffset).AsInt64();

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
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled);
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
                                    Unsafe.AddByteOffset(ref outputIterator, outputOffset) = shuffled.GetElement((i * 8) + 7);
                                    outputOffset += 1;
                                    break;
                                case 1:
                                    WriteUInt8(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.GetElement((i * 8) + 7));
                                    outputOffset += 2;
                                    break;
                                case 2:
                                    WriteUInt16(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.AsUInt16().GetElement((i * 4) + 3));
                                    outputOffset += 3;
                                    break;
                                case 3:
                                    WriteUInt32(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.AsUInt32().GetElement((i * 2) + 1));
                                    outputOffset += 5;
                                    break;
                                default:
                                    WriteUInt64(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.AsUInt64().GetElement(i));
                                    outputOffset += 9;
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

            var destination = writer.GetSpan(inputLength * (sizeof(long) + 1));
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            nuint outputOffset = 0;
            for (nuint index = 0; index < (nuint)inputLength; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                outputOffset += ReverseWriteUnknown(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), Unsafe.Add(ref inputIterator, index));
            }

            length -= inputLength;
            writer.Advance((int)outputOffset);
        }
    }
}
