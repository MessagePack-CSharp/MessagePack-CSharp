// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public sealed class PrimitiveArrayResolver : IFormatterResolver
    {
        public static readonly PrimitiveArrayResolver Instance = new PrimitiveArrayResolver();

        private PrimitiveArrayResolver()
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
                Formatter = PrimitiveArrayGetFormatterHelper.GetFormatter(typeof(T)) as IMessagePackFormatter<T>;
            }
        }
    }
}
