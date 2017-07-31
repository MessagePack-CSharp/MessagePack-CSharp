using System;

namespace MessagePack.Internal
{
    // like ArraySegment<byte> hashtable.
    // API is `Get(byte[] keyBytes, int offset, int count)`.
    // This is a cheap alternative of UTF8String(not yet completed) dictionary.
    internal class ThreadsafeByteArrayHashTable<TValue>
    {
        Entry[] buckets;
        int size; // only use in writer lock

        readonly object writerLock = new object();
        readonly float loadFactor;

        public ThreadsafeByteArrayHashTable(int capacity = 4, float loadFactor = 0.75f)
        {
            var tableSize = CalculateCapacity(capacity, loadFactor);
            this.buckets = new Entry[tableSize];
            this.loadFactor = loadFactor;
        }

        public bool TryAdd(byte[] key, Func<byte[], TValue> valueFactory)
        {
            TValue _;
            return TryAddInternal(key, valueFactory, out _);
        }

        bool TryAddInternal(byte[] key, Func<byte[], TValue> valueFactory, out TValue resultingValue)
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

        bool AddToBuckets(Entry[] buckets, byte[] newKey, Entry newEntryOrNull, Func<byte[], TValue> valueFactory, out TValue resultingValue)
        {
            var h = (newEntryOrNull != null) ? newEntryOrNull.Hash : ByteArrayGetHashCode(newKey, 0, newKey.Length);
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
                    if (ByteArrayEquals(searchLastEntry.Key, 0, searchLastEntry.Key.Length, newKey, 0, newKey.Length))
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

        public bool TryGetValue(byte[] key, int offset, int count, out TValue value)
        {
            var table = buckets;
            var hash = ByteArrayGetHashCode(key, offset, count);
            var entry = table[hash & table.Length - 1];

            if (entry == null) goto NOT_FOUND;

            if (ByteArrayEquals(entry.Key, 0, entry.Key.Length, key, offset, count))
            {
                value = entry.Value;
                return true;
            }

            var next = entry.Next;
            while (next != null)
            {
                if (ByteArrayEquals(next.Key, 0, next.Key.Length, key, offset, count))
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

        public TValue GetOrAdd(byte[] key, int offset, int count, Func<byte[], TValue> valueFactory)
        {
            TValue v;
            if (TryGetValue(key, offset, count, out v))
            {
                return v;
            }

            TryAddInternal(key, valueFactory, out v);
            return v;
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static uint ByteArrayGetHashCode(byte[] x, int offset, int count)
        {
            // borrow from Roslyn's ComputeStringHash, calculate FNV-1a hash
            // http://source.roslyn.io/#Microsoft.CodeAnalysis.CSharp/Compiler/MethodBodySynthesizer.Lowered.cs,26

            uint hash = 0;
            if (x != null)
            {
                hash = 2166136261u; // hash = FNV_offset_basis

                var i = offset;
                var max = i + count;
                goto start;

                again:
                hash = unchecked((x[i] ^ hash) * 16777619); // hash = hash XOR byte_of_data, hash = hash × FNV_prime
                i = i + 1;

                start:
                if (i < max)
                {
                    goto again;
                }
            }

            return hash;
        }

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static bool ByteArrayEquals(byte[] x, int xOffset, int xCount, byte[] y, int yOffset, int yCount)
        {
            // More improvement, use ReadOnlySpan<byte>.SequenceEqual.

            if (xCount != yCount) return false;

            var xMax = xOffset + xCount;
            while (xOffset < xMax)
            {
                if (x[xOffset++] != y[yOffset++]) return false;
            }

            return true;
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

        class Entry
        {
            public byte[] Key;
            public TValue Value;
            public uint Hash;
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
