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
    public class DynamicObjectResolverClassReuseTests
    {
        [Fact]
        public void DynamicObjectResolver_ReusesExistingClassGraph_ForIntKeyMembers()
        {
            GraphPayloadFormatter.Reset();
            var options = CreateOptions(new GraphPayloadFormatter());

            var payload = MessagePackSerializer.Serialize(
                new IntKeyGraphHolder
                {
                    Value = new GraphPayload { Id = 10, Name = "new" },
                    Version = 2,
                },
                options);

            var reader = new MessagePackReader(payload);
            var existing = new IntKeyGraphHolder
            {
                Value = new GraphPayload { Id = -1, Name = "old" },
                Version = 1,
            };

            IntKeyGraphHolder? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Same(existing, result);
            Assert.NotNull(result!.Value);
            Assert.Same(existing.Value, result.Value);
            Assert.Equal(10, existing.Value!.Id);
            Assert.Equal("new", existing.Value.Name);
            Assert.Equal(2, existing.Version);
            Assert.Equal(1, GraphPayloadFormatter.DeserializeIntoCalls);
            Assert.Equal(0, GraphPayloadFormatter.DeserializeByValueCalls);
        }

        [Fact]
        public void DynamicObjectResolver_ReusesExistingClassGraph_ForStringKeyMembers()
        {
            GraphPayloadFormatter.Reset();
            var options = CreateOptions(new GraphPayloadFormatter());

            var payload = MessagePackSerializer.Serialize(
                new StringKeyGraphHolder
                {
                    Value = new GraphPayload { Id = 21, Name = "new-string" },
                    Version = 4,
                },
                options);

            var reader = new MessagePackReader(payload);
            var existing = new StringKeyGraphHolder
            {
                Value = new GraphPayload { Id = -1, Name = "old-string" },
                Version = 3,
            };

            StringKeyGraphHolder? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Same(existing, result);
            Assert.NotNull(result!.Value);
            Assert.Same(existing.Value, result.Value);
            Assert.Equal(21, existing.Value!.Id);
            Assert.Equal("new-string", existing.Value.Name);
            Assert.Equal(4, existing.Version);
            Assert.Equal(1, GraphPayloadFormatter.DeserializeIntoCalls);
            Assert.Equal(0, GraphPayloadFormatter.DeserializeByValueCalls);
        }

        [Fact]
        public void DynamicObjectResolver_ReusesExistingGenericInitOnlyClass_ForIntKeys()
        {
            MessagePackSerializerOptions options = CreateOptions();

            byte[] payload = MessagePackSerializer.Serialize(
                new GenericIntKeyInitOnlyHolder<int> { Name = "new-name" },
                options);

            var reader = new MessagePackReader(payload);
            var existing = new GenericIntKeyInitOnlyHolder<int> { Name = "old-name" };

            GenericIntKeyInitOnlyHolder<int>? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Same(existing, result);
            Assert.Equal("new-name", result!.Name);
        }

        [Fact]
        public void DynamicObjectResolver_ReusesExistingGenericInitOnlyClass_ForStringKeys()
        {
            MessagePackSerializerOptions options = CreateOptions();

            byte[] payload = MessagePackSerializer.Serialize(
                new GenericStringKeyInitOnlyHolder<int> { Name = "new-name" },
                options);

            var reader = new MessagePackReader(payload);
            var existing = new GenericStringKeyInitOnlyHolder<int> { Name = "old-name" };

            GenericStringKeyInitOnlyHolder<int>? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Same(existing, result);
            Assert.Equal("new-name", result!.Name);
        }

        private static MessagePackSerializerOptions CreateOptions(IMessagePackFormatter formatter)
        {
            return MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(
                    new IMessagePackFormatter[] { formatter },
                    new IFormatterResolver[] { DynamicObjectResolver.Instance, StandardResolver.Instance }));
        }

        private static MessagePackSerializerOptions CreateOptions()
        {
            return MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(
                    Array.Empty<IMessagePackFormatter>(),
                    new IFormatterResolver[] { DynamicObjectResolver.Instance, StandardResolver.Instance }));
        }

        [MessagePackObject]
        public class IntKeyGraphHolder
        {
            [Key(0)]
            public GraphPayload? Value { get; set; }

            [Key(1)]
            public int Version { get; set; }
        }

        [MessagePackObject]
        public class StringKeyGraphHolder
        {
            [Key("value")]
            public GraphPayload? Value { get; set; }

            [Key("version")]
            public int Version { get; set; }
        }

        [MessagePackObject]
        public class GraphPayload
        {
            [Key(0)]
            public int Id { get; set; }

            [Key(1)]
            public string? Name { get; set; }
        }

        [MessagePackObject]
        public class GenericIntKeyInitOnlyHolder<T>
        {
            [Key(0)]
            public string? Name { get; init; }
        }

        [MessagePackObject]
        public class GenericStringKeyInitOnlyHolder<T>
        {
            [Key("name")]
            public string? Name { get; init; }
        }

        private sealed class GraphPayloadFormatter :
            IMessagePackFormatter<GraphPayload?>,
            IMessagePackFormatterDeserializeInto<GraphPayload>
        {
            internal static int DeserializeByValueCalls;
            internal static int DeserializeIntoCalls;

            internal static void Reset()
            {
                DeserializeByValueCalls = 0;
                DeserializeIntoCalls = 0;
            }

            public void Serialize(ref MessagePackWriter writer, GraphPayload? value, MessagePackSerializerOptions options)
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

            public GraphPayload? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                DeserializeByValueCalls++;
                if (reader.TryReadNil())
                {
                    return null;
                }

                _ = reader.ReadArrayHeader();
                return new GraphPayload
                {
                    Id = reader.ReadInt32(),
                    Name = reader.ReadString(),
                };
            }

            public void Deserialize(ref MessagePackReader reader, GraphPayload value, MessagePackSerializerOptions options)
            {
                DeserializeIntoCalls++;
                _ = reader.ReadArrayHeader();
                value.Id = reader.ReadInt32();
                value.Name = reader.ReadString();
            }
        }
    }
}

#endif
