using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
#if NETSTANDARD1_4

    /// <summary>
    /// Used for `object` fields/collections, ex: var arr = new object[] { 1, "a", new Model() };
    /// DynamicContractless[IntKeys]ObjectResolver should be declared in resolvers before that one
    /// Serialized binary is valid MessagePack binary used ext-format and custom typecode(100).
    /// Inside ext - assembly qualified type name, and serialized object
    /// Path: try primitive -> try `object` -> dynamic contractless (for anonymous types)
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