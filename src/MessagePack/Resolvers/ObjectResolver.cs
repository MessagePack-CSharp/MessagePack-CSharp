using System;
using MessagePack.Formatters;
using MessagePack.Internal;
using System.Reflection;
using System.Reflection.Emit;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// ObjectResolver by dynamic code generation.
    /// </summary>
    public class ObjectResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new ObjectResolver();

        const string ModuleName = "MessagePack.Resolvers.ObjectResolver";

        static readonly DynamicAssembly assembly;

        ObjectResolver()
        {

        }

        static ObjectResolver()
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
                formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterTypeInfo.AsType());
            }
        }

        static TypeInfo BuildType(Type type)
        {
            var ti = type.GetTypeInfo();

            var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
            var typeBuilder = assembly.ModuleBuilder.DefineType("MessagePack.Formatters." + type.FullName.Replace(".", "_") + "Formatter", TypeAttributes.Public, null, new[] { formatterType });

            // TODO: MakeTypeReflection


            // int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver);
            {
                var method = typeBuilder.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(int),
                    new Type[] { typeof(byte[]).MakeByRefType(), typeof(int), type, typeof(IFormatterResolver) });

                var il = method.GetILGenerator();
                BuildSerialize(type, il);
            }

            // T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);
            {
                var method = typeBuilder.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    type,
                    new Type[] { typeof(byte[]), typeof(int), typeof(IFormatterResolver), typeof(int).MakeByRefType() });

                var il = method.GetILGenerator();
                BuildDeserialize(type, il);
            }

            return typeBuilder.CreateTypeInfo();
        }

        // TODO:...
        static void BuildSerialize(Type type, ILGenerator il)
        {
            il.Emit(OpCodes.Ret);
        }

        // TODO:...
        static void BuildDeserialize(Type type, ILGenerator il)
        {
            // getMap
            // getLength
            // for(...){

            // var type = TryReadType();
            // if(type.IsInteger() -> ReadInt -> switch() case... field = getValue...;
            // else if(type == string) -> ReadString -> switch() case... field = getValue...;

            // constructor matching
            // new()... set...
            // return...



            il.Emit(OpCodes.Ret);
        }
    }
}

namespace MessagePack.Internal
{
    // [SerializationConstructor]
    internal class ObjectSerializationInfo
    {
        public bool IsIntKey { get; set; }
        public bool IsStringKey { get { return !IsIntKey; } }
        public bool IsClass { get; set; }
        public bool IsStruct { get { return !IsClass; } }
        public ConstructorInfo BestmatchConstructor { get; set; }

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

            if (contractAttr.KeyAsPropertyName)
            {
                // Opt-out: All public members are serialize target except [Ignore] member.


            }
            else
            {
                // Opt-in: Only KeyAttribute members

            }

            //var properties = type.GetRuntimeProperties();
            //var fields = type.GetRuntimeFields();
            throw new NotImplementedException();
        }


        public class Member
        {
            public bool IsProperty { get; set; }
            public bool IsField { get; set; }
            public bool IsWritable { get; set; }
            public bool IsReadable { get; set; }
        }
    }


    //public class MyClass
    //{
    //    public int MyProperty { get; set; }
    //    public string MyProperty2 { get; set; }
    //}

    //public class MyClassFormatter : IMessagePackFormatter<MyClass>
    //{
    //    public int Serialize(ref byte[] bytes, int offset, MyClass value, IFormatterResolver formatterResolver)
    //    {
    //        var startOffset = offset;
    //        offset += MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 2); // optimize 0~15 count
    //        offset += MessagePackBinary.WritePositiveFixedIntUnsafe(ref bytes, offset, 1); // optimize 0~127 key.
    //        offset += formatterResolver.GetFormatterWithVerify<int>().Serialize(ref bytes, offset, value.MyProperty, formatterResolver);

    //        return offset - startOffset;
    //    }

    //    public MyClass Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
    //    {
    //        var startOffset = offset;
    //        var intKeyFormatter = formatterResolver.GetFormatterWithVerify<int>();
    //        var stringKeyFormatter = formatterResolver.GetFormatterWithVerify<string>();
    //        var length = MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
    //        offset += readSize;

    //        int __MyProperty1__ = default(int);
    //        string __MyProperty2__ = default(string);

    //        // pattern of integer key.
    //        for (int i = 0; i < length; i++)
    //        {
    //            var key = intKeyFormatter.Deserialize(bytes, offset, formatterResolver, out readSize);
    //            offset += readSize;

    //            switch (key)
    //            {
    //                case 0:
    //                    __MyProperty1__ = formatterResolver.GetFormatterWithVerify<int>().Deserialize(bytes, offset, formatterResolver, out readSize);
    //                    break;
    //                case 1:
    //                    __MyProperty2__ = formatterResolver.GetFormatterWithVerify<string>().Deserialize(bytes, offset, formatterResolver, out readSize);
    //                    break;
    //                default:
    //                    break;
    //            }

    //            offset += readSize;
    //        }

    //        // pattern of string key
    //        // TODO:Dictionary Switch... Dictionary<string, int> and use same above code...

    //        // finish readSize
    //        readSize = offset - startOffset;

    //        var __result__ = new MyClass(); // use constructor(with argument?)
    //        __result__.MyProperty = __MyProperty1__;
    //        __result__.MyProperty2 = __MyProperty2__;
    //        return __result__;
    //    }
    //}
}