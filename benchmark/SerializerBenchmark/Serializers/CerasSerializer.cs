using Benchmark.Serializers;

public class Ceras_ : SerializerBase
{
    Ceras.CerasSerializer ceras = new Ceras.CerasSerializer();

    public override T Deserialize<T>(object input)
    {
        return ceras.Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return ceras.Serialize(input);
    }
}
