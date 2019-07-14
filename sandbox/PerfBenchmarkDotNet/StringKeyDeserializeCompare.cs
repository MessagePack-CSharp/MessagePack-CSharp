// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

using BenchmarkDotNet.Attributes;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    [Config(typeof(BenchmarkConfig))]
    public class StringKeyDeserializeCompare
    {
        private byte[] bin;
        private byte[] binIntKey;
        private newmsgpack::MessagePack.MessagePackSerializerOptions automata;
        private newmsgpack::MessagePack.MessagePackSerializerOptions hashtable;

        public StringKeyDeserializeCompare()
        {
            this.bin = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new StringKeySerializerTarget());
            this.binIntKey = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new IntKeySerializerTarget());
            this.automata = newmsgpack::MessagePack.MessagePackSerializerOptions.Default.WithResolver(new Resolver(new GeneratedFormatter.MessagePack.Formatters.StringKeySerializerTargetFormatter_AutomataLookup()));
            this.hashtable = newmsgpack::MessagePack.MessagePackSerializerOptions.Default.WithResolver(new Resolver(new GeneratedFormatter.MessagePack.Formatters.StringKeySerializerTargetFormatter_ByteArrayStringHashTable()));
        }

        [Benchmark(Baseline = true)]
        public IntKeySerializerTarget IntKey()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<IntKeySerializerTarget>(this.binIntKey);
        }

        [Benchmark]
        public StringKeySerializerTarget OldStringKey()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(this.bin, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public StringKeySerializerTarget Automata()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(this.bin, this.automata);
        }

        [Benchmark]
        public StringKeySerializerTarget Hashtable()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(this.bin, this.hashtable);
        }

        [Benchmark]
        public StringKeySerializerTarget AutomataInlineEmit()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(this.bin, newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

