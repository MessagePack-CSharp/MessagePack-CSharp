// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using MessagePack.Formatters;
using MessagePack.Internal;

namespace MessagePack.Resolvers
{
    public sealed class DynamicEnumAsStringIgnoreCaseResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly DynamicEnumAsStringIgnoreCaseResolver Instance = new();

        private DynamicEnumAsStringIgnoreCaseResolver()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            private static readonly object?[] FormatterCtorArgs = new object?[] { true };

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

                    var innerFormatter = Instance.GetFormatterDynamic(type);
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

                Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(EnumAsStringFormatter<>).MakeGenericType(typeof(T)), FormatterCtorArgs)!;
            }
        }
    }
}
