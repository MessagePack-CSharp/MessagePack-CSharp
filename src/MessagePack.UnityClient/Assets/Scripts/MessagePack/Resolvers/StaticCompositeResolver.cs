// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Singleton version of <see cref="CompositeResolver"/>, which can register a collection of formatters and resolvers to a single instance.
    /// </summary>
    public class StaticCompositeResolver : IFormatterResolver
    {
        public static readonly StaticCompositeResolver Instance = new StaticCompositeResolver();

        private bool freezed;
        private IReadOnlyList<IMessagePackFormatter> formatters;
        private IReadOnlyList<IFormatterResolver> resolvers;

        private StaticCompositeResolver()
        {
            formatters = Array.Empty<IMessagePackFormatter>();
            resolvers = Array.Empty<IFormatterResolver>();
        }

        /// <summary>
        /// Initializes a singleton instance with the specified formatters.
        /// This method can only call before use StaticCompositeResolver.Instance.GetFormatter.
        /// If call twice in the Register methods, registered formatters and resolvers will be overridden.
        /// </summary>
        /// <param name="formatters">
        /// A list of instances of <see cref="IMessagePackFormatter{T}"/>.
        /// The formatters are searched in the order given, so if two formatters support serializing the same type, the first one is used.
        /// </param>
        public void Register(params IMessagePackFormatter[] formatters)
        {
            if (this.freezed)
            {
                throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
            }

            if (formatters is null)
            {
                throw new ArgumentNullException(nameof(formatters));
            }

            this.formatters = formatters;
            this.resolvers = Array.Empty<IFormatterResolver>();
        }

        /// <summary>
        /// Initializes a singleton instance with the specified formatters and sub-resolvers.
        /// This method can only call before use StaticCompositeResolver.Instance.GetFormatter.
        /// If call twice in the Register methods, registered formatters and resolvers will be overridden.
        /// </summary>
        /// <param name="resolvers">
        /// A list of resolvers to use for serializing types.
        /// The resolvers are searched in the order given, so if two resolvers support serializing the same type, the first one is used.
        /// </param>
        public void Register(params IFormatterResolver[] resolvers)
        {
            if (this.freezed)
            {
                throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
            }

            if (resolvers is null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            this.formatters = Array.Empty<IMessagePackFormatter>();
            this.resolvers = resolvers;
        }

        /// <summary>
        /// Initializes a singleton instance with the specified formatters and sub-resolvers.
        /// This method can only call before use StaticCompositeResolver.Instance.GetFormatter.
        /// If call twice in the Register methods, registered formatters and resolvers will be overridden.
        /// </summary>
        /// <param name="formatters">
        /// A list of instances of <see cref="IMessagePackFormatter{T}"/>.
        /// The formatters are searched in the order given, so if two formatters support serializing the same type, the first one is used.
        /// </param>
        /// <param name="resolvers">
        /// A list of resolvers to use for serializing types for which <paramref name="formatters"/> does not include a formatter.
        /// The resolvers are searched in the order given, so if two resolvers support serializing the same type, the first one is used.
        /// </param>
        public void Register(IReadOnlyList<IMessagePackFormatter> formatters, IReadOnlyList<IFormatterResolver> resolvers)
        {
            if (this.freezed)
            {
                throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
            }

            if (formatters is null)
            {
                throw new ArgumentNullException(nameof(formatters));
            }

            if (resolvers is null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            this.formatters = formatters;
            this.resolvers = resolvers;
        }

        /// <summary>
        /// Gets an <see cref="IMessagePackFormatter{T}"/> instance that can serialize or deserialize some type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to be serialized or deserialized.</typeparam>
        /// <returns>A formatter, if this resolver supplies one for type <typeparamref name="T"/>; otherwise <see langword="null"/>.</returns>
        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }

        private static class Cache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            static Cache()
            {
                Instance.freezed = true;
                foreach (var item in Instance.formatters)
                {
                    if (item is IMessagePackFormatter<T> f)
                    {
                        Formatter = f;
                        return;
                    }
                }

                foreach (var item in Instance.resolvers)
                {
                    var f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        Formatter = f;
                        return;
                    }
                }
            }
        }
    }
}
