using System;
using System.Collections.Generic;

namespace MessagePack.Internal
{
    // for Unity, can not implementes IReadOnlyDictionary.

    // Safe for multiple-read, single-write.
    internal class ThreadsafeHashTable<TKey, TValue> // : IReadOnlyDictionary<TKey, TValue>
    {
        Entry[] buckets;
        int size; // only use in writer lock

        readonly object writerLock = new object();
        readonly float loadFactor;
        readonly IEqualityComparer<TKey> comparer;

        public ThreadsafeHashTable()
            : this(EqualityComparer<TKey>.Default)
        {

        }

        public ThreadsafeHashTable(IEqualityComparer<TKey> comaprer)
            : this(4, 0.75f, comaprer)
        {

        }

        public ThreadsafeHashTable(int capacity, float loadFactor = 0.75f)
            : this(capacity, loadFactor, EqualityComparer<TKey>.Default)
        {

        }

        public ThreadsafeHashTable(int capacity, float loadFactor, IEqualityComparer<TKey> comparer)
        {
            var tableSize = CalculateCapacity(capacity, loadFactor);
            this.buckets = new Entry[tableSize];
            this.loadFactor = loadFactor;
            this.comparer = comparer;
        }

        public bool TryAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            TValue _;
            return TryAddInternal(key, valueFactory, out _);
        }

        bool TryAddInternal(TKey key, Func<TKey, TValue> valueFactory, out TValue resultingValue)
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
                    System.Threading.Volatile.Write(ref buckets, nextBucket);

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

        bool AddToBuckets(Entry[] buckets, TKey newKey, Entry newEntryOrNull, Func<TKey, TValue> valueFactory, out TValue resultingValue)
        {
            var h = (newEntryOrNull != null) ? newEntryOrNull.Hash : comparer.GetHashCode(newKey);
            if (buckets[h & (buckets.Length - 1)] == null)
            {
                if (newEntryOrNull != null)
                {
                    resultingValue = newEntryOrNull.Value;
                    System.Threading.Volatile.Write(ref buckets[h & (buckets.Length - 1)], newEntryOrNull);
                }
                else
                {
                    resultingValue = valueFactory(newKey);
                    System.Threading.Volatile.Write(ref buckets[h & (buckets.Length - 1)], new Entry { Key = newKey, Value = resultingValue, Hash = h });
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
                            System.Threading.Volatile.Write(ref searchLastEntry.Next, newEntryOrNull);
                        }
                        else
                        {
                            resultingValue = valueFactory(newKey);
                            System.Threading.Volatile.Write(ref searchLastEntry.Next, new Entry { Key = newKey, Value = resultingValue, Hash = h });
                        }
                        break;
                    }
                    searchLastEntry = searchLastEntry.Next;
                }
            }

            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
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

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
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

        //#region IReadOnlyDictionary<>

        //// When writing and encounts gap the multithread, returns actual size -1(by design).
        //public int Count
        //{
        //    get
        //    {
        //        return size;
        //    }
        //}

        //public IEnumerable<TKey> Keys
        //{
        //    get
        //    {
        //        var e = buckets;
        //        foreach (var eItem in e)
        //        {
        //            var item = eItem;
        //            while (item != null)
        //            {
        //                yield return item.Key;
        //                item = item.Next;
        //            }
        //        }
        //    }
        //}

        //public IEnumerable<TValue> Values
        //{
        //    get
        //    {
        //        var e = buckets;
        //        foreach (var eItem in e)
        //        {
        //            var item = eItem;
        //            while (item != null)
        //            {
        //                yield return item.Value;
        //                item = item.Next;
        //            }
        //        }
        //    }
        //}

        //public TValue this[TKey key]
        //{
        //    get
        //    {
        //        TValue value;
        //        if (TryGetValue(key, out value))
        //        {
        //            return value;
        //        }
        //        throw new KeyNotFoundException("Key not found, key:" + key);
        //    }
        //}

        //public bool ContainsKey(TKey key)
        //{

        //    TValue value;
        //    return TryGetValue(key, out value);
        //}

        //public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        //{
        //    var e = buckets;
        //    foreach (var eItem in e)
        //    {
        //        var item = eItem;
        //        while (item != null)
        //        {
        //            yield return new KeyValuePair<TKey, TValue>(item.Key, item.Value);
        //            item = item.Next;
        //        }
        //    }
        //}
        ////IEnumerator IEnumerable.GetEnumerator()
        ////{
        ////    return GetEnumerator();
        ////}

        //#endregion

        class Entry
        {
            public TKey Key;
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
