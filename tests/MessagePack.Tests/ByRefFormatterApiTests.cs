// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Buffers;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests
{
    public class ByRefFormatterApiTests
    {
        [Fact]
        public void SerializeIn_UsesByRefInterface_WhenAvailable()
        {
            var formatter = new CounterFormatter();
            var options = MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(new IMessagePackFormatter[] { formatter }, new IFormatterResolver[] { StandardResolver.Instance }));

            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);
            var value = new TestStruct { Id = 3, PooledObject = "old" };

            MessagePackSerializer.Serialize(ref writer, in value, options);

            Assert.Equal(1, formatter.SerializeInCalls);
            Assert.Equal(0, formatter.SerializeByValueCalls);
        }

        [Fact]
        public void DeserializeRef_ReusesExistingValue_WhenAvailable()
        {
            var formatter = new CounterFormatter();
            var options = MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(new IMessagePackFormatter[] { formatter }, new IFormatterResolver[] { StandardResolver.Instance }));

            var payload = MessagePackSerializer.Serialize(new TestStruct { Id = 9, PooledObject = "new" }, options);
            var reader = new MessagePackReader(payload);
            var value = new TestStruct { Id = -1, PooledObject = "old-pooled" };

            MessagePackSerializer.Deserialize(ref reader, ref value, options);

            Assert.Equal("old-pooled", formatter.LastObservedOldObject);
            Assert.Equal(9, value.Id);
            Assert.Equal("new", value.PooledObject);
            Assert.Equal(1, formatter.DeserializeRefCalls);
        }

        [MessagePackObject]
        public struct TestStruct
        {
            [Key(0)]
            public int Id;

            [Key(1)]
            public object? PooledObject;
        }

        private sealed class CounterFormatter :
            IMessagePackFormatter<TestStruct>,
            IMessagePackFormatterSerializeIn<TestStruct>,
            IMessagePackFormatterDeserializeRef<TestStruct>
        {
            internal int SerializeByValueCalls;
            internal int SerializeInCalls;
            internal int DeserializeByValueCalls;
            internal int DeserializeRefCalls;
            internal object? LastObservedOldObject;

            public void Serialize(ref MessagePackWriter writer, TestStruct value, MessagePackSerializerOptions options)
            {
                this.SerializeByValueCalls++;
                writer.WriteArrayHeader(2);
                writer.Write(value.Id);
                MessagePackSerializer.Serialize(ref writer, value.PooledObject, options);
            }

            public void Serialize(ref MessagePackWriter writer, in TestStruct value, MessagePackSerializerOptions options)
            {
                this.SerializeInCalls++;
                writer.WriteArrayHeader(2);
                writer.Write(value.Id);
                MessagePackSerializer.Serialize(ref writer, value.PooledObject, options);
            }

            public TestStruct Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                this.DeserializeByValueCalls++;
                var result = default(TestStruct);
                this.Deserialize(ref reader, ref result, options);
                return result;
            }

            public void Deserialize(ref MessagePackReader reader, ref TestStruct value, MessagePackSerializerOptions options)
            {
                this.DeserializeRefCalls++;
                this.LastObservedOldObject = value.PooledObject;
                _ = reader.ReadArrayHeader();
                value.Id = reader.ReadInt32();
                value.PooledObject = MessagePackSerializer.Deserialize<object?>(ref reader, options);
            }
        }
    }
}
