// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MessagePack.Formatters;

public sealed partial class Int16ArrayFormatter : IMessagePackFormatter<short[]?>
{
    public unsafe void Serialize(ref MessagePackWriter writer, short[]? value, MessagePackSerializerOptions options)
    {
        if (value == null)
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

        fixed (short* pSource = &value[0])
        {
            var inputEnd = pSource + inputLength;
            var inputIterator = pSource;

            if (Sse41.IsSupported)
            {
                const int ShiftCount = 3;
                const int Stride = 1 << ShiftCount;
                if (inputLength < Stride << 1)
                {
                    goto ProcessEach;
                }

                {
                    // Make InputIterator Aligned
                    var offset = UnsafeMemoryAlignmentUtility.CalculateDifferenceAlign16(inputIterator);

                    // When offset is times of 2, you can adjust memory address.
                    if ((offset & 1) == 0)
                    {
                        offset >>= 1;
                        inputLength -= offset;
                        var offsetEnd = inputIterator + offset;
                        while (inputIterator != offsetEnd)
                        {
                            writer.Write(*inputIterator++);
                        }
                    }
                }

                fixed (byte* tablePointer = &ShuffleAndMaskTable[0])
                {
                    var countPointer = (int*)(tablePointer + CountTableOffset);
                    fixed (byte* maskTablePointer = &SingleInstructionMultipleDataPrimitiveArrayFormatterHelper.StoreMaskTable[0])
                    {
                        var vectorSByteMinNeg1 = Vector128.Create((short)(sbyte.MinValue - 1));
                        var vectorMinFixNeg1 = Vector128.Create((short)(MessagePackRange.MinFixNegativeInt - 1));
                        var vectorSByteMax = Vector128.Create((short)sbyte.MaxValue);
                        var vectorByteMaxValue = Vector128.Create((short)byte.MaxValue);
                        var vectorM1M5M25M125 = Vector128.Create(-1, -5, -25, -125, -1, -5, -25, -125);
                        var vector02468101214 = Vector128.Create(0, 2, 4, 6, 8, 10, 12, 14, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
                        var vectorLoopLength = (inputLength >> ShiftCount) << ShiftCount;
                        for (var vectorizedEnd = inputIterator + vectorLoopLength; inputIterator != vectorizedEnd; inputIterator += Stride)
                        {
                            var current = Sse2.LoadVector128(inputIterator);

                            var isGreaterThanMinFixNeg1 = Sse2.CompareGreaterThan(current, vectorMinFixNeg1);
                            var isGreaterThanSByteMax = Sse2.CompareGreaterThan(current, vectorSByteMax);

                            if (Sse2.MoveMask(Sse2.AndNot(isGreaterThanSByteMax, isGreaterThanMinFixNeg1).AsByte()) == 0xFFFF)
                            {
                                var span = writer.GetSpan(Stride);
                                var answer = Ssse3.Shuffle(current.AsByte(), vector02468101214).AsUInt64();
                                Unsafe.As<byte, ulong>(ref span[0]) = answer.GetElement(0);
                                writer.Advance(Stride);
                                continue;
                            }

                            var countVector = Sse2.Add(isGreaterThanSByteMax, isGreaterThanMinFixNeg1);
                            var isGreaterThanSByteMinNeg1 = Sse2.CompareGreaterThan(current, vectorSByteMinNeg1);
                            countVector = Sse2.Add(countVector, isGreaterThanSByteMinNeg1);
                            var isGreaterThanByteMax = Sse2.CompareGreaterThan(current, vectorByteMaxValue);
                            countVector = Sse2.Add(countVector, isGreaterThanByteMax);

                            var indexVector = Sse2.MultiplyAddAdjacent(countVector, vectorM1M5M25M125);
                            indexVector = Ssse3.HorizontalAdd(indexVector, indexVector);

                            var index0 = indexVector.GetElement(0);
                            var index1 = indexVector.GetElement(1);
                            var count0 = countPointer[index0];
                            var count1 = countPointer[index1];
                            var countTotal = count0 + count1;
                            var destination = writer.GetSpan(countTotal);
                            fixed (byte* pDestination = &destination[0])
                            {
                                var tmpDestination = pDestination;

                                var item0 = tablePointer + (index0 << 5);
                                var shuffle0 = Sse2.LoadVector128(item0);
                                var shuffled0 = Ssse3.Shuffle(current.AsByte(), shuffle0);
                                var constant0 = Sse2.LoadVector128(item0 + 16);
                                var answer0 = Sse2.Or(shuffled0, constant0);
                                Sse2.MaskMove(answer0, Sse2.LoadVector128(maskTablePointer + (count0 << 4)), tmpDestination);
                                tmpDestination += count0;

                                var item1 = tablePointer + (index1 << 5);
                                var shuffle1 = Sse2.LoadVector128(item1);
                                current = Sse2.ShiftRightLogical128BitLane(current, 8);
                                var shuffled1 = Ssse3.Shuffle(current.AsByte(), shuffle1);
                                var constant1 = Sse2.LoadVector128(item1 + 16);
                                var answer1 = Sse2.Or(shuffled1, constant1);
                                Sse2.MaskMove(answer1, Sse2.LoadVector128(maskTablePointer + (count1 << 4)), tmpDestination);
                            }

                            writer.Advance(countTotal);
                        }
                    }
                }
            }

ProcessEach:
            while (inputIterator != inputEnd)
            {
                writer.Write(*inputIterator++);
            }
        }
    }
}
