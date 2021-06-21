// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MessagePack.Formatters;
using MessagePack.Internal;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Represents a collection of formatters and resolvers acting as one.
    /// </summary>
    /// <remarks>
    /// This class is not thread-safe for mutations. It is thread-safe when not being written to.
    /// </remarks>
    public static class CompositeResolver
    {
        private static readonly ReadOnlyDictionary<Type, IMessagePackFormatter> EmptyFormattersByType = new ReadOnlyDictionary<Type, IMessagePackFormatter>(new Dictionary<Type, IMessagePackFormatter>());

        /// <summary>
        /// Initializes a new instance of an <see cref="IFormatterResolver"/> with the specified formatters and sub-resolvers.
        /// </summary>
        /// <param name="formatters">
        /// A list of instances of <see cref="IMessagePackFormatter{T}"/> to prefer (above the <paramref name="resolvers"/>).
        /// The formatters are searched in the order given, so if two formatters support serializing the same type, the first one is used.
        /// May not be null, but may be <see cref="Array.Empty{T}"/>.
        /// </param>
        /// <param name="resolvers">
        /// A list of resolvers to use for serializing types for which <paramref name="formatters"/> does not include a formatter.
        /// The resolvers are searched in the order given, so if two resolvers support serializing the same type, the first one is used.
        /// May not be null, but may be <see cref="Array.Empty{T}"/>.
        /// </param>
        /// <returns>
        /// An instance of <see cref="IFormatterResolver"/>.
        /// </returns>
        public static IFormatterResolver Create(IReadOnlyList<IMessagePackFormatter> formatters, IReadOnlyList<IFormatterResolver> resolvers)
        {
            if (formatters is null)
            {
                throw new ArgumentNullException(nameof(formatters));
            }

            if (resolvers is null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            // Make a copy of the resolvers list provided by the caller to guard against them changing it later.
            var immutableFormatters = formatters.ToArray();
            var immutableResolvers = resolvers.ToArray();

            return new CachingResolver(immutableFormatters, immutableResolvers);
        }

        public static IFormatterResolver Create(params IFormatterResolver[] resolvers) => Create(Array.Empty<IMessagePackFormatter>(), resolvers);

        public static IFormatterResolver Create(params IMessagePackFormatter[] formatters) => Create(formatters, Array.Empty<IFormatterResolver>());

        private class CachingResolver : IFormatterResolver
        {
            private readonly ThreadsafeTypeKeyHashTable<IMessagePackFormatter> formattersCache = new ThreadsafeTypeKeyHashTable<IMessagePackFormatter>();
            private readonly IMessagePackFormatter[] subFormatters;
            private readonly IFormatterResolver[] subResolvers;

            /// <summary>
            /// Initializes a new instance of the <see cref="CachingResolver"/> class.
            /// </summary>
            internal CachingResolver(IMessagePackFormatter[] subFormatters, IFormatterResolver[] subResolvers)
            {
                this.subFormatters = subFormatters ?? throw new ArgumentNullException(nameof(subFormatters));
                this.subResolvers = subResolvers ?? throw new ArgumentNullException(nameof(subResolvers));
            }

            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                if (!this.formattersCache.TryGetValue(typeof(T), out IMessagePackFormatter formatter))
                {
                    foreach (var subFormatter in this.subFormatters)
                    {
                        if (subFormatter is IMessagePackFormatter<T>)
                        {
                            formatter = subFormatter;
                            goto CACHE;
                        }
                    }

                    foreach (IFormatterResolver resolver in this.subResolvers)
                    {
                        formatter = resolver.GetFormatter<T>();
                        if (formatter != null)
                        {
                            goto CACHE;
                        }
                    }

// when not found, cache null.
CACHE:
                    this.formattersCache.TryAdd(typeof(T), formatter);
                }

                return (IMessagePackFormatter<T>)formatter;
            }
        }
    }
}
