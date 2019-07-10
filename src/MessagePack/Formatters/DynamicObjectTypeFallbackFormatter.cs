#if !UNITY

using MessagePack.Resolvers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MessagePack.Formatters
{
    public sealed class DynamicObjectTypeFallbackFormatter : IMessagePackFormatter<object>
    {
        delegate void SerializeMethod(object dynamicFormatter, ref MessagePackWriter writer, object value, MessagePackSerializerOptions options);

        readonly MessagePack.Internal.ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>> serializers = new Internal.ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>>();

        readonly IFormatterResolver[] innerResolvers;

        public DynamicObjectTypeFallbackFormatter(params IFormatterResolver[] innerResolvers)
        {
            this.innerResolvers = innerResolvers;
        }

        public void Serialize(ref MessagePackWriter writer, object value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var type = value.GetType();
            var ti = type.GetTypeInfo();

            if (type == typeof(object))
            {
                // serialize to empty map
                writer.WriteMapHeader(0);
                return;
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
                            var param1 = Expression.Parameter(typeof(MessagePackWriter).MakeByRefType(), "writer");
                            var param2 = Expression.Parameter(typeof(object), "value");
                            var param3 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

                            var serializeMethodInfo = formatterType.GetRuntimeMethod("Serialize", new[] { typeof(MessagePackWriter).MakeByRefType(), t, typeof(MessagePackSerializerOptions) });

                            var body = Expression.Call(
                                Expression.Convert(param0, formatterType),
                                serializeMethodInfo,
                                param1,
                                ti.IsValueType ? Expression.Unbox(param2, t) : Expression.Convert(param2, t),
                                param3);

                            var lambda = Expression.Lambda<SerializeMethod>(body, param0, param1, param2, param3).Compile();

                            formatterAndDelegate = new KeyValuePair<object, SerializeMethod>(formatter, lambda);
                        }

                        serializers.TryAdd(t, formatterAndDelegate);
                    }
                }
            }

            formatterAndDelegate.Value(formatterAndDelegate.Key, ref writer, value, options);
        }

        public object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return PrimitiveObjectFormatter.Instance.Deserialize(ref reader, options);
        }
    }
}

#endif