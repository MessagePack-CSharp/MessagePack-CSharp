// Copyright (c) .NET Foundation. All rights reserved.
// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MessagePack
{
    /// <summary>
    /// A fast access struct that wraps <see cref="IBufferWriter{T}"/>.
    /// </summary>
    internal ref struct BufferWriter
    {
        /// <summary>
        /// The underlying <see cref="IBufferWriter{T}"/>.
        /// </summary>
        private IBufferWriter<byte> _output;

        /// <summary>
        /// The result of the last call to <see cref="IBufferWriter{T}.GetSpan(int)"/>, less any bytes already "consumed" with <see cref="Advance(int)"/>.
        /// Backing field for the <see cref="Span"/> property.
        /// </summary>
        private Span<byte> _span;

        /// <summary>
        /// The result of the last call to <see cref="IBufferWriter{T}.GetMemory(int)"/>, less any bytes already "consumed" with <see cref="Advance(int)"/>.
        /// </summary>
        private ArraySegment<byte> _segment;

        /// <summary>
        /// The number of uncommitted bytes (all the calls to <see cref="Advance(int)"/> since the last call to <see cref="Commit"/>).
        /// </summary>
        private int _buffered;

        /// <summary>
        /// The total number of bytes written with this writer.
        /// Backing field for the <see cref="BytesCommitted"/> property.
        /// </summary>
        private long _bytesCommitted;

        private SequencePool _sequencePool;

        private SequencePool.Rental _rental;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferWriter"/> struct.
        /// </summary>
        /// <param name="output">The <see cref="IBufferWriter{T}"/> to be wrapped.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferWriter(IBufferWriter<byte> output)
        {
            _buffered = 0;
            _bytesCommitted = 0;
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _sequencePool = default;
            _rental = default;

            var memory = _output.GetMemory();
            MemoryMarshal.TryGetArray(memory, out _segment);
            _span = memory.Span;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferWriter"/> struct.
        /// </summary>
        /// <param name="sequencePool">The pool from which to draw an <see cref="IBufferWriter{T}"/> if required..</param>
        /// <param name="array">An array to start with so we can avoid accessing the <paramref name="sequencePool"/> if possible.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal BufferWriter(SequencePool sequencePool, byte[] array)
        {
            _buffered = 0;
            _bytesCommitted = 0;
            _sequencePool = sequencePool ?? throw new ArgumentNullException(nameof(sequencePool));
            _rental = default;
            _output = null;

            _segment = new ArraySegment<byte>(array);
            _span = _segment.AsSpan();
        }

        /// <summary>
        /// Gets the result of the last call to <see cref="IBufferWriter{T}.GetSpan(int)"/>.
        /// </summary>
        public Span<byte> Span => _span;

        /// <summary>
        /// Gets the total number of bytes written with this writer.
        /// </summary>
        public long BytesCommitted => _bytesCommitted;

        /// <summary>
        /// Gets the <see cref="IBufferWriter{T}"/> underlying this instance.
        /// </summary>
        internal IBufferWriter<byte> UnderlyingWriter => _output;

        internal SequencePool.Rental SequenceRental => _rental; 

        public Span<byte> GetSpan(int sizeHint)
        {
            Ensure(sizeHint);
            return this.Span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte GetPointer(int sizeHint)
        {
            Ensure(sizeHint);

            if (_segment.Array != null)
            {
                return ref _segment.Array[_segment.Offset + _buffered];
            }
            else
            {
                return ref _span.GetPinnableReference();
            }
        }

        /// <summary>
        /// Calls <see cref="IBufferWriter{T}.Advance(int)"/> on the underlying writer
        /// with the number of uncommitted bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Commit()
        {
            var buffered = _buffered;
            if (buffered > 0)
            {
                this.MigrateToSequence();

                _bytesCommitted += buffered;
                _buffered = 0;
                _output.Advance(buffered);
                _span = default;
            }
        }

        /// <summary>
        /// Used to indicate that part of the buffer has been written to.
        /// </summary>
        /// <param name="count">The number of bytes written to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            _buffered += count;
            _span = _span.Slice(count);
        }

        /// <summary>
        /// Copies the caller's buffer into this writer and calls <see cref="Advance(int)"/> with the length of the source buffer.
        /// </summary>
        /// <param name="source">The buffer to copy in.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> source)
        {
            if (_span.Length >= source.Length)
            {
                source.CopyTo(_span);
                Advance(source.Length);
            }
            else
            {
                WriteMultiBuffer(source);
            }
        }

        /// <summary>
        /// Acquires a new buffer if necessary to ensure that some given number of bytes can be written to a single buffer.
        /// </summary>
        /// <param name="count">The number of bytes that must be allocated in a single buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ensure(int count = 1)
        {
            if (_span.Length < count)
            {
                EnsureMore(count);
            }
        }

        /// <summary>
        /// Gets the span to the bytes written if they were never committed to the underlying buffer writer.
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        internal bool TryGetUncommittedSpan(out ReadOnlySpan<byte> span)
        {
            if (this._sequencePool != null)
            {
                span = _segment.AsSpan(0, _buffered);
                return true;
            }

            span = default;
            return false;
        }

        /// <summary>
        /// Gets a fresh span to write to, with an optional minimum size.
        /// </summary>
        /// <param name="count">The minimum size for the next requested buffer.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureMore(int count = 0)
        {
            if (_buffered > 0)
            {
                Commit();
            }
            else
            {
                this.MigrateToSequence();
            }

            var memory = _output.GetMemory(count);
            MemoryMarshal.TryGetArray(memory, out _segment);
            _span = memory.Span;
        }

        /// <summary>
        /// Copies the caller's buffer into this writer, potentially across multiple buffers from the underlying writer.
        /// </summary>
        /// <param name="source">The buffer to copy into this writer.</param>
        private void WriteMultiBuffer(ReadOnlySpan<byte> source)
        {
            while (source.Length > 0)
            {
                if (_span.Length == 0)
                {
                    EnsureMore();
                }

                var writable = Math.Min(source.Length, _span.Length);
                source.Slice(0, writable).CopyTo(_span);
                source = source.Slice(writable);
                Advance(writable);
            }
        }

        private void MigrateToSequence()
        {
            if (this._sequencePool != null)
            {
                // We were writing to our private scratch memory, so we have to copy it into the actual writer.
                _rental = _sequencePool.Rent();
                _output = _rental.Value;
                var realSpan = _output.GetSpan(_buffered);
                _segment.AsSpan(0, _buffered).CopyTo(realSpan);
                _sequencePool = null;
            }
        }
    }
}
