#if !UNITY

using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace MessagePack
{
    partial class MessagePackSerializer
    {
        private static readonly Func<Type, CompiledMethods> CreateCompiledMethods;
        private static readonly MessagePack.Internal.ThreadsafeTypeKeyHashTable<CompiledMethods> serializes = new MessagePack.Internal.ThreadsafeTypeKeyHashTable<CompiledMethods>(capacity: 64);

        static MessagePackSerializer()
        {
            CreateCompiledMethods = t => new CompiledMethods(t);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <seealso cref="Serialize{T}(T, MessagePackSerializerOptions)"/>
        public static byte[] Serialize(Type type, object obj, MessagePackSerializerOptions options = null)
        {
            return GetOrAdd(type).serialize_T_Options.Invoke(obj, options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="stream"></param>
        /// <param name="obj"></param>
        /// <param name="options"></param>
        /// <seealso cref="SerializeAsync{T}(Stream, T, MessagePackSerializerOptions, System.Threading.CancellationToken)"/>
        public static void Serialize(Type type, Stream stream, object obj, MessagePackSerializerOptions options = null)
        {
            GetOrAdd(type).serializeAsync_Stream_T_Options.Invoke(stream, obj, options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="stream"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <seealso cref="Deserialize{T}(Stream, MessagePackSerializerOptions)"/>
        public static object Deserialize(Type type, Stream stream, MessagePackSerializerOptions options = null)
        {
            return GetOrAdd(type).deserialize_Stream_Options.Invoke(stream, options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <seealso cref="Deserialize{T}(ReadOnlyMemory{byte}, MessagePackSerializerOptions)"/>
        public static object Deserialize(Type type, ReadOnlyMemory<byte> bytes, MessagePackSerializerOptions options = null)
        {
            return GetOrAdd(type).deserialize_ReadOnlyMemory_Options.Invoke(bytes, options);
        }

        private static CompiledMethods GetOrAdd(Type type)
        {
            return serializes.GetOrAdd(type, CreateCompiledMethods);
        }

        private class CompiledMethods
        {
            public readonly Func<object, MessagePackSerializerOptions, byte[]> serialize_T_Options;
            public readonly Action<Stream, object, MessagePackSerializerOptions> serializeAsync_Stream_T_Options;

            public readonly Func<Stream, MessagePackSerializerOptions, object> deserialize_Stream_Options;

            public readonly Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, object> deserialize_ReadOnlyMemory_Options;

            public CompiledMethods(Type type)
            {
                var ti = type.GetTypeInfo();
                {
                    // public static byte[] Serialize<T>(T obj, MessagePackSerializerOptions options)
                    var serialize = GetMethod(type, new Type[] { null, typeof(MessagePackSerializerOptions) });

                    var param1 = Expression.Parameter(typeof(object), "obj");
                    var param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

                    var body = Expression.Call(
                        null,
                        serialize,
                        ti.IsValueType ? Expression.Unbox(param1, type) : Expression.Convert(param1, type),
                        param2);
                    var lambda = Expression.Lambda<Func<object, MessagePackSerializerOptions, byte[]>>(body, param1, param2).Compile();

                    this.serialize_T_Options = lambda;
                }
                {
                    // public static void Serialize<T>(Stream stream, T obj, MessagePackSerializerOptions options)
                    var serialize = GetMethod(type, new Type[] { typeof(Stream), null, typeof(MessagePackSerializerOptions) });

                    var param1 = Expression.Parameter(typeof(Stream), "stream");
                    var param2 = Expression.Parameter(typeof(object), "obj");
                    var param3 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

                    var body = Expression.Call(
                        null,
                        serialize,
                        param1,
                        ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                        param3);
                    var lambda = Expression.Lambda<Action<Stream, object, MessagePackSerializerOptions>>(body, param1, param2, param3).Compile();

                    this.serializeAsync_Stream_T_Options = lambda;
                }
                {
                    // public static T Deserialize<T>(Stream stream, MessagePackSerializerOptions options)
                    var deserialize = GetMethod(type, new Type[] { typeof(Stream), typeof(MessagePackSerializerOptions) });

                    var param1 = Expression.Parameter(typeof(Stream), "stream");
                    var param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    var body = Expression.Convert(Expression.Call(null, deserialize, param1, param2), typeof(object));
                    var lambda = Expression.Lambda<Func<Stream, MessagePackSerializerOptions, object>>(body, param1, param2).Compile();

                    this.deserialize_Stream_Options = lambda;
                }

                {
                    // public static T Deserialize<T>(ReadOnlyMemory<byte> bytes, MessagePackSerializerOptions options)
                    var deserialize = GetMethod(type, new Type[] { typeof(ReadOnlyMemory<byte>), typeof(MessagePackSerializerOptions) });

                    var param1 = Expression.Parameter(typeof(ReadOnlyMemory<byte>), "bytes");
                    var param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
                    var body = Expression.Convert(Expression.Call(null, deserialize, param1, param2), typeof(object));
                    var lambda = Expression.Lambda<Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, object>>(body, param1, param2).Compile();

                    this.deserialize_ReadOnlyMemory_Options = lambda;
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

                    var ps = x.GetParameters();
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
