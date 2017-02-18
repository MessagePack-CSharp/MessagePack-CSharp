using System;
using System.Linq;
using MessagePack.Formatters;
using MessagePack.Internal;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// ObjectResolver by dynamic code generation.
    /// </summary>
    public class DynamicObjectResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new DynamicObjectResolver();

        const string ModuleName = "MessagePack.Resolvers.DynamicObjectResolver";

        static readonly DynamicAssembly assembly;

        DynamicObjectResolver()
        {

        }

        static DynamicObjectResolver()
        {
            assembly = new DynamicAssembly(ModuleName);
        }

        IMessagePackFormatter<T> IFormatterResolver.GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                // TODO:Nullable Struct

                var formatterTypeInfo = BuildType(typeof(T));
                if (formatterTypeInfo == null) return;

                formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterTypeInfo.AsType());
            }
        }

        static TypeInfo BuildType(Type type)
        {
            var serializationInfo = MessagePack.Internal.ObjectSerializationInfo.CreateOrNull(type);
            if (serializationInfo == null) return null;

            var ti = type.GetTypeInfo();

            var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
            var typeBuilder = assembly.ModuleBuilder.DefineType("MessagePack.Formatters." + type.FullName.Replace(".", "_") + "Formatter", TypeAttributes.Public, null, new[] { formatterType });

            // int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver);
            {
                var method = typeBuilder.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(int),
                    new Type[] { typeof(byte[]).MakeByRefType(), typeof(int), type, typeof(IFormatterResolver) });

                var il = method.GetILGenerator();
                BuildSerialize(type, serializationInfo, method, il);
            }

            // T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);
            {
                var method = typeBuilder.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    type,
                    new Type[] { typeof(byte[]), typeof(int), typeof(IFormatterResolver), typeof(int).MakeByRefType() });

                var il = method.GetILGenerator();
                BuildDeserialize(type, serializationInfo, method, il);
            }

            return typeBuilder.CreateTypeInfo();
        }

        // int Serialize([arg:1]ref byte[] bytes, [arg:2]int offset, [arg:3]T value, [arg:4]IFormatterResolver formatterResolver);
        static void BuildSerialize(Type type, ObjectSerializationInfo info, MethodBuilder method, ILGenerator il)
        {
            var writeCount = info.Members.Count(x => x.IsReadable);

            // var startOffset = offset;
            var startOffsetLocal = il.DeclareLocal(typeof(int)); // [loc:0]
            il.EmitLdarg(2);
            il.EmitStloc(startOffsetLocal);

            // offset += writeHeader
            EmitOffsetPlusEqual(il, null, () =>
             {
                 il.EmitLdc_I4(writeCount);
                 if (writeCount <= MessagePackRange.MaxFixMapCount)
                 {
                     il.EmitCall(MessagePackBinaryMethodInfo.WriteFixedMapHeaderUnsafe);
                 }
                 else
                 {
                     il.EmitCall(MessagePackBinaryMethodInfo.WriteMapHeader);
                 }
             });

            foreach (var item in info.Members)
            {
                // offset += writekey
                EmitOffsetPlusEqual(il, null, () =>
                 {
                     if (info.IsIntKey)
                     {
                         il.EmitLdc_I4(item.IntKey);
                         if (0 <= item.IntKey && item.IntKey <= MessagePackRange.MaxFixPositiveInt)
                         {
                             il.EmitCall(MessagePackBinaryMethodInfo.WritePositiveFixedIntUnsafe);
                         }
                         else
                         {
                             il.EmitCall(MessagePackBinaryMethodInfo.WriteInt32);
                         }
                     }
                     else
                     {
                         // TODO:string key
                     }
                 });

                // offset += serializeValue
                EmitSerializeValue(il, item);
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

        static void EmitSerializeValue(ILGenerator il, ObjectSerializationInfo.EmittableMember member)
        {
            var t = member.Type;
            if (t == typeof(Int32))
            {
                EmitOffsetPlusEqual(il, () => il.EmitLdsfld(StaticFormatterTypeInfo.Int32), () =>
                {
                    il.EmitLdarg(3);
                    member.EmitLoadValue(il);
                    il.EmitCall(StaticFormatterTypeInfo.Int32Serialize);
                });
            }

            // TODO:others...
        }

        // T Deserialize([arg:1]byte[] bytes, [arg:2]int offset, [arg:3]IFormatterResolver formatterResolver, [arg:4]out int readSize);
        static void BuildDeserialize(Type type, ObjectSerializationInfo info, MethodBuilder method, ILGenerator il)
        {
            // var startOffset = offset;
            var startOffsetLocal = il.DeclareLocal(typeof(int)); // [loc:0]
            il.EmitLdarg(2);
            il.EmitStloc(startOffsetLocal);

            // var length = ReadMapHeader
            var length = il.DeclareLocal(typeof(int)); // [loc:1]
            il.EmitLdarg(1);
            il.EmitLdarg(2);
            il.EmitLdarg(4);
            il.EmitCall(MessagePackBinaryMethodInfo.ReadMapHeader);
            il.EmitStloc(length);
            EmitOffsetPlusReadSize(il);

            // make local fields
            var intDictionary = new Dictionary<int, DeserializeInfo>();
            foreach (var item in info.Members.Where(x => x.IsWritable))
            {
                // TODO:string key?
                intDictionary.Add(item.IntKey, new DeserializeInfo
                {
                    MemberInfo = item,
                    LocalField = il.DeclareLocal(item.Type),
                    SwitchLabel = il.DefineLabel()
                });
            }

            // Read Loop(for var i = 0; i< length; i++)
            if (info.IsIntKey)
            {
                var key = il.DeclareLocal(typeof(int));
                var switchEnd = il.DefineLabel();
                il.EmitIncrementFor(length, forILocal =>
                {
                    // key = Deserialize, offset += readSize;
                    il.EmitLdsfld(StaticFormatterTypeInfo.Int32);
                    il.EmitLdarg(1);
                    il.EmitLdarg(2);
                    il.EmitLdarg(4);
                    il.EmitCall(StaticFormatterTypeInfo.Int32Deserialize);
                    il.EmitStloc(key);
                    EmitOffsetPlusReadSize(il);

                    // switch... local = Deserialize
                    var defaultLabel = il.DefineLabel();
                    il.EmitLdloc(key);
                    il.Emit(OpCodes.Switch, intDictionary.Values.Select(x => x.SwitchLabel).ToArray());
                    foreach (var item in intDictionary)
                    {
                        il.MarkLabel(item.Value.SwitchLabel);
                        EmitDeserializeValue(il, item.Value);
                        il.Emit(OpCodes.Br, switchEnd);
                    }
                    il.MarkLabel(defaultLabel);
                    il.Emit(OpCodes.Br, switchEnd);

                    // offset += readSize
                    il.MarkLabel(switchEnd);
                    EmitOffsetPlusReadSize(il);
                });
            }
            else
            {
                // TODO:string key
            }

            // finish readSize: readSize = offset - startOffset;
            il.EmitLdarg(4);
            il.EmitLdarg(2);
            il.EmitLdloc(startOffsetLocal);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Stind_I4);

            // create result object
            // TODO:string values
            EmitNewObject(il, info, intDictionary.Values.ToArray());

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
            if (t == typeof(Int32))
            {
                il.EmitLdsfld(StaticFormatterTypeInfo.Int32);
                il.EmitLdarg(1);
                il.EmitLdarg(2);
                il.EmitLdarg(4);
                il.EmitCall(StaticFormatterTypeInfo.Int32Deserialize);
            }

            // TODO:others...

            il.EmitStloc(info.LocalField);
        }

        static void EmitNewObject(ILGenerator il, ObjectSerializationInfo info, DeserializeInfo[] members)
        {
            // TODO:with argument?
            il.Emit(OpCodes.Newobj, info.BestmatchConstructor);

            foreach (var item in members)
            {
                il.Emit(OpCodes.Dup);
                if (item.MemberInfo.IsWritable)
                {
                    il.EmitLdloc(item.LocalField);
                    item.MemberInfo.EmitStoreValue(il);
                }
            }
        }

        // EmitInfos...

        static Type refByte = typeof(byte[]).MakeByRefType();
        static Type refInt = typeof(int).MakeByRefType();

        static class MessagePackBinaryMethodInfo
        {
            public static MethodInfo WriteFixedMapHeaderUnsafe = typeof(MessagePackBinary).GetRuntimeMethod("WriteFixedMapHeaderUnsafe", new[] { refByte, typeof(int), typeof(int) });
            public static MethodInfo WriteMapHeader = typeof(MessagePackBinary).GetRuntimeMethod("WriteMapHeader", new[] { refByte, typeof(int), typeof(int) });
            public static MethodInfo WritePositiveFixedIntUnsafe = typeof(MessagePackBinary).GetRuntimeMethod("WritePositiveFixedIntUnsafe", new[] { refByte, typeof(int), typeof(int) });
            public static MethodInfo WriteInt32 = typeof(MessagePackBinary).GetRuntimeMethod("WriteInt32", new[] { refByte, typeof(int), typeof(int) });

            public static MethodInfo ReadMapHeader = typeof(MessagePackBinary).GetRuntimeMethod("ReadMapHeader", new[] { typeof(byte[]), typeof(int), refInt });

            static MessagePackBinaryMethodInfo()
            {

            }
        }

        static class StaticFormatterTypeInfo
        {
            public static FieldInfo Int32 = typeof(Int32Formatter).GetRuntimeField("Instance");
            public static MethodInfo Int32Serialize = typeof(Int32Formatter).GetRuntimeMethod("Serialize", new[] { refByte, typeof(int), typeof(int) });
            public static MethodInfo Int32Deserialize = typeof(Int32Formatter).GetRuntimeMethod("Deserialize", new[] { typeof(byte[]), typeof(int), typeof(int).MakeByRefType() });
        }

        class DeserializeInfo
        {
            public ObjectSerializationInfo.EmittableMember MemberInfo { get; set; }
            public LocalBuilder LocalField { get; set; }
            public Label SwitchLabel { get; set; }
        }
    }
}

namespace MessagePack.Internal
{
    public class ObjectSerializationInfo
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

        public static ObjectSerializationInfo CreateOrNull(Type type)
        {
            var ti = type.GetTypeInfo();

            var contractAttr = ti.GetCustomAttribute<MessagePackObjectAttribute>();
            if (contractAttr == null)
            {
                return null;
            }

            var isIntKey = true;
            var intMemebrs = new Dictionary<int, EmittableMember>();
            var stringMembers = new Dictionary<string, EmittableMember>();

            if (contractAttr.KeyAsPropertyName)
            {
                // Opt-out: All public members are serialize target except [Ignore] member.
                isIntKey = false;

                foreach (var item in type.GetRuntimeProperties())
                {
                    if (item.GetCustomAttribute<IgnoreAttribute>(true) != null) continue;

                    var member = new EmittableMember
                    {
                        PropertyInfo = item,
                        IsReadable = (item.GetMethod != null) && item.GetMethod.IsPublic,
                        IsWritable = (item.SetMethod != null) && item.SetMethod.IsPublic,
                        StringKey = item.Name
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;

                    stringMembers.Add(member.StringKey, member);
                }
                foreach (var item in type.GetRuntimeFields())
                {
                    if (item.GetCustomAttribute<IgnoreAttribute>(true) != null) continue;
                    if (item.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>(true) != null) continue;

                    var member = new EmittableMember
                    {
                        FieldInfo = item,
                        IsReadable = item.IsPublic,
                        IsWritable = item.IsPublic && !item.IsInitOnly,
                        StringKey = item.Name
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;

                    stringMembers.Add(member.StringKey, member);
                }
            }
            else
            {
                // Opt-in: Only KeyAttribute members
                var searchFirst = false;

                foreach (var item in type.GetRuntimeProperties())
                {
                    if (item.GetCustomAttribute<IgnoreAttribute>(true) != null) continue;

                    var key = item.GetCustomAttribute<KeyAttribute>(true);
                    if (key == null) continue;

                    if (key.IntKey == null && key.StringKey == null) throw new MessagePackDynamicObjectResolverException("both IntKey and StringKey are null." + " type: " + type.FullName + " member:" + item.Name);

                    if (searchFirst)
                    {
                        isIntKey = key.IntKey != null;
                    }
                    else
                    {
                        if ((isIntKey && key.IntKey == null) || (!isIntKey && key.StringKey == null))
                        {
                            throw new MessagePackDynamicObjectResolverException("all members key type must be same." + " type: " + type.FullName + " member:" + item.Name);
                        }
                    }

                    var member = new EmittableMember
                    {
                        PropertyInfo = item,
                        IsReadable = (item.GetMethod != null) && item.GetMethod.IsPublic,
                        IsWritable = (item.SetMethod != null) && item.SetMethod.IsPublic,
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;

                    if (isIntKey)
                    {
                        member.IntKey = key.IntKey.Value;
                        if (intMemebrs.ContainsKey(member.IntKey)) throw new MessagePackDynamicObjectResolverException("key is duplicated, all members key must be unique." + " type: " + type.FullName + " member:" + item.Name);

                        intMemebrs.Add(member.IntKey, member);
                    }
                    else
                    {
                        member.StringKey = key.StringKey;
                        if (stringMembers.ContainsKey(member.StringKey)) throw new MessagePackDynamicObjectResolverException("key is duplicated, all members key must be unique." + " type: " + type.FullName + " member:" + item.Name);

                        stringMembers.Add(member.StringKey, member);
                    }
                }

                foreach (var item in type.GetRuntimeFields())
                {
                    if (item.GetCustomAttribute<IgnoreAttribute>(true) != null) continue;
                    if (item.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>(true) != null) continue;

                    if (item.GetCustomAttribute<IgnoreAttribute>(true) != null) continue;

                    var key = item.GetCustomAttribute<KeyAttribute>(true);
                    if (key == null) continue;

                    if (key.IntKey == null && key.StringKey == null) throw new MessagePackDynamicObjectResolverException("both IntKey and StringKey are null." + " type: " + type.FullName + " member:" + item.Name);

                    if (searchFirst)
                    {
                        isIntKey = key.IntKey != null;
                    }
                    else
                    {
                        if ((isIntKey && key.IntKey == null) || (!isIntKey && key.StringKey == null))
                        {
                            throw new MessagePackDynamicObjectResolverException("all members key type must be same." + " type: " + type.FullName + " member:" + item.Name);
                        }
                    }

                    var member = new EmittableMember
                    {
                        FieldInfo = item,
                        IsReadable = item.IsPublic,
                        IsWritable = item.IsPublic && !item.IsInitOnly,
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;

                    if (isIntKey)
                    {
                        member.IntKey = key.IntKey.Value;
                        if (intMemebrs.ContainsKey(member.IntKey)) throw new MessagePackDynamicObjectResolverException("key is duplicated, all members key must be unique." + " type: " + type.FullName + " member:" + item.Name);

                        intMemebrs.Add(member.IntKey, member);
                    }
                    else
                    {
                        member.StringKey = key.StringKey;
                        if (stringMembers.ContainsKey(member.StringKey)) throw new MessagePackDynamicObjectResolverException("key is duplicated, all members key must be unique." + " type: " + type.FullName + " member:" + item.Name);

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
            if (ctor == null) throw new MessagePackDynamicObjectResolverException("can't find public constructor. type:" + type.FullName);

            var constructorParameters = new List<EmittableMember>();
            var ctorParamIndex = 0;
            foreach (var item in ctor.GetParameters())
            {
                EmittableMember paramMember;
                if (isIntKey)
                {
                    if (intMemebrs.TryGetValue(ctorParamIndex, out paramMember))
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
                    if (stringMembers.TryGetValue(item.Name, out paramMember))
                    {
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

            return new ObjectSerializationInfo
            {
                IsClass = type.GetTypeInfo().IsClass,
                BestmatchConstructor = ctor,
                ConstructorParameters = constructorParameters.ToArray(),
                IsIntKey = isIntKey,
                Members = (isIntKey) ? intMemebrs.Values.ToArray() : stringMembers.Values.ToArray()
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

            public void EmitLoadValue(ILGenerator il)
            {
                if (IsProperty)
                {
                    il.EmitCallvirt(PropertyInfo.GetMethod);
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
                    il.EmitCallvirt(PropertyInfo.SetMethod);
                }
                else
                {
                    il.Emit(OpCodes.Stfld, FieldInfo);
                }
            }
        }
    }

    public class MessagePackDynamicObjectResolverException : Exception
    {
        public MessagePackDynamicObjectResolverException(string message)
            : base(message)
        {

        }
    }
}