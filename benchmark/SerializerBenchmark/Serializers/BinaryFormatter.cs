using Benchmark.Serializers;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class BinaryFormatter_ : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        using (var ms = new MemoryStream((byte[])input))
        {
            return (T)new BinaryFormatter().Deserialize(ms);
        }
    }

    public override object Serialize<T>(T input)
    {
        using (var ms = new MemoryStream())
        {
            new BinaryFormatter().Serialize(ms, input);
            ms.Flush();
            return ms.ToArray();
        }
    }
}
