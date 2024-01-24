// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MessagePack.Formatters;

internal static partial class ReadOnlySpanSerializeHelper
{
    internal static void Serialize(ref MessagePackWriter writer, ref readonly bool input, int length)
    {
        writer.WriteArrayHeader(length);
        if (length == 0)
        {
            return;
        }

        ref var inputIterator = ref Unsafe.AsRef(in input);
        if (length == 0)
        {
            return;
        }

        if (Vector128.IsHardwareAccelerated)
        {
            const int mask = ~15;
            var alignedInputLength = (nuint)(length & mask);
            if (alignedInputLength > 0)
            {
                var destination = writer.GetSpan((int)alignedInputLength);
                ref var outputIterator = ref MemoryMarshal.GetReference(destination);
                for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<byte>.Count)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();

                    // Load 16 bools.
                    var loaded = Vector128.LoadUnsafe(ref Unsafe.As<bool, sbyte>(ref inputIterator), inputOffset);

                    // true is not 0, but false is always 0 in C#. true -> 0. false -> -1.
                    var falses = Vector128.Equals(loaded, Vector128<sbyte>.Zero);

                    // MessagePackCode.True is 0xc3(-61). MessagePackCode.False is 0xc2(-62).
                    var results = Vector128.Create(unchecked((sbyte)MessagePackCode.True)) + falses;

                    // Store 16 values.
                    results.AsByte().StoreUnsafe(ref outputIterator, inputOffset);
                }

                writer.Advance((int)alignedInputLength);
                length -= (int)alignedInputLength;
                inputIterator = ref Unsafe.Add(ref inputIterator, alignedInputLength);
            }
        }

        {
            var destination = writer.GetSpan(length);
            ref var outputIterator = ref MemoryMarshal.GetReference(destination);
            for (nuint index = 0; index < (nuint)length; index++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                Unsafe.AddByteOffset(ref outputIterator, index) = Unsafe.Add(ref inputIterator, index) ? MessagePackCode.True : MessagePackCode.False;
            }
        }

        writer.Advance(length);
    }
}
