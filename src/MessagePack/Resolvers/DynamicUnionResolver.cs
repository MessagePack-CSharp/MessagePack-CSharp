using System;
using System.Linq;
using MessagePack.Formatters;
using MessagePack.Internal;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Buffers;

namespace MessagePack.Resolvers
{
#if !UNITY_WSA
#if !NET_STANDARD_2_0

    /// <summary>
    /// UnionResolver by dynamic code generation.
    /// </summary>
    public sealed class DynamicUnionResolver : IFormatterResolver
    {
        public static readonly DynamicUnionResolver Instance = new DynamicUnionResolver();

        const string ModuleName = "MessagePack.Resolvers.DynamicUnionResolver";

        static readonly DynamicAssembly assembly;
#if !UNITY
        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled);
#else
        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+");
#endif

        static int nameSequence = 0;

        DynamicUnionResolver()
        {

        }

        static DynamicUnionResolver()
        {
            assembly = new DynamicAssembly(ModuleName);
        }

#if NETFRAMEWORK
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
            if (!ti.IsInterface && !ti.IsAbstract)
            {
                throw new MessagePackDynamicUnionResolverException("Union can only be interface or abstract class. Type:" + type.Name);
            }

            var checker1 = new HashSet<int>();
            var checker2 = new HashSet<Type>();
            foreach (var item in unionAttrs)
            {
                if (!checker1.Add(item.Key)) throw new MessagePackDynamicUnionResolverException("Same union key has found. Type:" + type.Name + " Key:" + item.Key);
                if (!checker2.Add(item.SubType)) throw new MessagePackDynamicUnionResolverException("Same union subType has found. Type:" + type.Name + " SubType: " + item.SubType);
            }

            var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
            var typeBuilder = assembly.DefineType("MessagePack.Formatters." + SubtractFullNameRegex.Replace(type.FullName, "").Replace(".", "_") + "Formatter" + +Interlocked.Increment(ref nameSequence), TypeAttributes.Public | TypeAttributes.Sealed, null, new[] { formatterType });

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
                var method = typeBuilder.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                    null,
                    new Type[] { typeof(MessagePackWriter).MakeByRefType(), type, typeof(IFormatterResolver) });

                var il = method.GetILGenerator();
                BuildSerialize(type, unionAttrs, method, typeToKeyAndJumpMap, il);
            }
            {
                var method = typeBuilder.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                    type,
                    new Type[] { refMessagePackReader, typeof(IFormatterResolver) });

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


        // void Serialize([arg:1]MessagePackWriter writer, [arg:2]T value, [arg:3]IFormatterResolver formatterResolver);
        static void BuildSerialize(Type type, UnionAttribute[] infos, MethodBuilder method, FieldBuilder typeToKeyAndJumpMap, ILGenerator il)
        {
            // if(value == null) return WriteNil
            var elseBody = il.DefineLabel();
            var notFoundType = il.DefineLabel();

            il.EmitLdarg(2);
            il.Emit(OpCodes.Brtrue_S, elseBody);
            il.Emit(OpCodes.Br, notFoundType);
            il.MarkLabel(elseBody);

            var keyPair = il.DeclareLocal(typeof(KeyValuePair<int, int>));

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
                il.EmitLdarg(3);
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

        // T Deserialize([arg:1]ref MessagePackReader reader, [arg:2]IFormatterResolver formatterResolver);
        static void BuildDeserialize(Type type, UnionAttribute[] infos, MethodBuilder method, FieldBuilder keyToJumpMap, ILGenerator il)
        {
            // if(MessagePackBinary.TryReadNil()) { return null; }
            var falseLabel = il.DefineLabel();
            il.EmitLdarg(1);
            il.EmitCall(MessagePackReaderTypeInfo.TryReadNil);
            il.Emit(OpCodes.Brfalse_S, falseLabel);

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);

            // read-array header and validate, reader.ReadArrayHeader() != 2) throw;
            il.MarkLabel(falseLabel);

            var rightLabel = il.DefineLabel();
            var writer = new ArgumentField(il, 1);
            writer.EmitLdarg();
            il.EmitCall(MessagePackReaderTypeInfo.ReadArrayHeader);
            il.EmitLdc_I4(2);
            il.Emit(OpCodes.Beq_S, rightLabel);
            il.Emit(OpCodes.Ldstr, "Invalid Union data was detected. Type:" + type.FullName);
            il.Emit(OpCodes.Newobj, invalidOperationExceptionConstructor);
            il.Emit(OpCodes.Throw);

            il.MarkLabel(rightLabel);

            // read key
            var key = il.DeclareLocal(typeof(int));
            writer.EmitLdarg();
            il.EmitCall(MessagePackReaderTypeInfo.ReadInt32);
            il.EmitStloc(key);

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
            writer.EmitLdarg();
            il.EmitCall(MessagePackReaderTypeInfo.Skip);
            il.Emit(OpCodes.Br, loopEnd);

            foreach (var item in switchLabels)
            {
                il.MarkLabel(item.Label);
                il.EmitLdarg(2);
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

        static bool IsZeroStartSequential(UnionAttribute[] infos)
        {
            for (int i = 0; i < infos.Length; i++)
            {
                if (infos[i].Key != i) return false;
            }
            return true;
        }

        // EmitInfos...

        static readonly Type refMessagePackReader = typeof(MessagePackReader).MakeByRefType();
        static readonly Type refKvp = typeof(KeyValuePair<int, int>).MakeByRefType();
        static readonly MethodInfo getFormatterWithVerify = typeof(FormatterResolverExtensions).GetRuntimeMethods().First(x => x.Name == "GetFormatterWithVerify");

        static readonly Func<Type, MethodInfo> getSerialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod("Serialize", new[] { typeof(MessagePackWriter).MakeByRefType(), t, typeof(IFormatterResolver) });
        static readonly Func<Type, MethodInfo> getDeserialize = t => typeof(IMessagePackFormatter<>).MakeGenericType(t).GetRuntimeMethod("Deserialize", new[] { typeof(MessagePackReader).MakeByRefType(), typeof(IFormatterResolver) });

        static readonly FieldInfo runtimeTypeHandleEqualityComparer = typeof(RuntimeTypeHandleEqualityComparer).GetRuntimeField("Default");
        static readonly ConstructorInfo intIntKeyValuePairConstructor = typeof(KeyValuePair<int, int>).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 2);
        static readonly ConstructorInfo typeMapDictionaryConstructor = typeof(Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>).GetTypeInfo().DeclaredConstructors.First(x => { var p = x.GetParameters(); return p.Length == 2 && p[0].ParameterType == typeof(int); });
        static readonly MethodInfo typeMapDictionaryAdd = typeof(Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>).GetRuntimeMethod("Add", new[] { typeof(RuntimeTypeHandle), typeof(KeyValuePair<int, int>) });
        static readonly MethodInfo typeMapDictionaryTryGetValue = typeof(Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>).GetRuntimeMethod("TryGetValue", new[] { typeof(RuntimeTypeHandle), refKvp });

        static readonly ConstructorInfo keyMapDictionaryConstructor = typeof(Dictionary<int, int>).GetTypeInfo().DeclaredConstructors.First(x => { var p = x.GetParameters(); return p.Length == 1 && p[0].ParameterType == typeof(int); });
        static readonly MethodInfo keyMapDictionaryAdd = typeof(Dictionary<int, int>).GetRuntimeMethod("Add", new[] { typeof(int), typeof(int) });
        static readonly MethodInfo keyMapDictionaryTryGetValue = typeof(Dictionary<int, int>).GetRuntimeMethod("TryGetValue", new[] { typeof(int), typeof(int).MakeByRefType() });

        static readonly MethodInfo objectGetType = typeof(object).GetRuntimeMethod("GetType", Type.EmptyTypes);
        static readonly MethodInfo getTypeHandle = typeof(Type).GetRuntimeProperty("TypeHandle").GetGetMethod();

        static readonly MethodInfo intIntKeyValuePairGetKey = typeof(KeyValuePair<int, int>).GetRuntimeProperty("Key").GetGetMethod();
        static readonly MethodInfo intIntKeyValuePairGetValue = typeof(KeyValuePair<int, int>).GetRuntimeProperty("Value").GetGetMethod();

        static readonly ConstructorInfo invalidOperationExceptionConstructor = typeof(System.InvalidOperationException).GetTypeInfo().DeclaredConstructors.First(x => { var p = x.GetParameters(); return p.Length == 1 && p[0].ParameterType == typeof(string); });
        static readonly ConstructorInfo objectCtor = typeof(object).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 0);

        static class MessagePackReaderTypeInfo
        {
            public static TypeInfo ReaderTypeInfo = typeof(MessagePackReader).GetTypeInfo();

            public static MethodInfo ReadBytes = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadBytes), Type.EmptyTypes);
            public static MethodInfo ReadInt32 = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadInt32), Type.EmptyTypes);
            public static MethodInfo ReadString = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadString), Type.EmptyTypes);
            public static MethodInfo TryReadNil = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.TryReadNil), Type.EmptyTypes);
            public static MethodInfo Skip = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.Skip), Type.EmptyTypes);
            public static MethodInfo ReadArrayHeader = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadArrayHeader), Type.EmptyTypes);
            public static MethodInfo ReadMapHeader = typeof(MessagePackReader).GetRuntimeMethod(nameof(MessagePackReader.ReadMapHeader), Type.EmptyTypes);
        }

        static class MessagePackWriterTypeInfo
        {
            public static TypeInfo WriterTypeInfo = typeof(MessagePackWriter).GetTypeInfo();

            public static MethodInfo WriteArrayHeader = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.WriteArrayHeader), new[] { typeof(int) });
            public static MethodInfo WriteInt32 = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.Write), new[] { typeof(int) });
            public static MethodInfo WriteNil = typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.WriteNil), Type.EmptyTypes);
        }
    }

#endif
#endif
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

