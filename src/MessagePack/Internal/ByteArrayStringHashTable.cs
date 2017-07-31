using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Internal
{
    // like ArraySegment<byte> hashtable.
    // This is a cheap alternative of UTF8String(not yet completed) dictionary.

    // internal, but code generator requires this class
    public class ByteArrayStringHashTable<TValue> : IEnumerable<KeyValuePair<byte[], TValue>>
    {
        Entry[] buckets;
        int size; // only use in writer lock

        readonly object writerLock = new object();
        readonly float loadFactor;

        public ByteArrayStringHashTable()
            : this(4, 0.42f)
        {

        }

        public ByteArrayStringHashTable(int capacity)
            : this(capacity, 0.42f)
        {
        }

        public ByteArrayStringHashTable(int capacity = 4, float loadFactor = 0.42f) // default: 0.75f -> 0.42f
        {
            var tableSize = CalculateCapacity(capacity, loadFactor);
            this.buckets = new Entry[tableSize];
            this.loadFactor = loadFactor;
        }

        public void Add(string key, TValue value)
        {
            if (!TryAddInternal(Encoding.UTF8.GetBytes(key), value))
            {
                throw new ArgumentException("Key was already exists. Key:" + key);
            }
        }

        public void Add(byte[] key, TValue value)
        {
            if (!TryAddInternal(key, value))
            {
                throw new ArgumentException("Key was already exists. Key:" + key);
            }
        }

        bool TryAddInternal(byte[] key, TValue value)
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
                            AddToBuckets(nextBucket, key, newEntry, default(TValue));
                            e = e.Next;
                        }
                    }

                    // add entry(if failed to add, only do resize)
                    var successAdd = AddToBuckets(nextBucket, key, null, value);

                    // replace field(threadsafe for read)
                    System.Threading.Volatile.Write(ref buckets, nextBucket);

                    if (successAdd) size++;
                    return successAdd;
                }
                else
                {
                    // add entry(insert last is thread safe for read)
                    var successAdd = AddToBuckets(buckets, key, null, value);
                    if (successAdd) size++;
                    return successAdd;
                }
            }
        }

        bool AddToBuckets(Entry[] buckets, byte[] newKey, Entry newEntryOrNull, TValue value)
        {
            var h = (newEntryOrNull != null) ? newEntryOrNull.Hash : ByteArrayGetHashCode(newKey, 0, newKey.Length);
            if (buckets[h & (buckets.Length - 1)] == null)
            {
                if (newEntryOrNull != null)
                {
                    System.Threading.Volatile.Write(ref buckets[h & (buckets.Length - 1)], newEntryOrNull);
                }
                else
                {
                    System.Threading.Volatile.Write(ref buckets[h & (buckets.Length - 1)], new Entry { Key = newKey, Value = value, Hash = h });
                }
            }
            else
            {
                var searchLastEntry = buckets[h & (buckets.Length - 1)];
                while (true)
                {
                    if (ByteArrayEquals(searchLastEntry.Key, 0, searchLastEntry.Key.Length, newKey, 0, newKey.Length))
                    {
                        return false;
                    }

                    if (searchLastEntry.Next == null)
                    {
                        if (newEntryOrNull != null)
                        {
                            System.Threading.Volatile.Write(ref searchLastEntry.Next, newEntryOrNull);
                        }
                        else
                        {
                            System.Threading.Volatile.Write(ref searchLastEntry.Next, new Entry { Key = newKey, Value = value, Hash = h });
                        }
                        break;
                    }
                    searchLastEntry = searchLastEntry.Next;
                }
            }

            return true;
        }

        public bool TryGetValue(ArraySegment<byte> key, out TValue value)
        {
            var table = buckets;
            var hash = ByteArrayGetHashCode(key.Array, key.Offset, key.Count);
            var entry = table[hash & table.Length - 1];

            if (entry == null) goto NOT_FOUND;

            if (ByteArrayEquals(entry.Key, 0, entry.Key.Length, key.Array, key.Offset, key.Count))
            {
                value = entry.Value;
                return true;
            }

            var next = entry.Next;
            while (next != null)
            {
                if (ByteArrayEquals(next.Key, 0, next.Key.Length, key.Array, key.Offset, key.Count))
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

#if NETSTANDARD1_4
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        static uint ByteArrayGetHashCode(byte[] x, int offset, int count)
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
        static bool ByteArrayEquals(byte[] x, int xOffset, int xCount, byte[] y, int yOffset, int yCount)
        {
            // More improvement, use ReadOnlySpan<byte>.SequenceEqual.
            // or unsafe, retrieve long unroll loop.

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

        public IEnumerator<KeyValuePair<byte[], TValue>> GetEnumerator()
        {
            var b = this.buckets;

            foreach (var item in b)
            {
                if (item == null) continue;

                var n = item;
                while (n != null)
                {
                    yield return new KeyValuePair<byte[], TValue>(n.Key, n.Value);
                    n = n.Next;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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
