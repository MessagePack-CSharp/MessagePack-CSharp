// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

using System;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    [Config(typeof(BenchmarkConfig))]
    public class DictionaryLookupCompare
    {
        private newmsgpack::MessagePack.Internal.ByteArrayStringHashTable hashTable;
        private newmsgpack::MessagePack.Internal.AutomataDictionary automata;
        private byte[][] keys;

        public DictionaryLookupCompare()
        {
            this.hashTable = new newmsgpack::MessagePack.Internal.ByteArrayStringHashTable(9);
            this.automata = new newmsgpack::MessagePack.Internal.AutomataDictionary();
            this.keys = new byte[9][];
            foreach (var item in Enumerable.Range(0, 9).Select(x => new { str = "MyProperty" + (x + 1), i = x }))
            {
                this.hashTable.Add(Encoding.UTF8.GetBytes(item.str), item.i);
                this.automata.Add(item.str, item.i);
                this.keys[item.i] = Encoding.UTF8.GetBytes(item.str);
            }
        }

        [Benchmark]
        public void Automata()
        {
            for (int i = 0; i < this.keys.Length; i++)
            {
                this.automata.TryGetValue(this.keys[i], out _);
            }
        }

        [Benchmark]
        public void Hashtable()
        {
            for (int i = 0; i < this.keys.Length; i++)
            {
                this.hashTable.TryGetValue(new ArraySegment<byte>(this.keys[i], 0, this.keys[i].Length), out _);
            }
        }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

