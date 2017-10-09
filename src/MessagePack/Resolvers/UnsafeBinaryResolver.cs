#if NETSTANDARD

using MessagePack.Formatters;
using MessagePack.Internal;
using System;

namespace MessagePack.Resolvers
{
    public sealed class UnsafeBinaryResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new UnsafeBinaryResolver();

        UnsafeBinaryResolver()
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
                formatter = (IMessagePackFormatter<T>)UnsafeBinaryResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class UnsafeBinaryResolverGetFormatterHelper
    {
        internal static object GetFormatter(Type t)
        {
            if (t == typeof(Guid))
            {
                return BinaryGuidFormatter.Instance;
            }
            else if (t == typeof(Guid?))
            {
                return new StaticNullableFormatter<Guid>(BinaryGuidFormatter.Instance);
            }
            else if (t == typeof(Decimal))
            {
                return BinaryDecimalFormatter.Instance;
            }
            else if (t == typeof(Decimal?))
            {
                return new StaticNullableFormatter<Decimal>(BinaryDecimalFormatter.Instance);
            }

            return null;
        }
    }
}

#endif