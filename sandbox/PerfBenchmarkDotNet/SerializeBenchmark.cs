// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using GeneratedFormatter.MessagePack.Formatters;
using MsgPack.Serialization;
using Newtonsoft.Json;
using ZeroFormatter;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    [Config(typeof(BenchmarkConfig))]
    public class SerializeBenchmark
    {
        private static MsgPack.Serialization.SerializationContext mapContext = new MsgPack.Serialization.SerializationContext { SerializationMethod = SerializationMethod.Map };
        private static MsgPack.Serialization.SerializationContext arrayContext = new MsgPack.Serialization.SerializationContext { SerializationMethod = SerializationMethod.Array };
        private static JsonSerializer jsonSerialzier = new JsonSerializer();
        private static Hyperion.Serializer hyperionSerializer = new Hyperion.Serializer();
        private static newmsgpack::MessagePack.IFormatterResolver mpcGenFormatterResolver = new Resolver(new StringKeySerializerTargetFormatter_MpcGeneratedAutomata());
        private static newmsgpack::MessagePack.IFormatterResolver mpcGenDictFormatterResolver = new Resolver(new StringKeySerializerTargetFormatter_MpcGeneratedDictionary());
        private static IntKeySerializerTarget intData = new IntKeySerializerTarget();
        private static StringKeySerializerTarget stringData = new StringKeySerializerTarget();

        [Benchmark(Baseline = true)]
        public byte[] IntKey()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize<IntKeySerializerTarget>(intData);
        }

        [Benchmark]
        public byte[] StringKey()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize<StringKeySerializerTarget>(stringData);
        }

        [Benchmark]
        public byte[] Typeless_IntKey()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Typeless.Serialize(intData);
        }

        [Benchmark]
        public byte[] Typeless_StringKey()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Typeless.Serialize(stringData);
        }

        [Benchmark]
        public byte[] MsgPackCliMap()
        {
            return mapContext.GetSerializer<IntKeySerializerTarget>().PackSingleObject(intData);
        }

        [Benchmark]
        public byte[] MsgPackCliArray()
        {
            return arrayContext.GetSerializer<IntKeySerializerTarget>().PackSingleObject(intData);
        }

        [Benchmark]
        public byte[] ProtobufNet()
        {
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize<IntKeySerializerTarget>(ms, intData);
                return ms.ToArray();
            }
        }

        [Benchmark]
        public byte[] Hyperion()
        {
            using (var ms = new MemoryStream())
            {
                hyperionSerializer.Serialize(intData, ms);
                return ms.ToArray();
            }
        }

        [Benchmark]
        public byte[] ZeroFormatter()
        {
            return ZeroFormatterSerializer.Serialize(intData);
        }

        [Benchmark]
        public byte[] JsonNetString()
        {
            return Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(intData));
        }

        [Benchmark]
        public byte[] JsonNetStreamWriter()
        {
            using (var ms = new MemoryStream())
            {
                using (var sr = new StreamWriter(ms, Encoding.UTF8))
                using (var jr = new JsonTextWriter(sr))
                {
                    jsonSerialzier.Serialize(jr, intData);
                }

                return ms.ToArray();
            }
        }

        [Benchmark]
        public byte[] JilString()
        {
            return Encoding.UTF8.GetBytes(Jil.JSON.Serialize<IntKeySerializerTarget>(intData));
        }

        [Benchmark]
        public byte[] JilStreamWriter()
        {
            using (var ms = new MemoryStream())
            {
                using (var sr = new StreamWriter(ms, Encoding.UTF8))
                {
                    Jil.JSON.Serialize<IntKeySerializerTarget>(intData, sr);
                }

                return ms.ToArray();
            }
        }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

