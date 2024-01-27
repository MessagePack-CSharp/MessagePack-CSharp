// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET8_0_OR_GREATER
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using MessagePack.Formatters;

#pragma warning disable SA1402 // File may only contain a single type

namespace MessagePack.FrozenCollection
{
    public class FrozenCollectionResolver : IFormatterResolver
    {
        public static readonly FrozenCollectionResolver Instance = new FrozenCollectionResolver();

        private FrozenCollectionResolver()
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
                Formatter = (IMessagePackFormatter<T>?)FrozenCollectionGetFormatterHelper.GetFormatter(typeof(T));
            }
        }

        private static class FrozenCollectionGetFormatterHelper
        {
            private static readonly Dictionary<Type, Type> FormatterMap = new()
            {
                { typeof(FrozenSet<>), typeof(FrozenSetFormatter<>) },
                { typeof(FrozenDictionary<,>), typeof(FrozenDictionaryFormatter<,>) },
            };

            internal static object? GetFormatter(Type t)
            {
                if (t.IsGenericType)
                {
                    var genericType = t.GetGenericTypeDefinition();
                    if (FormatterMap.TryGetValue(genericType, out var formatterType))
                    {
                        return CreateInstance(formatterType, t.GenericTypeArguments);
                    }
                }

                return null;
            }

            private static object? CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
            {
                return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
            }
        }
    }
}

#endif
