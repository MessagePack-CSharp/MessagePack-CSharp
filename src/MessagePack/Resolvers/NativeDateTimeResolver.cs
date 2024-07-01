// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using MessagePack.Formatters;
using MessagePack.Internal;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1403 // File may only contain a single namespace

namespace MessagePack.Resolvers
{
    public sealed class NativeDateTimeResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly NativeDateTimeResolver Instance;

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options;

        static NativeDateTimeResolver()
        {
            Instance = new NativeDateTimeResolver();
            Options = new MessagePackSerializerOptions(Instance);
        }

        private NativeDateTimeResolver()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>?)NativeDateTimeResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class NativeDateTimeResolverGetFormatterHelper
    {
        internal static object? GetFormatter(Type t)
        {
            if (t == typeof(DateTime))
            {
                return NativeDateTimeFormatter.Instance;
            }
            else if (t == typeof(DateTime?))
            {
                return new StaticNullableFormatter<DateTime>(NativeDateTimeFormatter.Instance);
            }
            else if (t == typeof(DateTime[]))
            {
                return NativeDateTimeArrayFormatter.Instance;
            }

            return null;
        }
    }
}
