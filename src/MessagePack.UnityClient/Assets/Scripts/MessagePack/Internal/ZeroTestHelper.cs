// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MessagePack.Internal
{
    public static class ZeroTestHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDefault<T>(ref T value)
            where T : struct
        {
            return value.Equals(DefaultContainer<T>.DefaultObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsDefaultUnmanaged<T>(ulong* pointer8)
            where T : unmanaged
        {
            var size = sizeof(T);
            var len8 = size >> 3;
            for (var i = 0; i < len8; i++, pointer8++)
            {
                if (*pointer8 != 0)
                {
                    return false;
                }
            }

            var pointer4 = (uint*)pointer8;
            if ((size & 0b100) != 0)
            {
                if (*pointer4++ != 0)
                {
                    return false;
                }
            }

            var pointer2 = (ushort*)pointer4;
            if ((size & 0b10) != 0)
            {
                if (*pointer2++ != 0)
                {
                    return false;
                }
            }

            var pointer1 = (byte*)pointer2;
            return (size & 0b1) == 0 || *pointer1 == 0;
        }

        private static class DefaultContainer<T>
            where T : struct
        {
            public static readonly object DefaultObject = default(T);
        }
    }
}
