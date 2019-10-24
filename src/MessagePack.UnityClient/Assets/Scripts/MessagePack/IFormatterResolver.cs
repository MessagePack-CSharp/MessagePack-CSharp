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
    public interface IFormatterResolver
    {
        /// <summary>
        /// Gets an <see cref="IMessagePackFormatter{T}"/> instance that can serialize or deserialize some type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to be serialized or deserialized.</typeparam>
        /// <param name="state">State associated with this particular serialization or deserialization operation.</param>
        /// <returns>A formatter, if this resolver supplies one for type <typeparamref name="T"/>; otherwise <c>null</c>.</returns>
        IMessagePackFormatter<T> GetFormatter<T>(ref SerializationState state);
    }

    public static class FormatterResolverExtensions
    {
        private static readonly Type[] DelegateCacheTypeArray = new Type[1];

        private static readonly Type[] GetFormatterParameterTypes = new Type[] { typeof(IFormatterResolver), typeof(SerializationState).MakeByRefType() };

        private delegate object GetFormatterDelegate(IFormatterResolver resolver, ref SerializationState state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IMessagePackFormatter<T> GetFormatterWithVerify<T>(this IFormatterResolver resolver, ref SerializationState state)
        {
            IMessagePackFormatter<T> formatter;
            try
            {
                formatter = resolver.GetFormatter<T>(ref state);
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

        public static object GetFormatterDynamic(this IFormatterResolver resolver, Type type, ref SerializationState state)
        {
            var cache = typeof(DelegateCache<>).MakeGenericType(type);
            var del = (GetFormatterDelegate)cache.GetField(nameof(DelegateCache<int>.MyDelegate)).GetValue(null);
            var formatter = del(resolver, ref state);
            return formatter;
        }

        private static class DelegateCache<T>
        {
            internal static readonly GetFormatterDelegate MyDelegate;

            static DelegateCache()
            {
                MethodInfo methodInfo = typeof(DelegateCache<T>).GetRuntimeMethod(nameof(GetFormatterDynamic), GetFormatterParameterTypes);
                MyDelegate = (GetFormatterDelegate)methodInfo.CreateDelegate(typeof(GetFormatterDelegate));
            }

            private static object GetFormatterDynamic(IFormatterResolver resolver, ref SerializationState state) => resolver.GetFormatter<T>(ref state);
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
