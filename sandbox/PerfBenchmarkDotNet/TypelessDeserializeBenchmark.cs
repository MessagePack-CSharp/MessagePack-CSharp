// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

using BenchmarkDotNet.Attributes;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
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
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<ContractType>(this.OldStandardResolverBytes, oldmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
        }

        [Benchmark]
        public ContractlessType Old_MessagePackSerializer_Deserialize_ContractlessStandardResolver()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<ContractlessType>(this.OldContractlessStandardResolverBytes, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public TypelessPrimitiveType Old_MessagePackSerializer_Deserialize_TypelessContractlessStandardResolver()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<TypelessPrimitiveType>(this.OldTypelessContractlessStandardResolverBytes, oldmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public TypelessPrimitiveType Old_MessagePackSerializer_Deserialize_TypelessContractlessStandardResolverComplexBytes()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<TypelessPrimitiveType>(this.OldTypelessContractlessStandardResolverComplexBytes, oldmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public ContractType MessagePackSerializer_Deserialize_StandardResolver()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<ContractType>(this.NewStandardResolverBytes, newmsgpack::MessagePack.Resolvers.StandardResolver.Options);
        }

        [Benchmark]
        public ContractlessType MessagePackSerializer_Deserialize_ContractlessStandardResolver()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<ContractlessType>(this.NewContractlessStandardResolverBytes, newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }

        [Benchmark]
        public TypelessPrimitiveType MessagePackSerializer_Deserialize_TypelessContractlessStandardResolver()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<TypelessPrimitiveType>(this.NewTypelessContractlessStandardResolverBytes, newmsgpack.MessagePack.Resolvers.TypelessContractlessStandardResolver.Options);
        }

        [Benchmark]
        public TypelessPrimitiveType MessagePackSerializer_Deserialize_TypelessContractlessStandardResolverComplexBytes()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Deserialize<TypelessPrimitiveType>(this.NewTypelessContractlessStandardResolverComplexBytes, newmsgpack.MessagePack.Resolvers.TypelessContractlessStandardResolver.Options);
        }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

