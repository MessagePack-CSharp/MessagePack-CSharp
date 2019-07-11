#if !UNITY_WSA

using System;
using System.Buffers;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using MessagePack.Formatters;
using MessagePack.Internal;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// EnumResolver by dynamic code generation, serialized underlying type.
    /// </summary>
    public sealed class DynamicEnumResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly DynamicEnumResolver Instance = new DynamicEnumResolver();

        const string ModuleName = "MessagePack.Resolvers.DynamicEnumResolver";

        static readonly DynamicAssembly assembly;

        static int nameSequence = 0;

        DynamicEnumResolver()
        {

        }

        static DynamicEnumResolver()
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
                    // build underlying type and use wrapped formatter.
                    ti = ti.GenericTypeArguments[0].GetTypeInfo();
                    if (!ti.IsEnum)
                    {
                        return;
                    }

                    var innerFormatter = DynamicEnumResolver.Instance.GetFormatterDynamic(ti.AsType());
                    if (innerFormatter == null)
                    {
                        return;
                    }
                    formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }
                else if (!ti.IsEnum)
                {
                    return;
                }

                var formatterTypeInfo = BuildType(typeof(T));
                formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterTypeInfo.AsType());
            }
        }

        static TypeInfo BuildType(Type enumType)
        {
            var underlyingType = Enum.GetUnderlyingType(enumType);
            var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(enumType);

            var typeBuilder = assembly.DefineType("MessagePack.Formatters." + enumType.FullName.Replace(".", "_") + "Formatter" + Interlocked.Increment(ref nameSequence), TypeAttributes.Public | TypeAttributes.Sealed, null, new[] { formatterType });

            // void Serialize(ref MessagePackWriter writer, T value, IFormatterResolver resolver);
            {
                var method = typeBuilder.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    null,
                    new Type[] { typeof(MessagePackWriter).MakeByRefType(), enumType, typeof(IFormatterResolver) });

                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.Write), new[] { underlyingType }));
                il.Emit(OpCodes.Ret);
            }

            // T Deserialize(ref MessagePackReader reader, IFormatterResolver resolver);
            {
                var method = typeBuilder.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    enumType,
                    new Type[] { typeof(MessagePackReader).MakeByRefType(), typeof(IFormatterResolver) });

                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, typeof(MessagePackReader).GetRuntimeMethod("Read" + underlyingType.Name, Type.EmptyTypes));
                il.Emit(OpCodes.Ret);
            }

            return typeBuilder.CreateTypeInfo();
        }
    }
}

#endif
