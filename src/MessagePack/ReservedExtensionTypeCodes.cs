// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack
{
    /// <summary>
    /// The extension type codes that this library defines for just this library.
    /// </summary>
    public static class ReservedExtensionTypeCodes
    {
        /// <summary>
        /// For Unity's UnsafeBlitFormatter.
        /// </summary>
        public const sbyte UnityVector2 = 30;

        /// <summary>
        /// For Unity's UnsafeBlitFormatter.
        /// </summary>
        public const sbyte UnityVector3 = 31;

        /// <summary>
        /// For Unity's UnsafeBlitFormatter.
        /// </summary>
        public const sbyte UnityVector4 = 32;

        /// <summary>
        /// For Unity's UnsafeBlitFormatter.
        /// </summary>
        public const sbyte UnityQuaternion = 33;

        /// <summary>
        /// For Unity's UnsafeBlitFormatter.
        /// </summary>
        public const sbyte UnityColor = 34;

        /// <summary>
        /// For Unity's UnsafeBlitFormatter.
        /// </summary>
        public const sbyte UnityBounds = 35;

        /// <summary>
        /// For Unity's UnsafeBlitFormatter.
        /// </summary>
        public const sbyte UnityRect = 36;

        /// <summary>
        /// For Unity's UnsafeBlitFormatter.
        /// </summary>
        public const sbyte UnityInt = 37;

        /// <summary>
        /// For Unity's UnsafeBlitFormatter.
        /// </summary>
        public const sbyte UnityFloat = 38;

        /// <summary>
        /// For Unity's UnsafeBlitFormatter.
        /// </summary>
        public const sbyte UnityDouble = 39;

        /// <summary>
        /// The LZ4 array block compression extension.
        /// </summary>
        public const sbyte Lz4BlockArray = 98;

        /// <summary>
        /// The LZ4 single block compression extension.
        /// </summary>
        public const sbyte Lz4Block = 99;

        /// <summary>
        /// For the <see cref="Formatters.TypelessFormatter"/>.
        /// </summary>
        public const sbyte TypelessFormatter = 100;
    }
}
