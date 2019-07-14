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
    public class ImproveStringKeySerializeBenchmark
    {
        private static StringKeySerializerTarget stringData = new StringKeySerializerTarget();

        [Benchmark]
        public byte[] OldSerialize()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize<StringKeySerializerTarget>(stringData);
        }

        [Benchmark(Baseline = true)]
        public byte[] NewSerialize()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize<StringKeySerializerTarget>(stringData);
        }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

