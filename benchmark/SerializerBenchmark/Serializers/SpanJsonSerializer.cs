using Benchmark.Serializers;

public class SpanJson_ : SerializerBase
{
    public override object Serialize<T>(T input)
    {
        return SpanJson.JsonSerializer.Generic.Utf8.Serialize(input);
    }

    public override T Deserialize<T>(object input)
    {
        return SpanJson.JsonSerializer.Generic.Utf8.Deserialize<T>((byte[])input);
    }
}
