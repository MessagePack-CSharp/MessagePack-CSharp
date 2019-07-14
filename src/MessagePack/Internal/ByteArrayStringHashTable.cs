// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Internal
{
    // like ArraySegment<byte> hashtable.
    // Add is safe for construction phase only and requires capacity(does not do rehash)
    // and specialized for internal use(nongenerics, TValue is int)

    // internal, but code generator requires this class
    // or at least PerfBenchmarkDotNet
    public class ByteArrayStringHashTable : IEnumerable<KeyValuePair<string, int>>
    {
        private readonly Entry[][] buckets; // immutable array(faster than linkedlist)
        private readonly ulong indexFor;

        public ByteArrayStringHashTable(int capacity)
            : this(capacity, 0.42f) // default: 0.75f -> 0.42f
        {
        }

        public ByteArrayStringHashTable(int capacity, float loadFactor)
        {
            var tableSize = CalculateCapacity(capacity, loadFactor);
            this.buckets = new Entry[tableSize][];
            this.indexFor = (ulong)this.buckets.Length - 1;
        }

        public void Add(string key, int value)
        {
            if (!this.TryAddInternal(Encoding.UTF8.GetBytes(key), value))
            {
                throw new ArgumentException("Key was already exists. Key:" + key);
            }
        }

        public void Add(byte[] key, int value)
        {
            if (!this.TryAddInternal(key, value))
            {
                throw new ArgumentException("Key was already exists. Key:" + key);
            }
        }

        private bool TryAddInternal(byte[] key, int value)
        {
            var h = ByteArrayGetHashCode(key);
            var entry = new Entry { Key = key, Value = value };

            Entry[] array = this.buckets[h & this.indexFor];
            if (array == null)
            {
                this.buckets[h & this.indexFor] = new[] { entry };
            }
            else
            {
                // check duplicate
                for (int i = 0; i < array.Length; i++)
                {
                    Memory<byte> e = array[i].Key;
                    if (ByteArrayComparer.Equals(key, e.Span))
                    {
                        return false;
                    }
                }

                var newArray = new Entry[array.Length + 1];
                Array.Copy(array, newArray, array.Length);
                array = newArray;
                array[array.Length - 1] = entry;
                this.buckets[h & this.indexFor] = array;
            }

            return true;
        }

        public bool TryGetValue(in ReadOnlySequence<byte> key, out int value) => this.TryGetValue(CodeGenHelpers.GetSpanFromSequence(key), out value);

        public bool TryGetValue(ReadOnlySpan<byte> key, out int value)
        {
            Entry[][] table = this.buckets;
            var hash = ByteArrayGetHashCode(key);
            Entry[] entry = table[hash & this.indexFor];

            if (entry == null)
            {
                goto NOT_FOUND;
            }

            {
#if !UNITY
                ref Entry v = ref entry[0];
#else
                var v = entry[0];
#endif
                if (ByteArrayComparer.Equals(key, v.Key.Span))
                {
                    value = v.Value;
                    return true;
                }
            }

            for (int i = 1; i < entry.Length; i++)
            {
#if !UNITY
                ref Entry v = ref entry[i];
#else
                var v = entry[i];
#endif
                if (ByteArrayComparer.Equals(key, v.Key.Span))
                {
                    value = v.Value;
                    return true;
                }
            }

NOT_FOUND:
            value = default(int);
            return false;
        }

#if !UNITY
        private static readonly bool Is32Bit = IntPtr.Size == 4;
#endif

#if !UNITY
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        private static ulong ByteArrayGetHashCode(ReadOnlySpan<byte> x)
        {
#if !UNITY
            // FarmHash https://github.com/google/farmhash
            if (x == null)
            {
                return 0;
            }

            if (Is32Bit)
            {
                return (ulong)FarmHash.Hash32(x);
            }
            else
            {
                return FarmHash.Hash64(x);
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

        // only for Debug use
        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            Entry[][] b = this.buckets;

            foreach (Entry[] item in b)
            {
                if (item == null)
                {
                    continue;
                }

                foreach (Entry item2 in item)
                {
                    yield return new KeyValuePair<string, int>(Encoding.UTF8.GetString(item2.Key.Span), item2.Value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private struct Entry
        {
            public Memory<byte> Key;
            public int Value;

            // for debugging
            public override string ToString()
            {
                return "(" + Encoding.UTF8.GetString(this.Key.Span) + ", " + this.Value + ")";
            }
        }
    }
}
