// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !(MESSAGEPACK_FORCE_AOT || ENABLE_IL2CPP)
#define DYNAMIC_GENERATION
#endif

#if DYNAMIC_GENERATION

#nullable enable

using System;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
    public class DynamicObjectResolverByRefTests
    {
        [Fact]
        public void DynamicObjectResolver_UsesByRefDispatch_ForStructMembers()
        {
            var formatter = new ThrowingLegacyByRefFormatter();
            var options = MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(
                    new IMessagePackFormatter[] { formatter },
                    new IFormatterResolver[] { DynamicObjectResolver.Instance, StandardResolver.Instance }));

            var value = new Holder { Value = new PayloadStruct { Id = 123 } };

            byte[] payload = MessagePackSerializer.Serialize(value, options);
            Holder result = MessagePackSerializer.Deserialize<Holder>(payload, options);

            Assert.Equal(123, result.Value.Id);
            Assert.Equal(1, formatter.SerializeInCalls);
            Assert.Equal(1, formatter.DeserializeRefCalls);
            Assert.Equal(0, formatter.SerializeByValueCalls);
            Assert.Equal(0, formatter.DeserializeByValueCalls);
        }

        [MessagePackObject]
        public class Holder
        {
            [Key(0)]
            public PayloadStruct Value { get; set; }
        }

        public struct PayloadStruct
        {
            public int Id;
        }

        private sealed class ThrowingLegacyByRefFormatter :
            IMessagePackFormatter<PayloadStruct>,
            IMessagePackFormatterSerializeIn<PayloadStruct>,
            IMessagePackFormatterDeserializeRef<PayloadStruct>
        {
            internal int SerializeByValueCalls;
            internal int SerializeInCalls;
            internal int DeserializeByValueCalls;
            internal int DeserializeRefCalls;

            public void Serialize(ref MessagePackWriter writer, PayloadStruct value, MessagePackSerializerOptions options)
            {
                this.SerializeByValueCalls++;
                throw new InvalidOperationException("Legacy by-value serialize path should not be called.");
            }

            public void Serialize(ref MessagePackWriter writer, in PayloadStruct value, MessagePackSerializerOptions options)
            {
                this.SerializeInCalls++;
                writer.WriteArrayHeader(1);
                writer.Write(value.Id);
            }

            public PayloadStruct Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                this.DeserializeByValueCalls++;
                throw new InvalidOperationException("Legacy by-value deserialize path should not be called.");
            }

            public void Deserialize(ref MessagePackReader reader, ref PayloadStruct value, MessagePackSerializerOptions options)
            {
                this.DeserializeRefCalls++;
                _ = reader.ReadArrayHeader();
                value.Id = reader.ReadInt32();
            }
        }
    }
}

#endif
