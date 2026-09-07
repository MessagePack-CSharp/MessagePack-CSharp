// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using MessagePack.Formatters;

public class ByRefFormatterExecutionTests
{
    [Fact]
    public void GeneratedFormatter_UsesByRefDispatch_WhenStructFormatterSupportsIt()
    {
        ByRefTrackedStructFormatter.Reset();
        MessagePackSerializerOptions options = CreateOptions(new ByRefTrackedStructFormatter());
        IMessagePackFormatter<ByRefTrackedContainer>? formatter = GeneratedMessagePackResolver.Instance.GetFormatter<ByRefTrackedContainer>();
        Assert.NotNull(formatter);

        var expected = new ByRefTrackedContainer
        {
            Value = new ByRefTrackedStruct
            {
                Id = 5,
                Payload = "new",
            },
        };

        byte[] payload = SerializeWithFormatter(formatter, expected, options);

        Assert.Equal(1, ByRefTrackedStructFormatter.SerializeInCalls);
        Assert.Equal(0, ByRefTrackedStructFormatter.SerializeByValueCalls);

        var reader = new MessagePackReader(payload);
        ByRefTrackedContainer actual = formatter.Deserialize(ref reader, options);

        Assert.Equal(1, ByRefTrackedStructFormatter.DeserializeRefCalls);
        Assert.Equal(0, ByRefTrackedStructFormatter.DeserializeByValueCalls);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GeneratedFormatter_FallsBackToByValueDispatch_WhenStructFormatterOnlyImplementsLegacyInterface()
    {
        LegacyTrackedStructFormatter.Reset();
        MessagePackSerializerOptions options = CreateOptions(new LegacyTrackedStructFormatter());
        IMessagePackFormatter<LegacyTrackedContainer>? formatter = GeneratedMessagePackResolver.Instance.GetFormatter<LegacyTrackedContainer>();
        Assert.NotNull(formatter);

        var expected = new LegacyTrackedContainer
        {
            Value = new LegacyTrackedStruct
            {
                Id = 8,
                Payload = "legacy",
            },
        };

        byte[] payload = SerializeWithFormatter(formatter, expected, options);

        Assert.Equal(1, LegacyTrackedStructFormatter.SerializeByValueCalls);

        var reader = new MessagePackReader(payload);
        LegacyTrackedContainer actual = formatter.Deserialize(ref reader, options);

        Assert.Equal(1, LegacyTrackedStructFormatter.DeserializeByValueCalls);
        Assert.Equal(expected, actual);
    }

    private static MessagePackSerializerOptions CreateOptions(IMessagePackFormatter formatter)
    {
        return MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                new IMessagePackFormatter[] { formatter },
                new IFormatterResolver[] { GeneratedMessagePackResolver.Instance, StandardResolver.Instance }));
    }

    private static byte[] SerializeWithFormatter<T>(IMessagePackFormatter<T> formatter, T value, MessagePackSerializerOptions options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        formatter.Serialize(ref writer, value, options);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }
}

[MessagePackObject]
public sealed class ByRefTrackedContainer : IEquatable<ByRefTrackedContainer>
{
    [Key(0)]
    public ByRefTrackedStruct Value { get; set; }

    public bool Equals(ByRefTrackedContainer? other) => other is not null && this.Value.Equals(other.Value);

    public override bool Equals(object? obj) => this.Equals(obj as ByRefTrackedContainer);

    public override int GetHashCode() => this.Value.GetHashCode();
}

[MessagePackObject]
public struct ByRefTrackedStruct : IEquatable<ByRefTrackedStruct>
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public string? Payload { get; set; }

    public bool Equals(ByRefTrackedStruct other) => this.Id == other.Id && this.Payload == other.Payload;

    public override bool Equals(object? obj) => obj is ByRefTrackedStruct other && this.Equals(other);

    public override int GetHashCode() => HashCode.Combine(this.Id, this.Payload);
}

internal sealed class ByRefTrackedStructFormatter :
    IMessagePackFormatter<ByRefTrackedStruct>,
    IMessagePackFormatterSerializeIn<ByRefTrackedStruct>,
    IMessagePackFormatterDeserializeRef<ByRefTrackedStruct>
{
    internal static int SerializeByValueCalls;
    internal static int SerializeInCalls;
    internal static int DeserializeByValueCalls;
    internal static int DeserializeRefCalls;

    internal static void Reset()
    {
        SerializeByValueCalls = 0;
        SerializeInCalls = 0;
        DeserializeByValueCalls = 0;
        DeserializeRefCalls = 0;
    }

    public void Serialize(ref MessagePackWriter writer, ByRefTrackedStruct value, MessagePackSerializerOptions options)
    {
        SerializeByValueCalls++;
        Write(ref writer, in value, options);
    }

    public void Serialize(ref MessagePackWriter writer, in ByRefTrackedStruct value, MessagePackSerializerOptions options)
    {
        SerializeInCalls++;
        Write(ref writer, in value, options);
    }

    public ByRefTrackedStruct Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        DeserializeByValueCalls++;
        var value = default(ByRefTrackedStruct);
        this.Deserialize(ref reader, ref value, options);
        return value;
    }

    public void Deserialize(ref MessagePackReader reader, ref ByRefTrackedStruct value, MessagePackSerializerOptions options)
    {
        DeserializeRefCalls++;
        _ = reader.ReadArrayHeader();
        value.Id = reader.ReadInt32();
        value.Payload = MessagePackSerializer.Deserialize<string?>(ref reader, options);
    }

    private static void Write(ref MessagePackWriter writer, in ByRefTrackedStruct value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(2);
        writer.Write(value.Id);
        MessagePackSerializer.Serialize(ref writer, value.Payload, options);
    }
}

[MessagePackObject]
public sealed class LegacyTrackedContainer : IEquatable<LegacyTrackedContainer>
{
    [Key(0)]
    public LegacyTrackedStruct Value { get; set; }

    public bool Equals(LegacyTrackedContainer? other) => other is not null && this.Value.Equals(other.Value);

    public override bool Equals(object? obj) => this.Equals(obj as LegacyTrackedContainer);

    public override int GetHashCode() => this.Value.GetHashCode();
}

[MessagePackObject]
public struct LegacyTrackedStruct : IEquatable<LegacyTrackedStruct>
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public string? Payload { get; set; }

    public bool Equals(LegacyTrackedStruct other) => this.Id == other.Id && this.Payload == other.Payload;

    public override bool Equals(object? obj) => obj is LegacyTrackedStruct other && this.Equals(other);

    public override int GetHashCode() => HashCode.Combine(this.Id, this.Payload);
}

internal sealed class LegacyTrackedStructFormatter : IMessagePackFormatter<LegacyTrackedStruct>
{
    internal static int SerializeByValueCalls;
    internal static int DeserializeByValueCalls;

    internal static void Reset()
    {
        SerializeByValueCalls = 0;
        DeserializeByValueCalls = 0;
    }

    public void Serialize(ref MessagePackWriter writer, LegacyTrackedStruct value, MessagePackSerializerOptions options)
    {
        SerializeByValueCalls++;
        writer.WriteArrayHeader(2);
        writer.Write(value.Id);
        MessagePackSerializer.Serialize(ref writer, value.Payload, options);
    }

    public LegacyTrackedStruct Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        DeserializeByValueCalls++;
        _ = reader.ReadArrayHeader();
        return new LegacyTrackedStruct
        {
            Id = reader.ReadInt32(),
            Payload = MessagePackSerializer.Deserialize<string?>(ref reader, options),
        };
    }
}
