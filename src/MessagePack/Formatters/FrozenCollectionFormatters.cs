// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET8_0_OR_GREATER

using System.Collections.Frozen;
using System.Collections.Generic;
using MessagePack.Formatters;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.ImmutableCollection
{
    public sealed class FrozenDictionaryFormatter<TKey, TValue> : IMessagePackFormatter<FrozenDictionary<TKey, TValue>?>
        where TKey : notnull
    {
        private readonly IEqualityComparer<TKey>? comparer;

        public FrozenDictionaryFormatter()
        {
            comparer = default;
        }

        public FrozenDictionaryFormatter(IEqualityComparer<TKey>? comparer)
        {
            this.comparer = comparer;
        }

        public void Serialize(ref MessagePackWriter writer, FrozenDictionary<TKey, TValue>? value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            IFormatterResolver resolver = options.Resolver;
            IMessagePackFormatter<TKey> keyFormatter = resolver.GetFormatterWithVerify<TKey>();
            IMessagePackFormatter<TValue> valueFormatter = resolver.GetFormatterWithVerify<TValue>();

            // https://github.com/dotnet/runtime/blob/4c500699b938d53993b928b93543b8dbe68f69aa/src/libraries/System.Collections.Immutable/src/System/Collections/Frozen/FrozenHashTable.cs#L134C2-L134C2
            // FrozenDictionary.Count uses FrozenHashTable's Count property which is O(1).
            var count = value.Count;
            writer.WriteMapHeader(count);
            if (count == 0)
            {
                return;
            }

            FrozenDictionary<TKey, TValue>.Enumerator e = value.GetEnumerator();
            while (e.MoveNext())
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                KeyValuePair<TKey, TValue> item = e.Current;
                keyFormatter.Serialize(ref writer, item.Key, options);
                valueFormatter.Serialize(ref writer, item.Value, options);
            }
        }

        public FrozenDictionary<TKey, TValue>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }

            var count = reader.ReadMapHeader();
            if (count == 0)
            {
                return FrozenDictionary<TKey, TValue>.Empty;
            }

            options.Security.DepthStep(ref reader);
            IFormatterResolver resolver = options.Resolver;
            IMessagePackFormatter<TKey> keyFormatter = resolver.GetFormatterWithVerify<TKey>();
            IMessagePackFormatter<TValue> valueFormatter = resolver.GetFormatterWithVerify<TValue>();
            IEqualityComparer<TKey> comparer = this.comparer ?? options.Security.GetEqualityComparer<TKey>();

            // https://github.com/dotnet/runtime/blob/4c500699b938d53993b928b93543b8dbe68f69aa/src/libraries/System.Collections.Immutable/src/System/Collections/Frozen/FrozenDictionary.cs#L87
            // FrozenDictionary.ToFrozenDictionary internally allocates Dictionary<TKey, TValue> object.
            var dictionary = new Dictionary<TKey, TValue>(count, comparer);
            for (var i = 0; i < count; i++)
            {
                reader.CancellationToken.ThrowIfCancellationRequested();
                dictionary.Add(keyFormatter.Deserialize(ref reader, options), valueFormatter.Deserialize(ref reader, options));
            }

            return dictionary.ToFrozenDictionary(comparer);
        }
    }

    public sealed class FrozenSetFormatter<T> : IMessagePackFormatter<FrozenSet<T>?>
    {
        private readonly IEqualityComparer<T>? comparer;

        public FrozenSetFormatter()
        {
            comparer = default;
        }

        public FrozenSetFormatter(IEqualityComparer<T> comparer)
        {
            this.comparer = comparer;
        }

        public void Serialize(ref MessagePackWriter writer, FrozenSet<T>? value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var count = value.Count;
            writer.WriteArrayHeader(count);
            if (count == 0)
            {
                return;
            }

            IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();
            FrozenSet<T>.Enumerator e = value.GetEnumerator();
            while (e.MoveNext())
            {
                formatter.Serialize(ref writer, e.Current, options);
            }
        }

        public FrozenSet<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }

            var count = reader.ReadArrayHeader();
            if (count == 0)
            {
                return FrozenSet<T>.Empty;
            }

            IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();
            IEqualityComparer<T> comparer = this.comparer ?? options.Security.GetEqualityComparer<T>();

            // https://github.com/dotnet/runtime/blob/4c500699b938d53993b928b93543b8dbe68f69aa/src/libraries/System.Collections.Immutable/src/System/Collections/Frozen/FrozenSet.cs#L41
            // FrozenSet.ToFrozenSet internally allocates HashSet<T> object.
            var set = new HashSet<T>(count, comparer);
            for (var i = 0; i < count; i++)
            {
                set.Add(formatter.Deserialize(ref reader, options));
            }

            return set.ToFrozenSet(comparer);
        }
    }
}

#endif
