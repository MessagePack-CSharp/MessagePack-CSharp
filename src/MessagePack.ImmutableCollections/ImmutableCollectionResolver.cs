using MessagePack.Formatters;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MessagePack.Resolvers;

namespace MessagePack.ImmutableCollections
{
    public class DefaultWithImmutableCollectionResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new DefaultWithImmutableCollectionResolver();

        static readonly IFormatterResolver[] resolvers = new[]
        {
            ImmutableCollectionResolver.Instance, // add supports ImmutableCollection
            DefaultResolver.Instance,             // use default
        };

        DefaultWithImmutableCollectionResolver()
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
                foreach (var item in resolvers)
                {
                    var f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        formatter = f;
                        return;
                    }
                }
            }
        }
    }

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