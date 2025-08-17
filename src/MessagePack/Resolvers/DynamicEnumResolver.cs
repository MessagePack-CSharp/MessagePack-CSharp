// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        private static readonly DynamicAssemblyFactory DynamicAssemblyFactory;

        private static int nameSequence = 0;

        private DynamicEnumResolver()
        {
        }

        static DynamicEnumResolver()
        {
            DynamicAssemblyFactory = new DynamicAssemblyFactory(ModuleName);
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

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            static FormatterCache()
            {
                Type type = typeof(T);
                if (type.IsNullable())
                {
                    // build underlying type and use wrapped formatter.
                    type = type.GenericTypeArguments[0];
                    if (!type.IsEnum)
                    {
                        return;
                    }

                    var innerFormatter = DynamicEnumResolver.Instance.GetFormatterDynamic(type);
                    if (innerFormatter == null)
                    {
                        return;
                    }

                    Formatter = (IMessagePackFormatter<T>?)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(type), new object[] { innerFormatter });
                    return;
                }
                else if (!type.IsEnum)
                {
                    return;
                }

                TypeInfo formatterTypeInfo = BuildType(type, allowPrivate: false);
                Formatter = (IMessagePackFormatter<T>?)Activator.CreateInstance(formatterTypeInfo);
            }
        }

        private static TypeInfo BuildType(Type enumType, bool allowPrivate)
        {
            Type underlyingType = Enum.GetUnderlyingType(enumType);
            Type formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(enumType);

            MessagePackEventSource.Instance.FormatterDynamicallyGeneratedStart();
            try
            {
                using (MonoProtection.EnterRefEmitLock())
                {
                    TypeBuilder typeBuilder = DynamicAssemblyFactory.GetDynamicAssembly(enumType, allowPrivate).DefineType("MessagePack.Formatters." + enumType.FullName!.Replace(".", "_") + "Formatter" + Interlocked.Increment(ref nameSequence), TypeAttributes.Public | TypeAttributes.Sealed, null, new[] { formatterType });

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
                        il.Emit(OpCodes.Call, typeof(MessagePackWriter).GetRuntimeMethod(nameof(MessagePackWriter.Write), new[] { underlyingType })!);
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
                        il.Emit(OpCodes.Call, typeof(MessagePackReader).GetRuntimeMethod("Read" + underlyingType.Name, Type.EmptyTypes)!);
                        il.Emit(OpCodes.Ret);
                    }

                    return typeBuilder.CreateTypeInfo()!;
                }
            }
            finally
            {
                MessagePackEventSource.Instance.FormatterDynamicallyGeneratedStop(enumType);
            }
        }
    }
}
