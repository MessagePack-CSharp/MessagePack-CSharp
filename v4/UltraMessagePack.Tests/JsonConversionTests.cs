namespace UltraMessagePack.Tests;

public class JsonConversionTests
{
    [Fact]
    public void ConvertToJson_BasicShapes()
    {
        var payload = MessagePackSerializer.Serialize(new Dictionary<string, object?>
        {
            ["name"] = "山岡",
            ["age"] = 27,
            ["negative"] = -5,
            ["pi"] = 3.25,
            ["flag"] = true,
            ["nothing"] = null,
            ["list"] = new object?[] { 1, "two" },
        }, new MessagePackSerializerOptions(new MessagePackFormatterResolver([MessagePackFormatterFactory.Default])));

        var json = MessagePackSerializer.ConvertToJson(payload);
        Assert.Equal("""{"name":"山岡","age":27,"negative":-5,"pi":3.25,"flag":true,"nothing":null,"list":[1,"two"]}""", json);
    }

    [Fact]
    public void ConvertFromJson_RoundTripsSemantics()
    {
        const string json = """{"a":1,"b":[true,null,"s"],"c":{"nested":-2},"d":1.5}""";
        var payload = MessagePackSerializer.ConvertFromJson(json);
        Assert.Equal(json, MessagePackSerializer.ConvertToJson(payload));
    }

    [Fact]
    public void ConvertFromJson_WritesSmallestIntegers()
    {
        Assert.Equal(new byte[] { 0x2A }, MessagePackSerializer.ConvertFromJson("42")); // positive fixint
        Assert.Equal(new byte[] { 0xC0 }, MessagePackSerializer.ConvertFromJson("null"));
    }

    [Fact]
    public void ConvertToJson_Binary_IsBase64String()
    {
        var payload = MessagePackSerializer.Serialize(new byte[] { 1, 2, 3 });
        Assert.Equal("\"AQID\"", MessagePackSerializer.ConvertToJson(payload));
    }

    [Fact]
    public void ConvertToJson_Timestamp_IsIso8601()
    {
        var payload = MessagePackSerializer.Serialize(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        var json = MessagePackSerializer.ConvertToJson(payload);
        Assert.Equal("\"2026-01-02T03:04:05Z\"", json);
    }

    [Fact]
    public void ConvertToJson_UnknownExt_IsAnnotatedObject()
    {
        // typeless ext(100): shown as {"$extension":100,"$data":"..."} rather than lost
        byte[] payload = [0xD4, 0x64, 0x01]; // fixext1, code 100, one byte
        var json = MessagePackSerializer.ConvertToJson(payload);
        Assert.Equal("""{"$extension":100,"$data":"AQ=="}""", json);
    }

    [Fact]
    public void ConvertToJson_IntegerMapKeys_AreStringified()
    {
        var payload = MessagePackSerializer.Serialize(new Dictionary<int, string> { [1] = "one", [-2] = "minus" });
        Assert.Equal("""{"1":"one","-2":"minus"}""", MessagePackSerializer.ConvertToJson(payload));
    }

    [Fact]
    public void ConvertToJson_NanAndInfinity_BecomeStrings()
    {
        Assert.Equal("\"NaN\"", MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(double.NaN)));
        Assert.Contains("Infinity", MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(double.PositiveInfinity)));
    }

    [Fact]
    public void ConvertToJson_LargeNumbers()
    {
        Assert.Equal(long.MaxValue.ToString(), MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(long.MaxValue)));
        Assert.Equal(ulong.MaxValue.ToString(), MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(ulong.MaxValue)));
        Assert.Equal(long.MinValue.ToString(), MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(long.MinValue)));
    }

    [Fact]
    public void ConvertToJson_ComposesWithSerialize()
    {
        var contractless = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [MessagePackFormatterFactory.Default.WithContractless()]));
        var json = MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(new JsonPerson { Id = 3, Name = "j" }, contractless));
        Assert.Equal("""{"Id":3,"Name":"j"}""", json);
    }

    public class JsonPerson
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [Fact]
    public void ConvertToJson_Truncated_Throws()
    {
        byte[] truncated = [0x92, 0x01]; // array2 with one element
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.ConvertToJson(truncated));
    }

    [Fact]
    public void ConvertFromJson_InvalidJson_ThrowsJsonException()
    {
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => MessagePackSerializer.ConvertFromJson("{broken"));
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => MessagePackSerializer.ConvertFromJson(ReadOnlySpan<char>.Empty));
    }

    [Fact]
    public void ConvertFromJson_CharSpanOverload()
    {
        ReadOnlySpan<char> json = """{"a":1,"日本語":"値"}""".AsSpan();
        var payload = MessagePackSerializer.ConvertFromJson(json);
        Assert.Equal("""{"a":1,"日本語":"値"}""", MessagePackSerializer.ConvertToJson(payload));
    }

    [Fact]
    public void ConvertFromJson_Utf8SpanOverload()
    {
        var payload = MessagePackSerializer.ConvertFromJson("""{"a":[1,null]}"""u8);
        Assert.Equal("""{"a":[1,null]}""", MessagePackSerializer.ConvertToJson(payload));
    }
}
