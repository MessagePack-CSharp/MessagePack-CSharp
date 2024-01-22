// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MessagePack.Formatters
{
    public sealed partial class SByteArrayFormatter : IMessagePackFormatter<sbyte[]?>
    {
        public unsafe void Serialize(ref MessagePackWriter writer, sbyte[]? value, MessagePackSerializerOptions options)
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

            fixed (sbyte* pSource = &value[0])
            {
                var inputEnd = pSource + inputLength;
                var inputIterator = pSource;

                if (Popcnt.IsSupported)
                {
                    const int ShiftCount = 4;
                    const int Stride = 1 << ShiftCount;

                    // We enter the SIMD mode when there are more than the Stride after alignment adjustment.
                    if (inputLength < Stride << 1)
                    {
                        goto ProcessEach;
                    }

                    {
                        // Make InputIterator Aligned
                        var offset = UnsafeMemoryAlignmentUtility.CalculateDifferenceAlign16(inputIterator);
                        inputLength -= offset;
                        var offsetEnd = inputIterator + offset;
                        while (inputIterator != offsetEnd)
                        {
                            writer.Write(*inputIterator++);
                        }
                    }

                    fixed (byte* tablePointer = &ShuffleAndMaskTable[0])
                    {
                        fixed (byte* maskTablePointer = &SingleInstructionMultipleDataPrimitiveArrayFormatterHelper.StoreMaskTable[0])
                        {
                            var vectorMinFixNegInt = Vector128.Create((sbyte)MessagePackRange.MinFixNegativeInt);
                            var vectorMessagePackCodeInt8 = Vector128.Create(MessagePackCode.Int8);
                            for (var vectorizedEnd = inputIterator + ((inputLength >> ShiftCount) << ShiftCount); inputIterator != vectorizedEnd; inputIterator += Stride)
                            {
                                var current = Sse2.LoadVector128(inputIterator);
                                var index = unchecked((uint)Sse2.MoveMask(Sse2.CompareGreaterThan(vectorMinFixNegInt, current)));

                                if (index == 0)
                                {
                                    // When all 32 input values are in the FixNum range.
                                    var span = writer.GetSpan(Stride);
                                    Sse2.Store((sbyte*)Unsafe.AsPointer(ref span[0]), current);

                                    writer.Advance(Stride);
                                    continue;
                                }

                                unchecked
                                {
                                    var index0 = (byte)index;
                                    var index1 = (byte)(index >> 8);
                                    var count0 = (int)(Popcnt.PopCount(index0) + 8);
                                    var count1 = (int)(Popcnt.PopCount(index1) + 8);
                                    var countTotal = count0 + count1;
                                    var destination = writer.GetSpan(countTotal);
                                    fixed (byte* pDestination = &destination[0])
                                    {
                                        var tempDestination = pDestination;
                                        var shuffle0 = Sse2.LoadVector128(tablePointer + (index0 << 4));
                                        var shuffled0 = Ssse3.Shuffle(current.AsByte(), shuffle0);
                                        var answer0 = Sse41.BlendVariable(shuffled0, vectorMessagePackCodeInt8, shuffle0);
                                        Sse2.MaskMove(answer0, Sse2.LoadVector128(maskTablePointer + (count0 << 4)), tempDestination);
                                        tempDestination += count0;

                                        var shuffle1 = Sse2.LoadVector128(tablePointer + (index1 << 4));
                                        var shift1 = Sse2.ShiftRightLogical128BitLane(current.AsByte(), 8);
                                        var shuffled1 = Ssse3.Shuffle(shift1, shuffle1);
                                        var answer1 = Sse41.BlendVariable(shuffled1, vectorMessagePackCodeInt8, shuffle1);
                                        Sse2.MaskMove(answer1, Sse2.LoadVector128(maskTablePointer + (count1 << 4)), tempDestination);
                                    }

                                    writer.Advance(countTotal);
                                }
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
