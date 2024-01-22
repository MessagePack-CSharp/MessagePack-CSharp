// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MessagePack.Formatters
{
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
