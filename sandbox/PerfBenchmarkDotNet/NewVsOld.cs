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
    public class NewVsOld
    {
        private byte[] bin;

        public NewVsOld()
        {
            this.bin = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new StringKeySerializerTarget());
        }

        [Benchmark(Baseline = true)]
        public StringKeySerializerTarget New()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(this.bin, newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }

        [Benchmark]
        public StringKeySerializerTarget Old()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(this.bin, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

