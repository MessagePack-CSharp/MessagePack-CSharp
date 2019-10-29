// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Represents a collection of formatters and resolvers acting as one.
    /// </summary>
    /// <remarks>
    /// This class is not thread-safe for mutations. It is thread-safe when not being written to.
    /// </remarks>
#if ENABLE_IL2CPP
    [Obsolete("CompositeResolver is not supported in IL2CPP, use StaticCompositeResolver instead.", false)]
#endif
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

            var formattersByType = new Dictionary<Type, IMessagePackFormatter>();
            foreach (IMessagePackFormatter formatter in formatters)
            {
                if (formatter == null)
                {
                    throw new ArgumentException("An element in the array is null.", nameof(formatters));
                }

                bool foundAny = false;
                foreach (Type implInterface in formatter.GetType().GetTypeInfo().ImplementedInterfaces)
                {
                    TypeInfo ti = implInterface.GetTypeInfo();
                    if (ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IMessagePackFormatter<>))
                    {
                        foundAny = true;
                        if (!formattersByType.ContainsKey(ti.GenericTypeArguments[0]))
                        {
                            formattersByType.Add(ti.GenericTypeArguments[0], formatter);
                        }
                    }
                }

                if (!foundAny)
                {
                    throw new ArgumentException("No formatters found on this object: " + formatter.GetType().FullName, nameof(formatter));
                }
            }

            // Make a copy of the resolvers list provided by the caller to guard against them changing it later.
            var immutableResolvers = new ReadOnlyCollection<IFormatterResolver>(resolvers.ToArray());

            // Return a type optimized for no formatters if applicable.
            return formattersByType.Count > 0
                ? (IFormatterResolver)new CachingResolver(new ReadOnlyDictionary<Type, IMessagePackFormatter>(formattersByType), immutableResolvers)
                : new CachingResolverWithNoFormatters(immutableResolvers);
        }

        public static IFormatterResolver Create(params IFormatterResolver[] resolvers) => Create(Array.Empty<IMessagePackFormatter>(), resolvers);

        public static IFormatterResolver Create(params IMessagePackFormatter[] formatters) => Create(formatters, Array.Empty<IFormatterResolver>());

        private class CachingResolver : CachingFormatterResolver
        {
            private readonly ReadOnlyDictionary<Type, IMessagePackFormatter> formattersByType;
            private readonly ReadOnlyCollection<IFormatterResolver> subResolvers;

            /// <summary>
            /// Initializes a new instance of the <see cref="CachingResolver"/> class.
            /// </summary>
            internal CachingResolver(ReadOnlyDictionary<Type, IMessagePackFormatter> formattersByType, ReadOnlyCollection<IFormatterResolver> subResolvers)
            {
                this.formattersByType = formattersByType ?? throw new ArgumentNullException(nameof(formattersByType));
                this.subResolvers = subResolvers ?? throw new ArgumentNullException(nameof(subResolvers));
            }

            /// <inheritdoc/>
            protected override IMessagePackFormatter<T> GetFormatterCore<T>()
            {
                if (!this.formattersByType.TryGetValue(typeof(T), out IMessagePackFormatter formatter))
                {
                    foreach (IFormatterResolver resolver in this.subResolvers)
                    {
                        formatter = resolver.GetFormatter<T>();
                        if (formatter != null)
                        {
                            break;
                        }
                    }
                }

                return (IMessagePackFormatter<T>)formatter;
            }
        }

        private class CachingResolverWithNoFormatters : CachingFormatterResolver
        {
            private readonly ReadOnlyCollection<IFormatterResolver> subResolvers;

            /// <summary>
            /// Initializes a new instance of the <see cref="CachingResolverWithNoFormatters"/> class.
            /// </summary>
            internal CachingResolverWithNoFormatters(ReadOnlyCollection<IFormatterResolver> subResolvers)
            {
                this.subResolvers = subResolvers ?? throw new ArgumentNullException(nameof(subResolvers));
            }

            /// <inheritdoc/>
            protected override IMessagePackFormatter<T> GetFormatterCore<T>()
            {
                foreach (IFormatterResolver resolver in this.subResolvers)
                {
                    IMessagePackFormatter<T> formatter = resolver.GetFormatter<T>();
                    if (formatter != null)
                    {
                        return (IMessagePackFormatter<T>)formatter;
                    }
                }

                return null;
            }
        }
    }
}
