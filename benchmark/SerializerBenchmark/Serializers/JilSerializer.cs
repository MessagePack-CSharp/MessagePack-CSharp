using Benchmark.Serializers;
using Jil;
using System.Text;

public class Jil_ : SerializerBase
{
    public override object Serialize<T>(T input)
    {
        return Encoding.UTF8.GetBytes(Jil.JSON.Serialize(input, Options.ISO8601));
    }

    public override T Deserialize<T>(object input)
    {
        return Jil.JSON.Deserialize<T>(Encoding.UTF8.GetString((byte[])input), Options.ISO8601);
    }
}