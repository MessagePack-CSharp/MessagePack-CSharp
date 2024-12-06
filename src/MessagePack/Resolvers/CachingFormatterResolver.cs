// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;
using MessagePack.Internal;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// A base class for <see cref="IFormatterResolver"/> classes that want to cache their responses for perf reasons.
    /// </summary>
    internal abstract class CachingFormatterResolver : IFormatterResolver
    {
        /// <summary>
        /// The cache of types to their formatters.
        /// </summary>
        private readonly ThreadsafeTypeKeyHashTable<IMessagePackFormatter?> formatters = new();

        /// <inheritdoc />
        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            if (!this.formatters.TryGetValue(typeof(T), out IMessagePackFormatter? formatter))
            {
                formatter = this.GetFormatterCore<T>();
                this.formatters.TryAdd(typeof(T), formatter);
            }

            return (IMessagePackFormatter<T>?)formatter;
        }

        /// <summary>
        /// Looks up a formatter for a type that has not been previously cached.
        /// </summary>
        /// <typeparam name="T">The type to be formatted.</typeparam>
        /// <returns>The formatter to use, or <see langword="null"/> if none found.</returns>
        protected abstract IMessagePackFormatter<T>? GetFormatterCore<T>();
    }
}
