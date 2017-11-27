using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public sealed class CompositeFieldPropertyValueSkipResolver : IFormatterResolverWithValueSkip
    {
        private readonly IEnumerable<IFormatterResolver> formatterResolver;

        private readonly IReadOnlyDictionary<Type, IEnumerable<string>> fieldPropertyValueSkipMap;

        public CompositeFieldPropertyValueSkipResolver(IEnumerable<IFormatterResolver> formatterResolver, IReadOnlyDictionary<Type, IEnumerable<string>> fieldPropertyValueSkipMap)
        {
            this.formatterResolver = formatterResolver;
            this.fieldPropertyValueSkipMap = fieldPropertyValueSkipMap;
        }

        public IMessagePackFormatter<T> GetFormatter<T>(IEnumerable<string> propertySkip)
        {
            return FormatterCache<T>.GetMessagePackFormatter(this.formatterResolver, propertySkip);
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (this.fieldPropertyValueSkipMap != null && this.fieldPropertyValueSkipMap.ContainsKey(typeof(T)))
            {
                return FormatterCache<T>.GetMessagePackFormatter(this.formatterResolver, this.fieldPropertyValueSkipMap[typeof(T)]);
            }
            else
            {
                return FormatterCache<T>.GetMessagePackFormatter(this.formatterResolver, null);
            }
        }

        static class FormatterCache<T>
        {
            private static readonly ConcurrentDictionary<long, IMessagePackFormatter<T>> formatters = new ConcurrentDictionary<long, IMessagePackFormatter<T>>();

            public static IMessagePackFormatter<T> GetMessagePackFormatter(IEnumerable<IFormatterResolver> formatterResolver, IEnumerable<string> fieldPropertyValueSkipMap)
            {
                if (fieldPropertyValueSkipMap == null || fieldPropertyValueSkipMap.Count() == 0)
                {
                    return formatters.GetOrAdd(0, BuildMessagePackFormatter(formatterResolver, fieldPropertyValueSkipMap));
                }

                return formatters.GetOrAdd(fieldPropertyValueSkipMap.GetHashcode<T>(), BuildMessagePackFormatter(formatterResolver, fieldPropertyValueSkipMap));
            }

            private static IMessagePackFormatter<T> BuildMessagePackFormatter(IEnumerable<IFormatterResolver> formatterResolver, IEnumerable<string> fieldPropertyValueSkipMap)
            {
                foreach (var item in formatterResolver)
                {
                    var f = item is IFormatterResolverWithValueSkip ? ((IFormatterResolverWithValueSkip)item).GetFormatter<T>(fieldPropertyValueSkipMap) : item.GetFormatter<T>();
                    if (f != null)
                    {
                        return f;
                    }
                }

                return null;
            }
        }
    }
}
