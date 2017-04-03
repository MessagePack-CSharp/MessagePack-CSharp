using System;
using System.Linq;
using MessagePack.Formatters;
using MessagePack.Internal;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// UnionResolver by dynamic code generation.
    /// </summary>
    public class DynamicUnionResolver : IFormatterResolver
    {
        public static readonly DynamicUnionResolver Instance = new DynamicUnionResolver();

        const string ModuleName = "MessagePack.Resolvers.DynamicUnionResolver";

        static readonly DynamicAssembly assembly;
#if NETSTANDARD1_4
        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled);
#else
        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+");
#endif

        DynamicUnionResolver()
        {

        }

        static DynamicUnionResolver()
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

                    var innerFormatter = DynamicUnionResolver.Instance.GetFormatterDynamic(ti.AsType());
                    if (innerFormatter == null)
                    {
                        return;
                    }
                    formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }

                var formatterTypeInfo = BuildType(typeof(T));
                if (formatterTypeInfo == null) return;

                formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterTypeInfo.AsType());
            }
        }

        static TypeInfo BuildType(Type type)
        {
            var ti = type.GetTypeInfo();
            // order by key(important for use jump-table of switch)
            var unionAttrs = ti.GetCustomAttributes<UnionAttribute>().OrderBy(x => x.Key).ToArray();

            if (unionAttrs.Length == 0) return null;

            var checker1 = new HashSet<int>();
            var checker2 = new HashSet<Type>();
            foreach (var item in unionAttrs)
            {
                if (!checker1.Add(item.Key)) throw new MessagePackDynamicUnionResolverException("Same union key has found. Type:" + type.Name + " Key:" + item.Key);
                if (!checker2.Add(item.SubType)) throw new MessagePackDynamicUnionResolverException("Same union subType has found. Type:" + type.Name + " SubType: " + item.SubType);
            }

            var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
            var typeBuilder = assembly.ModuleBuilder.DefineType("MessagePack.Formatters." + SubtractFullNameRegex.Replace(type.FullName, "").Replace(".", "_") + "Formatter", TypeAttributes.Public | TypeAttributes.Sealed, null, new[] { formatterType });

            FieldBuilder typeToKeyAndJumpMap = null; // Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>
            FieldBuilder keyToJumpMap = null; // Dictionary<int, int>

            // create map dictionary
            {
                var method = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                typeToKeyAndJumpMap = typeBuilder.DefineField("typeToKeyAndJumpMap", typeof(Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>), FieldAttributes.Private | FieldAttributes.InitOnly);
                keyToJumpMap = typeBuilder.DefineField("keyToJumpMap", typeof(Dictionary<int, int>), FieldAttributes.Private | FieldAttributes.InitOnly);

                var il = method.GetILGenerator();
                BuildConstructor(type, unionAttrs, method, typeToKeyAndJumpMap, keyToJumpMap, il);
            }

            {
                var method = typeBuilder.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(int),
                    new Type[] { typeof(byte[]).MakeByRefType(), typeof(int), type, typeof(IFormatterResolver) });

                var il = method.GetILGenerator();
                BuildSerialize(type, unionAttrs, method, typeToKeyAndJumpMap, il);
            }
            {
                var method = typeBuilder.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    type,
                    new Type[] { typeof(byte[]), typeof(int), typeof(IFormatterResolver), typeof(int).MakeByRefType() });

                var il = method.GetILGenerator();
                BuildDeserialize(type, unionAttrs, method, keyToJumpMap, il);
            }

            return typeBuilder.CreateTypeInfo();
        }

        static void BuildConstructor(Type type, UnionAttribute[] infos, ConstructorInfo method, FieldBuilder typeToKeyAndJumpMap, FieldBuilder keyToJumpMap, ILGenerator il)
        {
            il.EmitLdarg(0);
            il.Emit(OpCodes.Call, objectCtor);

            {
                il.EmitLdarg(0);
                il.EmitLdc_I4(infos.Length);
                il.Emit(OpCodes.Ldsfld, runtimeTypeHandleEqualityComparer);
                il.Emit(OpCodes.Newobj, typeMapDictionaryConstructor);

                var index = 0;
                foreach (var item in infos)
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
                foreach (var item in infos)
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


        // int Serialize([arg:1]ref byte[] bytes, [arg:2]int offset, [arg:3]T value, [arg:4]IFormatterResolver formatterResolver);
        static void BuildSerialize(Type type, UnionAttribute[] infos, MethodBuilder method, FieldBuilder typeToKeyAndJumpMap, ILGenerator il)
        {
            // if(value == null) return WriteNil
            var elseBody = il.DefineLabel();
            var notFoundType = il.DefineLabel();

            il.EmitLdarg(3);
            il.Emit(OpCodes.Brtrue_S, elseBody);
            il.Emit(OpCodes.Br, notFoundType);
            il.MarkLabel(elseBody);

            var keyPair = il.DeclareLocal(typeof(KeyValuePair<int, int>));

            il.EmitLoadThis();
            il.EmitLdfld(typeToKeyAndJumpMap);
            il.EmitLdarg(3);
            il.EmitCall(objectGetType);
            il.EmitCall(getTypeHandle);
            il.EmitLdloca(keyPair);
            il.EmitCall(typeMapDictionaryTryGetValue);
            il.Emit(OpCodes.Brfalse, notFoundType);

            // var startOffset = offset;
            var startOffsetLocal = il.DeclareLocal(typeof(int));
            il.EmitLdarg(2);
            il.EmitStloc(startOffsetLocal);

            // offset += WriteFixedArrayHeaderUnsafe(,,2);
            EmitOffsetPlusEqual(il, null, () =>
            {
                il.EmitLdc_I4(2);
                il.EmitCall(MessagePackBinaryTypeInfo.WriteFixedArrayHeaderUnsafe);
            });

            // offset += WriteInt32(,,keyPair.Key)
            EmitOffsetPlusEqual(il, null, () =>
            {
                il.EmitLdloca(keyPair);
                il.EmitCall(intIntKeyValuePairGetKey);
                il.EmitCall(MessagePackBinaryTypeInfo.WriteInt32);
            });

            var loopEnd = il.DefineLabel();

            // switch-case (offset += resolver.GetFormatter.Serialize(with cast)
            var switchLabels = infos.Select(x => new { Label = il.DefineLabel(), Attr = x }).ToArray();
            il.EmitLdloca(keyPair);
            il.EmitCall(intIntKeyValuePairGetValue);
            il.Emit(OpCodes.Switch, switchLabels.Select(x => x.Label).ToArray());
            il.Emit(OpCodes.Br, loopEnd); // default

            foreach (var item in switchLabels)
            {
                il.MarkLabel(item.Label);
                EmitOffsetPlusEqual(il, () =>
                {
                    il.EmitLdarg(4);
                    il.Emit(OpCodes.Call, getFormatterWithVerify.MakeGenericMethod(item.Attr.SubType));
                }, () =>
                {
                    il.EmitLdarg(3);
                    if (item.Attr.SubType.GetTypeInfo().IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, item.Attr.SubType);
                    }
                    else
                    {
                        il.Emit(OpCodes.Castclass, item.Attr.SubType);
                    }
                    il.EmitLdarg(4);
                    il.Emit(OpCodes.Callvirt, getSerialize(item.Attr.SubType));
                });

                il.Emit(OpCodes.Br, loopEnd);
            }

            // return startOffset- offset;
            il.MarkLabel(loopEnd);
            il.EmitLdarg(2);
            il.EmitLdloc(startOffsetLocal);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Ret);

            // else, return WriteNil
            il.MarkLabel(notFoundType);
            il.EmitLdarg(1);
            il.EmitLdarg(2);
            il.EmitCall(MessagePackBinaryTypeInfo.WriteNil);
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

        // T Deserialize([arg:1]byte[] bytes, [arg:2]int offset, [arg:3]IFormatterResolver formatterResolver, [arg:4]out int readSize);
        static void BuildDeserialize(Type type, UnionAttribute[] infos, MethodBuilder method, FieldBuilder keyToJumpMap, ILGenerator il)
        {
            // if(MessagePackBinary.IsNil) readSize = 1, return null;
            var falseLabel = il.DefineLabel();
            il.EmitLdarg(1);
            il.EmitLdarg(2);
            il.EmitCall(MessagePackBinaryTypeInfo.IsNil);
            il.Emit(OpCodes.Brfalse_S, falseLabel);

            il.EmitLdarg(4);
            il.EmitLdc_I4(1);
            il.Emit(OpCodes.Stind_I4);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);

            // read-array header and validate, ReadArrayHeader(bytes, offset, out readSize) != 2) throw;
            il.MarkLabel(falseLabel);
            var startOffset = il.DeclareLocal(typeof(int));
            il.EmitLdarg(2);
            il.EmitStloc(startOffset);

            var rightLabel = il.DefineLabel();
            il.EmitLdarg(1);
            il.EmitLdarg(2);
            il.EmitLdarg(4);
            il.EmitCall(MessagePackBinaryTypeInfo.ReadArrayHeader);
            il.EmitLdc_I4(2);
            il.Emit(OpCodes.Beq_S, rightLabel);
            il.Emit(OpCodes.Ldstr, "Invalid Union data was detected. Type:" + type.FullName);
            il.Emit(OpCodes.Newobj, invalidOperationExceptionConstructor);
            il.Emit(OpCodes.Throw);

            il.MarkLabel(rightLabel);
            EmitOffsetPlusReadSize(il);

            // read key
            var key = il.DeclareLocal(typeof(int));
            il.EmitLdarg(1);
            il.EmitLdarg(2);
            il.EmitLdarg(4);
            il.EmitCall(MessagePackBinaryTypeInfo.ReadInt32);
            il.EmitStloc(key);
            EmitOffsetPlusReadSize(il);

            // is-sequential don't need else convert key to jump-table value
            if (!IsZeroStartSequential(infos))
            {
                var endKeyMapGet = il.DefineLabel();
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
            var result = il.DeclareLocal(type);
            var loopEnd = il.DefineLabel();
            il.Emit(OpCodes.Ldnull);
            il.EmitStloc(result);
            il.Emit(OpCodes.Ldloc, key);

            var switchLabels = infos.Select(x => new { Label = il.DefineLabel(), Attr = x }).ToArray();
            il.Emit(OpCodes.Switch, switchLabels.Select(x => x.Label).ToArray());

            // default
            il.EmitLdarg(2);
            il.EmitLdarg(1);
            il.EmitLdarg(2);
            il.EmitCall(MessagePackBinaryTypeInfo.ReadNextBlock);
            il.Emit(OpCodes.Add);
            il.EmitStarg(2);
            il.Emit(OpCodes.Br, loopEnd);

            foreach (var item in switchLabels)
            {
                il.MarkLabel(item.Label);
                il.EmitLdarg(3);
                il.EmitCall(getFormatterWithVerify.MakeGenericMethod(item.Attr.SubType));
                il.EmitLdarg(1);
                il.EmitLdarg(2);
                il.EmitLdarg(3);
                il.EmitLdarg(4);
                il.EmitCall(getDeserialize(item.Attr.SubType));
                if (item.Attr.SubType.GetTypeInfo().IsValueType)
                {
                    il.Emit(OpCodes.Box, item.Attr.SubType);
                }
                il.Emit(OpCodes.Stloc, result);
                EmitOffsetPlusReadSize(il);
                il.Emit(OpCodes.Br, loopEnd);
            }

            il.MarkLabel(loopEnd);

            //// finish readSize = offset - startOffset;
            il.EmitLdarg(4);
            il.EmitLdarg(2);
            il.EmitLdloc(startOffset);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Stind_I4);
            il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ret);
        }

        static bool IsZeroStartSequential(UnionAttribute[] infos)
        {
            for (int i = 0; i < infos.Length; i++)
            {
                if (infos[i].Key != i) return false;
            }
            return true;
        }

        static void EmitOffsetPlusReadSize(ILGenerator il)
        {
            il.EmitLdarg(2);
            il.EmitLdarg(4);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Add);
            il.EmitStarg(2);
        }

        // EmitInfos...

        static readonly Type refByte = typeof(byte[]).MakeByRefType();
        static readonly Type refInt = typeof(int).MakeByRefType();
        static readonly Type refKvp = typeof(KeyValuePair<int, int>).MakeByRefType();
        static readonly MethodInfo getFormatterWithVerify = typeof(FormatterResolverExtensions).GetRuntimeMethods().First(x => x.Name == "GetFormatterWithVerify");

        static readonly Func<Type, MethodInfo> getSerialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod("Serialize", new[] { refByte, typeof(int), t, typeof(IFormatterResolver) });
        static readonly Func<Type, MethodInfo> getDeserialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod("Deserialize", new[] { typeof(byte[]), typeof(int), typeof(IFormatterResolver), refInt });

        static readonly FieldInfo runtimeTypeHandleEqualityComparer = typeof(RuntimeTypeHandleEqualityComparer).GetRuntimeField("Default");
        static readonly ConstructorInfo intIntKeyValuePairConstructor = typeof(KeyValuePair<int, int>).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 2);
        static readonly ConstructorInfo typeMapDictionaryConstructor = typeof(Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>).GetTypeInfo().DeclaredConstructors.First(x => { var p = x.GetParameters(); return p.Length == 2 && p[0].ParameterType == typeof(int); });
        static readonly MethodInfo typeMapDictionaryAdd = typeof(Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>).GetRuntimeMethod("Add", new[] { typeof(RuntimeTypeHandle), typeof(KeyValuePair<int, int>) });
        static readonly MethodInfo typeMapDictionaryTryGetValue = typeof(Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>).GetRuntimeMethod("TryGetValue", new[] { typeof(RuntimeTypeHandle), refKvp });

        static readonly ConstructorInfo keyMapDictionaryConstructor = typeof(Dictionary<int, int>).GetTypeInfo().DeclaredConstructors.First(x => { var p = x.GetParameters(); return p.Length == 1 && p[0].ParameterType == typeof(int); });
        static readonly MethodInfo keyMapDictionaryAdd = typeof(Dictionary<int, int>).GetRuntimeMethod("Add", new[] { typeof(int), typeof(int) });
        static readonly MethodInfo keyMapDictionaryTryGetValue = typeof(Dictionary<int, int>).GetRuntimeMethod("TryGetValue", new[] { typeof(int), refInt });

        static readonly MethodInfo objectGetType = typeof(object).GetRuntimeMethod("GetType", Type.EmptyTypes);
        static readonly MethodInfo getTypeHandle = typeof(Type).GetRuntimeProperty("TypeHandle").GetGetMethod();

        static readonly MethodInfo intIntKeyValuePairGetKey = typeof(KeyValuePair<int, int>).GetRuntimeProperty("Key").GetGetMethod();
        static readonly MethodInfo intIntKeyValuePairGetValue = typeof(KeyValuePair<int, int>).GetRuntimeProperty("Value").GetGetMethod();

        static readonly ConstructorInfo invalidOperationExceptionConstructor = typeof(System.InvalidOperationException).GetTypeInfo().DeclaredConstructors.First(x => { var p = x.GetParameters(); return p.Length == 1 && p[0].ParameterType == typeof(string); });
        static readonly ConstructorInfo objectCtor = typeof(object).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 0);

        static class MessagePackBinaryTypeInfo
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
    }
}

namespace MessagePack.Internal
{
    // RuntimeTypeHandle can embed directly by OpCodes.Ldtoken
    // It does not implements IEquatable<T>(but GetHashCode and Equals is implemented) so needs this to avoid boxing.
    public class RuntimeTypeHandleEqualityComparer : IEqualityComparer<RuntimeTypeHandle>
    {
        public static IEqualityComparer<RuntimeTypeHandle> Default = new RuntimeTypeHandleEqualityComparer();

        RuntimeTypeHandleEqualityComparer()
        {

        }

        public bool Equals(RuntimeTypeHandle x, RuntimeTypeHandle y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(RuntimeTypeHandle obj)
        {
            return obj.GetHashCode();
        }
    }

    internal class MessagePackDynamicUnionResolverException : Exception
    {
        public MessagePackDynamicUnionResolverException(string message)
            : base(message)
        {

        }
    }
}