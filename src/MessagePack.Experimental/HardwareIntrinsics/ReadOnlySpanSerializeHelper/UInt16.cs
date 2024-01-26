// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MessagePack.Formatters;

internal static partial class RefSerializeHelper
{
    internal static void Serialize(ref MessagePackWriter writer, ref ushort input, int length)
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
        const int maxInputSize = int.MaxValue / (sizeof(ushort) + 1);
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
                    writer.CancellationToken.ThrowIfCancellationRequested();

                    var loaded = Vector128.LoadUnsafe(ref inputIterator, inputOffset).AsInt16();

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
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled);
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
                                    Unsafe.AddByteOffset(ref outputIterator, outputOffset) = shuffled.GetElement((i * 2) + 1);
                                    outputOffset += 1;
                                    break;
                                case 1:
                                    WriteUInt8(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.GetElement((i * 2) + 1));
                                    outputOffset += 2;
                                    break;
                                default:
                                    WriteUInt16(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), shuffled.AsUInt16().GetElement(i));
                                    outputOffset += 3;
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

            var destination = writer.GetSpan(inputLength * (sizeof(ushort) + 1));
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
