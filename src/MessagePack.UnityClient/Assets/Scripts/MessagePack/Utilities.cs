// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;

namespace MessagePack
{
    /// <summary>
    /// Internal utilities and extension methods for various external types.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// A value indicating whether we're running on mono.
        /// </summary>
#if UNITY_2018_3_OR_NEWER
        internal const bool IsMono = true; // hard code since we haven't tested whether mono is detected on all unity platforms.
#else
        internal static readonly bool IsMono = Type.GetType("Mono.Runtime") is Type;
#endif

        internal delegate void GetWriterBytesAction<TArg>(ref MessagePackWriter writer, TArg argument);

        internal static byte[] GetWriterBytes<TArg>(TArg arg, GetWriterBytesAction<TArg> action, SequencePool pool)
        {
            using (var sequenceRental = pool.Rent())
            {
                var writer = new MessagePackWriter(sequenceRental.Value);
                action(ref writer, arg);
                writer.Flush();
                return sequenceRental.Value.AsReadOnlySequence.ToArray();
            }
        }

        internal static Memory<T> GetMemoryCheckResult<T>(this IBufferWriter<T> bufferWriter, int size = 0)
        {
            var memory = bufferWriter.GetMemory(size);
            if (memory.IsEmpty)
            {
                throw new InvalidOperationException("The underlying IBufferWriter<byte>.GetMemory(int) method returned an empty memory block, which is not allowed. This is a bug in " + bufferWriter.GetType().FullName);
            }

            return memory;
        }
    }
}
