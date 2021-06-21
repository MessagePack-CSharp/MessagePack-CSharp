using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MessagePackv3
{
    // System.Buffers.BinaryPrimitives.TryWrite***(Span<byte> buffer, T value);

    // Write/Read/TryWrite/TryRead

    // Write to highest index first so the JIT skips bounds checks on subsequent writes.
    // Primitives is important part of performance, don't use slice and call other private method.
    // Needs Span and byte[] api.

    public static partial class MessagePackPrimitives
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(Span<byte> buffer, int value)
        {
            if (buffer.Length < 5) Throw();

            unchecked
            {
                buffer[4] = (byte)value;
                buffer[3] = (byte)(value >> 8);
                buffer[2] = (byte)(value >> 16);
                buffer[1] = (byte)(value >> 24);
                buffer[0] = MessagePackCode.Int32;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteInt32(byte[] buffer, int offset, int count, int value)
        {
            RangeCheck(buffer, offset, count, 5);

            unchecked
            {
                fixed (byte* p = &buffer[offset])
                {
                    p[4] = (byte)value;
                    p[3] = (byte)(value >> 8);
                    p[2] = (byte)(value >> 16);
                    p[1] = (byte)(value >> 24);
                    p[0] = MessagePackCode.Int32;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteInt32(Span<byte> buffer, int value)
        {
            if (buffer.Length < 5) return false;

            unchecked
            {
                buffer[4] = (byte)value;
                buffer[3] = (byte)(value >> 8);
                buffer[2] = (byte)(value >> 16);
                buffer[1] = (byte)(value >> 24);
                buffer[0] = MessagePackCode.Int32;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryWriteInt32(byte[] buffer, int offset, int count, int value)
        {
            if (!TryRangeCheck(buffer, offset, count, 5)) return false;

            unchecked
            {
                fixed (byte* p = &buffer[offset])
                {
                    p[4] = (byte)value;
                    p[3] = (byte)(value >> 8);
                    p[2] = (byte)(value >> 16);
                    p[1] = (byte)(value >> 24);
                    p[0] = MessagePackCode.Int32;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteArrayHeader(Span<byte> buffer, int count)
        {
            if (count <= MessagePackRange.MaxFixArrayCount)
            {
                if (buffer.Length < 1) Throw();
                buffer[0] = (byte)(MessagePackCode.MinFixArray | count);
                return 1;
            }
            else if (count <= ushort.MaxValue)
            {
                Throw(); // not implemented
            }
            else
            {
                Throw(); // not implemented
            }
            return -1; // not implemented
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteArrayHeader(byte[] buffer, int offset, int count, int arrayCount)
        {
            if (arrayCount <= MessagePackRange.MaxFixArrayCount)
            {
                RangeCheck(buffer, offset, count, 1);
                buffer[offset] = (byte)(MessagePackCode.MinFixArray | arrayCount);
                return 1;
            }
            else if (count <= ushort.MaxValue)
            {
                Throw(); // not implemented
            }
            else
            {
                Throw(); // not implemented
            }
            return -1; // not implemented
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteArrayHeader(Span<byte> buffer, int count, out int requireSize)
        {
            if (count <= MessagePackRange.MaxFixArrayCount)
            {
                requireSize = 1;
                if (buffer.Length < 1) return false;
                buffer[0] = (byte)(MessagePackCode.MinFixArray | count);
                return true;
            }
            else if (count <= ushort.MaxValue)
            {
                Throw(); // not implemented
            }
            else
            {
                Throw(); // not implemented
            }
            requireSize = 0;
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteArrayHeader(byte[] buffer, int offset, int count, int arrayCount, out int requireSize)
        {
            if (arrayCount <= MessagePackRange.MaxFixArrayCount)
            {
                requireSize = 1;
                if (!TryRangeCheck(buffer, offset, count, 1))
                {
                    return false;
                }
                buffer[offset] = (byte)(MessagePackCode.MinFixArray | arrayCount);
                return true;
            }
            else if (count <= ushort.MaxValue)
            {
                Throw(); // not implemented
            }
            else
            {
                Throw(); // not implemented
            }
            requireSize = 0;
            return false;
        }







        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void RangeCheck(byte[] array, int offset, int count, uint require)
        {
            if ((uint)offset > (uint)array.Length || (uint)count > (uint)(array.Length - offset))
            {
                Throw();
            }

            if ((uint)count < require)
            {
                Throw();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryRangeCheck(byte[] array, int offset, int count, uint require)
        {
            if ((uint)offset > (uint)array.Length || (uint)count > (uint)(array.Length - offset))
            {
                return false;
            }

            if ((uint)count < require)
            {
                return false;
            }
            return true;
        }

        static void Throw()
        {
            throw new ArgumentOutOfRangeException();
        }
    }
}
