using MessagePack.Formatters;
using System.Linq;
using MessagePack.Internal;
using System;
using System.Reflection;
using System.Collections.Generic;
using MessagePack.Resolvers;
using System.Linq.Expressions;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// Default composited resolver, builtin -> attribute -> dynamic enum -> dynamic generic -> dynamic union -> dynamic object -> primitive.
    /// </summary>
    public class StandardResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new StandardResolver();

        static readonly IFormatterResolver[] resolvers = new[]
        {
            BuiltinResolver.Instance, // Try Builtin

            AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]

#if !NETSTANDARD1_4
            MessagePack.Unity.UnityResolver.Instance,
#endif

#if !ENABLE_IL2CPP

            DynamicEnumResolver.Instance, // Try Enum
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection
            DynamicUnionResolver.Instance, // Try Union(Interface)
            DynamicObjectResolver.Instance, // Try Object
#endif

            // finally, try primitive resolver
            PrimitiveObjectResolver.Instance
        };

        StandardResolver()
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

    public class ContractlessStandardResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new ContractlessStandardResolver();

        static readonly IFormatterResolver[] resolvers = new[]
        {
            BuiltinResolver.Instance, // Try Builtin

            AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]

#if !NETSTANDARD1_4
            MessagePack.Unity.UnityResolver.Instance,
#endif

#if !ENABLE_IL2CPP

            DynamicEnumResolver.Instance, // Try Enum
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection
            DynamicUnionResolver.Instance, // Try Union(Interface)
            DynamicObjectResolver.Instance, // Try Object
#endif

            DynamicContractlessObjectResolver.Instance,

            // finally, try primitive -> dynamic contractless
            DynamicObjectFallbackResolver.Instance
        };

        ContractlessStandardResolver()
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

namespace MessagePack.Internal
{
    /// <summary>
    /// In `object`, when serializing resolve by concrete type and when deserializing use primitive.
    /// </summary>
    internal class DynamicObjectFallbackResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new DynamicObjectFallbackResolver();

        DynamicObjectFallbackResolver()
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
                    ? (IMessagePackFormatter<T>)(object)DynamicObjectFallbackResolverFormatter.Instance
                    : null;
            }
        }
    }

    internal class DynamicObjectFallbackResolverFormatter : IMessagePackFormatter<object>
    {
        internal delegate int SerializeMethod(object dynamicContractlessFormatter, ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver);

        internal static readonly IMessagePackFormatter<object> Instance = new DynamicObjectFallbackResolverFormatter();

        static readonly System.Collections.Generic.Dictionary<Type, KeyValuePair<object, SerializeMethod>> serializers = new Dictionary<Type, KeyValuePair<object, SerializeMethod>>();

        DynamicObjectFallbackResolverFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var type = value.GetType();
            var ti = type.GetTypeInfo();

            if (PrimitiveObjectFormatter.IsSupportedType(type, ti, value))
            {
                return PrimitiveObjectFormatter.Instance.Serialize(ref bytes, offset, value, formatterResolver);
            }

            KeyValuePair<object, SerializeMethod> formatterAndDelegate;
            lock (serializers)
            {
                if (!serializers.TryGetValue(type, out formatterAndDelegate))
                {
                    var formatter = DynamicContractlessObjectResolver.Instance.GetFormatterDynamic(type);
                    if (formatter == null)
                    {
                        throw new FormatterNotRegisteredException(type.FullName + " is not registered in this resolver. resolver:" + typeof(DynamicContractlessObjectResolver).Name);
                    }

                    var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
                    var param0 = Expression.Parameter(typeof(object), "formatter");
                    var param1 = Expression.Parameter(typeof(byte[]).MakeByRefType(), "bytes");
                    var param2 = Expression.Parameter(typeof(int), "offset");
                    var param3 = Expression.Parameter(typeof(object), "value");
                    var param4 = Expression.Parameter(typeof(IFormatterResolver), "formatterResolver");

                    var serializeMethodInfo = formatterType.GetRuntimeMethod("Serialize", new[] { typeof(byte[]).MakeByRefType(), typeof(int), type, typeof(IFormatterResolver) });

                    var body = Expression.Call(
                        Expression.Convert(param0, formatterType),
                        serializeMethodInfo,
                        param1,
                        param2,
                        ti.IsValueType ? Expression.Unbox(param3, type) : Expression.Convert(param3, type),
                        param4);

                    var lambda = Expression.Lambda<SerializeMethod>(body, param0, param1, param2, param3, param4).Compile();

                    formatterAndDelegate = new KeyValuePair<object, SerializeMethod>(formatter, lambda);

                    serializers[type] = formatterAndDelegate;
                }
            }

            return formatterAndDelegate.Value(formatterAndDelegate.Key, ref bytes, offset, value, formatterResolver);
        }

        public object Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return PrimitiveObjectFormatter.Instance.Deserialize(bytes, offset, formatterResolver, out readSize);
        }
    }
}