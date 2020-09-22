// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public sealed class DynamicUnsafeUnmanagedStructResolver : IFormatterResolver
    {
        public static readonly DynamicUnsafeUnmanagedStructResolver Instance = new DynamicUnsafeUnmanagedStructResolver();

        private static readonly MethodInfo BaseIsReferenceOrContainsReferences = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.IsReferenceOrContainsReferences)) ?? throw new NullReferenceException();
        private static readonly Type BaseFormatterCache = typeof(FormatterCache<>);
        private static readonly Type BaseStruct = typeof(UnsafeUnmanagedStructFormatter<>);
        private static readonly Type BaseArray = typeof(UnsafeUnmanagedStructArrayFormatter<>);

        [ThreadStatic] private static Type[]? one;

        static DynamicUnsafeUnmanagedStructResolver()
        {
            var currentDomain = AppDomain.CurrentDomain;
            var assemblies = currentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                ProcessAssembly(assembly);
            }

            currentDomain.AssemblyLoad += CurrentDomainOnAssemblyLoad;
        }

        private DynamicUnsafeUnmanagedStructResolver()
        {
        }

        private static void CurrentDomainOnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
        {
            ProcessAssembly(args.LoadedAssembly);
        }

        private static void ProcessAssembly(Assembly assembly)
        {
            one ??= new Type[1];

            foreach (var module in assembly.Modules)
            {
                foreach (var attribute in module.GetCustomAttributes<UnsafeUnmanagedStructAttribute>())
                {
                    if (attribute.Kind == 0)
                    {
                        continue;
                    }

                    var type = attribute.Type;
                    one[0] = type;
                    var isReferenceOrContainsReferences = (bool)BaseIsReferenceOrContainsReferences.MakeGenericMethod(one).Invoke(null, null)!;
                    if (isReferenceOrContainsReferences)
                    {
                        throw new MessagePackSerializationException("UnsafeUnmanagedStructAttribute target type should be unmanaged type! type: " + type);
                    }

                    if ((attribute.Kind & UnsafeUnmanagedStructFormatterImplementationKind.Self) != 0)
                    {
                        var instance = BaseStruct.MakeGenericType(one).GetField("Instance")!.GetValue(default);
                        BaseFormatterCache.MakeGenericType(one).GetField("Formatter")!.SetValue(null, instance);
                    }

                    if ((attribute.Kind & UnsafeUnmanagedStructFormatterImplementationKind.Array) != 0)
                    {
                        var instance = BaseArray.MakeGenericType(one).GetField("Instance")!.GetValue(default);
                        one[0] = type.MakeArrayType();
                        BaseFormatterCache.MakeGenericType(one).GetField("Formatter")!.SetValue(null, instance);
                    }
                }
            }
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
#pragma warning disable SA1401 // Fields should be private
            public static IMessagePackFormatter<T>? Formatter = default;
#pragma warning restore SA1401 // Fields should be private
        }
    }
}
