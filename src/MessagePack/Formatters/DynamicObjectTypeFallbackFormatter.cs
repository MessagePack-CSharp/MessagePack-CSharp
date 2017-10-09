#if NETSTANDARD

using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MessagePack.Formatters
{
    public sealed class DynamicObjectTypeFallbackFormatter : IMessagePackFormatter<object>
    {
        delegate int SerializeMethod(object dynamicFormatter, ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver);

        readonly MessagePack.Internal.ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>> serializers = new Internal.ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>>();

        readonly IFormatterResolver[] innerResolvers;

        public DynamicObjectTypeFallbackFormatter(params IFormatterResolver[] innerResolvers)
        {
            this.innerResolvers = innerResolvers;
        }

        public int Serialize(ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var type = value.GetType();
            var ti = type.GetTypeInfo();

            if (type == typeof(object))
            {
                // serialize to empty map
                return MessagePackBinary.WriteMapHeader(ref bytes, offset, 0);
            }

            KeyValuePair<object, SerializeMethod> formatterAndDelegate;
            if (!serializers.TryGetValue(type, out formatterAndDelegate))
            {
                lock (serializers)
                {
                    if (!serializers.TryGetValue(type, out formatterAndDelegate))
                    {
                        object formatter = null;
                        foreach (var innerResolver in innerResolvers)
                        {
                            formatter = innerResolver.GetFormatterDynamic(type);
                            if (formatter != null) break;
                        }
                        if (formatter == null)
                        {
                            throw new FormatterNotRegisteredException(type.FullName + " is not registered in this resolver. resolvers:" + string.Join(", ", innerResolvers.Select(x => x.GetType().Name).ToArray()));
                        }

                        var t = type;
                        {
                            var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(t);
                            var param0 = Expression.Parameter(typeof(object), "formatter");
                            var param1 = Expression.Parameter(typeof(byte[]).MakeByRefType(), "bytes");
                            var param2 = Expression.Parameter(typeof(int), "offset");
                            var param3 = Expression.Parameter(typeof(object), "value");
                            var param4 = Expression.Parameter(typeof(IFormatterResolver), "formatterResolver");

                            var serializeMethodInfo = formatterType.GetRuntimeMethod("Serialize", new[] { typeof(byte[]).MakeByRefType(), typeof(int), t, typeof(IFormatterResolver) });

                            var body = Expression.Call(
                                Expression.Convert(param0, formatterType),
                                serializeMethodInfo,
                                param1,
                                param2,
                                ti.IsValueType ? Expression.Unbox(param3, t) : Expression.Convert(param3, t),
                                param4);

                            var lambda = Expression.Lambda<SerializeMethod>(body, param0, param1, param2, param3, param4).Compile();

                            formatterAndDelegate = new KeyValuePair<object, SerializeMethod>(formatter, lambda);
                        }

                        serializers.TryAdd(t, formatterAndDelegate);
                    }
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