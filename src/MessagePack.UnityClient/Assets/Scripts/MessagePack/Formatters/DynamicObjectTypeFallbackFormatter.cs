// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY_STANDALONE

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MessagePack.Resolvers;

namespace MessagePack.Formatters
{
    public sealed class DynamicObjectTypeFallbackFormatter : IMessagePackFormatter<object>
    {
        private delegate void SerializeMethod(object dynamicFormatter, ref MessagePackWriter writer, object value, MessagePackSerializerOptions options);

        private readonly MessagePack.Internal.ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>> serializers = new Internal.ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>>();

        private readonly IFormatterResolver[] innerResolvers;

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

            Type type = value.GetType();
            TypeInfo ti = type.GetTypeInfo();

            if (type == typeof(object))
            {
                // serialize to empty map
                writer.WriteMapHeader(0);
                return;
            }

            KeyValuePair<object, SerializeMethod> formatterAndDelegate;
            if (!this.serializers.TryGetValue(type, out formatterAndDelegate))
            {
                lock (this.serializers)
                {
                    if (!this.serializers.TryGetValue(type, out formatterAndDelegate))
                    {
                        object formatter = null;
                        foreach (IFormatterResolver innerResolver in this.innerResolvers)
                        {
                            formatter = innerResolver.GetFormatterDynamic(type);
                            if (formatter != null)
                            {
                                break;
                            }
                        }

                        if (formatter == null)
                        {
                            throw new FormatterNotRegisteredException(type.FullName + " is not registered in this resolver. resolvers:" + string.Join(", ", this.innerResolvers.Select(x => x.GetType().Name).ToArray()));
                        }

                        Type t = type;
                        {
                            Type formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(t);
                            ParameterExpression param0 = Expression.Parameter(typeof(object), "formatter");
                            ParameterExpression param1 = Expression.Parameter(typeof(MessagePackWriter).MakeByRefType(), "writer");
                            ParameterExpression param2 = Expression.Parameter(typeof(object), "value");
                            ParameterExpression param3 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

                            MethodInfo serializeMethodInfo = formatterType.GetRuntimeMethod("Serialize", new[] { typeof(MessagePackWriter).MakeByRefType(), t, typeof(MessagePackSerializerOptions) });

                            MethodCallExpression body = Expression.Call(
                                Expression.Convert(param0, formatterType),
                                serializeMethodInfo,
                                param1,
                                ti.IsValueType ? Expression.Unbox(param2, t) : Expression.Convert(param2, t),
                                param3);

                            SerializeMethod lambda = Expression.Lambda<SerializeMethod>(body, param0, param1, param2, param3).Compile();

                            formatterAndDelegate = new KeyValuePair<object, SerializeMethod>(formatter, lambda);
                        }

                        this.serializers.TryAdd(t, formatterAndDelegate);
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
