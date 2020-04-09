// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

using System;
using BenchmarkDotNet.Attributes;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    [Config(typeof(BenchmarkConfig))]
    public class GuidImprov
    {
        private Guid guid;
        private byte[] bin;

        public GuidImprov()
        {
            this.guid = Guid.NewGuid();
            this.bin = newmsgpack.MessagePack.MessagePackSerializer.Serialize(this.guid);
        }

        [Benchmark]
        public byte[] NewSerialize()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize(this.guid);
        }

        [Benchmark]
        public byte[] OldSerialize()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(this.guid);
        }

        [Benchmark]
        public Guid NewDeserialize()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<Guid>(this.bin);
        }

        [Benchmark]
        public Guid OldDeserialize()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<Guid>(this.bin);
        }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

