using System;
using System.Collections.Generic;
using System.Text;
using MessagePack.Formatters;
using MessagePack.Internal;

namespace MessagePack.Resolvers
{
    public class BuiltinResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new BuiltinResolver();

        BuiltinResolver()
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
                // Reduce IL2CPP code generate size.
                formatter = (IMessagePackFormatter<T>)BuiltinResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class BuiltinResolverGetFormatterHelper
    {
        internal static object GetFormatter(Type type)
        {
            if (type == typeof(bool))
            {
                return new BooleanFormatter();
            }

            // nullable
            else if (type == typeof(bool?))
            {
                return new NullableBooleanFormatter();
            }

            return null;
        }
    }
}
