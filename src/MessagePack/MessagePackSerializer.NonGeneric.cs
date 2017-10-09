#if NETSTANDARD

using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MessagePack
{
    // NonGeneric API
    public static partial class MessagePackSerializer
    {
        public static class NonGeneric
        {
            static readonly Func<Type, CompiledMethods> CreateCompiledMethods;
            static readonly MessagePack.Internal.ThreadsafeTypeKeyHashTable<CompiledMethods> serializes = new MessagePack.Internal.ThreadsafeTypeKeyHashTable<CompiledMethods>(capacity: 64);

            static NonGeneric()
            {
                CreateCompiledMethods = t => new CompiledMethods(t);
            }

            public static byte[] Serialize(Type type, object obj)
            {
                return GetOrAdd(type).serialize1.Invoke(obj);
            }

            public static byte[] Serialize(Type type, object obj, IFormatterResolver resolver)
            {
                return GetOrAdd(type).serialize2.Invoke(obj, resolver);
            }

            public static void Serialize(Type type, Stream stream, object obj)
            {
                GetOrAdd(type).serialize3.Invoke(stream, obj);
            }

            public static void Serialize(Type type, Stream stream, object obj, IFormatterResolver resolver)
            {
                GetOrAdd(type).serialize4.Invoke(stream, obj, resolver);
            }

            public static object Deserialize(Type type, byte[] bytes)
            {
                return GetOrAdd(type).deserialize1.Invoke(bytes);
            }

            public static object Deserialize(Type type, byte[] bytes, IFormatterResolver resolver)
            {
                return GetOrAdd(type).deserialize2.Invoke(bytes, resolver);
            }

            public static object Deserialize(Type type, Stream stream)
            {
                return GetOrAdd(type).deserialize3.Invoke(stream);
            }

            public static object Deserialize(Type type, Stream stream, IFormatterResolver resolver)
            {
                return GetOrAdd(type).deserialize4.Invoke(stream, resolver);
            }

            public static object Deserialize(Type type, Stream stream, bool readStrict)
            {
                return GetOrAdd(type).deserialize5.Invoke(stream, readStrict);
            }

            public static object Deserialize(Type type, Stream stream, IFormatterResolver resolver, bool readStrict)
            {
                return GetOrAdd(type).deserialize6.Invoke(stream, resolver, readStrict);
            }

            public static object Deserialize(Type type, ArraySegment<byte> bytes)
            {
                return GetOrAdd(type).deserialize7.Invoke(bytes);
            }

            public static object Deserialize(Type type, ArraySegment<byte> bytes, IFormatterResolver resolver)
            {
                return GetOrAdd(type).deserialize8.Invoke(bytes, resolver);
            }

            static CompiledMethods GetOrAdd(Type type)
            {
                return serializes.GetOrAdd(type, CreateCompiledMethods);
            }

            class CompiledMethods
            {
                public readonly Func<object, byte[]> serialize1;
                public readonly Func<object, IFormatterResolver, byte[]> serialize2;
                public readonly Action<Stream, object> serialize3;
                public readonly Action<Stream, object, IFormatterResolver> serialize4;

                public readonly Func<byte[], object> deserialize1;
                public readonly Func<byte[], IFormatterResolver, object> deserialize2;
                public readonly Func<Stream, object> deserialize3;
                public readonly Func<Stream, IFormatterResolver, object> deserialize4;
                public readonly Func<Stream, bool, object> deserialize5;
                public readonly Func<Stream, IFormatterResolver, bool, object> deserialize6;

                public readonly Func<ArraySegment<byte>, object> deserialize7;
                public readonly Func<ArraySegment<byte>, IFormatterResolver, object> deserialize8;

                public CompiledMethods(Type type)
                {
                    var ti = type.GetTypeInfo();
                    {
                        // public static byte[] Serialize<T>(T obj)
                        var serialize = GetMethod(type, new Type[] { null });

                        var param1 = Expression.Parameter(typeof(object), "obj");
                        var body = Expression.Call(serialize, ti.IsValueType
                            ? Expression.Unbox(param1, type)
                            : Expression.Convert(param1, type));
                        var lambda = Expression.Lambda<Func<object, byte[]>>(body, param1).Compile();

                        this.serialize1 = lambda;
                    }
                    {
                        // public static byte[] Serialize<T>(T obj, IFormatterResolver resolver)
                        var serialize = GetMethod(type, new Type[] { null, typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(object), "obj");
                        var param2 = Expression.Parameter(typeof(IFormatterResolver), "formatterResolver");

                        var body = Expression.Call(serialize, ti.IsValueType
                            ? Expression.Unbox(param1, type)
                            : Expression.Convert(param1, type), param2);
                        var lambda = Expression.Lambda<Func<object, IFormatterResolver, byte[]>>(body, param1, param2).Compile();

                        this.serialize2 = lambda;
                    }
                    {
                        // public static void Serialize<T>(Stream stream, T obj)
                        var serialize = GetMethod(type, new Type[] { typeof(Stream), null });

                        var param1 = Expression.Parameter(typeof(Stream), "stream");
                        var param2 = Expression.Parameter(typeof(object), "obj");

                        var body = Expression.Call(serialize, param1, ti.IsValueType
                            ? Expression.Unbox(param2, type)
                            : Expression.Convert(param2, type));
                        var lambda = Expression.Lambda<Action<Stream, object>>(body, param1, param2).Compile();

                        this.serialize3 = lambda;
                    }
                    {
                        // public static void Serialize<T>(Stream stream, T obj, IFormatterResolver resolver)
                        var serialize = GetMethod(type, new Type[] { typeof(Stream), null, typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(Stream), "stream");
                        var param2 = Expression.Parameter(typeof(object), "obj");
                        var param3 = Expression.Parameter(typeof(IFormatterResolver), "formatterResolver");

                        var body = Expression.Call(serialize, param1, ti.IsValueType
                            ? Expression.Unbox(param2, type)
                            : Expression.Convert(param2, type), param3);
                        var lambda = Expression.Lambda<Action<Stream, object, IFormatterResolver>>(body, param1, param2, param3).Compile();

                        this.serialize4 = lambda;
                    }

                    {
                        // public static T Deserialize<T>(byte[] bytes)
                        var deserialize = GetMethod(type, new Type[] { typeof(byte[]) });

                        var param1 = Expression.Parameter(typeof(byte[]), "bytes");
                        var body = Expression.Convert(Expression.Call(deserialize, param1), typeof(object));
                        var lambda = Expression.Lambda<Func<byte[], object>>(body, param1).Compile();

                        this.deserialize1 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(byte[] bytes, IFormatterResolver resolver)
                        var deserialize = GetMethod(type, new Type[] { typeof(byte[]), typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(byte[]), "bytes");
                        var param2 = Expression.Parameter(typeof(IFormatterResolver), "resolver");
                        var body = Expression.Convert(Expression.Call(deserialize, param1, param2), typeof(object));
                        var lambda = Expression.Lambda<Func<byte[], IFormatterResolver, object>>(body, param1, param2).Compile();

                        this.deserialize2 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(Stream stream)
                        var deserialize = GetMethod(type, new Type[] { typeof(Stream) });

                        var param1 = Expression.Parameter(typeof(Stream), "stream");
                        var body = Expression.Convert(Expression.Call(deserialize, param1), typeof(object));
                        var lambda = Expression.Lambda<Func<Stream, object>>(body, param1).Compile();

                        this.deserialize3 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(Stream stream, IFormatterResolver resolver)
                        var deserialize = GetMethod(type, new Type[] { typeof(Stream), typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(Stream), "stream");
                        var param2 = Expression.Parameter(typeof(IFormatterResolver), "resolver");
                        var body = Expression.Convert(Expression.Call(deserialize, param1, param2), typeof(object));
                        var lambda = Expression.Lambda<Func<Stream, IFormatterResolver, object>>(body, param1, param2).Compile();

                        this.deserialize4 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(Stream stream, bool readStrict)
                        var deserialize = GetMethod(type, new Type[] { typeof(Stream), typeof(bool) });

                        var param1 = Expression.Parameter(typeof(Stream), "stream");
                        var param2 = Expression.Parameter(typeof(bool), "readStrict");
                        var body = Expression.Convert(Expression.Call(deserialize, param1, param2), typeof(object));
                        var lambda = Expression.Lambda<Func<Stream, bool, object>>(body, param1, param2).Compile();

                        this.deserialize5 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(Stream stream, IFormatterResolver resolver, bool readStrict)
                        var deserialize = GetMethod(type, new Type[] { typeof(Stream), typeof(IFormatterResolver), typeof(bool) });

                        var param1 = Expression.Parameter(typeof(Stream), "stream");
                        var param2 = Expression.Parameter(typeof(IFormatterResolver), "resolver");
                        var param3 = Expression.Parameter(typeof(bool), "readStrict");
                        var body = Expression.Convert(Expression.Call(deserialize, param1, param2, param3), typeof(object));
                        var lambda = Expression.Lambda<Func<Stream, IFormatterResolver, bool, object>>(body, param1, param2, param3).Compile();

                        this.deserialize6 = lambda;
                    }

                    {
                        // public static T Deserialize<T>(ArraySegment<byte> bytes)
                        var deserialize = GetMethod(type, new Type[] { typeof(ArraySegment<byte>) });

                        var param1 = Expression.Parameter(typeof(ArraySegment<byte>), "bytes");
                        var body = Expression.Convert(Expression.Call(deserialize, param1), typeof(object));
                        var lambda = Expression.Lambda<Func<ArraySegment<byte>, object>>(body, param1).Compile();

                        this.deserialize7 = lambda;
                    }
                    {
                        // public static T Deserialize<T>(ArraySegment<byte> bytes, IFormatterResolver resolver)
                        var deserialize = GetMethod(type, new Type[] { typeof(ArraySegment<byte>), typeof(IFormatterResolver) });

                        var param1 = Expression.Parameter(typeof(ArraySegment<byte>), "bytes");
                        var param2 = Expression.Parameter(typeof(IFormatterResolver), "resolver");
                        var body = Expression.Convert(Expression.Call(deserialize, param1, param2), typeof(object));
                        var lambda = Expression.Lambda<Func<ArraySegment<byte>, IFormatterResolver, object>>(body, param1, param2).Compile();

                        this.deserialize8 = lambda;
                    }
                }

                // null is generic type marker.
                static MethodInfo GetMethod(Type type, Type[] parameters)
                {
                    return typeof(MessagePackSerializer).GetRuntimeMethods().Where(x =>
                    {
                        if (!(x.Name == "Serialize" || x.Name == "Deserialize")) return false;
                        var ps = x.GetParameters();
                        if (ps.Length != parameters.Length) return false;
                        for (int i = 0; i < ps.Length; i++)
                        {
                            if (parameters[i] == null && ps[i].ParameterType.IsGenericParameter) continue;
                            if (ps[i].ParameterType != parameters[i]) return false;
                        }
                        return true;
                    })
                    .Single()
                    .MakeGenericMethod(type);
                }
            }
        }
    }
}

#endif