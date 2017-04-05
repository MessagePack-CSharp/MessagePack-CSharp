using System;
using System.Linq;
using MessagePack.Formatters;
using MessagePack.Internal;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// ObjectResolver by dynamic code generation.
    /// </summary>
    public class DynamicObjectResolver : IFormatterResolver
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

                var formatterTypeInfo = DynamicObjectTypeBuilder.BuildType(assembly, typeof(T), false);
                if (formatterTypeInfo == null) return;

                formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterTypeInfo.AsType());
            }
        }
    }

    /// <summary>
    /// ObjectResolver by dynamic code generation, no needs MessagePackObject attribute and serialized key as string.
    /// </summary>
    public class DynamicContractlessObjectResolver : IFormatterResolver
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
        public void Save()
        {
            assembly.Save();
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

                if (!typeof(T).GetTypeInfo().IsPublic() && !typeof(T).GetTypeInfo().IsNestedPublic && ti.IsClass)
                {
                    formatter = (IMessagePackFormatter<T>)DynamicPrivateFormatterBuilder.BuildFormatter(typeof(T));
                    return;
                }

                var formatterTypeInfo = DynamicObjectTypeBuilder.BuildType(assembly, typeof(T), true); // true.
                if (formatterTypeInfo == null) return;

                formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterTypeInfo.AsType());
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class DynamicObjectTypeBuilder
    {
#if NETSTANDARD1_4
        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled);
#else
        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+");
#endif

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

        public static TypeInfo BuildType(DynamicAssembly assembly, Type type, bool forceStringKey)
        {
            if (ignoreTypes.Contains(type)) return null;

            var serializationInfo = MessagePack.Internal.ObjectSerializationInfo.CreateOrNull(type, forceStringKey);
            if (serializationInfo == null) return null;

            var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
            var typeBuilder = assembly.ModuleBuilder.DefineType("MessagePack.Formatters." + SubtractFullNameRegex.Replace(type.FullName, "").Replace(".", "_") + "Formatter", TypeAttributes.Public | TypeAttributes.Sealed, null, new[] { formatterType });

            FieldBuilder dictionaryField = null;

            // string key needs string->int mapper for deserialize switch statement
            if (serializationInfo.IsStringKey)
            {
                var method = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                dictionaryField = typeBuilder.DefineField("keyMapping", typeof(Dictionary<string, int>), FieldAttributes.Private | FieldAttributes.InitOnly);

                var il = method.GetILGenerator();
                BuildConstructor(type, serializationInfo, method, dictionaryField, il);
            }
            {
                var method = typeBuilder.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(int),
                    new Type[] { typeof(byte[]).MakeByRefType(), typeof(int), type, typeof(IFormatterResolver) });

                var il = method.GetILGenerator();
                BuildSerialize(type, serializationInfo, method, il);
            }

            {
                var method = typeBuilder.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    type,
                    new Type[] { typeof(byte[]), typeof(int), typeof(IFormatterResolver), typeof(int).MakeByRefType() });

                var il = method.GetILGenerator();
                BuildDeserialize(type, serializationInfo, method, dictionaryField, il);
            }

            return typeBuilder.CreateTypeInfo();
        }

        static void BuildConstructor(Type type, ObjectSerializationInfo info, ConstructorInfo method, FieldBuilder dictionaryField, ILGenerator il)
        {
            il.EmitLdarg(0);
            il.Emit(OpCodes.Call, objectCtor);

            il.EmitLdarg(0);
            il.EmitLdc_I4(info.Members.Length);
            il.Emit(OpCodes.Newobj, dictionaryConstructor);

            foreach (var item in info.Members)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldstr, item.StringKey);
                il.EmitLdc_I4(item.IntKey);
                il.EmitCall(dictionaryAdd);
            }

            il.Emit(OpCodes.Stfld, dictionaryField);
            il.Emit(OpCodes.Ret);
        }

        // int Serialize([arg:1]ref byte[] bytes, [arg:2]int offset, [arg:3]T value, [arg:4]IFormatterResolver formatterResolver);
        static void BuildSerialize(Type type, ObjectSerializationInfo info, MethodInfo method, ILGenerator il)
        {
            // if(value == null) return WriteNil
            if (type.GetTypeInfo().IsClass)
            {
                var elseBody = il.DefineLabel();

                il.EmitLdarg(3);
                il.Emit(OpCodes.Brtrue_S, elseBody);
                il.EmitLdarg(1);
                il.EmitLdarg(2);
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
                    if (info.IsStruct)
                    {
                        il.EmitLdarga(3);
                    }
                    else
                    {
                        il.EmitLdarg(3);
                    }
                    il.Emit(OpCodes.Call, runtimeMethods[0]); // don't use EmitCall helper(must use 'Call')
                }
                else
                {
                    il.EmitLdarg(3);
                    if (info.IsStruct)
                    {
                        il.Emit(OpCodes.Box, type);
                    }
                    il.EmitCall(onBeforeSerialize);
                }
            }

            // var startOffset = offset;
            var startOffsetLocal = il.DeclareLocal(typeof(int)); // [loc:0]
            il.EmitLdarg(2);
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
                });

                for (int i = 0; i <= maxKey; i++)
                {
                    ObjectSerializationInfo.EmittableMember member;
                    if (intKeyMap.TryGetValue(i, out member))
                    {
                        // offset += serialzie
                        EmitSerializeValue(il, type.GetTypeInfo(), member);
                    }
                    else
                    {
                        // Write Nil as Blanc
                        EmitOffsetPlusEqual(il, null, () =>
                        {
                            il.EmitCall(MessagePackBinaryTypeInfo.WriteNil);
                        });
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
                });

                foreach (var item in info.Members.Where(x => x.IsReadable))
                {
                    // offset += writekey
                    if (info.IsStringKey)
                    {
                        EmitOffsetPlusEqual(il, null, () =>
                        {
                            // embed string and bytesize
                            il.Emit(OpCodes.Ldstr, item.StringKey);
                            il.EmitLdc_I4(StringEncoding.UTF8.GetByteCount(item.StringKey));
                            il.EmitCall(MessagePackBinaryTypeInfo.WriteStringUnsafe);
                        });
                    }

                    // offset += serialzie
                    EmitSerializeValue(il, type.GetTypeInfo(), item);
                }
            }

            // return startOffset- offset;
            il.EmitLdarg(2);
            il.EmitLdloc(startOffsetLocal);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Ret);
        }

        // offset += ***(ref bytes, offset....
        static void EmitOffsetPlusEqual(ILGenerator il, Action loadEmit, Action emit)
        {
            il.EmitLdarg(2);

            if (loadEmit != null) loadEmit();

            il.EmitLdarg(1);
            il.EmitLdarg(2);

            emit();

            il.Emit(OpCodes.Add);
            il.EmitStarg(2);
        }

        static void EmitSerializeValue(ILGenerator il, TypeInfo type, ObjectSerializationInfo.EmittableMember member)
        {
            var t = member.Type;
            if (IsOptimizeTargetType(t))
            {
                EmitOffsetPlusEqual(il, null, () =>
                {
                    il.EmitLoadArg(type, 3);
                    member.EmitLoadValue(il);
                    if (t == typeof(byte[]))
                    {
                        il.EmitCall(MessagePackBinaryTypeInfo.WriteBytes);
                    }
                    else
                    {
                        il.EmitCall(MessagePackBinaryTypeInfo.TypeInfo.GetDeclaredMethod("Write" + t.Name));
                    }
                });
            }
            else
            {
                EmitOffsetPlusEqual(il, () =>
                {
                    il.EmitLdarg(4);
                    il.Emit(OpCodes.Call, getFormatterWithVerify.MakeGenericMethod(t));
                }, () =>
                {
                    il.EmitLoadArg(type, 3);
                    member.EmitLoadValue(il);
                    il.EmitLdarg(4);
                    il.EmitCall(getSerialize(t));
                });
            }
        }

        // T Deserialize([arg:1]byte[] bytes, [arg:2]int offset, [arg:3]IFormatterResolver formatterResolver, [arg:4]out int readSize);
        static void BuildDeserialize(Type type, ObjectSerializationInfo info, MethodBuilder method, FieldBuilder dictionaryField, ILGenerator il)
        {
            // if(MessagePackBinary.IsNil) readSize = 1, return null;
            var falseLabel = il.DefineLabel();
            il.EmitLdarg(1);
            il.EmitLdarg(2);
            il.EmitCall(MessagePackBinaryTypeInfo.IsNil);
            il.Emit(OpCodes.Brfalse_S, falseLabel);
            if (type.GetTypeInfo().IsClass)
            {
                il.EmitLdarg(4);
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
            il.EmitLdarg(2);
            il.EmitStloc(startOffsetLocal);

            // var length = ReadMapHeader
            var length = il.DeclareLocal(typeof(int)); // [loc:1]
            il.EmitLdarg(1);
            il.EmitLdarg(2);
            il.EmitLdarg(4);

            if (info.IsIntKey)
            {
                il.EmitCall(MessagePackBinaryTypeInfo.ReadArrayHeader);
            }
            else
            {
                il.EmitCall(MessagePackBinaryTypeInfo.ReadMapHeader);
            }
            il.EmitStloc(length);
            EmitOffsetPlusReadSize(il);

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
                        SwitchLabel = il.DefineLabel()
                    })
                    .ToArray();
            }


            // Read Loop(for var i = 0; i< length; i++)
            {
                var key = il.DeclareLocal(typeof(int));
                var switchDefault = il.DefineLabel();
                var loopEnd = il.DefineLabel();
                var stringKeyTrue = il.DefineLabel();
                il.EmitIncrementFor(length, forILocal =>
                {
                    if (info.IsStringKey)
                    {
                        // get string key -> dictionary lookup
                        il.EmitLdarg(0);
                        il.Emit(OpCodes.Ldfld, dictionaryField);
                        il.EmitLdarg(1);
                        il.EmitLdarg(2);
                        il.EmitLdarg(4);
                        il.EmitCall(MessagePackBinaryTypeInfo.ReadString);
                        il.EmitLdloca(key);
                        il.EmitCall(dictionaryTryGetValue);
                        EmitOffsetPlusReadSize(il);
                        il.Emit(OpCodes.Brtrue_S, stringKeyTrue);

                        il.EmitLdarg(4);
                        il.EmitLdarg(1);
                        il.EmitLdarg(2);
                        il.EmitCall(MessagePackBinaryTypeInfo.ReadNextBlock);
                        il.Emit(OpCodes.Stind_I4);
                        il.Emit(OpCodes.Br, loopEnd);

                        il.MarkLabel(stringKeyTrue);
                    }
                    else
                    {
                        il.EmitLdloc(forILocal);
                        il.EmitStloc(key);
                    }

                    // switch... local = Deserialize
                    il.EmitLdloc(key);

                    il.Emit(OpCodes.Switch, infoList.Select(x => x.SwitchLabel).ToArray());

                    il.MarkLabel(switchDefault);
                    // default, only read. readSize = MessagePackBinary.ReadNextBlock(bytes, offset);
                    il.EmitLdarg(4);
                    il.EmitLdarg(1);
                    il.EmitLdarg(2);
                    il.EmitCall(MessagePackBinaryTypeInfo.ReadNextBlock);
                    il.Emit(OpCodes.Stind_I4);
                    il.Emit(OpCodes.Br, loopEnd);

                    if (gotoDefault != null)
                    {
                        il.MarkLabel(gotoDefault.Value);
                        il.Emit(OpCodes.Br, switchDefault);
                    }

                    foreach (var item in infoList)
                    {
                        if (item.MemberInfo != null)
                        {
                            il.MarkLabel(item.SwitchLabel);
                            EmitDeserializeValue(il, item);
                            il.Emit(OpCodes.Br, loopEnd);
                        }
                    }

                    // offset += readSize
                    il.MarkLabel(loopEnd);
                    EmitOffsetPlusReadSize(il);
                });
            }

            // finish readSize: readSize = offset - startOffset;
            il.EmitLdarg(4);
            il.EmitLdarg(2);
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

        static void EmitOffsetPlusReadSize(ILGenerator il)
        {
            il.EmitLdarg(2);
            il.EmitLdarg(4);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Add);
            il.EmitStarg(2);
        }

        static void EmitDeserializeValue(ILGenerator il, DeserializeInfo info)
        {
            var member = info.MemberInfo;
            var t = member.Type;
            if (IsOptimizeTargetType(t))
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
                    il.EmitCall(MessagePackBinaryTypeInfo.TypeInfo.GetDeclaredMethod("Read" + t.Name));
                }
            }
            else
            {
                il.EmitLdarg(3);
                il.EmitCall(getFormatterWithVerify.MakeGenericMethod(t));
                il.EmitLdarg(1);
                il.EmitLdarg(2);
                il.EmitLdarg(3);
                il.EmitLdarg(4);
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
        static readonly ConstructorInfo dictionaryConstructor = typeof(Dictionary<string, int>).GetTypeInfo().DeclaredConstructors.First(x => { var p = x.GetParameters(); return p.Length == 1 && p[0].ParameterType == typeof(int); });
        static readonly MethodInfo dictionaryAdd = typeof(Dictionary<string, int>).GetRuntimeMethod("Add", new[] { typeof(string), typeof(int) });
        static readonly MethodInfo dictionaryTryGetValue = typeof(Dictionary<string, int>).GetRuntimeMethod("TryGetValue", new[] { typeof(string), refInt });
        static readonly ConstructorInfo invalidOperationExceptionConstructor = typeof(System.InvalidOperationException).GetTypeInfo().DeclaredConstructors.First(x => { var p = x.GetParameters(); return p.Length == 1 && p[0].ParameterType == typeof(string); });

        static readonly MethodInfo onBeforeSerialize = typeof(IMessagePackSerializationCallbackReceiver).GetRuntimeMethod("OnBeforeSerialize", Type.EmptyTypes);
        static readonly MethodInfo onAfterDeserialize = typeof(IMessagePackSerializationCallbackReceiver).GetRuntimeMethod("OnAfterDeserialize", Type.EmptyTypes);

        static readonly ConstructorInfo objectCtor = typeof(object).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 0);

        internal static class MessagePackBinaryTypeInfo
        {
            public static TypeInfo TypeInfo = typeof(MessagePackBinary).GetTypeInfo();

            public static MethodInfo WriteFixedMapHeaderUnsafe = typeof(MessagePackBinary).GetRuntimeMethod("WriteFixedMapHeaderUnsafe", new[] { refByte, typeof(int), typeof(int) });
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
            public static MethodInfo IsNil = typeof(MessagePackBinary).GetRuntimeMethod("IsNil", new[] { typeof(byte[]), typeof(int) });
            public static MethodInfo ReadNextBlock = typeof(MessagePackBinary).GetRuntimeMethod("ReadNextBlock", new[] { typeof(byte[]), typeof(int) });
            public static MethodInfo WriteStringUnsafe = typeof(MessagePackBinary).GetRuntimeMethod("WriteStringUnsafe", new[] { refByte, typeof(int), typeof(string), typeof(int) });

            public static MethodInfo ReadArrayHeader = typeof(MessagePackBinary).GetRuntimeMethod("ReadArrayHeader", new[] { typeof(byte[]), typeof(int), refInt });
            public static MethodInfo ReadMapHeader = typeof(MessagePackBinary).GetRuntimeMethod("ReadMapHeader", new[] { typeof(byte[]), typeof(int), refInt });

            static MessagePackBinaryTypeInfo()
            {
            }
        }

        class DeserializeInfo
        {
            public ObjectSerializationInfo.EmittableMember MemberInfo { get; set; }
            public LocalBuilder LocalField { get; set; }
            public Label SwitchLabel { get; set; }
        }
    }

    internal static class DynamicPrivateFormatterBuilder
    {
        static readonly Type refByte = typeof(byte[]).MakeByRefType();
        static readonly MethodInfo getFormatterWithVerify = typeof(FormatterResolverExtensions).GetRuntimeMethods().First(x => x.Name == "GetFormatterWithVerify");
        static readonly Func<Type, MethodInfo> getSerialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod("Serialize", new[] { refByte, typeof(int), t, typeof(IFormatterResolver) });

        // Private type formatter can not create by DynamicAssembly but sometimes needs it(anonymous type, etc...)
        // use DynamicMethod(skipVisibility:true) can avoid it so use delegation formatter.
        public static object BuildFormatter(Type type)
        {
            var info = ObjectSerializationInfo.CreateOrNull(type, true);

            var serialize = new DynamicMethod("Serialize", typeof(int), new[] { typeof(byte[]).MakeByRefType(), typeof(int), type, typeof(IFormatterResolver) }, type, true);

            var il = serialize.GetILGenerator();

            // Build Serialize(same as DynamicObjectTypeBuilder.BuildSerialize but argument - 1)
            {
                // if(value == null) return WriteNil
                var elseBody = il.DefineLabel();

                il.EmitLdarg(2);
                il.Emit(OpCodes.Brtrue_S, elseBody);
                il.EmitLdarg(0);
                il.EmitLdarg(1);
                il.EmitCall(DynamicObjectTypeBuilder.MessagePackBinaryTypeInfo.WriteNil);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(elseBody);

                // var startOffset = offset;
                var startOffsetLocal = il.DeclareLocal(typeof(int)); // [loc:0]
                il.EmitLdarg(1);
                il.EmitStloc(startOffsetLocal);

                // use only Map!
                var writeCount = info.Members.Count(x => x.IsReadable);

                EmitOffsetPlusEqual(il, null, () =>
                {
                    il.EmitLdc_I4(writeCount);
                    if (writeCount <= MessagePackRange.MaxFixMapCount)
                    {
                        il.EmitCall(DynamicObjectTypeBuilder.MessagePackBinaryTypeInfo.WriteFixedMapHeaderUnsafe);
                    }
                    else
                    {
                        il.EmitCall(DynamicObjectTypeBuilder.MessagePackBinaryTypeInfo.WriteMapHeader);
                    }
                });

                foreach (var item in info.Members.Where(x => x.IsReadable))
                {
                    // offset += writekey
                    if (info.IsStringKey)
                    {
                        EmitOffsetPlusEqual(il, null, () =>
                        {
                            // embed string and bytesize
                            il.Emit(OpCodes.Ldstr, item.StringKey);
                            il.EmitLdc_I4(StringEncoding.UTF8.GetByteCount(item.StringKey));
                            il.EmitCall(DynamicObjectTypeBuilder.MessagePackBinaryTypeInfo.WriteStringUnsafe);
                        });
                    }

                    // offset += serialzie
                    EmitSerializeValue(il, type.GetTypeInfo(), item);
                }

                // return startOffset- offset;
                il.EmitLdarg(1);
                il.EmitLdloc(startOffsetLocal);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Ret);
            }

            var method = serialize.CreateDelegate(typeof(SerializeDelegate<>).MakeGenericType(type));
            var formatter = Activator.CreateInstance(typeof(AnonymousSerializableFormatter<>).MakeGenericType(type), new object[] { method });
            return formatter;
        }

        static void EmitOffsetPlusEqual(ILGenerator il, Action loadEmit, Action emit)
        {
            il.EmitLdarg(1);

            if (loadEmit != null) loadEmit();

            il.EmitLdarg(0);
            il.EmitLdarg(1);

            emit();

            il.Emit(OpCodes.Add);
            il.EmitStarg(1);
        }

        static void EmitSerializeValue(ILGenerator il, TypeInfo type, ObjectSerializationInfo.EmittableMember member)
        {
            var t = member.Type;
            if (IsOptimizeTargetType(t))
            {
                EmitOffsetPlusEqual(il, null, () =>
                {
                    il.EmitLoadArg(type, 2);
                    member.EmitLoadValue(il);
                    if (t == typeof(byte[]))
                    {
                        il.EmitCall(DynamicObjectTypeBuilder.MessagePackBinaryTypeInfo.WriteBytes);
                    }
                    else
                    {
                        il.EmitCall(DynamicObjectTypeBuilder.MessagePackBinaryTypeInfo.TypeInfo.GetDeclaredMethod("Write" + t.Name));
                    }
                });
            }
            else
            {
                EmitOffsetPlusEqual(il, () =>
                {
                    il.EmitLdarg(3);
                    il.Emit(OpCodes.Call, getFormatterWithVerify.MakeGenericMethod(t));
                }, () =>
                {
                    il.EmitLoadArg(type, 2);
                    member.EmitLoadValue(il);
                    il.EmitLdarg(3);
                    il.EmitCall(getSerialize(t));
                });
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
    }

    internal delegate int SerializeDelegate<T>(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver);

    internal class AnonymousSerializableFormatter<T> : IMessagePackFormatter<T>
    {
        readonly SerializeDelegate<T> serialize;

        public AnonymousSerializableFormatter(SerializeDelegate<T> serialize)
        {
            this.serialize = serialize;
        }

        public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
        {
            return serialize(ref bytes, offset, value, formatterResolver);
        }

        public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            throw new NotSupportedException("Anonymous Formatter does not support Deserialize.");
        }
    }

    internal class ObjectSerializationInfo
    {
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

        public static ObjectSerializationInfo CreateOrNull(Type type, bool forceStringKey)
        {
            var ti = type.GetTypeInfo();
            var isClass = ti.IsClass;

            var contractAttr = ti.GetCustomAttribute<MessagePackObjectAttribute>();
            if (contractAttr == null && !forceStringKey)
            {
                return null;
            }

            var isIntKey = true;
            var intMembers = new Dictionary<int, EmittableMember>();
            var stringMembers = new Dictionary<string, EmittableMember>();

            if (forceStringKey || contractAttr.KeyAsPropertyName)
            {
                // All public members are serialize target except [Ignore] member.
                isIntKey = false;

                var hiddenIntKey = 0;
                foreach (var item in type.GetRuntimeProperties())
                {
                    if (item.GetCustomAttribute<IgnoreMemberAttribute>(true) != null) continue;
                    if (item.GetCustomAttribute<IgnoreDataMemberAttribute>(true) != null) continue;

                    var member = new EmittableMember
                    {
                        PropertyInfo = item,
                        IsReadable = (item.GetGetMethod() != null) && item.GetGetMethod().IsPublic && !item.GetGetMethod().IsStatic,
                        IsWritable = (item.GetSetMethod() != null) && item.GetSetMethod().IsPublic && !item.GetSetMethod().IsStatic,
                        StringKey = item.Name
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;
                    member.IntKey = hiddenIntKey++;
                    stringMembers.Add(member.StringKey, member);
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
                        IsReadable = item.IsPublic,
                        IsWritable = item.IsPublic && !item.IsInitOnly,
                        StringKey = item.Name
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;
                    member.IntKey = hiddenIntKey++;
                    stringMembers.Add(member.StringKey, member);
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

                    var member = new EmittableMember
                    {
                        PropertyInfo = item,
                        IsReadable = (item.GetGetMethod() != null) && item.GetGetMethod().IsPublic && !item.GetGetMethod().IsStatic,
                        IsWritable = (item.GetSetMethod() != null) && item.GetSetMethod().IsPublic && !item.GetSetMethod().IsStatic,
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;

                    var key = item.GetCustomAttribute<KeyAttribute>(true);
                    if (key == null) throw new MessagePackDynamicObjectResolverException("all public members must mark KeyAttribute or IgnoreMemberAttribute." + " type: " + type.FullName + " member:" + item.Name);

                    if (key.IntKey == null && key.StringKey == null) throw new MessagePackDynamicObjectResolverException("both IntKey and StringKey are null." + " type: " + type.FullName + " member:" + item.Name);

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
                        IsReadable = item.IsPublic,
                        IsWritable = item.IsPublic && !item.IsInitOnly,
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;

                    var key = item.GetCustomAttribute<KeyAttribute>(true);
                    if (key == null) throw new MessagePackDynamicObjectResolverException("all public members must mark KeyAttribute or IgnoreMemberAttribute." + " type: " + type.FullName + " member:" + item.Name);

                    if (key.IntKey == null && key.StringKey == null) throw new MessagePackDynamicObjectResolverException("both IntKey and StringKey are null." + " type: " + type.FullName + " member:" + item.Name);

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
            var ctor = ti.DeclaredConstructors.Where(x => x.IsPublic).SingleOrDefault(x => x.GetCustomAttribute<SerializationConstructorAttribute>(false) != null);
            if (ctor == null)
            {
                ctor = ti.DeclaredConstructors.Where(x => x.IsPublic).OrderBy(x => x.GetParameters().Length).FirstOrDefault();
            }
            // struct allows null ctor
            if (ctor == null && isClass) throw new MessagePackDynamicObjectResolverException("can't find public constructor. type:" + type.FullName);

            var constructorParameters = new List<EmittableMember>();
            if (ctor != null)
            {
                var constructorLookupDictionary = stringMembers.ToLookup(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);

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
                                throw new MessagePackDynamicObjectResolverException("can't find matched constructor parameter, parameterType mismatch. type:" + type.FullName + " parameterIndex:" + ctorParamIndex + " paramterType:" + item.ParameterType.Name);
                            }
                        }
                        else
                        {
                            throw new MessagePackDynamicObjectResolverException("can't find matched constructor parameter, index not found. type:" + type.FullName + " parameterIndex:" + ctorParamIndex);
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
                                throw new MessagePackDynamicObjectResolverException("duplicate matched constructor parameter name:" + type.FullName + " parameterName:" + item.Name + " paramterType:" + item.ParameterType.Name);
                            }

                            paramMember = hasKey.First().Value;
                            if (item.ParameterType == paramMember.Type && paramMember.IsReadable)
                            {
                                constructorParameters.Add(paramMember);
                            }
                            else
                            {
                                throw new MessagePackDynamicObjectResolverException("can't find matched constructor parameter, parameterType mismatch. type:" + type.FullName + " parameterName:" + item.Name + " paramterType:" + item.ParameterType.Name);
                            }
                        }
                        else
                        {
                            throw new MessagePackDynamicObjectResolverException("can't find matched constructor parameter, index not found. type:" + type.FullName + " parameterName:" + item.Name);
                        }
                    }
                    ctorParamIndex++;
                }
            }

            return new ObjectSerializationInfo
            {
                IsClass = isClass,
                BestmatchConstructor = ctor,
                ConstructorParameters = constructorParameters.ToArray(),
                IsIntKey = isIntKey,
                Members = (isIntKey) ? intMembers.Values.ToArray() : stringMembers.Values.ToArray()
            };
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
            public bool IsValueType
            {
                get
                {
                    var mi = IsProperty ? (MemberInfo)PropertyInfo : FieldInfo;
                    return mi.DeclaringType.GetTypeInfo().IsValueType;
                }
            }

            public void EmitLoadValue(ILGenerator il)
            {
                if (IsProperty)
                {
                    il.EmitCall(PropertyInfo.GetGetMethod());
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
                    il.EmitCall(PropertyInfo.GetSetMethod());
                }
                else
                {
                    il.Emit(OpCodes.Stfld, FieldInfo);
                }
            }
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