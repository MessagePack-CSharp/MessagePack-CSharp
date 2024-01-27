// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET8_0_OR_GREATER
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using MessagePack.Formatters;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.FrozenCollection
{
    public class FrozenDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, Dictionary<TKey, TValue>, FrozenDictionary<TKey, TValue>.Enumerator, FrozenDictionary<TKey, TValue>>
            where TKey : notnull
    {
        protected override void Add(Dictionary<TKey, TValue> collection, int index, TKey key, TValue value, MessagePackSerializerOptions options)
        =>  collection.Add(key, value);

        protected override FrozenDictionary<TKey, TValue> Complete(Dictionary<TKey, TValue> intermediateCollection)
        => intermediateCollection.ToFrozenDictionary();

        protected override Dictionary<TKey, TValue> Create(int count, MessagePackSerializerOptions options)
        => new(options.Security.GetEqualityComparer<TKey>());

        protected override FrozenDictionary<TKey, TValue>.Enumerator GetSourceEnumerator(FrozenDictionary<TKey, TValue> source)
        => source.GetEnumerator();
    }

    public class FrozenSetFormatter<T> : CollectionFormatterBase<T, HashSet<T>, FrozenSet<T>.Enumerator, FrozenSet<T>>
    {
        protected override void Add(HashSet<T> collection, int index, T value, MessagePackSerializerOptions options)
        => collection.Add(value);

        protected override FrozenSet<T> Complete(HashSet<T> intermediateCollection)
        => intermediateCollection.ToFrozenSet();

        protected override HashSet<T> Create(int count, MessagePackSerializerOptions options)
        => new(options.Security.GetEqualityComparer<T>());

        protected override FrozenSet<T>.Enumerator GetSourceEnumerator(FrozenSet<T> source)
        => source.GetEnumerator();
    }
}

#endif
