// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MessagePack.Formatters;

internal static partial class SpanDeserializeHelper
{
    internal static void Deserialize(ref MessagePackReader reader, Span<float> destination)
    {
        ref var outputIterator = ref MemoryMarshal.GetReference(destination);
        ref var outputEnd = ref Unsafe.Add(ref outputIterator, destination.Length);

        for (;
            !Unsafe.AreSame(ref outputIterator, ref outputEnd);
            outputIterator = ref Unsafe.Add(ref outputIterator, 1))
        {
            outputIterator = reader.ReadSingle();
        }
    }
}
