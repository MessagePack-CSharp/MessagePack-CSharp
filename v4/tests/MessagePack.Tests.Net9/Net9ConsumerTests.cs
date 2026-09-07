using System.Runtime.Versioning;
using MessagePack;

namespace MessagePack.Tests.Net9;

// The generated formatters of this assembly compile against the netstandard2.1 core and
// are served through its compat (Type-based) factory dispatch.
public class Net9ConsumerTests
{
    [Fact]
    public void CoreIsTheNetStandard21Asset()
    {
        var target = typeof(MessagePackSerializer).Assembly.GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
            .OfType<TargetFrameworkAttribute>().Single().FrameworkName;
        Assert.Equal(".NETStandard,Version=v2.1", target);
    }

    [Fact]
    public void GeneratedKeyedPoco_Roundtrips()
    {
        var value = new KeyedPoco { Id = 7, Name = "seven", Tags = ["a", "b"], Nested = new MapPoco { Score = 1.5, Flags = [true, false] } };
        var bytes = MessagePackSerializer.Serialize(value);
        var back = MessagePackSerializer.Deserialize<KeyedPoco>(bytes)!;
        Assert.Equal(7, back.Id);
        Assert.Equal("seven", back.Name);
        Assert.Equal(["a", "b"], back.Tags);
        Assert.Equal(1.5, back.Nested!.Score);
        Assert.Equal([true, false], back.Nested.Flags!);

        // the generated formatter, not the reflection fallback, serves the type
        var formatter = MessagePackSerializerOptions.Default.Resolver.GetFormatter<SerializerFoundation.CompatibleArrayPoolListWriteBuffer, SerializerFoundation.CompatibleReadOnlySpanReadBuffer, KeyedPoco>();
        Assert.Contains("KeyedPocoFormatter", formatter.GetType().Name);
    }

    [Fact]
    public void GeneratedMapPoco_Roundtrips()
    {
        var bytes = MessagePackSerializer.Serialize(new MapPoco { Score = 2.5, Flags = [true] });
        Assert.Equal("{\"Score\":2.5,\"Flags\":[true]}", MessagePackSerializer.ConvertToJson(bytes));
        Assert.Equal(2.5, MessagePackSerializer.Deserialize<MapPoco>(bytes)!.Score);
    }

    [Fact]
    public async Task StreamAndPipeEntries_Work()
    {
        var stream = new MemoryStream();
        await MessagePackSerializer.SerializeAsync(stream, new KeyedPoco { Id = 1, Name = "one" });
        stream.Position = 0;
        Assert.Equal("one", (await MessagePackSerializer.DeserializeAsync<KeyedPoco>(stream))!.Name);

        var elements = new MemoryStream();
        await MessagePackSerializer.SerializeElementsAsync(elements, new[] { 1, 2, 3 }, 3);
        elements.Position = 0;
        var seen = new List<int>();
        await foreach (var element in MessagePackSerializer.DeserializeElementsAsync<int>(elements))
        {
            seen.Add(element);
        }
        Assert.Equal([1, 2, 3], seen);
    }
}

[MessagePackObject]
public class KeyedPoco
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
    [Key(2)] public List<string>? Tags { get; set; }
    [Key(3)] public MapPoco? Nested { get; set; }
}

[MessagePackObject(true)]
public class MapPoco
{
    public double Score { get; set; }
    public bool[]? Flags { get; set; }
}
