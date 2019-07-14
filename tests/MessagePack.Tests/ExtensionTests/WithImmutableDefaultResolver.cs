// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;
using MessagePack.ImmutableCollection;
using MessagePack.Resolvers;

namespace MessagePack.Tests.ExtensionTests
{
    public class WithImmutableDefaultResolver : IFormatterResolver
    {
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return ImmutableCollectionResolver.Instance.GetFormatter<T>()
                 ?? StandardResolver.Instance.GetFormatter<T>();
        }
    }
}
