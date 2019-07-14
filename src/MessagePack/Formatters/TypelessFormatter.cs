﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY

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
        public const sbyte ExtensionTypeCode = 100;

        private static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled);

        private delegate void SerializeMethod(object dynamicContractlessFormatter, in MessagePackWriter writer, object value, MessagePackSerializerOptions options);

        private delegate object DeserializeMethod(object dynamicContractlessFormatter, ref MessagePackReader reader, MessagePackSerializerOptions options);

        private readonly ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>> serializers = new ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>>();
        private readonly ThreadsafeTypeKeyHashTable<KeyValuePair<object, DeserializeMethod>> deserializers = new ThreadsafeTypeKeyHashTable<KeyValuePair<object, DeserializeMethod>>();
        private readonly ThreadsafeTypeKeyHashTable<byte[]> typeNameCache = new ThreadsafeTypeKeyHashTable<byte[]>();
        private readonly AsymmetricKeyHashTable<byte[], ArraySegment<byte>, Type> typeCache = new AsymmetricKeyHashTable<byte[], ArraySegment<byte>, Type>(new StringArraySegmentByteAscymmetricEqualityComparer());

        private static readonly HashSet<string> BlacklistCheck = new HashSet<string>
        {
            "System.CodeDom.Compiler.TempFileCollection",
            "System.IO.FileSystemInfo",
            "System.Management.IWbemClassObjectFreeThreaded",
        };

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

            // array should save there types.
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

        public Func<string, Type> BindToType { get; set; } = (string typeName) => Type.GetType(typeName, false);

        // mscorlib or System.Private.CoreLib
        private static readonly bool IsMscorlib = typeof(int).AssemblyQualifiedName.Contains("mscorlib");

        /// <summary>
        /// Gets or sets a value indicating whether to exclude assembly qualifiers from type names.
        /// </summary>
        /// <value>The default value is <c>true</c>.</value>
        public bool RemoveAssemblyVersion { get; set; } = true;

        public TypelessFormatter()
        {
            this.serializers.TryAdd(typeof(object), _ => new KeyValuePair<object, SerializeMethod>(null, (object p1, in MessagePackWriter p2, object p3, MessagePackSerializerOptions p4) => { }));
            this.deserializers.TryAdd(typeof(object), _ => new KeyValuePair<object, DeserializeMethod>(null, (object p1, ref MessagePackReader p2, MessagePackSerializerOptions p3) => new object()));
        }

        // see:http://msdn.microsoft.com/en-us/library/w3f99sx1.aspx
        // subtract Version, Culture and PublicKeyToken from AssemblyQualifiedName
        private string BuildTypeName(Type type)
        {
            if (this.RemoveAssemblyVersion)
            {
                string full = type.AssemblyQualifiedName;

                var shortened = SubtractFullNameRegex.Replace(full, string.Empty);
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

        public void Serialize(in MessagePackWriter writer, object value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            Type type = value.GetType();

            byte[] typeName;
            if (!this.typeNameCache.TryGetValue(type, out typeName))
            {
                if (BlacklistCheck.Contains(type.FullName))
                {
                    throw new InvalidOperationException("Type is in blacklist:" + type.FullName);
                }

                TypeInfo ti = type.GetTypeInfo();
                if (ti.IsAnonymous() || UseBuiltinTypes.Contains(type))
                {
                    typeName = null;
                }
                else
                {
                    typeName = StringEncoding.UTF8.GetBytes(this.BuildTypeName(type));
                }

                this.typeNameCache.TryAdd(type, typeName);
            }

            if (typeName == null)
            {
                Resolvers.TypelessFormatterFallbackResolver.Instance.GetFormatter<object>().Serialize(writer, value, options);
                return;
            }

            // don't use GetOrAdd for avoid closure capture.
            KeyValuePair<object, SerializeMethod> formatterAndDelegate;
            if (!this.serializers.TryGetValue(type, out formatterAndDelegate))
            {
                // double check locking...
                lock (this.serializers)
                {
                    if (!this.serializers.TryGetValue(type, out formatterAndDelegate))
                    {
                        TypeInfo ti = type.GetTypeInfo();

                        IFormatterResolver resolver = options.Resolver;
                        var formatter = resolver.GetFormatterDynamic(type);
                        if (formatter == null)
                        {
                            throw new FormatterNotRegisteredException(type.FullName + " is not registered in this resolver. resolver:" + resolver.GetType().Name);
                        }

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

                        SerializeMethod lambda = Expression.Lambda<SerializeMethod>(body, param0, param1, param2, param3).Compile();

                        formatterAndDelegate = new KeyValuePair<object, SerializeMethod>(formatter, lambda);
                        this.serializers.TryAdd(type, formatterAndDelegate);
                    }
                }
            }

            // mark will be written at the end, when size is known
            using (var scratch = new Nerdbank.Streams.Sequence<byte>())
            {
                MessagePackWriter scratchWriter = writer.Clone(scratch);
                scratchWriter.WriteString(typeName);
                formatterAndDelegate.Value(formatterAndDelegate.Key, scratchWriter, value, options);
                scratchWriter.Flush();

                // mark as extension with code 100
                writer.WriteExtensionFormat(new ExtensionResult((sbyte)TypelessFormatter.ExtensionTypeCode, scratch.AsReadOnlySequence));
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
                if (ext.TypeCode == TypelessFormatter.ExtensionTypeCode)
                {
                    // it has type name serialized
                    ReadOnlySequence<byte> typeName = reader.ReadStringSegment();
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
            return Resolvers.TypelessFormatterFallbackResolver.Instance.GetFormatter<object>().Deserialize(ref reader, options);
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
                type = this.BindToType(str);
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

            KeyValuePair<object, DeserializeMethod> formatterAndDelegate;
            if (!this.deserializers.TryGetValue(type, out formatterAndDelegate))
            {
                lock (this.deserializers)
                {
                    if (!this.deserializers.TryGetValue(type, out formatterAndDelegate))
                    {
                        TypeInfo ti = type.GetTypeInfo();

                        IFormatterResolver resolver = options.Resolver;
                        var formatter = resolver.GetFormatterDynamic(type);
                        if (formatter == null)
                        {
                            throw new FormatterNotRegisteredException(type.FullName + " is not registered in this resolver. resolver:" + resolver.GetType().Name);
                        }

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

                        DeserializeMethod lambda = Expression.Lambda<DeserializeMethod>(body, param0, param1, param2).Compile();

                        formatterAndDelegate = new KeyValuePair<object, DeserializeMethod>(formatter, lambda);
                        this.deserializers.TryAdd(type, formatterAndDelegate);
                    }
                }
            }

            return formatterAndDelegate.Value(formatterAndDelegate.Key, ref byteSequence, options);
        }
    }
}

#endif
