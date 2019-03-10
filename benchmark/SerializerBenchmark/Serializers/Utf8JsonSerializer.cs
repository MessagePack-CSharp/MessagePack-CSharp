using Benchmark.Serializers;

public class Utf8Json_ : SerializerBase
{
    public override object Serialize<T>(T input)
    {
        return Utf8Json.JsonSerializer.Serialize(input);
    }

    public override T Deserialize<T>(object input)
    {
        return Utf8Json.JsonSerializer.Deserialize<T>((byte[])input);
    }
}
