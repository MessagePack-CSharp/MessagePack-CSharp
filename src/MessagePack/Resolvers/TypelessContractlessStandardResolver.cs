using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Embed c# type names for `object` typed fields/collection items
    /// Preserve c# DateTime timezone
    /// </summary>
    public class TypelessContractlessStandardResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new TypelessContractlessStandardResolver();

        static readonly IFormatterResolver[] resolvers = new[]
        {
            NativeDateTimeResolver.Instance, // Native c# DateTime format, preserving timezone
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
#if NETSTANDARD1_4
            TypelessObjectResolver.Instance
#else
            PrimitiveObjectResolver.Instance
#endif
        };

        TypelessContractlessStandardResolver()
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
