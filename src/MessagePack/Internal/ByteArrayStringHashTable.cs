using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Internal
{
    // like ArraySegment<byte> hashtable.
    // Add is safe for construction phase only and requires capacity(does not do rehash)
    // and specialized for internal use(nongenerics, TValue is int)

    // internal, but code generator requires this class
    public class ByteArrayStringHashTable : IEnumerable<KeyValuePair<string, int>>
    {
        readonly Entry[][] buckets; // immutable array(faster than linkedlist)
        readonly ulong indexFor;

        public ByteArrayStringHashTable(int capacity)
            : this(capacity, 0.42f) // default: 0.75f -> 0.42f
        {
        }

        public ByteArrayStringHashTable(int capacity, float loadFactor)
        {
            var tableSize = CalculateCapacity(capacity, loadFactor);
            this.buckets = new Entry[tableSize][];
            this.indexFor = (ulong)buckets.Length - 1;
        }

        public void Add(string key, int value)
        {
            if (!TryAddInternal(Encoding.UTF8.GetBytes(key), value))
            {
                throw new ArgumentException("Key was already exists. Key:" + key);
            }
        }

        public void Add(byte[] key, int value)
        {
            if (!TryAddInternal(key, value))
            {
                throw new ArgumentException("Key was already exists. Key:" + key);
            }
        }

        bool TryAddInternal(byte[] key, int value)
        {
            var h = ByteArrayGetHashCode(key, 0, key.Length);
            var entry = new Entry { Key = key, Value = value };

            var array = buckets[h & (indexFor)];
            if (array == null)
            {
                buckets[h & (indexFor)] = new[] { entry };
            }
            else
            {
                // check duplicate
                for (int i = 0; i < array.Length; i++)
                {
                    var e = array[i].Key;
                    if (ByteArrayComparer.Equals(key, 0, key.Length, e))
                    {
                        return false;
                    }
                }

                var newArray = new Entry[array.Length + 1];
                Array.Copy(array, newArray, array.Length);
                array = newArray;
                array[array.Length - 1] = entry;
                buckets[h & (indexFor)] = array;
            }

            return true;
        }

        public bool TryGetValue(ArraySegment<byte> key, out int value)
        {
            var table = buckets;
            var hash = ByteArrayGetHashCode(key.Array, key.Offset, key.Count);
            var entry = table[hash & indexFor];

            if (entry == null) goto NOT_FOUND;

            {
#if NETSTANDARD
                ref var v = ref entry[0];
#else
                var v = entry[0];
#endif
                if (ByteArrayComparer.Equals(key.Array, key.Offset, key.Count, v.Key))
                {
                    value = v.Value;
                    return true;
                }
            }

            for (int i = 1; i < entry.Length; i++)
            {
#if NETSTANDARD
                ref var v = ref entry[i];
#else
                var v = entry[i];
#endif
                if (ByteArrayComparer.Equals(key.Array, key.Offset, key.Count, v.Key))
                {
                    value = v.Value;
                    return true;
                }
            }

            NOT_FOUND:
            value = default(int);
            return false;
        }

#if NETSTANDARD
        static readonly bool Is32Bit = (IntPtr.Size == 4);
#endif

#if NETSTANDARD
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        static ulong ByteArrayGetHashCode(byte[] x, int offset, int count)
        {
#if NETSTANDARD
            // FarmHash https://github.com/google/farmhash
            if (x == null) return 0;

            if (Is32Bit)
            {
                return (ulong)FarmHash.Hash32(x, offset, count);
            }
            else
            {
                return FarmHash.Hash64(x, offset, count);
            }

#else

            // FNV1-1a 32bit https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
            uint hash = 0;
            if (x != null)
            {
                var max = offset + count;

                hash = 2166136261;
                for (int i = offset; i < max; i++)
                {
                    hash = unchecked((x[i] ^ hash) * 16777619);
                }
            }

            return (ulong)hash;

#endif
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

        // only for Debug use
        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            var b = this.buckets;

            foreach (var item in b)
            {
                if (item == null) continue;
                foreach (var item2 in item)
                {
                    yield return new KeyValuePair<string, int>(Encoding.UTF8.GetString(item2.Key), item2.Value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        struct Entry
        {
            public byte[] Key;
            public int Value;

            // for debugging
            public override string ToString()
            {
                return "(" + Encoding.UTF8.GetString(Key) + ", " + Value + ")";
            }
        }
    }
}
