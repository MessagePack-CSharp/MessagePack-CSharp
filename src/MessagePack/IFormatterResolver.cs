﻿
using MessagePack.Formatters;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;

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
                Exception inner = ex;
                while (inner.InnerException != null)
                {
                    inner = inner.InnerException;
                }

                ExceptionDispatchInfo.Capture(inner).Throw(); // rethrow
                throw inner; // this is never called
            }

            if (formatter == null)
            {
                throw new FormatterNotRegisteredException(typeof(T).FullName + " is not registered in this resolver. resolver:" + resolver.GetType().Name);
            }

            return formatter;
        }

#if !UNITY_WSA

        public static object GetFormatterDynamic(this IFormatterResolver resolver, Type type)
        {
            var methodInfo = typeof(IFormatterResolver).GetRuntimeMethod("GetFormatter", Type.EmptyTypes);

            var formatter = methodInfo.MakeGenericMethod(type).Invoke(resolver, null);
            return formatter;
        }

#endif
    }

    public class FormatterNotRegisteredException : Exception
    {
        public FormatterNotRegisteredException(string message) : base(message)
        {
        }
    }
}