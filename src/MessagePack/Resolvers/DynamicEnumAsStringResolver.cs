#if !UNITY_WSA

using MessagePack.Formatters;
using MessagePack.Internal;
using System;
using System.Reflection;

namespace MessagePack.Resolvers
{
    public sealed class DynamicEnumAsStringResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new DynamicEnumAsStringResolver();

        DynamicEnumAsStringResolver()
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
                var ti = typeof(T).GetTypeInfo();

                if (ti.IsNullable())
                {
                    // build underlying type and use wrapped formatter.
                    ti = ti.GenericTypeArguments[0].GetTypeInfo();
                    if (!ti.IsEnum)
                    {
                        return;
                    }

                    var innerFormatter = DynamicEnumAsStringResolver.Instance.GetFormatterDynamic(ti.AsType());
                    if (innerFormatter == null)
                    {
                        return;
                    }
                    formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }
                else if (!ti.IsEnum)
                {
                    return;
                }

                formatter = (IMessagePackFormatter<T>)(object)new EnumAsStringFormatter<T>();
            }
        }
    }
}

#endif