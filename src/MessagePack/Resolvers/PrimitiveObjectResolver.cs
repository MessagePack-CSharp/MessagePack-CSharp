using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    internal sealed class PrimitiveObjectResolver : IFormatterResolver
    {
        internal static IFormatterResolver Instance = new PrimitiveObjectResolver();

        PrimitiveObjectResolver()
        {

        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            internal static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = (typeof(T) == typeof(object))
                    ? (IMessagePackFormatter<T>)(object)PrimitiveObjectFormatter.Instance
                    : null;
            }
        }
    }

//#if !UNITY

//    /// <summary>
//    /// In `object`, when serializing resolve by concrete type and when deserializing use primitive.
//    /// </summary>
//    internal sealed class DynamicObjectTypeFallbackResolver : IFormatterResolver
//    {
//        internal static IFormatterResolver Instance = new DynamicObjectTypeFallbackResolver();

//        DynamicObjectTypeFallbackResolver()
//        {

//        }

//        public IMessagePackFormatter<T> GetFormatter<T>()
//        {
//            return FormatterCache<T>.formatter;
//        }

//        static class FormatterCache<T>
//        {
//            internal static readonly IMessagePackFormatter<T> formatter;

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
