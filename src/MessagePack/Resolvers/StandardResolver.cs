using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Default composited resolver, builtin -> attribute -> dynamic enum -> dynamic generic -> dynamic union -> dynamic object -> primitive.
    /// </summary>
    public class StandardResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new StandardResolver();

        static readonly IFormatterResolver[] resolvers = new[]
        {
            BuiltinResolver.Instance, // Try Builtin

            AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]

#if !NETSTANDARD1_4
            MessagePack.Unity.UnityResolver.Instance,
#endif

#if !ENABLE_IL2CPP

            DynamicEnumResolver.Instance, // Try Enum
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection
            DynamicUnionResolver.Instance, // Try Union(Interface)
            DynamicObjectResolver.Instance, // Try Object
#endif

            // finally, try primitive resolver
            PrimitiveObjectResolver.Instance
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

    public class ContractlessStandardResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new ContractlessStandardResolver();

        static readonly IFormatterResolver[] resolvers = new[]
        {
            BuiltinResolver.Instance, // Try Builtin

            AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]

#if !NETSTANDARD1_4
            MessagePack.Unity.UnityResolver.Instance,
#endif

#if !ENABLE_IL2CPP

            DynamicEnumResolver.Instance, // Try Enum
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection
            DynamicUnionResolver.Instance, // Try Union(Interface)
            DynamicObjectResolver.Instance, // Try Object
#endif

            DynamicContractlessObjectResolver.Instance, // Serializes keys as strings

            // finally, try primitive -> try `object` -> dynamic contractless
#if NETSTANDARD1_4
            DynamicObjectTypeFallbackResolver.Instance
#else
            PrimitiveObjectResolver.Instance
#endif
        };

        ContractlessStandardResolver()
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