// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Nerdbank.Streams;

namespace PerfBenchmarkDotNet
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class MessagePackReaderBenchmark
    {
        private const int BufferLength = 10000;
        private byte[] buffer = new byte[BufferLength];

        [GlobalSetup]
        public void GlobalSetup()
        {
            for (int i = 0; i < this.buffer.Length; i++)
            {
                this.buffer[i] = newmsgpack::MessagePack.MessagePackCode.MaxFixInt;
            }
        }

        [Benchmark(OperationsPerInvoke = BufferLength)]
        [BenchmarkCategory("2.0")]
        public void ReadByte20()
        {
            var reader = new newmsgpack::MessagePack.MessagePackReader(this.buffer);
            for (int i = 0; i < this.buffer.Length; i++)
            {
                reader.ReadInt32();
            }
        }

        [Benchmark(OperationsPerInvoke = BufferLength)]
        [BenchmarkCategory("1.0")]
        public void ReadByte10()
        {
            int offset = 0;
            for (int i = 0; i < this.buffer.Length; i++)
            {
                oldmsgpack::MessagePack.MessagePackBinary.ReadInt32(this.buffer, offset, out int readSize);
                offset += readSize;
            }
        }
    }
}
