// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MessagePack.Formatters;

internal static partial class RefSerializeHelper
{
    internal static void Serialize(ref MessagePackWriter writer, ref readonly sbyte input, int length)
    {
        writer.WriteArrayHeader(length);
        if (length == 0)
        {
            return;
        }

        ref var inputIterator = ref Unsafe.AsRef(in input);
        const int maxInputSize = int.MaxValue / (sizeof(sbyte) + 1);
        if (Vector256.IsHardwareAccelerated)
        {
            const int mask = ~31;
            for (var alignedInputLength = (nuint)(length & mask); alignedInputLength > 0; alignedInputLength = (nuint)(length & mask))
            {
                if (alignedInputLength >= maxInputSize)
                {
                    alignedInputLength = maxInputSize & mask;
                }

                var destination = writer.GetSpan((int)alignedInputLength * (sizeof(sbyte) + 1));
                ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                nuint outputOffset = 0;
                for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector256<sbyte>.Count)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();

                    var loaded = Vector256.LoadUnsafe(ref inputIterator, inputOffset);

                    // Less than MinFixNegativeInt value requires 2 byte.
                    var lessThanMinFixNegativeInt = Vector256.ExtractMostSignificantBits(Vector256.LessThan(loaded, Vector256.Create((sbyte)MessagePackRange.MinFixNegativeInt)));
                    if (lessThanMinFixNegativeInt == 0)
                    {
                        loaded.AsByte().StoreUnsafe(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset));
                        outputOffset += (nuint)Vector256<sbyte>.Count;
                    }
                    else
                    {
                        for (var i = 0; i < Vector256<sbyte>.Count; i++, lessThanMinFixNegativeInt >>= 1)
                        {
                            var inputValue = Unsafe.AddByteOffset(ref inputIterator, inputOffset + (nuint)i);
                            if ((lessThanMinFixNegativeInt & 1) == 1)
                            {
                                WriteInt8(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), inputValue);
                                outputOffset += 2;
                            }
                            else
                            {
                                Unsafe.AddByteOffset(ref outputIterator, outputOffset) = (byte)inputValue;
                                outputOffset += 1;
                            }
                        }
                    }
                }

                writer.Advance((int)outputOffset);
                length -= (int)alignedInputLength;
                inputIterator = ref Unsafe.Add(ref inputIterator, alignedInputLength);
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

                var destination = writer.GetSpan((int)alignedInputLength * (sizeof(sbyte) + 1));
                ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                nuint outputOffset = 0;
                for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<sbyte>.Count)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();

                    var loaded = Vector128.LoadUnsafe(ref inputIterator, inputOffset);

                    // Less than MinFixNegativeInt value requires 2 byte.
                    var lessThanMinFixNegativeInt = Vector128.ExtractMostSignificantBits(Vector128.LessThan(loaded, Vector128.Create((sbyte)MessagePackRange.MinFixNegativeInt)));
                    if (lessThanMinFixNegativeInt == 0)
                    {
                        loaded.AsByte().StoreUnsafe(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset));
                        outputOffset += (nuint)Vector128<sbyte>.Count;
                    }
                    else
                    {
                        for (var i = 0; i < Vector128<sbyte>.Count; i++, lessThanMinFixNegativeInt >>= 1)
                        {
                            var inputValue = Unsafe.AddByteOffset(ref inputIterator, inputOffset + (nuint)i);
                            if ((lessThanMinFixNegativeInt & 1) == 1)
                            {
                                WriteInt8(ref Unsafe.AddByteOffset(ref outputIterator, outputOffset), inputValue);
                                outputOffset += 2;
                            }
                            else
                            {
                                Unsafe.AddByteOffset(ref outputIterator, outputOffset) = (byte)inputValue;
                                outputOffset += 1;
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

            var destination = writer.GetSpan(inputLength * (sizeof(sbyte) + 1));
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
