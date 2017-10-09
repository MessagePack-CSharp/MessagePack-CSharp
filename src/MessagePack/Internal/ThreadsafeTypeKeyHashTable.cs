using System;

namespace MessagePack.Internal
{
    // Safe for multiple-read, single-write.
    internal class ThreadsafeTypeKeyHashTable<TValue>
    {
        Entry[] buckets;
        int size; // only use in writer lock

        readonly object writerLock = new object();
        readonly float loadFactor;

        // IEqualityComparer.Equals is overhead if key only Type, don't use it.
        // readonly IEqualityComparer<TKey> comparer;

        public ThreadsafeTypeKeyHashTable(int capacity = 4, float loadFactor = 0.75f)
        {
            var tableSize = CalculateCapacity(capacity, loadFactor);
            this.buckets = new Entry[tableSize];
            this.loadFactor = loadFactor;
        }

        public bool TryAdd(Type key, TValue value)
        {
            return TryAdd(key, _ => value); // create lambda capture
        }

        public bool TryAdd(Type key, Func<Type, TValue> valueFactory)
        {
            TValue _;
            return TryAddInternal(key, valueFactory, out _);
        }

        bool TryAddInternal(Type key, Func<Type, TValue> valueFactory, out TValue resultingValue)
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

        bool AddToBuckets(Entry[] buckets, Type newKey, Entry newEntryOrNull, Func<Type, TValue> valueFactory, out TValue resultingValue)
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
                    resultingValue = valueFactory(newKey);
                    VolatileWrite(ref buckets[h & (buckets.Length - 1)], new Entry { Key = newKey, Value = resultingValue, Hash = h });
                }
            }
            else
            {
                var searchLastEntry = buckets[h & (buckets.Length - 1)];
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

        public bool TryGetValue(Type key, out TValue value)
        {
            var table = buckets;
            var hash = key.GetHashCode();
            var entry = table[hash & table.Length - 1];

            if (entry == null) goto NOT_FOUND;

            if (entry.Key == key)
            {
                value = entry.Value;
                return true;
            }

            var next = entry.Next;
            while (next != null)
            {
                if (next.Key == key)
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

        public TValue GetOrAdd(Type key, Func<Type, TValue> valueFactory)
        {
            TValue v;
            if (TryGetValue(key, out v))
            {
                return v;
            }

            TryAddInternal(key, valueFactory, out v);
            return v;
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
#if NETSTANDARD
            System.Threading.Volatile.Write(ref location, value);
#elif UNITY_WSA || NET_4_6
            System.Threading.Volatile.Write(ref location, value);
#else
            System.Threading.Thread.MemoryBarrier();
            location = value;
#endif
        }

        static void VolatileWrite(ref Entry[] location, Entry[] value)
        {
#if NETSTANDARD
            System.Threading.Volatile.Write(ref location, value);
#elif UNITY_WSA || NET_4_6
            System.Threading.Volatile.Write(ref location, value);
#else
            System.Threading.Thread.MemoryBarrier();
            location = value;
#endif
        }

        class Entry
        {
            public Type Key;
            public TValue Value;
            public int Hash;
            public Entry Next;

            // debug only
            public override string ToString()
            {
                return Key + "(" + Count() + ")";
            }

            int Count()
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
