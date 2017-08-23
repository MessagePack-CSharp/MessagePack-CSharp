using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Internal
{
    public static class ByteArrayComparer
    {
#if ENABLE_UNSAFE_MSGPACK

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe bool Compare(byte[] xs, int xsOffset, int xsCount, byte[] ys)
        {
            return Compare(xs, xsOffset, xsCount, ys, 0, ys.Length);
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe bool Compare(byte[] xs, int xsOffset, int xsCount, byte[] ys, int ysOffset, int ysCount)
        {
            if (xs == null || ys == null || xsCount != ysCount)
            {
                return false;
            }

            fixed (byte* p1 = xs)
            fixed (byte* p2 = ys)
            {
                var x1 = p1 + xsOffset;
                var x2 = p2 + ysOffset;

                var length = xsCount;
                var loooCount = length / 8;

                for (var i = 0; i < loooCount; i++, x1 += 8, x2 += 8)
                {
                    if (*(long*)x1 != *(long*)x2)
                    {
                        return false;
                    }
                }

                if ((length & 4) != 0)
                {
                    if (*(int*)x1 != *(int*)x2)
                    {
                        return false;
                    }
                    x1 += 4;
                    x2 += 4;
                }

                if ((length & 2) != 0)
                {
                    if (*(short*)x1 != *(short*)x2)
                    {
                        return false;
                    }
                    x1 += 2;
                    x2 += 2;
                }

                if ((length & 1) != 0)
                {
                    if (*x1 != *x2)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

#else
#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static bool Compare(byte[] xs, int xsOffset, int xsCount, byte[] ys)
        {
            if (xs == null || ys == null || xsCount != ys.Length)
            {
                return false;
            }

            for (int i = 0; i < ys.Length; i++)
            {
                if (xs[xsOffset++] != ys[i]) return false;
            }

            return true;
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static bool Compare(byte[] xs, int xsOffset, int xsCount, byte[] ys, int ysOffset, int ysCount)
        {
            if (xs == null || ys == null || xsCount != ysCount)
            {
                return false;
            }

            for (int i = 0; i < xsCount; i++)
            {
                if (xs[xsOffset++] != ys[ysOffset++]) return false;
            }

            return true;
        }

#endif

    }
}