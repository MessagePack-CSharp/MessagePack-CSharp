using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Internal
{
    internal static class ByteArrayComparer
    {
#if ENABLE_UNSAFE_MSGPACK

#if !UNITY

        static readonly bool Is32Bit = (IntPtr.Size == 4);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(ReadOnlySpan<byte> bytes)
        {
            if (Is32Bit)
            {
                return unchecked((int)FarmHash.Hash32(bytes));
            }
            else
            {
                return unchecked((int)FarmHash.Hash64(bytes));
            }
        }

#endif

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe bool Equals(ReadOnlySpan<byte> xs, ReadOnlySpan<byte> ys)
        {
            if (xs.Length != ys.Length)
            {
                return false;
            }

            fixed (byte* p1 = xs)
            fixed (byte* p2 = ys)
            {
                switch (xs.Length)
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

                            byte* xEnd = p1 + xs.Length - 8;
                            byte* yEnd = p2 + ys.Length - 8;

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
#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static bool Equals(ReadOnlySpan<byte> xs, ReadOnlySpan<byte> ys)
        {
            if (xs.Length != ys.Length)
            {
                return false;
            }

            for (int i = 0; i < ys.Length; i++)
            {
                if (xs[i] != ys[i]) return false;
            }

            return true;
        }

#endif

    }
}