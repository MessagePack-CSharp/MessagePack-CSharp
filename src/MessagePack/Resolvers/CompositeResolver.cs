// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public sealed class CompositeResolver : IFormatterResolver
    {
        private readonly Dictionary<Type, object> formattersByType;
        private readonly List<IFormatterResolver> subResolvers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeResolver"/> class
        /// with no formatters or resolvers.
        /// </summary>
        public CompositeResolver()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeResolver"/> class
        /// with the supplied resolver that was pre-generated, and adds the standard non-AOT resolvers.
        /// </summary>
        /// <param name="aotResolver">An instance of the AOT code generated resolver.</param>
        /// <returns>
        /// The composite resolver that includes the AOT code generated resolver and several standard ones
        /// that the AOT code generator expects to be present.
        /// </returns>
        public static CompositeResolver CreateForAot(IFormatterResolver aotResolver)
        {
            var composite = new CompositeResolver();
            composite.RegisterResolver(aotResolver);

            composite.RegisterResolver(
                BuiltinResolver.Instance,
                AttributeFormatterResolver.Instance,
                PrimitiveObjectResolver.Instance);

            return composite;
        }

        /// <summary>
        /// Adds a resolver to this composite resolver.
        /// </summary>
        /// <param name="resolver">The resolver to add.</param>
        /// <remarks>
        /// Each registered resolver is appended to the end of the list of resolvers to try for each requested type.
        /// Registered formatters take precedence over all resolvers.
        /// </remarks>
        public void RegisterResolver(IFormatterResolver resolver)
        {
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            this.subResolvers.Add(resolver);
        }

        /// <summary>
        /// Adds resolvers to this composite resolver.
        /// </summary>
        /// <param name="resolvers">The resolvers to add.</param>
        /// <remarks>
        /// Each registered resolver is appended to the end of the list of resolvers to try for each requested type.
        /// Registered formatters take precedence over all resolvers.
        /// </remarks>
        public void RegisterResolver(params IFormatterResolver[] resolvers)
        {
            if (resolvers is null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            this.subResolvers.AddRange(resolvers);
        }

        /// <summary>
        /// Adds a formatter to this composite resolver.
        /// </summary>
        /// <param name="formatter">An object that implements <see cref="IMessagePackFormatter{T}"/> one or more times.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="formatter"/> does not implement any <see cref="IMessagePackFormatter{T}"/> interfaces.</exception>
        /// <remarks>
        /// Registered formatters always take precedence over registered resolvers.
        /// </remarks>
        public void RegisterFormatter(object formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            bool foundAny = false;
            foreach (Type implInterface in formatter.GetType().GetTypeInfo().ImplementedInterfaces)
            {
                TypeInfo ti = implInterface.GetTypeInfo();
                if (ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IMessagePackFormatter<>))
                {
                    foundAny = true;
                    if (!this.formattersByType.ContainsKey(ti.GenericTypeArguments[0]))
                    {
                        this.formattersByType.Add(ti.GenericTypeArguments[0], formatter);
                    }
                }
            }

            if (!foundAny)
            {
                throw new ArgumentException("No formatters found on this object.", nameof(formatter));
            }
        }

        /// <inheritdoc/>
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            // We use locks here because this resolver can be called by multiple threads concurrently
            // and we may be mutating some of these collections as we cache formatters per type.
            object formatter;
            lock (this.formattersByType)
            {
                if (this.formattersByType.TryGetValue(typeof(T), out formatter))
                {
                    return (IMessagePackFormatter<T>)formatter;
                }
            }

            foreach (IFormatterResolver resolver in this.subResolvers)
            {
                formatter = resolver.GetFormatter<T>();
                if (formatter != null)
                {
                    break;
                }
            }

            // Remember the answer for next time.
            lock (this.formattersByType)
            {
                if (!this.formattersByType.ContainsKey(typeof(T)))
                {
                    this.formattersByType.Add(typeof(T), formatter);
                }
            }

            return (IMessagePackFormatter<T>)formatter;
        }
    }
}
