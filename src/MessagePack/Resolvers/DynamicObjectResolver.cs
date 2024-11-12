// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using MessagePack.Formatters;
using MessagePack.Internal;

#pragma warning disable SA1402 // File may only contain a single type
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
        public static readonly DynamicObjectResolver Instance = new();

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        internal static readonly DynamicAssemblyFactory DynamicAssemblyFactory = new(ModuleName);

        static DynamicObjectResolver()
        {
            Options = new MessagePackSerializerOptions(Instance);
        }

        private DynamicObjectResolver()
        {
        }

#if NETFRAMEWORK
        internal AssemblyBuilder? Save()
        {
            return DynamicAssemblyFactory.GetDynamicAssembly(type: null, allowPrivate: false)?.Save();
        }
#endif

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        internal static IMessagePackFormatter<T>? BuildFormatterHelper<T>(IFormatterResolver self, DynamicAssemblyFactory dynamicAssemblyFactory, bool forceStringKey, bool contractless, bool allowPrivate)
        {
            TypeInfo ti = typeof(T).GetTypeInfo();

            if (ti.IsInterface || ti.IsAbstract)
            {
                return null;
            }

            DynamicAssembly? dynamicAssembly = null;
            if (ti.IsAnonymous())
            {
                forceStringKey = true;
                contractless = true;

                // For anonymous types, it's important to be able to access the internal type itself,
                // but *not* look at non-public members to avoid double-serialization of the properties
                // as well as their backing fields.
                allowPrivate = false;
                dynamicAssembly = DynamicAssemblyFactory.GetDynamicAssembly(typeof(T), true);
            }
            else if (ti.IsNullable())
            {
                ti = ti.GenericTypeArguments[0].GetTypeInfo();

                var innerFormatter = self.GetFormatterDynamic(ti.AsType());
                if (innerFormatter == null)
                {
                    return null;
                }

                return (IMessagePackFormatter<T>?)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), [innerFormatter]);
            }

            allowPrivate |= !contractless && typeof(T).GetCustomAttributes<MessagePackObjectAttribute>().Any(a => a.AllowPrivate);
            dynamicAssembly ??= DynamicAssemblyFactory.GetDynamicAssembly(typeof(T), allowPrivate);
            TypeInfo? formatterTypeInfo = DynamicObjectTypeBuilder.BuildType(dynamicAssembly, typeof(T), forceStringKey, contractless, allowPrivate);
            return formatterTypeInfo is null ? null : (IMessagePackFormatter<T>)ResolverUtilities.ActivateFormatter(formatterTypeInfo.AsType());
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter = BuildFormatterHelper<T>(Instance, DynamicAssemblyFactory, false, false, false);
        }
    }

    /// <summary>
    /// ObjectResolver by dynamic code generation, allow private member.
    /// </summary>
    public sealed class DynamicObjectResolverAllowPrivate : IFormatterResolver
    {
        private const string ModuleName = "MessagePack.Resolvers.DynamicObjectResolverAllowPrivate";

        public static readonly DynamicObjectResolverAllowPrivate Instance = new DynamicObjectResolverAllowPrivate();

        internal static readonly DynamicAssemblyFactory DynamicAssemblyFactory = new(ModuleName);

        private DynamicObjectResolverAllowPrivate()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            internal static readonly IMessagePackFormatter<T>? Formatter = DynamicObjectResolver.BuildFormatterHelper<T>(Instance, DynamicAssemblyFactory, false, false, true);
        }
    }

    /// <summary>
    /// ObjectResolver by dynamic code generation, no needs MessagePackObject attribute and serialized key as string.
    /// </summary>
    public sealed class DynamicContractlessObjectResolver : IFormatterResolver
    {
        private const string ModuleName = "MessagePack.Resolvers.DynamicContractlessObjectResolver";

        public static readonly DynamicContractlessObjectResolver Instance = new DynamicContractlessObjectResolver();

        private static readonly DynamicAssemblyFactory DynamicAssemblyFactory = new(ModuleName);

        private DynamicContractlessObjectResolver()
        {
        }

        static DynamicContractlessObjectResolver()
        {
            DynamicAssemblyFactory = new DynamicAssemblyFactory(ModuleName);
        }

#if NETFRAMEWORK
        internal AssemblyBuilder? Save()
        {
            return DynamicAssemblyFactory.GetDynamicAssembly(type: null, false)?.Save();
        }
#endif

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter = typeof(T) == typeof(object) ? null :
                DynamicObjectResolver.BuildFormatterHelper<T>(Instance, DynamicAssemblyFactory, true, true, false);
        }
    }

    /// <summary>
    /// ObjectResolver by dynamic code generation, no needs MessagePackObject attribute and serialized key as string, allow private member.
    /// </summary>
    public sealed class DynamicContractlessObjectResolverAllowPrivate : IFormatterResolver
    {
        private const string ModuleName = "MessagePack.Resolvers.DynamicContractlessObjectResolverAllowPrivate";

        public static readonly DynamicContractlessObjectResolverAllowPrivate Instance = new DynamicContractlessObjectResolverAllowPrivate();

        private static readonly DynamicAssemblyFactory DynamicAssemblyFactory = new(ModuleName);

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter = typeof(T) == typeof(object) ? null :
                DynamicObjectResolver.BuildFormatterHelper<T>(Instance, DynamicAssemblyFactory, true, true, true);
        }
    }
}

namespace MessagePack.Internal
{
    internal static class DynamicObjectTypeBuilder
    {
        internal static readonly Regex SubtractFullNameRegex = new(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", DynamicAssembly.AvoidDynamicCode ? RegexOptions.None : RegexOptions.Compiled);

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

        internal static TypeInfo? BuildType(DynamicAssembly assembly, Type type, bool forceStringKey, bool contractless, bool allowPrivate)
        {
            if (ignoreTypes.Contains(type))
            {
                return null;
            }

            var serializationInfo = ObjectSerializationInfo.CreateOrNull(type, forceStringKey, contractless, allowPrivate);
            if (serializationInfo == null)
            {
                return null;
            }

            if (!allowPrivate && !(type.IsPublic || type.IsNestedPublic) && !type.GetTypeInfo().IsAnonymous())
            {
                throw new MessagePackSerializationException("Building dynamic formatter only allows public type. Type: " + type.FullName);
            }

            MessagePackEventSource.Instance.FormatterDynamicallyGeneratedStart();
            try
            {
                using (MonoProtection.EnterRefEmitLock())
                {
                    Type formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
                    TypeBuilder typeBuilder = assembly.DefineType("MessagePack.Formatters." + SubtractFullNameRegex.Replace(type.FullName!, string.Empty).Replace(".", "_") + "Formatter" + Interlocked.Increment(ref nameSequence), TypeAttributes.Public | TypeAttributes.Sealed, null, new[] { formatterType });

                    FieldBuilder? stringByteKeysField = null;
                    Dictionary<ObjectSerializationInfo.EmittableMember, FieldInfo>? customFormatterLookup = null;

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
                                il.EmitLdfld(stringByteKeysField!);
                            },
                            (index, member) =>
                            {
                                if (!customFormatterLookup.TryGetValue(member, out FieldInfo? fi))
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
                            typeBuilder,
                            il,
                            (index, member) =>
                            {
                                if (!customFormatterLookup.TryGetValue(member, out FieldInfo? fi))
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

                    TypeInfo? result = typeBuilder.CreateTypeInfo();

#if !NET6_0_OR_GREATER
                    if (result is not null)
                    {
                        foreach (var member in serializationInfo.Members)
                        {
                            member.OnTypeCreated(result);
                        }
                    }
#endif

                    return result;
                }
            }
            finally
            {
                MessagePackEventSource.Instance.FormatterDynamicallyGeneratedStop(type);
            }
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
                il.Emit(OpCodes.Ldstr, item.StringKey!);
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
                MessagePackFormatterAttribute? attr = item.GetMessagePackFormatterAttribute();
                if (attr is not null)
                {
                    // Verify that the specified formatter implements the required interface.
                    // Doing this now provides a more helpful error message than if we let the CLR throw an EntryPointNotFoundException later.
                    if (!attr.FormatterType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessagePackFormatter<>) && i.GenericTypeArguments[0].IsEquivalentTo(item.Type)))
                    {
                        throw new MessagePackSerializationException($"{info.Type.FullName}.{item.Name} is declared as type {item.Type.FullName}, but the prescribed {attr.FormatterType.FullName} does not implement IMessagePackFormatter<{item.Type.Name}>.");
                    }

                    FieldBuilder f = builder.DefineField(item.Name + "_formatter", attr.FormatterType, FieldAttributes.Private | FieldAttributes.InitOnly);

                    // If no args were provided and the formatter implements the singleton pattern, fetch the formatter from the field.
                    if ((attr.Arguments == null || attr.Arguments.Length == 0) && ResolverUtilities.FetchSingletonField(attr.FormatterType) is FieldInfo singletonField)
                    {
                        il.EmitLoadThis();
                        il.EmitLdsfld(singletonField);
                    }
                    else
                    {
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
                    }

                    il.Emit(OpCodes.Stfld, f);

                    dict.Add(item, f);
                }
            }

            return dict;
        }

        // void Serialize(ref [arg:1]MessagePackWriter writer, [arg:2]T value, [arg:3]MessagePackSerializerOptions options);
        private static void BuildSerialize(Type type, ObjectSerializationInfo info, ILGenerator il, Action emitStringByteKeys, Func<int, ObjectSerializationInfo.EmittableMember, Action?> tryEmitLoadCustomFormatter, int firstArgIndex)
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
                    if (intKeyMap.TryGetValue(i, out ObjectSerializationInfo.EmittableMember? member))
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
                    var valueLen = CodeGenHelpers.GetEncodedStringBytes(item.StringKey!).Length;
                    if (valueLen <= MessagePackRange.MaxFixStringLength)
                    {
                        if (UnsafeMemory.Is32Bit)
                        {
                            il.EmitCall(typeof(UnsafeMemory32).GetRuntimeMethod("WriteRaw" + valueLen, new[] { typeof(MessagePackWriter).MakeByRefType(), typeof(ReadOnlySpan<byte>) })!);
                        }
                        else
                        {
                            il.EmitCall(typeof(UnsafeMemory64).GetRuntimeMethod("WriteRaw" + valueLen, new[] { typeof(MessagePackWriter).MakeByRefType(), typeof(ReadOnlySpan<byte>) })!);
                        }
                    }
                    else
                    {
                        il.EmitCall(MessagePackWriterTypeInfo.WriteRaw);
                    }

                    EmitSerializeValue(il, type.GetTypeInfo(), item, index, tryEmitLoadCustomFormatter, argWriter, argValue, argOptions, localResolver);
                    index++;
                }
            }

            il.Emit(OpCodes.Ret);
        }

        private static void EmitSerializeValue(ILGenerator il, TypeInfo type, ObjectSerializationInfo.EmittableMember member, int index, Func<int, ObjectSerializationInfo.EmittableMember, Action?> tryEmitLoadCustomFormatter, ArgumentField argWriter, ArgumentField argValue, ArgumentField argOptions, LocalBuilder localResolver)
        {
            Label endLabel = il.DefineLabel();
            Type t = member.Type;
            Action? emitter = tryEmitLoadCustomFormatter(index, member);
            if (emitter is not null)
            {
                emitter();
                argWriter.EmitLoad();
                argValue.EmitLoad();
                member.EmitLoadValue(il);
                argOptions.EmitLoad();
                il.EmitCall(getSerialize(t));
            }
            else if (ObjectSerializationInfo.IsOptimizeTargetType(t))
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
                    il.EmitCall(typeof(MessagePackWriter).GetRuntimeMethod("Write", new Type[] { t })!);
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
        private static void BuildDeserialize(Type type, ObjectSerializationInfo info, TypeBuilder typeBuilder, ILGenerator il, Func<int, ObjectSerializationInfo.EmittableMember, Action?> tryEmitLoadCustomFormatter, int firstArgIndex)
        {
            var argReader = new ArgumentField(il, firstArgIndex, @ref: true);
            var argOptions = new ArgumentField(il, firstArgIndex + 1);

            // if (reader.TryReadNil()) { throw / return; }
            BuildDeserializeInternalTryReadNil(type, il, ref argReader);

            // T ____result;
            var localResult = il.DeclareLocal(type);

            // where T : new()
            var canOverwrite = info.ConstructorParameters.Length == 0;
            if (canOverwrite)
            {
                // ____result = new T();
                BuildDeserializeInternalCreateInstance(type, info, il, localResult);
            }

            // options.Security.DepthStep(ref reader);
            BuildDeserializeInternalDepthStep(il, ref argReader, ref argOptions);

            // var length = reader.Read(Map|Array)Header();
            var localLength = BuildDeserializeInternalReadHeaderLength(info, il, ref argReader);

            // var resolver = options.Resolver;
            var localResolver = BuildDeserializeInternalResolver(info, il, ref argOptions)!;

            if (info.IsIntKey)
            {
                // switch (key) { ... }
                BuildDeserializeInternalDeserializeEachPropertyIntKey(info, typeBuilder, il, tryEmitLoadCustomFormatter, canOverwrite, ref argReader, ref argOptions, localResolver, localResult, localLength);
            }
            else
            {
                // var span = reader.ReadStringSpan();
                BuildDeserializeInternalDeserializeEachPropertyStringKey(info, typeBuilder, il, tryEmitLoadCustomFormatter, canOverwrite, ref argReader, argOptions, localResolver, localResult, localLength);
            }

            // ____result.OnAfterDeserialize()
            BuildDeserializeInternalOnAfterDeserialize(type, info, il, localResult);

            // reader.Depth--;
            BuildDeserializeInternalDepthUnStep(il, ref argReader);

            // return ____result;
            il.Emit(OpCodes.Ldloc, localResult);
            il.Emit(OpCodes.Ret);
        }

        private static void BuildDeserializeInternalDeserializeEachPropertyStringKey(ObjectSerializationInfo info, TypeBuilder typeBuilder, ILGenerator il, Func<int, ObjectSerializationInfo.EmittableMember, Action?> tryEmitLoadCustomFormatter, bool canOverwrite, ref ArgumentField argReader, ArgumentField argOptions, LocalBuilder localResolver, LocalBuilder localResult, LocalBuilder localLength)
        {
            // Prepare local variables or assignment fields/properties
            var infoList = BuildDeserializeInternalDeserializationInfoArrayStringKey(info, il, canOverwrite);

            // Read Loop(for var i = 0; i < length; i++)
            BuildDeserializeInternalDeserializeLoopStringKey(typeBuilder, il, tryEmitLoadCustomFormatter, ref argReader, ref argOptions, infoList, localResolver, localResult, localLength, canOverwrite, info);

            if (canOverwrite)
            {
                return;
            }

            // ____result = new T(...);
            BuildDeserializeInternalCreateInstanceWithArguments(info, il, infoList, localResult);

            // ... if (__field__IsInitialized) { ____result.field = __field__; } ...
            BuildDeserializeInternalAssignFieldFromLocalVariableStringKey(info, typeBuilder, il, infoList, localResult);
        }

        private static void BuildDeserializeInternalDeserializeEachPropertyIntKey(ObjectSerializationInfo info, TypeBuilder typeBuilder, ILGenerator il, Func<int, ObjectSerializationInfo.EmittableMember, Action?> tryEmitLoadCustomFormatter, bool canOverwrite, ref ArgumentField argReader, ref ArgumentField argOptions, LocalBuilder localResolver, LocalBuilder localResult, LocalBuilder localLength)
        {
            // Prepare local variables or assignment fields/properties
            var infoList = BuildDeserializeInternalDeserializationInfoArrayIntKey(info, il, canOverwrite, out var gotoDefault, out var maxKey);

            // Read Loop(for var i = 0; i < length; i++)
            BuildDeserializeInternalDeserializeLoopIntKey(typeBuilder, il, tryEmitLoadCustomFormatter, ref argReader, ref argOptions, infoList, localResolver, localResult, localLength, canOverwrite, gotoDefault);

            if (canOverwrite)
            {
                return;
            }

            // ____result = new T(...);
            BuildDeserializeInternalCreateInstanceWithArguments(info, il, infoList, localResult);

            // ... ____result.field = __field__; ...
            BuildDeserializeInternalAssignFieldFromLocalVariableIntKey(typeBuilder, info, il, infoList, localResult, localLength, maxKey);
        }

        private static void BuildDeserializeInternalAssignFieldFromLocalVariableStringKey(ObjectSerializationInfo info, TypeBuilder typeBuilder, ILGenerator il, DeserializeInfo[] infoList, LocalBuilder localResult)
        {
            foreach (var item in infoList)
            {
                if (item.MemberInfo == null || item.IsInitializedLocalVariable == null || item.MemberInfo.IsWrittenByConstructor)
                {
                    continue;
                }

                // if (__field__IsInitialized) { ____result.field = __field__; }
                var skipLabel = il.DefineLabel();
                il.EmitLdloc(item.IsInitializedLocalVariable);
                il.Emit(OpCodes.Brfalse_S, skipLabel);

                if (info.IsClass)
                {
                    il.EmitLdloc(localResult);
                }
                else
                {
                    il.EmitLdloca(localResult);
                }

                il.EmitLdloc(item.LocalVariable!);
                item.MemberInfo.EmitStoreValue(il, typeBuilder);

                il.MarkLabel(skipLabel);
            }
        }

        private static void BuildDeserializeInternalAssignFieldFromLocalVariableIntKey(TypeBuilder typeBuilder, ObjectSerializationInfo info, ILGenerator il, DeserializeInfo[] infoList, LocalBuilder localResult, LocalBuilder localLength, int maxKey)
        {
            if (maxKey == -1)
            {
                return;
            }

            Label? memberAssignmentDoneLabel = null;
            var intKeyMap = infoList.Where(x => x.MemberInfo is not null && x.MemberInfo.IsWritable).ToDictionary(x => x.MemberInfo!.IntKey);
            for (var key = 0; key <= maxKey; key++)
            {
                if (!intKeyMap.TryGetValue(key, out var item))
                {
                    continue;
                }

                if (item.MemberInfo!.IsWrittenByConstructor)
                {
                    continue;
                }

                // if (length <= key) { goto MEMBER_ASSIGNMENT_DONE; }
                il.EmitLdloc(localLength);
                il.EmitLdc_I4(key);
                if (memberAssignmentDoneLabel == null)
                {
                    memberAssignmentDoneLabel = il.DefineLabel();
                }

                il.Emit(OpCodes.Ble, memberAssignmentDoneLabel.Value);

                // ____result.field = __field__;
                if (info.IsClass)
                {
                    il.EmitLdloc(localResult);
                }
                else
                {
                    il.EmitLdloca(localResult);
                }

                il.EmitLdloc(item.LocalVariable!);
                item.MemberInfo.EmitStoreValue(il, typeBuilder);
            }

            // MEMBER_ASSIGNMENT_DONE:
            if (memberAssignmentDoneLabel is not null)
            {
                il.MarkLabel(memberAssignmentDoneLabel.Value);
            }
        }

        private static void BuildDeserializeInternalCreateInstanceWithArguments(ObjectSerializationInfo info, ILGenerator il, DeserializeInfo[] infoList, LocalBuilder localResult)
        {
            foreach (var item in info.ConstructorParameters)
            {
                var local = infoList.First(x => x.MemberInfo == item.MemberInfo);
                il.EmitLdloc(local.LocalVariable!);

                if (!item.ConstructorParameter.ParameterType.IsValueType && local.MemberInfo?.IsValueType is true)
                {
                    // When a constructor argument of type object is being provided by a serialized member value that is a value type
                    // then that value must be boxed in order for the generated code to be valid (see issue #987). This may occur because
                    // the only requirement when determining whether a member value may be used to populate a constructor argument in an
                    // IsAssignableFrom check and typeof(object) IsAssignableFrom typeof(int), for example.
                    il.Emit(OpCodes.Box, local.MemberInfo.Type);
                }
            }

            il.Emit(OpCodes.Newobj, info.BestmatchConstructor!);
            il.Emit(OpCodes.Stloc, localResult);
        }

        private static DeserializeInfo[] BuildDeserializeInternalDeserializationInfoArrayStringKey(ObjectSerializationInfo info, ILGenerator il, bool canOverwrite)
        {
            var infoList = new DeserializeInfo[info.Members.Length];
            for (var i = 0; i < infoList.Length; i++)
            {
                var item = info.Members[i];
                if (canOverwrite && item.IsWritable)
                {
                    infoList[i] = new DeserializeInfo
                    {
                        MemberInfo = item,
                    };
                }
                else
                {
                    var isConstructorParameter = info.ConstructorParameters.Any(p => p.MemberInfo.Equals(item));
                    infoList[i] = new DeserializeInfo
                    {
                        MemberInfo = item,
                        LocalVariable = il.DeclareLocal(item.Type),
                        IsInitializedLocalVariable = isConstructorParameter ? default : il.DeclareLocal(typeof(bool)),
                    };
                }
            }

            return infoList;
        }

        private static DeserializeInfo[] BuildDeserializeInternalDeserializationInfoArrayIntKey(ObjectSerializationInfo info, ILGenerator il, bool canOverwrite, out Label? gotoDefault, out int maxKey)
        {
            maxKey = info.Members.Select(x => x.IntKey).DefaultIfEmpty(-1).Max();
            var len = maxKey + 1;
            var intKeyMap = info.Members.ToDictionary(x => x.IntKey);
            gotoDefault = null;

            var infoList = new DeserializeInfo[len];
            for (var i = 0; i < infoList.Length; i++)
            {
                if (intKeyMap.TryGetValue(i, out var member))
                {
                    if (canOverwrite && member.IsWritable)
                    {
                        infoList[i] = new DeserializeInfo
                        {
                            MemberInfo = member,
                            SwitchLabel = il.DefineLabel(),
                        };
                    }
                    else
                    {
                        infoList[i] = new DeserializeInfo
                        {
                            MemberInfo = member,
                            LocalVariable = il.DeclareLocal(member.Type),
                            SwitchLabel = il.DefineLabel(),
                        };
                    }
                }
                else
                {
                    // return null MemberInfo, should filter null
                    if (gotoDefault == null)
                    {
                        gotoDefault = il.DefineLabel();
                    }

                    infoList[i] = new DeserializeInfo
                    {
                        SwitchLabel = gotoDefault.Value,
                    };
                }
            }

            return infoList;
        }

        private static void BuildDeserializeInternalDeserializeLoopIntKey(TypeBuilder typeBuilder, ILGenerator il, Func<int, ObjectSerializationInfo.EmittableMember, Action?> tryEmitLoadCustomFormatter, ref ArgumentField argReader, ref ArgumentField argOptions, DeserializeInfo[] infoList, LocalBuilder localResolver, LocalBuilder localResult, LocalBuilder localLength, bool canOverwrite, Label? gotoDefault)
        {
            var key = il.DeclareLocal(typeof(int));
            var switchDefault = il.DefineLabel();
            var reader = argReader;
            var options = argOptions;

            void ForBody(LocalBuilder forILocal)
            {
                var loopEnd = il.DefineLabel();

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

                if (gotoDefault is not null)
                {
                    il.MarkLabel(gotoDefault.Value);
                    il.Emit(OpCodes.Br, switchDefault);
                }

                var i = 0;
                foreach (var item in infoList)
                {
                    if (item.MemberInfo == null)
                    {
                        continue;
                    }

                    il.MarkLabel(item.SwitchLabel);
                    if (canOverwrite)
                    {
                        BuildDeserializeInternalDeserializeValueAssignDirectly(typeBuilder, il, item, i++, tryEmitLoadCustomFormatter, ref reader, ref options, localResolver, localResult);
                    }
                    else
                    {
                        BuildDeserializeInternalDeserializeValueAssignLocalVariable(il, item, i++, tryEmitLoadCustomFormatter, ref reader, ref options, localResolver, localResult);
                    }

                    il.Emit(OpCodes.Br, loopEnd);
                }

                il.MarkLabel(loopEnd);
            }

            il.EmitIncrementFor(localLength, ForBody);
        }

        private static void BuildDeserializeInternalDeserializeLoopStringKey(TypeBuilder typeBuilder, ILGenerator il, Func<int, ObjectSerializationInfo.EmittableMember, Action?> tryEmitLoadCustomFormatter, ref ArgumentField argReader, ref ArgumentField argOptions, DeserializeInfo[] infoList, LocalBuilder localResolver, LocalBuilder localResult, LocalBuilder localLength, bool canOverwrite, ObjectSerializationInfo info)
        {
            var automata = new AutomataDictionary();
            for (var i = 0; i < info.Members.Length; i++)
            {
                automata.Add(info.Members[i].StringKey!, i);
            }

            var buffer = il.DeclareLocal(typeof(ReadOnlySpan<byte>));
            var longKey = il.DeclareLocal(typeof(ulong));
            var reader = argReader;
            var options = argOptions;

            // for (int i = 0; i < len; i++)
            void ForBody(LocalBuilder forILocal)
            {
                var readNext = il.DefineLabel();
                var loopEnd = il.DefineLabel();

                reader.EmitLdarg();
                il.EmitCall(ReadStringSpan);
                il.EmitStloc(buffer);

                // gen automata name lookup
                void OnFoundAssignDirect(KeyValuePair<string?, int> x)
                {
                    var i = x.Value;
                    var item = infoList[i];
                    if (item.MemberInfo is not null)
                    {
                        BuildDeserializeInternalDeserializeValueAssignDirectly(typeBuilder, il, item, i, tryEmitLoadCustomFormatter, ref reader, ref options, localResolver, localResult);
                        il.Emit(OpCodes.Br, loopEnd);
                    }
                    else
                    {
                        il.Emit(OpCodes.Br, readNext);
                    }
                }

                void OnFoundAssignLocalVariable(KeyValuePair<string?, int> x)
                {
                    var i = x.Value;
                    var item = infoList[i];
                    if (item.MemberInfo is not null)
                    {
                        BuildDeserializeInternalDeserializeValueAssignLocalVariable(il, item, i, tryEmitLoadCustomFormatter, ref reader, ref options, localResolver, localResult);
                        il.Emit(OpCodes.Br, loopEnd);
                    }
                    else
                    {
                        il.Emit(OpCodes.Br, readNext);
                    }
                }

                void OnNotFound()
                {
                    il.Emit(OpCodes.Br, readNext);
                }

#if NET_STANDARD_2_0
                throw new NotImplementedException("NET_STANDARD_2_0 directive was used");
#else
                if (canOverwrite)
                {
                    automata.EmitMatch(il, buffer, longKey, OnFoundAssignDirect, OnNotFound);
                }
                else
                {
                    automata.EmitMatch(il, buffer, longKey, OnFoundAssignLocalVariable, OnNotFound);
                }
#endif

                il.MarkLabel(readNext);
                reader.EmitLdarg();
                il.EmitCall(MessagePackReaderTypeInfo.Skip);

                il.MarkLabel(loopEnd);
            }

            il.EmitIncrementFor(localLength, ForBody);
        }

        private static void BuildDeserializeInternalTryReadNil(Type type, ILGenerator il, ref ArgumentField argReader)
        {
            // if(reader.TryReadNil()) { return null; }
            var falseLabel = il.DefineLabel();
            argReader.EmitLdarg();
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
        }

        private static void BuildDeserializeInternalDepthUnStep(ILGenerator il, ref ArgumentField argReader)
        {
            argReader.EmitLdarg();
            il.Emit(OpCodes.Dup);
            il.EmitCall(readerDepthGet);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Sub_Ovf);
            il.EmitCall(readerDepthSet);
        }

        private static void BuildDeserializeInternalOnAfterDeserialize(Type type, ObjectSerializationInfo info, ILGenerator il, LocalBuilder localResult)
        {
            if (type.GetTypeInfo().ImplementedInterfaces.All(x => x != typeof(IMessagePackSerializationCallbackReceiver)))
            {
                return;
            }

            if (info.IsClass)
            {
                il.EmitLdloc(localResult);
            }

            // call directly
            var runtimeMethod = type.GetRuntimeMethods().SingleOrDefault(x => x.Name == "OnAfterDeserialize");
            if (runtimeMethod is not null)
            {
                if (info.IsStruct)
                {
                    il.EmitLdloca(localResult);
                }

                il.Emit(OpCodes.Call, runtimeMethod); // don't use EmitCall helper(must use 'Call')
            }
            else
            {
                if (info.IsStruct)
                {
                    il.EmitLdloc(localResult);
                    il.Emit(OpCodes.Box, type);
                }

                il.EmitCall(onAfterDeserialize);
            }
        }

        private static LocalBuilder? BuildDeserializeInternalResolver(ObjectSerializationInfo info, ILGenerator il, ref ArgumentField argOptions)
        {
            if (!info.ShouldUseFormatterResolver)
            {
                return default;
            }

            // IFormatterResolver resolver = options.Resolver;
            var localResolver = il.DeclareLocal(typeof(IFormatterResolver));
            argOptions.EmitLoad();
            il.EmitCall(getResolverFromOptions);
            il.EmitStloc(localResolver);
            return localResolver;
        }

        private static LocalBuilder BuildDeserializeInternalReadHeaderLength(ObjectSerializationInfo info, ILGenerator il, ref ArgumentField argReader)
        {
            // var length = ReadMapHeader(ref byteSequence);
            var length = il.DeclareLocal(typeof(int)); // [loc:1]
            argReader.EmitLdarg();

            il.EmitCall(info.IsIntKey ? MessagePackReaderTypeInfo.ReadArrayHeader : MessagePackReaderTypeInfo.ReadMapHeader);

            il.EmitStloc(length);
            return length;
        }

        private static void BuildDeserializeInternalDepthStep(ILGenerator il, ref ArgumentField argReader, ref ArgumentField argOptions)
        {
            argOptions.EmitLoad();
            il.EmitCall(getSecurityFromOptions);
            argReader.EmitLdarg();
            il.EmitCall(securityDepthStep);
        }

        // where T : new();
        private static void BuildDeserializeInternalCreateInstance(Type type, ObjectSerializationInfo info, ILGenerator il, LocalBuilder localResult)
        {
            // var result = new T();
            if (info.IsClass)
            {
                il.Emit(OpCodes.Newobj, info.BestmatchConstructor!);
                il.EmitStloc(localResult);
            }
            else
            {
                il.Emit(OpCodes.Ldloca, localResult);
                il.Emit(OpCodes.Initobj, type);
            }
        }

        private static void BuildDeserializeInternalDeserializeValueAssignDirectly(TypeBuilder typeBuilder, ILGenerator il, DeserializeInfo info, int index, Func<int, ObjectSerializationInfo.EmittableMember, Action?> tryEmitLoadCustomFormatter, ref ArgumentField argReader, ref ArgumentField argOptions, LocalBuilder localResolver, LocalBuilder localResult)
        {
            var storeLabel = il.DefineLabel();
            var member = info.MemberInfo!;
            var t = member.Type;
            var emitter = tryEmitLoadCustomFormatter(index, member);

            if (member.IsWritable)
            {
                member.EmitPreStoreValue(typeBuilder, il, localResult);
            }
            else if (info.IsInitializedLocalVariable is not null)
            {
                il.EmitLdc_I4(1);
                il.EmitStloc(info.IsInitializedLocalVariable);
            }

            if (emitter is not null)
            {
                emitter();
                argReader.EmitLdarg();
                argOptions.EmitLoad();
                il.EmitCall(getDeserialize(t));
            }
            else if (ObjectSerializationInfo.IsOptimizeTargetType(t))
            {
                if (!t.GetTypeInfo().IsValueType)
                {
                    // As a nullable type (e.g. byte[] and string) we need to first call TryReadNil
                    // if (reader.TryReadNil())
                    var readNonNilValueLabel = il.DefineLabel();
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
                    var local = il.DeclareLocal(typeof(ReadOnlySequence<byte>?));
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
            if (member.IsWritable)
            {
                member.EmitStoreValue(il, typeBuilder);
            }
            else
            {
                il.Emit(OpCodes.Pop);
            }
        }

        private static void BuildDeserializeInternalDeserializeValueAssignLocalVariable(ILGenerator il, DeserializeInfo info, int index, Func<int, ObjectSerializationInfo.EmittableMember, Action?> tryEmitLoadCustomFormatter, ref ArgumentField argReader, ref ArgumentField argOptions, LocalBuilder localResolver, LocalBuilder localResult)
        {
            var storeLabel = il.DefineLabel();
            var member = info.MemberInfo!;
            var t = member.Type;
            var emitter = tryEmitLoadCustomFormatter(index, member);

            if (info.IsInitializedLocalVariable is not null)
            {
                il.EmitLdc_I4(1);
                il.EmitStloc(info.IsInitializedLocalVariable);
            }

            if (emitter is not null)
            {
                emitter();
                argReader.EmitLdarg();
                argOptions.EmitLoad();
                il.EmitCall(getDeserialize(t));
            }
            else if (ObjectSerializationInfo.IsOptimizeTargetType(t))
            {
                if (!t.GetTypeInfo().IsValueType)
                {
                    // As a nullable type (e.g. byte[] and string) we need to first call TryReadNil
                    // if (reader.TryReadNil())
                    var readNonNilValueLabel = il.DefineLabel();
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
                    var local = il.DeclareLocal(typeof(ReadOnlySequence<byte>?));
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
            il.EmitStloc(info.LocalVariable!);
        }

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

        // EmitInfos...
        private static readonly Type refMessagePackReader = typeof(MessagePackReader).MakeByRefType();

        private static readonly MethodInfo ReadOnlySpanFromByteArray = typeof(ReadOnlySpan<byte>).GetRuntimeMethod("op_Implicit", new[] { typeof(byte[]) })!;
        private static readonly MethodInfo ReadStringSpan = typeof(CodeGenHelpers).GetRuntimeMethod(nameof(CodeGenHelpers.ReadStringSpan), new[] { typeof(MessagePackReader).MakeByRefType() })!;
        private static readonly MethodInfo ArrayFromNullableReadOnlySequence = typeof(CodeGenHelpers).GetRuntimeMethod(nameof(CodeGenHelpers.GetArrayFromNullableSequence), new[] { typeof(ReadOnlySequence<byte>?).MakeByRefType() })!;

        private static readonly MethodInfo getFormatterWithVerify = typeof(FormatterResolverExtensions).GetRuntimeMethods().First(x => x.Name == nameof(FormatterResolverExtensions.GetFormatterWithVerify));
        private static readonly MethodInfo getResolverFromOptions = typeof(MessagePackSerializerOptions).GetRuntimeProperty(nameof(MessagePackSerializerOptions.Resolver))!.GetMethod!;
        private static readonly MethodInfo getSecurityFromOptions = typeof(MessagePackSerializerOptions).GetRuntimeProperty(nameof(MessagePackSerializerOptions.Security))!.GetMethod!;
        private static readonly MethodInfo securityDepthStep = typeof(MessagePackSecurity).GetRuntimeMethod(nameof(MessagePackSecurity.DepthStep), new[] { typeof(MessagePackReader).MakeByRefType() })!;
        private static readonly MethodInfo readerDepthGet = typeof(MessagePackReader).GetRuntimeProperty(nameof(MessagePackReader.Depth))!.GetMethod!;
        private static readonly MethodInfo readerDepthSet = typeof(MessagePackReader).GetRuntimeProperty(nameof(MessagePackReader.Depth))!.SetMethod!;
        private static readonly Func<Type, MethodInfo> getSerialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod(nameof(IMessagePackFormatter<int>.Serialize), new[] { typeof(MessagePackWriter).MakeByRefType(), t, typeof(MessagePackSerializerOptions) })!;
        private static readonly Func<Type, MethodInfo> getDeserialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod(nameof(IMessagePackFormatter<int>.Deserialize), new[] { refMessagePackReader, typeof(MessagePackSerializerOptions) })!;
        //// static readonly ConstructorInfo dictionaryConstructor = typeof(ByteArrayStringHashTable).GetTypeInfo().DeclaredConstructors.First(x => { var p = x.GetParameters(); return p.Length == 1 && p[0].ParameterType == typeof(int); });
        //// static readonly MethodInfo dictionaryAdd = typeof(ByteArrayStringHashTable).GetRuntimeMethod("Add", new[] { typeof(string), typeof(int) });
        //// static readonly MethodInfo dictionaryTryGetValue = typeof(ByteArrayStringHashTable).GetRuntimeMethod("TryGetValue", new[] { typeof(ArraySegment<byte>), refInt });
        private static readonly ConstructorInfo messagePackSerializationExceptionMessageOnlyConstructor = typeof(MessagePackSerializationException).GetTypeInfo().DeclaredConstructors.First(x =>
        {
            ParameterInfo[] p = x.GetParameters();
            return p.Length == 1 && p[0].ParameterType == typeof(string);
        });

        private static readonly MethodInfo onBeforeSerialize = typeof(IMessagePackSerializationCallbackReceiver).GetRuntimeMethod(nameof(IMessagePackSerializationCallbackReceiver.OnBeforeSerialize), Type.EmptyTypes)!;
        private static readonly MethodInfo onAfterDeserialize = typeof(IMessagePackSerializationCallbackReceiver).GetRuntimeMethod(nameof(IMessagePackSerializationCallbackReceiver.OnAfterDeserialize), Type.EmptyTypes)!;

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

            internal static readonly MethodInfo WriteMapHeader = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.WriteMapHeader), new[] { typeof(int) })!;
            internal static readonly MethodInfo WriteArrayHeader = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.WriteArrayHeader), new[] { typeof(int) })!;
            internal static readonly MethodInfo WriteBytes = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.Write), new[] { typeof(ReadOnlySpan<byte>) })!;
            internal static readonly MethodInfo WriteNil = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.WriteNil), Type.EmptyTypes)!;
            internal static readonly MethodInfo WriteRaw = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.WriteRaw), new[] { typeof(ReadOnlySpan<byte>) })!;
        }

        internal static class MessagePackReaderTypeInfo
        {
            internal static readonly TypeInfo TypeInfo = typeof(MessagePackReader).GetTypeInfo();

            internal static readonly MethodInfo ReadArrayHeader = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadArrayHeader), Type.EmptyTypes)!;
            internal static readonly MethodInfo ReadMapHeader = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadMapHeader), Type.EmptyTypes)!;
            internal static readonly MethodInfo ReadBytes = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadBytes), Type.EmptyTypes)!;
            internal static readonly MethodInfo TryReadNil = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.TryReadNil), Type.EmptyTypes)!;
            internal static readonly MethodInfo Skip = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.Skip), Type.EmptyTypes)!;
        }

        internal static class CodeGenHelpersTypeInfo
        {
            internal static readonly MethodInfo GetEncodedStringBytes = typeof(CodeGenHelpers).GetRuntimeMethod(nameof(CodeGenHelpers.GetEncodedStringBytes), new[] { typeof(string) })!;
        }

        internal static class EmitInfo
        {
            internal static readonly MethodInfo GetTypeFromHandle = ExpressionUtility.GetMethodInfo(() => Type.GetTypeFromHandle(default(RuntimeTypeHandle)));
            internal static readonly MethodInfo TypeGetProperty = ExpressionUtility.GetMethodInfo((Type t) => t.GetTypeInfo().GetProperty(default(string)!, default(BindingFlags)));
            internal static readonly MethodInfo TypeGetField = ExpressionUtility.GetMethodInfo((Type t) => t.GetTypeInfo().GetField(default(string)!, default(BindingFlags)));
            internal static readonly MethodInfo GetCustomAttributeMessagePackFormatterAttribute = ExpressionUtility.GetMethodInfo(() => CustomAttributeExtensions.GetCustomAttribute<MessagePackFormatterAttribute>(default(MemberInfo)!, default(bool)));
            internal static readonly MethodInfo ActivatorCreateInstance = ExpressionUtility.GetMethodInfo(() => Activator.CreateInstance(default(Type)!, default(object[])));

            internal static class MessagePackFormatterAttr
            {
                internal static readonly MethodInfo FormatterType = ExpressionUtility.GetPropertyInfo((MessagePackFormatterAttribute attr) => attr.FormatterType).GetGetMethod()!;
                internal static readonly MethodInfo Arguments = ExpressionUtility.GetPropertyInfo((MessagePackFormatterAttribute attr) => attr.Arguments).GetGetMethod()!;
            }
        }

        private class DeserializeInfo
        {
            internal ObjectSerializationInfo.EmittableMember? MemberInfo { get; set; }

            internal LocalBuilder? LocalVariable { get; set; }

            internal LocalBuilder? IsInitializedLocalVariable { get; set; }

            internal Label SwitchLabel { get; set; }
        }
    }

    internal class ObjectSerializationInfo
    {
        internal Type Type { get; }

        internal bool IsIntKey { get; }

        internal bool IsStringKey => !this.IsIntKey;

        internal bool IsClass { get; }

        internal bool IsStruct => !this.IsClass;

        internal bool ShouldUseFormatterResolver { get; private set; }

        internal ConstructorInfo? BestmatchConstructor { get; }

        internal EmittableMemberAndConstructorParameter[] ConstructorParameters { get; }

        internal EmittableMember[] Members { get; }

        private ObjectSerializationInfo(Type type, EmittableMemberAndConstructorParameter[] constructorParameters, EmittableMember[] members, bool isClass, ConstructorInfo? bestmatchConstructor, bool isIntKey)
        {
            this.Type = type;
            this.ConstructorParameters = constructorParameters;
            this.Members = members;
            this.IsClass = isClass;
            this.BestmatchConstructor = bestmatchConstructor;
            this.IsIntKey = isIntKey;

#if !NET6_0_OR_GREATER
            foreach (EmittableMember member in members)
            {
                // https://github.com/neuecc/MessagePack-CSharp/issues/1134 describes a bug in MethodBuilder
                // that blocks its ability to invoke property init accessors when in a generic class.
                if (member is { IsInitOnly: true, PropertyInfo: PropertyInfo { DeclaringType.IsGenericType: true } } && !this.ConstructorParameters.Any(cp => cp.MemberInfo == member))
                {
                    member.IsProblematicInitProperty = true;
                }
            }
#endif
        }

        internal static ObjectSerializationInfo? CreateOrNull(Type type, bool forceStringKey, bool contractless, bool allowPrivate)
        {
            TypeInfo ti = type.GetTypeInfo();
            var isClass = ti.IsClass || ti.IsInterface || ti.IsAbstract;
            var isClassRecord = isClass && IsClassRecord(ti);
            var isStruct = ti.IsValueType;

            MessagePackObjectAttribute? contractAttr = ti.GetCustomAttributes<MessagePackObjectAttribute>().FirstOrDefault();
            DataContractAttribute? dataContractAttr = ti.GetCustomAttribute<DataContractAttribute>();
            if (contractAttr == null && dataContractAttr == null && !forceStringKey && !contractless)
            {
                return null;
            }

            var isIntKey = true;
            var intMembers = new Dictionary<int, EmittableMember>();
            var stringMembers = new Dictionary<string, EmittableMember>();

            // When returning false, it means should ignoring this member.
            bool AddEmittableMemberOrIgnore(bool isIntKeyMode, EmittableMember member, bool checkConflicting)
            {
                if (checkConflicting)
                {
                    if (isIntKeyMode ? intMembers.TryGetValue(member.IntKey, out var conflictingMember) : stringMembers.TryGetValue(member.StringKey!, out conflictingMember))
                    {
                        // Quietly skip duplicate if this is an override property.
                        if (member.PropertyInfo is not null && conflictingMember.PropertyInfo is not null
                            && member.PropertyInfo.Name == conflictingMember.PropertyInfo.Name)
                        {
                            // According to MethodBase.IsVirtual docs an overridable property should be 'IsVirtual == true && IsFinal == false'.
                            // Property methods can be marked 'virtual final' in case of 'sealed override' or when implementing interface implicitly.
                            var isGetMethodOverridable = (conflictingMember.PropertyInfo.GetMethod?.IsVirtual ?? false) && !(conflictingMember.PropertyInfo.GetMethod?.IsFinal ?? false);
                            var isSetMethodOverridable = (conflictingMember.PropertyInfo.SetMethod?.IsVirtual ?? false) && !(conflictingMember.PropertyInfo.SetMethod?.IsFinal ?? false);
                            if (isGetMethodOverridable || isSetMethodOverridable)
                            {
                                return false;
                            }
                        }

                        var memberInfo = (MemberInfo?)member.PropertyInfo ?? member.FieldInfo;
                        throw new MessagePackDynamicObjectResolverException($"key is duplicated, all members key must be unique. type:{type.FullName} member:{memberInfo?.Name}");
                    }
                }

                if (isIntKeyMode)
                {
                    intMembers.Add(member.IntKey, member);
                }
                else
                {
                    stringMembers.Add(member.StringKey!, member);
                }

                return true;
            }

            EmittableMember? CreateEmittableMember(MemberInfo m)
            {
                if (m.IsDefined(typeof(IgnoreMemberAttribute), true) || m.IsDefined(typeof(IgnoreDataMemberAttribute), true) || m.IsDefined(typeof(NonSerializedAttribute), true))
                {
                    return null;
                }

                EmittableMember result;
                switch (m)
                {
                    case PropertyInfo property:
                        if (property.IsIndexer())
                        {
                            return null;
                        }

                        if (isClassRecord && property.Name == "EqualityContract")
                        {
                            return null;
                        }

                        var getMethod = property.GetGetMethod(true);
                        var setMethod = property.GetSetMethod(true);
                        result = new EmittableMember(property)
                        {
                            IsReadable = (getMethod is not null) && (allowPrivate || getMethod.IsPublic) && !getMethod.IsStatic,
                            IsWritable = (setMethod is not null) && (allowPrivate || setMethod.IsPublic) && !setMethod.IsStatic,
                        };
                        break;
                    case FieldInfo field:
                        if (field.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>(true) is not null)
                        {
                            return null;
                        }

                        if (field.IsStatic)
                        {
                            return null;
                        }

                        result = new EmittableMember(field)
                        {
                            IsReadable = allowPrivate || field.IsPublic,
                            IsWritable = allowPrivate || (field.IsPublic && !field.IsInitOnly),
                        };
                        break;
                    default:
                        throw new MessagePackSerializationException("unexpected member type");
                }

                return result.IsReadable || result.IsWritable ? result : null;
            }

            // Determine whether to ignore MessagePackObjectAttribute or DataContract.
            if (forceStringKey || contractless || (contractAttr?.KeyAsPropertyName == true))
            {
                // All public members are serialize target except [Ignore] member.
                isIntKey = !(forceStringKey || (contractAttr is not null && contractAttr.KeyAsPropertyName));
                var hiddenIntKey = 0;

                // Group the properties and fields by name to qualify members of the same name
                // (declared with the 'new' keyword) with the declaring type.
                var membersByName = type.GetRuntimeProperties().Concat(type.GetRuntimeFields().Cast<MemberInfo>())
                    .OrderBy(m => m.DeclaringType, OrderBaseTypesBeforeDerivedTypes.Instance)
                    .GroupBy(m => m.Name);
                foreach (var memberGroup in membersByName)
                {
                    var first = true;
                    foreach (var member in memberGroup.Select(CreateEmittableMember))
                    {
                        if (member is null)
                        {
                            continue;
                        }

                        var memberInfo = (MemberInfo?)member.PropertyInfo ?? member.FieldInfo!;
                        if (first)
                        {
                            first = false;
                            member.StringKey = memberInfo.Name;
                        }
                        else
                        {
                            member.StringKey = $"{memberInfo.DeclaringType!.FullName}.{memberInfo.Name}";
                        }

                        member.IntKey = hiddenIntKey++;
                        AddEmittableMemberOrIgnore(isIntKey, member, false);
                    }
                }
            }
            else
            {
                // Public members with KeyAttribute except [Ignore] member.
                var searchFirst = true;
                var hiddenIntKey = 0;

                var memberInfos = GetAllProperties(type).Cast<MemberInfo>().Concat(GetAllFields(type));
                foreach (var member in memberInfos.Select(CreateEmittableMember))
                {
                    if (member is null)
                    {
                        continue;
                    }

                    MemberInfo memberInfo = (MemberInfo?)member.PropertyInfo ?? member.FieldInfo!;

                    KeyAttribute key;
                    if (contractAttr is not null)
                    {
                        // MessagePackObjectAttribute. KeyAttribute must be marked, and IntKey or StringKey must be set.
                        key = memberInfo.GetCustomAttribute<KeyAttribute>(true) ??
                            throw new MessagePackDynamicObjectResolverException($"all public members must mark KeyAttribute or IgnoreMemberAttribute. type:{type.FullName} member:{memberInfo.Name}");
                        if (key.IntKey == null && key.StringKey == null)
                        {
                            throw new MessagePackDynamicObjectResolverException($"both IntKey and StringKey are null. type: {type.FullName} member:{memberInfo.Name}");
                        }
                    }
                    else
                    {
                        // DataContractAttribute. Try to use the DataMemberAttribute to fake KeyAttribute.
                        // This member has no DataMemberAttribute nor IgnoreMemberAttribute.
                        // But the type *did* have a DataContractAttribute on it, so no attribute implies the member should not be serialized.
                        var pseudokey = memberInfo.GetCustomAttribute<DataMemberAttribute>(true);
                        if (pseudokey == null)
                        {
                            continue;
                        }

                        key =
                            pseudokey.Order != -1 ? new KeyAttribute(pseudokey.Order) :
                            pseudokey.Name is not null ? new KeyAttribute(pseudokey.Name) :
                            new KeyAttribute(memberInfo.Name);
                    }

                    member.IsExplicitContract = true;

                    // Cannot assign StringKey and IntKey at the same time.
                    if (searchFirst)
                    {
                        searchFirst = false;
                        isIntKey = key.IntKey is not null;
                    }
                    else if ((isIntKey && key.IntKey == null) || (!isIntKey && key.StringKey == null))
                    {
                        throw new MessagePackDynamicObjectResolverException($"all members key type must be same. type: {type.FullName} member:{memberInfo.Name}");
                    }

                    if (isIntKey)
                    {
                        member.IntKey = key.IntKey!.Value;
                    }
                    else
                    {
                        member.StringKey = key.StringKey;
                        member.IntKey = hiddenIntKey++;
                    }

                    if (!AddEmittableMemberOrIgnore(isIntKey, member, true))
                    {
                        continue;
                    }
                }
            }

            // GetConstructor
            IEnumerator<ConstructorInfo>? ctorEnumerator = null;
            ConstructorInfo? ctor = ti.DeclaredConstructors.SingleOrDefault(x => x.GetCustomAttribute<SerializationConstructorAttribute>(false) is not null);
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
            if (ctor is not null)
            {
                IReadOnlyDictionary<int, EmittableMember> ctorParamIndexIntMembersDictionary = intMembers.OrderBy(x => x.Key).Select((x, i) => (Key: x.Value, Index: i)).ToDictionary(x => x.Index, x => x.Key);
                ILookup<string, KeyValuePair<string, EmittableMember>> constructorLookupByKeyDictionary = stringMembers.ToLookup(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);
                ILookup<string, KeyValuePair<string, EmittableMember>> constructorLookupByMemberNameDictionary = stringMembers.ToLookup(x => x.Value.Name, x => x, StringComparer.OrdinalIgnoreCase);
                do
                {
                    constructorParameters.Clear();
                    var ctorParamIndex = 0;
                    foreach (ParameterInfo item in ctor.GetParameters())
                    {
                        EmittableMember? paramMember;
                        if (isIntKey)
                        {
                            if (ctorParamIndexIntMembersDictionary.TryGetValue(ctorParamIndex, out paramMember))
                            {
                                if ((item.ParameterType == paramMember.Type ||
                                    item.ParameterType.GetTypeInfo().IsAssignableFrom(paramMember.Type))
                                    && paramMember.IsReadable)
                                {
                                    constructorParameters.Add(new EmittableMemberAndConstructorParameter(paramMember, item));
                                }
                                else
                                {
                                    if (ctorEnumerator is not null)
                                    {
                                        ctor = null;
                                        break;
                                    }
                                    else
                                    {
                                        throw new MessagePackDynamicObjectResolverException("can't find matched constructor parameter, parameterType mismatch. type:" + type.FullName + " parameterIndex:" + ctorParamIndex + " parameterType:" + item.ParameterType.Name);
                                    }
                                }
                            }
                            else
                            {
                                if (ctorEnumerator is not null)
                                {
                                    ctor = null;
                                    break;
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
                            IEnumerable<KeyValuePair<string, EmittableMember>> hasKey = constructorLookupByKeyDictionary[item.Name!];
                            IEnumerable<KeyValuePair<string, EmittableMember>> hasKeyByMemberName = constructorLookupByMemberNameDictionary[item.Name!];

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
                                paramMember = hasKey.First().Value;
                                if (item.ParameterType.IsAssignableFrom(paramMember.Type) && paramMember.IsReadable)
                                {
                                    constructorParameters.Add(new EmittableMemberAndConstructorParameter(paramMember, item));
                                }
                                else
                                {
                                    if (ctorEnumerator is not null)
                                    {
                                        ctor = null;
                                        break;
                                    }
                                    else
                                    {
                                        throw new MessagePackDynamicObjectResolverException("can't find matched constructor parameter, parameterType mismatch. type:" + type.FullName + " parameterName:" + item.Name + " parameterType:" + item.ParameterType.Name);
                                    }
                                }
                            }
                            else
                            {
                                if (ctorEnumerator is not null)
                                {
                                    ctor = null;
                                    break;
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
                        DataMemberAttribute? attr = x.GetDataMemberAttribute();
                        if (attr == null)
                        {
                            return int.MaxValue;
                        }

                        return attr.Order;
                    })
                    .ToArray();
            }

            var shouldUseFormatterResolver = false;

            // Mark each member that will be set by way of the constructor.
            foreach (var item in constructorParameters)
            {
                item.MemberInfo.IsWrittenByConstructor = true;
            }

            var membersArray = members.Where(m => m.IsExplicitContract || m.IsWrittenByConstructor || m.IsWritable).ToArray();
            foreach (var member in membersArray)
            {
                if (IsOptimizeTargetType(member.Type))
                {
                    continue;
                }

                var attr = member.GetMessagePackFormatterAttribute();
                if (!(attr is null))
                {
                    continue;
                }

                shouldUseFormatterResolver = true;
                break;
            }

            return new ObjectSerializationInfo(type, constructorParameters.ToArray(), membersArray, isClass, ctor, isIntKey)
            {
                ShouldUseFormatterResolver = shouldUseFormatterResolver,
            };
        }

        /// <devremarks>
        /// Keep this list in sync with ShouldUseFormatterResolverHelper.PrimitiveTypes.
        /// </devremarks>
        internal static bool IsOptimizeTargetType(Type type)
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

        private static bool IsClassRecord(TypeInfo type)
        {
            // The only truly unique thing about a C# 9 record class is the presence of a <Clone>$ method,
            // which cannot be declared in C# because of the reserved characters in its name.
            return type.IsClass
                && type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is object;
        }

        private static bool TryGetNextConstructor(IEnumerator<ConstructorInfo>? ctorEnumerator, [NotNullWhen(true)] ref ConstructorInfo? ctor)
        {
            if (ctorEnumerator == null || ctor is not null)
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

        internal class EmittableMemberAndConstructorParameter
        {
            internal EmittableMemberAndConstructorParameter(EmittableMember memberInfo, ParameterInfo constructorParameter)
            {
                this.MemberInfo = memberInfo;
                this.ConstructorParameter = constructorParameter;
            }

            internal EmittableMember MemberInfo { get; }

            internal ParameterInfo ConstructorParameter { get; }
        }

        internal class EmittableMember
        {
            internal EmittableMember(MemberInfo memberInfo)
            {
                this.MemberInfo = memberInfo;
            }

            internal bool IsProperty => this.PropertyInfo is not null;

            internal bool IsField => this.FieldInfo is not null;

            internal bool IsWritable { get; set; }

            internal bool IsWrittenByConstructor { get; set; }

            /// <summary>
            /// Gets a value indicating whether the property can only be set by an object initializer, a constructor, or another `init` member.
            /// </summary>
            internal bool IsInitOnly => this.PropertyInfo?.GetSetMethod(true)?.ReturnParameter.GetRequiredCustomModifiers().Any(modifierType => modifierType.FullName == "System.Runtime.CompilerServices.IsExternalInit") ?? false;

            internal bool IsReadable { get; set; }

            internal int IntKey { get; set; }

            internal string? StringKey { get; set; }

            internal Type Type => this.FieldInfo?.FieldType ?? this.PropertyInfo!.PropertyType;

            internal MemberInfo MemberInfo { get; }

            internal FieldInfo? FieldInfo => this.MemberInfo as FieldInfo;

            internal string Name => this.PropertyInfo?.Name ?? this.FieldInfo!.Name;

            internal PropertyInfo? PropertyInfo => this.MemberInfo as PropertyInfo;

            internal bool IsValueType
            {
                get
                {
                    Type t = this.PropertyInfo?.PropertyType ?? this.FieldInfo!.FieldType;
                    return t.IsValueType;
                }
            }

            /// <summary>
            /// Gets or sets a value indicating whether this member is explicitly opted in with an attribute.
            /// </summary>
            internal bool IsExplicitContract { get; set; }

#if !NET6_0_OR_GREATER
            /// <summary>
            /// Gets or sets a value indicating whether this member is a property with an <see langword="init" /> property setter
            /// that must be set via a <see cref="DynamicMethod"/> rather than directly by a <see cref="MethodBuilder"/>.
            /// </summary>
            /// <remarks>
            /// <see href="https://github.com/neuecc/MessagePack-CSharp/issues/1134">A bug</see> in <see cref="MethodBuilder"/>
            /// blocks its ability to invoke property init accessors when in a generic class.
            /// </remarks>
            internal bool IsProblematicInitProperty { get; set; }
#endif

            internal MessagePackFormatterAttribute? GetMessagePackFormatterAttribute()
            {
                return this.PropertyInfo is not null
                    ? this.PropertyInfo.GetCustomAttribute<MessagePackFormatterAttribute>(true)
                    : this.FieldInfo!.GetCustomAttribute<MessagePackFormatterAttribute>(true);
            }

            internal DataMemberAttribute? GetDataMemberAttribute()
            {
                return this.PropertyInfo is not null
                    ? this.PropertyInfo.GetCustomAttribute<DataMemberAttribute>(true)
                    : this.FieldInfo!.GetCustomAttribute<DataMemberAttribute>(true);
            }

            internal void EmitLoadValue(ILGenerator il)
            {
                if (this.PropertyInfo is not null)
                {
                    il.EmitCall(this.PropertyInfo.GetGetMethod(true) ?? throw new Exception("No get accessor"));
                }
                else
                {
                    il.Emit(OpCodes.Ldfld, this.FieldInfo!);
                }
            }

#if !NET6_0_OR_GREATER
            private delegate void PropertySetterHelperForStructs<T, TValue>(ref T target, TValue value)
                where T : struct;

            private Delegate? setterHelperDelegate;
            private FieldBuilder? setterHelperField;

            internal void OnTypeCreated(TypeInfo formatterType)
            {
                if (this.setterHelperDelegate is not null && this.setterHelperField is not null)
                {
                    // Set this delegate to a static field on the dynamic formatter so that we can invoke it here.
                    formatterType.GetField(this.setterHelperField.Name, BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, this.setterHelperDelegate);
                }
            }
#endif

            internal void EmitPreStoreValue(TypeBuilder typeBuilder, ILGenerator il, LocalBuilder localResult)
            {
#if !NET6_0_OR_GREATER
                if (this.PropertyInfo is not null && this.IsProblematicInitProperty)
                {
                    // On all runtimes older than .NET 6 (i.e. .NET Framework), a bug prevents MethodBuilder from being able to generate code that calls
                    // a property "init" setter that belongs to a generic type.
                    // But DynamicMethod does not share that bug. So we'll use a DynamicMethod to workaround this and invoke that from our MethodBuilder code.
                    Type[] parameterTypes = [localResult.LocalType.IsClass ? this.MemberInfo.DeclaringType : this.MemberInfo.DeclaringType.MakeByRefType(), this.Type];
                    DynamicMethod dynamicMethod = new($"Set{this.Name}Helper", null, parameterTypes);
                    ILGenerator dynamicMethodIL = dynamicMethod.GetILGenerator();
                    dynamicMethodIL.Emit(OpCodes.Ldarg_0);
                    dynamicMethodIL.Emit(OpCodes.Ldarg_1);
                    dynamicMethodIL.EmitCall(this.PropertyInfo.GetSetMethod(true) ?? throw new Exception("No set accessor"));
                    dynamicMethodIL.Emit(OpCodes.Ret);
                    Type delegateType = (localResult.LocalType.IsClass ? typeof(Action<,>) : typeof(PropertySetterHelperForStructs<,>)).MakeGenericType([this.MemberInfo.DeclaringType, this.Type]);
                    this.setterHelperDelegate = dynamicMethod.CreateDelegate(delegateType);

                    // Define a static field on the formatter that will store the delegate once the formatter type is created.
                    this.setterHelperField = typeBuilder.DefineField($"{this.Name}Setter", delegateType, FieldAttributes.Private | FieldAttributes.Static);

                    // Now emit code which at runtime will read from that field to get the delegate onto the stack for invocation.
                    il.Emit(OpCodes.Ldsfld, this.setterHelperField);
                }
#endif

                if (localResult.LocalType.IsClass)
                {
                    il.EmitLdloc(localResult);
                }
                else
                {
                    il.EmitLdloca(localResult);
                }
            }

            internal void EmitStoreValue(ILGenerator il, TypeBuilder typeBuilder)
            {
                if (this.PropertyInfo is not null)
                {
#if !NET6_0_OR_GREATER
                    if (this.IsProblematicInitProperty)
                    {
                        // Use a DynamicMethod to workaround this.
                        if (this.setterHelperDelegate is null)
                        {
                            throw new Exception();
                        }

                        // EmitPreStoreValue loaded the delegate and target object onto the stack.
                        // Our caller then pushed the value to set onto the stack.
                        // All we need to do now is invoke the setter delegate.
                        il.Emit(OpCodes.Callvirt, this.setterHelperDelegate.GetType().GetMethod("Invoke") ?? throw new Exception("Unable to find Invoke method"));
                    }
                    else
#endif
                    {
                        il.EmitCall(this.PropertyInfo.GetSetMethod(true) ?? throw new Exception("No set accessor"));
                    }
                }
                else
                {
                    il.Emit(OpCodes.Stfld, this.FieldInfo!);
                }
            }
        }

        private class OrderBaseTypesBeforeDerivedTypes : IComparer<Type?>
        {
            internal static readonly OrderBaseTypesBeforeDerivedTypes Instance = new OrderBaseTypesBeforeDerivedTypes();

            private OrderBaseTypesBeforeDerivedTypes()
            {
            }

            public int Compare(Type? x, Type? y)
            {
                if (x is null || y is null)
                {
                    throw new NotSupportedException();
                }

                return
                    x == y || x.IsEquivalentTo(y) ? 0 :
                    x.IsAssignableFrom(y) ? -1 :
                    y.IsAssignableFrom(x) ? 1 :
                    0;
            }
        }
    }

    internal class MessagePackDynamicObjectResolverException : MessagePackSerializationException
    {
        internal MessagePackDynamicObjectResolverException(string message)
            : base(message)
        {
        }
    }
}
