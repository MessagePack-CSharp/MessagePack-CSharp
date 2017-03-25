using System;

namespace MessagePack.LZ4
{
    public static partial class LZ4Codec
    {
#if NETSTANDARD1_4

        public static int Encode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength)
        {
            if (IntPtr.Size == 4)
            {
                return LZ4Codec.Encode32Unsafe(input, inputOffset, inputLength, output, outputOffset, outputLength);
            }
            else
            {
                return LZ4Codec.Encode64Unsafe(input, inputOffset, inputLength, output, outputOffset, outputLength);
            }
        }

        public static int Decode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength)
        {
            if (IntPtr.Size == 4)
            {
                return LZ4Codec.Decode32Unsafe(input, inputOffset, inputLength, output, outputOffset, outputLength);
            }
            else
            {
                return LZ4Codec.Decode64Unsafe(input, inputOffset, inputLength, output, outputOffset, outputLength);
            }
        }

#else

        // use 'Safe' code for Unity because in IL2CPP gots strange behaviour.

        public static int Encode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength)
        {
            if (IntPtr.Size == 4)
            {
                return LZ4Codec.Encode32Safe(input, inputOffset, inputLength, output, outputOffset, outputLength);
            }
            else
            {
                return LZ4Codec.Encode64Safe(input, inputOffset, inputLength, output, outputOffset, outputLength);
            }
        }

        public static int Decode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength)
        {
            if (IntPtr.Size == 4)
            {
                return LZ4Codec.Decode32Safe(input, inputOffset, inputLength, output, outputOffset, outputLength);
            }
            else
            {
                return LZ4Codec.Decode64Safe(input, inputOffset, inputLength, output, outputOffset, outputLength);
            }
        }

#endif

        internal static class HashTablePool
        {
            [ThreadStatic]
            static ushort[] ushortPool;

            [ThreadStatic]
            static uint[] uintPool;

            [ThreadStatic]
            static int[] intPool;

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

