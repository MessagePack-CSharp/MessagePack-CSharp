using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MessagePack
{
    /// <summary>
    /// A wrapper around <see cref="MemoryPool{T}.Shared"/> that ensures we never allocate arrays smaller than 4096.
    /// </summary>
    internal class MemoryPoolWithMinSize : MemoryPool<byte>
    {
        /// <summary>
        /// The minimum size for an array that we will ever request from the underlying pool.
        /// </summary>
        private const int MinSize = 4 * 1024;

        internal static readonly MemoryPoolWithMinSize Instance = new MemoryPoolWithMinSize();

        private MemoryPoolWithMinSize()
        {
        }

        /// <inheritdoc />
        public override int MaxBufferSize => throw new NotImplementedException();

        /// <inheritdoc />
        public override IMemoryOwner<byte> Rent(int minBufferSize = -1) => Shared.Rent(Math.Max(MinSize, minBufferSize));

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
        }
    }
}
