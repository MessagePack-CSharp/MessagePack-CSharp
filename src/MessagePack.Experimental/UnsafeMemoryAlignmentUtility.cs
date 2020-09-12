// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace MessagePack.Formatters
{
    internal static unsafe class UnsafeMemoryAlignmentUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CalculateDifferenceAlign16(void* pointer)
        {
            var difference = new UIntPtr(pointer).ToUInt64();
            difference -= (difference >> 4) << 4;
            difference = (16 - difference) & 0xfUL;
            return (int)difference;
        }

        public static int CalculateDifferenceAlign32(void* pointer)
        {
            var difference = new UIntPtr(pointer).ToUInt64();
            difference -= (difference >> 5) << 5;
            difference = (32 - difference) & 0x1fUL;
            return (int)difference;
        }
    }
}
