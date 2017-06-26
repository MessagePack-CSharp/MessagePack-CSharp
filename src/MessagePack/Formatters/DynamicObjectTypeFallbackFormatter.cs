#if NETSTANDARD1_4

using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace MessagePack.Formatters
{
    /// <summary>
    /// PrimitiveObjectFormatter -> DynamicContractlessFormatter(NonGeneric).
    /// </summary>
    public class DynamicObjectTypeFallbackFormatter : IMessagePackFormatter<object>
    {
        delegate int SerializeMethod(object dynamicContractlessFormatter, ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver);

        public static readonly IMessagePackFormatter<object> Instance = new DynamicObjectTypeFallbackFormatter();

        static readonly System.Collections.Generic.Dictionary<Type, KeyValuePair<object, SerializeMethod>> serializers = new Dictionary<Type, KeyValuePair<object, SerializeMethod>>();

        DynamicObjectTypeFallbackFormatter()
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

#endif