// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;
using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace Benchmark
{
    [ShortRunJob]
    public class StringBenchmark_MessagePackNoSimd_Vs_MessagePackSimd
    {
        [Params("", "abcdefghijklmnopqrstuvwxyz0123456", "abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456abcdefghijklmnopqrstuvwxyz0123456")]
        public string Text { get; set; }

        [Benchmark]
        public byte[] SerializeSimd()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(Text);
        }

        [Benchmark]
        public byte[] SerializeNoSimd()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(Text);
        }
    }

    public class SByteArrayBenchmark_MessagePackNoSimd_Vs_MessagePackSimd
    {
        public sbyte[] Input = new sbyte[16 * 1024 * 1024];

        [GlobalSetup]
        public void SetUp()
        {
            var r = new Random();
            r.NextBytes(MemoryMarshal.AsBytes(Input.AsSpan()));
        }

        [Benchmark]
        public byte[] SerializeSimd()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(Input);
        }

        [Benchmark]
        public byte[] SerializeNoSimd()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(Input);
        }
    }
}
