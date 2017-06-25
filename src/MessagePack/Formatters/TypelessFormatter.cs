#if NETSTANDARD1_4

using MessagePack.Internal;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MessagePack.Formatters
{
    /// <summary>
    /// For `object` field that holds derived from `object` value, ex: var arr = new object[] { 1, "a", new Model() };
    /// </summary>
    public class TypelessFormatter : IMessagePackFormatter<object>
    {
#if NETSTANDARD1_4
        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled);
#else
        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+");
#endif
        delegate int SerializeMethod(object dynamicContractlessFormatter, ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver);
        delegate object DeserializeMethod(object dynamicContractlessFormatter, byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);

        public static readonly IMessagePackFormatter<object> Instance = new TypelessFormatter();

        static readonly Dictionary<Type, KeyValuePair<object, SerializeMethod>> serializers = new Dictionary<Type, KeyValuePair<object, SerializeMethod>>();
        static readonly Dictionary<Type, KeyValuePair<object, DeserializeMethod>> deserializers = new Dictionary<Type, KeyValuePair<object, DeserializeMethod>>();

        readonly HashSet<string> blacklistCheck;

        /// <summary>
        /// When type name does not have Version, Culture, Public token - sometimes can not find type, example - ExpandoObject
        /// </summary>
        public static volatile bool RemoveAssemblyVersion = true;

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
                return SubtractFullNameRegex.Replace(type.AssemblyQualifiedName, "");
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
            var ti = type.GetTypeInfo();

            if (PrimitiveObjectFormatter.IsSupportedType(type, ti))
            {
                return PrimitiveObjectFormatter.Instance.Serialize(ref bytes, offset, value, formatterResolver);
            }

            var typeName = BuildTypeName(type);
            if (blacklistCheck.Contains(type.FullName))
            {
                throw new InvalidOperationException("Type is in blacklist:" + type.FullName);
            }

            KeyValuePair<object, SerializeMethod> formatterAndDelegate;
            lock (serializers)
            {
                if (!serializers.TryGetValue(type, out formatterAndDelegate))
                {
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

                    serializers[type] = formatterAndDelegate;
                }
            }

            if (!ti.IsAnonymous()) // anonymous type does not support typed deserializing yet
            {
                // mark as extension with code 100
                var startOffset = offset;
                offset += 6; // mark will be written at the end, when size is known
                offset += MessagePackBinary.WriteString(ref bytes, offset, typeName);
                offset += formatterAndDelegate.Value(formatterAndDelegate.Key, ref bytes, offset, value, formatterResolver);
                MessagePackBinary.WriteExtensionFormatHeaderForceExt32Block(ref bytes, startOffset, (sbyte)ReservedMessagePackExtensionTypeCode.DynamicObjectWithTypeName, offset - startOffset - 6);
                return offset - startOffset;
            }
            else
            {
                return DynamicObjectTypeFallbackFormatter.Instance.Serialize(ref bytes, offset, value, formatterResolver);
            }
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
            if (packType == MessagePackType.Extension)
            {
                var ext = MessagePackBinary.ReadExtensionFormatHeader(bytes, offset, out readSize);
                if (ext.TypeCode == ReservedMessagePackExtensionTypeCode.DynamicObjectWithTypeName)
                {
                    // it has type name serialized
                    offset += readSize;
                    var typeName = MessagePackBinary.ReadString(bytes, offset, out readSize);
                    offset += readSize;
                    var result = DeserializeByTypeName(typeName, bytes, offset, formatterResolver, out readSize);
                    offset += readSize;
                    readSize = offset - startOffset;
                    return result;
                }
            }
            // fallback to primitive object formatter
            return PrimitiveObjectFormatter.Instance.Deserialize(bytes, startOffset, formatterResolver, out readSize);
        }

        /// <summary>
        /// Does not support deserializing of anonymous types
        /// Assembly with serialized type have to be shared between serializer app and deseralizer app
        /// </summary>
        private object DeserializeByTypeName(string typeName, byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            // try get type with assembly name, throw if not found
            var type = Type.GetType(typeName, true);
            var ti = type.GetTypeInfo();

            KeyValuePair<object, DeserializeMethod> formatterAndDelegate;
            lock (deserializers)
            {
                if (!deserializers.TryGetValue(type, out formatterAndDelegate))
                {
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

                    var body = Expression.Call(
                        Expression.Convert(param0, formatterType),
                        deserializeMethodInfo,
                        param1,
                        param2,
                        param3,
                        param4);

                    var lambda = Expression.Lambda<DeserializeMethod>(body, param0, param1, param2, param3, param4).Compile();

                    formatterAndDelegate = new KeyValuePair<object, DeserializeMethod>(formatter, lambda);

                    deserializers[type] = formatterAndDelegate;
                }
            }

            return formatterAndDelegate.Value(formatterAndDelegate.Key, bytes, offset, formatterResolver, out readSize);
        }
    }
}

#endif