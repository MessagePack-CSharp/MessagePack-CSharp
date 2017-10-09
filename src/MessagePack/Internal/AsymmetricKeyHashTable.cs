#if NETSTANDARD

using System;

namespace MessagePack.Internal
{
    // Safe for multiple-read, single-write.

    // Add and Get Key is asymmetric.

    internal interface IAsymmetricEqualityComparer<TKey1, TKey2>
    {
        int GetHashCode(TKey1 key1);
        int GetHashCode(TKey2 key2);
        bool Equals(TKey1 x, TKey1 y); // when used rehash
        bool Equals(TKey1 x, TKey2 y); // when used get
    }

    internal class StringArraySegmentByteAscymmetricEqualityComparer : IAsymmetricEqualityComparer<byte[], ArraySegment<byte>>
    {
        static readonly bool Is32Bit = (IntPtr.Size == 4);

        public bool Equals(byte[] x, byte[] y)
        {
            if (x.Length != y.Length) return false;

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i]) return false;
            }

            return true;
        }

        public bool Equals(byte[] x, ArraySegment<byte> y)
        {
            return ByteArrayComparer.Equals(y.Array, y.Offset, y.Count, x);
        }

        public int GetHashCode(byte[] key1)
        {
            return GetHashCode(new ArraySegment<byte>(key1, 0, key1.Length));
        }

        public int GetHashCode(ArraySegment<byte> key2)
        {
            unchecked
            {
                if (Is32Bit)
                {
                    return (int)FarmHash.Hash32(key2.Array, key2.Offset, key2.Count);
                }
                else
                {
                    return (int)FarmHash.Hash64(key2.Array, key2.Offset, key2.Count);
                }
            }
        }
    }

    internal sealed class AsymmetricKeyHashTable<TKey1, TKey2, TValue>
    {
        Entry[] buckets;
        int size; // only use in writer lock

        readonly object writerLock = new object();
        readonly float loadFactor;
        readonly IAsymmetricEqualityComparer<TKey1, TKey2> comparer;

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
            TryAddInternal(key1, valueFactory, out v);
            return v;
        }

        public bool TryAdd(TKey1 key, TValue value)
        {
            return TryAdd(key, _ => value); // closure capture
        }

        public bool TryAdd(TKey1 key, Func<TKey1, TValue> valueFactory)
        {
            TValue _;
            return TryAddInternal(key, valueFactory, out _);
        }

        bool TryAddInternal(TKey1 key, Func<TKey1, TValue> valueFactory, out TValue resultingValue)
        {
            lock (writerLock)
            {
                var nextCapacity = CalculateCapacity(size + 1, loadFactor);

                if (buckets.Length < nextCapacity)
                {
                    // rehash
                    var nextBucket = new Entry[nextCapacity];
                    for (int i = 0; i < buckets.Length; i++)
                    {
                        var e = buckets[i];
                        while (e != null)
                        {
                            var newEntry = new Entry { Key = e.Key, Value = e.Value, Hash = e.Hash };
                            AddToBuckets(nextBucket, key, newEntry, null, out resultingValue);
                            e = e.Next;
                        }
                    }

                    // add entry(if failed to add, only do resize)
                    var successAdd = AddToBuckets(nextBucket, key, null, valueFactory, out resultingValue);

                    // replace field(threadsafe for read)
                    VolatileWrite(ref buckets, nextBucket);

                    if (successAdd) size++;
                    return successAdd;
                }
                else
                {
                    // add entry(insert last is thread safe for read)
                    var successAdd = AddToBuckets(buckets, key, null, valueFactory, out resultingValue);
                    if (successAdd) size++;
                    return successAdd;
                }
            }
        }

        bool AddToBuckets(Entry[] buckets, TKey1 newKey, Entry newEntryOrNull, Func<TKey1, TValue> valueFactory, out TValue resultingValue)
        {
            var h = (newEntryOrNull != null) ? newEntryOrNull.Hash : comparer.GetHashCode(newKey);
            if (buckets[h & (buckets.Length - 1)] == null)
            {
                if (newEntryOrNull != null)
                {
                    resultingValue = newEntryOrNull.Value;
                    VolatileWrite(ref buckets[h & (buckets.Length - 1)], newEntryOrNull);
                }
                else
                {
                    resultingValue = valueFactory(newKey);
                    VolatileWrite(ref buckets[h & (buckets.Length - 1)], new Entry { Key = newKey, Value = resultingValue, Hash = h });
                }
            }
            else
            {
                var searchLastEntry = buckets[h & (buckets.Length - 1)];
                while (true)
                {
                    if (comparer.Equals(searchLastEntry.Key, newKey))
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
                            resultingValue = valueFactory(newKey);
                            VolatileWrite(ref searchLastEntry.Next, new Entry { Key = newKey, Value = resultingValue, Hash = h });
                        }
                        break;
                    }
                    searchLastEntry = searchLastEntry.Next;
                }
            }

            return true;
        }

        public bool TryGetValue(TKey2 key, out TValue value)
        {
            var table = buckets;
            var hash = comparer.GetHashCode(key);
            var entry = table[hash & table.Length - 1];

            if (entry == null) goto NOT_FOUND;

            if (comparer.Equals(entry.Key, key))
            {
                value = entry.Value;
                return true;
            }

            var next = entry.Next;
            while (next != null)
            {
                if (comparer.Equals(next.Key, key))
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

        static int CalculateCapacity(int collectionSize, float loadFactor)
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

        static void VolatileWrite(ref Entry location, Entry value)
        {
            System.Threading.Volatile.Write(ref location, value);
        }

        static void VolatileWrite(ref Entry[] location, Entry[] value)
        {
            System.Threading.Volatile.Write(ref location, value);
        }

        class Entry
        {
            public TKey1 Key;
            public TValue Value;
            public int Hash;
            public Entry Next;

            // from debugger only
            public override string ToString()
            {
                return "Count:" + Count;
            }

            internal int Count
            {
                get
                {
                    var count = 1;
                    var n = this;
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