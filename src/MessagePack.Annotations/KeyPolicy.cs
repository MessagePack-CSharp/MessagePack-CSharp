// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack
{
    /// <summary>
    /// Key Policy.
    /// </summary>
    public enum KeyPolicy
    {
        /// <summary>
        /// Each property and field must be attributed with <see cref="KeyAttribute"/> or <see cref="IgnoreMemberAttribute"/>.
        /// </summary>
        Explicit,

        /// <summary>
        /// <see langword="public" /> and <see langword="internal" /> properties and fields are serialized using their name as the key in a map.
        /// </summary>
        ImplicitPropertyNames,

        /// <summary>
        /// <see langword="public" /> and <see langword="internal" /> properties and fields are serialized using a camelCase transformation of their name as the key in a map.
        /// </summary>
        ImplicitCamelCasePropertyNames,
    }
}
