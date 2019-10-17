// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public class StaticCompositeResolver : IFormatterResolver
    {
        public static readonly StaticCompositeResolver Instance = new StaticCompositeResolver();

        private bool freezed;
        private IReadOnlyList<IMessagePackFormatter> formatters;
        private IReadOnlyList<IFormatterResolver> resolvers;

        private StaticCompositeResolver()
        {
            formatters = Array.Empty<IMessagePackFormatter>();
            resolvers = Array.Empty<IFormatterResolver>();
        }

        public static void Register(params IMessagePackFormatter[] formatters)
        {
            if (Instance.freezed)
            {
                throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
            }

            Instance.formatters = formatters;
        }

        public static void Register(params IFormatterResolver[] resolvers)
        {
            if (Instance.freezed)
            {
                throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
            }

            Instance.resolvers = resolvers;
        }

        public static void Register(IReadOnlyList<IMessagePackFormatter> formatters, IReadOnlyList<IFormatterResolver> resolvers)
        {
            if (Instance.freezed)
            {
                throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
            }

            Instance.formatters = formatters;
            Instance.resolvers = resolvers;
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }

        private static class Cache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static Cache()
            {
                Instance.freezed = true;
                foreach (var item in Instance.formatters)
                {
                    if (item is IMessagePackFormatter<T> f)
                    {
                        Formatter = f;
                        return;
                    }
                }

                foreach (var item in Instance.resolvers)
                {
                    var f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        Formatter = f;
                        return;
                    }
                }
            }
        }
    }
}
