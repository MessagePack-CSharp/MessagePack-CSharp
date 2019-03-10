using Benchmark.Serializers;
using ProtoBuf;
using System.IO;

public class Protobuf : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        using (var ms = new MemoryStream((byte[])input))
        {
            return Serializer.Deserialize<T>(ms);
        }
    }

    public override object Serialize<T>(T input)
    {
        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, input);
            return ms.ToArray();
        }
    }
}
