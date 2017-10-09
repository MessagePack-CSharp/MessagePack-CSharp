using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Internal
{
    public static class ByteArrayComparer
    {
#if ENABLE_UNSAFE_MSGPACK

#if NETSTANDARD

        static readonly bool Is32Bit = (IntPtr.Size == 4);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(byte[] bytes, int offset, int count)
        {
            if (Is32Bit)
            {
                return unchecked((int)FarmHash.Hash32(bytes, offset, count));
            }
            else
            {
                return unchecked((int)FarmHash.Hash64(bytes, offset, count));
            }
        }

#endif

#if NETSTANDARD
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe bool Equals(byte[] xs, int xsOffset, int xsCount, byte[] ys)
        {
            return Equals(xs, xsOffset, xsCount, ys, 0, ys.Length);
        }

#if NETSTANDARD
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe bool Equals(byte[] xs, int xsOffset, int xsCount, byte[] ys, int ysOffset, int ysCount)
        {
            if (xs == null || ys == null || xsCount != ysCount)
            {
                return false;
            }

            fixed (byte* p1 = &xs[xsOffset])
            fixed (byte* p2 = &ys[ysOffset])
            {
                switch (xsCount)
                {
                    case 0:
                        return true;
                    case 1:
                        return *p1 == *p2;
                    case 2:
                        return *(short*)p1 == *(short*)p2;
                    case 3:
                        if (*(byte*)p1 != *(byte*)p2) return false;
                        return *(short*)(p1 + 1) == *(short*)(p2 + 1);
                    case 4:
                        return *(int*)p1 == *(int*)p2;
                    case 5:
                        if (*(byte*)p1 != *(byte*)p2) return false;
                        return *(int*)(p1 + 1) == *(int*)(p2 + 1);
                    case 6:
                        if (*(short*)p1 != *(short*)p2) return false;
                        return *(int*)(p1 + 2) == *(int*)(p2 + 2);
                    case 7:
                        if (*(byte*)p1 != *(byte*)p2) return false;
                        if (*(short*)(p1 + 1) != *(short*)(p2 + 1)) return false;
                        return *(int*)(p1 + 3) == *(int*)(p2 + 3);
                    default:
                        {
                            var x1 = p1;
                            var x2 = p2;

                            byte* xEnd = p1 + xsCount - 8;
                            byte* yEnd = p2 + ysCount - 8;

                            while (x1 < xEnd)
                            {
                                if (*(long*)x1 != *(long*)x2)
                                {
                                    return false;
                                }

                                x1 += 8;
                                x2 += 8;
                            }

                            return *(long*)xEnd == *(long*)yEnd;
                        }
                }
            }
        }

#else
#if NETSTANDARD
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static bool Equals(byte[] xs, int xsOffset, int xsCount, byte[] ys)
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

#if NETSTANDARD
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static bool Equals(byte[] xs, int xsOffset, int xsCount, byte[] ys, int ysOffset, int ysCount)
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