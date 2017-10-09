#if NETSTANDARD

using System.Runtime.CompilerServices;

namespace MessagePack.Internal
{
    public static partial class UnsafeMemory32
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw4(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw5(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 1) = *(int*)(pSrc + 1);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw6(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 2) = *(int*)(pSrc + 2);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw7(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 3) = *(int*)(pSrc + 3);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw8(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw9(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 5) = *(int*)(pSrc + 5);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw10(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 6) = *(int*)(pSrc + 6);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw11(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 7) = *(int*)(pSrc + 7);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw12(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw13(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 9) = *(int*)(pSrc + 9);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw14(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 10) = *(int*)(pSrc + 10);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw15(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 11) = *(int*)(pSrc + 11);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw16(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw17(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 13) = *(int*)(pSrc + 13);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw18(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 14) = *(int*)(pSrc + 14);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw19(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 15) = *(int*)(pSrc + 15);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw20(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw21(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 17) = *(int*)(pSrc + 17);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw22(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 18) = *(int*)(pSrc + 18);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw23(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 19) = *(int*)(pSrc + 19);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw24(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw25(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 21) = *(int*)(pSrc + 21);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw26(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 22) = *(int*)(pSrc + 22);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw27(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 23) = *(int*)(pSrc + 23);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw28(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 24) = *(int*)(pSrc + 24);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw29(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 24) = *(int*)(pSrc + 24);
                *(int*)(pDst + 25) = *(int*)(pSrc + 25);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw30(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 24) = *(int*)(pSrc + 24);
                *(int*)(pDst + 26) = *(int*)(pSrc + 26);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw31(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(int*)(pDst + 0) = *(int*)(pSrc + 0);
                *(int*)(pDst + 4) = *(int*)(pSrc + 4);
                *(int*)(pDst + 8) = *(int*)(pSrc + 8);
                *(int*)(pDst + 12) = *(int*)(pSrc + 12);
                *(int*)(pDst + 16) = *(int*)(pSrc + 16);
                *(int*)(pDst + 20) = *(int*)(pSrc + 20);
                *(int*)(pDst + 24) = *(int*)(pSrc + 24);
                *(int*)(pDst + 27) = *(int*)(pSrc + 27);
            }

            return src.Length;
        }

    }

    public static partial class UnsafeMemory64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw8(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw9(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 1) = *(long*)(pSrc + 1);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw10(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 2) = *(long*)(pSrc + 2);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw11(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 3) = *(long*)(pSrc + 3);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw12(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 4) = *(long*)(pSrc + 4);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw13(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 5) = *(long*)(pSrc + 5);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw14(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 6) = *(long*)(pSrc + 6);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw15(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 7) = *(long*)(pSrc + 7);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw16(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw17(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 9) = *(long*)(pSrc + 9);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw18(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 10) = *(long*)(pSrc + 10);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw19(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 11) = *(long*)(pSrc + 11);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw20(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 12) = *(long*)(pSrc + 12);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw21(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 13) = *(long*)(pSrc + 13);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw22(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 14) = *(long*)(pSrc + 14);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw23(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 15) = *(long*)(pSrc + 15);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw24(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw25(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 17) = *(long*)(pSrc + 17);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw26(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 18) = *(long*)(pSrc + 18);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw27(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 19) = *(long*)(pSrc + 19);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw28(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 20) = *(long*)(pSrc + 20);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw29(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 21) = *(long*)(pSrc + 21);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw30(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 22) = *(long*)(pSrc + 22);
            }

            return src.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteRaw31(ref byte[] dst, int dstOffset, byte[] src)
        {
            MessagePackBinary.EnsureCapacity(ref dst, dstOffset, src.Length);

            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[dstOffset])
            {
                *(long*)(pDst + 0) = *(long*)(pSrc + 0);
                *(long*)(pDst + 8) = *(long*)(pSrc + 8);
                *(long*)(pDst + 16) = *(long*)(pSrc + 16);
                *(long*)(pDst + 23) = *(long*)(pSrc + 23);
            }

            return src.Length;
        }

    }
}

#endif