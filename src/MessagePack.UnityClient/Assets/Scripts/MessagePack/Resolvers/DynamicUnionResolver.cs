// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !(UNITY_2018_3_OR_NEWER && NET_STANDARD_2_0)

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading;
using MessagePack.Formatters;
using MessagePack.Internal;

#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1509 // Opening braces should not be preceded by blank line

namespace MessagePack.Resolvers
{
    /// <summary>
    /// UnionResolver by dynamic code generation.
    /// </summary>
    public sealed class DynamicUnionResolver : IFormatterResolver
    {
        private const string ModuleName = "MessagePack.Resolvers.DynamicUnionResolver";

        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly DynamicUnionResolver Instance;

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        private static readonly Lazy<DynamicAssembly> DynamicAssembly;
#if !UNITY_2018_3_OR_NEWER
        private static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled);
#else
        private static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+");
#endif

        private static int nameSequence = 0;

        static DynamicUnionResolver()
        {
            Instance = new DynamicUnionResolver();
            Options = new MessagePackSerializerOptions(Instance);
            DynamicAssembly = new Lazy<DynamicAssembly>(() => new DynamicAssembly(ModuleName));
        }

        private DynamicUnionResolver()
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
                if (ti.IsNullable())
                {
                    ti = ti.GenericTypeArguments[0].GetTypeInfo();

                    var innerFormatter = DynamicUnionResolver.Instance.GetFormatterDynamic(ti.AsType());
                    if (innerFormatter == null)
                    {
                        return;
                    }

                    Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }

                TypeInfo formatterTypeInfo = BuildType(typeof(T));
                if (formatterTypeInfo == null)
                {
                    return;
                }

                Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterTypeInfo.AsType());
            }
        }

        private static TypeInfo BuildType(Type type)
        {
            TypeInfo ti = type.GetTypeInfo();

            // order by key(important for use jump-table of switch)
            UnionAttribute[] unionAttrs = ti.GetCustomAttributes<UnionAttribute>().OrderBy(x => x.Key).ToArray();

            if (unionAttrs.Length == 0)
            {
                return null;
            }

            if (!ti.IsInterface && !ti.IsAbstract)
            {
                throw new MessagePackDynamicUnionResolverException("Union can only be interface or abstract class. Type:" + type.Name);
            }

            var checker1 = new HashSet<int>();
            var checker2 = new HashSet<Type>();
            foreach (UnionAttribute item in unionAttrs)
            {
                if (!checker1.Add(item.Key))
                {
                    throw new MessagePackDynamicUnionResolverException("Same union key has found. Type:" + type.Name + " Key:" + item.Key);
                }

                if (!checker2.Add(item.SubType))
                {
                    throw new MessagePackDynamicUnionResolverException("Same union subType has found. Type:" + type.Name + " SubType: " + item.SubType);
                }
            }

            Type formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
            TypeBuilder typeBuilder = DynamicAssembly.Value.DefineType("MessagePack.Formatters." + SubtractFullNameRegex.Replace(type.FullName, string.Empty).Replace(".", "_") + "Formatter" + +Interlocked.Increment(ref nameSequence), TypeAttributes.Public | TypeAttributes.Sealed, null, new[] { formatterType });

            FieldBuilder typeToKeyAndJumpMap = null; // Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>
            FieldBuilder keyToJumpMap = null; // Dictionary<int, int>

            // create map dictionary
            {
                ConstructorBuilder method = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                typeToKeyAndJumpMap = typeBuilder.DefineField("typeToKeyAndJumpMap", typeof(Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>), FieldAttributes.Private | FieldAttributes.InitOnly);
                keyToJumpMap = typeBuilder.DefineField("keyToJumpMap", typeof(Dictionary<int, int>), FieldAttributes.Private | FieldAttributes.InitOnly);

                ILGenerator il = method.GetILGenerator();
                BuildConstructor(type, unionAttrs, method, typeToKeyAndJumpMap, keyToJumpMap, il);
            }

            {
                MethodBuilder method = typeBuilder.DefineMethod(
                    "Serialize",
                    MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                    null,
                    new Type[] { typeof(MessagePackWriter).MakeByRefType(), type, typeof(MessagePackSerializerOptions) });

                ILGenerator il = method.GetILGenerator();
                BuildSerialize(type, unionAttrs, method, typeToKeyAndJumpMap, il);
            }

            {
                MethodBuilder method = typeBuilder.DefineMethod(
                    "Deserialize",
                    MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                    type,
                    new Type[] { refMessagePackReader, typeof(MessagePackSerializerOptions) });

                ILGenerator il = method.GetILGenerator();
                BuildDeserialize(type, unionAttrs, method, keyToJumpMap, il);
            }

            return typeBuilder.CreateTypeInfo();
        }

        private static void BuildConstructor(Type type, UnionAttribute[] infos, ConstructorInfo method, FieldBuilder typeToKeyAndJumpMap, FieldBuilder keyToJumpMap, ILGenerator il)
        {
            il.EmitLdarg(0);
            il.Emit(OpCodes.Call, objectCtor);

            {
                il.EmitLdarg(0);
                il.EmitLdc_I4(infos.Length);
                il.Emit(OpCodes.Ldsfld, runtimeTypeHandleEqualityComparer);
                il.Emit(OpCodes.Newobj, typeMapDictionaryConstructor);

                var index = 0;
                foreach (UnionAttribute item in infos)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldtoken, item.SubType);
                    il.EmitLdc_I4(item.Key);
                    il.EmitLdc_I4(index);
                    il.Emit(OpCodes.Newobj, intIntKeyValuePairConstructor);
                    il.EmitCall(typeMapDictionaryAdd);

                    index++;
                }

                il.Emit(OpCodes.Stfld, typeToKeyAndJumpMap);
            }

            {
                il.EmitLdarg(0);
                il.EmitLdc_I4(infos.Length);
                il.Emit(OpCodes.Newobj, keyMapDictionaryConstructor);

                var index = 0;
                foreach (UnionAttribute item in infos)
                {
                    il.Emit(OpCodes.Dup);
                    il.EmitLdc_I4(item.Key);
                    il.EmitLdc_I4(index);
                    il.EmitCall(keyMapDictionaryAdd);

                    index++;
                }

                il.Emit(OpCodes.Stfld, keyToJumpMap);
            }

            il.Emit(OpCodes.Ret);
        }

        // void Serialize([arg:1]MessagePackWriter writer, [arg:2]T value, [arg:3]MessagePackSerializerOptions options);
        private static void BuildSerialize(Type type, UnionAttribute[] infos, MethodBuilder method, FieldBuilder typeToKeyAndJumpMap, ILGenerator il)
        {
            // if(value == null) return WriteNil
            Label elseBody = il.DefineLabel();
            Label notFoundType = il.DefineLabel();

            il.EmitLdarg(2);
            il.Emit(OpCodes.Brtrue_S, elseBody);
            il.Emit(OpCodes.Br, notFoundType);
            il.MarkLabel(elseBody);

            // IFormatterResolver resolver = options.Resolver;
            LocalBuilder localResolver = il.DeclareLocal(typeof(IFormatterResolver));
            il.EmitLdarg(3);
            il.EmitCall(getResolverFromOptions);
            il.EmitStloc(localResolver);

            LocalBuilder keyPair = il.DeclareLocal(typeof(KeyValuePair<int, int>));

            il.EmitLoadThis();
            il.EmitLdfld(typeToKeyAndJumpMap);
            il.EmitLdarg(2);
            il.EmitCall(objectGetType);
            il.EmitCall(getTypeHandle);
            il.EmitLdloca(keyPair);
            il.EmitCall(typeMapDictionaryTryGetValue);
            il.Emit(OpCodes.Brfalse, notFoundType);

            // writer.WriteArrayHeader(2, false);
            il.EmitLdarg(1);
            il.EmitLdc_I4(2);
            il.EmitCall(MessagePackWriterTypeInfo.WriteArrayHeader);

            // writer.Write(keyPair.Key)
            il.EmitLdarg(1);
            il.EmitLdloca(keyPair);
            il.EmitCall(intIntKeyValuePairGetKey);
            il.EmitCall(MessagePackWriterTypeInfo.WriteInt32);

            Label loopEnd = il.DefineLabel();

            // switch-case (offset += resolver.GetFormatter.Serialize(with cast)
            var switchLabels = infos.Select(x => new { Label = il.DefineLabel(), Attr = x }).ToArray();
            il.EmitLdloca(keyPair);
            il.EmitCall(intIntKeyValuePairGetValue);
            il.Emit(OpCodes.Switch, switchLabels.Select(x => x.Label).ToArray());
            il.Emit(OpCodes.Br, loopEnd); // default

            foreach (var item in switchLabels)
            {
                il.MarkLabel(item.Label);
                il.EmitLdloc(localResolver);
                il.Emit(OpCodes.Call, getFormatterWithVerify.MakeGenericMethod(item.Attr.SubType));

                il.EmitLdarg(1);
                il.EmitLdarg(2);
                if (item.Attr.SubType.GetTypeInfo().IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, item.Attr.SubType);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, item.Attr.SubType);
                }

                il.EmitLdarg(3);
                il.Emit(OpCodes.Callvirt, getSerialize(item.Attr.SubType));

                il.Emit(OpCodes.Br, loopEnd);
            }

            // return;
            il.MarkLabel(loopEnd);
            il.Emit(OpCodes.Ret);

            // else, return WriteNil
            il.MarkLabel(notFoundType);
            il.EmitLdarg(1);
            il.EmitCall(MessagePackWriterTypeInfo.WriteNil);
            il.Emit(OpCodes.Ret);
        }

        // T Deserialize([arg:1]ref MessagePackReader reader, [arg:2]MessagePackSerializerOptions options);
        private static void BuildDeserialize(Type type, UnionAttribute[] infos, MethodBuilder method, FieldBuilder keyToJumpMap, ILGenerator il)
        {
            // if(MessagePackBinary.TryReadNil()) { return null; }
            Label falseLabel = il.DefineLabel();
            il.EmitLdarg(1);
            il.EmitCall(MessagePackReaderTypeInfo.TryReadNil);
            il.Emit(OpCodes.Brfalse_S, falseLabel);

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);

            il.MarkLabel(falseLabel);

            // IFormatterResolver resolver = options.Resolver;
            LocalBuilder localResolver = il.DeclareLocal(typeof(IFormatterResolver));
            il.EmitLdarg(2);
            il.EmitCall(getResolverFromOptions);
            il.EmitStloc(localResolver);

            // read-array header and validate, reader.ReadArrayHeader() != 2) throw;
            Label rightLabel = il.DefineLabel();
            var reader = new ArgumentField(il, 1);
            reader.EmitLdarg();
            il.EmitCall(MessagePackReaderTypeInfo.ReadArrayHeader);
            il.EmitLdc_I4(2);
            il.Emit(OpCodes.Beq_S, rightLabel);
            il.Emit(OpCodes.Ldstr, "Invalid Union data was detected. Type:" + type.FullName);
            il.Emit(OpCodes.Newobj, invalidOperationExceptionConstructor);
            il.Emit(OpCodes.Throw);

            il.MarkLabel(rightLabel);

            // read key
            LocalBuilder key = il.DeclareLocal(typeof(int));
            reader.EmitLdarg();
            il.EmitCall(MessagePackReaderTypeInfo.ReadInt32);
            il.EmitStloc(key);

            // is-sequential don't need else convert key to jump-table value
            if (!IsZeroStartSequential(infos))
            {
                Label endKeyMapGet = il.DefineLabel();
                il.EmitLdarg(0);
                il.EmitLdfld(keyToJumpMap);
                il.EmitLdloc(key);
                il.EmitLdloca(key);
                il.EmitCall(keyMapDictionaryTryGetValue);
                il.Emit(OpCodes.Brtrue_S, endKeyMapGet);
                il.EmitLdc_I4(-1);
                il.EmitStloc(key);

                il.MarkLabel(endKeyMapGet);
            }

            // switch->read
            LocalBuilder result = il.DeclareLocal(type);
            Label loopEnd = il.DefineLabel();
            il.Emit(OpCodes.Ldnull);
            il.EmitStloc(result);
            il.Emit(OpCodes.Ldloc, key);

            var switchLabels = infos.Select(x => new { Label = il.DefineLabel(), Attr = x }).ToArray();
            il.Emit(OpCodes.Switch, switchLabels.Select(x => x.Label).ToArray());

            // default
            reader.EmitLdarg();
            il.EmitCall(MessagePackReaderTypeInfo.Skip);
            il.Emit(OpCodes.Br, loopEnd);

            foreach (var item in switchLabels)
            {
                il.MarkLabel(item.Label);
                il.EmitLdloc(localResolver);
                il.EmitCall(getFormatterWithVerify.MakeGenericMethod(item.Attr.SubType));
                il.EmitLdarg(1);
                il.EmitLdarg(2);
                il.EmitCall(getDeserialize(item.Attr.SubType));
                if (item.Attr.SubType.GetTypeInfo().IsValueType)
                {
                    il.Emit(OpCodes.Box, item.Attr.SubType);
                }

                il.Emit(OpCodes.Stloc, result);
                il.Emit(OpCodes.Br, loopEnd);
            }

            il.MarkLabel(loopEnd);

            il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ret);
        }

        private static bool IsZeroStartSequential(UnionAttribute[] infos)
        {
            for (int i = 0; i < infos.Length; i++)
            {
                if (infos[i].Key != i)
                {
                    return false;
                }
            }

            return true;
        }

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter

        // EmitInfos...
        private static readonly Type refMessagePackReader = typeof(MessagePackReader).MakeByRefType();
        private static readonly Type refKvp = typeof(KeyValuePair<int, int>).MakeByRefType();
        private static readonly MethodInfo getFormatterWithVerify = typeof(FormatterResolverExtensions).GetRuntimeMethods().First(x => x.Name == "GetFormatterWithVerify");
        private static readonly MethodInfo getResolverFromOptions = typeof(MessagePackSerializerOptions).GetRuntimeProperty(nameof(MessagePackSerializerOptions.Resolver)).GetMethod;

        private static readonly Func<Type, MethodInfo> getSerialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod("Serialize", new[] { typeof(MessagePackWriter).MakeByRefType(), t, typeof(MessagePackSerializerOptions) });
        private static readonly Func<Type, MethodInfo> getDeserialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod("Deserialize", new[] { typeof(MessagePackReader).MakeByRefType(), typeof(MessagePackSerializerOptions) });

        private static readonly FieldInfo runtimeTypeHandleEqualityComparer = typeof(RuntimeTypeHandleEqualityComparer).GetRuntimeField("Default");
        private static readonly ConstructorInfo intIntKeyValuePairConstructor = typeof(KeyValuePair<int, int>).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 2);
        private static readonly ConstructorInfo typeMapDictionaryConstructor = typeof(Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>).GetTypeInfo().DeclaredConstructors.First(x =>
        {
            ParameterInfo[] p = x.GetParameters();
            return p.Length == 2 && p[0].ParameterType == typeof(int);
        });

        private static readonly MethodInfo typeMapDictionaryAdd = typeof(Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>).GetRuntimeMethod("Add", new[] { typeof(RuntimeTypeHandle), typeof(KeyValuePair<int, int>) });
        private static readonly MethodInfo typeMapDictionaryTryGetValue = typeof(Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>).GetRuntimeMethod("TryGetValue", new[] { typeof(RuntimeTypeHandle), refKvp });

        private static readonly ConstructorInfo keyMapDictionaryConstructor = typeof(Dictionary<int, int>).GetTypeInfo().DeclaredConstructors.First(x =>
        {
            ParameterInfo[] p = x.GetParameters();
            return p.Length == 1 && p[0].ParameterType == typeof(int);
        });

        private static readonly MethodInfo keyMapDictionaryAdd = typeof(Dictionary<int, int>).GetRuntimeMethod("Add", new[] { typeof(int), typeof(int) });
        private static readonly MethodInfo keyMapDictionaryTryGetValue = typeof(Dictionary<int, int>).GetRuntimeMethod("TryGetValue", new[] { typeof(int), typeof(int).MakeByRefType() });

        private static readonly MethodInfo objectGetType = typeof(object).GetRuntimeMethod("GetType", Type.EmptyTypes);
        private static readonly MethodInfo getTypeHandle = typeof(Type).GetRuntimeProperty("TypeHandle").GetGetMethod();

        private static readonly MethodInfo intIntKeyValuePairGetKey = typeof(KeyValuePair<int, int>).GetRuntimeProperty("Key").GetGetMethod();
        private static readonly MethodInfo intIntKeyValuePairGetValue = typeof(KeyValuePair<int, int>).GetRuntimeProperty("Value").GetGetMethod();

        private static readonly ConstructorInfo invalidOperationExceptionConstructor = typeof(System.InvalidOperationException).GetTypeInfo().DeclaredConstructors.First(
            x =>
            {
                ParameterInfo[] p = x.GetParameters();
                return p.Length == 1 && p[0].ParameterType == typeof(string);
            });

        private static readonly ConstructorInfo objectCtor = typeof(object).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 0);

#pragma warning restore SA1311 // Static readonly fields should begin with upper-case letter

        private static class MessagePackReaderTypeInfo
        {
            internal static readonly TypeInfo ReaderTypeInfo = typeof(MessagePackReader).GetTypeInfo();

            internal static readonly MethodInfo ReadBytes = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadBytes), Type.EmptyTypes);
            internal static readonly MethodInfo ReadInt32 = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadInt32), Type.EmptyTypes);
            internal static readonly MethodInfo ReadString = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadString), Type.EmptyTypes);
            internal static readonly MethodInfo TryReadNil = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.TryReadNil), Type.EmptyTypes);
            internal static readonly MethodInfo Skip = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.Skip), Type.EmptyTypes);
            internal static readonly MethodInfo ReadArrayHeader = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadArrayHeader), Type.EmptyTypes);
            internal static readonly MethodInfo ReadMapHeader = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadMapHeader), Type.EmptyTypes);
        }

        private static class MessagePackWriterTypeInfo
        {
            internal static readonly TypeInfo WriterTypeInfo = typeof(MessagePackWriter).GetTypeInfo();

            internal static readonly MethodInfo WriteArrayHeader = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.WriteArrayHeader), new[] { typeof(int) });
            internal static readonly MethodInfo WriteInt32 = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.Write), new[] { typeof(int) });
            internal static readonly MethodInfo WriteNil = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.WriteNil), Type.EmptyTypes);
        }
    }

    internal class MessagePackDynamicUnionResolverException : MessagePackSerializationException
    {
        public MessagePackDynamicUnionResolverException(string message)
            : base(message)
        {
        }
    }
}

#endif
