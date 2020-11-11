// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;

namespace MessagePack
{
    public static class Pool
    {
        /// <summary>
        /// Reinitializes internal shared pool.
        /// </summary>
        /// <param name="maxSize">The maximum size to allow the pool to grow. Default value is "<see cref="System.Environment.ProcessorCount"/> * 2".</param>
        /// <param name="arrayPool">Array pool that will be used.</param>
        public static void Use(int maxSize, ArrayPool<byte> arrayPool) =>
            SequencePool.Shared = new SequencePool(maxSize, arrayPool);
    }
}
