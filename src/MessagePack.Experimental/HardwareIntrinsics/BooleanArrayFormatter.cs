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
        nint outputOffset = 0;
        foreach (var memory in rawSequence)
        {
            reader.CancellationToken.ThrowIfCancellationRequested();
            if (memory.IsEmpty)
            {
                continue;
            }

            ref var inputIterator = ref MemoryMarshal.GetReference(memory.Span);
            var inputLength = memory.Length;
            if (Vector128.IsHardwareAccelerated)
            {
                const int mask = ~15;
                var alignedInputLength = (nuint)(inputLength & mask);
                if (alignedInputLength > 0)
                {
                    for (nuint inputOffset = 0; inputOffset < alignedInputLength; inputOffset += (nuint)Vector256<byte>.Count)
                    {
                        reader.CancellationToken.ThrowIfCancellationRequested();

                        var loaded = Vector128.LoadUnsafe(ref inputIterator, inputOffset).AsSByte();
                        var results = loaded - Vector128.Create(unchecked((sbyte)MessagePackCode.False));
                        var error = Vector128.ExtractMostSignificantBits(Vector128.GreaterThan(results, Vector128.Create((sbyte)1)) | Vector128.LessThan(results, Vector128<sbyte>.Zero));
                        if (error == 0)
                        {
                            results.StoreUnsafe(ref Unsafe.As<bool, sbyte>(ref Unsafe.AddByteOffset(ref outputIterator, inputOffset + (nuint)outputOffset)));
                        }
                        else
                        {
                            ThrowUnexpectedMessagePackCode(Unsafe.AddByteOffset(ref inputIterator, inputOffset + (nuint)BitOperations.TrailingZeroCount(error)));
                        }
                    }
                }

                outputOffset = (nint)alignedInputLength;
            }

            for (nint i = 0; i < inputLength; i++)
            {
                reader.CancellationToken.ThrowIfCancellationRequested();
                var code = Unsafe.AddByteOffset(ref inputIterator, i);
                switch (code)
                {
                    case MessagePackCode.True:
                        Unsafe.AddByteOffset(ref outputIterator, outputOffset++) = true;
                        break;
                    case MessagePackCode.False:
                        Unsafe.AddByteOffset(ref outputIterator, outputOffset++) = false;
                        break;
                    default:
                        ThrowUnexpectedMessagePackCode(code);
                        break;
                }
            }
        }

        return array;
    }

    [DoesNotReturn]
    private static void ThrowUnexpectedMessagePackCode(byte code)
    {
        throw new MessagePackSerializationException(string.Format("Unexpected msgpack code {0} ({1}) encountered.", code, MessagePackCode.ToFormatName(code)));
    }
}
