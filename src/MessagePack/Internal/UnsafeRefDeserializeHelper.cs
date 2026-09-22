// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*
 * The reason why deserialize method signature is int Deserialize(ref byte input, int length, ref bool output)
 * 1. Vector128.Load/Store method requires only ref T or T*, not ReadOnlySpan<T>.
 * 2. Unity IL2CPP does not treat ReadOnlySpan<T> as first-class-citizen.
 *
 * The reason why return type is int is to detect which index the invalid messagepack is.
 *
 * When .NET team introduces the overload method for ReadOnlySpan<T>, the signature can be changed.
 */

using System.Runtime.CompilerServices;

#if NET8_0_OR_GREATER
using System.Numerics;
using System.Runtime.Intrinsics;
#endif

namespace MessagePack.Internal;

internal static class UnsafeRefDeserializeHelper
{
    internal static int Deserialize(ref byte input, int length, ref bool output)
    {
        var inputOffset = 0;

#if NET8_0_OR_GREATER
        unchecked
        {
            if (Vector256.IsHardwareAccelerated)
            {
                const int mask = ~31;
                var alignedLength = length & mask;
                if (alignedLength > 0)
                {
                    for (; inputOffset < alignedLength; inputOffset += Vector256<sbyte>.Count)
                    {
                        var loaded = Vector256.LoadUnsafe(ref input, (nuint)inputOffset).AsSByte();

                        // MessagePackCode.True is 0xc3(-61). MessagePackCode.False is 0xc2(-62).
                        var results = loaded - Vector256.Create((sbyte)MessagePackCode.False);
                        var error = Vector256.ExtractMostSignificantBits(Vector256.GreaterThan(results, Vector256<sbyte>.One) | Vector256.LessThan(results, Vector256<sbyte>.Zero));
                        if (error != 0)
                        {
                            return inputOffset + BitOperations.TrailingZeroCount(error);
                        }

                        results.StoreUnsafe(ref Unsafe.As<bool, sbyte>(ref output), (nuint)inputOffset);
                    }
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                const int mask = ~15;
                var alignedLength = length & mask;
                if (alignedLength > 0)
                {
                    for (; inputOffset < alignedLength; inputOffset += Vector128<sbyte>.Count)
                    {
                        var loaded = Vector128.LoadUnsafe(ref input, (nuint)inputOffset).AsSByte();

                        // MessagePackCode.True is 0xc3(-61). MessagePackCode.False is 0xc2(-62).
                        var results = loaded - Vector128.Create((sbyte)MessagePackCode.False);
                        var error = Vector128.ExtractMostSignificantBits(Vector128.GreaterThan(results, Vector128<sbyte>.One) | Vector128.LessThan(results, Vector128<sbyte>.Zero));
                        if (error != 0)
                        {
                            return inputOffset + BitOperations.TrailingZeroCount(error);
                        }

                        results.StoreUnsafe(ref Unsafe.As<bool, sbyte>(ref output), (nuint)inputOffset);
                    }
                }
            }
        }
#endif

        for (; inputOffset < length; inputOffset++)
        {
            var code = Unsafe.Add(ref input, inputOffset);
            switch (code)
            {
                case MessagePackCode.True:
                    Unsafe.Add(ref output, inputOffset) = true;
                    break;
                case MessagePackCode.False:
                    Unsafe.Add(ref output, inputOffset) = false;
                    break;
                default:
                    return inputOffset;
            }
        }

        return -1;
    }
}
