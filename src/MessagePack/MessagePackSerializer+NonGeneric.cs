#if !UNITY

using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MessagePack
{
    partial class MessagePackSerializer
    {
        /// <summary>
        /// A convenience wrapper around <see cref="MessagePackSerializer"/> that allows all generic type arguments
        /// to be specified as <see cref="Type"/> parameters instead.
        /// </summary>
        public class NonGeneric
        {
            private static readonly Func<Type, CompiledMethods> CreateCompiledMethods;
            private static readonly MessagePack.Internal.ThreadsafeTypeKeyHashTable<CompiledMethods> serializes = new MessagePack.Internal.ThreadsafeTypeKeyHashTable<CompiledMethods>(capacity: 64);

            private readonly MessagePackSerializer serializer;

            static NonGeneric()
            {
                CreateCompiledMethods = t => new CompiledMethods(t);
            }

            public NonGeneric()
                : this(new MessagePackSerializer())
            {
            }

            public NonGeneric(MessagePackSerializer serializer)
            {
                this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            }

            public NonGeneric(IFormatterResolver resolver)
                : this(new MessagePackSerializer(resolver))
            {
            }

            public byte[] Serialize(Type type, object obj, IFormatterResolver resolver = null)
            {
                return GetOrAdd(type).serialize2.Invoke(this.serializer, obj, resolver);
            }

            public void Serialize(Type type, Stream stream, object obj, IFormatterResolver resolver = null)
            {
                GetOrAdd(type).serialize4.Invoke(this.serializer, stream, obj, resolver);
            }

            public object Deserialize(Type type, byte[] bytes, int offset = 0, IFormatterResolver resolver = null)
            {
                return GetOrAdd(type).deserialize2.Invoke(this.serializer, bytes, offset, resolver);
            }

            public object Deserialize(Type type, Stream stream, IFormatterResolver resolver = null)
            {
                return GetOrAdd(type).deserialize4.Invoke(this.serializer, stream, resolver);
            }

            public object Deserialize(Type type, Memory<byte> bytes, IFormatterResolver resolver = null)
            {
                return GetOrAdd(type).deserialize8.Invoke(this.serializer, bytes, resolver);
            }

            private static CompiledMethods GetOrAdd(Type type)
            {
                return serializes.GetOrAdd(type, CreateCompiledMethods);
            }

            private class CompiledMethods
            {
                public readonly Func<MessagePackSerializer, object, IFormatterResolver, byte[]> serialize2;
                public readonly Action<MessagePackSerializer, Stream, object, IFormatterResolver> serialize4;

                public readonly Func<MessagePackSerializer, byte[], int, IFormatterResolver, object> deserialize2;
                public readonly Func<MessagePackSerializer, Stream, IFormatterResolver, object> deserialize4;

                public readonly Func<MessagePackSerializer, Memory<byte>, IFormatterResolver, object> deserialize8;

                public CompiledMethods(Type type)
                {
                    var ti = type.GetTypeInfo();
                    var param0 = Expression.Parameter(typeof(MessagePackSerializer), "serializer");
                    {
                        // public static byte[] Serialize<T>(T obj, IFormatterResolver resolver)
                        var serialize = GetMethod(type, new Type[] { null, typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(object), "obj");
                        var param2 = Expression.Parameter(typeof(IFormatterResolver), "formatterResolver");

                        var body = Expression.Call(
                            param0,
                            serialize,
                            ti.IsValueType ? Expression.Unbox(param1, type) : Expression.Convert(param1, type),
                            param2);
                        var lambda = Expression.Lambda<Func<MessagePackSerializer, object, IFormatterResolver, byte[]>>(body, param0, param1, param2).Compile();

                        this.serialize2 = lambda;
                    }
                    {
                        // public static void Serialize<T>(Stream stream, T obj, IFormatterResolver resolver)
                        var serialize = GetMethod(type, new Type[] { typeof(Stream), null, typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(Stream), "stream");
                        var param2 = Expression.Parameter(typeof(object), "obj");
                        var param3 = Expression.Parameter(typeof(IFormatterResolver), "formatterResolver");

                        var body = Expression.Call(
                            param0,
                            serialize,
                            param1,
                            ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                            param3);
                        var lambda = Expression.Lambda<Action<MessagePackSerializer, Stream, object, IFormatterResolver>>(body, param0, param1, param2, param3).Compile();

                        this.serialize4 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(byte[] bytes, int offset, IFormatterResolver resolver)
                        var deserialize = GetMethod(type, new Type[] { typeof(byte[]), typeof(int), typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(byte[]), "bytes");
                        var param2 = Expression.Parameter(typeof(int), "offset");
                        var param3 = Expression.Parameter(typeof(IFormatterResolver), "resolver");
                        var body = Expression.Convert(Expression.Call(param0, deserialize, param1, param2, param3), typeof(object));
                        var lambda = Expression.Lambda<Func<MessagePackSerializer, byte[], int, IFormatterResolver, object>>(body, param0, param1, param2, param3).Compile();

                        this.deserialize2 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(Stream stream, IFormatterResolver resolver)
                        var deserialize = GetMethod(type, new Type[] { typeof(Stream), typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(Stream), "stream");
                        var param2 = Expression.Parameter(typeof(IFormatterResolver), "resolver");
                        var body = Expression.Convert(Expression.Call(param0, deserialize, param1, param2), typeof(object));
                        var lambda = Expression.Lambda<Func<MessagePackSerializer, Stream, IFormatterResolver, object>>(body, param0, param1, param2).Compile();

                        this.deserialize4 = lambda;
                    }

                    {
                        // public static T Deserialize<T>(Memory<byte> bytes, IFormatterResolver resolver)
                        var deserialize = GetMethod(type, new Type[] { typeof(Memory<byte>), typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(Memory<byte>), "bytes");
                        var param2 = Expression.Parameter(typeof(IFormatterResolver), "resolver");
                        var body = Expression.Convert(Expression.Call(param0, deserialize, param1, param2), typeof(object));
                        var lambda = Expression.Lambda<Func<MessagePackSerializer, Memory<byte>, IFormatterResolver, object>>(body, param0, param1, param2).Compile();

                        this.deserialize8 = lambda;
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
}

#endif
