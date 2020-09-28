// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !(UNITY_2018_3_OR_NEWER && NET_STANDARD_2_0)

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using MessagePack.Formatters;
using MessagePack.Internal;

#pragma warning disable SA1403 // File may only contain a single namespace

namespace MessagePack.Resolvers
{
    /// <summary>
    /// ObjectResolver by dynamic code generation.
    /// </summary>
    public sealed class DynamicObjectResolver : IFormatterResolver
    {
        private const string ModuleName = "MessagePack.Resolvers.DynamicObjectResolver";

        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly DynamicObjectResolver Instance;

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        internal static readonly Lazy<DynamicAssembly> DynamicAssembly;

        static DynamicObjectResolver()
        {
            Instance = new DynamicObjectResolver();
            Options = new MessagePackSerializerOptions(Instance);
            DynamicAssembly = new Lazy<DynamicAssembly>(() => new DynamicAssembly(ModuleName));
        }

        private DynamicObjectResolver()
        {
        }

#if NETFRAMEWORK
        public AssemblyBuilder Save()
        {
            return DynamicAssembly.Value.Save();
        }
#endif

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                TypeInfo ti = typeof(T).GetTypeInfo();

                if (ti.IsInterface)
                {
                    return;
                }

                if (ti.IsNullable())
                {
                    ti = ti.GenericTypeArguments[0].GetTypeInfo();

                    var innerFormatter = DynamicObjectResolver.Instance.GetFormatterDynamic(ti.AsType());
                    if (innerFormatter == null)
                    {
                        return;
                    }

                    Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }

                if (ti.IsAnonymous())
                {
                    Formatter = (IMessagePackFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod(typeof(T), true, true, false);
                    return;
                }

                TypeInfo formatterTypeInfo = DynamicObjectTypeBuilder.BuildType(DynamicAssembly.Value, typeof(T), false, false);
                if (formatterTypeInfo == null)
                {
                    return;
                }

                Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterTypeInfo.AsType());
            }
        }
    }

    /// <summary>
    /// ObjectResolver by dynamic code generation, allow private member.
    /// </summary>
    public sealed class DynamicObjectResolverAllowPrivate : IFormatterResolver
    {
        public static readonly DynamicObjectResolverAllowPrivate Instance = new DynamicObjectResolverAllowPrivate();

        private DynamicObjectResolverAllowPrivate()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            internal static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                TypeInfo ti = typeof(T).GetTypeInfo();

                if (ti.IsInterface)
                {
                    return;
                }

                if (ti.IsNullable())
                {
                    ti = ti.GenericTypeArguments[0].GetTypeInfo();

                    var innerFormatter = DynamicObjectResolverAllowPrivate.Instance.GetFormatterDynamic(ti.AsType());
                    if (innerFormatter == null)
                    {
                        return;
                    }

                    Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }

                if (ti.IsAnonymous())
                {
                    Formatter = (IMessagePackFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod(typeof(T), true, true, false);
                }
                else
                {
                    Formatter = (IMessagePackFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod(typeof(T), false, false, true);
                }
            }
        }
    }

    /// <summary>
    /// ObjectResolver by dynamic code generation, no needs MessagePackObject attribute and serialized key as string.
    /// </summary>
    public sealed class DynamicContractlessObjectResolver : IFormatterResolver
    {
        public static readonly DynamicContractlessObjectResolver Instance = new DynamicContractlessObjectResolver();

        private const string ModuleName = "MessagePack.Resolvers.DynamicContractlessObjectResolver";

        private static readonly Lazy<DynamicAssembly> DynamicAssembly;

        private DynamicContractlessObjectResolver()
        {
        }

        static DynamicContractlessObjectResolver()
        {
            DynamicAssembly = new Lazy<DynamicAssembly>(() => new DynamicAssembly(ModuleName));
        }

#if NETFRAMEWORK
        public AssemblyBuilder Save()
        {
            return DynamicAssembly.Value.Save();
        }
#endif

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                if (typeof(T) == typeof(object))
                {
                    return;
                }

                TypeInfo ti = typeof(T).GetTypeInfo();

                if (ti.IsInterface)
                {
                    return;
                }

                if (ti.IsNullable())
                {
                    ti = ti.GenericTypeArguments[0].GetTypeInfo();

                    var innerFormatter = DynamicContractlessObjectResolver.Instance.GetFormatterDynamic(ti.AsType());
                    if (innerFormatter == null)
                    {
                        return;
                    }

                    Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }

                if (ti.IsAnonymous())
                {
                    Formatter = (IMessagePackFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod(typeof(T), true, true, false);
                    return;
                }

                TypeInfo formatterTypeInfo = DynamicObjectTypeBuilder.BuildType(DynamicAssembly.Value, typeof(T), true, true);
                if (formatterTypeInfo == null)
                {
                    return;
                }

                Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterTypeInfo.AsType());
            }
        }
    }

    /// <summary>
    /// ObjectResolver by dynamic code generation, no needs MessagePackObject attribute and serialized key as string, allow private member.
    /// </summary>
    public sealed class DynamicContractlessObjectResolverAllowPrivate : IFormatterResolver
    {
        public static readonly DynamicContractlessObjectResolverAllowPrivate Instance = new DynamicContractlessObjectResolverAllowPrivate();

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            internal static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                if (typeof(T) == typeof(object))
                {
                    return;
                }

                TypeInfo ti = typeof(T).GetTypeInfo();

                if (ti.IsInterface)
                {
                    return;
                }

                if (ti.IsNullable())
                {
                    ti = ti.GenericTypeArguments[0].GetTypeInfo();

                    var innerFormatter = DynamicContractlessObjectResolverAllowPrivate.Instance.GetFormatterDynamic(ti.AsType());
                    if (innerFormatter == null)
                    {
                        return;
                    }

                    Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }

                if (ti.IsAnonymous())
                {
                    Formatter = (IMessagePackFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod(typeof(T), true, true, false);
                }
                else
                {
                    Formatter = (IMessagePackFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod(typeof(T), true, true, true);
                }
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class DynamicObjectTypeBuilder
    {
#if !UNITY_2018_3_OR_NEWER
        private static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled);
#else
        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+");
#endif

        private static int nameSequence = 0;

        private static HashSet<Type> ignoreTypes = new HashSet<Type>
        {
            { typeof(object) },
            { typeof(short) },
            { typeof(int) },
            { typeof(long) },
            { typeof(ushort) },
            { typeof(uint) },
            { typeof(ulong) },
            { typeof(float) },
            { typeof(double) },
            { typeof(bool) },
            { typeof(byte) },
            { typeof(sbyte) },
            { typeof(decimal) },
            { typeof(char) },
            { typeof(string) },
            { typeof(System.Guid) },
            { typeof(System.TimeSpan) },
            { typeof(System.DateTime) },
            { typeof(System.DateTimeOffset) },
            { typeof(MessagePack.Nil) },
        };

        public static TypeInfo BuildType(DynamicAssembly assembly, Type type, bool forceStringKey, bool contractless)
        {
            if (ignoreTypes.Contains(type))
            {
                return null;
            }

            var serializationInfo = MessagePack.Internal.ObjectSerializationInfo.CreateOrNull(type, forceStringKey, contractless, false);
            if (serializationInfo == null)
            {
                return null;
            }

            if (!(type.IsPublic || type.IsNestedPublic))
            {
                throw new MessagePackSerializationException("Building dynamic formatter only allows public type. Type: " + type.FullName);
            }

            using (MonoProtection.EnterRefEmitLock())
            {
                Type formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
                TypeBuilder typeBuilder = assembly.DefineType("MessagePack.Formatters." + SubtractFullNameRegex.Replace(type.FullName, string.Empty).Replace(".", "_") + "Formatter" + Interlocked.Increment(ref nameSequence), TypeAttributes.Public | TypeAttributes.Sealed, null, new[] { formatterType });

                FieldBuilder stringByteKeysField = null;
                Dictionary<ObjectSerializationInfo.EmittableMember, FieldInfo> customFormatterLookup = null;

                // string key needs string->int mapper for deserialize switch statement
                if (serializationInfo.IsStringKey)
                {
                    ConstructorBuilder method = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                    stringByteKeysField = typeBuilder.DefineField("stringByteKeys", typeof(byte[][]), FieldAttributes.Private | FieldAttributes.InitOnly);

                    ILGenerator il = method.GetILGenerator();
                    BuildConstructor(type, serializationInfo, method, stringByteKeysField, il);
                    customFormatterLookup = BuildCustomFormatterField(typeBuilder, serializationInfo, il);
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    ConstructorBuilder method = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                    ILGenerator il = method.GetILGenerator();
                    il.EmitLoadThis();
                    il.Emit(OpCodes.Call, objectCtor);
                    customFormatterLookup = BuildCustomFormatterField(typeBuilder, serializationInfo, il);
                    il.Emit(OpCodes.Ret);
                }

                {
                    MethodBuilder method = typeBuilder.DefineMethod(
                        "Serialize",
                        MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                        returnType: null,
                        parameterTypes: new Type[] { typeof(MessagePackWriter).MakeByRefType(), type, typeof(MessagePackSerializerOptions) });
                    method.DefineParameter(1, ParameterAttributes.None, "writer");
                    method.DefineParameter(2, ParameterAttributes.None, "value");
                    method.DefineParameter(3, ParameterAttributes.None, "options");

                    ILGenerator il = method.GetILGenerator();
                    BuildSerialize(
                        type,
                        serializationInfo,
                        il,
                        () =>
                        {
                            il.EmitLoadThis();
                            il.EmitLdfld(stringByteKeysField);
                        },
                        (index, member) =>
                        {
                            FieldInfo fi;
                            if (!customFormatterLookup.TryGetValue(member, out fi))
                            {
                                return null;
                            }

                            return () =>
                            {
                                il.EmitLoadThis();
                                il.EmitLdfld(fi);
                            };
                        },
                        1);
                }

                {
                    MethodBuilder method = typeBuilder.DefineMethod(
                        "Deserialize",
                        MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                        type,
                        new Type[] { refMessagePackReader, typeof(MessagePackSerializerOptions) });
                    method.DefineParameter(1, ParameterAttributes.None, "reader");
                    method.DefineParameter(2, ParameterAttributes.None, "options");

                    ILGenerator il = method.GetILGenerator();
                    BuildDeserialize(
                        type,
                        serializationInfo,
                        il,
                        (index, member) =>
                        {
                            FieldInfo fi;
                            if (!customFormatterLookup.TryGetValue(member, out fi))
                            {
                                return null;
                            }

                            return () =>
                            {
                                il.EmitLoadThis();
                                il.EmitLdfld(fi);
                            };
                        },
                        1); // firstArgIndex:0 is this.
                }

                return typeBuilder.CreateTypeInfo();
            }
        }

        public static object BuildFormatterToDynamicMethod(Type type, bool forceStringKey, bool contractless, bool allowPrivate)
        {
            var serializationInfo = ObjectSerializationInfo.CreateOrNull(type, forceStringKey, contractless, allowPrivate);
            if (serializationInfo == null)
            {
                return null;
            }

            // internal delegate void AnonymousSerializeFunc<T>(byte[][] stringByteKeysField, object[] customFormatters, ref MessagePackWriter writer, T value, MessagePackSerializerOptions options);
            // internal delegate T AnonymousDeserializeFunc<T>(object[] customFormatters, ref MessagePackReader reader, MessagePackSerializerOptions options);
            var serialize = new DynamicMethod("Serialize", null, new[] { typeof(byte[][]), typeof(object[]), typeof(MessagePackWriter).MakeByRefType(), type, typeof(MessagePackSerializerOptions) }, type, true);
            DynamicMethod deserialize = null;

            List<byte[]> stringByteKeysField = new List<byte[]>();
            List<object> serializeCustomFormatters = new List<object>();
            List<object> deserializeCustomFormatters = new List<object>();

            if (serializationInfo.IsStringKey)
            {
                var i = 0;
                foreach (ObjectSerializationInfo.EmittableMember item in serializationInfo.Members.Where(x => x.IsReadable))
                {
                    stringByteKeysField.Add(Utilities.GetWriterBytes(item.StringKey, (ref MessagePackWriter writer, string arg) => writer.Write(arg)));
                    i++;
                }
            }

            foreach (ObjectSerializationInfo.EmittableMember item in serializationInfo.Members.Where(x => x.IsReadable))
            {
                MessagePackFormatterAttribute attr = item.GetMessagePackFormatterAttribute();
                if (attr != null)
                {
                    var formatter = Activator.CreateInstance(attr.FormatterType, attr.Arguments);
                    serializeCustomFormatters.Add(formatter);
                }
                else
                {
                    serializeCustomFormatters.Add(null);
                }
            }

            foreach (ObjectSerializationInfo.EmittableMember item in serializationInfo.Members)
            {
                // not only for writable because for use ctor.
                MessagePackFormatterAttribute attr = item.GetMessagePackFormatterAttribute();
                if (attr != null)
                {
                    var formatter = Activator.CreateInstance(attr.FormatterType, attr.Arguments);
                    deserializeCustomFormatters.Add(formatter);
                }
                else
                {
                    deserializeCustomFormatters.Add(null);
                }
            }

            {
                ILGenerator il = serialize.GetILGenerator();
                BuildSerialize(
                    type,
                    serializationInfo,
                    il,
                    () =>
                    {
                        il.EmitLdarg(0);
                    },
                    (index, member) =>
                    {
                        if (serializeCustomFormatters.Count == 0)
                        {
                            return null;
                        }

                        if (serializeCustomFormatters[index] == null)
                        {
                            return null;
                        }

                        return () =>
                        {
                            il.EmitLdarg(1); // read object[]
                            il.EmitLdc_I4(index);
                            il.Emit(OpCodes.Ldelem_Ref); // object
                            il.Emit(OpCodes.Castclass, serializeCustomFormatters[index].GetType());
                        };
                    },
                    2);  // 0, 1 is parameter.
            }

            if (serializationInfo.IsStruct || serializationInfo.BestmatchConstructor != null)
            {
                deserialize = new DynamicMethod("Deserialize", type, new[] { typeof(object[]), refMessagePackReader, typeof(MessagePackSerializerOptions) }, type, true);

                ILGenerator il = deserialize.GetILGenerator();
                BuildDeserialize(
                    type,
                    serializationInfo,
                    il,
                    (index, member) =>
                    {
                        if (deserializeCustomFormatters.Count == 0)
                        {
                            return null;
                        }

                        if (deserializeCustomFormatters[index] == null)
                        {
                            return null;
                        }

                        return () =>
                        {
                            il.EmitLdarg(0); // read object[]
                            il.EmitLdc_I4(index);
                            il.Emit(OpCodes.Ldelem_Ref); // object
                            il.Emit(OpCodes.Castclass, deserializeCustomFormatters[index].GetType());
                        };
                    },
                    1);
            }

            object serializeDelegate = serialize.CreateDelegate(typeof(AnonymousSerializeFunc<>).MakeGenericType(type));
            object deserializeDelegate = (deserialize == null)
                ? (object)null
                : (object)deserialize.CreateDelegate(typeof(AnonymousDeserializeFunc<>).MakeGenericType(type));
            var resultFormatter = Activator.CreateInstance(
                typeof(AnonymousSerializableFormatter<>).MakeGenericType(type),
                new[] { stringByteKeysField.ToArray(), serializeCustomFormatters.ToArray(), deserializeCustomFormatters.ToArray(), serializeDelegate, deserializeDelegate });
            return resultFormatter;
        }

        private static void BuildConstructor(Type type, ObjectSerializationInfo info, ConstructorInfo method, FieldBuilder stringByteKeysField, ILGenerator il)
        {
            il.EmitLoadThis();
            il.Emit(OpCodes.Call, objectCtor);

            var writeCount = info.Members.Count(x => x.IsReadable);
            il.EmitLoadThis();
            il.EmitLdc_I4(writeCount);
            il.Emit(OpCodes.Newarr, typeof(byte[]));

            var i = 0;
            foreach (ObjectSerializationInfo.EmittableMember item in info.Members.Where(x => x.IsReadable))
            {
                il.Emit(OpCodes.Dup);
                il.EmitLdc_I4(i);
                il.Emit(OpCodes.Ldstr, item.StringKey);
                il.EmitCall(CodeGenHelpersTypeInfo.GetEncodedStringBytes);
                il.Emit(OpCodes.Stelem_Ref);
                i++;
            }

            il.Emit(OpCodes.Stfld, stringByteKeysField);
        }

        private static Dictionary<ObjectSerializationInfo.EmittableMember, FieldInfo> BuildCustomFormatterField(TypeBuilder builder, ObjectSerializationInfo info, ILGenerator il)
        {
            Dictionary<ObjectSerializationInfo.EmittableMember, FieldInfo> dict = new Dictionary<ObjectSerializationInfo.EmittableMember, FieldInfo>();
            foreach (ObjectSerializationInfo.EmittableMember item in info.Members.Where(x => x.IsReadable || x.IsWritable))
            {
                MessagePackFormatterAttribute attr = item.GetMessagePackFormatterAttribute();
                if (attr != null)
                {
                    FieldBuilder f = builder.DefineField(item.Name + "_formatter", attr.FormatterType, FieldAttributes.Private | FieldAttributes.InitOnly);

                    var bindingFlags = (int)(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    LocalBuilder attrVar = il.DeclareLocal(typeof(MessagePackFormatterAttribute));

                    il.Emit(OpCodes.Ldtoken, info.Type);
                    il.EmitCall(EmitInfo.GetTypeFromHandle);
                    il.Emit(OpCodes.Ldstr, item.Name);
                    il.EmitLdc_I4(bindingFlags);
                    if (item.IsProperty)
                    {
                        il.EmitCall(EmitInfo.TypeGetProperty);
                    }
                    else
                    {
                        il.EmitCall(EmitInfo.TypeGetField);
                    }

                    il.EmitTrue();
                    il.EmitCall(EmitInfo.GetCustomAttributeMessagePackFormatterAttribute);
                    il.EmitStloc(attrVar);

                    il.EmitLoadThis();

                    il.EmitLdloc(attrVar);
                    il.EmitCall(EmitInfo.MessagePackFormatterAttr.FormatterType);
                    il.EmitLdloc(attrVar);
                    il.EmitCall(EmitInfo.MessagePackFormatterAttr.Arguments);
                    il.EmitCall(EmitInfo.ActivatorCreateInstance);

                    il.Emit(OpCodes.Castclass, attr.FormatterType);
                    il.Emit(OpCodes.Stfld, f);

                    dict.Add(item, f);
                }
            }

            return dict;
        }

        // void Serialize(ref [arg:1]MessagePackWriter writer, [arg:2]T value, [arg:3]MessagePackSerializerOptions options);
        private static void BuildSerialize(Type type, ObjectSerializationInfo info, ILGenerator il, Action emitStringByteKeys, Func<int, ObjectSerializationInfo.EmittableMember, Action> tryEmitLoadCustomFormatter, int firstArgIndex)
        {
            var argWriter = new ArgumentField(il, firstArgIndex);
            var argValue = new ArgumentField(il, firstArgIndex + 1, type);
            var argOptions = new ArgumentField(il, firstArgIndex + 2);

            // if(value == null) return WriteNil
            if (type.GetTypeInfo().IsClass)
            {
                Label elseBody = il.DefineLabel();

                argValue.EmitLoad();
                il.Emit(OpCodes.Brtrue_S, elseBody);
                argWriter.EmitLoad();
                il.EmitCall(MessagePackWriterTypeInfo.WriteNil);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(elseBody);
            }

            // IMessagePackSerializationCallbackReceiver.OnBeforeSerialize()
            if (type.GetTypeInfo().ImplementedInterfaces.Any(x => x == typeof(IMessagePackSerializationCallbackReceiver)))
            {
                // call directly
                MethodInfo[] runtimeMethods = type.GetRuntimeMethods().Where(x => x.Name == "OnBeforeSerialize").ToArray();
                if (runtimeMethods.Length == 1)
                {
                    argValue.EmitLoad();
                    il.Emit(OpCodes.Call, runtimeMethods[0]); // don't use EmitCall helper(must use 'Call')
                }
                else
                {
                    argValue.EmitLdarg(); // force ldarg
                    il.EmitBoxOrDoNothing(type);
                    il.EmitCall(onBeforeSerialize);
                }
            }

            // IFormatterResolver resolver = options.Resolver;
            LocalBuilder localResolver = il.DeclareLocal(typeof(IFormatterResolver));
            argOptions.EmitLoad();
            il.EmitCall(getResolverFromOptions);
            il.EmitStloc(localResolver);

            if (info.IsIntKey)
            {
                // use Array
                var maxKey = info.Members.Where(x => x.IsReadable).Select(x => x.IntKey).DefaultIfEmpty(-1).Max();
                var intKeyMap = info.Members.Where(x => x.IsReadable).ToDictionary(x => x.IntKey);

                var len = maxKey + 1;
                argWriter.EmitLoad();
                il.EmitLdc_I4(len);
                il.EmitCall(MessagePackWriterTypeInfo.WriteArrayHeader);

                var index = 0;
                for (int i = 0; i <= maxKey; i++)
                {
                    ObjectSerializationInfo.EmittableMember member;
                    if (intKeyMap.TryGetValue(i, out member))
                    {
                        EmitSerializeValue(il, type.GetTypeInfo(), member, index++, tryEmitLoadCustomFormatter, argWriter, argValue, argOptions, localResolver);
                    }
                    else
                    {
                        // Write Nil as Blanc
                        argWriter.EmitLoad();
                        il.EmitCall(MessagePackWriterTypeInfo.WriteNil);
                    }
                }
            }
            else
            {
                // use Map
                var writeCount = info.Members.Count(x => x.IsReadable);

                argWriter.EmitLoad();
                il.EmitLdc_I4(writeCount);
                ////if (writeCount <= MessagePackRange.MaxFixMapCount)
                ////{
                ////    il.EmitCall(MessagePackWriterTypeInfo.WriteFixedMapHeaderUnsafe);
                ////}
                ////else
                {
                    il.EmitCall(MessagePackWriterTypeInfo.WriteMapHeader);
                }

                var index = 0;
                foreach (ObjectSerializationInfo.EmittableMember item in info.Members.Where(x => x.IsReadable))
                {
                    argWriter.EmitLoad();
                    emitStringByteKeys();
                    il.EmitLdc_I4(index);
                    il.Emit(OpCodes.Ldelem_Ref);
                    il.Emit(OpCodes.Call, ReadOnlySpanFromByteArray); // convert byte[] to ReadOnlySpan<byte>

                    // Optimize, WriteRaw(Unity, large) or UnsafeMemory32/64.WriteRawX
#if !UNITY_2018_3_OR_NEWER
                    var valueLen = CodeGenHelpers.GetEncodedStringBytes(item.StringKey).Length;
                    if (valueLen <= MessagePackRange.MaxFixStringLength)
                    {
                        if (UnsafeMemory.Is32Bit)
                        {
                            il.EmitCall(typeof(UnsafeMemory32).GetRuntimeMethod("WriteRaw" + valueLen, new[] { typeof(MessagePackWriter).MakeByRefType(), typeof(ReadOnlySpan<byte>) }));
                        }
                        else
                        {
                            il.EmitCall(typeof(UnsafeMemory64).GetRuntimeMethod("WriteRaw" + valueLen, new[] { typeof(MessagePackWriter).MakeByRefType(), typeof(ReadOnlySpan<byte>) }));
                        }
                    }
                    else
#endif
                    {
                        il.EmitCall(MessagePackWriterTypeInfo.WriteRaw);
                    }

                    EmitSerializeValue(il, type.GetTypeInfo(), item, index, tryEmitLoadCustomFormatter, argWriter, argValue, argOptions, localResolver);
                    index++;
                }
            }

            il.Emit(OpCodes.Ret);
        }

        private static void EmitSerializeValue(ILGenerator il, TypeInfo type, ObjectSerializationInfo.EmittableMember member, int index, Func<int, ObjectSerializationInfo.EmittableMember, Action> tryEmitLoadCustomFormatter, ArgumentField argWriter, ArgumentField argValue, ArgumentField argOptions, LocalBuilder localResolver)
        {
            Label endLabel = il.DefineLabel();
            Type t = member.Type;
            Action emitter = tryEmitLoadCustomFormatter(index, member);
            if (emitter != null)
            {
                emitter();
                argWriter.EmitLoad();
                argValue.EmitLoad();
                member.EmitLoadValue(il);
                argOptions.EmitLoad();
                il.EmitCall(getSerialize(t));
            }
            else if (IsOptimizeTargetType(t))
            {
                if (!t.GetTypeInfo().IsValueType)
                {
                    // As a nullable type (e.g. byte[] and string) we need to call WriteNil for null values.
                    Label writeNonNilValueLabel = il.DefineLabel();
                    LocalBuilder memberValue = il.DeclareLocal(t);
                    argValue.EmitLoad();
                    member.EmitLoadValue(il);
                    il.Emit(OpCodes.Dup);
                    il.EmitStloc(memberValue);
                    il.Emit(OpCodes.Brtrue, writeNonNilValueLabel);
                    argWriter.EmitLoad();
                    il.EmitCall(MessagePackWriterTypeInfo.WriteNil);
                    il.Emit(OpCodes.Br, endLabel);

                    il.MarkLabel(writeNonNilValueLabel);
                    argWriter.EmitLoad();
                    il.EmitLdloc(memberValue);
                }
                else
                {
                    argWriter.EmitLoad();
                    argValue.EmitLoad();
                    member.EmitLoadValue(il);
                }

                if (t == typeof(byte[]))
                {
                    il.EmitCall(ReadOnlySpanFromByteArray);
                    il.EmitCall(MessagePackWriterTypeInfo.WriteBytes);
                }
                else
                {
                    il.EmitCall(typeof(MessagePackWriter).GetRuntimeMethod("Write", new Type[] { t }));
                }
            }
            else
            {
                il.EmitLdloc(localResolver);
                il.Emit(OpCodes.Call, getFormatterWithVerify.MakeGenericMethod(t));

                argWriter.EmitLoad();
                argValue.EmitLoad();
                member.EmitLoadValue(il);
                argOptions.EmitLoad();
                il.EmitCall(getSerialize(t));
            }

            il.MarkLabel(endLabel);
        }

        // T Deserialize([arg:1]ref MessagePackReader reader, [arg:2]MessagePackSerializerOptions options);
        private static void BuildDeserialize(Type type, ObjectSerializationInfo info, ILGenerator il, Func<int, ObjectSerializationInfo.EmittableMember, Action> tryEmitLoadCustomFormatter, int firstArgIndex)
        {
            var reader = new ArgumentField(il, firstArgIndex, @ref: true);
            var argOptions = new ArgumentField(il, firstArgIndex + 1);

            // if(reader.TryReadNil()) { return null; }
            Label falseLabel = il.DefineLabel();
            reader.EmitLdarg();
            il.EmitCall(MessagePackReaderTypeInfo.TryReadNil);
            il.Emit(OpCodes.Brfalse_S, falseLabel);
            if (type.GetTypeInfo().IsClass)
            {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                il.Emit(OpCodes.Ldstr, "typecode is null, struct not supported");
                il.Emit(OpCodes.Newobj, messagePackSerializationExceptionMessageOnlyConstructor);
                il.Emit(OpCodes.Throw);
            }

            il.MarkLabel(falseLabel);

            // options.Security.DepthStep(ref reader);
            argOptions.EmitLoad();
            il.EmitCall(getSecurityFromOptions);
            reader.EmitLdarg();
            il.EmitCall(securityDepthStep);

            // var length = ReadMapHeader(ref byteSequence);
            LocalBuilder length = il.DeclareLocal(typeof(int)); // [loc:1]
            reader.EmitLdarg();

            if (info.IsIntKey)
            {
                il.EmitCall(MessagePackReaderTypeInfo.ReadArrayHeader);
            }
            else
            {
                il.EmitCall(MessagePackReaderTypeInfo.ReadMapHeader);
            }

            il.EmitStloc(length);

            // make local fields
            Label? gotoDefault = null;
            DeserializeInfo[] infoList;
            if (info.IsIntKey)
            {
                var maxKey = info.Members.Select(x => x.IntKey).DefaultIfEmpty(-1).Max();
                var len = maxKey + 1;
                var intKeyMap = info.Members.ToDictionary(x => x.IntKey);

                infoList = Enumerable.Range(0, len)
                    .Select(x =>
                    {
                        ObjectSerializationInfo.EmittableMember member;
                        if (intKeyMap.TryGetValue(x, out member))
                        {
                            return new DeserializeInfo
                            {
                                MemberInfo = member,
                                LocalField = il.DeclareLocal(member.Type),
                                SwitchLabel = il.DefineLabel(),
                            };
                        }
                        else
                        {
                            // return null MemberInfo, should filter null
                            if (gotoDefault == null)
                            {
                                gotoDefault = il.DefineLabel();
                            }

                            return new DeserializeInfo
                            {
                                MemberInfo = null,
                                LocalField = null,
                                SwitchLabel = gotoDefault.Value,
                            };
                        }
                    })
                    .ToArray();
            }
            else
            {
                infoList = info.Members
                    .Select(item => new DeserializeInfo
                    {
                        MemberInfo = item,
                        LocalField = il.DeclareLocal(item.Type),
                        //// SwitchLabel = il.DefineLabel()
                    })
                    .ToArray();
            }

            // IFormatterResolver resolver = options.Resolver;
            LocalBuilder localResolver = il.DeclareLocal(typeof(IFormatterResolver));
            argOptions.EmitLoad();
            il.EmitCall(getResolverFromOptions);
            il.EmitStloc(localResolver);

            // Read Loop(for var i = 0; i < length; i++)
            if (info.IsStringKey)
            {
                var automata = new AutomataDictionary();
                for (int i = 0; i < info.Members.Length; i++)
                {
                    automata.Add(info.Members[i].StringKey, i);
                }

                LocalBuilder buffer = il.DeclareLocal(typeof(ReadOnlySpan<byte>));
                LocalBuilder longKey = il.DeclareLocal(typeof(ulong));

                // for (int i = 0; i < len; i++)
                il.EmitIncrementFor(length, forILocal =>
                {
                    Label readNext = il.DefineLabel();
                    Label loopEnd = il.DefineLabel();

                    reader.EmitLdarg();
                    il.EmitCall(ReadStringSpan);
                    il.EmitStloc(buffer);

                    // gen automata name lookup
                    automata.EmitMatch(
                        il,
                        buffer,
                        longKey,
                        x =>
                        {
                            var i = x.Value;
                            if (infoList[i].MemberInfo != null)
                            {
                                EmitDeserializeValue(il, infoList[i], i, tryEmitLoadCustomFormatter, reader, argOptions, localResolver);
                                il.Emit(OpCodes.Br, loopEnd);
                            }
                            else
                            {
                                il.Emit(OpCodes.Br, readNext);
                            }
                        },
                        () =>
                        {
                            il.Emit(OpCodes.Br, readNext);
                        });

                    il.MarkLabel(readNext);
                    reader.EmitLdarg();
                    il.EmitCall(MessagePackReaderTypeInfo.Skip);

                    il.MarkLabel(loopEnd);
                });
            }
            else
            {
                LocalBuilder key = il.DeclareLocal(typeof(int));
                Label switchDefault = il.DefineLabel();

                il.EmitIncrementFor(length, forILocal =>
                {
                    Label loopEnd = il.DefineLabel();

                    il.EmitLdloc(forILocal);
                    il.EmitStloc(key);

                    // switch... local = Deserialize
                    il.EmitLdloc(key);

                    il.Emit(OpCodes.Switch, infoList.Select(x => x.SwitchLabel).ToArray());

                    il.MarkLabel(switchDefault);

                    // default, only read. reader.ReadNextBlock();
                    reader.EmitLdarg();
                    il.EmitCall(MessagePackReaderTypeInfo.Skip);
                    il.Emit(OpCodes.Br, loopEnd);

                    if (gotoDefault != null)
                    {
                        il.MarkLabel(gotoDefault.Value);
                        il.Emit(OpCodes.Br, switchDefault);
                    }

                    var i = 0;
                    foreach (DeserializeInfo item in infoList)
                    {
                        if (item.MemberInfo != null)
                        {
                            il.MarkLabel(item.SwitchLabel);
                            EmitDeserializeValue(il, item, i++, tryEmitLoadCustomFormatter, reader, argOptions, localResolver);
                            il.Emit(OpCodes.Br, loopEnd);
                        }
                    }

                    il.MarkLabel(loopEnd);
                });
            }

            // create result object
            LocalBuilder structLocal = EmitNewObject(il, type, info, infoList);

            // IMessagePackSerializationCallbackReceiver.OnAfterDeserialize()
            if (type.GetTypeInfo().ImplementedInterfaces.Any(x => x == typeof(IMessagePackSerializationCallbackReceiver)))
            {
                // call directly
                MethodInfo[] runtimeMethods = type.GetRuntimeMethods().Where(x => x.Name == "OnAfterDeserialize").ToArray();
                if (runtimeMethods.Length == 1)
                {
                    if (info.IsClass)
                    {
                        il.Emit(OpCodes.Dup);
                    }
                    else
                    {
                        il.EmitLdloca(structLocal);
                    }

                    il.Emit(OpCodes.Call, runtimeMethods[0]); // don't use EmitCall helper(must use 'Call')
                }
                else
                {
                    if (info.IsStruct)
                    {
                        il.EmitLdloc(structLocal);
                        il.Emit(OpCodes.Box, type);
                    }
                    else
                    {
                        il.Emit(OpCodes.Dup);
                    }

                    il.EmitCall(onAfterDeserialize);
                }
            }

            // reader.Depth--;
            reader.EmitLdarg();
            il.Emit(OpCodes.Dup);
            il.EmitCall(readerDepthGet);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Sub_Ovf);
            il.EmitCall(readerDepthSet);

            if (info.IsStruct)
            {
                il.Emit(OpCodes.Ldloc, structLocal);
            }

            il.Emit(OpCodes.Ret);
        }

        private static void EmitDeserializeValue(ILGenerator il, DeserializeInfo info, int index, Func<int, ObjectSerializationInfo.EmittableMember, Action> tryEmitLoadCustomFormatter, ArgumentField argReader, ArgumentField argOptions, LocalBuilder localResolver)
        {
            Label storeLabel = il.DefineLabel();
            ObjectSerializationInfo.EmittableMember member = info.MemberInfo;
            Type t = member.Type;
            Action emitter = tryEmitLoadCustomFormatter(index, member);
            if (emitter != null)
            {
                emitter();
                argReader.EmitLdarg();
                argOptions.EmitLoad();
                il.EmitCall(getDeserialize(t));
            }
            else if (IsOptimizeTargetType(t))
            {
                if (!t.GetTypeInfo().IsValueType)
                {
                    // As a nullable type (e.g. byte[] and string) we need to first call TryReadNil
                    // if (reader.TryReadNil())
                    Label readNonNilValueLabel = il.DefineLabel();
                    argReader.EmitLdarg();
                    il.EmitCall(MessagePackReaderTypeInfo.TryReadNil);
                    il.Emit(OpCodes.Brfalse_S, readNonNilValueLabel);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Br, storeLabel);

                    il.MarkLabel(readNonNilValueLabel);
                }

                argReader.EmitLdarg();
                if (t == typeof(byte[]))
                {
                    LocalBuilder local = il.DeclareLocal(typeof(ReadOnlySequence<byte>?));
                    il.EmitCall(MessagePackReaderTypeInfo.ReadBytes);
                    il.EmitStloc(local);
                    il.EmitLdloca(local);
                    il.EmitCall(ArrayFromNullableReadOnlySequence);
                }
                else
                {
                    il.EmitCall(MessagePackReaderTypeInfo.TypeInfo.GetDeclaredMethods("Read" + t.Name).First(x => x.GetParameters().Length == 0));
                }
            }
            else
            {
                il.EmitLdloc(localResolver);
                il.EmitCall(getFormatterWithVerify.MakeGenericMethod(t));
                argReader.EmitLdarg();
                argOptions.EmitLoad();
                il.EmitCall(getDeserialize(t));
            }

            il.MarkLabel(storeLabel);
            il.EmitStloc(info.LocalField);
        }

        private static LocalBuilder EmitNewObject(ILGenerator il, Type type, ObjectSerializationInfo info, DeserializeInfo[] members)
        {
            if (info.IsClass)
            {
                EmitNewObjectConstructorArguments(il, info, members);

                il.Emit(OpCodes.Newobj, info.BestmatchConstructor);

                foreach (DeserializeInfo item in members.Where(x => x.MemberInfo != null && x.MemberInfo.IsWritable))
                {
                    il.Emit(OpCodes.Dup);
                    il.EmitLdloc(item.LocalField);
                    item.MemberInfo.EmitStoreValue(il);
                }

                return null;
            }
            else
            {
                LocalBuilder result = il.DeclareLocal(type);
                if (info.BestmatchConstructor == null)
                {
                    il.Emit(OpCodes.Ldloca, result);
                    il.Emit(OpCodes.Initobj, type);
                }
                else
                {
                    EmitNewObjectConstructorArguments(il, info, members);

                    il.Emit(OpCodes.Newobj, info.BestmatchConstructor);
                    il.Emit(OpCodes.Stloc, result);
                }

                foreach (DeserializeInfo item in members.Where(x => x.MemberInfo != null && x.MemberInfo.IsWritable))
                {
                    il.EmitLdloca(result);
                    il.EmitLdloc(item.LocalField);
                    item.MemberInfo.EmitStoreValue(il);
                }

                return result; // struct returns local result field
            }
        }

        private static void EmitNewObjectConstructorArguments(ILGenerator il, ObjectSerializationInfo info, DeserializeInfo[] members)
        {
            foreach (ObjectSerializationInfo.EmittableMemberAndConstructorParameter item in info.ConstructorParameters)
            {
                DeserializeInfo local = members.First(x => x.MemberInfo == item.MemberInfo);
                il.EmitLdloc(local.LocalField);

                if (!item.ConstructorParameter.ParameterType.IsValueType && local.MemberInfo.IsValueType)
                {
                    // When a constructor argument of type object is being provided by a serialized member value that is a value type
                    // then that value must be boxed in order for the generated code to be valid (see issue #987). This may occur because
                    // the only requirement when determining whether a member value may be used to populate a constructor argument in an
                    // IsAssignableFrom check and typeof(object) IsAssignableFrom typeof(int), for example.
                    il.Emit(OpCodes.Box, local.MemberInfo.Type);
                }
            }
        }

        /// <devremarks>
        /// Keep this list in sync with ShouldUseFormatterResolverHelper.PrimitiveTypes.
        /// </devremarks>
        private static bool IsOptimizeTargetType(Type type)
        {
            return type == typeof(Int16)
                || type == typeof(Int32)
                || type == typeof(Int64)
                || type == typeof(UInt16)
                || type == typeof(UInt32)
                || type == typeof(UInt64)
                || type == typeof(Single)
                || type == typeof(Double)
                || type == typeof(bool)
                || type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(char)
                || type == typeof(byte[])

            // Do not include types that resolvers are allowed to modify.
            ////|| type == typeof(DateTime) // OldSpec has no support, so for that and perf reasons a .NET native DateTime resolver exists.
            ////|| type == typeof(string) // https://github.com/Cysharp/MasterMemory provides custom formatter for string interning.
            ;
        }

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

        // EmitInfos...
        private static readonly Type refMessagePackReader = typeof(MessagePackReader).MakeByRefType();

        private static readonly MethodInfo ReadOnlySpanFromByteArray = typeof(ReadOnlySpan<byte>).GetRuntimeMethod("op_Implicit", new[] { typeof(byte[]) });
        private static readonly MethodInfo ReadStringSpan = typeof(CodeGenHelpers).GetRuntimeMethod(nameof(CodeGenHelpers.ReadStringSpan), new[] { typeof(MessagePackReader).MakeByRefType() });
        private static readonly MethodInfo ArrayFromNullableReadOnlySequence = typeof(CodeGenHelpers).GetRuntimeMethod(nameof(CodeGenHelpers.GetArrayFromNullableSequence), new[] { typeof(ReadOnlySequence<byte>?).MakeByRefType() });

        private static readonly MethodInfo getFormatterWithVerify = typeof(FormatterResolverExtensions).GetRuntimeMethods().First(x => x.Name == nameof(FormatterResolverExtensions.GetFormatterWithVerify));
        private static readonly MethodInfo getResolverFromOptions = typeof(MessagePackSerializerOptions).GetRuntimeProperty(nameof(MessagePackSerializerOptions.Resolver)).GetMethod;
        private static readonly MethodInfo getSecurityFromOptions = typeof(MessagePackSerializerOptions).GetRuntimeProperty(nameof(MessagePackSerializerOptions.Security)).GetMethod;
        private static readonly MethodInfo securityDepthStep = typeof(MessagePackSecurity).GetRuntimeMethod(nameof(MessagePackSecurity.DepthStep), new[] { typeof(MessagePackReader).MakeByRefType() });
        private static readonly MethodInfo readerDepthGet = typeof(MessagePackReader).GetRuntimeProperty(nameof(MessagePackReader.Depth)).GetMethod;
        private static readonly MethodInfo readerDepthSet = typeof(MessagePackReader).GetRuntimeProperty(nameof(MessagePackReader.Depth)).SetMethod;
        private static readonly Func<Type, MethodInfo> getSerialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod(nameof(IMessagePackFormatter<int>.Serialize), new[] { typeof(MessagePackWriter).MakeByRefType(), t, typeof(MessagePackSerializerOptions) });
        private static readonly Func<Type, MethodInfo> getDeserialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod(nameof(IMessagePackFormatter<int>.Deserialize), new[] { refMessagePackReader, typeof(MessagePackSerializerOptions) });
        //// static readonly ConstructorInfo dictionaryConstructor = typeof(ByteArrayStringHashTable).GetTypeInfo().DeclaredConstructors.First(x => { var p = x.GetParameters(); return p.Length == 1 && p[0].ParameterType == typeof(int); });
        //// static readonly MethodInfo dictionaryAdd = typeof(ByteArrayStringHashTable).GetRuntimeMethod("Add", new[] { typeof(string), typeof(int) });
        //// static readonly MethodInfo dictionaryTryGetValue = typeof(ByteArrayStringHashTable).GetRuntimeMethod("TryGetValue", new[] { typeof(ArraySegment<byte>), refInt });
        private static readonly ConstructorInfo messagePackSerializationExceptionMessageOnlyConstructor = typeof(MessagePackSerializationException).GetTypeInfo().DeclaredConstructors.First(x =>
        {
            ParameterInfo[] p = x.GetParameters();
            return p.Length == 1 && p[0].ParameterType == typeof(string);
        });

        private static readonly MethodInfo onBeforeSerialize = typeof(IMessagePackSerializationCallbackReceiver).GetRuntimeMethod(nameof(IMessagePackSerializationCallbackReceiver.OnBeforeSerialize), Type.EmptyTypes);
        private static readonly MethodInfo onAfterDeserialize = typeof(IMessagePackSerializationCallbackReceiver).GetRuntimeMethod(nameof(IMessagePackSerializationCallbackReceiver.OnAfterDeserialize), Type.EmptyTypes);

        private static readonly ConstructorInfo objectCtor = typeof(object).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 0);

#pragma warning restore SA1311 // Static readonly fields should begin with upper-case letter

        /// <summary>
        /// Helps match parameters when searching a method when the parameter is a generic type.
        /// </summary>
        private static bool Matches(MethodInfo m, int parameterIndex, Type desiredType)
        {
            ParameterInfo[] parameters = m.GetParameters();
            return parameters.Length > parameterIndex
                ////&& parameters[0].ParameterType.IsGenericType // returns false for some bizarre reason
                && parameters[parameterIndex].ParameterType.Name == desiredType.Name
                && parameters[parameterIndex].ParameterType.Namespace == desiredType.Namespace;
        }

        internal static class MessagePackWriterTypeInfo
        {
            internal static readonly TypeInfo TypeInfo = typeof(MessagePackWriter).GetTypeInfo();

            internal static readonly MethodInfo WriteMapHeader = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.WriteMapHeader), new[] { typeof(int) });
            internal static readonly MethodInfo WriteArrayHeader = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.WriteArrayHeader), new[] { typeof(int) });
            internal static readonly MethodInfo WriteBytes = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.Write), new[] { typeof(ReadOnlySpan<byte>) });
            internal static readonly MethodInfo WriteNil = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.WriteNil), Type.EmptyTypes);
            internal static readonly MethodInfo WriteRaw = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.WriteRaw), new[] { typeof(ReadOnlySpan<byte>) });
        }

        internal static class MessagePackReaderTypeInfo
        {
            internal static readonly TypeInfo TypeInfo = typeof(MessagePackReader).GetTypeInfo();

            internal static readonly MethodInfo ReadArrayHeader = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadArrayHeader), Type.EmptyTypes);
            internal static readonly MethodInfo ReadMapHeader = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadMapHeader), Type.EmptyTypes);
            internal static readonly MethodInfo ReadBytes = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadBytes), Type.EmptyTypes);
            internal static readonly MethodInfo TryReadNil = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.TryReadNil), Type.EmptyTypes);
            internal static readonly MethodInfo Skip = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.Skip), Type.EmptyTypes);
        }

        internal static class CodeGenHelpersTypeInfo
        {
            public static readonly MethodInfo GetEncodedStringBytes = typeof(CodeGenHelpers).GetRuntimeMethod(nameof(CodeGenHelpers.GetEncodedStringBytes), new[] { typeof(string) });
        }

        internal static class EmitInfo
        {
            public static readonly MethodInfo GetTypeFromHandle = ExpressionUtility.GetMethodInfo(() => Type.GetTypeFromHandle(default(RuntimeTypeHandle)));
            public static readonly MethodInfo TypeGetProperty = ExpressionUtility.GetMethodInfo((Type t) => t.GetTypeInfo().GetProperty(default(string), default(BindingFlags)));
            public static readonly MethodInfo TypeGetField = ExpressionUtility.GetMethodInfo((Type t) => t.GetTypeInfo().GetField(default(string), default(BindingFlags)));
            public static readonly MethodInfo GetCustomAttributeMessagePackFormatterAttribute = ExpressionUtility.GetMethodInfo(() => CustomAttributeExtensions.GetCustomAttribute<MessagePackFormatterAttribute>(default(MemberInfo), default(bool)));
            public static readonly MethodInfo ActivatorCreateInstance = ExpressionUtility.GetMethodInfo(() => Activator.CreateInstance(default(Type), default(object[])));

            internal static class MessagePackFormatterAttr
            {
                internal static readonly MethodInfo FormatterType = ExpressionUtility.GetPropertyInfo((MessagePackFormatterAttribute attr) => attr.FormatterType).GetGetMethod();
                internal static readonly MethodInfo Arguments = ExpressionUtility.GetPropertyInfo((MessagePackFormatterAttribute attr) => attr.Arguments).GetGetMethod();
            }
        }

        private class DeserializeInfo
        {
            public ObjectSerializationInfo.EmittableMember MemberInfo { get; set; }

            public LocalBuilder LocalField { get; set; }

            public Label SwitchLabel { get; set; }
        }
    }

    internal delegate void AnonymousSerializeFunc<T>(byte[][] stringByteKeysField, object[] customFormatters, ref MessagePackWriter writer, T value, MessagePackSerializerOptions options);

    internal delegate T AnonymousDeserializeFunc<T>(object[] customFormatters, ref MessagePackReader reader, MessagePackSerializerOptions options);

    internal class AnonymousSerializableFormatter<T> : IMessagePackFormatter<T>
    {
        private readonly byte[][] stringByteKeysField;
        private readonly object[] serializeCustomFormatters;
        private readonly object[] deserializeCustomFormatters;
        private readonly AnonymousSerializeFunc<T> serialize;
        private readonly AnonymousDeserializeFunc<T> deserialize;

        public AnonymousSerializableFormatter(byte[][] stringByteKeysField, object[] serializeCustomFormatters, object[] deserializeCustomFormatters, AnonymousSerializeFunc<T> serialize, AnonymousDeserializeFunc<T> deserialize)
        {
            this.stringByteKeysField = stringByteKeysField;
            this.serializeCustomFormatters = serializeCustomFormatters;
            this.deserializeCustomFormatters = deserializeCustomFormatters;
            this.serialize = serialize;
            this.deserialize = deserialize;
        }

        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            if (this.serialize == null)
            {
                throw new MessagePackSerializationException(this.GetType().Name + " does not support Serialize.");
            }

            this.serialize(this.stringByteKeysField, this.serializeCustomFormatters, ref writer, value, options);
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (this.deserialize == null)
            {
                throw new MessagePackSerializationException(this.GetType().Name + " does not support Deserialize.");
            }

            return this.deserialize(this.deserializeCustomFormatters, ref reader, options);
        }
    }

    internal class ObjectSerializationInfo
    {
        public Type Type { get; set; }

        public bool IsIntKey { get; set; }

        public bool IsStringKey
        {
            get { return !this.IsIntKey; }
        }

        public bool IsClass { get; set; }

        public bool IsStruct
        {
            get { return !this.IsClass; }
        }

        public ConstructorInfo BestmatchConstructor { get; set; }

        public EmittableMemberAndConstructorParameter[] ConstructorParameters { get; set; }

        public EmittableMember[] Members { get; set; }

        private ObjectSerializationInfo()
        {
        }

        public static ObjectSerializationInfo CreateOrNull(Type type, bool forceStringKey, bool contractless, bool allowPrivate)
        {
            TypeInfo ti = type.GetTypeInfo();
            var isClass = ti.IsClass || ti.IsInterface || ti.IsAbstract;
            var isStruct = ti.IsValueType;

            MessagePackObjectAttribute contractAttr = ti.GetCustomAttributes<MessagePackObjectAttribute>().FirstOrDefault();
            DataContractAttribute dataContractAttr = ti.GetCustomAttribute<DataContractAttribute>();
            if (contractAttr == null && dataContractAttr == null && !forceStringKey && !contractless)
            {
                return null;
            }

            var isIntKey = true;
            var intMembers = new Dictionary<int, EmittableMember>();
            var stringMembers = new Dictionary<string, EmittableMember>();

            if (forceStringKey || contractless || (contractAttr != null && contractAttr.KeyAsPropertyName))
            {
                // All public members are serialize target except [Ignore] member.
                isIntKey = !(forceStringKey || (contractAttr != null && contractAttr.KeyAsPropertyName));

                var hiddenIntKey = 0;

                // Group the properties and fields by name to qualify members of the same name
                // (declared with the 'new' keyword) with the declaring type.
                IEnumerable<IGrouping<string, MemberInfo>> membersByName = type.GetRuntimeProperties()
                    .Concat(type.GetRuntimeFields().Cast<MemberInfo>())
                    .OrderBy(m => m.DeclaringType, OrderBaseTypesBeforeDerivedTypes.Instance)
                    .GroupBy(m => m.Name);
                foreach (var memberGroup in membersByName)
                {
                    bool firstMemberByName = true;
                    foreach (MemberInfo item in memberGroup)
                    {
                        if (item.GetCustomAttribute<IgnoreMemberAttribute>(true) != null)
                        {
                            continue;
                        }

                        if (item.GetCustomAttribute<IgnoreDataMemberAttribute>(true) != null)
                        {
                            continue;
                        }

                        EmittableMember member;
                        if (item is PropertyInfo property)
                        {
                            if (property.IsIndexer())
                            {
                                continue;
                            }

                            MethodInfo getMethod = property.GetGetMethod(true);
                            MethodInfo setMethod = property.GetSetMethod(true);

                            member = new EmittableMember
                            {
                                PropertyInfo = property,
                                IsReadable = (getMethod != null) && (allowPrivate || getMethod.IsPublic) && !getMethod.IsStatic,
                                IsWritable = (setMethod != null) && (allowPrivate || setMethod.IsPublic) && !setMethod.IsStatic,
                                StringKey = firstMemberByName ? item.Name : $"{item.DeclaringType.FullName}.{item.Name}",
                            };
                        }
                        else if (item is FieldInfo field)
                        {
                            if (item.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>(true) != null)
                            {
                                continue;
                            }

                            if (field.IsStatic)
                            {
                                continue;
                            }

                            member = new EmittableMember
                            {
                                FieldInfo = field,
                                IsReadable = allowPrivate || field.IsPublic,
                                IsWritable = allowPrivate || (field.IsPublic && !field.IsInitOnly),
                                StringKey = firstMemberByName ? item.Name : $"{item.DeclaringType.FullName}.{item.Name}",
                            };
                        }
                        else
                        {
                            throw new MessagePackSerializationException("unexpected member type");
                        }

                        if (!member.IsReadable && !member.IsWritable)
                        {
                            continue;
                        }

                        member.IntKey = hiddenIntKey++;
                        if (isIntKey)
                        {
                            intMembers.Add(member.IntKey, member);
                        }
                        else
                        {
                            stringMembers.Add(member.StringKey, member);
                        }

                        firstMemberByName = false;
                    }
                }
            }
            else
            {
                // Public members with KeyAttribute except [Ignore] member.
                var searchFirst = true;
                var hiddenIntKey = 0;

                foreach (PropertyInfo item in GetAllProperties(type))
                {
                    if (item.GetCustomAttribute<IgnoreMemberAttribute>(true) != null)
                    {
                        continue;
                    }

                    if (item.GetCustomAttribute<IgnoreDataMemberAttribute>(true) != null)
                    {
                        continue;
                    }

                    if (item.IsIndexer())
                    {
                        continue;
                    }

                    MethodInfo getMethod = item.GetGetMethod(true);
                    MethodInfo setMethod = item.GetSetMethod(true);

                    var member = new EmittableMember
                    {
                        PropertyInfo = item,
                        IsReadable = (getMethod != null) && (allowPrivate || getMethod.IsPublic) && !getMethod.IsStatic,
                        IsWritable = (setMethod != null) && (allowPrivate || setMethod.IsPublic) && !setMethod.IsStatic,
                    };
                    if (!member.IsReadable && !member.IsWritable)
                    {
                        continue;
                    }

                    KeyAttribute key;
                    if (contractAttr != null)
                    {
                        // MessagePackObjectAttribute
                        key = item.GetCustomAttribute<KeyAttribute>(true);
                        if (key == null)
                        {
                            throw new MessagePackDynamicObjectResolverException("all public members must mark KeyAttribute or IgnoreMemberAttribute." + " type: " + type.FullName + " member:" + item.Name);
                        }

                        member.IsExplicitContract = true;
                        if (key.IntKey == null && key.StringKey == null)
                        {
                            throw new MessagePackDynamicObjectResolverException("both IntKey and StringKey are null." + " type: " + type.FullName + " member:" + item.Name);
                        }
                    }
                    else
                    {
                        // DataContractAttribute
                        DataMemberAttribute pseudokey = item.GetCustomAttribute<DataMemberAttribute>(true);
                        if (pseudokey == null)
                        {
                            // This member has no DataMemberAttribute nor IgnoreMemberAttribute.
                            // But the type *did* have a DataContractAttribute on it, so no attribute implies the member should not be serialized.
                            continue;
                        }

                        member.IsExplicitContract = true;

                        // use Order first
                        if (pseudokey.Order != -1)
                        {
                            key = new KeyAttribute(pseudokey.Order);
                        }
                        else if (pseudokey.Name != null)
                        {
                            key = new KeyAttribute(pseudokey.Name);
                        }
                        else
                        {
                            key = new KeyAttribute(item.Name); // use property name
                        }
                    }

                    if (searchFirst)
                    {
                        searchFirst = false;
                        isIntKey = key.IntKey != null;
                    }
                    else
                    {
                        if ((isIntKey && key.IntKey == null) || (!isIntKey && key.StringKey == null))
                        {
                            throw new MessagePackDynamicObjectResolverException("all members key type must be same." + " type: " + type.FullName + " member:" + item.Name);
                        }
                    }

                    if (isIntKey)
                    {
                        member.IntKey = key.IntKey.Value;
                        if (intMembers.ContainsKey(member.IntKey))
                        {
                            throw new MessagePackDynamicObjectResolverException("key is duplicated, all members key must be unique." + " type: " + type.FullName + " member:" + item.Name);
                        }

                        intMembers.Add(member.IntKey, member);
                    }
                    else
                    {
                        member.StringKey = key.StringKey;
                        if (stringMembers.ContainsKey(member.StringKey))
                        {
                            throw new MessagePackDynamicObjectResolverException("key is duplicated, all members key must be unique." + " type: " + type.FullName + " member:" + item.Name);
                        }

                        member.IntKey = hiddenIntKey++;
                        stringMembers.Add(member.StringKey, member);
                    }
                }

                foreach (FieldInfo item in GetAllFields(type))
                {
                    if (item.GetCustomAttribute<IgnoreMemberAttribute>(true) != null)
                    {
                        continue;
                    }

                    if (item.GetCustomAttribute<IgnoreDataMemberAttribute>(true) != null)
                    {
                        continue;
                    }

                    if (item.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>(true) != null)
                    {
                        continue;
                    }

                    if (item.IsStatic)
                    {
                        continue;
                    }

                    var member = new EmittableMember
                    {
                        FieldInfo = item,
                        IsReadable = allowPrivate || item.IsPublic,
                        IsWritable = allowPrivate || (item.IsPublic && !item.IsInitOnly),
                    };
                    if (!member.IsReadable && !member.IsWritable)
                    {
                        continue;
                    }

                    KeyAttribute key;
                    if (contractAttr != null)
                    {
                        // MessagePackObjectAttribute
                        key = item.GetCustomAttribute<KeyAttribute>(true);
                        if (key == null)
                        {
                            throw new MessagePackDynamicObjectResolverException("all public members must mark KeyAttribute or IgnoreMemberAttribute." + " type: " + type.FullName + " member:" + item.Name);
                        }

                        member.IsExplicitContract = true;
                        if (key.IntKey == null && key.StringKey == null)
                        {
                            throw new MessagePackDynamicObjectResolverException("both IntKey and StringKey are null." + " type: " + type.FullName + " member:" + item.Name);
                        }
                    }
                    else
                    {
                        // DataContractAttribute
                        DataMemberAttribute pseudokey = item.GetCustomAttribute<DataMemberAttribute>(true);
                        if (pseudokey == null)
                        {
                            // This member has no DataMemberAttribute nor IgnoreMemberAttribute.
                            // But the type *did* have a DataContractAttribute on it, so no attribute implies the member should not be serialized.
                            continue;
                        }

                        member.IsExplicitContract = true;

                        // use Order first
                        if (pseudokey.Order != -1)
                        {
                            key = new KeyAttribute(pseudokey.Order);
                        }
                        else if (pseudokey.Name != null)
                        {
                            key = new KeyAttribute(pseudokey.Name);
                        }
                        else
                        {
                            key = new KeyAttribute(item.Name); // use property name
                        }
                    }

                    if (searchFirst)
                    {
                        searchFirst = false;
                        isIntKey = key.IntKey != null;
                    }
                    else
                    {
                        if ((isIntKey && key.IntKey == null) || (!isIntKey && key.StringKey == null))
                        {
                            throw new MessagePackDynamicObjectResolverException("all members key type must be same." + " type: " + type.FullName + " member:" + item.Name);
                        }
                    }

                    if (isIntKey)
                    {
                        member.IntKey = key.IntKey.Value;
                        if (intMembers.ContainsKey(member.IntKey))
                        {
                            throw new MessagePackDynamicObjectResolverException("key is duplicated, all members key must be unique." + " type: " + type.FullName + " member:" + item.Name);
                        }

                        intMembers.Add(member.IntKey, member);
                    }
                    else
                    {
                        member.StringKey = key.StringKey;
                        if (stringMembers.ContainsKey(member.StringKey))
                        {
                            throw new MessagePackDynamicObjectResolverException("key is duplicated, all members key must be unique." + " type: " + type.FullName + " member:" + item.Name);
                        }

                        member.IntKey = hiddenIntKey++;
                        stringMembers.Add(member.StringKey, member);
                    }
                }
            }

            // GetConstructor
            IEnumerator<ConstructorInfo> ctorEnumerator = null;
            ConstructorInfo ctor = ti.DeclaredConstructors.SingleOrDefault(x => x.GetCustomAttribute<SerializationConstructorAttribute>(false) != null);
            if (ctor == null)
            {
                ctorEnumerator =
                    ti.DeclaredConstructors.Where(x => !x.IsStatic && (allowPrivate || x.IsPublic)).OrderByDescending(x => x.GetParameters().Length)
                    .GetEnumerator();

                if (ctorEnumerator.MoveNext())
                {
                    ctor = ctorEnumerator.Current;
                }
            }

            // struct allows null ctor
            if (ctor == null && !isStruct)
            {
                throw new MessagePackDynamicObjectResolverException("can't find public constructor. type:" + type.FullName);
            }

            var constructorParameters = new List<EmittableMemberAndConstructorParameter>();
            if (ctor != null)
            {
                ILookup<string, KeyValuePair<string, EmittableMember>> constructorLookupByKeyDictionary = stringMembers.ToLookup(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);
                ILookup<string, KeyValuePair<string, EmittableMember>> constructorLookupByMemberNameDictionary = stringMembers.ToLookup(x => x.Value.Name, x => x, StringComparer.OrdinalIgnoreCase);
                do
                {
                    constructorParameters.Clear();
                    var ctorParamIndex = 0;
                    foreach (ParameterInfo item in ctor.GetParameters())
                    {
                        EmittableMember paramMember;
                        if (isIntKey)
                        {
                            if (intMembers.TryGetValue(ctorParamIndex, out paramMember))
                            {
                                if ((item.ParameterType == paramMember.Type ||
                                    item.ParameterType.GetTypeInfo().IsAssignableFrom(paramMember.Type))
                                    && paramMember.IsReadable)
                                {
                                    constructorParameters.Add(new EmittableMemberAndConstructorParameter { ConstructorParameter = item, MemberInfo = paramMember });
                                }
                                else
                                {
                                    if (ctorEnumerator != null)
                                    {
                                        ctor = null;
                                        continue;
                                    }
                                    else
                                    {
                                        throw new MessagePackDynamicObjectResolverException("can't find matched constructor parameter, parameterType mismatch. type:" + type.FullName + " parameterIndex:" + ctorParamIndex + " paramterType:" + item.ParameterType.Name);
                                    }
                                }
                            }
                            else
                            {
                                if (ctorEnumerator != null)
                                {
                                    ctor = null;
                                    continue;
                                }
                                else
                                {
                                    throw new MessagePackDynamicObjectResolverException("can't find matched constructor parameter, index not found. type:" + type.FullName + " parameterIndex:" + ctorParamIndex);
                                }
                            }
                        }
                        else
                        {
                            // Lookup by both string key name and member name
                            IEnumerable<KeyValuePair<string, EmittableMember>> hasKey = constructorLookupByKeyDictionary[item.Name];
                            IEnumerable<KeyValuePair<string, EmittableMember>> hasKeyByMemberName = constructorLookupByMemberNameDictionary[item.Name];

                            var lenByKey = hasKey.Count();
                            var lenByMemberName = hasKeyByMemberName.Count();

                            var len = lenByKey;

                            // Prefer to use string key name unless a matching string key is not found but a matching member name is
                            if (lenByKey == 0 && lenByMemberName != 0)
                            {
                                len = lenByMemberName;
                                hasKey = hasKeyByMemberName;
                            }

                            if (len != 0)
                            {
                                if (len != 1)
                                {
                                    if (ctorEnumerator != null)
                                    {
                                        ctor = null;
                                        continue;
                                    }
                                    else
                                    {
                                        throw new MessagePackDynamicObjectResolverException("duplicate matched constructor parameter name:" + type.FullName + " parameterName:" + item.Name + " paramterType:" + item.ParameterType.Name);
                                    }
                                }

                                paramMember = hasKey.First().Value;
                                if (item.ParameterType.IsAssignableFrom(paramMember.Type) && paramMember.IsReadable)
                                {
                                    constructorParameters.Add(new EmittableMemberAndConstructorParameter { ConstructorParameter = item, MemberInfo = paramMember });
                                }
                                else
                                {
                                    if (ctorEnumerator != null)
                                    {
                                        ctor = null;
                                        continue;
                                    }
                                    else
                                    {
                                        throw new MessagePackDynamicObjectResolverException("can't find matched constructor parameter, parameterType mismatch. type:" + type.FullName + " parameterName:" + item.Name + " paramterType:" + item.ParameterType.Name);
                                    }
                                }
                            }
                            else
                            {
                                if (ctorEnumerator != null)
                                {
                                    ctor = null;
                                    continue;
                                }
                                else
                                {
                                    throw new MessagePackDynamicObjectResolverException("can't find matched constructor parameter, index not found. type:" + type.FullName + " parameterName:" + item.Name);
                                }
                            }
                        }

                        ctorParamIndex++;
                    }
                }
                while (TryGetNextConstructor(ctorEnumerator, ref ctor));

                if (ctor == null)
                {
                    throw new MessagePackDynamicObjectResolverException("can't find matched constructor. type:" + type.FullName);
                }
            }

            EmittableMember[] members;
            if (isIntKey)
            {
                members = intMembers.Values.OrderBy(x => x.IntKey).ToArray();
            }
            else
            {
                members = stringMembers.Values
                    .OrderBy(x =>
                    {
                        DataMemberAttribute attr = x.GetDataMemberAttribute();
                        if (attr == null)
                        {
                            return int.MaxValue;
                        }

                        return attr.Order;
                    })
                    .ToArray();
            }

            return new ObjectSerializationInfo
            {
                Type = type,
                IsClass = isClass,
                BestmatchConstructor = ctor,
                ConstructorParameters = constructorParameters.ToArray(),
                IsIntKey = isIntKey,
                Members = members.Where(m => m.IsExplicitContract || constructorParameters.Any(p => p.MemberInfo.Equals(m)) || m.IsWritable).ToArray(),
            };
        }

        private static IEnumerable<FieldInfo> GetAllFields(Type type)
        {
            if (type.BaseType is object)
            {
                foreach (var item in GetAllFields(type.BaseType))
                {
                    yield return item;
                }
            }

            // with declared only
            foreach (var item in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                yield return item;
            }
        }

        private static IEnumerable<PropertyInfo> GetAllProperties(Type type)
        {
            if (type.BaseType is object)
            {
                foreach (var item in GetAllProperties(type.BaseType))
                {
                    yield return item;
                }
            }

            // with declared only
            foreach (var item in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                yield return item;
            }
        }

        private static bool TryGetNextConstructor(IEnumerator<ConstructorInfo> ctorEnumerator, ref ConstructorInfo ctor)
        {
            if (ctorEnumerator == null || ctor != null)
            {
                return false;
            }

            if (ctorEnumerator.MoveNext())
            {
                ctor = ctorEnumerator.Current;
                return true;
            }
            else
            {
                ctor = null;
                return false;
            }
        }

        public class EmittableMemberAndConstructorParameter
        {
            public EmittableMember MemberInfo { get; set; }

            public ParameterInfo ConstructorParameter { get; set; }
        }

        public class EmittableMember
        {
            public bool IsProperty
            {
                get { return this.PropertyInfo != null; }
            }

            public bool IsField
            {
                get { return this.FieldInfo != null; }
            }

            public bool IsWritable { get; set; }

            public bool IsReadable { get; set; }

            public int IntKey { get; set; }

            public string StringKey { get; set; }

            public Type Type
            {
                get { return this.IsField ? this.FieldInfo.FieldType : this.PropertyInfo.PropertyType; }
            }

            public FieldInfo FieldInfo { get; set; }

            public PropertyInfo PropertyInfo { get; set; }

            public string Name
            {
                get
                {
                    return this.IsProperty ? this.PropertyInfo.Name : this.FieldInfo.Name;
                }
            }

            public bool IsValueType
            {
                get
                {
                    Type t = this.IsProperty ? this.PropertyInfo.PropertyType : this.FieldInfo.FieldType;
                    return t.IsValueType;
                }
            }

            /// <summary>
            /// Gets or sets a value indicating whether this member is explicitly opted in with an attribute.
            /// </summary>
            public bool IsExplicitContract { get; set; }

            public MessagePackFormatterAttribute GetMessagePackFormatterAttribute()
            {
                if (this.IsProperty)
                {
                    return (MessagePackFormatterAttribute)this.PropertyInfo.GetCustomAttribute<MessagePackFormatterAttribute>(true);
                }
                else
                {
                    return (MessagePackFormatterAttribute)this.FieldInfo.GetCustomAttribute<MessagePackFormatterAttribute>(true);
                }
            }

            public DataMemberAttribute GetDataMemberAttribute()
            {
                if (this.IsProperty)
                {
                    return (DataMemberAttribute)this.PropertyInfo.GetCustomAttribute<DataMemberAttribute>(true);
                }
                else
                {
                    return (DataMemberAttribute)this.FieldInfo.GetCustomAttribute<DataMemberAttribute>(true);
                }
            }

            public void EmitLoadValue(ILGenerator il)
            {
                if (this.IsProperty)
                {
                    il.EmitCall(this.PropertyInfo.GetGetMethod(true));
                }
                else
                {
                    il.Emit(OpCodes.Ldfld, this.FieldInfo);
                }
            }

            public void EmitStoreValue(ILGenerator il)
            {
                if (this.IsProperty)
                {
                    il.EmitCall(this.PropertyInfo.GetSetMethod(true));
                }
                else
                {
                    il.Emit(OpCodes.Stfld, this.FieldInfo);
                }
            }

            ////public object ReflectionLoadValue(object value)
            ////{
            ////    if (IsProperty)
            ////    {
            ////        return PropertyInfo.GetValue(value, null);
            ////    }
            ////    else
            ////    {
            ////        return FieldInfo.GetValue(value);
            ////    }
            ////}

            ////public void ReflectionStoreValue(object obj, object value)
            ////{
            ////    if (IsProperty)
            ////    {
            ////        PropertyInfo.SetValue(obj, value, null);
            ////    }
            ////    else
            ////    {
            ////        FieldInfo.SetValue(obj, value);
            ////    }
            ////}
        }

        private class OrderBaseTypesBeforeDerivedTypes : IComparer<Type>
        {
            internal static readonly OrderBaseTypesBeforeDerivedTypes Instance = new OrderBaseTypesBeforeDerivedTypes();

            private OrderBaseTypesBeforeDerivedTypes()
            {
            }

            public int Compare(Type x, Type y)
            {
                return
                    x.IsEquivalentTo(y) ? 0 :
                    x.IsAssignableFrom(y) ? -1 :
                    y.IsAssignableFrom(x) ? 1 :
                    0;
            }
        }
    }

    internal class MessagePackDynamicObjectResolverException : MessagePackSerializationException
    {
        public MessagePackDynamicObjectResolverException(string message)
            : base(message)
        {
        }
    }
}

#endif
