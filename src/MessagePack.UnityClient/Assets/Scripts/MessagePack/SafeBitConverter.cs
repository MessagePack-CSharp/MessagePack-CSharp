// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace MessagePack
{
    internal static class SafeBitConverter
    {
        internal static unsafe long ToInt64(ReadOnlySpan<byte> value)
        {
#if UNITY_ANDROID
            if (sizeof(int*) == 4)
            {
                if (BitConverter.IsLittleEndian)
                {
                    uint i1 = (uint)(value[0] | (value[1] << 8) | (value[2] << 16) | (value[3] << 24));
                    uint i2 = (uint)(value[4] | (value[5] << 8) | (value[6] << 16) | (value[7] << 24));
                    return i1 | ((long)i2 << 32);
                }
                else
                {
                    uint i1 = (uint)((value[0] << 24) | (value[1] << 16) | (value[2] << 8) | value[3]);
                    uint i2 = (uint)((value[4] << 24) | (value[5] << 16) | (value[6] << 8) | value[7]);
                    return i2 | ((long)i1 << 32);
                }
            }
#endif
            return MemoryMarshal.Cast<byte, long>(value)[0];
        }

        internal static ulong ToUInt64(ReadOnlySpan<byte> value) => unchecked((ulong)ToInt64(value));

        internal static unsafe ushort ToUInt16(ReadOnlySpan<byte> value)
        {
#if UNITY_ANDROID
            if (sizeof(int*) == 4)
            {
                if (BitConverter.IsLittleEndian)
                {
                    return (ushort)(value[0] | (value[1] << 8));
                }
                else
                {
                    return (ushort)((value[0] << 8) | value[1]);
                }
            }
#endif
            return MemoryMarshal.Cast<byte, ushort>(value)[0];
        }

        internal static unsafe uint ToUInt32(ReadOnlySpan<byte> value)
        {
#if UNITY_ANDROID
            if (sizeof(int*) == 4)
            {
                if (BitConverter.IsLittleEndian)
                {
                    return (uint)(value[0] | (value[1] << 8) | (value[2] << 16) | (value[3] << 24));
                }
                else
                {
                    return (uint)((value[0] << 24) | (value[1] << 16) | (value[2] << 8) | value[3]);
                }
            }
#endif
            return MemoryMarshal.Cast<byte, uint>(value)[0];
        }
    }
}
