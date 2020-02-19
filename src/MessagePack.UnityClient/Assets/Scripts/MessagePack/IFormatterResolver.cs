// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using MessagePack.Formatters;
using MessagePack.Internal;

namespace MessagePack
{
    /// <summary>
    /// Allows querying for a formatter for serializing or deserializing a particular <see cref="Type" />.
    /// </summary>
    public interface IFormatterResolver
    {
        /// <summary>
        /// Gets an <see cref="IMessagePackFormatter{T}"/> instance that can serialize or deserialize some type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to be serialized or deserialized.</typeparam>
        /// <returns>A formatter, if this resolver supplies one for type <typeparamref name="T"/>; otherwise <c>null</c>.</returns>
        IMessagePackFormatter<T> GetFormatter<T>();
    }

    public static class FormatterResolverExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IMessagePackFormatter<T> GetFormatterWithVerify<T>(this IFormatterResolver resolver)
        {
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            IMessagePackFormatter<T> formatter;
            try
            {
                formatter = resolver.GetFormatter<T>();
            }
            catch (TypeInitializationException ex)
            {
                // The fact that we're using static constructors to initialize this is an internal detail.
                // Rethrow the inner exception if there is one.
                // Do it carefully so as to not stomp on the original callstack.
                Throw(ex);
                return default; // not reachable
            }

            if (formatter == null)
            {
                Throw(typeof(T), resolver);
            }

            return formatter;
        }

        private static void Throw(TypeInitializationException ex)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
        }

        private static void Throw(Type t, IFormatterResolver resolver)
        {
            throw new FormatterNotRegisteredException(t.FullName + " is not registered in resolver: " + resolver.GetType());
        }

        private static readonly ThreadsafeTypeKeyHashTable<Func<IFormatterResolver, IMessagePackFormatter>> FormatterGetters =
            new ThreadsafeTypeKeyHashTable<Func<IFormatterResolver, IMessagePackFormatter>>();

        private static readonly MethodInfo GetFormatterRuntimeMethod = typeof(IFormatterResolver).GetRuntimeMethod(nameof(IFormatterResolver.GetFormatter), Type.EmptyTypes);

        public static object GetFormatterDynamic(this IFormatterResolver resolver, Type type)
        {
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!FormatterGetters.TryGetValue(type, out var formatterGetter))
            {
                var genericMethod = GetFormatterRuntimeMethod.MakeGenericMethod(type);
                var inputResolver = Expression.Parameter(typeof(IFormatterResolver), "inputResolver");
                formatterGetter = Expression.Lambda<Func<IFormatterResolver, IMessagePackFormatter>>(
                    Expression.Call(inputResolver, genericMethod), inputResolver).Compile();
                FormatterGetters.TryAdd(type, formatterGetter);
            }

            return formatterGetter(resolver);
        }

        internal static object GetFormatterDynamicWithVerify(this IFormatterResolver resolver, Type type)
        {
            var result = GetFormatterDynamic(resolver, type);
            if (result == null)
            {
                Throw(type, resolver);
            }

            return result;
        }
    }

    public class FormatterNotRegisteredException : MessagePackSerializationException
    {
        public FormatterNotRegisteredException(string message)
            : base(message)
        {
        }
    }
}
