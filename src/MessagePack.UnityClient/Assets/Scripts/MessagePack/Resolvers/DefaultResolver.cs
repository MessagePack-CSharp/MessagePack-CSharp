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
            //DynamicEnumResolver.Instance, // Try Enum
            //DynamicGenericResolver.Instance, // Try Array, Tuple, Collection
            // Try Union
            

            //DynamicObjectResolver.Instance // Try Object
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