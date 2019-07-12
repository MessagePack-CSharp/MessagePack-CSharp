extern alias newmsgpack;
extern alias oldmsgpack;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using GeneratedFormatter.MessagePack.Formatters;
using MsgPack.Serialization;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroFormatter;

namespace PerfBenchmarkDotNet
{
    internal class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);

            Add(Job.ShortRun.With(BenchmarkDotNet.Environments.Platform.X64).WithWarmupCount(1).WithIterationCount(1));

            //Add(Job.ShortRun.With(BenchmarkDotNet.Environments.Platform.X64).WithWarmupCount(1).WithIterationCount(1),
            //Job.ShortRun.With(BenchmarkDotNet.Environments.Platform.X86).WithWarmupCount(1).WithIterationCount(1));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[]
            {
                //typeof(TypelessSerializeBenchmark),
                //typeof(TypelessDeserializeBenchmark),
                typeof(DeserializeBenchmark),
                typeof(SerializeBenchmark),
                typeof(DictionaryLookupCompare),
                typeof(StringKeyDeserializeCompare),
                typeof(NewVsOld),
                typeof(GuidImprov),
                typeof(ImproveStringKeySerializeBenchmark),
                typeof(MessagePackReaderBenchmark),
                typeof(MessagePackWriterBenchmark),
                typeof(SpanBenchmarks),
            });

            // args = new[] { "0" };
#if !DEBUG
            switcher.Run(args);
#else
            switcher.Run(args, new DebugInProcessConfig());
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

    [oldmsgpack::MessagePack.MessagePackObject(true)]
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
    [ZeroFormattable]
    public class IntKeySerializerTarget
    {
        [newmsgpack::MessagePack.Key(0)]
        [Index(0)]
        [ProtoMember(1)]
        public virtual int MyProperty1 { get; set; }
        [newmsgpack::MessagePack.Key(1)]
        [Index(1)]
        [ProtoMember(2)]
        public virtual int MyProperty2 { get; set; }
        [newmsgpack::MessagePack.Key(2)]
        [Index(2)]
        [ProtoMember(3)]
        public virtual int MyProperty3 { get; set; }
        [newmsgpack::MessagePack.Key(3)]
        [Index(3)]
        [ProtoMember(4)]
        public virtual int MyProperty4 { get; set; }
        [newmsgpack::MessagePack.Key(4)]
        [Index(4)]
        [ProtoMember(5)]
        public virtual int MyProperty5 { get; set; }
        [newmsgpack::MessagePack.Key(5)]
        [Index(5)]
        [ProtoMember(6)]
        public virtual int MyProperty6 { get; set; }
        [newmsgpack::MessagePack.Key(6)]
        [Index(6)]
        [ProtoMember(7)]
        public virtual int MyProperty7 { get; set; }
        [ProtoMember(8)]
        [newmsgpack::MessagePack.Key(7)]
        [Index(7)]
        public virtual int MyProperty8 { get; set; }
        [ProtoMember(9)]
        [newmsgpack::MessagePack.Key(8)]
        [Index(8)]
        public virtual int MyProperty9 { get; set; }
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
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize(TestContractType, newmsgpack::MessagePack.Resolvers.StandardResolver.Options);
        }

        [Benchmark]
        public byte[] MessagePackSerializer_Serialize_ContractlessStandardResolver()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize(TestContractlessType, newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }

        [Benchmark]
        public byte[] MessagePackSerializer_Serialize_TypelessContractlessStandardResolver_primitive()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize(TestTypelessPrimitiveType, newmsgpack.MessagePack.Resolvers.TypelessContractlessStandardResolver.Options);
        }

        [Benchmark]
        public byte[] MessagePackSerializer_Serialize_TypelessContractlessStandardResolver_complex()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize(TestTypelessComplexType, newmsgpack.MessagePack.Resolvers.TypelessContractlessStandardResolver.Options);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class TypelessDeserializeBenchmark
    {
        private byte[] OldStandardResolverBytes = oldmsgpack::MessagePack.MessagePackSerializer.Serialize(new ContractType("John", new ContractType("Jack", null)), oldmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
        private byte[] OldContractlessStandardResolverBytes = oldmsgpack::MessagePack.MessagePackSerializer.Serialize(new ContractlessType("John", new ContractlessType("Jack", null)), oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        private byte[] OldTypelessContractlessStandardResolverBytes = oldmsgpack::MessagePack.MessagePackSerializer.Serialize(new TypelessPrimitiveType("John", 555), oldmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        private byte[] OldTypelessContractlessStandardResolverComplexBytes = oldmsgpack::MessagePack.MessagePackSerializer.Serialize(new TypelessPrimitiveType("John", new TypelessPrimitiveType("John", null)), oldmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);

        private byte[] NewStandardResolverBytes = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new ContractType("John", new ContractType("Jack", null)), newmsgpack::MessagePack.Resolvers.StandardResolver.Options);
        private byte[] NewContractlessStandardResolverBytes = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new ContractlessType("John", new ContractlessType("Jack", null)), newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Options);
        private byte[] NewTypelessContractlessStandardResolverBytes = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new TypelessPrimitiveType("John", 555), newmsgpack.MessagePack.Resolvers.TypelessContractlessStandardResolver.Options);
        private byte[] NewTypelessContractlessStandardResolverComplexBytes = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new TypelessPrimitiveType("John", new TypelessPrimitiveType("John", null)), newmsgpack.MessagePack.Resolvers.TypelessContractlessStandardResolver.Options);

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
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<ContractType>(NewStandardResolverBytes, newmsgpack::MessagePack.Resolvers.StandardResolver.Options);
        }

        [Benchmark]
        public ContractlessType MessagePackSerializer_Deserialize_ContractlessStandardResolver()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<ContractlessType>(NewContractlessStandardResolverBytes, newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }

        [Benchmark]
        public TypelessPrimitiveType MessagePackSerializer_Deserialize_TypelessContractlessStandardResolver()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<TypelessPrimitiveType>(NewTypelessContractlessStandardResolverBytes, newmsgpack.MessagePack.Resolvers.TypelessContractlessStandardResolver.Options);
        }

        [Benchmark]
        public TypelessPrimitiveType MessagePackSerializer_Deserialize_TypelessContractlessStandardResolverComplexBytes()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<TypelessPrimitiveType>(NewTypelessContractlessStandardResolverComplexBytes, newmsgpack.MessagePack.Resolvers.TypelessContractlessStandardResolver.Options);
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
        static Hyperion.Serializer hyperionSerializer = new Hyperion.Serializer();
        static byte[] hyperionObj;

        static newmsgpack::MessagePack.MessagePackSerializerOptions mpcGenFormatterResolver = newmsgpack::MessagePack.MessagePackSerializerOptions.Default.WithResolver(new Resolver(new StringKeySerializerTargetFormatter_MpcGeneratedAutomata()));
        static newmsgpack::MessagePack.MessagePackSerializerOptions mpcGenDictFormatterResolver = newmsgpack::MessagePack.MessagePackSerializerOptions.Default.WithResolver(new Resolver(new StringKeySerializerTargetFormatter_MpcGeneratedDictionary()));

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

    [Config(typeof(BenchmarkConfig))]
    public class SerializeBenchmark
    {
        static MsgPack.Serialization.SerializationContext mapContext = new MsgPack.Serialization.SerializationContext { SerializationMethod = SerializationMethod.Map };
        static MsgPack.Serialization.SerializationContext arrayContext = new MsgPack.Serialization.SerializationContext { SerializationMethod = SerializationMethod.Array };
        static JsonSerializer jsonSerialzier = new JsonSerializer();
        static Hyperion.Serializer hyperionSerializer = new Hyperion.Serializer();
        static newmsgpack::MessagePack.IFormatterResolver mpcGenFormatterResolver = new Resolver(new StringKeySerializerTargetFormatter_MpcGeneratedAutomata());
        static newmsgpack::MessagePack.IFormatterResolver mpcGenDictFormatterResolver = new Resolver(new StringKeySerializerTargetFormatter_MpcGeneratedDictionary());
        static IntKeySerializerTarget intData = new IntKeySerializerTarget();
        static StringKeySerializerTarget stringData = new StringKeySerializerTarget();

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

    [Config(typeof(BenchmarkConfig))]
    public class DictionaryLookupCompare
    {
        newmsgpack::MessagePack.Internal.ByteArrayStringHashTable hashTable;
        newmsgpack::MessagePack.Internal.AutomataDictionary automata;
        byte[][] keys;

        public DictionaryLookupCompare()
        {
            hashTable = new newmsgpack::MessagePack.Internal.ByteArrayStringHashTable(9);
            automata = new newmsgpack::MessagePack.Internal.AutomataDictionary();
            keys = new byte[9][];
            foreach (var item in Enumerable.Range(0, 9).Select(x => new { str = "MyProperty" + (x + 1), i = x }))
            {
                hashTable.Add(Encoding.UTF8.GetBytes(item.str), item.i);
                automata.Add(item.str, item.i);
                keys[item.i] = Encoding.UTF8.GetBytes(item.str);
            }
        }

        [Benchmark]
        public void Automata()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                automata.TryGetValue(keys[i], out _);
            }
        }

        [Benchmark]
        public void Hashtable()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                hashTable.TryGetValue(new ArraySegment<byte>(keys[i], 0, keys[i].Length), out _);
            }
        }
    }


    [Config(typeof(BenchmarkConfig))]
    public class ImproveStringKeySerializeBenchmark
    {
        static StringKeySerializerTarget stringData = new StringKeySerializerTarget();

        [Benchmark]
        public byte[] OldSerialize()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize<StringKeySerializerTarget>(stringData);
        }

        [Benchmark(Baseline = true)]
        public byte[] NewSerialize()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize<StringKeySerializerTarget>(stringData);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class StringKeyDeserializeCompare
    {
        byte[] bin;
        byte[] binIntKey;
        newmsgpack::MessagePack.MessagePackSerializerOptions automata;
        newmsgpack::MessagePack.MessagePackSerializerOptions hashtable;

        public StringKeyDeserializeCompare()
        {
            bin = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new StringKeySerializerTarget());
            binIntKey = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new IntKeySerializerTarget());
            automata = newmsgpack::MessagePack.MessagePackSerializerOptions.Default.WithResolver(new Resolver(new GeneratedFormatter.MessagePack.Formatters.StringKeySerializerTargetFormatter_AutomataLookup()));
            hashtable = newmsgpack::MessagePack.MessagePackSerializerOptions.Default.WithResolver(new Resolver(new GeneratedFormatter.MessagePack.Formatters.StringKeySerializerTargetFormatter_ByteArrayStringHashTable()));
        }

        [Benchmark(Baseline = true)]
        public IntKeySerializerTarget IntKey()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<IntKeySerializerTarget>(binIntKey);
        }

        [Benchmark]
        public StringKeySerializerTarget OldStringKey()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(bin, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public StringKeySerializerTarget Automata()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(bin, automata);
        }

        [Benchmark]
        public StringKeySerializerTarget Hashtable()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(bin, hashtable);
        }

        [Benchmark]
        public StringKeySerializerTarget AutomataInlineEmit()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(bin, newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }
    }



    [Config(typeof(BenchmarkConfig))]
    public class NewVsOld
    {
        byte[] bin;
        public NewVsOld()
        {
            bin = newmsgpack.MessagePack.MessagePackSerializer.Serialize(new StringKeySerializerTarget());
        }

        [Benchmark(Baseline = true)]
        public StringKeySerializerTarget New()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(bin, newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }

        [Benchmark]
        public StringKeySerializerTarget Old()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(bin, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }
    }


    [Config(typeof(BenchmarkConfig))]
    public class GuidImprov
    {
        Guid guid;
        byte[] bin;
        public GuidImprov()
        {
            guid = Guid.NewGuid();
            bin = newmsgpack.MessagePack.MessagePackSerializer.Serialize(guid);
        }

        [Benchmark]
        public byte[] NewSerialize()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize(guid);
        }

        [Benchmark]
        public byte[] OldSerialize()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(guid);
        }

        [Benchmark]
        public Guid NewDeserialize()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<Guid>(bin);
        }

        [Benchmark]
        public Guid OldDeserialize()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<Guid>(bin);
        }
    }

    public class Resolver : newmsgpack::MessagePack.IFormatterResolver
    {
        object formatter;

        public Resolver(object formatter)
        {
            this.formatter = formatter;
        }

        public newmsgpack::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()
        {
            return (newmsgpack::MessagePack.Formatters.IMessagePackFormatter<T>)formatter;
        }
    }
}


namespace GeneratedFormatter
{
    using System;
    using System.Text;
    using newmsgpack::MessagePack.Internal;
    using newmsgpack::MessagePack.Formatters;
    using PerfBenchmarkDotNet;
    using newmsgpack::MessagePack;
    using System.Buffers;

    namespace MessagePack.Formatters
    {
        public sealed class StringKeySerializerTargetFormatter_ByteArrayStringHashTable : IMessagePackFormatter<StringKeySerializerTarget>
        {
            private readonly ByteArrayStringHashTable keyMapping;

            private readonly byte[][] stringByteKeys;

            public StringKeySerializerTargetFormatter_ByteArrayStringHashTable()
            {
                this.keyMapping = new ByteArrayStringHashTable(9)
                {
                    {
                        "MyProperty1",
                        0
                    },
                    {
                        "MyProperty2",
                        1
                    },
                    {
                        "MyProperty3",
                        2
                    },
                    {
                        "MyProperty4",
                        3
                    },
                    {
                        "MyProperty5",
                        4
                    },
                    {
                        "MyProperty6",
                        5
                    },
                    {
                        "MyProperty7",
                        6
                    },
                    {
                        "MyProperty8",
                        7
                    },
                    {
                        "MyProperty9",
                        8
                    }
                };
                this.stringByteKeys = new byte[][]
                {
                Encoding.UTF8.GetBytes("MyProperty1"),
                Encoding.UTF8.GetBytes("MyProperty2"),
                Encoding.UTF8.GetBytes("MyProperty3"),
                Encoding.UTF8.GetBytes("MyProperty4"),
                Encoding.UTF8.GetBytes("MyProperty5"),
                Encoding.UTF8.GetBytes("MyProperty6"),
                Encoding.UTF8.GetBytes("MyProperty7"),
                Encoding.UTF8.GetBytes("MyProperty8"),
                Encoding.UTF8.GetBytes("MyProperty9")
                };
            }

            public void Serialize(ref MessagePackWriter writer, StringKeySerializerTarget stringKeySerializerTarget, MessagePackSerializerOptions options)
            {
                if (stringKeySerializerTarget == null)
                {
                    writer.WriteNil();
                    return;
                }

                writer.WriteMapHeader(9);
                writer.WriteString(this.stringByteKeys[0]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty1);
                writer.WriteString(this.stringByteKeys[1]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty2);
                writer.WriteString(this.stringByteKeys[2]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty3);
                writer.WriteString(this.stringByteKeys[3]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty4);
                writer.WriteString(this.stringByteKeys[4]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty5);
                writer.WriteString(this.stringByteKeys[5]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty6);
                writer.WriteString(this.stringByteKeys[6]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty7);
                writer.WriteString(this.stringByteKeys[7]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty8);
                writer.WriteString(this.stringByteKeys[8]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty9);
            }

            public StringKeySerializerTarget Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                if (reader.TryReadNil())
                {
                    return null;
                }

                int num3 = reader.ReadMapHeader();
                int myProperty = 0;
                int myProperty2 = 0;
                int myProperty3 = 0;
                int myProperty4 = 0;
                int myProperty5 = 0;
                int myProperty6 = 0;
                int myProperty7 = 0;
                int myProperty8 = 0;
                int myProperty9 = 0;
                for (int i = 0; i < num3; i++)
                {
                    int num4;
                    bool arg_47_0 = this.keyMapping.TryGetValue(reader.ReadStringSegment(), out num4);
                    if (!arg_47_0)
                    {
                        reader.Skip();
                    }
                    else
                    {
                        switch (num4)
                        {
                            case 0:
                                myProperty = reader.ReadInt32();
                                break;
                            case 1:
                                myProperty2 = reader.ReadInt32();
                                break;
                            case 2:
                                myProperty3 = reader.ReadInt32();
                                break;
                            case 3:
                                myProperty4 = reader.ReadInt32();
                                break;
                            case 4:
                                myProperty5 = reader.ReadInt32();
                                break;
                            case 5:
                                myProperty6 = reader.ReadInt32();
                                break;
                            case 6:
                                myProperty7 = reader.ReadInt32();
                                break;
                            case 7:
                                myProperty8 = reader.ReadInt32();
                                break;
                            case 8:
                                myProperty9 = reader.ReadInt32();
                                break;
                            default:
                                reader.Skip();
                                break;
                        }
                    }
                }

                return new StringKeySerializerTarget
                {
                    MyProperty1 = myProperty,
                    MyProperty2 = myProperty2,
                    MyProperty3 = myProperty3,
                    MyProperty4 = myProperty4,
                    MyProperty5 = myProperty5,
                    MyProperty6 = myProperty6,
                    MyProperty7 = myProperty7,
                    MyProperty8 = myProperty8,
                    MyProperty9 = myProperty9
                };
            }
        }


        public sealed class StringKeySerializerTargetFormatter_AutomataLookup : IMessagePackFormatter<StringKeySerializerTarget>
        {
            private readonly AutomataDictionary keyMapping;

            private readonly byte[][] stringByteKeys;

            public StringKeySerializerTargetFormatter_AutomataLookup()
            {
                this.keyMapping = new AutomataDictionary()
                {
                    {
                        "MyProperty1",
                        0
                    },
                    {
                        "MyProperty2",
                        1
                    },
                    {
                        "MyProperty3",
                        2
                    },
                    {
                        "MyProperty4",
                        3
                    },
                    {
                        "MyProperty5",
                        4
                    },
                    {
                        "MyProperty6",
                        5
                    },
                    {
                        "MyProperty7",
                        6
                    },
                    {
                        "MyProperty8",
                        7
                    },
                    {
                        "MyProperty9",
                        8
                    }
                };
                this.stringByteKeys = new byte[][]
                {
                Encoding.UTF8.GetBytes("MyProperty1"),
                Encoding.UTF8.GetBytes("MyProperty2"),
                Encoding.UTF8.GetBytes("MyProperty3"),
                Encoding.UTF8.GetBytes("MyProperty4"),
                Encoding.UTF8.GetBytes("MyProperty5"),
                Encoding.UTF8.GetBytes("MyProperty6"),
                Encoding.UTF8.GetBytes("MyProperty7"),
                Encoding.UTF8.GetBytes("MyProperty8"),
                Encoding.UTF8.GetBytes("MyProperty9")
                };
            }

            public void Serialize(ref MessagePackWriter writer, StringKeySerializerTarget stringKeySerializerTarget, MessagePackSerializerOptions options)
            {
                if (stringKeySerializerTarget == null)
                {
                    writer.WriteNil();
                    return;
                }

                writer.WriteMapHeader(9);
                writer.WriteString(this.stringByteKeys[0]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty1);
                writer.WriteString(this.stringByteKeys[1]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty2);
                writer.WriteString(this.stringByteKeys[2]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty3);
                writer.WriteString(this.stringByteKeys[3]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty4);
                writer.WriteString(this.stringByteKeys[4]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty5);
                writer.WriteString(this.stringByteKeys[5]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty6);
                writer.WriteString(this.stringByteKeys[6]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty7);
                writer.WriteString(this.stringByteKeys[7]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty8);
                writer.WriteString(this.stringByteKeys[8]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty9);
            }

            public StringKeySerializerTarget Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                if (reader.TryReadNil())
                {
                    return null;
                }
                int num3 = reader.ReadMapHeader();
                int myProperty = 0;
                int myProperty2 = 0;
                int myProperty3 = 0;
                int myProperty4 = 0;
                int myProperty5 = 0;
                int myProperty6 = 0;
                int myProperty7 = 0;
                int myProperty8 = 0;
                int myProperty9 = 0;
                for (int i = 0; i < num3; i++)
                {
                    int num4;
                    var segment = reader.ReadStringSegment();
                    bool arg_47_0 = this.keyMapping.TryGetValue(segment, out num4);
                    if (!arg_47_0)
                    {
                        reader.Skip();
                    }
                    else
                    {
                        switch (num4)
                        {
                            case 0:
                                myProperty = reader.ReadInt32();
                                break;
                            case 1:
                                myProperty2 = reader.ReadInt32();
                                break;
                            case 2:
                                myProperty3 = reader.ReadInt32();
                                break;
                            case 3:
                                myProperty4 = reader.ReadInt32();
                                break;
                            case 4:
                                myProperty5 = reader.ReadInt32();
                                break;
                            case 5:
                                myProperty6 = reader.ReadInt32();
                                break;
                            case 6:
                                myProperty7 = reader.ReadInt32();
                                break;
                            case 7:
                                myProperty8 = reader.ReadInt32();
                                break;
                            case 8:
                                myProperty9 = reader.ReadInt32();
                                break;
                            default:
                                reader.Skip();
                                break;
                        }
                    }
                }

                return new StringKeySerializerTarget
                {
                    MyProperty1 = myProperty,
                    MyProperty2 = myProperty2,
                    MyProperty3 = myProperty3,
                    MyProperty4 = myProperty4,
                    MyProperty5 = myProperty5,
                    MyProperty6 = myProperty6,
                    MyProperty7 = myProperty7,
                    MyProperty8 = myProperty8,
                    MyProperty9 = myProperty9
                };
            }
        }


        public sealed class StringKeySerializerTargetFormatter_MpcGeneratedAutomata : newmsgpack::MessagePack.Formatters.IMessagePackFormatter<StringKeySerializerTarget>
        {

            readonly newmsgpack::MessagePack.Internal.AutomataDictionary ____keyMapping;
            readonly byte[][] ____stringByteKeys;

            public StringKeySerializerTargetFormatter_MpcGeneratedAutomata()
            {
                this.____keyMapping = new newmsgpack::MessagePack.Internal.AutomataDictionary()
            {
                { "MyProperty1", 0},
                { "MyProperty2", 1},
                { "MyProperty3", 2},
                { "MyProperty4", 3},
                { "MyProperty5", 4},
                { "MyProperty6", 5},
                { "MyProperty7", 6},
                { "MyProperty8", 7},
                { "MyProperty9", 8},
            };

                this.____stringByteKeys = new byte[][]
                {
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty1"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty2"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty3"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty4"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty5"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty6"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty7"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty8"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty9"),

                };
            }


            public void Serialize(ref MessagePackWriter writer, global::PerfBenchmarkDotNet.StringKeySerializerTarget value, newmsgpack::MessagePack.MessagePackSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public global::PerfBenchmarkDotNet.StringKeySerializerTarget Deserialize(ref MessagePackReader reader, newmsgpack::MessagePack.MessagePackSerializerOptions options)
            {
                if (reader.TryReadNil())
                {
                    return null;
                }

                var length = reader.ReadMapHeader();

                var __MyProperty1__ = default(int);
                var __MyProperty2__ = default(int);
                var __MyProperty3__ = default(int);
                var __MyProperty4__ = default(int);
                var __MyProperty5__ = default(int);
                var __MyProperty6__ = default(int);
                var __MyProperty7__ = default(int);
                var __MyProperty8__ = default(int);
                var __MyProperty9__ = default(int);

                for (int i = 0; i < length; i++)
                {
                    var stringKey = reader.ReadStringSegment();
                    int key;
                    if (!____keyMapping.TryGetValue(stringKey, out key))
                    {
                        reader.Skip();
                        continue;
                    }

                    switch (key)
                    {
                        case 0:
                            __MyProperty1__ = reader.ReadInt32();
                            break;
                        case 1:
                            __MyProperty2__ = reader.ReadInt32();
                            break;
                        case 2:
                            __MyProperty3__ = reader.ReadInt32();
                            break;
                        case 3:
                            __MyProperty4__ = reader.ReadInt32();
                            break;
                        case 4:
                            __MyProperty5__ = reader.ReadInt32();
                            break;
                        case 5:
                            __MyProperty6__ = reader.ReadInt32();
                            break;
                        case 6:
                            __MyProperty7__ = reader.ReadInt32();
                            break;
                        case 7:
                            __MyProperty8__ = reader.ReadInt32();
                            break;
                        case 8:
                            __MyProperty9__ = reader.ReadInt32();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                var ____result = new global::PerfBenchmarkDotNet.StringKeySerializerTarget();
                ____result.MyProperty1 = __MyProperty1__;
                ____result.MyProperty2 = __MyProperty2__;
                ____result.MyProperty3 = __MyProperty3__;
                ____result.MyProperty4 = __MyProperty4__;
                ____result.MyProperty5 = __MyProperty5__;
                ____result.MyProperty6 = __MyProperty6__;
                ____result.MyProperty7 = __MyProperty7__;
                ____result.MyProperty8 = __MyProperty8__;
                ____result.MyProperty9 = __MyProperty9__;
                return ____result;
            }
        }

        public sealed class StringKeySerializerTargetFormatter_MpcGeneratedDictionary : newmsgpack::MessagePack.Formatters.IMessagePackFormatter<StringKeySerializerTarget>
        {

            readonly newmsgpack::MessagePack.Internal.ByteArrayStringHashTable ____keyMapping;
            readonly byte[][] ____stringByteKeys;

            public StringKeySerializerTargetFormatter_MpcGeneratedDictionary()
            {
                this.____keyMapping = new newmsgpack::MessagePack.Internal.ByteArrayStringHashTable(9)
            {
                { "MyProperty1", 0},
                { "MyProperty2", 1},
                { "MyProperty3", 2},
                { "MyProperty4", 3},
                { "MyProperty5", 4},
                { "MyProperty6", 5},
                { "MyProperty7", 6},
                { "MyProperty8", 7},
                { "MyProperty9", 8},
            };

                this.____stringByteKeys = new byte[][]
                {
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty1"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty2"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty3"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty4"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty5"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty6"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty7"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty8"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty9"),

                };
            }


            public void Serialize(ref MessagePackWriter writer, global::PerfBenchmarkDotNet.StringKeySerializerTarget value, newmsgpack::MessagePack.MessagePackSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public global::PerfBenchmarkDotNet.StringKeySerializerTarget Deserialize(ref MessagePackReader reader, newmsgpack::MessagePack.MessagePackSerializerOptions options)
            {
                if (reader.TryReadNil())
                {
                    return null;
                }

                var length = reader.ReadMapHeader();

                var __MyProperty1__ = default(int);
                var __MyProperty2__ = default(int);
                var __MyProperty3__ = default(int);
                var __MyProperty4__ = default(int);
                var __MyProperty5__ = default(int);
                var __MyProperty6__ = default(int);
                var __MyProperty7__ = default(int);
                var __MyProperty8__ = default(int);
                var __MyProperty9__ = default(int);

                for (int i = 0; i < length; i++)
                {
                    var stringKey = reader.ReadStringSegment();
                    int key;
                    if (!____keyMapping.TryGetValue(stringKey, out key))
                    {
                        reader.Skip();
                        continue;
                    }

                    switch (key)
                    {
                        case 0:
                            __MyProperty1__ = reader.ReadInt32();
                            break;
                        case 1:
                            __MyProperty2__ = reader.ReadInt32();
                            break;
                        case 2:
                            __MyProperty3__ = reader.ReadInt32();
                            break;
                        case 3:
                            __MyProperty4__ = reader.ReadInt32();
                            break;
                        case 4:
                            __MyProperty5__ = reader.ReadInt32();
                            break;
                        case 5:
                            __MyProperty6__ = reader.ReadInt32();
                            break;
                        case 6:
                            __MyProperty7__ = reader.ReadInt32();
                            break;
                        case 7:
                            __MyProperty8__ = reader.ReadInt32();
                            break;
                        case 8:
                            __MyProperty9__ = reader.ReadInt32();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                var ____result = new global::PerfBenchmarkDotNet.StringKeySerializerTarget();
                ____result.MyProperty1 = __MyProperty1__;
                ____result.MyProperty2 = __MyProperty2__;
                ____result.MyProperty3 = __MyProperty3__;
                ____result.MyProperty4 = __MyProperty4__;
                ____result.MyProperty5 = __MyProperty5__;
                ____result.MyProperty6 = __MyProperty6__;
                ____result.MyProperty7 = __MyProperty7__;
                ____result.MyProperty8 = __MyProperty8__;
                ____result.MyProperty9 = __MyProperty9__;
                return ____result;
            }
        }
    }
}
