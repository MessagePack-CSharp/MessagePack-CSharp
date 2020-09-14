// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !(UNITY_2018_3_OR_NEWER && NET_STANDARD_2_0)

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

        private const string ModuleName = "MessagePack.Resolvers.DynamicEnumResolver";

        private static readonly Lazy<DynamicAssembly> DynamicAssembly;

        private static int nameSequence = 0;

        private DynamicEnumResolver()
        {
        }

        static DynamicEnumResolver()
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
                TypeInfo ti = typeof(T).GetTypeInfo();
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

                    Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(ti.AsType()), new object[] { innerFormatter });
                    return;
                }
                else if (!ti.IsEnum)
                {
                    return;
                }

                TypeInfo formatterTypeInfo = BuildType(typeof(T));
                Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(formatterTypeInfo.AsType());
            }
        }

        private static TypeInfo BuildType(Type enumType)
        {
            Type underlyingType = Enum.GetUnderlyingType(enumType);
            Type formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(enumType);

            using (MonoProtection.EnterRefEmitLock())
            {
                TypeBuilder typeBuilder = DynamicAssembly.Value.DefineType("MessagePack.Formatters." + enumType.FullName.Replace(".", "_") + "Formatter" + Interlocked.Increment(ref nameSequence), TypeAttributes.Public | TypeAttributes.Sealed, null, new[] { formatterType });

                // void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options);
                {
                    MethodBuilder method = typeBuilder.DefineMethod(
                        "Serialize",
                        MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                        null,
                        new Type[] { typeof(MessagePackWriter).MakeByRefType(), enumType, typeof(MessagePackSerializerOptions) });

                    ILGenerator il = method.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.Write), new[] { underlyingType }));
                    il.Emit(OpCodes.Ret);
                }

                // T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options);
                {
                    MethodBuilder method = typeBuilder.DefineMethod(
                        "Deserialize",
                        MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                        enumType,
                        new Type[] { typeof(MessagePackReader).MakeByRefType(), typeof(MessagePackSerializerOptions) });

                    ILGenerator il = method.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, typeof(MessagePackReader).GetRuntimeMethod("Read" + underlyingType.Name, Type.EmptyTypes));
                    il.Emit(OpCodes.Ret);
                }

                return typeBuilder.CreateTypeInfo();
            }
        }
    }
}

#endif
