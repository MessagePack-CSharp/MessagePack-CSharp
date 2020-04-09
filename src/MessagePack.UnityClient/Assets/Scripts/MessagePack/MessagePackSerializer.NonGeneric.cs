// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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

        /// <seealso cref="Serialize{T}(ref MessagePackWriter, T, MessagePackSerializerOptions)"/>
        public static void Serialize(Type type, ref MessagePackWriter writer, object obj, MessagePackSerializerOptions options = null)
        {
            GetOrAdd(type).Serialize_MessagePackWriter_T_Options.Invoke(ref writer, obj, options);
        }

        /// <seealso cref="Serialize{T}(IBufferWriter{byte}, T, MessagePackSerializerOptions, CancellationToken)"/>
        public static void Serialize(Type type, IBufferWriter<byte> writer, object obj, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            GetOrAdd(type).Serialize_IBufferWriter_T_Options_CancellationToken.Invoke(writer, obj, options, cancellationToken);
        }

        /// <seealso cref="Serialize{T}(T, MessagePackSerializerOptions, CancellationToken)"/>
        public static byte[] Serialize(Type type, object obj, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return GetOrAdd(type).Serialize_T_Options.Invoke(obj, options, cancellationToken);
        }

        /// <seealso cref="Serialize{T}(Stream, T, MessagePackSerializerOptions, CancellationToken)"/>
        public static void Serialize(Type type, Stream stream, object obj, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            GetOrAdd(type).Serialize_Stream_T_Options_CancellationToken.Invoke(stream, obj, options, cancellationToken);
        }

        /// <seealso cref="SerializeAsync{T}(Stream, T, MessagePackSerializerOptions, CancellationToken)"/>
        public static Task SerializeAsync(Type type, Stream stream, object obj, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return GetOrAdd(type).SerializeAsync_Stream_T_Options_CancellationToken.Invoke(stream, obj, options, cancellationToken);
        }

        /// <seealso cref="Deserialize{T}(ref MessagePackReader, MessagePackSerializerOptions)"/>
        public static object Deserialize(Type type, ref MessagePackReader reader, MessagePackSerializerOptions options = null)
        {
            return GetOrAdd(type).Deserialize_MessagePackReader_Options.Invoke(ref reader, options);
        }

        /// <seealso cref="Deserialize{T}(Stream, MessagePackSerializerOptions, CancellationToken)"/>
        public static object Deserialize(Type type, Stream stream, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return GetOrAdd(type).Deserialize_Stream_Options_CancellationToken.Invoke(stream, options, cancellationToken);
        }

        /// <seealso cref="DeserializeAsync{T}(Stream, MessagePackSerializerOptions, CancellationToken)"/>
        public static ValueTask<object> DeserializeAsync(Type type, Stream stream, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return GetOrAdd(type).DeserializeAsync_Stream_Options_CancellationToken.Invoke(stream, options, cancellationToken);
        }

        /// <seealso cref="Deserialize{T}(ReadOnlyMemory{byte}, MessagePackSerializerOptions, CancellationToken)"/>
        public static object Deserialize(Type type, ReadOnlyMemory<byte> bytes, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return GetOrAdd(type).Deserialize_ReadOnlyMemory_Options.Invoke(bytes, options, cancellationToken);
        }

        /// <seealso cref="Deserialize{T}(in ReadOnlySequence{byte}, MessagePackSerializerOptions, CancellationToken)"/>
        public static object Deserialize(Type type, ReadOnlySequence<byte> bytes, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return GetOrAdd(type).Deserialize_ReadOnlySequence_Options_CancellationToken.Invoke(bytes, options, cancellationToken);
        }

        private static async ValueTask<object> DeserializeObjectAsync<T>(Stream stream, MessagePackSerializerOptions options, CancellationToken cancellationToken) => await DeserializeAsync<T>(stream, options, cancellationToken).ConfigureAwait(false);

        private static CompiledMethods GetOrAdd(Type type)
        {
            return Serializes.GetOrAdd(type, CreateCompiledMethods);
        }

        private class CompiledMethods
        {
            internal delegate void MessagePackWriterSerialize(ref MessagePackWriter writer, object value, MessagePackSerializerOptions options);

            internal delegate object MessagePackReaderDeserialize(ref MessagePackReader reader, MessagePackSerializerOptions options);

#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
            internal readonly Func<object, MessagePackSerializerOptions, CancellationToken, byte[]> Serialize_T_Options;
            internal readonly Action<Stream, object, MessagePackSerializerOptions, CancellationToken> Serialize_Stream_T_Options_CancellationToken;
            internal readonly Func<Stream, object, MessagePackSerializerOptions, CancellationToken, Task> SerializeAsync_Stream_T_Options_CancellationToken;
            internal readonly MessagePackWriterSerialize Serialize_MessagePackWriter_T_Options;
            internal readonly Action<IBufferWriter<byte>, object, MessagePackSerializerOptions, CancellationToken> Serialize_IBufferWriter_T_Options_CancellationToken;

            internal readonly MessagePackReaderDeserialize Deserialize_MessagePackReader_Options;
            internal readonly Func<Stream, MessagePackSerializerOptions, CancellationToken, object> Deserialize_Stream_Options_CancellationToken;
            internal readonly Func<Stream, MessagePackSerializerOptions, CancellationToken, ValueTask<object>> DeserializeAsync_Stream_Options_CancellationToken;

            internal readonly Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, CancellationToken, object> Deserialize_ReadOnlyMemory_Options;
            internal readonly Func<ReadOnlySequence<byte>, MessagePackSerializerOptions, CancellationToken, object> Deserialize_ReadOnlySequence_Options_CancellationToken;
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter
#pragma warning restore SA1310 // Field names should not contain underscore

            internal CompiledMethods(Type type)
            {
                TypeInfo ti = type.GetTypeInfo();
                {
                    // public static byte[] Serialize<T>(T obj, MessagePackSerializerOptions options, CancellationToken cancellationToken)
                    MethodInfo serialize = GetMethod(nameof(Serialize), type, new Type[] { null, typeof(MessagePackSerializerOptions), typeof(CancellationToken) });

                    ParameterExpression param1 = Expression.Parameter(typeof(object), "obj");
                    ParameterExpression param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    ParameterExpression param3 = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                    MethodCallExpression body = Expression.Call(
                        null,
                        serialize,
                        ti.IsValueType ? Expression.Unbox(param1, type) : Expression.Convert(param1, type),
                        param2,
                        param3);
                    Func<object, MessagePackSerializerOptions, CancellationToken, byte[]> lambda = Expression.Lambda<Func<object, MessagePackSerializerOptions, CancellationToken, byte[]>>(body, param1, param2, param3).Compile();

                    this.Serialize_T_Options = lambda;
                }

                {
                    // public static void Serialize<T>(Stream stream, T obj, MessagePackSerializerOptions options, CancellationToken cancellationToken)
                    MethodInfo serialize = GetMethod(nameof(Serialize), type, new Type[] { typeof(Stream), null, typeof(MessagePackSerializerOptions), typeof(CancellationToken) });

                    ParameterExpression param1 = Expression.Parameter(typeof(Stream), "stream");
                    ParameterExpression param2 = Expression.Parameter(typeof(object), "obj");
                    ParameterExpression param3 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    ParameterExpression param4 = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                    MethodCallExpression body = Expression.Call(
                        null,
                        serialize,
                        param1,
                        ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                        param3,
                        param4);
                    Action<Stream, object, MessagePackSerializerOptions, CancellationToken> lambda = Expression.Lambda<Action<Stream, object, MessagePackSerializerOptions, CancellationToken>>(body, param1, param2, param3, param4).Compile();

                    this.Serialize_Stream_T_Options_CancellationToken = lambda;
                }

                {
                    // public static Task SerializeAsync<T>(Stream stream, T obj, MessagePackSerializerOptions options, CancellationToken cancellationToken)
                    MethodInfo serialize = GetMethod(nameof(SerializeAsync), type, new Type[] { typeof(Stream), null, typeof(MessagePackSerializerOptions), typeof(CancellationToken) });

                    ParameterExpression param1 = Expression.Parameter(typeof(Stream), "stream");
                    ParameterExpression param2 = Expression.Parameter(typeof(object), "obj");
                    ParameterExpression param3 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    ParameterExpression param4 = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                    MethodCallExpression body = Expression.Call(
                        null,
                        serialize,
                        param1,
                        ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                        param3,
                        param4);
                    Func<Stream, object, MessagePackSerializerOptions, CancellationToken, Task> lambda = Expression.Lambda<Func<Stream, object, MessagePackSerializerOptions, CancellationToken, Task>>(body, param1, param2, param3, param4).Compile();

                    this.SerializeAsync_Stream_T_Options_CancellationToken = lambda;
                }

                {
                    // public static Task Serialize<T>(IBufferWriter<byte> writer, T obj, MessagePackSerializerOptions options, CancellationToken cancellationToken)
                    MethodInfo serialize = GetMethod(nameof(Serialize), type, new Type[] { typeof(IBufferWriter<byte>), null, typeof(MessagePackSerializerOptions), typeof(CancellationToken) });

                    ParameterExpression param1 = Expression.Parameter(typeof(IBufferWriter<byte>), "writer");
                    ParameterExpression param2 = Expression.Parameter(typeof(object), "obj");
                    ParameterExpression param3 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    ParameterExpression param4 = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                    MethodCallExpression body = Expression.Call(
                        null,
                        serialize,
                        param1,
                        ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                        param3,
                        param4);
                    Action<IBufferWriter<byte>, object, MessagePackSerializerOptions, CancellationToken> lambda = Expression.Lambda<Action<IBufferWriter<byte>, object, MessagePackSerializerOptions, CancellationToken>>(body, param1, param2, param3, param4).Compile();

                    this.Serialize_IBufferWriter_T_Options_CancellationToken = lambda;
                }

                {
                    // public static void Serialize<T>(ref MessagePackWriter writer, T obj, MessagePackSerializerOptions options)
                    MethodInfo serialize = GetMethod(nameof(Serialize), type, new Type[] { typeof(MessagePackWriter).MakeByRefType(), null, typeof(MessagePackSerializerOptions) });

                    ParameterExpression param1 = Expression.Parameter(typeof(MessagePackWriter).MakeByRefType(), "writer");
                    ParameterExpression param2 = Expression.Parameter(typeof(object), "obj");
                    ParameterExpression param3 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

                    MethodCallExpression body = Expression.Call(
                        null,
                        serialize,
                        param1,
                        ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                        param3);
                    MessagePackWriterSerialize lambda = Expression.Lambda<MessagePackWriterSerialize>(body, param1, param2, param3).Compile();

                    this.Serialize_MessagePackWriter_T_Options = lambda;
                }

                {
                    // public static T Deserialize<T>(ref MessagePackReader reader, MessagePackSerializerOptions options)
                    MethodInfo deserialize = GetMethod(nameof(Deserialize), type, new Type[] { typeof(MessagePackReader).MakeByRefType(), typeof(MessagePackSerializerOptions) });

                    ParameterExpression param1 = Expression.Parameter(typeof(MessagePackReader).MakeByRefType(), "reader");
                    ParameterExpression param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    UnaryExpression body = Expression.Convert(Expression.Call(null, deserialize, param1, param2), typeof(object));
                    MessagePackReaderDeserialize lambda = Expression.Lambda<MessagePackReaderDeserialize>(body, param1, param2).Compile();

                    this.Deserialize_MessagePackReader_Options = lambda;
                }

                {
                    // public static T Deserialize<T>(Stream stream, MessagePackSerializerOptions options, CancellationToken cancellationToken)
                    MethodInfo deserialize = GetMethod(nameof(Deserialize), type, new Type[] { typeof(Stream), typeof(MessagePackSerializerOptions), typeof(CancellationToken) });

                    ParameterExpression param1 = Expression.Parameter(typeof(Stream), "stream");
                    ParameterExpression param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    ParameterExpression param3 = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                    UnaryExpression body = Expression.Convert(Expression.Call(null, deserialize, param1, param2, param3), typeof(object));
                    Func<Stream, MessagePackSerializerOptions, CancellationToken, object> lambda = Expression.Lambda<Func<Stream, MessagePackSerializerOptions, CancellationToken, object>>(body, param1, param2, param3).Compile();

                    this.Deserialize_Stream_Options_CancellationToken = lambda;
                }

                {
                    // public static ValueTask<object> DeserializeObjectAsync<T>(Stream stream, MessagePackSerializerOptions options, CancellationToken cancellationToken)
                    MethodInfo deserialize = GetMethod(nameof(DeserializeObjectAsync), type, new Type[] { typeof(Stream), typeof(MessagePackSerializerOptions), typeof(CancellationToken) });

                    ParameterExpression param1 = Expression.Parameter(typeof(Stream), "stream");
                    ParameterExpression param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    ParameterExpression param3 = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                    UnaryExpression body = Expression.Convert(Expression.Call(null, deserialize, param1, param2, param3), typeof(ValueTask<object>));
                    Func<Stream, MessagePackSerializerOptions, CancellationToken, ValueTask<object>> lambda = Expression.Lambda<Func<Stream, MessagePackSerializerOptions, CancellationToken, ValueTask<object>>>(body, param1, param2, param3).Compile();

                    this.DeserializeAsync_Stream_Options_CancellationToken = lambda;
                }

                {
                    // public static T Deserialize<T>(ReadOnlyMemory<byte> bytes, MessagePackSerializerOptions options, CancellationToken cancellationToken)
                    MethodInfo deserialize = GetMethod(nameof(Deserialize), type, new Type[] { typeof(ReadOnlyMemory<byte>), typeof(MessagePackSerializerOptions), typeof(CancellationToken) });

                    ParameterExpression param1 = Expression.Parameter(typeof(ReadOnlyMemory<byte>), "bytes");
                    ParameterExpression param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    ParameterExpression param3 = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                    UnaryExpression body = Expression.Convert(Expression.Call(null, deserialize, param1, param2, param3), typeof(object));
                    Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, CancellationToken, object> lambda = Expression.Lambda<Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, CancellationToken, object>>(body, param1, param2, param3).Compile();

                    this.Deserialize_ReadOnlyMemory_Options = lambda;
                }

                {
                    // public static T Deserialize<T>(ReadOnlySequence<byte> bytes, MessagePackSerializerOptions options, CancellationToken cancellationToken)
                    MethodInfo deserialize = GetMethod(nameof(Deserialize), type, new Type[] { typeof(ReadOnlySequence<byte>).MakeByRefType(), typeof(MessagePackSerializerOptions), typeof(CancellationToken) });

                    ParameterExpression param1 = Expression.Parameter(typeof(ReadOnlySequence<byte>), "bytes");
                    ParameterExpression param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    ParameterExpression param3 = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                    UnaryExpression body = Expression.Convert(Expression.Call(null, deserialize, param1, param2, param3), typeof(object));
                    Func<ReadOnlySequence<byte>, MessagePackSerializerOptions, CancellationToken, object> lambda = Expression.Lambda<Func<ReadOnlySequence<byte>, MessagePackSerializerOptions, CancellationToken, object>>(body, param1, param2, param3).Compile();

                    this.Deserialize_ReadOnlySequence_Options_CancellationToken = lambda;
                }
            }

            // null is generic type marker.
            private static MethodInfo GetMethod(string methodName, Type type, Type[] parameters)
            {
                return typeof(MessagePackSerializer).GetRuntimeMethods().Single(x =>
                {
                    if (methodName != x.Name)
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
