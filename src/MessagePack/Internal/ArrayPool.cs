using System;

namespace MessagePack.Internal
{
    internal sealed class BufferPool : ArrayPool<byte>
    {
        public static readonly BufferPool Default = new BufferPool(65535);

        public BufferPool(int bufferLength)
            : base(bufferLength)
        {
        }
    }

    internal class ArrayPool<T>
    {
        readonly int bufferLength;
        readonly object gate;
        int index;
        T[][] buffers;

        public ArrayPool(int bufferLength)
        {
            this.bufferLength = bufferLength;
            this.buffers = new T[4][];
            this.gate = new object();
        }

        public T[] Rent()
        {
            lock (gate)
            {
                if (index >= buffers.Length)
                {
                    Array.Resize(ref buffers, buffers.Length * 2);
                }

                if (buffers[index] == null)
                {
                    buffers[index] = new T[bufferLength];
                }

                var buffer = buffers[index];
                buffers[index] = null;
                index++;

                return buffer;
            }
        }

        public void Return(T[] array)
        {
            if (array.Length != bufferLength)
            {
                throw new InvalidOperationException("return buffer is not from pool");
            }

            lock (gate)
            {
                if (index != 0)
                {
                    buffers[--index] = array;
                }
            }
        }
    }
}
