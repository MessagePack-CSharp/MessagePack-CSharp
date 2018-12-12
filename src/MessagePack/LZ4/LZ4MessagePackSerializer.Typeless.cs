#if NETSTANDARD || NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Text;
using MessagePack.Resolvers;
using MessagePack.Formatters;
using System.IO;

namespace MessagePack
{
    // Typeless API
    public static partial class LZ4MessagePackSerializer
    {
        public static class Typeless
        {
            static IFormatterResolver defaultResolver = new TypelessContractlessStandardResolver();

            public static void RegisterDefaultResolver(params IFormatterResolver[] resolvers)
            {
                CompositeResolver.Register(resolvers);
                defaultResolver = CompositeResolver.Instance;
            }

            public static byte[] Serialize(object obj)
            {
                return LZ4MessagePackSerializer.Serialize(obj, defaultResolver);
            }

            public static void Serialize(Stream stream, object obj)
            {
                LZ4MessagePackSerializer.Serialize(stream, obj, defaultResolver);
            }

            public static object Deserialize(byte[] bytes)
            {
                return LZ4MessagePackSerializer.Deserialize<object>(bytes, defaultResolver);
            }

            public static object Deserialize(Stream stream)
            {
                return LZ4MessagePackSerializer.Deserialize<object>(stream, defaultResolver);
            }

            public static object Deserialize(Stream stream, bool readStrict)
            {
                return LZ4MessagePackSerializer.Deserialize<object>(stream, defaultResolver, readStrict);
            }

            class CompositeResolver : IFormatterResolver
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
    }
}

#endif