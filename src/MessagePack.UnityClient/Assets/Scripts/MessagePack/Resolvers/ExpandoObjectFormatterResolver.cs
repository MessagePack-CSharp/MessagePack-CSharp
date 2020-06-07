// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public sealed class ExpandoObjectFormatterResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new ExpandoObjectFormatterResolver();

        private ExpandoObjectFormatterResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(ExpandoObject))
            {
                return (IMessagePackFormatter<T>)ExpandoObjectFormatter.Instance;
            }

            return null;
        }
    }
}
