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
    public class TypelessSerializeBenchmark
    {
        private ContractType TestContractType = new ContractType("John", new ContractType("Jack", null));
        private ContractlessType TestContractlessType = new ContractlessType("John", new ContractlessType("Jack", null));
        private TypelessPrimitiveType TestTypelessPrimitiveType = new TypelessPrimitiveType("John", 555);
        private TypelessPrimitiveType TestTypelessComplexType = new TypelessPrimitiveType("John", new TypelessPrimitiveType("John", null));

        [Benchmark]
        public byte[] Old_MessagePackSerializer_Serialize_StandardResolver()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(this.TestContractType, oldmsgpack::MessagePack.Resolvers.StandardResolver.Instance);
        }

        [Benchmark]
        public byte[] Old_MessagePackSerializer_Serialize_ContractlessStandardResolver()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(this.TestContractlessType, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public byte[] Old_MessagePackSerializer_Serialize_TypelessContractlessStandardResolver_primitive()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(this.TestTypelessPrimitiveType, oldmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public byte[] Old_MessagePackSerializer_Serialize_TypelessContractlessStandardResolver_complex()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Serialize(this.TestTypelessComplexType, oldmsgpack::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }

        [Benchmark]
        public byte[] MessagePackSerializer_Serialize_StandardResolver()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize(this.TestContractType, newmsgpack::MessagePack.Resolvers.StandardResolver.Options);
        }

        [Benchmark]
        public byte[] MessagePackSerializer_Serialize_ContractlessStandardResolver()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize(this.TestContractlessType, newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }

        [Benchmark]
        public byte[] MessagePackSerializer_Serialize_TypelessContractlessStandardResolver_primitive()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize(this.TestTypelessPrimitiveType, newmsgpack.MessagePack.Resolvers.TypelessContractlessStandardResolver.Options);
        }

        [Benchmark]
        public byte[] MessagePackSerializer_Serialize_TypelessContractlessStandardResolver_complex()
        {
            return newmsgpack.MessagePack.MessagePackSerializer.Serialize(this.TestTypelessComplexType, newmsgpack.MessagePack.Resolvers.TypelessContractlessStandardResolver.Options);
        }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

