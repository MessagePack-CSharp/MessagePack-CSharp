// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using MessagePack.Formatters;

namespace MessagePack.ImmutableCollection
{
    public class ImmutableCollectionResolver : IFormatterResolver
    {
        public static readonly ImmutableCollectionResolver Instance = new ImmutableCollectionResolver();

        private ImmutableCollectionResolver()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            internal static readonly IMessagePackFormatter<T>? Formatter;

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>?)ImmutableCollectionGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }

    internal static class ImmutableCollectionGetFormatterHelper
    {
        private static readonly Dictionary<Type, Type> FormatterMap = new Dictionary<Type, Type>()
        {
            { typeof(ImmutableArray<>), typeof(ImmutableArrayFormatter<>) },
            { typeof(ImmutableList<>), typeof(ImmutableListFormatter<>) },
            { typeof(ImmutableDictionary<,>), typeof(ImmutableDictionaryFormatter<,>) },
            { typeof(ImmutableHashSet<>), typeof(ImmutableHashSetFormatter<>) },
            { typeof(ImmutableSortedDictionary<,>), typeof(ImmutableSortedDictionaryFormatter<,>) },
            { typeof(ImmutableSortedSet<>), typeof(ImmutableSortedSetFormatter<>) },
            { typeof(ImmutableQueue<>), typeof(ImmutableQueueFormatter<>) },
            { typeof(ImmutableStack<>), typeof(ImmutableStackFormatter<>) },
            { typeof(IImmutableList<>), typeof(InterfaceImmutableListFormatter<>) },
            { typeof(IImmutableDictionary<,>), typeof(InterfaceImmutableDictionaryFormatter<,>) },
            { typeof(IImmutableQueue<>), typeof(InterfaceImmutableQueueFormatter<>) },
            { typeof(IImmutableSet<>), typeof(InterfaceImmutableSetFormatter<>) },
            { typeof(IImmutableStack<>), typeof(InterfaceImmutableStackFormatter<>) },
        };

        internal static object? GetFormatter(Type t)
        {
            TypeInfo ti = t.GetTypeInfo();

            if (ti.IsGenericType)
            {
                Type genericType = ti.GetGenericTypeDefinition();
                TypeInfo genericTypeInfo = genericType.GetTypeInfo();
                var isNullable = genericTypeInfo.IsNullable();
                Type? nullableElementType = isNullable ? ti.GenericTypeArguments[0] : null;

                if (FormatterMap.TryGetValue(genericType, out Type? formatterType))
                {
                    return CreateInstance(formatterType, ti.GenericTypeArguments);
                }
                else if (isNullable && nullableElementType?.IsConstructedGenericType is true && nullableElementType.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
                {
                    return CreateInstance(typeof(NullableFormatter<>), new[] { nullableElementType });
                }
            }

            return null;
        }

        private static object? CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
        {
            return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
        }
    }

    internal static class ReflectionExtensions
    {
        public static bool IsNullable(this System.Reflection.TypeInfo type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Nullable<>);
        }
    }
}
