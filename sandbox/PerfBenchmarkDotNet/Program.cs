extern alias newmsgpack;
extern alias oldmsgpack;

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
                typeof(DictionaryLookupCompare),
                typeof(StringKeyDeserializeCompare)
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
                automata.TryGetValue(keys[i], 0, keys[i].Length, out _);
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
    public class StringKeyDeserializeCompare
    {
        byte[] bin;
        byte[] binIntKey;
        newmsgpack::MessagePack.IFormatterResolver automata;
        newmsgpack::MessagePack.IFormatterResolver hashtable;

        public StringKeyDeserializeCompare()
        {
            bin = newmsgpack::MessagePack.MessagePackSerializer.Serialize(new StringKeySerializerTarget());
            binIntKey = newmsgpack::MessagePack.MessagePackSerializer.Serialize(new IntKeySerializerTarget());
            automata = new Resolver(new GeneratedFormatter.MessagePack.Formatters.StringKeySerializerTargetFormatter_AutomataLookup());
            hashtable = new Resolver(new GeneratedFormatter.MessagePack.Formatters.StringKeySerializerTargetFormatter_ByteArrayStringHashTable());
        }

        [Benchmark(Baseline = true)]
        public IntKeySerializerTarget IntKey()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<IntKeySerializerTarget>(binIntKey);
        }


        [Benchmark]
        public StringKeySerializerTarget Automata()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(bin, automata);
        }

        [Benchmark]
        public StringKeySerializerTarget Hashtable()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(bin, hashtable);
        }

        [Benchmark]
        public StringKeySerializerTarget AutomataInlineEmit()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<StringKeySerializerTarget>(bin, newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
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

            public int Serialize(ref byte[] bytes, int num, StringKeySerializerTarget stringKeySerializerTarget, IFormatterResolver formatterResolver)
            {
                if (stringKeySerializerTarget == null)
                {
                    return MessagePackBinary.WriteNil(ref bytes, num);
                }
                int num2 = num;
                num += MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, num, 9);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[0]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty1);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[1]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty2);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[2]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty3);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[3]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty4);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[4]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty5);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[5]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty6);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[6]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty7);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[7]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty8);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[8]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty9);
                return num - num2;
            }

            public StringKeySerializerTarget Deserialize(byte[] bytes, int num, IFormatterResolver formatterResolver, out int ptr)
            {
                if (MessagePackBinary.IsNil(bytes, num))
                {
                    ptr = 1;
                    return null;
                }
                int num2 = num;
                int num3 = MessagePackBinary.ReadMapHeader(bytes, num, out ptr);
                num += ptr;
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
                    bool arg_47_0 = this.keyMapping.TryGetValue(MessagePackBinary.ReadStringSegment(bytes, num, out ptr), out num4);
                    num += ptr;
                    if (!arg_47_0)
                    {
                        ptr = MessagePackBinary.ReadNextBlock(bytes, num);
                    }
                    else
                    {
                        switch (num4)
                        {
                            case 0:
                                myProperty = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 1:
                                myProperty2 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 2:
                                myProperty3 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 3:
                                myProperty4 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 4:
                                myProperty5 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 5:
                                myProperty6 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 6:
                                myProperty7 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 7:
                                myProperty8 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 8:
                                myProperty9 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            default:
                                ptr = MessagePackBinary.ReadNextBlock(bytes, num);
                                break;
                        }
                    }
                    num += ptr;
                }
                ptr = num - num2;
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

            public int Serialize(ref byte[] bytes, int num, StringKeySerializerTarget stringKeySerializerTarget, IFormatterResolver formatterResolver)
            {
                if (stringKeySerializerTarget == null)
                {
                    return MessagePackBinary.WriteNil(ref bytes, num);
                }
                int num2 = num;
                num += MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, num, 9);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[0]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty1);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[1]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty2);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[2]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty3);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[3]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty4);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[4]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty5);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[5]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty6);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[6]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty7);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[7]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty8);
                num += MessagePackBinary.WriteStringBytes(ref bytes, num, this.stringByteKeys[8]);
                num += MessagePackBinary.WriteInt32(ref bytes, num, stringKeySerializerTarget.MyProperty9);
                return num - num2;
            }

            public StringKeySerializerTarget Deserialize(byte[] bytes, int num, IFormatterResolver formatterResolver, out int ptr)
            {
                if (MessagePackBinary.IsNil(bytes, num))
                {
                    ptr = 1;
                    return null;
                }
                int num2 = num;
                int num3 = MessagePackBinary.ReadMapHeader(bytes, num, out ptr);
                num += ptr;
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
                    var segment = MessagePackBinary.ReadStringSegment(bytes, num, out ptr);
                    bool arg_47_0 = this.keyMapping.TryGetValue(segment.Array, segment.Offset, segment.Count, out num4);
                    num += ptr;
                    if (!arg_47_0)
                    {
                        ptr = MessagePackBinary.ReadNextBlock(bytes, num);
                    }
                    else
                    {
                        switch (num4)
                        {
                            case 0:
                                myProperty = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 1:
                                myProperty2 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 2:
                                myProperty3 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 3:
                                myProperty4 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 4:
                                myProperty5 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 5:
                                myProperty6 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 6:
                                myProperty7 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 7:
                                myProperty8 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            case 8:
                                myProperty9 = MessagePackBinary.ReadInt32(bytes, num, out ptr);
                                break;
                            default:
                                ptr = MessagePackBinary.ReadNextBlock(bytes, num);
                                break;
                        }
                    }
                    num += ptr;
                }
                ptr = num - num2;
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
    }

}