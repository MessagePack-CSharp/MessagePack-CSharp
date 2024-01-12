// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack
{
    /// <summary>
    /// Identifies the various compression schemes which might be applied at the msgpack level.
    /// </summary>
    public enum MessagePackCompression
    {
        /// <summary>
        /// No compression is applied at the msgpack level.
        /// </summary>
        None,

        /// <summary>
        /// Compresses an entire msgpack sequence as a single lz4 block format.
        /// This is the simple compression that achieves best compression ratio,
        /// at the cost of copying the entire sequence when necessary to get contiguous memory.
        /// </summary>
        /// <remarks>
        /// Uses msgpack type code ext99 and is compatible with v1 of this library.
        /// </remarks>
        /// <devremarks>
        /// See also ThisLibraryExtensionTypeCodes.Lz4Block.
        /// </devremarks>
        Lz4Block,

        /// <summary>
        /// Compresses an entire msgpack sequence as a array of lz4 block format.
        /// This is compressed/decompressed in chunks that do not consume LOH,
        /// but the compression ratio is slightly sacrificed.
        /// </summary>
        /// <remarks>
        /// Uses msgpack type code ext98 in array.
        /// </remarks>
        /// <devremarks>
        /// See also ThisLibraryExtensionTypeCodes.Lz4BlockArray.
        /// </devremarks>
        Lz4BlockArray,
    }

#pragma warning disable SA1649 // File name should match first type name

    /// <summary>
    /// Extensions for <see cref="MessagePackCompression"/>.
    /// </summary>
    internal static class MessagePackCompressionExtensions
    {
        public static bool IsCompression(this MessagePackCompression compression)
        {
            return compression != MessagePackCompression.None;
        }
    }

#pragma warning restore SA1649 // File name should match first type name
}
