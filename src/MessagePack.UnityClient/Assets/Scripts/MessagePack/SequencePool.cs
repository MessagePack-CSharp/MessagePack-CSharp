// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using Nerdbank.Streams;

namespace MessagePack
{
    /// <summary>
    /// A thread-safe, alloc-free reusable object pool.
    /// </summary>
    internal class SequencePool
    {
        /// <summary>
        /// The value to use for <see cref="Sequence{T}.MinimumSpanLength"/>.
        /// </summary>
        /// <remarks>
        /// Individual users that want a different value for this can modify the setting on the rented <see cref="Sequence{T}"/>
        /// or by supplying their own <see cref="IBufferWriter{T}" />.
        /// </remarks>
        private const int MinimumSpanLength = 64 * 1024;

        private readonly int maxSize;
        private readonly Stack<Sequence<byte>> pool = new Stack<Sequence<byte>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SequencePool"/> class.
        /// </summary>
        /// <param name="maxSize">The maximum size to allow the pool to grow.</param>
        internal SequencePool(int maxSize)
        {
            this.maxSize = maxSize;
        }

        /// <summary>
        /// Gets an instance of <see cref="Sequence{T}"/>
        /// This is taken from the recycled pool if one is available; otherwise a new one is created.
        /// </summary>
        /// <returns>The rental tracker that provides access to the object as well as a means to return it.</returns>
        internal Rental Rent()
        {
            lock (this.pool)
            {
                if (this.pool.Count > 0)
                {
                    return new Rental(this, this.pool.Pop());
                }
            }

            return new Rental(this, new Sequence<byte> { MinimumSpanLength = MinimumSpanLength });
        }

        private void Return(Sequence<byte> value)
        {
            value.Reset();
            lock (this.pool)
            {
                if (this.pool.Count < this.maxSize)
                {
                    // Reset to preferred settings in case the renter changed them.
                    value.MinimumSpanLength = MinimumSpanLength;

                    this.pool.Push(value);
                }
            }
        }

        internal struct Rental : IDisposable
        {
            private readonly SequencePool owner;

            internal Rental(SequencePool owner, Sequence<byte> value)
            {
                this.owner = owner;
                this.Value = value;
            }

            /// <summary>
            /// Gets the recyclable object.
            /// </summary>
            public Sequence<byte> Value { get; }

            /// <summary>
            /// Returns the recyclable object to the pool.
            /// </summary>
            /// <remarks>
            /// The instance is cleaned first, if a clean delegate was provided.
            /// It is dropped instead of being returned to the pool if the pool is already at its maximum size.
            /// </remarks>
            public void Dispose()
            {
                this.owner.Return(this.Value);
            }
        }
    }
}
