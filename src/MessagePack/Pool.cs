using System;
using System.Collections.Generic;

namespace MessagePack
{
    /// <summary>
    /// A thread-safe, alloc-free reusable object pool.
    /// </summary>
    /// <typeparam name="T">The type of object to share.</typeparam>
    internal class Pool<T>
    {
        private readonly int maxSize;
        private readonly Func<T> valueFactory;
        private readonly Action<T> cleanup;
        private readonly Stack<T> pool = new Stack<T>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Pool{T}"/> class.
        /// </summary>
        /// <param name="maxSize">The maximum size to allow the pool to grow.</param>
        /// <param name="valueFactory">The value factory to use to create a new instance of <typeparamref name="T"/> when a rental is requested and the pool is depleted.</param>
        /// <param name="cleanup">An optional action to take on an instance of <typeparamref name="T"/> before returning to a pool.</param>
        internal Pool(int maxSize, Func<T> valueFactory, Action<T> cleanup)
        {
            this.maxSize = maxSize;
            this.valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
            this.cleanup = cleanup;
        }

        /// <summary>
        /// Gets an instance of <typeparamref name="T"/>.
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

            return new Rental(this, this.valueFactory());
        }

        private void Return(T value)
        {
            this.cleanup?.Invoke(value);
            lock (this.pool)
            {
                if (this.pool.Count < this.maxSize)
                {
                    this.pool.Push(value);
                }
            }
        }

        internal struct Rental : IDisposable
        {
            private readonly Pool<T> owner;

            internal Rental(Pool<T> owner, T value)
            {
                this.owner = owner;
                this.Value = value;
            }

            /// <summary>
            /// Gets the recyclable object.
            /// </summary>
            public T Value { get; }

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
