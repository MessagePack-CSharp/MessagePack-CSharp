using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Resolvers
{
    public class CompositeResolver : IFormatterResolver
    {
        public static readonly CompositeResolver Instance = new CompositeResolver();

        static bool isFreezed = false;
        static IFormatterResolver[] resolvers = new IFormatterResolver[0];

        CompositeResolver()
        {
        }

        public static void Register(params IFormatterResolver[] resolvers)
        {
            if (isFreezed)
            {
                throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
            }

            CompositeResolver.resolvers = resolvers;
        }

        public static void RegisterAndSetAsDefault(params IFormatterResolver[] resolvers)
        {
            Register(resolvers);
            MessagePack.MessagePackSerializer.SetDefaultResolver(CompositeResolver.Instance);
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
                isFreezed = true;

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
