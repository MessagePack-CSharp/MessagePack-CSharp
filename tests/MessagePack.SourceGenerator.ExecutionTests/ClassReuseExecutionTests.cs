// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using MessagePack.Formatters;

public class ClassReuseExecutionTests
{
    [Fact]
    public void GeneratedFormatter_ReusesExistingClassGraph_ForIntKeyMembers()
    {
        GeneratedGraphPayloadFormatter.Reset();
        MessagePackSerializerOptions options = CreateOptions(new GeneratedGraphPayloadFormatter());

        var expected = new GeneratedIntKeyGraphHolder
        {
            Value = new GeneratedGraphPayload { Id = 11, Name = "new" },
            Version = 2,
        };

        byte[] payload = Serialize(expected, options);
        var reader = new MessagePackReader(payload);
        var existing = new GeneratedIntKeyGraphHolder
        {
            Value = new GeneratedGraphPayload { Id = -1, Name = "old" },
            Version = 1,
        };

        GeneratedIntKeyGraphHolder? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

        Assert.Same(existing, result);
        Assert.NotNull(result!.Value);
        Assert.Same(existing.Value, result.Value);
        Assert.Equal(11, existing.Value!.Id);
        Assert.Equal("new", existing.Value.Name);
        Assert.Equal(2, existing.Version);
        Assert.Equal(1, GeneratedGraphPayloadFormatter.DeserializeIntoCalls);
        Assert.Equal(0, GeneratedGraphPayloadFormatter.DeserializeByValueCalls);
    }

    [Fact]
    public void GeneratedFormatter_ReusesExistingClassGraph_ForStringKeyMembers()
    {
        GeneratedGraphPayloadFormatter.Reset();
        MessagePackSerializerOptions options = CreateOptions(new GeneratedGraphPayloadFormatter());

        var expected = new GeneratedStringKeyGraphHolder
        {
            Value = new GeneratedGraphPayload { Id = 23, Name = "new-string" },
            Version = 4,
        };

        byte[] payload = Serialize(expected, options);
        var reader = new MessagePackReader(payload);
        var existing = new GeneratedStringKeyGraphHolder
        {
            Value = new GeneratedGraphPayload { Id = -1, Name = "old-string" },
            Version = 3,
        };

        GeneratedStringKeyGraphHolder? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

        Assert.Same(existing, result);
        Assert.NotNull(result!.Value);
        Assert.Same(existing.Value, result.Value);
        Assert.Equal(23, existing.Value!.Id);
        Assert.Equal("new-string", existing.Value.Name);
        Assert.Equal(4, existing.Version);
        Assert.Equal(1, GeneratedGraphPayloadFormatter.DeserializeIntoCalls);
        Assert.Equal(0, GeneratedGraphPayloadFormatter.DeserializeByValueCalls);
    }

    [Fact]
    public void GeneratedFormatter_FallsBackToNewInstance_ForInitOnlyMembers()
    {
        MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(Array.Empty<IMessagePackFormatter>(), new IFormatterResolver[] { GeneratedMessagePackResolver.Instance, StandardResolver.Instance }));

        var expected = new GeneratedInitOnlyHolder { Name = "new-name" };
        byte[] payload = Serialize(expected, options);
        var reader = new MessagePackReader(payload);
        var existing = new GeneratedInitOnlyHolder { Name = "old-name" };

        GeneratedInitOnlyHolder? result = MessagePackSerializer.Deserialize(ref reader, existing, options);

        Assert.NotNull(result);
        Assert.NotSame(existing, result);
        Assert.Equal("old-name", existing.Name);
        Assert.Equal("new-name", result!.Name);
    }

    private static MessagePackSerializerOptions CreateOptions(IMessagePackFormatter formatter)
    {
        return MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                new IMessagePackFormatter[] { formatter },
                new IFormatterResolver[] { GeneratedMessagePackResolver.Instance, StandardResolver.Instance }));
    }

    private static byte[] Serialize<T>(T value, MessagePackSerializerOptions options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        MessagePackSerializer.Serialize(ref writer, value, options);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }
}

[MessagePackObject]
public sealed class GeneratedIntKeyGraphHolder
{
    [Key(0)]
    [MessagePackFormatter(typeof(GeneratedGraphPayloadFormatter))]
    public GeneratedGraphPayload? Value { get; set; }

    [Key(1)]
    public int Version { get; set; }
}

[MessagePackObject]
public sealed class GeneratedStringKeyGraphHolder
{
    [Key("value")]
    [MessagePackFormatter(typeof(GeneratedGraphPayloadFormatter))]
    public GeneratedGraphPayload? Value { get; set; }

    [Key("version")]
    public int Version { get; set; }
}

public sealed class GeneratedGraphPayload
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public string? Name { get; set; }
}

[MessagePackObject]
public sealed class GeneratedInitOnlyHolder
{
    [Key(0)]
    public string? Name { get; init; }
}

internal sealed class GeneratedGraphPayloadFormatter :
    IMessagePackFormatter<GeneratedGraphPayload?>,
    IMessagePackFormatterDeserializeInto<GeneratedGraphPayload>
{
    internal static int DeserializeByValueCalls;
    internal static int DeserializeIntoCalls;

    internal static void Reset()
    {
        DeserializeByValueCalls = 0;
        DeserializeIntoCalls = 0;
    }

    public void Serialize(ref MessagePackWriter writer, GeneratedGraphPayload? value, MessagePackSerializerOptions options)
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

    public GeneratedGraphPayload? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        DeserializeByValueCalls++;
        if (reader.TryReadNil())
        {
            return null;
        }

        _ = reader.ReadArrayHeader();
        return new GeneratedGraphPayload
        {
            Id = reader.ReadInt32(),
            Name = reader.ReadString(),
        };
    }

    public void Deserialize(ref MessagePackReader reader, GeneratedGraphPayload value, MessagePackSerializerOptions options)
    {
        DeserializeIntoCalls++;
        _ = reader.ReadArrayHeader();
        value.Id = reader.ReadInt32();
        value.Name = reader.ReadString();
    }
}
