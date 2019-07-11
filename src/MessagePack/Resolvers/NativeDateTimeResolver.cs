using MessagePack.Formatters;
using MessagePack.Internal;
using System;

namespace MessagePack.Resolvers
{
    public sealed class NativeDateTimeResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly NativeDateTimeResolver Instance = new NativeDateTimeResolver();

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options = new MessagePackSerializerOptions(Instance);

        NativeDateTimeResolver()
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
                formatter = (IMessagePackFormatter<T>)NativeDateTimeResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class NativeDateTimeResolverGetFormatterHelper
    {
        internal static object GetFormatter(Type t)
        {
            if (t == typeof(DateTime))
            {
                return NativeDateTimeFormatter.Instance;
            }
            else if (t == typeof(DateTime?))
            {
                return new StaticNullableFormatter<DateTime>(NativeDateTimeFormatter.Instance);
            }
            else if (t == typeof(DateTime[]))
            {
                return NativeDateTimeArrayFormatter.Instance;
            }

            return null;
        }
    }
}