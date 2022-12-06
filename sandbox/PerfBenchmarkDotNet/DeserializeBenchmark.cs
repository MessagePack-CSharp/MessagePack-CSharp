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

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    [Config(typeof(BenchmarkConfig))]
    public class DeserializeBenchmark
    {
        private static MsgPack.Serialization.SerializationContext mapContext = new MsgPack.Serialization.SerializationContext { SerializationMethod = SerializationMethod.Map };
        private static MsgPack.Serialization.SerializationContext arrayContext = new MsgPack.Serialization.SerializationContext { SerializationMethod = SerializationMethod.Array };
        private static byte[] intObj = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new IntKeySerializerTarget());
        private static byte[] stringKeyObj = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new StringKeySerializerTarget());
        private static byte[] typelessWithIntKeyObj = newmsgpack.MessagePack.MessagePackSerializer.Typeless.Serialize(new IntKeySerializerTarget());
        private static byte[] typelessWithStringKeyObj = newmsgpack.MessagePack.MessagePackSerializer.Typeless.Serialize(new StringKeySerializerTarget());
        private static byte[] mapObj = mapContext.GetSerializer<IntKeySerializerTarget>().PackSingleObject(new IntKeySerializerTarget());
        private static byte[] arrayObj = arrayContext.GetSerializer<IntKeySerializerTarget>().PackSingleObject(new IntKeySerializerTarget());
        private static byte[] protoObj;
        private static string jsonnetObj = Newtonsoft.Json.JsonConvert.SerializeObject(new IntKeySerializerTarget());
        private static byte[] jsonnetByteArray = Encoding.UTF8.GetBytes(jsonnetObj);
        private static string jilObj = Jil.JSON.Serialize(new IntKeySerializerTarget());
        private static byte[] jilByteArray = Encoding.UTF8.GetBytes(jilObj);
        private static JsonSerializer jsonSerialzier = new JsonSerializer();
        private static Hyperion.Serializer hyperionSerializer = new Hyperion.Serializer();
        private static byte[] hyperionObj;

        private static newmsgpack::MessagePack.MessagePackSerializerOptions mpcGenFormatterResolver = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(new Resolver(new StringKeySerializerTargetFormatter_MpcGeneratedAutomata()));
        private static newmsgpack::MessagePack.MessagePackSerializerOptions mpcGenDictFormatterResolver = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(new Resolver(new StringKeySerializerTargetFormatter_MpcGeneratedDictionary()));

        static DeserializeBenchmark()
        {
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, new IntKeySerializerTarget());
                protoObj = ms.ToArray();
            }

            using (var ms = new MemoryStream())
            {
                hyperionSerializer.Serialize(new IntKeySerializerTarget(), ms);
                hyperionObj = ms.ToArray();
            }
        }

        [Benchmark(Baseline = true)]
        public IntKeySerializerTarget IntKey()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<IntKeySerializerTarget>(intObj);
        }

        [Benchmark]
        public IntKeySerializerTarget IntKey_NonGeneric()
        {
            return (IntKeySerializerTarget)newmsgpack.MessagePack.MessagePackSerializer.Deserialize(typeof(IntKeySerializerTarget), intObj);
        }

        [Benchmark]
        public StringKeySerializerTarget StringKey()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(stringKeyObj);
        }

        // [Benchmark]
        public StringKeySerializerTarget StringKey_MpcGenerated()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(stringKeyObj, mpcGenFormatterResolver);
        }

        // [Benchmark]
        public StringKeySerializerTarget StringKey_MpcGeneratedDict()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(stringKeyObj, mpcGenDictFormatterResolver);
        }

        [Benchmark]
        public IntKeySerializerTarget Typeless_IntKey()
        {
            return (IntKeySerializerTarget)newmsgpack.MessagePack.MessagePackSerializer.Typeless.Deserialize(typelessWithIntKeyObj);
        }

        [Benchmark]
        public StringKeySerializerTarget Typeless_StringKey()
        {
            return (StringKeySerializerTarget)newmsgpack.MessagePack.MessagePackSerializer.Typeless.Deserialize(typelessWithStringKeyObj);
        }

        [Benchmark]
        public IntKeySerializerTarget MsgPackCliMap()
        {
            return mapContext.GetSerializer<IntKeySerializerTarget>().UnpackSingleObject(mapObj);
        }

        [Benchmark]
        public IntKeySerializerTarget MsgPackCliArray()
        {
            return arrayContext.GetSerializer<IntKeySerializerTarget>().UnpackSingleObject(arrayObj);
        }

        [Benchmark]
        public IntKeySerializerTarget ProtobufNet()
        {
            using (var ms = new MemoryStream(protoObj))
            {
                return ProtoBuf.Serializer.Deserialize<IntKeySerializerTarget>(ms);
            }
        }

        [Benchmark]
        public IntKeySerializerTarget Hyperion()
        {
            using (var ms = new MemoryStream(hyperionObj))
            {
                return hyperionSerializer.Deserialize<IntKeySerializerTarget>(ms);
            }
        }

        [Benchmark]
        public IntKeySerializerTarget JsonNetString()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<IntKeySerializerTarget>(jsonnetObj);
        }

        [Benchmark]
        public IntKeySerializerTarget JsonNetStreamReader()
        {
            using (var ms = new MemoryStream(jsonnetByteArray))
            using (var sr = new StreamReader(ms, Encoding.UTF8))
            using (var jr = new JsonTextReader(sr))
            {
                return jsonSerialzier.Deserialize<IntKeySerializerTarget>(jr);
            }
        }

        [Benchmark]
        public IntKeySerializerTarget JilString()
        {
            return Jil.JSON.Deserialize<IntKeySerializerTarget>(jilObj);
        }

        [Benchmark]
        public IntKeySerializerTarget JilStreamReader()
        {
            using (var ms = new MemoryStream(jilByteArray))
            using (var sr = new StreamReader(ms, Encoding.UTF8))
            {
                return Jil.JSON.Deserialize<IntKeySerializerTarget>(sr);
            }
        }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

