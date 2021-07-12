// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack
{
    /// <summary>
    /// Reuses previously created <see cref="string"/> objects wherever possible
    /// to reduce memory allocations while deserializing msgpack sequences.
    /// </summary>
    /// <remarks>
    /// This class is thread-safe.
    /// Derived types are also expected to be thread-safe.
    /// </remarks>
    public class InternedStringCollection
    {
        /// <summary>
        /// Reads a string from a given <see cref="MessagePackReader"/>.
        /// </summary>
        /// <param name="reader">The reader to read the string from.</param>
        /// <returns>A string that matches the value at the caller's position on the reader.</returns>
        /// <remarks>
        /// The default implementation simply interns all strings using <see cref="string.Intern(string)"/>.
        /// This method may be overridden to:
        /// 1) be more selective about which strings should be interned,
        /// 2) change where interned strings are stored,
        /// 3) avoid the initial string allocation and go straight to the interned string.
        /// </remarks>
        public virtual string GetString(ref MessagePackReader reader)
        {
            string value = reader.ReadString();
            return value is null ? null : string.Intern(value);
        }
    }
}
