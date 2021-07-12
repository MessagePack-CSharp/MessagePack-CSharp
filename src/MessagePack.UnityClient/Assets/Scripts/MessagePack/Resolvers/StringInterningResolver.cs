// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Directs strings to be deserialized with the <see cref="StringInterningFormatter"/>.
    /// </summary>
    public sealed class StringInterningResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly StringInterningResolver Instance = new StringInterningResolver();

        private readonly StringInterningFormatter formatter = new StringInterningFormatter();

        private StringInterningResolver()
        {
        }

        /// <inheritdoc/>
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(string))
            {
                return (IMessagePackFormatter<T>)(object)this.formatter;
            }

            return null;
        }
    }
}
