#if NETSTANDARD1_4
using MessagePack.Internal;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MessagePack.Formatters
{
    /// <summary>
    /// For `object` field that holds derived from `object` value, ex: var arr = new object[] { 1, "a", new Model() };
    /// </summary>
    public sealed class TypelessFormatter : IMessagePackFormatter<object>
    {
        public const sbyte ExtensionTypeCode = 100;

        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled);

        delegate int SerializeMethod(object dynamicContractlessFormatter, ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver);
        delegate object DeserializeMethod(object dynamicContractlessFormatter, byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);

        public static readonly IMessagePackFormatter<object> Instance = new TypelessFormatter();

        static readonly ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>> serializers = new ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>>();
        static readonly ThreadsafeTypeKeyHashTable<KeyValuePair<object, DeserializeMethod>> deserializers = new ThreadsafeTypeKeyHashTable<KeyValuePair<object, DeserializeMethod>>();
        static readonly ThreadsafeTypeKeyHashTable<byte[]> typeNameCache = new ThreadsafeTypeKeyHashTable<byte[]>();
        static readonly AsymmetricKeyHashTable<byte[], ArraySegment<byte>, Type> typeCache = new AsymmetricKeyHashTable<byte[], ArraySegment<byte>, Type>(new StringArraySegmentByteAscymmetricEqualityComparer());

        readonly HashSet<string> blacklistCheck;

        /// <summary>
        /// When type name does not have Version, Culture, Public token - sometimes can not find type, example - ExpandoObject
        /// In that can set to `false`
        /// </summary>
        public static volatile bool RemoveAssemblyVersion = true;
        private static ConcurrentDictionary<string, string> shortenedTypeNames = new ConcurrentDictionary<string, string>();

        TypelessFormatter()
        {
            blacklistCheck = new HashSet<string>()
            {
                "System.CodeDom.Compiler.TempFileCollection",
                "System.IO.FileSystemInfo",
                "System.Management.IWbemClassObjectFreeThreaded"
            };
        }

        // see:http://msdn.microsoft.com/en-us/library/w3f99sx1.aspx
        // subtract Version, Culture and PublicKeyToken from AssemblyQualifiedName 
        internal static string BuildTypeName(Type type)
        {
            if (RemoveAssemblyVersion)
            {
                string full = type.AssemblyQualifiedName;
                string shortened;
                if (shortenedTypeNames.TryGetValue(full, out shortened))
                {
                    return shortened;
                }
                else
                {
                    shortened = SubtractFullNameRegex.Replace(full, "");
                    if (Type.GetType(shortened, false) == null)
                    {
                        // if type cannot be found with shortened name - use full name
                        shortened = full;
                    }
                }
                shortenedTypeNames.TryAdd(full, shortened);
                return shortened;
            }
            else
                return type.AssemblyQualifiedName;
        }

        public int Serialize(ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var type = value.GetType();
            
            var typeName = typeNameCache.GetOrAdd(type, t =>
            {
                if (blacklistCheck.Contains(t.FullName))
                {
                    throw new InvalidOperationException("Type is in blacklist:" + type.FullName);
                }

                var ti = type.GetTypeInfo();

                // reduce check cost cache.
                // TODO:don't use value.
                if ((PrimitiveObjectFormatter.IsSupportedType(type, ti, value)
                    && !(value is DateTime)
                    && !(value is IDictionary)
                    && !(value is ICollection))
                    || ti.IsAnonymous())
                {
                    return null;
                }

                return StringEncoding.UTF8.GetBytes(BuildTypeName(t));
            });

            if (value == null)
            {
                return Resolvers.ContractlessStandardResolver.Instance.GetFormatter<object>().Serialize(ref bytes, offset, value, formatterResolver);
            }

            KeyValuePair<object, SerializeMethod> formatterAndDelegate;
            if (type == typeof(object))
            {
                formatterAndDelegate = new KeyValuePair<object, SerializeMethod>(null, (object p1, ref byte[] p2, int p3, object p4, IFormatterResolver p5) => 0);
            }
            else
            {
                if (!serializers.TryGetValue(type, out formatterAndDelegate))
                {
                    var ti = type.GetTypeInfo();

                    var formatter = formatterResolver.GetFormatterDynamic(type);
                    if (formatter == null)
                    {
                        throw new FormatterNotRegisteredException(type.FullName + " is not registered in this resolver. resolver:" + formatterResolver.GetType().Name);
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

                    // TODO:remove closure capture.
                    serializers.TryAdd(type, _ => formatterAndDelegate);
                }
            }
            // mark as extension with code 100
            var startOffset = offset;
            offset += 6; // mark will be written at the end, when size is known
            offset += MessagePackBinary.WriteStringBytes(ref bytes, offset, typeName);
            offset += formatterAndDelegate.Value(formatterAndDelegate.Key, ref bytes, offset, value, formatterResolver);
            MessagePackBinary.WriteExtensionFormatHeaderForceExt32Block(ref bytes, startOffset, (sbyte)TypelessFormatter.ExtensionTypeCode, offset - startOffset - 6);
            return offset - startOffset;
        }

        public object Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            int startOffset = offset;
            var packType = MessagePackBinary.GetMessagePackType(bytes, offset);
            switch (packType)
            {
                case MessagePackType.Extension:
                    {
                        var ext = MessagePackBinary.ReadExtensionFormatHeader(bytes, offset, out readSize);
                        if (ext.TypeCode == TypelessFormatter.ExtensionTypeCode)
                        {
                            // it has type name serialized
                            offset += readSize;
                            var typeName = MessagePackBinary.ReadStringSegment(bytes, offset, out readSize);
                            offset += readSize;
                            var result = DeserializeByTypeName(typeName, bytes, offset, formatterResolver, out readSize);
                            offset += readSize;
                            readSize = offset - startOffset;
                            return result;
                        }
                        break;
                    }
            }
            // fallback
            return Resolvers.ContractlessStandardResolver.Instance.GetFormatter<object>().Deserialize(bytes, startOffset, formatterResolver, out readSize);
        }

        /// <summary>
        /// Does not support deserializing of anonymous types
        /// Type should be covered by preceeding resolvers in complex/standard resolver
        /// </summary>
        private object DeserializeByTypeName(ArraySegment<byte> typeName, byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            // try get type with assembly name, throw if not found
            Type type;
            if (!typeCache.TryGetValue(typeName, out type))
            {
                var buffer = new byte[typeName.Count];
                Buffer.BlockCopy(typeName.Array, typeName.Offset, buffer, 0, buffer.Length);
                type = typeCache.AddOrGet(buffer, xs =>
                {
                    var str = StringEncoding.UTF8.GetString(xs);
                    return Type.GetType(str, true);
                });
            }

            KeyValuePair<object, DeserializeMethod> formatterAndDelegate;
            if (type == typeof(object))
            {
                formatterAndDelegate = new KeyValuePair<object, DeserializeMethod>(null, (object p1, byte[] p2, int p3, IFormatterResolver p4, out int p5) =>
                {
                    p5 = 0;
                    return new object();
                });
            }
            else
            {
                if (!deserializers.TryGetValue(type, out formatterAndDelegate))
                {
                    var ti = type.GetTypeInfo();

                    var formatter = formatterResolver.GetFormatterDynamic(type);
                    if (formatter == null)
                    {
                        throw new FormatterNotRegisteredException(type.FullName + " is not registered in this resolver. resolver:" + formatterResolver.GetType().Name);
                    }

                    var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
                    var param0 = Expression.Parameter(typeof(object), "formatter");
                    var param1 = Expression.Parameter(typeof(byte[]), "bytes");
                    var param2 = Expression.Parameter(typeof(int), "offset");
                    var param3 = Expression.Parameter(typeof(IFormatterResolver), "formatterResolver");
                    var param4 = Expression.Parameter(typeof(int).MakeByRefType(), "readSize");

                    var deserializeMethodInfo = formatterType.GetRuntimeMethod("Deserialize", new[] { typeof(byte[]), typeof(int), typeof(IFormatterResolver), typeof(int).MakeByRefType() });

                    var deserialize = Expression.Call(
                        Expression.Convert(param0, formatterType),
                        deserializeMethodInfo,
                        param1,
                        param2,
                        param3,
                        param4);

                    Expression body = deserialize;
                    if (ti.IsValueType)
                        body = Expression.Convert(deserialize, typeof(object));
                    var lambda = Expression.Lambda<DeserializeMethod>(body, param0, param1, param2, param3, param4).Compile();

                    formatterAndDelegate = new KeyValuePair<object, DeserializeMethod>(formatter, lambda);

                    // TODO:remove closure capture.
                    deserializers.TryAdd(type, _ => formatterAndDelegate);
                }
            }
            return formatterAndDelegate.Value(formatterAndDelegate.Key, bytes, offset, formatterResolver, out readSize);
        }
    }
}
#endif