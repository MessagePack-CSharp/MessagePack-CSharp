using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public sealed class PrimitiveObjectResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly PrimitiveObjectResolver Instance = new PrimitiveObjectResolver();

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options = new MessagePackSerializerOptions(Instance);

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

    //#if !UNITY

    //    /// <summary>
    //    /// In `object`, when serializing resolve by concrete type and when deserializing use primitive.
    //    /// </summary>
    //    public sealed class DynamicObjectTypeFallbackResolver : IFormatterResolver
    //    {
    //        public static readonly DynamicObjectTypeFallbackResolver Instance = new DynamicObjectTypeFallbackResolver();

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