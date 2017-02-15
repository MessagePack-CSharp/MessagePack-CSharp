using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Resolvers
{
    public class DefaultResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new DefaultResolver();

        DefaultResolver()
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
                // Try Builtin
                var f = BuiltinResolver.Instance.GetFormatter<T>();
                if (f != null)
                {
                    formatter = f;
                    return;
                }

                // Try Enum
                f = EnumResolver.Instance.GetFormatter<T>();
                if (f != null)
                {
                    formatter = f;
                    return;
                }

                // Try Union

                // Try Dynamic
                // Unknown
            }
        }
    }
}