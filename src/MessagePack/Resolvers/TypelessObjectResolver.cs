using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
#if NETSTANDARD1_4

    /// <summary>
    /// Used for `object` fields/collections, ex: var arr = new object[] { 1, "a", new Model() };
    /// The runtime type of value in object field, should be covered by one of resolvers in complex/standard resolver.
    /// TypelessObjectResolver should be placed before DynamicObjectTypeFallbackResolver and PrimitiveObjectFormatter in resolvers list.
    /// Deserializer uses Namescape.TypeName, AssemblyName to get runtime type in destination app, so that combination must be present in destination app.
    /// Serialized binary is valid MessagePack binary used ext-format and custom typecode(100).
    /// Inside ext - assembly qualified type name, and serialized object
    /// </summary>
    public class TypelessObjectResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new TypelessObjectResolver();

        TypelessObjectResolver()
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
                formatter = (typeof(T) == typeof(object))
                    ? (IMessagePackFormatter<T>)(object)TypelessFormatter.Instance
                    : null;
            }
        }
    }

#endif
}