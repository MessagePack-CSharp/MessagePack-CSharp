using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public class PrimitiveObjectResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new PrimitiveObjectResolver();

        PrimitiveObjectResolver()
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
                    ? (IMessagePackFormatter<T>)(object)PrimitiveObjectFormatter.Instance
                    : null;
            }
        }
    }
}