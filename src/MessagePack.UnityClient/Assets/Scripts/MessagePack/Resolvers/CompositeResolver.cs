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
    public sealed class CompositeResolver
    {
        private static readonly ReadOnlyDictionary<Type, object> EmptyFormattersByType = new ReadOnlyDictionary<Type, object>(new Dictionary<Type, object>());

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeResolver"/> class
        /// with the supplied resolver that was pre-generated, and adds the standard non-AOT resolvers.
        /// </summary>
        /// <param name="aotResolver">An instance of the AOT code generated resolver.</param>
        /// <returns>
        /// The composite resolver that includes the AOT code generated resolver and several standard ones
        /// that the AOT code generator expects to be present.
        /// </returns>
        public static IFormatterResolver CreateForAot(IFormatterResolver aotResolver)
        {
            if (aotResolver is null)
            {
                throw new ArgumentNullException(nameof(aotResolver));
            }

            return new CachingResolver(
                EmptyFormattersByType,
                new ReadOnlyCollection<IFormatterResolver>(
                    new[]
                    {
                        aotResolver,
                        BuiltinResolver.Instance,
                        AttributeFormatterResolver.Instance,
                        PrimitiveObjectResolver.Instance,
                    }));
        }

        public static IFormatterResolver Create(IReadOnlyList<object> formatters, IReadOnlyList<IFormatterResolver> resolvers)
        {
            if (formatters is null)
            {
                throw new ArgumentNullException(nameof(formatters));
            }

            if (resolvers is null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            var formattersByType = new Dictionary<Type, object>();
            foreach (var formatter in formatters)
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
            return new CachingResolver(
                new ReadOnlyDictionary<Type, object>(formattersByType),
                new ReadOnlyCollection<IFormatterResolver>(resolvers.ToArray()));
        }

        private class CachingResolver : CachingFormatterResolver
        {
            private readonly ReadOnlyDictionary<Type, object> formattersByType;
            private readonly ReadOnlyCollection<IFormatterResolver> subResolvers;

            /// <summary>
            /// Initializes a new instance of the <see cref="CachingResolver"/> class.
            /// </summary>
            internal CachingResolver(ReadOnlyDictionary<Type, object> formattersByType, ReadOnlyCollection<IFormatterResolver> subResolvers)
            {
                this.formattersByType = formattersByType ?? throw new ArgumentNullException(nameof(formattersByType));
                this.subResolvers = subResolvers ?? throw new ArgumentNullException(nameof(subResolvers));
            }

            /// <inheritdoc/>
            protected override IMessagePackFormatter<T> GetFormatterCore<T>()
            {
                if (!this.formattersByType.TryGetValue(typeof(T), out object formatter))
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
    }
}
