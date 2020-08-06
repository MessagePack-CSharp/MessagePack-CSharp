// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using System;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

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

    [HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.CacheMisses)]
    public class Int8ArrayBenchmarkMessagePackNoSimdVsMessagePackSimd
    {
        private readonly sbyte[] input = new sbyte[16 * 1024 * 1024];
        private readonly sbyte[] inputM32 = new sbyte[16 * 1024 * 1024];
        private readonly sbyte[] inputM33 = new sbyte[16 * 1024 * 1024];
        private readonly sbyte[] zero = new sbyte[16 * 1024 * 1024];

        [GlobalSetup]
        public void SetUp()
        {
            var r = new Random();
            r.NextBytes(MemoryMarshal.AsBytes(input.AsSpan()));
            for (var i = 0; i < inputM32.Length; i++)
            {
                inputM32[i] = -32;
                inputM33[i] = -33;
            }
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

        [Benchmark]
        public byte[] SerializeSimdM32()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(inputM32);
        }

        [Benchmark]
        public byte[] SerializeNoSimdM32()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(inputM32);
        }

        [Benchmark]
        public byte[] SerializeSimdM33()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(inputM33);
        }

        [Benchmark]
        public byte[] SerializeNoSimdM33()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(inputM33);
        }
    }

    [HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.CacheMisses)]
    public class Int16ArrayBenchmarkMessagePackNoSimdVsMessagePackSimd
    {
        private readonly short[] input = new short[16 * 1024 * 1024];
        private readonly short[] zero = new short[16 * 1024 * 1024];

        [GlobalSetup]
        public void SetUp()
        {
            var r = new Random();
            r.NextBytes(MemoryMarshal.AsBytes(input.AsSpan()));

            Console.WriteLine(newmsgpack::MessagePack.MessagePackSerializer.Serialize(input).Length);
            Console.WriteLine(oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input).Length);
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

    [HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.CacheMisses)]
    public class Int32ArrayBenchmarkMessagePackNoSimdVsMessagePackSimd
    {
        private readonly int[] input = new int[16 * 1024 * 1024];
        private readonly int[] zero = new int[16 * 1024 * 1024];
        private readonly int[] inputShortMin = new int[16 * 1024 * 1024];

        [GlobalSetup]
        public void SetUp()
        {
            var r = new Random();
            r.NextBytes(MemoryMarshal.AsBytes(input.AsSpan()));
            for (var i = 0; i < inputShortMin.Length; i++)
            {
                inputShortMin[i] = short.MinValue;
            }
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

        [Benchmark]
        public byte[] SerializeSimdShortMin()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(inputShortMin);
        }

        [Benchmark]
        public byte[] SerializeNoSimdShortMin()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(inputShortMin);
        }
    }
}
