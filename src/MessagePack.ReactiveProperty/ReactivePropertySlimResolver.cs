using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reflection;
using System.Text;
using MessagePack.Formatters;
using Reactive.Bindings;

namespace MessagePack.ReactivePropertyExtension
{
    public class ReactivePropertySlimResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new ReactivePropertySlimResolver();

        ReactivePropertySlimResolver()
        {

        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = (IMessagePackFormatter<T>)ReactivePropertySlimResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }

    internal static class ReactivePropertySlimResolverGetFormatterHelper
    {
        static readonly Dictionary<Type, Type> formatterMap = new Dictionary<Type, Type>()
        {
             {typeof(ReactivePropertySlim<>), typeof(ReactivePropertySlimFormatter<>)},
        };

        internal static object GetFormatter(Type t)
        {
            var ti = t.GetTypeInfo();

            if (ti.IsGenericType)
            {
                var genericType = ti.GetGenericTypeDefinition();
                if (formatterMap.TryGetValue(genericType, out Type formatterType))
                {
                    return CreateInstance(formatterType, ti.GenericTypeArguments);
                }
            }

            return null;
        }

        static object CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
        {
            return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
        }
    }
}
