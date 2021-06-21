using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MessagePackv3
{
    [StructLayout(LayoutKind.Auto)] // optimize layout
    public ref struct MessagePackWriter
    {
        Span<byte> bufferSpan;
        byte[]? bufferArray; // if null, used Span buffer
        int bufferArrayCount;
        int bytesWritten; // bytes written in buffer
        int totalWritten;

        // BufferWritter
        IBufferWriter<byte>? bufferWriter;

        // bitflags
        byte flags;
        const byte NoneFlag = 0b00000000;
        const byte UsingBufferWriterFlag = 0b00000001;
        const byte PreferArrayFlag = 0b00000010;
        bool UsingBufferWriter => (flags & UsingBufferWriterFlag) == UsingBufferWriterFlag;
        bool PreferArray => (flags & PreferArrayFlag) == PreferArrayFlag;

        // properties

        public int TotalWritten => totalWritten;

        Span<byte> WritableSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return bufferSpan.Slice(bytesWritten);
            }
        }

        public MessagePackWriter(IBufferWriter<byte> bufferWriter, bool preferArray = false)
        {
            this.bufferWriter = bufferWriter;
            this.bufferArray = null;
            this.bufferArrayCount = 0;
            this.bufferSpan = default;
            this.bytesWritten = 0;
            this.totalWritten = 0;
            this.flags = preferArray ? PreferArrayFlag : NoneFlag;
        }

        public MessagePackWriter(byte[] buffer)
        {
            this.bufferWriter = null;
            this.bufferArray = buffer;
            this.bufferArrayCount = buffer.Length;
            this.bufferSpan = default;
            this.bytesWritten = 0;
            this.totalWritten = 0;
            this.flags = PreferArrayFlag;
        }

        public MessagePackWriter(Span<byte> buffer)
        {
            this.bufferWriter = null;
            this.bufferArray = null;
            this.bufferArrayCount = 0;
            this.bufferSpan = buffer;
            this.bytesWritten = 0;
            this.totalWritten = 0;
            this.flags = NoneFlag;
        }

        public MessagePackWriter(IBufferWriter<byte> bufferWriter, Span<byte> initialBuffer)
        {
            this.bufferWriter = bufferWriter;
            this.bufferSpan = initialBuffer;
            this.bufferArray = null;
            this.bufferArrayCount = 0;
            this.bytesWritten = 0;
            this.totalWritten = 0;
            this.flags = NoneFlag;
        }

        public MessagePackWriter(IBufferWriter<byte> bufferWriter, byte[] initialBuffer)
        {
            this.bufferWriter = bufferWriter;
            this.bufferSpan = default;
            this.bufferArray = initialBuffer;
            this.bufferArrayCount = initialBuffer.Length;
            this.bytesWritten = 0;
            this.totalWritten = 0;
            this.flags = PreferArrayFlag;
        }

        public MessagePackWriter(IBufferWriter<byte> bufferWriter, byte[] initialBuffer, bool preferArray)
        {
            this.bufferWriter = bufferWriter;
            this.bufferSpan = default;
            this.bufferArray = initialBuffer;
            this.bufferArrayCount = initialBuffer.Length;
            this.bytesWritten = 0;
            this.totalWritten = 0;
            this.flags = preferArray ? PreferArrayFlag : NoneFlag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int value)
        {
            if (bufferArray == null)
            {
                if (!MessagePackPrimitives.TryWriteInt32(WritableSpan, value))
                {
                    EnsureAndWriteInt32(value);
                }
            }
            else
            {
                if (!MessagePackPrimitives.TryWriteInt32(bufferArray, bytesWritten, bufferArrayCount, value))
                {
                    EnsureAndWriteInt32(value);
                }
                else
                {
                    bufferArrayCount -= 5;
                }
            }

            bytesWritten += 5;
            totalWritten += 5;
        }

        void EnsureAndWriteInt32(int value)
        {
            Ensure(5);
            if (bufferArray == null)
            {
                MessagePackPrimitives.WriteInt32(WritableSpan, value);
            }
            else
            {
                MessagePackPrimitives.WriteInt32(bufferArray, bytesWritten, bufferArrayCount, value);
                bufferArrayCount -= 5;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayHeader(int count)
        {
            int size;
            if (bufferArray == null)
            {
                if (!MessagePackPrimitives.TryWriteArrayHeader(WritableSpan, count, out size))
                {
                    size = EnsureAndWriteArrayHeader(size, count);
                }
            }
            else
            {
                if (!MessagePackPrimitives.TryWriteArrayHeader(bufferArray, bytesWritten, bufferArrayCount, count, out size))
                {
                    size = EnsureAndWriteArrayHeader(size, count);
                }
                else
                {
                    bufferArrayCount -= size;
                }
            }

            bytesWritten += size;
            totalWritten += size;
        }

        int EnsureAndWriteArrayHeader(int size, int count)
        {
            Ensure(size);
            if (bufferArray == null)
            {
                return MessagePackPrimitives.WriteArrayHeader(WritableSpan, count);
            }
            else
            {
                var newSize = MessagePackPrimitives.WriteArrayHeader(bufferArray, bytesWritten, bufferArrayCount, count);
                bufferArrayCount -= newSize;
                return newSize;
            }
        }

        public void Flush()
        {
            if (UsingBufferWriter)
            {
                Debug.Assert(bufferWriter != null);
                bufferWriter.Advance(bytesWritten);
                bytesWritten = 0;
            }
        }

        internal byte[] FlushAndGetArray()
        {
            if (UsingBufferWriter)
            {
                //
                // if(bufferWriter is Sequence<byte>)
                // ...
                throw new NotImplementedException();
            }
            else
            {
                var result = new byte[bytesWritten];
                if (bufferArray == null)
                {
                    bufferSpan.Slice(bytesWritten).CopyTo(result);
                }
                else
                {
                    Buffer.BlockCopy(bufferArray, 0, result, 0, bytesWritten);
                }
                return result;
            }
        }

        void Ensure(int count)
        {
            if (UsingBufferWriter)
            {
                Debug.Assert(bufferWriter != null);
                bufferWriter.Advance(bytesWritten);
                SetNewBuffer(count);
            }
            else
            {
                // migrate to buffer-writer
                if (bufferWriter == null)
                {
                    throw new Exception("TODO: msg, should set bufferwriter");
                }

                if (bytesWritten != 0)
                {
                    var newBuffer = bufferWriter.GetSpan(bytesWritten);
                    bufferSpan.Slice(bytesWritten).CopyTo(newBuffer);
                    bufferWriter.Advance(bytesWritten);
                    bytesWritten = 0;
                }

                flags |= UsingBufferWriterFlag;
                SetNewBuffer(count);
            }
        }

        void SetNewBuffer(int count)
        {
            Debug.Assert(bufferWriter != null);

            if (PreferArray)
            {
                var memory = bufferWriter.GetMemory(count);
                if (MemoryMarshal.TryGetArray<byte>(memory, out var segment))
                {
                    bufferArray = segment.Array;
                    bufferArrayCount = segment.Count;
                    bytesWritten = segment.Offset;
                }
                else
                {
                    bufferSpan = memory.Span;
                    bytesWritten = 0;
                }
            }
            else
            {
                bufferSpan = bufferWriter.GetSpan(count);
                bytesWritten = 0;
            }
        }
    }
}
