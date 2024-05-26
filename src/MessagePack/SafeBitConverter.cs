// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MessagePack
{
    internal static class SafeBitConverter
    {
        internal static unsafe long ToInt64(ReadOnlySpan<byte> value)
        {
            fixed (byte* p = &MemoryMarshal.GetReference(value))
            {
                return Unsafe.ReadUnaligned<long>(p);
            }
        }

        internal static ulong ToUInt64(ReadOnlySpan<byte> value) => unchecked((ulong)ToInt64(value));

        internal static unsafe ushort ToUInt16(ReadOnlySpan<byte> value)
        {
            fixed (byte* p = &MemoryMarshal.GetReference(value))
            {
                return Unsafe.ReadUnaligned<ushort>(p);
            }
        }

        internal static unsafe uint ToUInt32(ReadOnlySpan<byte> value)
        {
            fixed (byte* p = &MemoryMarshal.GetReference(value))
            {
                return Unsafe.ReadUnaligned<uint>(p);
            }
        }
    }
}
