using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Resolvers
{
    public class DefaultResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new DefaultResolver();

        DefaultResolver()
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
                //if (typeof(T) == typeof(NotMsgPackMarkedClass))
                //{
                //    formatter = (IMessagePackFormatter<T>)(object)new NotMsgPackMarkedClassFormatter();
                //}
                //else
                //{
                //    // fallback, use default formatter.
                //    // formatter = 
                //}
            }
        }
    }
}
