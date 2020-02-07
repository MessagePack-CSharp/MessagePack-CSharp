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
                    byte[] e = array[i].Key;
                    if (key.AsSpan().SequenceEqual(e))
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(ReadOnlySpan<byte> key, out int value)
        {
            Entry[][] table = this.buckets;
            var hash = ByteArrayGetHashCode(key);
            Entry[] entry = table[hash & this.indexFor];

            if (entry == null)
            {
                value = default(int);
                return false;
            }

            ref Entry v = ref entry[0];
            if (key.SequenceEqual(v.Key))
            {
                value = v.Value;
                return true;
            }

            return TryGetValueSlow(key, entry, out value);
        }

        private bool TryGetValueSlow(ReadOnlySpan<byte> key, Entry[] entry, out int value)
        {
            for (int i = 1; i < entry.Length; i++)
            {
                ref Entry v = ref entry[i];
                if (key.SequenceEqual(v.Key))
                {
                    value = v.Value;
                    return true;
                }
            }

            value = default(int);
            return false;
        }

        private static readonly bool Is32Bit = IntPtr.Size == 4;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static ulong ByteArrayGetHashCode(ReadOnlySpan<byte> x)
        {
            // FarmHash https://github.com/google/farmhash
            if (Is32Bit)
            {
                return (ulong)FarmHash.Hash32(x);
            }
            else
            {
                return FarmHash.Hash64(x);
            }
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
                    yield return new KeyValuePair<string, int>(Encoding.UTF8.GetString(item2.Key), item2.Value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private struct Entry
        {
            public byte[] Key;
            public int Value;

            // for debugging
            public override string ToString()
            {
                return "(" + Encoding.UTF8.GetString(this.Key) + ", " + this.Value + ")";
            }
        }
    }
}
