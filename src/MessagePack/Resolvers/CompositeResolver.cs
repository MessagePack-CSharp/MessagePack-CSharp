using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Represents a collection of formatters and resolvers acting as one.
    /// </summary>
    public sealed class CompositeResolver : IFormatterResolver
    {
        private readonly Dictionary<Type, object> formattersByType = new Dictionary<Type, object>();
        private readonly List<IFormatterResolver> subResolvers = new List<IFormatterResolver>();

        public void RegisterResolver(IFormatterResolver resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            lock (this.subResolvers)
            {
                this.subResolvers.Add(resolver);
            }
        }

        /// <summary>
        /// Adds a formatter to this composite resolver.
        /// </summary>
        /// <param name="formatter">an object that implements <see cref="IMessagePackFormatter{T}"/> one or more times</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="formatter"/> does not implement any <see cref="IMessagePackFormatter{T}"/> interfaces.</exception>
        public void RegisterFormatter(object formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            bool foundAny = false;
            foreach (var implInterface in formatter.GetType().GetTypeInfo().ImplementedInterfaces)
            {
                var ti = implInterface.GetTypeInfo();
                if (ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IMessagePackFormatter<>))
                {
                    foundAny = true;
                    lock (this.formattersByType)
                    {
                        if (!this.formattersByType.ContainsKey(ti.GenericTypeArguments[0]))
                        {
                            this.formattersByType.Add(ti.GenericTypeArguments[0], formatter);
                        }
                    }
                }
            }

            if (!foundAny)
            {
                throw new ArgumentException("No formatters found on this object.", nameof(formatter));
            }
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            object formatter;
            lock (this.formattersByType)
            {
                if (this.formattersByType.TryGetValue(typeof(T), out formatter))
                {
                    return (IMessagePackFormatter<T>)formatter;
                }
            }

            lock (this.subResolvers)
            {
                foreach (var resolver in this.subResolvers)
                {
                    formatter = resolver.GetFormatter<T>();
                    if (formatter != null)
                    {
                        break;
                    }
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
