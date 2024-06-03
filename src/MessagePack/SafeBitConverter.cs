// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MessagePack;

internal static class SafeBitConverter
{
    internal static long ToInt64(ReadOnlySpan<byte> value)
        => Unsafe.ReadUnaligned<long>(ref Unsafe.AsRef(in MemoryMarshal.GetReference(value)));

    internal static ulong ToUInt64(ReadOnlySpan<byte> value) => unchecked((ulong)ToInt64(value));

    internal static unsafe ushort ToUInt16(ReadOnlySpan<byte> value)
        => Unsafe.ReadUnaligned<ushort>(ref Unsafe.AsRef(in MemoryMarshal.GetReference(value)));

    internal static unsafe uint ToUInt32(ReadOnlySpan<byte> value)
        => Unsafe.ReadUnaligned<uint>(ref Unsafe.AsRef(in MemoryMarshal.GetReference(value)));
}
