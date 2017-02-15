using System;
using System.Collections.Generic;
using System.Text;
using MessagePack.Formatters;
using MessagePack.Internal;
using System.Reflection;

namespace MessagePack.Resolvers
{
    public class BuiltinResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new BuiltinResolver();

        BuiltinResolver()
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
                // Reduce IL2CPP code generate size.
                formatter = (IMessagePackFormatter<T>)BuiltinResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class BuiltinResolverGetFormatterHelper
    {
        internal static object GetFormatter(Type type)
        {
            var ti = type.GetTypeInfo();

            if (type == typeof(bool))
            {
                return new BooleanFormatter();
            }
            else if (type == typeof(byte))
            {
                return new ByteFormatter();
            }
            else if (type == typeof(int))
            {
                return new Int32Formatter();
            }
            else if (type == typeof(double))
            {
                return new DoubleFormatter();
            }

            // nullable

            // don't use dynamic, builtin should resolve static.
            //else if (ti.IsNullable())
            //{
            //    // build underlying type and use wrapped formatter.
            //    var underlyhingType = ti.GenericTypeArguments[0];
            //    var innerFormatter = GetFormatter(underlyhingType);
            //    if (innerFormatter == null) return null;

            //    return Activator.CreateInstance(typeof(NullableFormatter<>).MakeGenericType(type), new object[] { innerFormatter });
                

            //}

            

            return null;
        }
    }
}
