using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Resolvers
{
    public class DefaultResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new DefaultResolver();

        static readonly IFormatterResolver[] resolvers = new[]
        {
            BuiltinResolver.Instance, // Try Builtin
            EnumResolver.Instance     // Try Enum
            // Try Union
            // Try Dynamic
        };

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
}