using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public static bool TryReadInt32(ReadOnlySpan<byte> source, out int value, out int readCount)
        {
            if (source.Length < 1)
            {
                value = default;
                readCount = 0;
                return false;
            }

            switch (source[0])
            {
                case MessagePackCode.Int32:
                    unchecked
                    {
                        if (source.Length < 5)
                        {
                            value = default;
                            readCount = 0;
                            return false;
                        }
                        else
                        {
                            value = (source[4]) | (source[3] << 8) | (source[2] << 16) | (source[1] << 24);
                            readCount = 5;
                            return true;
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryReadInt32(byte[] source, int offset, int count, out int value, out int readCount)
        {
            var len = source.Length - offset + count;

            if (len < 1)
            {
                value = default;
                readCount = 0;
                return false;
            }

            fixed (byte* buf = &source[offset])
            {
                switch (buf[0])
                {
                    case MessagePackCode.Int32:
                        unchecked
                        {
                            if (count < 5)
                            {
                                value = default;
                                readCount = 0;
                                return false;
                            }
                            else
                            {
                                value = (buf[4]) | (buf[3] << 8) | (buf[2] << 16) | (buf[1] << 24);
                                readCount = 5;
                                return true;
                            }
                        }
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
