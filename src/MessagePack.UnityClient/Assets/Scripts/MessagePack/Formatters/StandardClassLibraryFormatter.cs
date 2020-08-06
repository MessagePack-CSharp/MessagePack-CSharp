// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MessagePack.Internal;

#if HARDWARE_INTRINSICS_X86
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    // NET40 -> BigInteger, Complex, Tuple

    // byte[] is special. represents bin type.
    public sealed class ByteArrayFormatter : IMessagePackFormatter<byte[]>
    {
        public static readonly ByteArrayFormatter Instance = new ByteArrayFormatter();

        private ByteArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, byte[] value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public byte[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadBytes()?.ToArray();
        }
    }

    public sealed class SByteArrayFormatter : IMessagePackFormatter<sbyte[]>
    {
        public static readonly SByteArrayFormatter Instance = new SByteArrayFormatter();

        private SByteArrayFormatter()
        {
        }

        public unsafe void Serialize(ref MessagePackWriter writer, sbyte[] value, MessagePackSerializerOptions options)
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

                #if HARDWARE_INTRINSICS_X86
                const int Stride = 32;
                // We enter the SIMD mode when there are more than the Stride after alignment adjustment.
                if (Avx2.IsSupported && inputLength >= Stride * 2)
                {
                    {
                        // Make InputIterator Aligned
                        var offset = UnsafeMemoryAlignmentUtility.CalculateDifferenceAlign32(inputIterator);
                        inputLength -= offset;
                        var offsetEnd = inputIterator + offset;
                        while (inputIterator != offsetEnd)
                        {
                            writer.Write(*inputIterator++);
                        }
                    }

                    ReadOnlySpan<byte> table = IntegerArrayFormatterHelper.SByteShuffleTable;
                    fixed (byte* tablePointer = table)
                    {
                        var vector256MinFixNegInt = Vector256.Create((sbyte)MessagePackRange.MinFixNegativeInt);
                        var vector128MessagePackCodeInt8 = Vector128.Create(MessagePackCode.Int8);
                        var vector128ByteMaxValue = Vector128.Create(byte.MaxValue);
                        for (var vectorizedEnd = inputIterator + ((inputLength >> 5) << 5); inputIterator != vectorizedEnd; inputIterator += Stride)
                        {
                            var current = Avx.LoadVector256(inputIterator);
                            var index = unchecked((uint)Avx2.MoveMask(Avx2.CompareGreaterThan(vector256MinFixNegInt, current)));

                            if (index == 0)
                            {
                                // When all 32 input values are in the single byte range.
                                var destination = writer.GetSpan(32);
                                fixed (byte* pDestination = &destination[0])
                                {
                                    Avx.Store(pDestination, current.AsByte());
                                }

                                writer.Advance(32);
                                continue;
                            }

                            unchecked
                            {
                                var index0 = (byte)index;
                                var index1 = (byte)(index >> 8);
                                var index2 = (byte)(index >> 16);
                                var index3 = (byte)(index >> 24);
                                var count0 = (int)(Popcnt.PopCount(index0) + 8);
                                var count1 = (int)(Popcnt.PopCount(index1) + 8);
                                var count2 = (int)(Popcnt.PopCount(index2) + 8);
                                var count3 = (int)(Popcnt.PopCount(index3) + 8);
                                var countTotal = count0 + count1 + count2 + count3;
                                var destination = writer.GetSpan(countTotal);
                                fixed (byte* pDestination = &destination[0])
                                {
                                    var tempDestination = pDestination;
                                    var current01 = Avx2.ExtractVector128(current, 0);
                                    var shuffle0 = Sse2.LoadVector128(tablePointer + (index0 << 4));
                                    var shuffled0 = Ssse3.Shuffle(current01.AsByte(), shuffle0);
                                    var answer0 = Sse41.BlendVariable(shuffled0, vector128MessagePackCodeInt8, shuffle0);
                                    var mask0 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count0));
                                    Sse2.MaskMove(answer0, mask0, tempDestination);
                                    tempDestination += count0;

                                    var shuffle1 = Sse2.LoadVector128(tablePointer + (index1 << 4));
                                    var shift1 = Sse2.ShiftRightLogical128BitLane(current01.AsByte(), 8);
                                    var shuffled1 = Ssse3.Shuffle(shift1, shuffle1);
                                    var answer1 = Sse41.BlendVariable(shuffled1, vector128MessagePackCodeInt8, shuffle1);
                                    var mask1 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count1));
                                    Sse2.MaskMove(answer1, mask1, tempDestination);
                                    tempDestination += count1;

                                    var current23 = Avx2.ExtractVector128(current, 1);
                                    var shuffle2 = Sse2.LoadVector128(tablePointer + (index2 << 4));
                                    var shuffled2 = Ssse3.Shuffle(current23.AsByte(), shuffle2);
                                    var answer2 = Sse41.BlendVariable(shuffled2, vector128MessagePackCodeInt8, shuffle2);
                                    var mask2 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count2));
                                    Sse2.MaskMove(answer2, mask2, tempDestination);
                                    tempDestination += count2;

                                    var shuffle3 = Sse2.LoadVector128(tablePointer + (index3 << 4));
                                    var shift3 = Sse2.ShiftRightLogical128BitLane(current23.AsByte(), 8);
                                    var shuffled3 = Ssse3.Shuffle(shift3, shuffle3);
                                    var answer3 = Sse41.BlendVariable(shuffled3, vector128MessagePackCodeInt8, shuffle3);
                                    var mask3 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count3));
                                    Sse2.MaskMove(answer3, mask3, tempDestination);
                                }

                                writer.Advance(countTotal);
                            }
                        }
                    }
                }
            #endif

                while (inputIterator != inputEnd)
                {
                    writer.Write(*inputIterator++);
                }
            }
        }

        public sbyte[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();

                if (len == 0)
                {
                    return Array.Empty<sbyte>();
                }

                var array = new sbyte[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadSByte();
                }

                return array;
            }
        }
    }

    public sealed class Int16ArrayFormatter : IMessagePackFormatter<short[]>
    {
        public static readonly Int16ArrayFormatter Instance = new Int16ArrayFormatter();

        private Int16ArrayFormatter()
        {
        }

        public unsafe void Serialize(ref MessagePackWriter writer, short[] value, MessagePackSerializerOptions options)
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

                #if HARDWARE_INTRINSICS_X86
                const int Stride = 16;
                if (Avx2.IsSupported && inputLength >= Stride)
                {
                    {
                        // Make InputIterator Aligned
                        var offset = UnsafeMemoryAlignmentUtility.CalculateDifferenceAlign32(inputIterator);
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

                    var table = IntegerArrayFormatterHelper.Int16ShuffleTable;
                    fixed (byte* tablePointer = &table[0])
                    {
                        var countPointer = (int*)(tablePointer + IntegerArrayFormatterHelper.Int16CountTableOffset);
                        var vector256SByteMinNeg1 = Vector256.Create((short)(sbyte.MinValue - 1));
                        var vector256MinFixNeg1 = Vector256.Create((short)(MessagePackRange.MinFixNegativeInt - 1));
                        var vector256SByteMax = Vector256.Create((short)sbyte.MaxValue);
                        var vector256ByteMaxValue = Vector256.Create((short)byte.MaxValue);
                        var vector256M1M5 = Vector256.Create(-1, -5, -1, -5, -1, -5, -1, -5, -1, -5, -1, -5, -1, -5, -1, -5);
                        var vector128ByteMaxValue = Vector128.Create(byte.MaxValue);
                        for (var vectorizedEnd = inputIterator + ((inputLength >> 4) << 4); inputIterator != vectorizedEnd; inputIterator += Stride)
                        {
                            var current = Avx.LoadVector256(inputIterator);

                            var countVector = Avx2.CompareGreaterThan(current, vector256SByteMinNeg1);
                            var isGreaterThanMinFixNeg1 = Avx2.CompareGreaterThan(current, vector256MinFixNeg1);
                            countVector = Avx2.Add(countVector, isGreaterThanMinFixNeg1);
                            var isGreaterThanSByteMax = Avx2.CompareGreaterThan(current, vector256SByteMax);
                            countVector = Avx2.Add(countVector, isGreaterThanSByteMax);
                            var isGreaterThanByteMax = Avx2.CompareGreaterThan(current, vector256ByteMaxValue);
                            countVector = Avx2.Add(countVector, isGreaterThanByteMax);

                            var indexVector = Avx2.MultiplyAddAdjacent(countVector, vector256M1M5);

                            var index0 = indexVector.GetElement(0);
                            var index1 = indexVector.GetElement(1);
                            var index2 = indexVector.GetElement(2);
                            var index3 = indexVector.GetElement(3);
                            var index4 = indexVector.GetElement(4);
                            var index5 = indexVector.GetElement(5);
                            var index6 = indexVector.GetElement(6);
                            var index7 = indexVector.GetElement(7);
                            var count0 = countPointer[index0];
                            var count1 = countPointer[index1];
                            var count2 = countPointer[index2];
                            var count3 = countPointer[index3];
                            var count4 = countPointer[index4];
                            var count5 = countPointer[index5];
                            var count6 = countPointer[index6];
                            var count7 = countPointer[index7];
                            var countTotal = count0 + count1 + count2 + count3 + count4 + count5 + count6 + count7;
                            var destination = writer.GetSpan(countTotal);
                            fixed (byte* pDestination = &destination[0])
                            {
                                var tmpDestination = pDestination;

                                var current03 = Avx2.ExtractVector128(current, 0).AsByte();

                                var item0 = Avx.LoadVector256(tablePointer + (index0 << 5));
                                var shuffle0 = Avx2.ExtractVector128(item0, 0);
                                var shuffled0 = Ssse3.Shuffle(current03, shuffle0);
                                var constant0 = Avx2.ExtractVector128(item0, 1);
                                var answer0 = Sse2.Or(shuffled0, constant0);
                                var maskMove0 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count0));
                                Sse2.MaskMove(answer0, maskMove0, tmpDestination);
                                tmpDestination += count0;

                                var item1 = Avx.LoadVector256(tablePointer + (index1 << 5));
                                var shuffle1 = Avx2.ExtractVector128(item1, 0);
                                current03 = Sse2.ShiftRightLogical128BitLane(current03, 4);
                                var shuffled1 = Ssse3.Shuffle(current03, shuffle1);
                                var constant1 = Avx2.ExtractVector128(item1, 1);
                                var answer1 = Sse2.Or(shuffled1, constant1);
                                var maskMove1 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count1));
                                Sse2.MaskMove(answer1, maskMove1, tmpDestination);
                                tmpDestination += count1;

                                var item2 = Avx.LoadVector256(tablePointer + (index2 << 5));
                                var shuffle2 = Avx2.ExtractVector128(item2, 0);
                                current03 = Sse2.ShiftRightLogical128BitLane(current03, 4);
                                var shuffled2 = Ssse3.Shuffle(current03, shuffle2);
                                var constant2 = Avx2.ExtractVector128(item2, 1);
                                var answer2 = Sse2.Or(shuffled2, constant2);
                                var maskMove2 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count2));
                                Sse2.MaskMove(answer2, maskMove2, tmpDestination);
                                tmpDestination += count2;

                                var item3 = Avx.LoadVector256(tablePointer + (index3 << 5));
                                var shuffle3 = Avx2.ExtractVector128(item3, 0);
                                current03 = Sse2.ShiftRightLogical128BitLane(current03, 4);
                                var shuffled3 = Ssse3.Shuffle(current03, shuffle3);
                                var constant3 = Avx2.ExtractVector128(item3, 1);
                                var answer3 = Sse2.Or(shuffled3, constant3);
                                var maskMove3 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count3));
                                Sse2.MaskMove(answer3, maskMove3, tmpDestination);
                                tmpDestination += count3;

                                var current47 = Avx2.ExtractVector128(current, 1).AsByte();

                                var item4 = Avx.LoadVector256(tablePointer + (index4 << 5));
                                var shuffle4 = Avx2.ExtractVector128(item4, 0);
                                var shuffled4 = Ssse3.Shuffle(current47, shuffle4);
                                var constant4 = Avx2.ExtractVector128(item4, 1);
                                var answer4 = Sse2.Or(shuffled4, constant4);
                                var maskMove4 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count4));
                                Sse2.MaskMove(answer4, maskMove4, tmpDestination);
                                tmpDestination += count4;

                                var item5 = Avx.LoadVector256(tablePointer + (index5 << 5));
                                var shuffle5 = Avx2.ExtractVector128(item5, 0);
                                current47 = Sse2.ShiftRightLogical128BitLane(current47, 4);
                                var shuffled5 = Ssse3.Shuffle(current47, shuffle5);
                                var constant5 = Avx2.ExtractVector128(item5, 1);
                                var answer5 = Sse2.Or(shuffled5, constant5);
                                var maskMove5 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count5));
                                Sse2.MaskMove(answer5, maskMove5, tmpDestination);
                                tmpDestination += count5;

                                var item6 = Avx.LoadVector256(tablePointer + (index6 << 5));
                                var shuffle6 = Avx2.ExtractVector128(item6, 0);
                                current47 = Sse2.ShiftRightLogical128BitLane(current47, 4);
                                var shuffled6 = Ssse3.Shuffle(current47, shuffle6);
                                var constant6 = Avx2.ExtractVector128(item6, 1);
                                var answer6 = Sse2.Or(shuffled6, constant6);
                                var maskMove6 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count6));
                                Sse2.MaskMove(answer6, maskMove6, tmpDestination);
                                tmpDestination += count6;

                                var item7 = Avx.LoadVector256(tablePointer + (index7 << 5));
                                var shuffle7 = Avx2.ExtractVector128(item7, 0);
                                current47 = Sse2.ShiftRightLogical128BitLane(current47, 4);
                                var shuffled7 = Ssse3.Shuffle(current47, shuffle7);
                                var constant7 = Avx2.ExtractVector128(item7, 1);
                                var answer7 = Sse2.Or(shuffled7, constant7);
                                var maskMove7 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count7));
                                Sse2.MaskMove(answer7, maskMove7, tmpDestination);
                            }

                            writer.Advance(countTotal);
                        }
                    }
                }
                #endif

                while (inputIterator != inputEnd)
                {
                    writer.Write(*inputIterator++);
                }
            }
        }

        public short[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();

                if (len == 0)
                {
                    return Array.Empty<short>();
                }

                var array = new short[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadInt16();
                }

                return array;
            }
        }
    }

    public sealed class Int32ArrayFormatter : IMessagePackFormatter<int[]>
    {
        public static readonly Int32ArrayFormatter Instance = new Int32ArrayFormatter();

        private Int32ArrayFormatter()
        {
        }

        public unsafe void Serialize(ref MessagePackWriter writer, int[] value, MessagePackSerializerOptions options)
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

                #if HARDWARE_INTRINSICS_X86
                // Inline everything for the sake of the optimization.
                if (Avx2.IsSupported && inputLength >= 16)
                {
                    {
                        // Make InputIterator Aligned
                        var offset = UnsafeMemoryAlignmentUtility.CalculateDifferenceAlign32(inputIterator);
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

                    var tableInt32 = IntegerArrayFormatterHelper.Int32ShuffleTable;
                    fixed (byte* tableInt32Pointer = &tableInt32[0])
                    {
                        var countInt32Pointer = (int*)(tableInt32Pointer + IntegerArrayFormatterHelper.Int32CountTableOffset);
                        var vector256ShortMinValueM1 = Vector256.Create(short.MinValue - 1);
                        var vector256SByteMinValueM1 = Vector256.Create(sbyte.MinValue - 1);
                        var vector256MinFixNegIntM1 = Vector256.Create(MessagePackRange.MinFixNegativeInt - 1);
                        var vector256SByteMaxValue = Vector256.Create((int)sbyte.MaxValue);
                        var vector256ByteMaxValue = Vector256.Create((int)byte.MaxValue);
                        var vector256UShortMaxValue = Vector256.Create((int)ushort.MaxValue);
                        var vector256M1M7 = Vector256.Create(-1, -7, -1, -7, -1, -7, -1, -7);
                        var vector128ByteMaxValue = Vector128.Create(byte.MaxValue);
                        var vector256In1Range = Vector256.Create(0, 4, 8, 12, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0, 4, 8, 12, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
                        for (var vectorizedEnd = inputIterator + ((inputLength >> 3) << 3); inputIterator != vectorizedEnd; inputIterator += 8)
                        {
                            var current = Avx.LoadVector256(inputIterator);
                            var isGreaterThanMinFixNegIntM1 = Avx2.CompareGreaterThan(current, vector256MinFixNegIntM1);
                            var isGreaterThanSByteMaxValue = Avx2.CompareGreaterThan(current, vector256SByteMaxValue);

                            if (unchecked((uint)Avx2.MoveMask(Avx2.AndNot(isGreaterThanSByteMaxValue, isGreaterThanMinFixNegIntM1).AsByte())) == uint.MaxValue)
                            {
                                var answer = Avx2.Shuffle(current.AsByte(), vector256In1Range).AsUInt32();
                                var number = answer.GetElement(0) | ((ulong)answer.GetElement(4) << 32);
                                var span = writer.GetSpan(8);
                                Unsafe.As<byte, ulong>(ref span[0]) = number;
                                writer.Advance(8);
                                continue;
                            }

                            var indexVector = Avx2.Add(isGreaterThanSByteMaxValue, isGreaterThanMinFixNegIntM1);
                            indexVector = Avx2.Add(indexVector, Avx2.CompareGreaterThan(current, vector256UShortMaxValue));
                            indexVector = Avx2.Add(indexVector, Avx2.CompareGreaterThan(current, vector256ByteMaxValue));
                            indexVector = Avx2.Add(indexVector, Avx2.CompareGreaterThan(current, vector256ShortMinValueM1));
                            indexVector = Avx2.Add(indexVector, Avx2.CompareGreaterThan(current, vector256SByteMinValueM1));
                            indexVector = Avx2.MultiplyLow(indexVector, vector256M1M7);
                            indexVector = Avx2.HorizontalAdd(indexVector, indexVector);

                            var index0 = indexVector.GetElement(0);
                            var index1 = indexVector.GetElement(1);
                            var index2 = indexVector.GetElement(4);
                            var index3 = indexVector.GetElement(5);

                            var count0 = countInt32Pointer[index0];
                            var count1 = countInt32Pointer[index1];
                            var count2 = countInt32Pointer[index2];
                            var count3 = countInt32Pointer[index3];
                            var countTotal = count0 + count1 + count2 + count3;

                            var destination = writer.GetSpan(countTotal);
                            fixed (byte* pDestination = &destination[0])
                            {
                                var tmpDestination = pDestination;

                                var item0 = Avx.LoadVector256(tableInt32Pointer + (index0 << 5));
                                var current01 = Avx2.ExtractVector128(current, 0).AsByte();
                                var shuffleCurrent0 = Avx2.ExtractVector128(item0, 0);
                                var constantCurrent0 = Avx2.ExtractVector128(item0, 1);
                                var shuffled0 = Ssse3.Shuffle(current01, shuffleCurrent0);
                                var answer0 = Sse2.Or(shuffled0, constantCurrent0);
                                var maskMove0 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count0));
                                Sse2.MaskMove(answer0, maskMove0, pDestination);
                                tmpDestination += count0;

                                var shift1 = Sse2.ShiftRightLogical128BitLane(current01, 8);
                                var item1 = Avx.LoadVector256(tableInt32Pointer + (index1 << 5));
                                var shuffleCurrent1 = Avx2.ExtractVector128(item1, 0);
                                var constantCurrent1 = Avx2.ExtractVector128(item1, 1);
                                var shuffled1 = Ssse3.Shuffle(shift1, shuffleCurrent1);
                                var answer1 = Sse2.Or(shuffled1, constantCurrent1);
                                var maskMove1 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count1));
                                Sse2.MaskMove(answer1, maskMove1, tmpDestination);
                                tmpDestination += count1;

                                var current23 = Avx2.ExtractVector128(current, 1).AsByte();
                                var item2 = Avx.LoadVector256(tableInt32Pointer + (index2 << 5));
                                var shuffleCurrent2 = Avx2.ExtractVector128(item2, 0);
                                var constantCurrent2 = Avx2.ExtractVector128(item2, 1);
                                var shuffled2 = Ssse3.Shuffle(current23, shuffleCurrent2);
                                var answer2 = Sse2.Or(shuffled2, constantCurrent2);
                                var maskMove2 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count2));
                                Sse2.MaskMove(answer2, maskMove2, tmpDestination);
                                tmpDestination += count2;

                                var shift3 = Sse2.ShiftRightLogical128BitLane(current23, 8);
                                var item3 = Avx.LoadVector256(tableInt32Pointer + (index3 << 5));
                                var shuffleCurrent3 = Avx2.ExtractVector128(item3, 0);
                                var constantCurrent3 = Avx2.ExtractVector128(item3, 1);
                                var shuffled3 = Ssse3.Shuffle(shift3, shuffleCurrent3);
                                var answer3 = Sse2.Or(shuffled3, constantCurrent3);
                                var maskMove3 = Sse2.ShiftRightLogical128BitLane(vector128ByteMaxValue, (byte)(16 - count3));
                                Sse2.MaskMove(answer3, maskMove3, tmpDestination);
                            }

                            writer.Advance(countTotal);
                        }
                    }
                }
                #endif

                while (inputIterator != inputEnd)
                {
                    writer.Write(*inputIterator++);
                }
            }
        }

        public int[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();

                if (len == 0)
                {
                    return Array.Empty<int>();
                }

                var array = new int[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadInt32();
                }

                return array;
            }
        }
    }

    public sealed class NullableStringFormatter : IMessagePackFormatter<String>
    {
        public static readonly NullableStringFormatter Instance = new NullableStringFormatter();

        private NullableStringFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, string value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public string Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadString();
        }
    }

    public sealed class NullableStringArrayFormatter : IMessagePackFormatter<String[]>
    {
        public static readonly NullableStringArrayFormatter Instance = new NullableStringArrayFormatter();

        private NullableStringArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, String[] value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
            }
        }

        public String[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                var array = new String[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = reader.ReadString();
                }

                return array;
            }
        }
    }

    public sealed class DecimalFormatter : IMessagePackFormatter<Decimal>
    {
        public static readonly DecimalFormatter Instance = new DecimalFormatter();

        private DecimalFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, decimal value, MessagePackSerializerOptions options)
        {
            var dest = writer.GetSpan(MessagePackRange.MaxFixStringLength);
            if (System.Buffers.Text.Utf8Formatter.TryFormat(value, dest.Slice(1), out var written))
            {
                // write header
                dest[0] = (byte)(MessagePackCode.MinFixStr | written);
                writer.Advance(written + 1);
            }
            else
            {
                // reset writer's span previously acquired that does not use
                writer.Advance(0);
                writer.Write(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public decimal Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (!(reader.ReadStringSequence() is ReadOnlySequence<byte> sequence))
            {
                throw new MessagePackSerializationException(string.Format("Unexpected msgpack code {0} ({1}) encountered.", MessagePackCode.Nil, MessagePackCode.ToFormatName(MessagePackCode.Nil)));
            }

            if (sequence.IsSingleSegment)
            {
                var span = sequence.First.Span;
                if (System.Buffers.Text.Utf8Parser.TryParse(span, out decimal result, out var bytesConsumed))
                {
                    if (span.Length != bytesConsumed)
                    {
                        throw new MessagePackSerializationException("Unexpected length of string.");
                    }

                    return result;
                }
            }
            else
            {
                // sequence.Length is not free
                var seqLen = (int)sequence.Length;
                if (seqLen < 128)
                {
                    Span<byte> span = stackalloc byte[seqLen];
                    sequence.CopyTo(span);
                    if (System.Buffers.Text.Utf8Parser.TryParse(span, out decimal result, out var bytesConsumed))
                    {
                        if (seqLen != bytesConsumed)
                        {
                            throw new MessagePackSerializationException("Unexpected length of string.");
                        }

                        return result;
                    }
                }
                else
                {
                    var rentArray = ArrayPool<byte>.Shared.Rent(seqLen);
                    try
                    {
                        sequence.CopyTo(rentArray);
                        if (System.Buffers.Text.Utf8Parser.TryParse(rentArray.AsSpan(0, seqLen), out decimal result, out var bytesConsumed))
                        {
                            if (seqLen != bytesConsumed)
                            {
                                throw new MessagePackSerializationException("Unexpected length of string.");
                            }

                            return result;
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(rentArray);
                    }
                }
            }

            throw new MessagePackSerializationException("Can't parse to decimal, input string was not in a correct format.");
        }
    }

    public sealed class TimeSpanFormatter : IMessagePackFormatter<TimeSpan>
    {
        public static readonly IMessagePackFormatter<TimeSpan> Instance = new TimeSpanFormatter();

        private TimeSpanFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, TimeSpan value, MessagePackSerializerOptions options)
        {
            writer.Write(value.Ticks);
            return;
        }

        public TimeSpan Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return new TimeSpan(reader.ReadInt64());
        }
    }

    public sealed class DateTimeOffsetFormatter : IMessagePackFormatter<DateTimeOffset>
    {
        public static readonly IMessagePackFormatter<DateTimeOffset> Instance = new DateTimeOffsetFormatter();

        private DateTimeOffsetFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, DateTimeOffset value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(new DateTime(value.Ticks, DateTimeKind.Utc)); // current ticks as is
            writer.Write((short)value.Offset.TotalMinutes); // offset is normalized in minutes
            return;
        }

        public DateTimeOffset Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2)
            {
                throw new MessagePackSerializationException("Invalid DateTimeOffset format.");
            }

            DateTime utc = reader.ReadDateTime();

            var dtOffsetMinutes = reader.ReadInt16();

            return new DateTimeOffset(utc.Ticks, TimeSpan.FromMinutes(dtOffsetMinutes));
        }
    }

    public sealed class GuidFormatter : IMessagePackFormatter<Guid>
    {
        public static readonly IMessagePackFormatter<Guid> Instance = new GuidFormatter();

        private GuidFormatter()
        {
        }

        public unsafe void Serialize(ref MessagePackWriter writer, Guid value, MessagePackSerializerOptions options)
        {
            byte* pBytes = stackalloc byte[36];
            Span<byte> bytes = new Span<byte>(pBytes, 36);
            new GuidBits(ref value).Write(bytes);
            writer.WriteString(bytes);
        }

        public Guid Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            ReadOnlySequence<byte> segment = reader.ReadStringSequence().Value;
            if (segment.Length != 36)
            {
                throw new MessagePackSerializationException("Unexpected length of string.");
            }

            GuidBits result;
            if (segment.IsSingleSegment)
            {
                result = new GuidBits(segment.First.Span);
            }
            else
            {
                Span<byte> bytes = stackalloc byte[36];
                segment.CopyTo(bytes);
                result = new GuidBits(bytes);
            }

            return result.Value;
        }
    }

    public sealed class UriFormatter : IMessagePackFormatter<Uri>
    {
        public static readonly IMessagePackFormatter<Uri> Instance = new UriFormatter();

        private UriFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Uri value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.ToString());
            }
        }

        public Uri Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return new Uri(reader.ReadString(), UriKind.RelativeOrAbsolute);
            }
        }
    }

    public sealed class VersionFormatter : IMessagePackFormatter<Version>
    {
        public static readonly IMessagePackFormatter<Version> Instance = new VersionFormatter();

        private VersionFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Version value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.ToString());
            }
        }

        public Version Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return new Version(reader.ReadString());
            }
        }
    }

    public sealed class KeyValuePairFormatter<TKey, TValue> : IMessagePackFormatter<KeyValuePair<TKey, TValue>>
    {
        public void Serialize(ref MessagePackWriter writer, KeyValuePair<TKey, TValue> value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            IFormatterResolver resolver = options.Resolver;
            resolver.GetFormatterWithVerify<TKey>().Serialize(ref writer, value.Key, options);
            resolver.GetFormatterWithVerify<TValue>().Serialize(ref writer, value.Value, options);
            return;
        }

        public KeyValuePair<TKey, TValue> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2)
            {
                throw new MessagePackSerializationException("Invalid KeyValuePair format.");
            }

            IFormatterResolver resolver = options.Resolver;
            options.Security.DepthStep(ref reader);
            try
            {
                TKey key = resolver.GetFormatterWithVerify<TKey>().Deserialize(ref reader, options);
                TValue value = resolver.GetFormatterWithVerify<TValue>().Deserialize(ref reader, options);
                return new KeyValuePair<TKey, TValue>(key, value);
            }
            finally
            {
                reader.Depth--;
            }
        }
    }

    public sealed class StringBuilderFormatter : IMessagePackFormatter<StringBuilder>
    {
        public static readonly IMessagePackFormatter<StringBuilder> Instance = new StringBuilderFormatter();

        private StringBuilderFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, StringBuilder value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.ToString());
            }
        }

        public StringBuilder Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                return new StringBuilder(reader.ReadString());
            }
        }
    }

    public sealed class BitArrayFormatter : IMessagePackFormatter<BitArray>
    {
        public static readonly IMessagePackFormatter<BitArray> Instance = new BitArrayFormatter();

        private BitArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, BitArray value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                var len = value.Length;
                writer.WriteArrayHeader(len);
                for (int i = 0; i < len; i++)
                {
                    writer.Write(value.Get(i));
                }

                return;
            }
        }

        public BitArray Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var len = reader.ReadArrayHeader();

                var array = new BitArray(len);
                for (int i = 0; i < len; i++)
                {
                    array[i] = reader.ReadBoolean();
                }

                return array;
            }
        }
    }

    public sealed class BigIntegerFormatter : IMessagePackFormatter<System.Numerics.BigInteger>
    {
        public static readonly IMessagePackFormatter<System.Numerics.BigInteger> Instance = new BigIntegerFormatter();

        private BigIntegerFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, System.Numerics.BigInteger value, MessagePackSerializerOptions options)
        {
#if NETCOREAPP2_1
            if (!writer.OldSpec)
            {
                // try to get bin8 buffer.
                var span = writer.GetSpan(byte.MaxValue);
                if (value.TryWriteBytes(span.Slice(2), out var written))
                {
                    span[0] = MessagePackCode.Bin8;
                    span[1] = (byte)written;

                    writer.Advance(written + 2);
                    return;
                }
                else
                {
                    // reset writer's span previously acquired that does not use
                    writer.Advance(0);
                }
            }
#endif

            writer.Write(value.ToByteArray());
            return;
        }

        public System.Numerics.BigInteger Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            ReadOnlySequence<byte> bytes = reader.ReadBytes().Value;
#if NETCOREAPP2_1
            if (bytes.IsSingleSegment)
            {
                return new System.Numerics.BigInteger(bytes.First.Span);
            }
            else
            {
                byte[] bytesArray = ArrayPool<byte>.Shared.Rent((int)bytes.Length);
                try
                {
                    bytes.CopyTo(bytesArray);
                    return new System.Numerics.BigInteger(bytesArray.AsSpan(0, (int)bytes.Length));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bytesArray);
                }
            }
#else
            return new System.Numerics.BigInteger(bytes.ToArray());
#endif
        }
    }

    public sealed class ComplexFormatter : IMessagePackFormatter<System.Numerics.Complex>
    {
        public static readonly IMessagePackFormatter<System.Numerics.Complex> Instance = new ComplexFormatter();

        private ComplexFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, System.Numerics.Complex value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.Real);
            writer.Write(value.Imaginary);
            return;
        }

        public System.Numerics.Complex Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var count = reader.ReadArrayHeader();

            if (count != 2)
            {
                throw new MessagePackSerializationException("Invalid Complex format.");
            }

            var real = reader.ReadDouble();

            var imaginary = reader.ReadDouble();

            return new System.Numerics.Complex(real, imaginary);
        }
    }

    public sealed class LazyFormatter<T> : IMessagePackFormatter<Lazy<T>>
    {
        public void Serialize(ref MessagePackWriter writer, Lazy<T> value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                IFormatterResolver resolver = options.Resolver;
                resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Value, options);
            }
        }

        public Lazy<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                options.Security.DepthStep(ref reader);
                try
                {
                    // deserialize immediately(no delay, because capture byte[] causes memory leak)
                    IFormatterResolver resolver = options.Resolver;
                    T v = resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);
                    return new Lazy<T>(() => v);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }
    }
}
