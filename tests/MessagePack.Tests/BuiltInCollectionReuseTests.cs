// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
    public class BuiltInCollectionReuseTests
    {
        [Fact]
        public void DeserializeInto_ReusesExistingListAndNestedClassItems()
        {
            ReusablePayloadFormatter.Reset();
            MessagePackSerializerOptions options = CreateOptions(new ReusablePayloadFormatter());

            byte[] payload = MessagePackSerializer.Serialize(
                new List<ReusablePayload>
                {
                    new() { Id = 10, Name = "new-0" },
                    new() { Id = 20, Name = "new-1" },
                },
                options);

            var first = new ReusablePayload { Id = -1, Name = "old-0" };
            var second = new ReusablePayload { Id = -2, Name = "old-1" };
            var existing = new List<ReusablePayload> { first, second };
            var reader = new MessagePackReader(payload);

            List<ReusablePayload>? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Same(existing, result);
            Assert.NotNull(result);
            Assert.Equal(2, result!.Count);
            Assert.Same(first, result[0]);
            Assert.Same(second, result[1]);
            Assert.Equal(10, first.Id);
            Assert.Equal("new-0", first.Name);
            Assert.Equal(20, second.Id);
            Assert.Equal("new-1", second.Name);
            Assert.Equal(2, ReusablePayloadFormatter.DeserializeIntoCalls);
            Assert.Equal(0, ReusablePayloadFormatter.DeserializeByValueCalls);
        }

        [Fact]
        public void DeserializeInto_ReusesExistingListAndRemovesExtraItems()
        {
            ReusablePayloadFormatter.Reset();
            MessagePackSerializerOptions options = CreateOptions(new ReusablePayloadFormatter());

            byte[] payload = MessagePackSerializer.Serialize(
                new List<ReusablePayload>
                {
                    new() { Id = 99, Name = "remaining" },
                },
                options);

            var remaining = new ReusablePayload { Id = -1, Name = "old-remaining" };
            var removed = new ReusablePayload { Id = -2, Name = "removed" };
            var existing = new List<ReusablePayload> { remaining, removed };
            var reader = new MessagePackReader(payload);

            List<ReusablePayload>? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Same(existing, result);
            Assert.NotNull(result);
            Assert.Single(result!);
            Assert.Same(remaining, result[0]);
            Assert.Equal(99, remaining.Id);
            Assert.Equal("remaining", remaining.Name);
            Assert.Equal(1, ReusablePayloadFormatter.DeserializeIntoCalls);
            Assert.Equal(0, ReusablePayloadFormatter.DeserializeByValueCalls);
        }

        [Fact]
        public void DeserializeInto_ReusesExistingDictionaryAndNestedClassValues()
        {
            ReusablePayloadFormatter.Reset();
            MessagePackSerializerOptions options = CreateOptions(new ReusablePayloadFormatter());

            byte[] payload = MessagePackSerializer.Serialize(
                new Dictionary<int, ReusablePayload>
                {
                    [1] = new() { Id = 101, Name = "new-1" },
                    [2] = new() { Id = 202, Name = "new-2" },
                },
                options);

            var first = new ReusablePayload { Id = -1, Name = "old-1" };
            var second = new ReusablePayload { Id = -2, Name = "old-2" };
            var existing = new Dictionary<int, ReusablePayload>
            {
                [1] = first,
                [2] = second,
            };
            var reader = new MessagePackReader(payload);

            Dictionary<int, ReusablePayload>? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Same(existing, result);
            Assert.NotNull(result);
            Assert.Equal(2, result!.Count);
            Assert.Same(first, result[1]);
            Assert.Same(second, result[2]);
            Assert.Equal(101, first.Id);
            Assert.Equal("new-1", first.Name);
            Assert.Equal(202, second.Id);
            Assert.Equal("new-2", second.Name);
            Assert.Equal(2, ReusablePayloadFormatter.DeserializeIntoCalls);
            Assert.Equal(0, ReusablePayloadFormatter.DeserializeByValueCalls);
        }

        [Fact]
        public void DeserializeInto_ReusesExistingDictionaryAndRemovesMissingKeys()
        {
            ReusablePayloadFormatter.Reset();
            MessagePackSerializerOptions options = CreateOptions(new ReusablePayloadFormatter());

            byte[] payload = MessagePackSerializer.Serialize(
                new Dictionary<int, ReusablePayload>
                {
                    [2] = new() { Id = 2002, Name = "remaining" },
                },
                options);

            var removed = new ReusablePayload { Id = -1, Name = "removed" };
            var remaining = new ReusablePayload { Id = -2, Name = "old-remaining" };
            var existing = new Dictionary<int, ReusablePayload>
            {
                [1] = removed,
                [2] = remaining,
            };
            var reader = new MessagePackReader(payload);

            Dictionary<int, ReusablePayload>? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Same(existing, result);
            Assert.NotNull(result);
            Assert.Single(result!);
            Assert.False(result.ContainsKey(1));
            Assert.Same(remaining, result[2]);
            Assert.Equal(2002, remaining.Id);
            Assert.Equal("remaining", remaining.Name);
            Assert.Equal(1, ReusablePayloadFormatter.DeserializeIntoCalls);
            Assert.Equal(0, ReusablePayloadFormatter.DeserializeByValueCalls);
        }

        [Fact]
        public void DeserializeInto_ReusesExistingInterfaceListAndNestedClassItems()
        {
            ReusablePayloadFormatter.Reset();
            MessagePackSerializerOptions options = CreateOptions(new ReusablePayloadFormatter());

            byte[] payload = MessagePackSerializer.Serialize<IList<ReusablePayload>>(
                new List<ReusablePayload>
                {
                    new() { Id = 301, Name = "new-0" },
                    new() { Id = 302, Name = "new-1" },
                },
                options);

            var first = new ReusablePayload { Id = -1, Name = "old-0" };
            var second = new ReusablePayload { Id = -2, Name = "old-1" };
            IList<ReusablePayload> existing = new List<ReusablePayload> { first, second };
            var reader = new MessagePackReader(payload);

            IList<ReusablePayload>? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Same(existing, result);
            Assert.NotNull(result);
            Assert.Equal(2, result!.Count);
            Assert.Same(first, result[0]);
            Assert.Same(second, result[1]);
            Assert.Equal(301, first.Id);
            Assert.Equal("new-0", first.Name);
            Assert.Equal(302, second.Id);
            Assert.Equal("new-1", second.Name);
            Assert.Equal(2, ReusablePayloadFormatter.DeserializeIntoCalls);
            Assert.Equal(0, ReusablePayloadFormatter.DeserializeByValueCalls);
        }

        [Fact]
        public void DeserializeInto_ReusesExistingInterfaceDictionaryAndNestedClassValues()
        {
            ReusablePayloadFormatter.Reset();
            MessagePackSerializerOptions options = CreateOptions(new ReusablePayloadFormatter());

            byte[] payload = MessagePackSerializer.Serialize<IDictionary<int, ReusablePayload>>(
                new Dictionary<int, ReusablePayload>
                {
                    [1] = new() { Id = 401, Name = "new-1" },
                    [2] = new() { Id = 402, Name = "new-2" },
                },
                options);

            var first = new ReusablePayload { Id = -1, Name = "old-1" };
            var second = new ReusablePayload { Id = -2, Name = "old-2" };
            IDictionary<int, ReusablePayload> existing = new Dictionary<int, ReusablePayload>
            {
                [1] = first,
                [2] = second,
            };
            var reader = new MessagePackReader(payload);

            IDictionary<int, ReusablePayload>? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

            Assert.Same(existing, result);
            Assert.NotNull(result);
            Assert.Equal(2, result!.Count);
            Assert.Same(first, result[1]);
            Assert.Same(second, result[2]);
            Assert.Equal(401, first.Id);
            Assert.Equal("new-1", first.Name);
            Assert.Equal(402, second.Id);
            Assert.Equal("new-2", second.Name);
            Assert.Equal(2, ReusablePayloadFormatter.DeserializeIntoCalls);
            Assert.Equal(0, ReusablePayloadFormatter.DeserializeByValueCalls);
        }

        private static MessagePackSerializerOptions CreateOptions(IMessagePackFormatter formatter)
        {
            return MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(
                    new IMessagePackFormatter[] { formatter },
                    new IFormatterResolver[] { StandardResolver.Instance }));
        }

        [MessagePackObject]
        public sealed class ReusablePayload
        {
            [Key(0)]
            public int Id { get; set; }

            [Key(1)]
            public string? Name { get; set; }
        }

        private sealed class ReusablePayloadFormatter :
            IMessagePackFormatter<ReusablePayload?>,
            IMessagePackFormatterDeserializeInto<ReusablePayload>
        {
            internal static int DeserializeByValueCalls;
            internal static int DeserializeIntoCalls;

            internal static void Reset()
            {
                DeserializeByValueCalls = 0;
                DeserializeIntoCalls = 0;
            }

            public void Serialize(ref MessagePackWriter writer, ReusablePayload? value, MessagePackSerializerOptions options)
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

            public ReusablePayload? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                DeserializeByValueCalls++;
                if (reader.TryReadNil())
                {
                    return null;
                }

                _ = reader.ReadArrayHeader();
                return new ReusablePayload
                {
                    Id = reader.ReadInt32(),
                    Name = reader.ReadString(),
                };
            }

            public void Deserialize(ref MessagePackReader reader, ReusablePayload value, MessagePackSerializerOptions options)
            {
                DeserializeIntoCalls++;
                _ = reader.ReadArrayHeader();
                value.Id = reader.ReadInt32();
                value.Name = reader.ReadString();
            }
        }
    }
}
