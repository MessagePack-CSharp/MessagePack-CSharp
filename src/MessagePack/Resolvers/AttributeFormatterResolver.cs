using MessagePack.Formatters;
using System;
using System.Reflection;
using System.Linq; // require UNITY_WSA

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Get formatter from [MessaegPackFromatter] attribute.
    /// </summary>
    internal sealed class AttributeFormatterResolver : IFormatterResolver
    {
        internal static IFormatterResolver Instance = new AttributeFormatterResolver();

        AttributeFormatterResolver()
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
#if UNITY_WSA && !NETFX_CORE
                var attr = (MessagePackFormatterAttribute)typeof(T).GetCustomAttributes(typeof(MessagePackFormatterAttribute), true).FirstOrDefault();
#else
                var attr = typeof(T).GetTypeInfo().GetCustomAttribute<MessagePackFormatterAttribute>();
#endif
                if (attr == null)
                {
                    return;
                }

                if (attr.Arguments == null)
                {
                    formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(attr.FormatterType);
                }
                else
                {
                    formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(attr.FormatterType, attr.Arguments);
                }
            }
        }
    }
}
