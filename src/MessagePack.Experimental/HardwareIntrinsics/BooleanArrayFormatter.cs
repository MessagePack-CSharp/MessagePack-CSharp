// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MessagePack.Formatters;

public sealed unsafe partial class BooleanArrayFormatter
{
    public bool[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return [];
        }

        var rawSequence = reader.ReadRaw(len);
        var array = new bool[len];
        ref var outputIterator = ref MemoryMarshal.GetArrayDataReference(array);
        nuint outputOffset = 0;
        foreach (var memory in rawSequence)
        {
            reader.CancellationToken.ThrowIfCancellationRequested();
            if (memory.IsEmpty)
            {
                continue;
            }

            ref var inputIterator = ref MemoryMarshal.GetReference(memory.Span);
            var inputLength = (nuint)memory.Length;
            if (inputLength > (nuint)array.Length - outputOffset)
            {
                inputLength = (nuint)(nint)array.Length - outputOffset;
            }

            nuint inputOffset = 0;
            if (Vector128.IsHardwareAccelerated)
            {
                const nuint mask = ~15U;
                var alignedInputLength = inputLength & mask;
                for (; inputOffset < alignedInputLength; inputOffset += (nuint)Vector128<byte>.Count)
                {
                    reader.CancellationToken.ThrowIfCancellationRequested();

                    var loaded = Vector128.LoadUnsafe(ref inputIterator, inputOffset).AsSByte();

                    // MessagePackCode.True is 0xc3(-61). MessagePackCode.False is 0xc2(-62).
                    var results = loaded - Vector128.Create(unchecked((sbyte)MessagePackCode.False));
                    var error = Vector128.ExtractMostSignificantBits(Vector128.GreaterThan(results, Vector128<sbyte>.One) | Vector128.LessThan(results, Vector128<sbyte>.Zero));
                    if (error == 0)
                    {
                        results.StoreUnsafe(ref Unsafe.As<bool, sbyte>(ref Unsafe.AddByteOffset(ref outputIterator, inputOffset + outputOffset)));
                    }
                    else
                    {
                        ThrowUnexpectedMessagePackCode(Unsafe.AddByteOffset(ref inputIterator, inputOffset + outputOffset + (nuint)BitOperations.TrailingZeroCount(error)));
                    }
                }
            }

            for (; inputOffset < inputLength; inputOffset++)
            {
                reader.CancellationToken.ThrowIfCancellationRequested();
                var code = Unsafe.AddByteOffset(ref inputIterator, inputOffset);
                switch (code)
                {
                    case MessagePackCode.True:
                        Unsafe.AddByteOffset(ref outputIterator, inputOffset + outputOffset) = true;
                        break;
                    case MessagePackCode.False:
                        Unsafe.AddByteOffset(ref outputIterator, inputOffset + outputOffset) = false;
                        break;
                    default:
                        ThrowUnexpectedMessagePackCode(code);
                        break;
                }
            }

            outputOffset += inputLength;
        }

        return array;
    }

    [DoesNotReturn]
    private static void ThrowUnexpectedMessagePackCode(byte code)
    {
        throw new MessagePackSerializationException(string.Format("Unexpected msgpack code {0} ({1}) encountered.", code, MessagePackCode.ToFormatName(code)));
    }
}
