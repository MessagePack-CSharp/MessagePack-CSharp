// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

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

    public sealed partial class SingleArrayFormatter : IMessagePackFormatter<float[]?>
    {
        public unsafe void Serialize(ref MessagePackWriter writer, float[]? value, MessagePackSerializerOptions options)
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

            // output byte[] length can be calculated from input float[] length.
            var outputLength = inputLength * 5;
            var destination = writer.GetSpan(outputLength);
            fixed (byte* pDestination = &destination[0])
            {
                var outputIterator = pDestination;
                fixed (float* pSource = &value[0])
                {
                    var inputEnd = pSource + inputLength;
                    var inputIterator = (uint*)pSource;

                    if (Sse42.IsSupported)
                    {
                        if (inputLength < 6)
                        {
                            goto ProcessEach;
                        }

                        // Process 3 floats at once.
                        // From 12 bytes to 15 bytes.
                        var vectorConstant = Vector128.Create(MessagePackCode.Float32, 0, 0, 0, 0, MessagePackCode.Float32, 0, 0, 0, 0, MessagePackCode.Float32, 0, 0, 0, 0, 0);
                        var vectorShuffle = Vector128.Create(0x80, 3, 2, 1, 0, 0x80, 7, 6, 5, 4, 0x80, 11, 10, 9, 8, 0x80);
                        var vectorLoopLength = ((inputLength / 3) - 1) * 3;
                        for (var vectorizedEnd = inputIterator + vectorLoopLength; inputIterator != vectorizedEnd; inputIterator += 3, outputIterator += 15)
                        {
                            // new float[] { 1.0, -2.0, 3.5, } is byte[12] { 00, 00, 80, 3f, 00, 00, 00, c0, 00, 00, 60, 40 } in binary expression;
                            var current = Sse2.LoadVector128((byte*)inputIterator);

                            // Output binary should be byte[15] { ca, 3f, 80, 00, 00, ca, c0, 00, 00, 00, ca, 40, 60, 00, 00 };
                            Sse2.Store(outputIterator, Sse2.Or(Ssse3.Shuffle(current, vectorShuffle), vectorConstant));
                        }
                    }

                ProcessEach:
                    while (inputIterator != inputEnd)
                    {
                        // Encode float as Big Endian
                        *outputIterator++ = MessagePackCode.Float32;
                        var current = *inputIterator++;
                        *outputIterator++ = (byte)(current >> 24);
                        *outputIterator++ = (byte)(current >> 16);
                        *outputIterator++ = (byte)(current >> 8);
                        *outputIterator++ = (byte)current;
                    }
                }
            }

            writer.Advance(outputLength);
        }
    }

    public sealed partial class DoubleArrayFormatter : IMessagePackFormatter<double[]?>
    {
        public unsafe void Serialize(ref MessagePackWriter writer, double[]? value, MessagePackSerializerOptions options)
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

            var outputLength = inputLength * 9;
            var destination = writer.GetSpan(outputLength);
            fixed (byte* pDestination = &destination[0])
            {
                var outputIterator = pDestination;
                fixed (double* pSource = &value[0])
                {
                    var inputEnd = pSource + inputLength;
                    var inputIterator = (ulong*)pSource;

                    if (Avx2.IsSupported)
                    {
                        const int ShiftCount = 2;
                        const int Stride = 1 << ShiftCount;

                        if (inputLength < Stride << 1)
                        {
                            goto ProcessEach;
                        }

                        var vectorShuffle = Vector256.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8);
                        for (var vectorizedEnd = inputIterator + ((inputLength >> ShiftCount) << ShiftCount); inputIterator != vectorizedEnd; inputIterator += Stride)
                        {
                            // Fetch 4 doubles.
                            var current = Avx.LoadVector256((byte*)inputIterator);

                            // Reorder Little Endian bytes to Big Endian.
                            var answer = Avx2.Shuffle(current, vectorShuffle).AsUInt64();

                            // Write 4 Big-Endian doubles.
                            *outputIterator++ = MessagePackCode.Float64;
                            *(ulong*)outputIterator = answer.GetElement(0);
                            outputIterator += 8;
                            *outputIterator++ = MessagePackCode.Float64;
                            *(ulong*)outputIterator = answer.GetElement(1);
                            outputIterator += 8;
                            *outputIterator++ = MessagePackCode.Float64;
                            *(ulong*)outputIterator = answer.GetElement(2);
                            outputIterator += 8;
                            *outputIterator++ = MessagePackCode.Float64;
                            *(ulong*)outputIterator = answer.GetElement(3);
                            outputIterator += 8;
                        }
                    }
                    else if (Ssse3.IsSupported)
                    {
                        const int ShiftCount = 1;
                        const int Stride = 1 << ShiftCount;

                        if (inputLength < Stride << 1)
                        {
                            goto ProcessEach;
                        }

                        var vectorShuffle = Vector128.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8);
                        for (var vectorizedEnd = inputIterator + ((inputLength >> ShiftCount) << ShiftCount); inputIterator != vectorizedEnd; inputIterator += Stride)
                        {
                            var current = Sse2.LoadVector128((byte*)inputIterator);
                            var answer = Ssse3.Shuffle(current, vectorShuffle).AsUInt64();
                            *outputIterator++ = MessagePackCode.Float64;
                            *(ulong*)outputIterator = answer.GetElement(0);
                            outputIterator += 8;
                            *outputIterator++ = MessagePackCode.Float64;
                            *(ulong*)outputIterator = answer.GetElement(1);
                            outputIterator += 8;
                        }
                    }

                ProcessEach:
                    while (inputIterator != inputEnd)
                    {
                        *outputIterator++ = MessagePackCode.Float64;
                        var current = *inputIterator++;
                        *outputIterator++ = (byte)(current >> 56);
                        *outputIterator++ = (byte)(current >> 48);
                        *outputIterator++ = (byte)(current >> 40);
                        *outputIterator++ = (byte)(current >> 32);
                        *outputIterator++ = (byte)(current >> 24);
                        *outputIterator++ = (byte)(current >> 16);
                        *outputIterator++ = (byte)(current >> 8);
                        *outputIterator++ = (byte)current;
                    }
                }
            }

            writer.Advance(outputLength);
        }
    }

    public sealed unsafe partial class BooleanArrayFormatter : IMessagePackFormatter<bool[]?>
    {
        public void Serialize(ref MessagePackWriter writer, bool[]? value, MessagePackSerializerOptions options)
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

            var outputLength = inputLength;
            fixed (bool* pSource = &value[0])
            {
                var inputEnd = pSource + inputLength;
                var inputIterator = pSource;
                var destination = writer.GetSpan(inputLength);
                fixed (byte* pDestination = &destination[0])
                {
                    var outputIterator = pDestination;

                    if (Avx2.IsSupported)
                    {
                        const int ShiftCount = 5;
                        const int Stride = 1 << ShiftCount;
                        if (inputLength < Stride << 1)
                        {
                            goto ProcessEach;
                        }

                        {
                            // make output span align 32
                            var offset = UnsafeMemoryAlignmentUtility.CalculateDifferenceAlign32(outputIterator);
                            inputLength -= offset;
                            var offsetEnd = inputIterator + offset;
                            while (inputIterator != offsetEnd)
                            {
                                *outputIterator++ = *inputIterator++ ? MessagePackCode.True : MessagePackCode.False;
                            }
                        }

                        var vectorTrue = Vector256.Create(MessagePackCode.True).AsSByte();
                        var vectorLoopLength = (inputLength >> ShiftCount) << ShiftCount;
                        for (var vectorizedEnd = inputIterator + vectorLoopLength; inputIterator != vectorizedEnd; inputIterator += Stride, outputIterator += Stride)
                        {
                            // Load 32 bool values.
                            var current = Avx.LoadVector256((sbyte*)inputIterator);

                            // A value of false for the type bool is 0 for the sbyte representation.
                            var isTrue = Avx2.CompareEqual(current, Vector256<sbyte>.Zero);

                            // A value of true in the SIMD context is -1 for the sbyte representation.
                            // True is 0xc3 as MessagePackCode and false is 0xc2.
                            // Reinterpreted as sbyte values, they are -61 and -62, respectively.
                            // For each of the 32 true Vectors, we can add -1 to the false ones to get the answer.
                            var answer = Avx2.Add(vectorTrue, isTrue);
                            Avx.Store((sbyte*)outputIterator, answer);
                        }
                    }
                    else if (Sse2.IsSupported)
                    {
                        // for older x86 cpu
                        const int ShiftCount = 4;
                        const int Stride = 1 << ShiftCount;
                        if (inputLength < Stride << 1)
                        {
                            goto ProcessEach;
                        }

                        {
                            // make output span align 16
                            var offset = UnsafeMemoryAlignmentUtility.CalculateDifferenceAlign16(outputIterator);
                            inputLength -= offset;
                            var offsetEnd = inputIterator + offset;
                            while (inputIterator != offsetEnd)
                            {
                                *outputIterator++ = *inputIterator++ ? MessagePackCode.True : MessagePackCode.False;
                            }
                        }

                        var vectorTrue = Vector128.Create(MessagePackCode.True).AsSByte();
                        var vectorLoopLength = (inputLength >> ShiftCount) << ShiftCount;
                        for (var vectorizedEnd = inputIterator + vectorLoopLength; inputIterator != vectorizedEnd; inputIterator += Stride, outputIterator += Stride)
                        {
                            // Load 16 bool values.
                            var current = Sse2.LoadVector128((sbyte*)inputIterator);

                            // A value of false for the type bool is 0 for the sbyte representation.
                            var isTrue = Sse2.CompareEqual(current, Vector128<sbyte>.Zero);

                            // A value of true in the SIMD context is -1 for the sbyte representation.
                            // True is 0xc3 as MessagePackCode and false is 0xc2.
                            // Reinterpreted as sbyte values, they are -61 and -62, respectively.
                            // For each of the 16 true Vectors, we can add -1 to the false ones to get the answer.
                            var answer = Sse2.Add(vectorTrue, isTrue);
                            Sse2.Store((sbyte*)outputIterator, answer);
                        }
                    }

                ProcessEach:
                    while (inputIterator != inputEnd)
                    {
                        *outputIterator++ = *inputIterator++ ? MessagePackCode.True : MessagePackCode.False;
                    }
                }

                writer.Advance(outputLength);
            }
        }

        public bool[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }

            var len = reader.ReadArrayHeader();
            if (len == 0)
            {
                return Array.Empty<bool>();
            }

            var rawSequence = reader.ReadRaw(len);
            var array = new bool[len];
            fixed (bool* destination = &array[0])
            {
                var outputIterator = destination;
                foreach (var memory in rawSequence)
                {
                    if (memory.IsEmpty)
                    {
                        continue;
                    }

                    fixed (byte* source = &memory.Span[0])
                    {
                        var inputIterator = source;
                        var inputLength = memory.Length;
                        var inputEnd = inputIterator + inputLength;

                        if (Avx2.IsSupported)
                        {
                            const int ShiftCount = 5;
                            const int Stride = 1 << ShiftCount;
                            if (inputLength < Stride << 1)
                            {
                                goto ProcessEach;
                            }

                            var vectorLoopLength = (inputLength >> ShiftCount) << ShiftCount;
                            var vectorFalse = Vector256.Create(MessagePackCode.False).AsSByte();
                            var vectorTrue = Vector256.Create(MessagePackCode.True).AsSByte();
                            var vectorOne = Vector256.Create((sbyte)1);
                            for (var vectorizedEnd = inputIterator + vectorLoopLength; inputIterator != vectorizedEnd; inputIterator += Stride, outputIterator += Stride)
                            {
                                var current = Avx.LoadVector256((sbyte*)inputIterator);
                                var isFalse = Avx2.CompareEqual(current, vectorFalse);
                                var isTrue = Avx2.CompareEqual(current, vectorTrue);
                                if (Avx2.MoveMask(Avx2.Xor(isFalse, isTrue)) != -1)
                                {
                                    throw new MessagePackSerializationException("Unexpected msgpack code encountered.");
                                }

                                var answer = Avx2.And(isTrue, vectorOne);
                                Avx.Store((byte*)outputIterator, answer.AsByte());
                            }
                        }
                        else if (Sse2.IsSupported)
                        {
                            // for older x86 cpu
                            const int ShiftCount = 4;
                            const int Stride = 1 << ShiftCount;
                            if (inputLength < Stride << 1)
                            {
                                goto ProcessEach;
                            }

                            var vectorLoopLength = (inputLength >> ShiftCount) << ShiftCount;
                            var vectorFalse = Vector128.Create(MessagePackCode.False).AsSByte();
                            var vectorTrue = Vector128.Create(MessagePackCode.True).AsSByte();
                            var vectorOne = Vector128.Create((sbyte)1);
                            for (var vectorizedEnd = inputIterator + vectorLoopLength; inputIterator != vectorizedEnd; inputIterator += Stride, outputIterator += Stride)
                            {
                                var current = Sse2.LoadVector128((sbyte*)inputIterator);
                                var isFalse = Sse2.CompareEqual(current, vectorFalse);
                                var isTrue = Sse2.CompareEqual(current, vectorTrue);
                                if (Sse2.MoveMask(Sse2.Xor(isFalse, isTrue)) != 0xFFFF)
                                {
                                    throw new MessagePackSerializationException("Unexpected msgpack code encountered.");
                                }

                                var answer = Sse2.And(isTrue, vectorOne);
                                Sse2.Store((byte*)outputIterator, answer.AsByte());
                            }
                        }

                    ProcessEach:
                        while (inputIterator != inputEnd)
                        {
                            switch (*inputIterator++)
                            {
                                case MessagePackCode.False:
                                    *outputIterator++ = false;
                                    break;
                                case MessagePackCode.True:
                                    *outputIterator++ = true;
                                    break;
                                default:
                                    throw new MessagePackSerializationException("Unexpected msgpack code encountered.");
                            }
                        }
                    }
                }
            }

            return array;
        }
    }
}
