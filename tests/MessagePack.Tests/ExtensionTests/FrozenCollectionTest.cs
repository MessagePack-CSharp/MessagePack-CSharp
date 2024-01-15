// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET8_0_OR_GREATER
#nullable enable

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace MessagePack.Tests.ExtensionTests
{
    public class FrozenCollectionTest
    {
        private T Convert<T>(T value)
        {
            MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard;
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, options), options);
        }

        [Fact]
        public void EmptySet()
        {
            {
                var empty = FrozenSet<int>.Empty;
                Convert(empty).IsStructuralEqual(empty);
            }

            {
                var empty = FrozenSet<string>.Empty;
                Convert(empty).IsStructuralEqual(empty);
            }

            {
                var empty = FrozenSet<ulong>.Empty;
                Convert(empty).IsStructuralEqual(empty);
            }

            {
                var empty = FrozenSet<DateTime>.Empty;
                Convert(empty).IsStructuralEqual(empty);
            }
        }

        [Fact]
        public void EmptyDictionary()
        {
            {
                var empty = FrozenDictionary<string, int>.Empty;
                Convert(empty).IsStructuralEqual(empty);
            }

            {
                var empty = FrozenDictionary<string, string?>.Empty;
                Convert(empty).IsStructuralEqual(empty);
            }

            {
                var empty = FrozenDictionary<string, ulong>.Empty;
                Convert(empty).IsStructuralEqual(empty);
            }

            {
                var empty = FrozenDictionary<string, DateTime>.Empty;
                Convert(empty).IsStructuralEqual(empty);
            }

            {
                var empty = FrozenDictionary<int, int>.Empty;
                Convert(empty).IsStructuralEqual(empty);
            }

            {
                var empty = FrozenDictionary<int, string?>.Empty;
                Convert(empty).IsStructuralEqual(empty);
            }

            {
                var empty = FrozenDictionary<int, ulong>.Empty;
                Convert(empty).IsStructuralEqual(empty);
            }

            {
                var empty = FrozenDictionary<int, DateTime>.Empty;
                Convert(empty).IsStructuralEqual(empty);
            }
        }

        [Fact]
        public void IntSet()
        {
            for (var i = 1; i < 11; i++)
            {
                var array = new int[1 << i];
                Random.Shared.NextBytes(MemoryMarshal.AsBytes<int>(array));
                var set = array.ToFrozenSet();
                Convert(set).IsStructuralEqualIgnoreCollectionOrder(set);
            }
        }

        [Fact]
        public void IntDictionary()
        {
            for (var i = 1; i < 11; i++)
            {
                var array = new KeyValuePair<int, int>[1 << i];
                Random.Shared.NextBytes(MemoryMarshal.AsBytes<KeyValuePair<int, int>>(array));
                var dictionary = array.ToFrozenDictionary();
                Convert(dictionary).IsStructuralEqualIgnoreCollectionOrder(dictionary);
            }
        }
    }
}
#endif
