#if !UNITY_WSA
#if !NET_STANDARD_2_0

using System;
using System.Linq;
using MessagePack.Formatters;
using MessagePack.Internal;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// ObjectResolver by dynamic code generation.
    /// </summary>
    public sealed class DynamicObjectResolver : IFormatterResolver
    {
        public static readonly DynamicObjectResolver Instance = new DynamicObjectResolver();

        const string ModuleName = "MessagePack.Resolvers.DynamicObjectResolver";

        internal static readonly DynamicAssembly assembly;

        DynamicObjectResolver()
        {

        }

        static DynamicObjectResolver()
        {
            assembly = new DynamicAssembly(ModuleName);
        }

#if NET_35
        public AssemblyBuilder Save()
        {
            return assembly.Save();
        }
#endif

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                var ti = typeof(T).GetTypeInfo();

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
                    formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }

                if (ti.IsAnonymous())
                {
                    formatter = (IMessagePackFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod(typeof(T), true, true, false);
                    return;
                }

                var formatterTypeInfo = DynamicObjectTypeBuilder.BuildType(assembly, typeof(T), false, false);
                if (formatterTypeInfo == null) return;

                formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterTypeInfo.AsType());
            }
        }
    }

    /// <summary>
    /// ObjectResolver by dynamic code generation, allow private member.
    /// </summary>
    public sealed class DynamicObjectResolverAllowPrivate : IFormatterResolver
    {
        public static readonly DynamicObjectResolverAllowPrivate Instance = new DynamicObjectResolverAllowPrivate();

        DynamicObjectResolverAllowPrivate()
        {

        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                var ti = typeof(T).GetTypeInfo();

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
                    formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }

                if (ti.IsAnonymous())
                {
                    formatter = (IMessagePackFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod(typeof(T), true, true, false);
                }
                else
                {
                    formatter = (IMessagePackFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod(typeof(T), false, false, true);
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

        const string ModuleName = "MessagePack.Resolvers.DynamicContractlessObjectResolver";

        static readonly DynamicAssembly assembly;

        DynamicContractlessObjectResolver()
        {

        }

        static DynamicContractlessObjectResolver()
        {
            assembly = new DynamicAssembly(ModuleName);
        }

#if NET_35
        public AssemblyBuilder Save()
        {
            return assembly.Save();
        }
#endif

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                if (typeof(T) == typeof(object))
                {
                    return;
                }

                var ti = typeof(T).GetTypeInfo();

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
                    formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }

                if (ti.IsAnonymous())
                {
                    formatter = (IMessagePackFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod(typeof(T), true, true, false);
                    return;
                }

                var formatterTypeInfo = DynamicObjectTypeBuilder.BuildType(assembly, typeof(T), true, true);
                if (formatterTypeInfo == null) return;

                formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterTypeInfo.AsType());
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
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                if (typeof(T) == typeof(object))
                {
                    return;
                }

                var ti = typeof(T).GetTypeInfo();

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
                    formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }

                if (ti.IsAnonymous())
                {
                    formatter = (IMessagePackFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod(typeof(T), true, true, false);
                }
                else
                {
                    formatter = (IMessagePackFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod(typeof(T), true, true, true);
                }
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class DynamicObjectTypeBuilder
    {
#if NETSTANDARD
        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled);
#else
        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+");
#endif

        static int nameSequence = 0;

        static HashSet<Type> ignoreTypes = new HashSet<Type>
        {
            {typeof(object)},
            {typeof(short)},
            {typeof(int)},
            {typeof(long)},
            {typeof(ushort)},
            {typeof(uint)},
            {typeof(ulong)},
            {typeof(float)},
            {typeof(double)},
            {typeof(bool)},
            {typeof(byte)},
            {typeof(sbyte)},
            {typeof(decimal)},
            {typeof(char)},
            {typeof(string)},
            {typeof(System.Guid)},
            {typeof(System.TimeSpan)},
            {typeof(System.DateTime)},
            {typeof(System.DateTimeOffset)},
            {typeof(MessagePack.Nil)},
        };

        public static TypeInfo BuildType(DynamicAssembly assembly, Type type, bool forceStringKey, bool contractless)
        {
            if (ignoreTypes.Contains(type)) return null;

            var serializationInfo = MessagePack.Internal.ObjectSerializationInfo.CreateOrNull(type, forceStringKey, contractless, false);
            if (serializationInfo == null) return null;

            var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
            var typeBuilder = assembly.DefineType("MessagePack.Formatters." + SubtractFullNameRegex.Replace(type.FullName, "").Replace(".", "_") + "Formatter" + Interlocked.Increment(ref nameSequence), TypeAttributes.Public | TypeAttributes.Sealed, null, new[] { formatterType });

            FieldBuilder stringByteKeysField = null;
            Dictionary<ObjectSerializationInfo.EmittableMember, FieldInfo> customFormatterLookup = null;

            // string key needs string->int mapper for deserialize switch statement
            if (serializationInfo.IsStringKey)
            {
                var method = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                stringByteKeysField = typeBuilder.DefineField("stringByteKeys", typeof(byte[][]), FieldAttributes.Private | FieldAttributes.InitOnly);

                var il = method.GetILGenerator();
                BuildConstructor(type, serializationInfo, method, stringByteKeysField, il);
                customFormatterLookup = BuildCustomFormatterField(typeBuilder, serializationInfo, il);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                var method = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                var il = method.GetILGenerator();
                il.EmitLoadThis();
                il.Emit(OpCodes.Call, objectCtor);
                customFormatterLookup = BuildCustomFormatterField(typeBuilder, serializationInfo, il);
                il.Emit(OpCodes.Ret);
            }

            {
                var method = typeBuilder.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(int),
                    new Type[] { typeof(byte[]).MakeByRefType(), typeof(int), type, typeof(IFormatterResolver) });

                var il = method.GetILGenerator();
                BuildSerialize(type, serializationInfo, il, () =>
                {
                    il.EmitLoadThis();
                    il.EmitLdfld(stringByteKeysField);
                }, (index, member) =>
                {
                    FieldInfo fi;
                    if (!customFormatterLookup.TryGetValue(member, out fi)) return null;

                    return () =>
                    {
                        il.EmitLoadThis();
                        il.EmitLdfld(fi);
                    };
                }, 1);
            }

            {
                var method = typeBuilder.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    type,
                    new Type[] { typeof(byte[]), typeof(int), typeof(IFormatterResolver), typeof(int).MakeByRefType() });

                var il = method.GetILGenerator();
                BuildDeserialize(type, serializationInfo, il, (index, member) =>
                {
                    FieldInfo fi;
                    if (!customFormatterLookup.TryGetValue(member, out fi)) return null;

                    return () =>
                    {
                        il.EmitLoadThis();
                        il.EmitLdfld(fi);
                    };
                }, 1); // firstArgIndex:0 is this.
            }

            return typeBuilder.CreateTypeInfo();
        }

        public static object BuildFormatterToDynamicMethod(Type type, bool forceStringKey, bool contractless, bool allowPrivate)
        {
            var serializationInfo = ObjectSerializationInfo.CreateOrNull(type, forceStringKey, contractless, allowPrivate);
            if (serializationInfo == null) return null;

            // internal delegate int AnonymousSerializeFunc<T>(byte[][] stringByteKeysField, object[] customFormatters, ref byte[] bytes, int offset, T value, IFormatterResolver resolver);
            // internal delegate T AnonymousDeserializeFunc<T>(object[] customFormatters, byte[] bytes, int offset, IFormatterResolver resolver, out int readSize);
            var serialize = new DynamicMethod("Serialize", typeof(int), new[] { typeof(byte[][]), typeof(object[]), typeof(byte[]).MakeByRefType(), typeof(int), type, typeof(IFormatterResolver) }, type, true);
            DynamicMethod deserialize = null;

            List<byte[]> stringByteKeysField = new List<byte[]>();
            List<object> serializeCustomFormatters = new List<object>();
            List<object> deserializeCustomFormatters = new List<object>();

            if (serializationInfo.IsStringKey)
            {
                var i = 0;
                foreach (var item in serializationInfo.Members.Where(x => x.IsReadable))
                {
                    stringByteKeysField.Add(MessagePackBinary.GetEncodedStringBytes(item.StringKey));
                    i++;
                }
            }
            foreach (var item in serializationInfo.Members.Where(x => x.IsReadable))
            {
                var attr = item.GetMessagePackFormatterAttribtue();
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
            foreach (var item in serializationInfo.Members) // not only for writable because for use ctor.
            {
                var attr = item.GetMessagePackFormatterAttribtue();
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
                var il = serialize.GetILGenerator();
                BuildSerialize(type, serializationInfo, il, () =>
                {
                    il.EmitLdarg(0);
                }, (index, member) =>
                {
                    if (serializeCustomFormatters.Count == 0) return null;
                    if (serializeCustomFormatters[index] == null) return null;

                    return () =>
                    {
                        il.EmitLdarg(1); // read object[]
                        il.EmitLdc_I4(index);
                        il.Emit(OpCodes.Ldelem_Ref); // object
                        il.Emit(OpCodes.Castclass, serializeCustomFormatters[index].GetType());
                    };
                }, 2);  // 0, 1 is parameter.
            }

            if (serializationInfo.IsStruct || serializationInfo.BestmatchConstructor != null)
            {
                deserialize = new DynamicMethod("Deserialize", type, new[] { typeof(object[]), typeof(byte[]), typeof(int), typeof(IFormatterResolver), typeof(int).MakeByRefType() }, type, true);

                var il = deserialize.GetILGenerator();
                BuildDeserialize(type, serializationInfo, il, (index, member) =>
                {
                    if (deserializeCustomFormatters.Count == 0) return null;
                    if (deserializeCustomFormatters[index] == null) return null;

                    return () =>
                    {
                        il.EmitLdarg(0); // read object[]
                        il.EmitLdc_I4(index);
                        il.Emit(OpCodes.Ldelem_Ref); // object
                        il.Emit(OpCodes.Castclass, deserializeCustomFormatters[index].GetType());
                    };
                }, 1);
            }

            object serializeDelegate = serialize.CreateDelegate(typeof(AnonymousSerializeFunc<>).MakeGenericType(type));
            object deserializeDelegate = (deserialize == null)
                ? (object)null
                : (object)deserialize.CreateDelegate(typeof(AnonymousDeserializeFunc<>).MakeGenericType(type));
            var resultFormatter = Activator.CreateInstance(typeof(AnonymousSerializableFormatter<>).MakeGenericType(type),
                new[] { stringByteKeysField.ToArray(), serializeCustomFormatters.ToArray(), deserializeCustomFormatters.ToArray(), serializeDelegate, deserializeDelegate });
            return resultFormatter;
        }

        static void BuildConstructor(Type type, ObjectSerializationInfo info, ConstructorInfo method, FieldBuilder stringByteKeysField, ILGenerator il)
        {
            il.EmitLoadThis();
            il.Emit(OpCodes.Call, objectCtor);

            var writeCount = info.Members.Count(x => x.IsReadable);
            il.EmitLoadThis();
            il.EmitLdc_I4(writeCount);
            il.Emit(OpCodes.Newarr, typeof(byte[]));

            var i = 0;
            foreach (var item in info.Members.Where(x => x.IsReadable))
            {
                il.Emit(OpCodes.Dup);
                il.EmitLdc_I4(i);
                il.Emit(OpCodes.Ldstr, item.StringKey);
                il.EmitCall(MessagePackBinaryTypeInfo.GetEncodedStringBytes);
                il.Emit(OpCodes.Stelem_Ref);
                i++;
            }

            il.Emit(OpCodes.Stfld, stringByteKeysField);
        }

        static Dictionary<ObjectSerializationInfo.EmittableMember, FieldInfo> BuildCustomFormatterField(TypeBuilder builder, ObjectSerializationInfo info, ILGenerator il)
        {
            Dictionary<ObjectSerializationInfo.EmittableMember, FieldInfo> dict = new Dictionary<ObjectSerializationInfo.EmittableMember, FieldInfo>();
            foreach (var item in info.Members.Where(x => x.IsReadable || x.IsWritable))
            {
                var attr = item.GetMessagePackFormatterAttribtue();
                if (attr != null)
                {
                    var f = builder.DefineField(item.Name + "_formatter", attr.FormatterType, FieldAttributes.Private | FieldAttributes.InitOnly);

                    var bindingFlags = (int)(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    var attrVar = il.DeclareLocal(typeof(MessagePackFormatterAttribute));

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

        // int Serialize([arg:1]ref byte[] bytes, [arg:2]int offset, [arg:3]T value, [arg:4]IFormatterResolver formatterResolver);
        static void BuildSerialize(Type type, ObjectSerializationInfo info, ILGenerator il, Action emitStringByteKeys, Func<int, ObjectSerializationInfo.EmittableMember, Action> tryEmitLoadCustomFormatter, int firstArgIndex)
        {
            var argBytes = new ArgumentField(il, firstArgIndex);
            var argOffset = new ArgumentField(il, firstArgIndex + 1);
            var argValue = new ArgumentField(il, firstArgIndex + 2, type);
            var argResolver = new ArgumentField(il, firstArgIndex + 3);

            // if(value == null) return WriteNil
            if (type.GetTypeInfo().IsClass)
            {
                var elseBody = il.DefineLabel();

                argValue.EmitLoad();
                il.Emit(OpCodes.Brtrue_S, elseBody);
                argBytes.EmitLoad();
                argOffset.EmitLoad();
                il.EmitCall(MessagePackBinaryTypeInfo.WriteNil);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(elseBody);
            }

            // IMessagePackSerializationCallbackReceiver.OnBeforeSerialize()
            if (type.GetTypeInfo().ImplementedInterfaces.Any(x => x == typeof(IMessagePackSerializationCallbackReceiver)))
            {
                // call directly
                var runtimeMethods = type.GetRuntimeMethods().Where(x => x.Name == "OnBeforeSerialize").ToArray();
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

            // var startOffset = offset;
            var startOffsetLocal = il.DeclareLocal(typeof(int)); // [loc:0]
            argOffset.EmitLoad();
            il.EmitStloc(startOffsetLocal);

            if (info.IsIntKey)
            {
                // use Array
                var maxKey = info.Members.Where(x => x.IsReadable).Select(x => x.IntKey).DefaultIfEmpty(-1).Max();
                var intKeyMap = info.Members.Where(x => x.IsReadable).ToDictionary(x => x.IntKey);

                EmitOffsetPlusEqual(il, null, () =>
                {
                    var len = maxKey + 1;
                    il.EmitLdc_I4(len);
                    if (len <= MessagePackRange.MaxFixArrayCount)
                    {
                        il.EmitCall(MessagePackBinaryTypeInfo.WriteFixedArrayHeaderUnsafe);
                    }
                    else
                    {
                        il.EmitCall(MessagePackBinaryTypeInfo.WriteArrayHeader);
                    }
                }, argBytes, argOffset);

                for (int i = 0; i <= maxKey; i++)
                {
                    ObjectSerializationInfo.EmittableMember member;
                    if (intKeyMap.TryGetValue(i, out member))
                    {
                        // offset += serialzie
                        EmitSerializeValue(il, type.GetTypeInfo(), member, i, tryEmitLoadCustomFormatter, argBytes, argOffset, argValue, argResolver);
                    }
                    else
                    {
                        // Write Nil as Blanc
                        EmitOffsetPlusEqual(il, null, () =>
                        {
                            il.EmitCall(MessagePackBinaryTypeInfo.WriteNil);
                        }, argBytes, argOffset);
                    }
                }
            }
            else
            {
                // use Map
                var writeCount = info.Members.Count(x => x.IsReadable);

                EmitOffsetPlusEqual(il, null, () =>
                {
                    il.EmitLdc_I4(writeCount);
                    if (writeCount <= MessagePackRange.MaxFixMapCount)
                    {
                        il.EmitCall(MessagePackBinaryTypeInfo.WriteFixedMapHeaderUnsafe);
                    }
                    else
                    {
                        il.EmitCall(MessagePackBinaryTypeInfo.WriteMapHeader);
                    }
                }, argBytes, argOffset);

                var index = 0;
                foreach (var item in info.Members.Where(x => x.IsReadable))
                {
                    // offset += writekey
                    EmitOffsetPlusEqual(il, null, () =>
                    {
                        emitStringByteKeys();
                        il.EmitLdc_I4(index);
                        il.Emit(OpCodes.Ldelem_Ref);

                        // Optimize, WriteRaw(Unity, large) or UnsafeMemory32/64.WriteRawX
#if NETSTANDARD
                        var valueLen = MessagePackBinary.GetEncodedStringBytes(item.StringKey).Length;
                        if (valueLen <= MessagePackRange.MaxFixStringLength)
                        {
                            if (UnsafeMemory.Is32Bit)
                            {
                                il.EmitCall(typeof(UnsafeMemory32).GetRuntimeMethod("WriteRaw" + valueLen, new[] { refByte, typeof(int), typeof(byte[]) }));
                            }
                            else
                            {
                                il.EmitCall(typeof(UnsafeMemory64).GetRuntimeMethod("WriteRaw" + valueLen, new[] { refByte, typeof(int), typeof(byte[]) }));
                            }
                        }
                        else
#endif
                        {
                            il.EmitCall(MessagePackBinaryTypeInfo.WriteRaw);
                        }
                    }, argBytes, argOffset);

                    // offset += serialzie
                    EmitSerializeValue(il, type.GetTypeInfo(), item, index, tryEmitLoadCustomFormatter, argBytes, argOffset, argValue, argResolver);
                    index++;
                }
            }

            // return startOffset- offset;
            argOffset.EmitLoad();
            il.EmitLdloc(startOffsetLocal);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Ret);
        }

        // offset += ***(ref bytes, offset....
        static void EmitOffsetPlusEqual(ILGenerator il, Action loadEmit, Action emit, ArgumentField argBytes, ArgumentField argOffset)
        {
            argOffset.EmitLoad();

            if (loadEmit != null) loadEmit();

            argBytes.EmitLoad();
            argOffset.EmitLoad();

            emit();

            il.Emit(OpCodes.Add);
            argOffset.EmitStore();
        }

        static void EmitSerializeValue(ILGenerator il, TypeInfo type, ObjectSerializationInfo.EmittableMember member, int index, Func<int, ObjectSerializationInfo.EmittableMember, Action> tryEmitLoadCustomFormatter, ArgumentField argBytes, ArgumentField argOffset, ArgumentField argValue, ArgumentField argResolver)
        {
            var t = member.Type;
            var emitter = tryEmitLoadCustomFormatter(index, member);
            if (emitter != null)
            {
                EmitOffsetPlusEqual(il, () =>
                {
                    emitter();
                }, () =>
                {
                    argValue.EmitLoad();
                    member.EmitLoadValue(il);
                    argResolver.EmitLoad();
                    il.EmitCall(typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod("Serialize", new[] { refByte, typeof(int), t, typeof(IFormatterResolver) }));
                }, argBytes, argOffset);
            }
            else if (IsOptimizeTargetType(t))
            {
                EmitOffsetPlusEqual(il, null, () =>
                {
                    argValue.EmitLoad();
                    member.EmitLoadValue(il);
                    if (t == typeof(byte[]))
                    {
                        il.EmitCall(MessagePackBinaryTypeInfo.WriteBytes);
                    }
                    else
                    {
                        il.EmitCall(MessagePackBinaryTypeInfo.TypeInfo.GetDeclaredMethods("Write" + t.Name).OrderByDescending(x => x.GetParameters().Length).First());
                    }
                }, argBytes, argOffset);
            }
            else
            {
                EmitOffsetPlusEqual(il, () =>
                {
                    argResolver.EmitLoad();
                    il.Emit(OpCodes.Call, getFormatterWithVerify.MakeGenericMethod(t));
                }, () =>
                {
                    argValue.EmitLoad();
                    member.EmitLoadValue(il);
                    argResolver.EmitLoad();
                    il.EmitCall(getSerialize(t));
                }, argBytes, argOffset);
            }
        }

        // T Deserialize([arg:1]byte[] bytes, [arg:2]int offset, [arg:3]IFormatterResolver formatterResolver, [arg:4]out int readSize);
        static void BuildDeserialize(Type type, ObjectSerializationInfo info, ILGenerator il, Func<int, ObjectSerializationInfo.EmittableMember, Action> tryEmitLoadCustomFormatter, int firstArgIndex)
        {
            var argBytes = new ArgumentField(il, firstArgIndex);
            var argOffset = new ArgumentField(il, firstArgIndex + 1);
            var argResolver = new ArgumentField(il, firstArgIndex + 2);
            var argReadSize = new ArgumentField(il, firstArgIndex + 3);

            // if(MessagePackBinary.IsNil) readSize = 1, return null;
            var falseLabel = il.DefineLabel();
            argBytes.EmitLoad();
            argOffset.EmitLoad();
            il.EmitCall(MessagePackBinaryTypeInfo.IsNil);
            il.Emit(OpCodes.Brfalse_S, falseLabel);
            if (type.GetTypeInfo().IsClass)
            {
                argReadSize.EmitLoad();
                il.EmitLdc_I4(1);
                il.Emit(OpCodes.Stind_I4);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                il.Emit(OpCodes.Ldstr, "typecode is null, struct not supported");
                il.Emit(OpCodes.Newobj, invalidOperationExceptionConstructor);
                il.Emit(OpCodes.Throw);
            }

            // var startOffset = offset;
            il.MarkLabel(falseLabel);
            var startOffsetLocal = il.DeclareLocal(typeof(int)); // [loc:0]
            argOffset.EmitLoad();
            il.EmitStloc(startOffsetLocal);

            // var length = ReadMapHeader
            var length = il.DeclareLocal(typeof(int)); // [loc:1]
            argBytes.EmitLoad();
            argOffset.EmitLoad();
            argReadSize.EmitLoad();

            if (info.IsIntKey)
            {
                il.EmitCall(MessagePackBinaryTypeInfo.ReadArrayHeader);
            }
            else
            {
                il.EmitCall(MessagePackBinaryTypeInfo.ReadMapHeader);
            }
            il.EmitStloc(length);
            EmitOffsetPlusReadSize(il, argOffset, argReadSize);

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
                                SwitchLabel = il.DefineLabel()
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
                        // SwitchLabel = il.DefineLabel()
                    })
                    .ToArray();
            }

            // Read Loop(for var i = 0; i< length; i++)
            if (info.IsStringKey)
            {
                var automata = new AutomataDictionary();
                for (int i = 0; i < info.Members.Length; i++)
                {
                    automata.Add(info.Members[i].StringKey, i);
                }

                var buffer = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
                var keyArraySegment = il.DeclareLocal(typeof(ArraySegment<byte>));
                var longKey = il.DeclareLocal(typeof(ulong));
                var p = il.DeclareLocal(typeof(byte*));
                var rest = il.DeclareLocal(typeof(int));

                // fixed (byte* buffer = &bytes[0]) {
                argBytes.EmitLoad();
                il.EmitLdc_I4(0);
                il.Emit(OpCodes.Ldelema, typeof(byte));
                il.EmitStloc(buffer);

                // for (int i = 0; i < len; i++)
                il.EmitIncrementFor(length, forILocal =>
                {
                    var readNext = il.DefineLabel();
                    var loopEnd = il.DefineLabel();

                    argBytes.EmitLoad();
                    argOffset.EmitLoad();
                    argReadSize.EmitLoad();
                    il.EmitCall(MessagePackBinaryTypeInfo.ReadStringSegment);
                    il.EmitStloc(keyArraySegment);
                    EmitOffsetPlusReadSize(il, argOffset, argReadSize);

                    // p = buffer + arraySegment.Offset
                    il.EmitLdloc(buffer);
                    il.Emit(OpCodes.Conv_I);
                    il.EmitLdloca(keyArraySegment);
                    il.EmitCall(typeof(ArraySegment<byte>).GetRuntimeProperty("Offset").GetGetMethod());
                    il.Emit(OpCodes.Add);
                    il.EmitStloc(p);

                    // rest = arraySegment.Count
                    il.EmitLdloca(keyArraySegment);
                    il.EmitCall(typeof(ArraySegment<byte>).GetRuntimeProperty("Count").GetGetMethod());
                    il.EmitStloc(rest);

                    // if(rest == 0) goto End
                    il.EmitLdloc(rest);
                    il.Emit(OpCodes.Brfalse, readNext);

                    // gen automata name lookup
                    automata.EmitMatch(il, p, rest, longKey, x =>
                    {
                        var i = x.Value;
                        if (infoList[i].MemberInfo != null)
                        {
                            EmitDeserializeValue(il, infoList[i], i, tryEmitLoadCustomFormatter, argBytes, argOffset, argResolver, argReadSize);
                            il.Emit(OpCodes.Br, loopEnd);
                        }
                        else
                        {
                            il.Emit(OpCodes.Br, readNext);
                        }
                    }, () =>
                    {
                        il.Emit(OpCodes.Br, readNext);
                    });

                    il.MarkLabel(readNext);
                    argReadSize.EmitLoad();
                    argBytes.EmitLoad();
                    argOffset.EmitLoad();
                    il.EmitCall(MessagePackBinaryTypeInfo.ReadNextBlock);
                    il.Emit(OpCodes.Stind_I4);

                    il.MarkLabel(loopEnd);
                    EmitOffsetPlusReadSize(il, argOffset, argReadSize);
                });

                // end fixed
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Conv_U);
                il.EmitStloc(buffer);
            }
            else
            {
                var key = il.DeclareLocal(typeof(int));
                var switchDefault = il.DefineLabel();

                il.EmitIncrementFor(length, forILocal =>
                {
                    var loopEnd = il.DefineLabel();

                    il.EmitLdloc(forILocal);
                    il.EmitStloc(key);

                    // switch... local = Deserialize
                    il.EmitLdloc(key);

                    il.Emit(OpCodes.Switch, infoList.Select(x => x.SwitchLabel).ToArray());

                    il.MarkLabel(switchDefault);
                    // default, only read. readSize = MessagePackBinary.ReadNextBlock(bytes, offset);
                    argReadSize.EmitLoad();
                    argBytes.EmitLoad();
                    argOffset.EmitLoad();
                    il.EmitCall(MessagePackBinaryTypeInfo.ReadNextBlock);
                    il.Emit(OpCodes.Stind_I4);
                    il.Emit(OpCodes.Br, loopEnd);

                    if (gotoDefault != null)
                    {
                        il.MarkLabel(gotoDefault.Value);
                        il.Emit(OpCodes.Br, switchDefault);
                    }

                    var i = 0;
                    foreach (var item in infoList)
                    {
                        if (item.MemberInfo != null)
                        {
                            il.MarkLabel(item.SwitchLabel);
                            EmitDeserializeValue(il, item, i++, tryEmitLoadCustomFormatter, argBytes, argOffset, argResolver, argReadSize);
                            il.Emit(OpCodes.Br, loopEnd);
                        }
                    }

                    // offset += readSize
                    il.MarkLabel(loopEnd);
                    EmitOffsetPlusReadSize(il, argOffset, argReadSize);
                });
            }

            // finish readSize: readSize = offset - startOffset;
            argReadSize.EmitLoad();
            argOffset.EmitLoad();
            il.EmitLdloc(startOffsetLocal);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Stind_I4);

            // create result object
            var structLocal = EmitNewObject(il, type, info, infoList);

            // IMessagePackSerializationCallbackReceiver.OnAfterDeserialize()
            if (type.GetTypeInfo().ImplementedInterfaces.Any(x => x == typeof(IMessagePackSerializationCallbackReceiver)))
            {
                // call directly
                var runtimeMethods = type.GetRuntimeMethods().Where(x => x.Name == "OnAfterDeserialize").ToArray();
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

            if (info.IsStruct)
            {
                il.Emit(OpCodes.Ldloc, structLocal);
            }


            il.Emit(OpCodes.Ret);
        }

        static void EmitOffsetPlusReadSize(ILGenerator il, ArgumentField argOffset, ArgumentField argReadSize)
        {
            argOffset.EmitLoad();
            argReadSize.EmitLoad();
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Add);
            argOffset.EmitStore();
        }

        static void EmitDeserializeValue(ILGenerator il, DeserializeInfo info, int index, Func<int, ObjectSerializationInfo.EmittableMember, Action> tryEmitLoadCustomFormatter, ArgumentField argBytes, ArgumentField argOffset, ArgumentField argResolver, ArgumentField argReadSize)
        {
            var member = info.MemberInfo;
            var t = member.Type;
            var emitter = tryEmitLoadCustomFormatter(index, member);
            if (emitter != null)
            {
                emitter();
                argBytes.EmitLoad();
                argOffset.EmitLoad();
                argResolver.EmitLoad();
                argReadSize.EmitLoad();
                il.EmitCall(typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod("Deserialize", new[] { typeof(byte[]), typeof(int), typeof(IFormatterResolver), refInt }));
            }
            else if (IsOptimizeTargetType(t))
            {
                il.EmitLdarg(1);
                il.EmitLdarg(2);
                il.EmitLdarg(4);
                if (t == typeof(byte[]))
                {
                    il.EmitCall(MessagePackBinaryTypeInfo.ReadBytes);
                }
                else
                {
                    il.EmitCall(MessagePackBinaryTypeInfo.TypeInfo.GetDeclaredMethods("Read" + t.Name).OrderByDescending(x => x.GetParameters().Length).First());
                }
            }
            else
            {
                argResolver.EmitLoad();
                il.EmitCall(getFormatterWithVerify.MakeGenericMethod(t));
                argBytes.EmitLoad();
                argOffset.EmitLoad();
                argResolver.EmitLoad();
                argReadSize.EmitLoad();
                il.EmitCall(getDeserialize(t));
            }

            il.EmitStloc(info.LocalField);
        }

        static LocalBuilder EmitNewObject(ILGenerator il, Type type, ObjectSerializationInfo info, DeserializeInfo[] members)
        {
            if (info.IsClass)
            {
                foreach (var item in info.ConstructorParameters)
                {
                    var local = members.First(x => x.MemberInfo == item);
                    il.EmitLdloc(local.LocalField);
                }
                il.Emit(OpCodes.Newobj, info.BestmatchConstructor);

                foreach (var item in members.Where(x => x.MemberInfo != null && x.MemberInfo.IsWritable))
                {
                    il.Emit(OpCodes.Dup);
                    il.EmitLdloc(item.LocalField);
                    item.MemberInfo.EmitStoreValue(il);
                }

                return null;
            }
            else
            {
                var result = il.DeclareLocal(type);
                if (info.BestmatchConstructor == null)
                {
                    il.Emit(OpCodes.Ldloca, result);
                    il.Emit(OpCodes.Initobj, type);
                }
                else
                {
                    foreach (var item in info.ConstructorParameters)
                    {
                        var local = members.First(x => x.MemberInfo == item);
                        il.EmitLdloc(local.LocalField);
                    }
                    il.Emit(OpCodes.Newobj, info.BestmatchConstructor);
                    il.Emit(OpCodes.Stloc, result);
                }

                foreach (var item in members.Where(x => x.MemberInfo != null && x.MemberInfo.IsWritable))
                {
                    il.EmitLdloca(result);
                    il.EmitLdloc(item.LocalField);
                    item.MemberInfo.EmitStoreValue(il);
                }

                return result; // struct returns local result field
            }
        }

        static bool IsOptimizeTargetType(Type type)
        {
            if (type == typeof(Int16)
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
             // not includes DateTime and String and Binary.
             //|| type == typeof(DateTime)
             //|| type == typeof(string)
             //|| type == typeof(byte[])
             )
            {
                return true;
            }
            return false;
        }

        // EmitInfos...

        static readonly Type refByte = typeof(byte[]).MakeByRefType();
        static readonly Type refInt = typeof(int).MakeByRefType();
        static readonly MethodInfo getFormatterWithVerify = typeof(FormatterResolverExtensions).GetRuntimeMethods().First(x => x.Name == "GetFormatterWithVerify");
        static readonly Func<Type, MethodInfo> getSerialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod("Serialize", new[] { refByte, typeof(int), t, typeof(IFormatterResolver) });
        static readonly Func<Type, MethodInfo> getDeserialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod("Deserialize", new[] { typeof(byte[]), typeof(int), typeof(IFormatterResolver), refInt });
        // static readonly ConstructorInfo dictionaryConstructor = typeof(ByteArrayStringHashTable).GetTypeInfo().DeclaredConstructors.First(x => { var p = x.GetParameters(); return p.Length == 1 && p[0].ParameterType == typeof(int); });
        // static readonly MethodInfo dictionaryAdd = typeof(ByteArrayStringHashTable).GetRuntimeMethod("Add", new[] { typeof(string), typeof(int) });
        // static readonly MethodInfo dictionaryTryGetValue = typeof(ByteArrayStringHashTable).GetRuntimeMethod("TryGetValue", new[] { typeof(ArraySegment<byte>), refInt });
        static readonly ConstructorInfo invalidOperationExceptionConstructor = typeof(System.InvalidOperationException).GetTypeInfo().DeclaredConstructors.First(x => { var p = x.GetParameters(); return p.Length == 1 && p[0].ParameterType == typeof(string); });

        static readonly MethodInfo onBeforeSerialize = typeof(IMessagePackSerializationCallbackReceiver).GetRuntimeMethod("OnBeforeSerialize", Type.EmptyTypes);
        static readonly MethodInfo onAfterDeserialize = typeof(IMessagePackSerializationCallbackReceiver).GetRuntimeMethod("OnAfterDeserialize", Type.EmptyTypes);

        static readonly ConstructorInfo objectCtor = typeof(object).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 0);

        internal static class MessagePackBinaryTypeInfo
        {
            public static TypeInfo TypeInfo = typeof(MessagePackBinary).GetTypeInfo();

            public static readonly MethodInfo GetEncodedStringBytes = typeof(MessagePackBinary).GetRuntimeMethod("GetEncodedStringBytes", new[] { typeof(string) });
            public static MethodInfo WriteFixedMapHeaderUnsafe = typeof(MessagePackBinary).GetRuntimeMethod("WriteFixedMapHeaderUnsafe", new[] { refByte,
typeof(int), typeof(int) });
            public static MethodInfo WriteFixedArrayHeaderUnsafe = typeof(MessagePackBinary).GetRuntimeMethod("WriteFixedArrayHeaderUnsafe", new[] { refByte, typeof(int), typeof(int) });
            public static MethodInfo WriteMapHeader = typeof(MessagePackBinary).GetRuntimeMethod("WriteMapHeader", new[] { refByte, typeof(int), typeof(int) });
            public static MethodInfo WriteArrayHeader = typeof(MessagePackBinary).GetRuntimeMethod("WriteArrayHeader", new[] { refByte, typeof(int), typeof(int) });
            public static MethodInfo WritePositiveFixedIntUnsafe = typeof(MessagePackBinary).GetRuntimeMethod("WritePositiveFixedIntUnsafe", new[] { refByte, typeof(int), typeof(int) });
            public static MethodInfo WriteInt32 = typeof(MessagePackBinary).GetRuntimeMethod("WriteInt32", new[] { refByte, typeof(int), typeof(int) });
            public static MethodInfo WriteBytes = typeof(MessagePackBinary).GetRuntimeMethod("WriteBytes", new[] { refByte, typeof(int), typeof(byte[]) });
            public static MethodInfo WriteNil = typeof(MessagePackBinary).GetRuntimeMethod("WriteNil", new[] { refByte, typeof(int) });
            public static MethodInfo ReadBytes = typeof(MessagePackBinary).GetRuntimeMethod("ReadBytes", new[] { typeof(byte[]), typeof(int), refInt });
            public static MethodInfo ReadInt32 = typeof(MessagePackBinary).GetRuntimeMethod("ReadInt32", new[] { typeof(byte[]), typeof(int), refInt });
            public static MethodInfo ReadString = typeof(MessagePackBinary).GetRuntimeMethod("ReadString", new[] { typeof(byte[]), typeof(int), refInt });
            public static MethodInfo ReadStringSegment = typeof(MessagePackBinary).GetRuntimeMethod("ReadStringSegment", new[] { typeof(byte[]), typeof(int), refInt });
            public static MethodInfo IsNil = typeof(MessagePackBinary).GetRuntimeMethod("IsNil", new[] { typeof(byte[]), typeof(int) });
            public static MethodInfo ReadNextBlock = typeof(MessagePackBinary).GetRuntimeMethod("ReadNextBlock", new[] { typeof(byte[]), typeof(int) });
            public static MethodInfo WriteStringUnsafe = typeof(MessagePackBinary).GetRuntimeMethod("WriteStringUnsafe", new[] { refByte, typeof(int), typeof(string), typeof(int) });
            public static MethodInfo WriteStringBytes = typeof(MessagePackBinary).GetRuntimeMethod("WriteStringBytes", new[] { refByte, typeof(int), typeof(byte[]) });
            public static MethodInfo WriteRaw = typeof(MessagePackBinary).GetRuntimeMethod("WriteRaw", new[] { refByte, typeof(int), typeof(byte[]) });

            public static MethodInfo ReadArrayHeader = typeof(MessagePackBinary).GetRuntimeMethod("ReadArrayHeader", new[] { typeof(byte[]), typeof(int), refInt });
            public static MethodInfo ReadMapHeader = typeof(MessagePackBinary).GetRuntimeMethod("ReadMapHeader", new[] { typeof(byte[]), typeof(int), refInt });

            static MessagePackBinaryTypeInfo()
            {
            }
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

        class DeserializeInfo
        {
            public ObjectSerializationInfo.EmittableMember MemberInfo { get; set; }
            public LocalBuilder LocalField { get; set; }
            public Label SwitchLabel { get; set; }
        }
    }

    internal delegate int AnonymousSerializeFunc<T>(byte[][] stringByteKeysField, object[] customFormatters, ref byte[] bytes, int offset, T value, IFormatterResolver resolver);
    internal delegate T AnonymousDeserializeFunc<T>(object[] customFormatters, byte[] bytes, int offset, IFormatterResolver resolver, out int readSize);

    internal class AnonymousSerializableFormatter<T> : IMessagePackFormatter<T>
    {
        readonly byte[][] stringByteKeysField;
        readonly object[] serializeCustomFormatters;
        readonly object[] deserializeCustomFormatters;
        readonly AnonymousSerializeFunc<T> serialize;
        readonly AnonymousDeserializeFunc<T> deserialize;

        public AnonymousSerializableFormatter(byte[][] stringByteKeysField, object[] serializeCustomFormatters, object[] deserializeCustomFormatters, AnonymousSerializeFunc<T> serialize, AnonymousDeserializeFunc<T> deserialize)
        {
            this.stringByteKeysField = stringByteKeysField;
            this.serializeCustomFormatters = serializeCustomFormatters;
            this.deserializeCustomFormatters = deserializeCustomFormatters;
            this.serialize = serialize;
            this.deserialize = deserialize;
        }

        public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
        {
            if (serialize == null) throw new InvalidOperationException(this.GetType().Name + " does not support Serialize.");
            return serialize(stringByteKeysField, serializeCustomFormatters, ref bytes, offset, value, formatterResolver);
        }

        public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (deserialize == null) throw new InvalidOperationException(this.GetType().Name + " does not support Deserialize.");
            return deserialize(deserializeCustomFormatters, bytes, offset, formatterResolver, out readSize);
        }
    }

    internal class ObjectSerializationInfo
    {
        public Type Type { get; set; }
        public bool IsIntKey { get; set; }
        public bool IsStringKey { get { return !IsIntKey; } }
        public bool IsClass { get; set; }
        public bool IsStruct { get { return !IsClass; } }
        public ConstructorInfo BestmatchConstructor { get; set; }
        public EmittableMember[] ConstructorParameters { get; set; }
        public EmittableMember[] Members { get; set; }

        ObjectSerializationInfo()
        {

        }

        public static ObjectSerializationInfo CreateOrNull(Type type, bool forceStringKey, bool contractless, bool allowPrivate)
        {
            var ti = type.GetTypeInfo();
            var isClass = ti.IsClass || ti.IsInterface || ti.IsAbstract;

            var contractAttr = ti.GetCustomAttribute<MessagePackObjectAttribute>();
            var dataContractAttr = ti.GetCustomAttribute<DataContractAttribute>();
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
                foreach (var item in type.GetRuntimeProperties())
                {
                    if (item.GetCustomAttribute<IgnoreMemberAttribute>(true) != null) continue;
                    if (item.GetCustomAttribute<IgnoreDataMemberAttribute>(true) != null) continue;
                    if (item.IsIndexer()) continue;

                    var getMethod = item.GetGetMethod(true);
                    var setMethod = item.GetSetMethod(true);

                    var member = new EmittableMember
                    {
                        PropertyInfo = item,
                        IsReadable = (getMethod != null) && (allowPrivate || getMethod.IsPublic) && !getMethod.IsStatic,
                        IsWritable = (setMethod != null) && (allowPrivate || setMethod.IsPublic) && !setMethod.IsStatic,
                        StringKey = item.Name
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;
                    member.IntKey = hiddenIntKey++;
                    if (isIntKey)
                    {
                        intMembers.Add(member.IntKey, member);
                    }
                    else
                    {
                        stringMembers.Add(member.StringKey, member);
                    }
                }
                foreach (var item in type.GetRuntimeFields())
                {
                    if (item.GetCustomAttribute<IgnoreMemberAttribute>(true) != null) continue;
                    if (item.GetCustomAttribute<IgnoreDataMemberAttribute>(true) != null) continue;
                    if (item.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>(true) != null) continue;
                    if (item.IsStatic) continue;

                    var member = new EmittableMember
                    {
                        FieldInfo = item,
                        IsReadable = allowPrivate || item.IsPublic,
                        IsWritable = allowPrivate || (item.IsPublic && !item.IsInitOnly),
                        StringKey = item.Name
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;
                    member.IntKey = hiddenIntKey++;
                    if (isIntKey)
                    {
                        intMembers.Add(member.IntKey, member);
                    }
                    else
                    {
                        stringMembers.Add(member.StringKey, member);
                    }
                }
            }
            else
            {
                // Public members with KeyAttribute except [Ignore] member.
                var searchFirst = true;
                var hiddenIntKey = 0;

                foreach (var item in type.GetRuntimeProperties())
                {
                    if (item.GetCustomAttribute<IgnoreMemberAttribute>(true) != null) continue;
                    if (item.GetCustomAttribute<IgnoreDataMemberAttribute>(true) != null) continue;
                    if (item.IsIndexer()) continue;

                    var getMethod = item.GetGetMethod(true);
                    var setMethod = item.GetSetMethod(true);

                    var member = new EmittableMember
                    {
                        PropertyInfo = item,
                        IsReadable = (getMethod != null) && (allowPrivate || getMethod.IsPublic) && !getMethod.IsStatic,
                        IsWritable = (setMethod != null) && (allowPrivate || setMethod.IsPublic) && !setMethod.IsStatic,
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;

                    KeyAttribute key;
                    if (contractAttr != null)
                    {
                        // MessagePackObjectAttribute
                        key = item.GetCustomAttribute<KeyAttribute>(true);
                        if (key == null)
                        {
                            throw new MessagePackDynamicObjectResolverException("all public members must mark KeyAttribute or IgnoreMemberAttribute." + " type: " + type.FullName + " member:" + item.Name);
                        }

                        if (key.IntKey == null && key.StringKey == null) throw new MessagePackDynamicObjectResolverException("both IntKey and StringKey are null." + " type: " + type.FullName + " member:" + item.Name);
                    }
                    else
                    {
                        // DataContractAttribute
                        var pseudokey = item.GetCustomAttribute<DataMemberAttribute>(true);
                        if (pseudokey == null)
                        {
                            throw new MessagePackDynamicObjectResolverException("all public members must mark DataMemberAttribute or IgnoreMemberAttribute." + " type: " + type.FullName + " member:" + item.Name);
                        }

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
                        if (intMembers.ContainsKey(member.IntKey)) throw new MessagePackDynamicObjectResolverException("key is duplicated, all members key must be unique." + " type: " + type.FullName + " member:" + item.Name);

                        intMembers.Add(member.IntKey, member);
                    }
                    else
                    {
                        member.StringKey = key.StringKey;
                        if (stringMembers.ContainsKey(member.StringKey)) throw new MessagePackDynamicObjectResolverException("key is duplicated, all members key must be unique." + " type: " + type.FullName + " member:" + item.Name);

                        member.IntKey = hiddenIntKey++;
                        stringMembers.Add(member.StringKey, member);
                    }
                }

                foreach (var item in type.GetRuntimeFields())
                {
                    if (item.GetCustomAttribute<IgnoreMemberAttribute>(true) != null) continue;
                    if (item.GetCustomAttribute<IgnoreDataMemberAttribute>(true) != null) continue;
                    if (item.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>(true) != null) continue;
                    if (item.IsStatic) continue;

                    var member = new EmittableMember
                    {
                        FieldInfo = item,
                        IsReadable = allowPrivate || item.IsPublic,
                        IsWritable = allowPrivate || (item.IsPublic && !item.IsInitOnly),
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;

                    KeyAttribute key;
                    if (contractAttr != null)
                    {
                        // MessagePackObjectAttribute
                        key = item.GetCustomAttribute<KeyAttribute>(true);
                        if (key == null)
                        {
                            throw new MessagePackDynamicObjectResolverException("all public members must mark KeyAttribute or IgnoreMemberAttribute." + " type: " + type.FullName + " member:" + item.Name);
                        }

                        if (key.IntKey == null && key.StringKey == null) throw new MessagePackDynamicObjectResolverException("both IntKey and StringKey are null." + " type: " + type.FullName + " member:" + item.Name);
                    }
                    else
                    {
                        // DataContractAttribute
                        var pseudokey = item.GetCustomAttribute<DataMemberAttribute>(true);
                        if (pseudokey == null)
                        {
                            throw new MessagePackDynamicObjectResolverException("all public members must mark DataMemberAttribute or IgnoreMemberAttribute." + " type: " + type.FullName + " member:" + item.Name);
                        }

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
                        if (intMembers.ContainsKey(member.IntKey)) throw new MessagePackDynamicObjectResolverException("key is duplicated, all members key must be unique." + " type: " + type.FullName + " member:" + item.Name);

                        intMembers.Add(member.IntKey, member);
                    }
                    else
                    {
                        member.StringKey = key.StringKey;
                        if (stringMembers.ContainsKey(member.StringKey)) throw new MessagePackDynamicObjectResolverException("key is duplicated, all members key must be unique." + " type: " + type.FullName + " member:" + item.Name);

                        member.IntKey = hiddenIntKey++;
                        stringMembers.Add(member.StringKey, member);
                    }
                }
            }

            // GetConstructor
            IEnumerator<ConstructorInfo> ctorEnumerator = null;
            var ctor = ti.DeclaredConstructors.Where(x => x.IsPublic).SingleOrDefault(x => x.GetCustomAttribute<SerializationConstructorAttribute>(false) != null);
            if (ctor == null)
            {
                ctorEnumerator =
                    ti.DeclaredConstructors.Where(x => x.IsPublic).OrderBy(x => x.GetParameters().Length)
                    .GetEnumerator();

                if (ctorEnumerator.MoveNext())
                {
                    ctor = ctorEnumerator.Current;
                }
            }
            // struct allows null ctor
            if (ctor == null && isClass) throw new MessagePackDynamicObjectResolverException("can't find public constructor. type:" + type.FullName);

            var constructorParameters = new List<EmittableMember>();
            if (ctor != null)
            {
                var constructorLookupDictionary = stringMembers.ToLookup(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);
                do
                {
                    constructorParameters.Clear();
                    var ctorParamIndex = 0;
                    foreach (var item in ctor.GetParameters())
                    {
                        EmittableMember paramMember;
                        if (isIntKey)
                        {
                            if (intMembers.TryGetValue(ctorParamIndex, out paramMember))
                            {
                                if (item.ParameterType == paramMember.Type && paramMember.IsReadable)
                                {
                                    constructorParameters.Add(paramMember);
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
                            var hasKey = constructorLookupDictionary[item.Name];
                            var len = hasKey.Count();
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
                                if (item.ParameterType == paramMember.Type && paramMember.IsReadable)
                                {
                                    constructorParameters.Add(paramMember);
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
                } while (TryGetNextConstructor(ctorEnumerator, ref ctor));

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
                        var attr = x.GetDataMemberAttribute();
                        if (attr == null) return int.MaxValue;
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
                Members = members,
            };
        }

        static bool TryGetNextConstructor(IEnumerator<ConstructorInfo> ctorEnumerator, ref ConstructorInfo ctor)
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

        public class EmittableMember
        {
            public bool IsProperty { get { return PropertyInfo != null; } }
            public bool IsField { get { return FieldInfo != null; } }
            public bool IsWritable { get; set; }
            public bool IsReadable { get; set; }
            public int IntKey { get; set; }
            public string StringKey { get; set; }
            public Type Type { get { return IsField ? FieldInfo.FieldType : PropertyInfo.PropertyType; } }
            public FieldInfo FieldInfo { get; set; }
            public PropertyInfo PropertyInfo { get; set; }

            public string Name
            {
                get
                {
                    return IsProperty ? PropertyInfo.Name : FieldInfo.Name;
                }
            }

            public bool IsValueType
            {
                get
                {
                    var mi = IsProperty ? (MemberInfo)PropertyInfo : FieldInfo;
                    return mi.DeclaringType.GetTypeInfo().IsValueType;
                }
            }

            public MessagePackFormatterAttribute GetMessagePackFormatterAttribtue()
            {
                if (IsProperty)
                {
                    return (MessagePackFormatterAttribute)PropertyInfo.GetCustomAttribute<MessagePackFormatterAttribute>(true);
                }
                else
                {
                    return (MessagePackFormatterAttribute)FieldInfo.GetCustomAttribute<MessagePackFormatterAttribute>(true);
                }
            }

            public DataMemberAttribute GetDataMemberAttribute()
            {
                if (IsProperty)
                {
                    return (DataMemberAttribute)PropertyInfo.GetCustomAttribute<DataMemberAttribute>(true);
                }
                else
                {
                    return (DataMemberAttribute)FieldInfo.GetCustomAttribute<DataMemberAttribute>(true);
                }
            }

            public void EmitLoadValue(ILGenerator il)
            {
                if (IsProperty)
                {
                    il.EmitCall(PropertyInfo.GetGetMethod(true));
                }
                else
                {
                    il.Emit(OpCodes.Ldfld, FieldInfo);
                }
            }

            public void EmitStoreValue(ILGenerator il)
            {
                if (IsProperty)
                {
                    il.EmitCall(PropertyInfo.GetSetMethod(true));
                }
                else
                {
                    il.Emit(OpCodes.Stfld, FieldInfo);
                }
            }

            //public object ReflectionLoadValue(object value)
            //{
            //    if (IsProperty)
            //    {
            //        return PropertyInfo.GetValue(value);
            //    }
            //    else
            //    {
            //        return FieldInfo.GetValue(value);
            //    }
            //}

            //public void ReflectionStoreValue(object obj, object value)
            //{
            //    if (IsProperty)
            //    {
            //        PropertyInfo.SetValue(obj, value);
            //    }
            //    else
            //    {
            //        FieldInfo.SetValue(obj, value);
            //    }
            //}
        }
    }

    internal class MessagePackDynamicObjectResolverException : Exception
    {
        public MessagePackDynamicObjectResolverException(string message)
            : base(message)
        {

        }
    }
}

#endif
#endif