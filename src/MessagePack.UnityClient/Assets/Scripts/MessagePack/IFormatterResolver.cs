// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using MessagePack.Formatters;

namespace MessagePack
{
    public interface IFormatterResolver
    {
        IMessagePackFormatter<T> GetFormatter<T>();
    }

    public static class FormatterResolverExtensions
    {
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
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                throw new InvalidOperationException("Unreachable"); // keep the compiler happy
            }

            if (formatter == null)
            {
                throw new FormatterNotRegisteredException(typeof(T).FullName + " is not registered in this resolver. resolver:" + resolver.GetType().Name);
            }

            return formatter;
        }

#if !UNITY_2018_3_OR_NEWER

        public static object GetFormatterDynamic(this IFormatterResolver resolver, Type type)
        {
            MethodInfo methodInfo = typeof(IFormatterResolver).GetRuntimeMethod(nameof(IFormatterResolver.GetFormatter), Type.EmptyTypes);

            var formatter = methodInfo.MakeGenericMethod(type).Invoke(resolver, null);
            return formatter;
        }

#endif
    }

    public class FormatterNotRegisteredException : Exception
    {
        public FormatterNotRegisteredException(string message)
            : base(message)
        {
        }
    }
}
