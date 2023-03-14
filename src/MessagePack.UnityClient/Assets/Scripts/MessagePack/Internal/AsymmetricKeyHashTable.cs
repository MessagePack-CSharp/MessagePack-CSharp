// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY_2018_3_OR_NEWER

using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Internal
{
    /* Safe for multiple-read, single-write.
     * Add and Get Key is asymmetric. */

    internal interface IAsymmetricEqualityComparer<TKey1, TKey2>
    {
        int GetHashCode(TKey1 key1);

        int GetHashCode(TKey2 key2);

        bool Equals(TKey1 x, TKey1 y); // when used rehash

        bool Equals(TKey1 x, TKey2 y); // when used get
    }

    internal class StringArraySegmentByteAscymmetricEqualityComparer : IAsymmetricEqualityComparer<byte[], ArraySegment<byte>>
    {
        private static readonly bool Is32Bit = IntPtr.Size == 4;

        public bool Equals(byte[] x, byte[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

        public bool Equals(byte[] x, ArraySegment<byte> y)
        {
            return x.AsSpan().SequenceEqual(y);
        }

        public int GetHashCode(byte[] key1)
        {
            return this.GetHashCode(new ArraySegment<byte>(key1, 0, key1.Length));
        }

        public int GetHashCode(ArraySegment<byte> key2)
        {
            unchecked
            {
                if (Is32Bit)
                {
                    return (int)FarmHash.Hash32(key2);
                }
                else
                {
                    return (int)FarmHash.Hash64(key2);
                }
            }
        }
    }

    internal sealed class AsymmetricKeyHashTable<TKey1, TKey2, TValue>
    {
        private Entry[] buckets;
        private int size; // only use in writer lock

        private readonly object writerLock = new object();
        private readonly float loadFactor;
        private readonly IAsymmetricEqualityComparer<TKey1, TKey2> comparer;

        public AsymmetricKeyHashTable(IAsymmetricEqualityComparer<TKey1, TKey2> comparer)
            : this(4, 0.72f, comparer)
        {
        }

        public AsymmetricKeyHashTable(int capacity, float loadFactor, IAsymmetricEqualityComparer<TKey1, TKey2> comparer)
        {
            var tableSize = CalculateCapacity(capacity, loadFactor);
            this.buckets = new Entry[tableSize];
            this.loadFactor = loadFactor;
            this.comparer = comparer;
        }

        public TValue AddOrGet(TKey1 key1, Func<TKey1, TValue> valueFactory)
        {
            TValue v;
            this.TryAddInternal(key1, valueFactory, out v);
            return v;
        }

        public bool TryAdd(TKey1 key, TValue value)
        {
            return this.TryAdd(key, _ => value); // closure capture
        }

        public bool TryAdd(TKey1 key, Func<TKey1, TValue> valueFactory)
        {
            return this.TryAddInternal(key, valueFactory, out TValue _);
        }

        private bool TryAddInternal(TKey1 key, Func<TKey1, TValue> valueFactory, out TValue resultingValue)
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

        private bool AddToBuckets(Entry[] buckets, TKey1 newKey, Entry? newEntryOrNull, Func<TKey1, TValue>? valueFactory, out TValue resultingValue)
        {
            var h = (newEntryOrNull != null) ? newEntryOrNull.Hash : this.comparer.GetHashCode(newKey);
            if (buckets[h & (buckets.Length - 1)] == null)
            {
                if (newEntryOrNull != null)
                {
                    resultingValue = newEntryOrNull.Value;
                    VolatileWrite(ref buckets[h & (buckets.Length - 1)], newEntryOrNull);
                }
                else if (valueFactory is object)
                {
                    resultingValue = valueFactory(newKey);
                    VolatileWrite(ref buckets[h & (buckets.Length - 1)], new Entry(newKey, resultingValue, h));
                }
                else
                {
                    throw new ArgumentNullException(nameof(valueFactory), "Either " + nameof(newEntryOrNull) + " or " + nameof(valueFactory) + " must be non-null.");
                }
            }
            else
            {
                Entry searchLastEntry = buckets[h & (buckets.Length - 1)];
                while (true)
                {
                    if (this.comparer.Equals(searchLastEntry.Key, newKey))
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
                        else if (valueFactory is object)
                        {
                            resultingValue = valueFactory(newKey);
                            VolatileWrite(ref searchLastEntry.Next, new Entry(newKey, resultingValue, h));
                        }
                        else
                        {
                            throw new ArgumentNullException(nameof(valueFactory), "Either " + nameof(newEntryOrNull) + " or " + nameof(valueFactory) + " must be non-null.");
                        }

                        break;
                    }

                    searchLastEntry = searchLastEntry.Next;
                }
            }

            return true;
        }

        public bool TryGetValue(TKey2 key, [MaybeNullWhen(false)] out TValue value)
        {
            Entry[] table = this.buckets;
            var hash = this.comparer.GetHashCode(key);
            Entry entry = table[hash & table.Length - 1];

            if (entry == null)
            {
                goto NOT_FOUND;
            }

            if (this.comparer.Equals(entry.Key, key))
            {
                value = entry.Value;
                return true;
            }

            Entry? next = entry.Next;
            while (next != null)
            {
                if (this.comparer.Equals(next.Key, key))
                {
                    value = next.Value;
                    return true;
                }

                next = next.Next;
            }

NOT_FOUND:
            value = default(TValue);
            return false;
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

        private static void VolatileWrite<T>(ref T location, T value)
            where T : Entry?
        {
            System.Threading.Volatile.Write(ref location, value);
        }

        private static void VolatileWrite(ref Entry[] location, Entry[] value)
        {
            System.Threading.Volatile.Write(ref location, value);
        }

        private class Entry
        {
#pragma warning disable SA1401 // Fields should be private
            internal readonly TKey1 Key;
            internal readonly TValue Value;
            internal readonly int Hash;
            internal Entry? Next;
#pragma warning restore SA1401 // Fields should be private

            internal Entry(TKey1 key, TValue value, int hash)
            {
                this.Key = key;
                this.Value = value;
                this.Hash = hash;
            }

            // from debugger only
            public override string ToString()
            {
                return "Count:" + this.Count;
            }

            internal int Count
            {
                get
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
}

#endif
