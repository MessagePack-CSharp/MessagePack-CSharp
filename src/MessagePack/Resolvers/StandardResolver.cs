using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Default composited resolver, builtin -> dynamic enum -> dynamic generic -> dynamic union -> dynamic object.
    /// </summary>
    public class StandardResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new StandardResolver();

        static readonly IFormatterResolver[] resolvers = new[]
        {
            BuiltinResolver.Instance, // Try Builtin

#if !ENABLE_IL2CPP

            DynamicEnumResolver.Instance, // Try Enum
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection
            DynamicUnionResolver.Instance, // Try Union(Interface)
            DynamicObjectResolver.Instance // Try Object

#endif
        };

        StandardResolver()
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

    public class DefaultWithContractlessResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new DefaultWithContractlessResolver();

        static readonly IFormatterResolver[] resolvers = new[]
        {
            StandardResolver.Instance,
            DynamicContractlessObjectResolver.Instance,
        };

        DefaultWithContractlessResolver()
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