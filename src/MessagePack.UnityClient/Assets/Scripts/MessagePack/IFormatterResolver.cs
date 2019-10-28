// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using MessagePack.Formatters;

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
            throw new FormatterNotRegisteredException(t.FullName + " is not registered in this resolver. resolver:" + resolver.GetType().Name);
        }

        public static object GetFormatterDynamic(this IFormatterResolver resolver, Type type)
        {
            MethodInfo methodInfo = typeof(IFormatterResolver).GetRuntimeMethod(nameof(IFormatterResolver.GetFormatter), Type.EmptyTypes);

            var formatter = methodInfo.MakeGenericMethod(type).Invoke(resolver, null);
            return formatter;
        }
    }

    public class FormatterNotRegisteredException : Exception
    {
        public FormatterNotRegisteredException(string message)
            : base(message)
        {
        }
    }
}
