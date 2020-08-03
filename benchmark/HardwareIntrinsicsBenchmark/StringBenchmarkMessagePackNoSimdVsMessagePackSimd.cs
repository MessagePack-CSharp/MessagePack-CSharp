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
    public class StringBenchmarkMessagePackNoSimdVsMessagePackSimd
    {
        [Params("")]
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

    public class SByteArrayBenchmarkMessagePackNoSimdVsMessagePackSimd
    {
        private readonly sbyte[] input = new sbyte[16 * 1024 * 1024];
        private readonly sbyte[] zero = new sbyte[16 * 1024 * 1024];

        [GlobalSetup]
        public void SetUp()
        {
            var r = new Random();
            r.NextBytes(MemoryMarshal.AsBytes(input.AsSpan()));
        }

        [Benchmark]
        public byte[] SerializeSimd()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
        }

        [Benchmark]
        public byte[] SerializeNoSimd()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
        }

        [Benchmark]
        public byte[] SerializeSimdZero()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(zero);
        }

        [Benchmark]
        public byte[] SerializeNoSimdZero()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(zero);
        }
    }

    public class Int16ArrayBenchmarkMessagePackNoSimdVsMessagePackSimd
    {
        private readonly short[] input = new short[16 * 1024 * 1024];
        private readonly short[] zero = new short[16 * 1024 * 1024];

        [GlobalSetup]
        public void SetUp()
        {
            var r = new Random();
            r.NextBytes(MemoryMarshal.AsBytes(input.AsSpan()));
        }

        [Benchmark]
        public byte[] SerializeSimd()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
        }

        [Benchmark]
        public byte[] SerializeNoSimd()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
        }

        [Benchmark]
        public byte[] SerializeSimdZero()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(zero);
        }

        [Benchmark]
        public byte[] SerializeNoSimdZero()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(zero);
        }
    }
}
