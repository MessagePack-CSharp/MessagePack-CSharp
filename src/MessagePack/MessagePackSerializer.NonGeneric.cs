// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY

using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace MessagePack
{
    public partial class MessagePackSerializer
    {
        private static readonly Func<Type, CompiledMethods> CreateCompiledMethods;
        private static readonly MessagePack.Internal.ThreadsafeTypeKeyHashTable<CompiledMethods> Serializes = new MessagePack.Internal.ThreadsafeTypeKeyHashTable<CompiledMethods>(capacity: 64);

        static MessagePackSerializer()
        {
            CreateCompiledMethods = t => new CompiledMethods(t);
        }

        /// <seealso cref="Serialize{T}(T, MessagePackSerializerOptions)"/>
        public static byte[] Serialize(Type type, object obj, MessagePackSerializerOptions options = null)
        {
            return GetOrAdd(type).Serialize_T_Options.Invoke(obj, options);
        }

        /// <seealso cref="SerializeAsync{T}(Stream, T, MessagePackSerializerOptions, System.Threading.CancellationToken)"/>
        public static void Serialize(Type type, Stream stream, object obj, MessagePackSerializerOptions options = null)
        {
            GetOrAdd(type).SerializeAsync_Stream_T_Options.Invoke(stream, obj, options);
        }

        /// <seealso cref="Deserialize{T}(Stream, MessagePackSerializerOptions)"/>
        public static object Deserialize(Type type, Stream stream, MessagePackSerializerOptions options = null)
        {
            return GetOrAdd(type).Deserialize_Stream_Options.Invoke(stream, options);
        }

        /// <seealso cref="Deserialize{T}(ReadOnlyMemory{byte}, MessagePackSerializerOptions)"/>
        public static object Deserialize(Type type, ReadOnlyMemory<byte> bytes, MessagePackSerializerOptions options = null)
        {
            return GetOrAdd(type).Deserialize_ReadOnlyMemory_Options.Invoke(bytes, options);
        }

        private static CompiledMethods GetOrAdd(Type type)
        {
            return Serializes.GetOrAdd(type, CreateCompiledMethods);
        }

        private class CompiledMethods
        {
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
            internal readonly Func<object, MessagePackSerializerOptions, byte[]> Serialize_T_Options;
            internal readonly Action<Stream, object, MessagePackSerializerOptions> SerializeAsync_Stream_T_Options;

            internal readonly Func<Stream, MessagePackSerializerOptions, object> Deserialize_Stream_Options;

            internal readonly Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, object> Deserialize_ReadOnlyMemory_Options;
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter
#pragma warning restore SA1310 // Field names should not contain underscore

            internal CompiledMethods(Type type)
            {
                TypeInfo ti = type.GetTypeInfo();
                {
                    // public static byte[] Serialize<T>(T obj, MessagePackSerializerOptions options)
                    MethodInfo serialize = GetMethod(type, new Type[] { null, typeof(MessagePackSerializerOptions) });

                    ParameterExpression param1 = Expression.Parameter(typeof(object), "obj");
                    ParameterExpression param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

                    MethodCallExpression body = Expression.Call(
                        null,
                        serialize,
                        ti.IsValueType ? Expression.Unbox(param1, type) : Expression.Convert(param1, type),
                        param2);
                    Func<object, MessagePackSerializerOptions, byte[]> lambda = Expression.Lambda<Func<object, MessagePackSerializerOptions, byte[]>>(body, param1, param2).Compile();

                    this.Serialize_T_Options = lambda;
                }

                {
                    // public static void Serialize<T>(Stream stream, T obj, MessagePackSerializerOptions options)
                    MethodInfo serialize = GetMethod(type, new Type[] { typeof(Stream), null, typeof(MessagePackSerializerOptions) });

                    ParameterExpression param1 = Expression.Parameter(typeof(Stream), "stream");
                    ParameterExpression param2 = Expression.Parameter(typeof(object), "obj");
                    ParameterExpression param3 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

                    MethodCallExpression body = Expression.Call(
                        null,
                        serialize,
                        param1,
                        ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                        param3);
                    Action<Stream, object, MessagePackSerializerOptions> lambda = Expression.Lambda<Action<Stream, object, MessagePackSerializerOptions>>(body, param1, param2, param3).Compile();

                    this.SerializeAsync_Stream_T_Options = lambda;
                }

                {
                    // public static T Deserialize<T>(Stream stream, MessagePackSerializerOptions options)
                    MethodInfo deserialize = GetMethod(type, new Type[] { typeof(Stream), typeof(MessagePackSerializerOptions) });

                    ParameterExpression param1 = Expression.Parameter(typeof(Stream), "stream");
                    ParameterExpression param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    UnaryExpression body = Expression.Convert(Expression.Call(null, deserialize, param1, param2), typeof(object));
                    Func<Stream, MessagePackSerializerOptions, object> lambda = Expression.Lambda<Func<Stream, MessagePackSerializerOptions, object>>(body, param1, param2).Compile();

                    this.Deserialize_Stream_Options = lambda;
                }

                {
                    // public static T Deserialize<T>(ReadOnlyMemory<byte> bytes, MessagePackSerializerOptions options)
                    MethodInfo deserialize = GetMethod(type, new Type[] { typeof(ReadOnlyMemory<byte>), typeof(MessagePackSerializerOptions) });

                    ParameterExpression param1 = Expression.Parameter(typeof(ReadOnlyMemory<byte>), "bytes");
                    ParameterExpression param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    UnaryExpression body = Expression.Convert(Expression.Call(null, deserialize, param1, param2), typeof(object));
                    Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, object> lambda = Expression.Lambda<Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, object>>(body, param1, param2).Compile();

                    this.Deserialize_ReadOnlyMemory_Options = lambda;
                }
            }

            // null is generic type marker.
            private static MethodInfo GetMethod(Type type, Type[] parameters)
            {
                return typeof(MessagePackSerializer).GetRuntimeMethods().Single(x =>
                {
                    if (!(x.Name == nameof(MessagePackSerializer.Serialize) || x.Name == nameof(MessagePackSerializer.Deserialize)))
                    {
                        return false;
                    }

                    ParameterInfo[] ps = x.GetParameters();
                    if (ps.Length != parameters.Length)
                    {
                        return false;
                    }

                    for (int i = 0; i < ps.Length; i++)
                    {
                        if (parameters[i] == null && ps[i].ParameterType.IsGenericParameter)
                        {
                            continue;
                        }

                        if (ps[i].ParameterType != parameters[i])
                        {
                            return false;
                        }
                    }

                    return true;
                })
                .MakeGenericMethod(type);
            }
        }
    }
}

#endif
