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
using System;
using System.Collections.Generic;
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
}
