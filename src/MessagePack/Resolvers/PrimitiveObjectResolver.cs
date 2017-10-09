using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public sealed class PrimitiveObjectResolver : IFormatterResolver
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

//#if NETSTANDARD

//    /// <summary>
//    /// In `object`, when serializing resolve by concrete type and when deserializing use primitive.
//    /// </summary>
//    public sealed class DynamicObjectTypeFallbackResolver : IFormatterResolver
//    {
//        public static IFormatterResolver Instance = new DynamicObjectTypeFallbackResolver();

//        DynamicObjectTypeFallbackResolver()
//        {

//        }

//        public IMessagePackFormatter<T> GetFormatter<T>()
//        {
//            return FormatterCache<T>.formatter;
//        }

//        static class FormatterCache<T>
//        {
//            public static readonly IMessagePackFormatter<T> formatter;

//            static FormatterCache()
//            {
//                formatter = (typeof(T) == typeof(object))
//                    ? (IMessagePackFormatter<T>)(object)DynamicObjectTypeFallbackFormatter.Instance
//                    : null;
//            }
//        }
//    }

//#endif
}