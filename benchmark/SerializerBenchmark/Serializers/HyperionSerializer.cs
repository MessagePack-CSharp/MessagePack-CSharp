using Benchmark.Serializers;
using Hyperion;
using System.IO;

public class Hyperion_ : SerializerBase
{
    static readonly Serializer serializer = new Hyperion.Serializer();

    public override T Deserialize<T>(object input)
    {
        using (var ms = new MemoryStream((byte[])input))
        {
            return serializer.Deserialize<T>(ms);
        }
    }

    public override object Serialize<T>(T input)
    {
        using (var ms = new MemoryStream())
        {
            serializer.Serialize(input, ms);
            ms.Flush();
            return ms.ToArray();
        }
    }
}
