// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MessagePack.Formatters
{
    public sealed partial class Int32ArrayFormatter : IMessagePackFormatter<int[]?>
    {
        public unsafe void Serialize(ref MessagePackWriter writer, int[]? value, MessagePackSerializerOptions options)
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

            fixed (int* pSource = &value[0])
            {
                var inputEnd = pSource + inputLength;
                var inputIterator = pSource;

                if (Sse41.IsSupported)
                {
                    const int ShiftCount = 2;
                    const int Stride = 1 << ShiftCount;

                    if (inputLength < Stride << 1)
                    {
                        goto ProcessEach;
                    }

                    {
                        // Make InputIterator Aligned
                        var offset = UnsafeMemoryAlignmentUtility.CalculateDifferenceAlign16(inputIterator);

                        // When offset is times of 4, you can adjust memory address.
                        if ((offset & 3) == 0)
                        {
                            offset >>= 2;
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
                            var vectorShortMinValueM1 = Vector128.Create(short.MinValue - 1);
                            var vectorSByteMinValueM1 = Vector128.Create(sbyte.MinValue - 1);
                            var vectorMinFixNegIntM1 = Vector128.Create(MessagePackRange.MinFixNegativeInt - 1);
                            var vectorSByteMaxValue = Vector128.Create((int)sbyte.MaxValue);
                            var vectorByteMaxValue = Vector128.Create((int)byte.MaxValue);
                            var vectorUShortMaxValue = Vector128.Create((int)ushort.MaxValue);
                            var vectorM1M7 = Vector128.Create(-1, -7, -1, -7);
                            var vectorIn1Range = Vector128.Create(0, 4, 8, 12, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
                            for (var vectorizedEnd = inputIterator + ((inputLength >> ShiftCount) << ShiftCount); inputIterator != vectorizedEnd; inputIterator += Stride)
                            {
                                var current = Sse2.LoadVector128(inputIterator);
                                var isGreaterThanMinFixNegIntM1 = Sse2.CompareGreaterThan(current, vectorMinFixNegIntM1);
                                var isGreaterThanSByteMaxValue = Sse2.CompareGreaterThan(current, vectorSByteMaxValue);

                                if (Sse2.MoveMask(Sse2.AndNot(isGreaterThanSByteMaxValue, isGreaterThanMinFixNegIntM1).AsByte()) == 0xFFFF)
                                {
                                    var answer = Ssse3.Shuffle(current.AsByte(), vectorIn1Range).AsUInt32();
                                    var span = writer.GetSpan(Stride);
                                    Unsafe.As<byte, uint>(ref span[0]) = answer.GetElement(0);
                                    writer.Advance(Stride);
                                    continue;
                                }

                                var indexVector = Sse2.Add(isGreaterThanSByteMaxValue, isGreaterThanMinFixNegIntM1);
                                indexVector = Sse2.Add(indexVector, Sse2.CompareGreaterThan(current, vectorUShortMaxValue));
                                indexVector = Sse2.Add(indexVector, Sse2.CompareGreaterThan(current, vectorByteMaxValue));
                                indexVector = Sse2.Add(indexVector, Sse2.CompareGreaterThan(current, vectorShortMinValueM1));
                                indexVector = Sse2.Add(indexVector, Sse2.CompareGreaterThan(current, vectorSByteMinValueM1));
                                indexVector = Sse41.MultiplyLow(indexVector, vectorM1M7);
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
                                    Sse2.MaskMove(answer0, Sse2.LoadVector128(maskTablePointer + (count0 << 4)), pDestination);
                                    tmpDestination += count0;

                                    var shift1 = Sse2.ShiftRightLogical128BitLane(current, 8).AsByte();
                                    var item1 = tablePointer + (index1 << 5);
                                    var shuffle1 = Sse2.LoadVector128(item1);
                                    var shuffled1 = Ssse3.Shuffle(shift1, shuffle1);
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
}
