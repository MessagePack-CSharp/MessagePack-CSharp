extern alias oldmsgpack;
extern alias newmsgpack;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using MsgPack.Serialization;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PerfBenchmarkDotNet
{
    internal class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);

            Add(Job.ShortRun.With(BenchmarkDotNet.Environments.Platform.X64).WithWarmupCount(1).WithTargetCount(1));

            //Add(Job.ShortRun.With(BenchmarkDotNet.Environments.Platform.X64).WithWarmupCount(1).WithTargetCount(1),
            //  Job.ShortRun.With(BenchmarkDotNet.Environments.Platform.X86).WithWarmupCount(1).WithTargetCount(1));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[]
            {
                typeof(TypelessSerializeBenchmark),
                typeof(TypelessDeserializeBenchmark),
                typeof(DeserializeBenchmark),
            });

            // args = new[] { "0" };
#if !DEBUG
            switcher.Run(args);
#else
            // new TypelessBenchmark().MessagePackSerializer_Deserialize_TypelessContractlessStandardResolverComplexBytes();
#endif
        }
    }

    [oldmsgpack::MessagePack.MessagePackObject]
    [newmsgpack::MessagePack.MessagePackObject]
    public class ContractType
    {
        public ContractType(string name, ContractType nested)
        {
            Name = name;
            Nested = nested;
        }

        [oldmsgpack::MessagePack.Key(0)]
        [newmsgpack::MessagePack.Key(0)]
        public string Name { get; }

        [oldmsgpack::MessagePack.Key(1)]
        [newmsgpack::MessagePack.Key(1)]
        public ContractType Nested { get; }
    }

    public class ContractlessType
    {
        public ContractlessType(string name, ContractlessType nested)
        {
            Name = name;
            Nested = nested;
        }

        public string Name { get; }

        public ContractlessType Nested { get; }
    }

    public class TypelessPrimitiveType
    {
        public TypelessPrimitiveType(string name, object nested)
        {
            Name = name;
            Nested = nested;
        }

        public string Name { get; }

        public object Nested { get; }
    }

    [newmsgpack::MessagePack.MessagePackObject(true)]
    public class StringKeySerializerTarget
    {
        public int MyProperty1 { get; set; }
        public int MyProperty2 { get; set; }
        public int MyProperty3 { get; set; }
        public int MyProperty4 { get; set; }
        public int MyProperty5 { get; set; }
        public int MyProperty6 { get; set; }
        public int MyProperty7 { get; set; }
        public int MyProperty8 { get; set; }
        public int MyProperty9 { get; set; }
    }

    [newmsgpack::MessagePack.MessagePackObject]
    [ProtoContract]
    public class IntKeySerializerTarget
    {
        [newmsgpack::MessagePack.Key(0)]
        [ProtoMember(1)]
        public int MyProperty1 { get; set; }
        [newmsgpack::MessagePack.Key(1)]
        [ProtoMember(2)]
        public int MyProperty2 { get; set; }
        [newmsgpack::MessagePack.Key(2)]
        [ProtoMember(3)]
        public int MyProperty3 { get; set; }
        [newmsgpack::MessagePack.Key(3)]
        [ProtoMember(4)]
        public int MyProperty4 { get; set; }
        [newmsgpack::MessagePack.Key(4)]
        [ProtoMember(5)]
        public int MyProperty5 { get; set; }
        [newmsgpack::MessagePack.Key(5)]
        [ProtoMember(6)]
        public int MyProperty6 { get; set; }
        [newmsgpack::MessagePack.Key(6)]
        [ProtoMember(7)]
        public int MyProperty7 { get; set; }
        [ProtoMember(8)]
        [newmsgpack::MessagePack.Key(7)]
        public int MyProperty8 { get; set; }
        [ProtoMember(9)]
        [newmsgpack::MessagePack.Key(8)]
        public int MyProperty9 { get; set; }
    }


    [Config(typeof(BenchmarkConfig))]
    public class TypelessSerializeBenchmark
    {
        private ContractType TestContractType = new ContractType("John", new ContractType("Jack", null));
        private ContractlessType TestContractlessType = new ContractlessType("John", new ContractlessType("Jack", null));
        private TypelessPrimitiveType TestTypelessPrimitiveType = new TypelessPrimitiveType("John", 555);
        private TypelessPrimitiveType TestTypelessComplexType = new TypelessPrimitiveType("John", new TypelessPrimitiveType("John", null));

        [Benchmark]
        public byte[] Old_MessagePackSerializer_Serialize_StandardResolver()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(TestContractType, oldmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
        }

        [Benchmark]
        public byte[] Old_MessagePackSerializer_Serialize_ContractlessStandardResolver()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(TestContractlessType, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public byte[] Old_MessagePackSerializer_Serialize_TypelessContractlessStandardResolver_primitive()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(TestTypelessPrimitiveType, oldmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public byte[] Old_MessagePackSerializer_Serialize_TypelessContractlessStandardResolver_complex()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(TestTypelessComplexType, oldmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public byte[] MessagePackSerializer_Serialize_StandardResolver()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(TestContractType, newmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
        }

        [Benchmark]
        public byte[] MessagePackSerializer_Serialize_ContractlessStandardResolver()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(TestContractlessType, newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public byte[] MessagePackSerializer_Serialize_TypelessContractlessStandardResolver_primitive()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(TestTypelessPrimitiveType, newmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public byte[] MessagePackSerializer_Serialize_TypelessContractlessStandardResolver_complex()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Serialize(TestTypelessComplexType, newmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class TypelessDeserializeBenchmark
    {
        private byte[] OldStandardResolverBytes = oldmsgpack::MessagePack.MessagePackSerializer.Serialize(new ContractType("John", new ContractType("Jack", null)), oldmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
        private byte[] OldContractlessStandardResolverBytes = oldmsgpack::MessagePack.MessagePackSerializer.Serialize(new ContractlessType("John", new ContractlessType("Jack", null)), oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        private byte[] OldTypelessContractlessStandardResolverBytes = oldmsgpack::MessagePack.MessagePackSerializer.Serialize(new TypelessPrimitiveType("John", 555), oldmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        private byte[] OldTypelessContractlessStandardResolverComplexBytes = oldmsgpack::MessagePack.MessagePackSerializer.Serialize(new TypelessPrimitiveType("John", new TypelessPrimitiveType("John", null)), oldmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);

        private byte[] NewStandardResolverBytes = newmsgpack::MessagePack.MessagePackSerializer.Serialize(new ContractType("John", new ContractType("Jack", null)), newmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
        private byte[] NewContractlessStandardResolverBytes = newmsgpack::MessagePack.MessagePackSerializer.Serialize(new ContractlessType("John", new ContractlessType("Jack", null)), newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        private byte[] NewTypelessContractlessStandardResolverBytes = newmsgpack::MessagePack.MessagePackSerializer.Serialize(new TypelessPrimitiveType("John", 555), newmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        private byte[] NewTypelessContractlessStandardResolverComplexBytes = newmsgpack::MessagePack.MessagePackSerializer.Serialize(new TypelessPrimitiveType("John", new TypelessPrimitiveType("John", null)), newmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);

        [Benchmark]
        public ContractType Old_MessagePackSerializer_Deserialize_StandardResolver()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<ContractType>(OldStandardResolverBytes, oldmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
        }

        [Benchmark]
        public ContractlessType Old_MessagePackSerializer_Deserialize_ContractlessStandardResolver()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<ContractlessType>(OldContractlessStandardResolverBytes, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public TypelessPrimitiveType Old_MessagePackSerializer_Deserialize_TypelessContractlessStandardResolver()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<TypelessPrimitiveType>(OldTypelessContractlessStandardResolverBytes, oldmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public TypelessPrimitiveType Old_MessagePackSerializer_Deserialize_TypelessContractlessStandardResolverComplexBytes()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<TypelessPrimitiveType>(OldTypelessContractlessStandardResolverComplexBytes, oldmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public ContractType MessagePackSerializer_Deserialize_StandardResolver()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<ContractType>(NewStandardResolverBytes, newmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
        }

        [Benchmark]
        public ContractlessType MessagePackSerializer_Deserialize_ContractlessStandardResolver()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<ContractlessType>(NewContractlessStandardResolverBytes, newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public TypelessPrimitiveType MessagePackSerializer_Deserialize_TypelessContractlessStandardResolver()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<TypelessPrimitiveType>(NewTypelessContractlessStandardResolverBytes, newmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public TypelessPrimitiveType MessagePackSerializer_Deserialize_TypelessContractlessStandardResolverComplexBytes()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<TypelessPrimitiveType>(NewTypelessContractlessStandardResolverComplexBytes, newmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }
    }


    [Config(typeof(BenchmarkConfig))]
    public class DeserializeBenchmark
    {
        static MsgPack.Serialization.SerializationContext mapContext = new MsgPack.Serialization.SerializationContext { SerializationMethod = SerializationMethod.Map };
        static MsgPack.Serialization.SerializationContext arrayContext = new MsgPack.Serialization.SerializationContext { SerializationMethod = SerializationMethod.Array };
        static byte[] intObj = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new IntKeySerializerTarget());
        static byte[] stringKeyObj = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new StringKeySerializerTarget());
        static byte[] typelessWithIntKeyObj = newmsgpack.MessagePack.MessagePackSerializer.Typeless.Serialize(new IntKeySerializerTarget());
        static byte[] typelessWithStringKeyObj = newmsgpack.MessagePack.MessagePackSerializer.Typeless.Serialize(new StringKeySerializerTarget());
        static byte[] mapObj = mapContext.GetSerializer<IntKeySerializerTarget>().PackSingleObject(new IntKeySerializerTarget());
        static byte[] arrayObj = arrayContext.GetSerializer<IntKeySerializerTarget>().PackSingleObject(new IntKeySerializerTarget());
        static byte[] protoObj;
        static string jsonnetObj = Newtonsoft.Json.JsonConvert.SerializeObject(new IntKeySerializerTarget());
        static byte[] jsonnetByteArray = Encoding.UTF8.GetBytes(jsonnetObj);
        static string jilObj = Jil.JSON.Serialize(new IntKeySerializerTarget());
        static byte[] jilByteArray = Encoding.UTF8.GetBytes(jilObj);
        static JsonSerializer jsonSerialzier = new JsonSerializer();

        static DeserializeBenchmark()
        {
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, new IntKeySerializerTarget());
                protoObj = ms.ToArray();
            }
        }

        [Benchmark(Baseline = true)]
        public IntKeySerializerTarget IntKey()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<IntKeySerializerTarget>(intObj);
        }

        [Benchmark]
        public StringKeySerializerTarget StringKey()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(stringKeyObj);
        }

        [Benchmark]
        public IntKeySerializerTarget Typeless_IntKey()
        {
            return (IntKeySerializerTarget)newmsgpack::MessagePack.MessagePackSerializer.Typeless.Deserialize(typelessWithIntKeyObj);
        }

        [Benchmark]
        public StringKeySerializerTarget Typeless_StringKey()
        {
            return (StringKeySerializerTarget)newmsgpack::MessagePack.MessagePackSerializer.Typeless.Deserialize(typelessWithStringKeyObj);
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
        public IntKeySerializerTarget JsonNetString()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<IntKeySerializerTarget>(jsonnetObj);
        }

        [Benchmark]
        public IntKeySerializerTarget JsonNetByteArray()
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
        public IntKeySerializerTarget JilByteArray()
        {
            using (var ms = new MemoryStream(jilByteArray))
            using (var sr = new StreamReader(ms, Encoding.UTF8))
            {
                return Jil.JSON.Deserialize<IntKeySerializerTarget>(sr);
            }
        }
    }
}
