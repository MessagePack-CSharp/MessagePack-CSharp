#if NETSTANDARD || NETFRAMEWORK

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
            private delegate int RawFormatterSerialize(MessagePackSerializer serializer, ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver);

            private delegate object RawFormatterDeserialize(MessagePackSerializer serializer, byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);

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

            public byte[] Serialize(Type type, object obj)
            {
                return GetOrAdd(type).serialize1.Invoke(this.serializer, obj);
            }

            public byte[] Serialize(Type type, object obj, IFormatterResolver resolver)
            {
                return GetOrAdd(type).serialize2.Invoke(this.serializer, obj, resolver);
            }

            public void Serialize(Type type, Stream stream, object obj)
            {
                GetOrAdd(type).serialize3.Invoke(this.serializer, stream, obj);
            }

            public void Serialize(Type type, Stream stream, object obj, IFormatterResolver resolver)
            {
                GetOrAdd(type).serialize4.Invoke(this.serializer, stream, obj, resolver);
            }

            public int Serialize(Type type, ref byte[] bytes, int offset, object value, IFormatterResolver resolver)
            {
                return GetOrAdd(type).serialize5.Invoke(this.serializer, ref bytes, offset, value, resolver);
            }

            public object Deserialize(Type type, byte[] bytes)
            {
                return GetOrAdd(type).deserialize1.Invoke(this.serializer, bytes);
            }

            public object Deserialize(Type type, byte[] bytes, IFormatterResolver resolver)
            {
                return GetOrAdd(type).deserialize2.Invoke(this.serializer, bytes, resolver);
            }

            public object Deserialize(Type type, Stream stream)
            {
                return GetOrAdd(type).deserialize3.Invoke(this.serializer, stream);
            }

            public object Deserialize(Type type, Stream stream, IFormatterResolver resolver)
            {
                return GetOrAdd(type).deserialize4.Invoke(this.serializer, stream, resolver);
            }

            public object Deserialize(Type type, Stream stream, bool readStrict)
            {
                return GetOrAdd(type).deserialize5.Invoke(this.serializer, stream, readStrict);
            }

            public object Deserialize(Type type, Stream stream, IFormatterResolver resolver, bool readStrict)
            {
                return GetOrAdd(type).deserialize6.Invoke(this.serializer, stream, resolver, readStrict);
            }

            public object Deserialize(Type type, ArraySegment<byte> bytes)
            {
                return GetOrAdd(type).deserialize7.Invoke(this.serializer, bytes);
            }

            public object Deserialize(Type type, ArraySegment<byte> bytes, IFormatterResolver resolver)
            {
                return GetOrAdd(type).deserialize8.Invoke(this.serializer, bytes, resolver);
            }

            public object Deserialize(Type type, byte[] bytes, int offset, IFormatterResolver resolver, out int readSize)
            {
                return GetOrAdd(type).deserialize9.Invoke(this.serializer, bytes, offset, resolver, out readSize);
            }

            private static CompiledMethods GetOrAdd(Type type)
            {
                return serializes.GetOrAdd(type, CreateCompiledMethods);
            }

            private class CompiledMethods
            {
                public readonly Func<MessagePackSerializer, object, byte[]> serialize1;
                public readonly Func<MessagePackSerializer, object, IFormatterResolver, byte[]> serialize2;
                public readonly Action<MessagePackSerializer, Stream, object> serialize3;
                public readonly Action<MessagePackSerializer, Stream, object, IFormatterResolver> serialize4;
                public readonly RawFormatterSerialize serialize5;

                public readonly Func<MessagePackSerializer, byte[], object> deserialize1;
                public readonly Func<MessagePackSerializer, byte[], IFormatterResolver, object> deserialize2;
                public readonly Func<MessagePackSerializer, Stream, object> deserialize3;
                public readonly Func<MessagePackSerializer, Stream, IFormatterResolver, object> deserialize4;
                public readonly Func<MessagePackSerializer, Stream, bool, object> deserialize5;
                public readonly Func<MessagePackSerializer, Stream, IFormatterResolver, bool, object> deserialize6;

                public readonly Func<MessagePackSerializer, ArraySegment<byte>, object> deserialize7;
                public readonly Func<MessagePackSerializer, ArraySegment<byte>, IFormatterResolver, object> deserialize8;
                public readonly RawFormatterDeserialize deserialize9;

                public CompiledMethods(Type type)
                {
                    var ti = type.GetTypeInfo();
                    var param0 = Expression.Parameter(typeof(MessagePackSerializer), "serializer");
                    {
                        // public static byte[] Serialize<T>(T obj)
                        var serialize = GetMethod(type, new Type[] { null });

                        var param1 = Expression.Parameter(typeof(object), "obj");

                        var body = Expression.Call(
                            param0,
                            serialize,
                            ti.IsValueType ? Expression.Unbox(param1, type) : Expression.Convert(param1, type));
                        var lambda = Expression.Lambda<Func<MessagePackSerializer, object, byte[]>>(body, param0, param1).Compile();

                        this.serialize1 = lambda;
                    }
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
                        // public static void Serialize<T>(Stream stream, T obj)
                        var serialize = GetMethod(type, new Type[] { typeof(Stream), null });

                        var param1 = Expression.Parameter(typeof(Stream), "stream");
                        var param2 = Expression.Parameter(typeof(object), "obj");

                        var body = Expression.Call(
                            param0,
                            serialize,
                            param1,
                            ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type));
                        var lambda = Expression.Lambda<Action<MessagePackSerializer, Stream, object>>(body, param0, param1, param2).Compile();

                        this.serialize3 = lambda;
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
                        // delegate int RawFormatterSerialize(ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver);
                        var serialize = GetMethod(type, new Type[] { typeof(byte[]).MakeByRefType(), typeof(int), null, typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(byte[]).MakeByRefType(), "bytes");
                        var param2 = Expression.Parameter(typeof(int), "offset");
                        var param3 = Expression.Parameter(typeof(object), "value");
                        var param4 = Expression.Parameter(typeof(IFormatterResolver), "formatterResolver");

                        var body = Expression.Call(
                            param0,
                            serialize,
                            param1,
                            param2,
                            ti.IsValueType ? Expression.Unbox(param3, type) : Expression.Convert(param3, type),
                            param4);
                        var lambda = Expression.Lambda<RawFormatterSerialize>(body, param0, param1, param2, param3, param4).Compile();

                        this.serialize5 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(byte[] bytes)
                        var deserialize = GetMethod(type, new Type[] { typeof(byte[]) });

                        var param1 = Expression.Parameter(typeof(byte[]), "bytes");
                        var body = Expression.Convert(Expression.Call(param0, deserialize, param1), typeof(object));
                        var lambda = Expression.Lambda<Func<MessagePackSerializer, byte[], object>>(body, param0, param1).Compile();

                        this.deserialize1 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(byte[] bytes, IFormatterResolver resolver)
                        var deserialize = GetMethod(type, new Type[] { typeof(byte[]), typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(byte[]), "bytes");
                        var param2 = Expression.Parameter(typeof(IFormatterResolver), "resolver");
                        var body = Expression.Convert(Expression.Call(param0, deserialize, param1, param2), typeof(object));
                        var lambda = Expression.Lambda<Func<MessagePackSerializer, byte[], IFormatterResolver, object>>(body, param0, param1, param2).Compile();

                        this.deserialize2 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(Stream stream)
                        var deserialize = GetMethod(type, new Type[] { typeof(Stream) });

                        var param1 = Expression.Parameter(typeof(Stream), "stream");
                        var body = Expression.Convert(Expression.Call(param0, deserialize, param1), typeof(object));
                        var lambda = Expression.Lambda<Func<MessagePackSerializer, Stream, object>>(body, param0, param1).Compile();

                        this.deserialize3 = lambda;
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
                        // public static T Deserialize<T>(Stream stream, bool readStrict)
                        var deserialize = GetMethod(type, new Type[] { typeof(Stream), typeof(bool) });

                        var param1 = Expression.Parameter(typeof(Stream), "stream");
                        var param2 = Expression.Parameter(typeof(bool), "readStrict");
                        var body = Expression.Convert(Expression.Call(param0, deserialize, param1, param2), typeof(object));
                        var lambda = Expression.Lambda<Func<MessagePackSerializer, Stream, bool, object>>(body, param0, param1, param2).Compile();

                        this.deserialize5 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(Stream stream, IFormatterResolver resolver, bool readStrict)
                        var deserialize = GetMethod(type, new Type[] { typeof(Stream), typeof(IFormatterResolver), typeof(bool) });

                        var param1 = Expression.Parameter(typeof(Stream), "stream");
                        var param2 = Expression.Parameter(typeof(IFormatterResolver), "resolver");
                        var param3 = Expression.Parameter(typeof(bool), "readStrict");
                        var body = Expression.Convert(Expression.Call(param0, deserialize, param1, param2, param3), typeof(object));
                        var lambda = Expression.Lambda<Func<MessagePackSerializer, Stream, IFormatterResolver, bool, object>>(body, param0, param1, param2, param3).Compile();

                        this.deserialize6 = lambda;
                    }

                    {
                        // public static T Deserialize<T>(ArraySegment<byte> bytes)
                        var deserialize = GetMethod(type, new Type[] { typeof(ArraySegment<byte>) });

                        var param1 = Expression.Parameter(typeof(ArraySegment<byte>), "bytes");
                        var body = Expression.Convert(Expression.Call(param0, deserialize, param1), typeof(object));
                        var lambda = Expression.Lambda<Func<MessagePackSerializer, ArraySegment<byte>, object>>(body, param0, param1).Compile();

                        this.deserialize7 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(ArraySegment<byte> bytes, IFormatterResolver resolver)
                        var deserialize = GetMethod(type, new Type[] { typeof(ArraySegment<byte>), typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(ArraySegment<byte>), "bytes");
                        var param2 = Expression.Parameter(typeof(IFormatterResolver), "resolver");
                        var body = Expression.Convert(Expression.Call(param0, deserialize, param1, param2), typeof(object));
                        var lambda = Expression.Lambda<Func<MessagePackSerializer, ArraySegment<byte>, IFormatterResolver, object>>(body, param0, param1, param2).Compile();

                        this.deserialize8 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(byte[] bytes, int offset, IFormatterResolver resolver, out int readSize)
                        var deserialize = GetMethod(type, new Type[] { typeof(byte[]), typeof(int), typeof(IFormatterResolver), typeof(int).MakeByRefType() });

                        var param1 = Expression.Parameter(typeof(byte[]), "bytes");
                        var param2 = Expression.Parameter(typeof(int), "offset");
                        var param3 = Expression.Parameter(typeof(IFormatterResolver), "resolver");
                        var param4 = Expression.Parameter(typeof(int).MakeByRefType(), "readSize");
                        var body = Expression.Convert(Expression.Call(param0, deserialize, param1, param2, param3, param4), typeof(object));
                        var lambda = Expression.Lambda<RawFormatterDeserialize>(body, param0, param1, param2, param3, param4).Compile();

                        this.deserialize9 = lambda;
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
