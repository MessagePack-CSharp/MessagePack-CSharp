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

    [ShortRunJob]
    public class BooleanArrayBenchmarkMessagePackNoSimdVsMessagePackSimd
    {
        [Params(64, 1024, 16 * 1024 * 1024)]
        public int Size { get; set; }

        private bool[] input;
        private byte[] inputSerialized;
        private bool[] inputTrue;
        private bool[] inputFalse;

        [GlobalSetup]
        public void SetUp()
        {
            inputFalse = new bool[Size];
            inputTrue = new bool[Size];
            input = new bool[Size];

            var r = new Random();
            for (var i = 0; i < inputTrue.Length; i++)
            {
                inputTrue[i] = true;
                input[i] = r.Next(0, 2) == 0;
            }

            inputSerialized = newmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
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
        public bool[] DeSerializeSimd()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<bool[]>(inputSerialized);
        }

        [Benchmark]
        public bool[] DeserializeNoSimd()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<bool[]>(inputSerialized);
        }

        [Benchmark]
        public byte[] SerializeSimdFalse()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(inputFalse);
        }

        [Benchmark]
        public byte[] SerializeNoSimdFalse()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(inputFalse);
        }

        [Benchmark]
        public byte[] SerializeSimdTrue()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(inputTrue);
        }

        [Benchmark]
        public byte[] SerializeNoSimdTrue()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(inputTrue);
        }
    }

    [ShortRunJob]
    public class Int8ArrayBenchmarkMessagePackNoSimdVsMessagePackSimd
    {
        [Params(64, 1024, 16 * 1024 * 1024)]
        public int Size { get; set; }

        private sbyte[] input;
        private sbyte[] inputM32;
        private sbyte[] inputM33;
        private sbyte[] zero;

        [GlobalSetup]
        public void SetUp()
        {
            zero = new sbyte[Size];
            inputM33 = new sbyte[Size];
            inputM32 = new sbyte[Size];
            input = new sbyte[Size];

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

    [ShortRunJob]
    public class Int16ArrayBenchmarkMessagePackNoSimdVsMessagePackSimd
    {
        [Params(16, 1024, 16 * 1024 * 1024)]
        public int Size { get; set; }

        private short[] input;
        private short[] zero;

        [GlobalSetup]
        public void SetUp()
        {
            input = new short[Size];
            zero = new short[Size];
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

    [ShortRunJob]
    public class Int32ArrayBenchmarkMessagePackNoSimdVsMessagePackSimd
    {
        [Params(8, 1024, 16 * 1024 * 1024)]
        public int Size { get; set; }

        private int[] input;
        private int[] zero;
        private int[] inputShortMin;

        [GlobalSetup]
        public void SetUp()
        {
            input = new int[Size];
            zero = new int[Size];
            inputShortMin = new int[Size];
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

    [ShortRunJob]
    public class SingleArrayBenchmarkMessagePackNoSimdVsMessagePackSimd
    {
        [Params(64, 1024, 16 * 1024 * 1024)]
        public int Size { get; set; }
        
        private float[] input;

        [GlobalSetup]
        public void SetUp()
        {
            input = new float[Size];

            var r = new Random();
            for (var i = 0; i < input.Length; i++)
            {
                input[i] = (float)r.NextDouble();
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
    }
}
