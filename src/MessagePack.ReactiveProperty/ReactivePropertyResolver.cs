using MessagePack.Formatters;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reflection;

namespace MessagePack.ReactivePropertyExtension
{
    public class ReactivePropertyResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new ReactivePropertyResolver();

        ReactivePropertyResolver()
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
                formatter = (IMessagePackFormatter<T>)ReactivePropertyResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }

    internal static class ReactivePropertyResolverGetFormatterHelper
    {
        static readonly Dictionary<Type, Type> formatterMap = new Dictionary<Type, Type>()
        {
              {typeof(ReactiveProperty<>), typeof(ReactivePropertyFormatter<>)},
              {typeof(IReactiveProperty<>), typeof(InterfaceReactivePropertyFormatter<>)},
              {typeof(IReadOnlyReactiveProperty<>), typeof(InterfaceReadOnlyReactivePropertyFormatter<>)},
              {typeof(ReactiveCollection<>), typeof(ReactiveCollectionFormatter<>)},
              {typeof(ReactivePropertySlim<>), typeof(ReactivePropertySlimFormatter<>)},
        };

        internal static object GetFormatter(Type t)
        {
            if (t == typeof(Unit))
            {
                return UnitFormatter.Instance;
            }
            else if (t == typeof(Unit?))
            {
                return NullableUnitFormatter.Instance;
            }

            var ti = t.GetTypeInfo();

            if (ti.IsGenericType)
            {
                var genericType = ti.GetGenericTypeDefinition();
                Type formatterType;
                if (formatterMap.TryGetValue(genericType, out formatterType))
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