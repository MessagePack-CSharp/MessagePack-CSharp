using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MessagePackv3
{
    [StructLayout(LayoutKind.Auto)] // optimize layout
    public ref struct MessagePackReader
    {
        byte[]? currentArray;
        int arrayCount;
        ReadOnlySpan<byte> currentSpan;

        int currentReadIndex;

        ReadOnlySequence<byte> sequence;

        ReadOnlySpan<byte> ReadableSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return currentSpan.Slice(currentReadIndex);
            }
        }

        public MessagePackReader(ReadOnlySequence<byte> sequence, bool preferArray = false)
        {
            this.sequence = sequence;
            if (preferArray)
            {
                var memory = sequence.First;
                if (MemoryMarshal.TryGetArray(memory, out var segment))
                {
                    if (segment.Array != null)
                    {
                        currentArray = segment.Array;
                        arrayCount = segment.Count;
                        currentReadIndex = segment.Offset;
                        currentSpan = default;
                        return;
                    }
                }

                currentArray = null;
                arrayCount = 0;
                currentReadIndex = 0;
                currentSpan = memory.Span;
            }
            else
            {
                currentArray = null;
                arrayCount = 0;
                currentReadIndex = 0;
#if NET5_0
                currentSpan = sequence.FirstSpan;
#else
                currentSpan = sequence.First.Span;
#endif
            }
        }

        public MessagePackReader(ReadOnlySpan<byte> span)
        {
            this.sequence = default;
            currentArray = null;
            arrayCount = 0;
            currentReadIndex = 0;
            currentSpan = span;
        }

        public MessagePackReader(byte[] source)
        {
            this.sequence = default;
            currentArray = source;
            arrayCount = source.Length;
            currentReadIndex = 0;
            currentSpan = default;
        }

        public int ReadInt32()
        {
            int value;
            int readCount;
            if (currentArray == null)
            {
                if (!MessagePackPrimitives.TryReadInt32(ReadableSpan, out value, out readCount))
                {
                    ReadMore(); // TODO:not implemented.
                    value = 0;
                    readCount = 0;
                }
            }
            else
            {
                if (!MessagePackPrimitives.TryReadInt32(currentArray, currentReadIndex, arrayCount, out value, out readCount))
                {
                    ReadMore(); // TODO:not implemented.
                    value = 0;
                    readCount = 0;
                }
            }

            currentReadIndex += readCount;
            return value;
        }

        void ReadMore()
        {
            // TODO: read next sequnece value
            throw new NotImplementedException();
        }

    }
}
