#if ENABLE_UNSAFE_MSGPACK

using System;

namespace LZ4
{
    internal static partial class LZ4Codec
    {
        public static int Encode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength)
        {
            if (IntPtr.Size == 4)
            {
                return LZ4Codec.Encode32(input, inputOffset, inputLength, output, outputOffset, outputLength);
            }
            else
            {
                return LZ4Codec.Encode64(input, inputOffset, inputLength, output, outputOffset, outputLength);
            }
        }

        public static int Decode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength, bool knownOutputLength)
        {
            if (IntPtr.Size == 4)
            {
                return LZ4Codec.Decode32(input, inputOffset, inputLength, output, outputOffset, outputLength, knownOutputLength);
            }
            else
            {
                return LZ4Codec.Decode64(input, inputOffset, inputLength, output, outputOffset, outputLength, knownOutputLength);
            }
        }

        internal unsafe static class HashTablePool
        {
            [ThreadStatic]
            static ushort[] ushortPool;

            [ThreadStatic]
            static uint[] uintPool;

            [ThreadStatic]
            static byte*[] bytePool;

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

            public static byte*[] GetByteHashTablePool()
            {
                if (bytePool == null)
                {
                    bytePool = new byte*[HASH_TABLESIZE];
                }
                else
                {
                    Array.Clear(bytePool, 0, bytePool.Length);
                }
                return bytePool;
            }
        }
    }
}

#endif