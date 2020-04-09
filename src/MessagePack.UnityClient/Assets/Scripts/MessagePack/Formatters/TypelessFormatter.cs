// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY_2018_3_OR_NEWER

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using MessagePack.Internal;

namespace MessagePack.Formatters
{
    /// <summary>
    /// For `object` field that holds derived from `object` value, ex: var arr = new object[] { 1, "a", new Model() };.
    /// </summary>
    public sealed class TypelessFormatter : IMessagePackFormatter<object>
    {
        private delegate void SerializeMethod(object dynamicContractlessFormatter, ref MessagePackWriter writer, object value, MessagePackSerializerOptions options);

        private delegate object DeserializeMethod(object dynamicContractlessFormatter, ref MessagePackReader reader, MessagePackSerializerOptions options);

        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly IMessagePackFormatter<object> Instance = new TypelessFormatter();

        private readonly ThreadsafeTypeKeyHashTable<SerializeMethod> serializers = new ThreadsafeTypeKeyHashTable<SerializeMethod>();
        private readonly ThreadsafeTypeKeyHashTable<DeserializeMethod> deserializers = new ThreadsafeTypeKeyHashTable<DeserializeMethod>();
        private readonly ThreadsafeTypeKeyHashTable<byte[]> fullTypeNameCache = new ThreadsafeTypeKeyHashTable<byte[]>();
        private readonly ThreadsafeTypeKeyHashTable<byte[]> shortenedTypeNameCache = new ThreadsafeTypeKeyHashTable<byte[]>();
        private readonly AsymmetricKeyHashTable<byte[], ArraySegment<byte>, Type> typeCache = new AsymmetricKeyHashTable<byte[], ArraySegment<byte>, Type>(new StringArraySegmentByteAscymmetricEqualityComparer());

        private static readonly HashSet<Type> UseBuiltinTypes = new HashSet<Type>
        {
            typeof(Boolean),
            ////typeof(Char),
            typeof(SByte),
            typeof(Byte),
            typeof(Int16),
            typeof(UInt16),
            typeof(Int32),
            typeof(UInt32),
            typeof(Int64),
            typeof(UInt64),
            typeof(Single),
            typeof(Double),
            typeof(string),
            typeof(byte[]),

            // array should save their types.
            ////typeof(Boolean[]),
            ////typeof(Char[]),
            ////typeof(SByte[]),
            ////typeof(Int16[]),
            ////typeof(UInt16[]),
            ////typeof(Int32[]),
            ////typeof(UInt32[]),
            ////typeof(Int64[]),
            ////typeof(UInt64[]),
            ////typeof(Single[]),
            ////typeof(Double[]),
            ////typeof(string[]),

            typeof(Boolean?),
            ////typeof(Char?),
            typeof(SByte?),
            typeof(Byte?),
            typeof(Int16?),
            typeof(UInt16?),
            typeof(Int32?),
            typeof(UInt32?),
            typeof(Int64?),
            typeof(UInt64?),
            typeof(Single?),
            typeof(Double?),
        };

        //ForceSizePrimitiveObjectResolver.Instance,
        //ContractlessStandardResolverAllowPrivate.Instance);

        // mscorlib or System.Private.CoreLib
        private static readonly bool IsMscorlib = typeof(int).AssemblyQualifiedName.Contains("mscorlib");

        private TypelessFormatter()
        {
            this.serializers.TryAdd(typeof(object), _ => (object p1, ref MessagePackWriter p2, object p3, MessagePackSerializerOptions p4) => { });
            this.deserializers.TryAdd(typeof(object), _ => (object p1, ref MessagePackReader p2, MessagePackSerializerOptions p3) => new object());
        }

        private string BuildTypeName(Type type, MessagePackSerializerOptions options)
        {
            if (options.OmitAssemblyVersion)
            {
                string full = type.AssemblyQualifiedName;

                var shortened = MessagePackSerializerOptions.AssemblyNameVersionSelectorRegex.Replace(full, string.Empty);
                if (Type.GetType(shortened, false) == null)
                {
                    // if type cannot be found with shortened name - use full name
                    shortened = full;
                }

                return shortened;
            }
            else
            {
                return type.AssemblyQualifiedName;
            }
        }

        public void Serialize(ref MessagePackWriter writer, object value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            Type type = value.GetType();

            byte[] typeName;
            var typeNameCache = options.OmitAssemblyVersion ? this.shortenedTypeNameCache : this.fullTypeNameCache;
            if (!typeNameCache.TryGetValue(type, out typeName))
            {
                TypeInfo ti = type.GetTypeInfo();
                if (ti.IsAnonymous() || UseBuiltinTypes.Contains(type))
                {
                    typeName = null;
                }
                else
                {
                    typeName = StringEncoding.UTF8.GetBytes(this.BuildTypeName(type, options));
                }

                typeNameCache.TryAdd(type, typeName);
            }

            if (typeName == null)
            {
                DynamicObjectTypeFallbackFormatter.Instance.Serialize(ref writer, value, options);
                return;
            }

            var formatter = options.Resolver.GetFormatterDynamicWithVerify(type);

            // don't use GetOrAdd for avoid closure capture.
            if (!this.serializers.TryGetValue(type, out SerializeMethod serializeMethod))
            {
                // double check locking...
                lock (this.serializers)
                {
                    if (!this.serializers.TryGetValue(type, out serializeMethod))
                    {
                        TypeInfo ti = type.GetTypeInfo();

                        Type formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
                        ParameterExpression param0 = Expression.Parameter(typeof(object), "formatter");
                        ParameterExpression param1 = Expression.Parameter(typeof(MessagePackWriter).MakeByRefType(), "writer");
                        ParameterExpression param2 = Expression.Parameter(typeof(object), "value");
                        ParameterExpression param3 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

                        MethodInfo serializeMethodInfo = formatterType.GetRuntimeMethod("Serialize", new[] { typeof(MessagePackWriter).MakeByRefType(), type, typeof(MessagePackSerializerOptions) });

                        MethodCallExpression body = Expression.Call(
                            Expression.Convert(param0, formatterType),
                            serializeMethodInfo,
                            param1,
                            ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                            param3);

                        serializeMethod = Expression.Lambda<SerializeMethod>(body, param0, param1, param2, param3).Compile();

                        this.serializers.TryAdd(type, serializeMethod);
                    }
                }
            }

            // mark will be written at the end, when size is known
            using (var scratchRental = SequencePool.Shared.Rent())
            {
                MessagePackWriter scratchWriter = writer.Clone(scratchRental.Value);
                scratchWriter.WriteString(typeName);
                serializeMethod(formatter, ref scratchWriter, value, options);
                scratchWriter.Flush();

                // mark as extension with code 100
                writer.WriteExtensionFormat(new ExtensionResult((sbyte)ThisLibraryExtensionTypeCodes.TypelessFormatter, scratchRental.Value));
            }
        }

        public object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.NextMessagePackType == MessagePackType.Extension)
            {
                ExtensionHeader ext = reader.ReadExtensionFormatHeader();
                if (ext.TypeCode == ThisLibraryExtensionTypeCodes.TypelessFormatter)
                {
                    // it has type name serialized
                    ReadOnlySequence<byte> typeName = reader.ReadStringSequence().Value;
                    ArraySegment<byte> typeNameArraySegment;
                    byte[] rented = null;
                    if (!typeName.IsSingleSegment || !MemoryMarshal.TryGetArray(typeName.First, out typeNameArraySegment))
                    {
                        rented = ArrayPool<byte>.Shared.Rent((int)typeName.Length);
                        typeName.CopyTo(rented);
                        typeNameArraySegment = new ArraySegment<byte>(rented, 0, (int)typeName.Length);
                    }

                    var result = this.DeserializeByTypeName(typeNameArraySegment, ref reader, options);

                    if (rented != null)
                    {
                        ArrayPool<byte>.Shared.Return(rented);
                    }

                    return result;
                }
            }

            // fallback
            return DynamicObjectTypeFallbackFormatter.Instance.Deserialize(ref reader, options);
        }

        /// <summary>
        /// Does not support deserializing of anonymous types
        /// Type should be covered by preceeding resolvers in complex/standard resolver.
        /// </summary>
        private object DeserializeByTypeName(ArraySegment<byte> typeName, ref MessagePackReader byteSequence, MessagePackSerializerOptions options)
        {
            // try get type with assembly name, throw if not found
            Type type;
            if (!this.typeCache.TryGetValue(typeName, out type))
            {
                var buffer = new byte[typeName.Count];
                Buffer.BlockCopy(typeName.Array, typeName.Offset, buffer, 0, buffer.Length);
                var str = StringEncoding.UTF8.GetString(buffer);
                type = options.LoadType(str);
                if (type == null)
                {
                    if (IsMscorlib && str.Contains("System.Private.CoreLib"))
                    {
                        str = str.Replace("System.Private.CoreLib", "mscorlib");
                        type = Type.GetType(str, true); // throw
                    }
                    else if (!IsMscorlib && str.Contains("mscorlib"))
                    {
                        str = str.Replace("mscorlib", "System.Private.CoreLib");
                        type = Type.GetType(str, true); // throw
                    }
                    else
                    {
                        type = Type.GetType(str, true); // re-throw
                    }
                }

                this.typeCache.TryAdd(buffer, type);
            }

            options.ThrowIfDeserializingTypeIsDisallowed(type);

            var formatter = options.Resolver.GetFormatterDynamicWithVerify(type);

            if (!this.deserializers.TryGetValue(type, out DeserializeMethod deserializeMethod))
            {
                lock (this.deserializers)
                {
                    if (!this.deserializers.TryGetValue(type, out deserializeMethod))
                    {
                        TypeInfo ti = type.GetTypeInfo();

                        Type formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
                        ParameterExpression param0 = Expression.Parameter(typeof(object), "formatter");
                        ParameterExpression param1 = Expression.Parameter(typeof(MessagePackReader).MakeByRefType(), "reader");
                        ParameterExpression param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

                        MethodInfo deserializeMethodInfo = formatterType.GetRuntimeMethod("Deserialize", new[] { typeof(MessagePackReader).MakeByRefType(), typeof(MessagePackSerializerOptions) });

                        MethodCallExpression deserialize = Expression.Call(
                            Expression.Convert(param0, formatterType),
                            deserializeMethodInfo,
                            param1,
                            param2);

                        Expression body = deserialize;
                        if (ti.IsValueType)
                        {
                            body = Expression.Convert(deserialize, typeof(object));
                        }

                        deserializeMethod = Expression.Lambda<DeserializeMethod>(body, param0, param1, param2).Compile();

                        this.deserializers.TryAdd(type, deserializeMethod);
                    }
                }
            }

            return deserializeMethod(formatter, ref byteSequence, options);
        }
    }
}

#endif
