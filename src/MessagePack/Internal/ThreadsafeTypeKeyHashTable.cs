﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Internal
{
    /// <summary>
    /// A dictionary where <see cref="Type"/> is the key, and a configurable <typeparamref name="TValue"/> type
    /// that is thread-safe to read and write, allowing concurrent reads and exclusive writes.
    /// </summary>
    /// <typeparam name="TValue">The type of value stored in the dictionary.</typeparam>
    internal class ThreadsafeTypeKeyHashTable<TValue>
    {
        private Entry[] buckets;
        private int size; // only use in writer lock

        private readonly object writerLock = new object();
        private readonly float loadFactor;

        // IEqualityComparer.Equals is overhead if key only Type, don't use it.
        //// readonly IEqualityComparer<TKey> comparer;

        public ThreadsafeTypeKeyHashTable(int capacity = 4, float loadFactor = 0.75f)
        {
            var tableSize = CalculateCapacity(capacity, loadFactor);
            this.buckets = new Entry[tableSize];
            this.loadFactor = loadFactor;
        }

        public bool TryAdd(Type key, TValue value)
        {
            return this.TryAdd(key, _ => value); // create lambda capture
        }

        public bool TryAdd(Type key, Func<Type, TValue> valueFactory)
        {
            return this.TryAddInternal(key, valueFactory, out TValue? _);
        }

        private bool TryAddInternal(Type key, Func<Type, TValue> valueFactory, out TValue resultingValue)
        {
            lock (this.writerLock)
            {
                var nextCapacity = CalculateCapacity(this.size + 1, this.loadFactor);

                if (this.buckets.Length < nextCapacity)
                {
                    // rehash
                    var nextBucket = new Entry[nextCapacity];
                    for (int i = 0; i < this.buckets.Length; i++)
                    {
                        Entry? e = this.buckets[i];
                        while (e != null)
                        {
                            var newEntry = new Entry(e.Key, e.Value, e.Hash);
                            this.AddToBuckets(nextBucket, key, newEntry, null, out resultingValue);
                            e = e.Next;
                        }
                    }

                    // add entry(if failed to add, only do resize)
                    var successAdd = this.AddToBuckets(nextBucket, key, null, valueFactory, out resultingValue);

                    // replace field(threadsafe for read)
                    VolatileWrite(ref this.buckets, nextBucket);

                    if (successAdd)
                    {
                        this.size++;
                    }

                    return successAdd;
                }
                else
                {
                    // add entry(insert last is thread safe for read)
                    var successAdd = this.AddToBuckets(this.buckets, key, null, valueFactory, out resultingValue);
                    if (successAdd)
                    {
                        this.size++;
                    }

                    return successAdd;
                }
            }
        }

        private bool AddToBuckets(Entry[] buckets, Type newKey, Entry? newEntryOrNull, Func<Type, TValue>? valueFactory, out TValue resultingValue)
        {
            var h = (newEntryOrNull != null) ? newEntryOrNull.Hash : newKey.GetHashCode();
            if (buckets[h & (buckets.Length - 1)] == null)
            {
                if (newEntryOrNull != null)
                {
                    resultingValue = newEntryOrNull.Value;
                    VolatileWrite(ref buckets[h & (buckets.Length - 1)], newEntryOrNull);
                }
                else
                {
                    if (valueFactory is null)
                    {
                        throw new ArgumentNullException(nameof(valueFactory));
                    }

                    resultingValue = valueFactory(newKey);
                    VolatileWrite(ref buckets[h & (buckets.Length - 1)], new Entry(newKey, resultingValue, h));
                }
            }
            else
            {
                Entry searchLastEntry = buckets[h & (buckets.Length - 1)];
                while (true)
                {
                    if (searchLastEntry.Key == newKey)
                    {
                        resultingValue = searchLastEntry.Value;
                        return false;
                    }

                    if (searchLastEntry.Next == null)
                    {
                        if (newEntryOrNull != null)
                        {
                            resultingValue = newEntryOrNull.Value;
                            VolatileWrite(ref searchLastEntry.Next, newEntryOrNull);
                        }
                        else
                        {
                            if (valueFactory is null)
                            {
                                throw new ArgumentNullException(nameof(valueFactory));
                            }

                            resultingValue = valueFactory(newKey);
                            VolatileWrite(ref searchLastEntry.Next, new Entry(newKey, resultingValue, h));
                        }

                        break;
                    }

                    searchLastEntry = searchLastEntry.Next;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(Type key, [MaybeNullWhen(false)] out TValue value)
        {
            Entry[] table = this.buckets;
            var hash = key.GetHashCode();
            Entry? entry = table[hash & table.Length - 1];

            while (entry != null)
            {
                if (entry.Key == key)
                {
                    value = entry.Value;
                    return true;
                }

                entry = entry.Next;
            }

            value = default(TValue);
            return false;
        }

        public TValue GetOrAdd(Type key, Func<Type, TValue> valueFactory)
        {
            if (this.TryGetValue(key, out TValue? v))
            {
                return v;
            }

            this.TryAddInternal(key, valueFactory, out v);
            return v;
        }

        private static int CalculateCapacity(int collectionSize, float loadFactor)
        {
            var initialCapacity = (int)(((float)collectionSize) / loadFactor);
            var capacity = 1;
            while (capacity < initialCapacity)
            {
                capacity <<= 1;
            }

            if (capacity < 8)
            {
                return 8;
            }

            return capacity;
        }

        private static void VolatileWrite([NotNullIfNotNull("value")] ref Entry? location, Entry value)
        {
            System.Threading.Volatile.Write(ref location!, value);
        }

        private static void VolatileWrite([NotNullIfNotNull("value")] ref Entry[] location, Entry[] value)
        {
            System.Threading.Volatile.Write(ref location, value);
        }

        private class Entry
        {
#pragma warning disable SA1401 // Fields should be private
            internal Entry? Next;
#pragma warning restore SA1401 // Fields should be private

            internal Entry(Type key, TValue value, int hash)
            {
                this.Key = key;
                this.Value = value;
                this.Hash = hash;
            }

            internal Type Key { get; }

            internal TValue Value { get; }

            internal int Hash { get; }

            // debug only
            public override string ToString()
            {
                return this.Key + "(" + this.Count() + ")";
            }

            private int Count()
            {
                var count = 1;
                Entry n = this;
                while (n.Next != null)
                {
                    count++;
                    n = n.Next;
                }

                return count;
            }
        }
    }
}
