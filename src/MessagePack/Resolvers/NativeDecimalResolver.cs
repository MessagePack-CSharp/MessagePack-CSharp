// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public sealed class NativeDecimalResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly NativeDecimalResolver Instance = new NativeDecimalResolver();

        private NativeDecimalResolver()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static object? GetFormatterHelper(Type t)
        {
            if (t == typeof(Decimal))
            {
                return NativeDecimalFormatter.Instance;
            }
            else if (t == typeof(Decimal?))
            {
                return new StaticNullableFormatter<Decimal>(NativeDecimalFormatter.Instance);
            }

            return null;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>?)GetFormatterHelper(typeof(T));
            }
        }
    }
}
