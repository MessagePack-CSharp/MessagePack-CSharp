// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

using BenchmarkDotNet.Attributes;
using newmsgpack::MessagePack;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    [Config(typeof(BenchmarkConfig))]
    public class Lz4Benchmark
    {
        private StringKeySerializerTarget2[] data;
        private MessagePackSerializerOptions lz4BlockOptions = MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4Block);
        private MessagePackSerializerOptions lz4ContiguousBlockOptions = MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4ContiguousBlock);
        private byte[] bin;
        private byte[] binLz4Block;
        private byte[] binLz4ContiguousBlock;

        public Lz4Benchmark()
        {
            var length = 1000;
            data = RandomFixtureKit.FixtureFactory.CreateMany<StringKeySerializerTarget2>(length);
            bin = MessagePackSerializer.Serialize(data);
            binLz4Block = MessagePackSerializer.Serialize(data, lz4BlockOptions);
            binLz4ContiguousBlock = MessagePackSerializer.Serialize(data, lz4ContiguousBlockOptions);
        }

        [Benchmark]
        public byte[] SerializeNone() => MessagePackSerializer.Serialize(data);

        [Benchmark]
        public byte[] SerializeLz4Block() => MessagePackSerializer.Serialize(data, lz4BlockOptions);

        [Benchmark]
        public byte[] SerializeLz4ContiguousBlock() => MessagePackSerializer.Serialize(data, lz4ContiguousBlockOptions);

        [Benchmark]
        public StringKeySerializerTarget2[] DeserializeNone() => MessagePackSerializer.Deserialize<StringKeySerializerTarget2[]>(bin);

        [Benchmark]
        public StringKeySerializerTarget2[] DeserializeLz4Block() => MessagePackSerializer.Deserialize<StringKeySerializerTarget2[]>(binLz4Block, lz4BlockOptions);

        [Benchmark]
        public StringKeySerializerTarget2[] DeserializeLz4ContiguousBlock() => MessagePackSerializer.Deserialize<StringKeySerializerTarget2[]>(binLz4ContiguousBlock, lz4ContiguousBlockOptions);
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

