// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using MessagePack.Formatters;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
    public class ClassReuseApiTests
    {
        [Fact]
        public void DeserializeInto_ReusesExistingReference_WhenAvailable()
        {
            var formatter = new ReuseAwareFormatter();
            var options = MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(new IMessagePackFormatter[] { formatter }, new IFormatterResolver[] { StandardResolver.Instance }));

            var payload = MessagePackSerializer.Serialize(new ReusableClass { Id = 9, Name = "new" }, options);
            var reader = new MessagePackReader(payload);
            var existing = new ReusableClass { Id = -1, Name = "old" };

            ReusableClass? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Same(existing, result);
            Assert.Equal("old", formatter.LastObservedOldName);
            Assert.Equal(9, existing.Id);
            Assert.Equal("new", existing.Name);
            Assert.Equal(1, formatter.DeserializeIntoCalls);
            Assert.Equal(0, formatter.DeserializeByValueCalls);
        }

        [Fact]
        public void DeserializeInto_FallsBackToLegacyFormatter_WhenIntoInterfaceMissing()
        {
            var formatter = new LegacyOnlyFormatter();
            var options = MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(new IMessagePackFormatter[] { formatter }, new IFormatterResolver[] { StandardResolver.Instance }));

            var payload = MessagePackSerializer.Serialize(new ReusableClass { Id = 7, Name = "new" }, options);
            var reader = new MessagePackReader(payload);
            var existing = new ReusableClass { Id = -1, Name = "old" };

            ReusableClass? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.NotSame(existing, result);
            Assert.Equal(-1, existing.Id);
            Assert.Equal("old", existing.Name);
            Assert.NotNull(result);
            Assert.Equal(7, result!.Id);
            Assert.Equal("new", result.Name);
            Assert.Equal(1, formatter.DeserializeByValueCalls);
        }

        [Fact]
        public void DeserializeInto_ReturnsNewInstance_WhenExistingIsNull()
        {
            var formatter = new ReuseAwareFormatter();
            var options = MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(new IMessagePackFormatter[] { formatter }, new IFormatterResolver[] { StandardResolver.Instance }));

            var payload = MessagePackSerializer.Serialize(new ReusableClass { Id = 12, Name = "created" }, options);
            var reader = new MessagePackReader(payload);

            ReusableClass? result = MessagePackSerializer.Deserialize<ReusableClass>(ref reader, existing: null, options);

            Assert.NotNull(result);
            Assert.Equal(12, result!.Id);
            Assert.Equal("created", result.Name);
            Assert.Equal(0, formatter.DeserializeIntoCalls);
            Assert.Equal(1, formatter.DeserializeByValueCalls);
        }

        [Fact]
        public void DeserializeInto_ReturnsNull_WhenPayloadIsNil()
        {
            var formatter = new ReuseAwareFormatter();
            var options = MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(new IMessagePackFormatter[] { formatter }, new IFormatterResolver[] { StandardResolver.Instance }));

            byte[] payload = MessagePackSerializer.Serialize<ReusableClass?>(null, options);
            var reader = new MessagePackReader(payload);
            var existing = new ReusableClass { Id = -1, Name = "old" };

            ReusableClass? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Null(result);
            Assert.Equal(-1, existing.Id);
            Assert.Equal("old", existing.Name);
            Assert.Equal(0, formatter.DeserializeIntoCalls);
            Assert.Equal(0, formatter.DeserializeByValueCalls);
        }

        [Fact]
        public void DeserializeInto_ReusesExistingReference_WhenPayloadIsCompressed()
        {
            var formatter = new ReuseAwareFormatter();
            var options = MessagePackSerializerOptions.Standard
                .WithCompression(MessagePackCompression.Lz4Block)
                .WithResolver(CompositeResolver.Create(new IMessagePackFormatter[] { formatter }, new IFormatterResolver[] { StandardResolver.Instance }));

            var payload = MessagePackSerializer.Serialize(new ReusableClass { Id = 27, Name = "compressed" }, options);
            var reader = new MessagePackReader(payload);
            var existing = new ReusableClass { Id = -1, Name = "old" };

            ReusableClass? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Same(existing, result);
            Assert.Equal(27, existing.Id);
            Assert.Equal("compressed", existing.Name);
            Assert.Equal(1, formatter.DeserializeIntoCalls);
            Assert.Equal(0, formatter.DeserializeByValueCalls);
        }

        [MessagePackObject]
        public class ReusableClass
        {
            [Key(0)]
            public int Id { get; set; }

            [Key(1)]
            public string? Name { get; set; }
        }

        private sealed class ReuseAwareFormatter :
            IMessagePackFormatter<ReusableClass?>,
            IMessagePackFormatterDeserializeInto<ReusableClass>
        {
            internal int DeserializeByValueCalls;
            internal int DeserializeIntoCalls;
            internal string? LastObservedOldName;

            public void Serialize(ref MessagePackWriter writer, ReusableClass? value, MessagePackSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNil();
                    return;
                }

                writer.WriteArrayHeader(2);
                writer.Write(value.Id);
                writer.Write(value.Name);
            }

            public ReusableClass? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                this.DeserializeByValueCalls++;
                if (reader.TryReadNil())
                {
                    return null;
                }

                _ = reader.ReadArrayHeader();
                return new ReusableClass
                {
                    Id = reader.ReadInt32(),
                    Name = reader.ReadString(),
                };
            }

            public void Deserialize(ref MessagePackReader reader, ReusableClass value, MessagePackSerializerOptions options)
            {
                this.DeserializeIntoCalls++;
                this.LastObservedOldName = value.Name;
                _ = reader.ReadArrayHeader();
                value.Id = reader.ReadInt32();
                value.Name = reader.ReadString();
            }
        }

        private sealed class LegacyOnlyFormatter : IMessagePackFormatter<ReusableClass?>
        {
            internal int DeserializeByValueCalls;

            public void Serialize(ref MessagePackWriter writer, ReusableClass? value, MessagePackSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNil();
                    return;
                }

                writer.WriteArrayHeader(2);
                writer.Write(value.Id);
                writer.Write(value.Name);
            }

            public ReusableClass? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                this.DeserializeByValueCalls++;
                if (reader.TryReadNil())
                {
                    return null;
                }

                _ = reader.ReadArrayHeader();
                return new ReusableClass
                {
                    Id = reader.ReadInt32(),
                    Name = reader.ReadString(),
                };
            }
        }
    }
}
