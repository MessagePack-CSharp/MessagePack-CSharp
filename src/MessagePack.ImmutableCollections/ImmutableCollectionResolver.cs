using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;

namespace MessagePack.ImmutableCollections
{
    public class ImmutableCollectionResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new ImmutableCollectionResolver();

        ImmutableCollectionResolver()
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
                formatter = (IMessagePackFormatter<T>)ImmutableCollectionGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }

    internal static class ImmutableCollectionGetFormatterHelper
    {
        static readonly Dictionary<Type, Type> formatterMap = new Dictionary<Type, Type>()
        {
              // TODO: register all.
              {typeof(ImmutableList<>), typeof(ImmutableListFormatter<>)},
        };

        internal static object GetFormatter(Type t)
        {
            var ti = t.GetTypeInfo();

            Type formatterType;
            if (formatterMap.TryGetValue(t, out formatterType))
            {
                return CreateInstance(formatterType, ti.GenericTypeArguments);
            }

            return null;
        }

        static object CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
        {
            return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
        }
    }
}