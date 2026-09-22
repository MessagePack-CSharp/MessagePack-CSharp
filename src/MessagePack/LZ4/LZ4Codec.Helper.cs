// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MessagePack.LZ4
{
    internal partial class LZ4Codec
    {
        internal static class HashTablePool
        {
            [ThreadStatic]
            private static ushort[]? ushortPool;

            [ThreadStatic]
            private static uint[]? uintPool;

            [ThreadStatic]
            private static int[]? intPool;

            public static ushort[] GetUShortHashTablePool()
            {
                if (ushortPool == null)
                {
                    ushortPool = new ushort[HASH64K_TABLESIZE];
                }
                else
                {
                    Array.Clear(ushortPool, 0, ushortPool.Length);
                }

                return ushortPool;
            }

            public static uint[] GetUIntHashTablePool()
            {
                if (uintPool == null)
                {
                    uintPool = new uint[HASH_TABLESIZE];
                }
                else
                {
                    Array.Clear(uintPool, 0, uintPool.Length);
                }

                return uintPool;
            }

            public static int[] GetIntHashTablePool()
            {
                if (intPool == null)
                {
                    intPool = new int[HASH_TABLESIZE];
                }
                else
                {
                    Array.Clear(intPool, 0, intPool.Length);
                }

                return intPool;
            }
        }
    }
}
