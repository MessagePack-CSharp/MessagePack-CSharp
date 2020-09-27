// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using System;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

#pragma warning disable SA1649 // File name should match first type name

namespace Benchmark
{
    [newmsgpack::MessagePack.MessagePackObjectAttribute]
    public struct Matrix4x4
    {
        [newmsgpack::MessagePack.Key(0)] public float M11;
        [newmsgpack::MessagePack.Key(1)] public float M12;
        [newmsgpack::MessagePack.Key(2)] public float M13;
        [newmsgpack::MessagePack.Key(3)] public float M14;
        [newmsgpack::MessagePack.Key(4)] public float M21;
        [newmsgpack::MessagePack.Key(5)] public float M22;
        [newmsgpack::MessagePack.Key(6)] public float M23;
        [newmsgpack::MessagePack.Key(7)] public float M24;
        [newmsgpack::MessagePack.Key(8)] public float M31;
        [newmsgpack::MessagePack.Key(9)] public float M32;
        [newmsgpack::MessagePack.Key(10)] public float M33;
        [newmsgpack::MessagePack.Key(11)] public float M34;
        [newmsgpack::MessagePack.Key(12)] public float M41;
        [newmsgpack::MessagePack.Key(13)] public float M42;
        [newmsgpack::MessagePack.Key(14)] public float M43;
        [newmsgpack::MessagePack.Key(15)] public float M44;

        public Matrix4x4(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24, float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
        {
            M11 = m11;
            M12 = m12;
            M13 = m13;
            M14 = m14;
            M21 = m21;
            M22 = m22;
            M23 = m23;
            M24 = m24;
            M31 = m31;
            M32 = m32;
            M33 = m33;
            M34 = m34;
            M41 = m41;
            M42 = m42;
            M43 = m43;
            M44 = m44;
        }
    }

    [ShortRunJob]
    public class UnsafeUnmanagedStructArrayBenchmark
    {
        [Params(1, 64, 1024, 16 * 1024 * 1024)]
        public int Size { get; set; }

        private Matrix4x4[] input;
        private byte[] inputSerializedUnsafe;
        private byte[] inputSerializedNormal;

        private newmsgpack::MessagePack.MessagePackSerializerOptions options;

        [GlobalSetup]
        public void SetUp()
        {
            var resolver = newmsgpack::MessagePack.Resolvers.CompositeResolver.Create(new newmsgpack::MessagePack.Formatters.IMessagePackFormatter[] { new newmsgpack.MessagePack.Formatters.UnsafeUnmanagedStructArrayFormatter<Matrix4x4>(50) }, new[] { newmsgpack::MessagePack.Resolvers.StandardResolver.Instance });
            options = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);

            input = new Matrix4x4[Size];
            var r = new Random();
            for (var i = 0; i < input.Length; i++)
            {
                input[i] = new Matrix4x4((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());
            }

            inputSerializedUnsafe = newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options);
            inputSerializedNormal = newmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
        }

        [Benchmark]
        public byte[] SerializeUnsafe()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options);
        }

        [Benchmark]
        public byte[] SerializeNormal()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
        }

        [Benchmark]
        public Matrix4x4[] DeserializeUnsafe()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<Matrix4x4[]>(inputSerializedUnsafe, options);
        }

        [Benchmark]
        public Matrix4x4[] DeserializeNormal()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<Matrix4x4[]>(inputSerializedNormal);
        }
    }

    [ShortRunJob]
    public class BooleanArrayBenchmarkMessagePackNoSingleInstructionMultipleDataVsMessagePackSingleInstructionMultipleData
    {
        [Params(64, 1024, 16 * 1024 * 1024)]
        public int Size { get; set; }

        private bool[] input;
        private byte[] inputSerialized;
        private bool[] inputTrue;
        private bool[] inputFalse;
        private newmsgpack::MessagePack.MessagePackSerializerOptions options;

        [GlobalSetup]
        public void SetUp()
        {
            var resolver = newmsgpack::MessagePack.Resolvers.CompositeResolver.Create(newmsgpack::MessagePack.Resolvers.PrimitiveArrayResolver.Instance, newmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
            options = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);

            inputFalse = new bool[Size];
            inputTrue = new bool[Size];
            input = new bool[Size];

            var r = new Random();
            for (var i = 0; i < inputTrue.Length; i++)
            {
                inputTrue[i] = true;
                input[i] = r.Next(0, 2) == 0;
            }

            inputSerialized = newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options);

            if (!oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input).SequenceEqual(inputSerialized))
            {
                throw new InvalidProgramException();
            }
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleData()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleData()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
        }

        [Benchmark]
        public bool[] DeSerializeSingleInstructionMultipleData()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<bool[]>(inputSerialized, options);
        }

        [Benchmark]
        public bool[] DeserializeNoSingleInstructionMultipleData()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<bool[]>(inputSerialized);
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleDataFalse()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(inputFalse, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleDataFalse()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(inputFalse);
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleDataTrue()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(inputTrue, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleDataTrue()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(inputTrue);
        }
    }

    [ShortRunJob]
    public class Int8ArrayBenchmarkMessagePackNoSingleInstructionMultipleDataVsMessagePackSingleInstructionMultipleData
    {
        [Params(64, 1024, 16 * 1024 * 1024)]
        public int Size { get; set; }

        private sbyte[] input;
        private sbyte[] inputM32;
        private sbyte[] inputM33;
        private sbyte[] zero;
        private newmsgpack::MessagePack.MessagePackSerializerOptions options;

        [GlobalSetup]
        public void SetUp()
        {
            var resolver = newmsgpack::MessagePack.Resolvers.CompositeResolver.Create(newmsgpack::MessagePack.Resolvers.PrimitiveArrayResolver.Instance, newmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
            options = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);

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

            if (!oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input).SequenceEqual(newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options)))
            {
                throw new InvalidProgramException();
            }
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleData()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleData()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleDataZero()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(zero, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleDataZero()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(zero);
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleDataM32()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(inputM32, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleDataM32()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(inputM32);
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleDataM33()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(inputM33, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleDataM33()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(inputM33);
        }
    }

    [ShortRunJob]
    public class Int16ArrayBenchmarkMessagePackNoSingleInstructionMultipleDataVsMessagePackSingleInstructionMultipleData
    {
        [Params(16, 1024, 16 * 1024 * 1024)]
        public int Size { get; set; }

        private newmsgpack::MessagePack.MessagePackSerializerOptions options;
        private short[] input;
        private short[] zero;

        [GlobalSetup]
        public void SetUp()
        {
            var resolver = newmsgpack::MessagePack.Resolvers.CompositeResolver.Create(newmsgpack::MessagePack.Resolvers.PrimitiveArrayResolver.Instance, newmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
            options = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);

            input = new short[Size];
            zero = new short[Size];
            var r = new Random();
            r.NextBytes(MemoryMarshal.AsBytes(input.AsSpan()));

            if (!oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input).SequenceEqual(newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options)))
            {
                throw new InvalidProgramException();
            }
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleData()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleData()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleDataZero()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(zero, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleDataZero()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(zero);
        }
    }

    [ShortRunJob]
    public class Int32ArrayBenchmarkMessagePackNoSingleInstructionMultipleDataVsMessagePackSingleInstructionMultipleData
    {
        [Params(8, 1024, 16 * 1024 * 1024)]
        public int Size { get; set; }

        private newmsgpack::MessagePack.MessagePackSerializerOptions options;
        private int[] input;
        private int[] zero;
        private int[] inputShortMin;

        [GlobalSetup]
        public void SetUp()
        {
            var resolver = newmsgpack::MessagePack.Resolvers.CompositeResolver.Create(newmsgpack::MessagePack.Resolvers.PrimitiveArrayResolver.Instance, newmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
            options = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);

            input = new int[Size];
            zero = new int[Size];
            inputShortMin = new int[Size];
            var r = new Random();
            r.NextBytes(MemoryMarshal.AsBytes(input.AsSpan()));
            for (var i = 0; i < inputShortMin.Length; i++)
            {
                inputShortMin[i] = short.MinValue;
            }

            if (!oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input).SequenceEqual(newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options)))
            {
                throw new InvalidProgramException();
            }
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleData()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleData()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleDataZero()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(zero, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleDataZero()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(zero);
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleDataShortMin()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(inputShortMin, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleDataShortMin()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(inputShortMin);
        }
    }

    [ShortRunJob]
    public class SingleArrayBenchmarkMessagePackNoSingleInstructionMultipleDataVsMessagePackSingleInstructionMultipleData
    {
        [Params(64, 1024, 16 * 1024 * 1024)]
        public int Size { get; set; }

        private newmsgpack::MessagePack.MessagePackSerializerOptions options;
        private float[] input;

        [GlobalSetup]
        public void SetUp()
        {
            var resolver = newmsgpack::MessagePack.Resolvers.CompositeResolver.Create(newmsgpack::MessagePack.Resolvers.PrimitiveArrayResolver.Instance, newmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
            options = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);
            input = new float[Size];

            var r = new Random();
            for (var i = 0; i < input.Length; i++)
            {
                input[i] = (float)r.NextDouble();
            }

            if (!oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input).SequenceEqual(newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options)))
            {
                throw new InvalidProgramException();
            }
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleData()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleData()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
        }
    }

    [ShortRunJob]
    public class DoubleArrayBenchmarkMessagePackNoSingleInstructionMultipleDataVsMessagePackSingleInstructionMultipleData
    {
        [Params(64, 1024, 16 * 1024 * 1024)]
        public int Size { get; set; }

        private newmsgpack::MessagePack.MessagePackSerializerOptions options;
        private double[] input;

        [GlobalSetup]
        public void SetUp()
        {
            var resolver = newmsgpack::MessagePack.Resolvers.CompositeResolver.Create(newmsgpack::MessagePack.Resolvers.PrimitiveArrayResolver.Instance, newmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
            options = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);
            input = new double[Size];

            var r = new Random();
            for (var i = 0; i < input.Length; i++)
            {
                input[i] = r.NextDouble();
            }

            if (!oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input).SequenceEqual(newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options)))
            {
                throw new InvalidProgramException();
            }
        }

        [Benchmark]
        public byte[] SerializeSingleInstructionMultipleData()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(input, options);
        }

        [Benchmark]
        public byte[] SerializeNoSingleInstructionMultipleData()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(input);
        }
    }
}
