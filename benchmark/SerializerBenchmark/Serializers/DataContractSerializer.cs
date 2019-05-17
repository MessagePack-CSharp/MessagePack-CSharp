using Benchmark.Serializers;
using System.IO;
using System.Runtime.Serialization;

public class DataContract_ : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        using (var ms = new MemoryStream((byte[])input))
        {
            return (T)new DataContractSerializer(typeof(T)).ReadObject(ms);
        }
    }

    public override object Serialize<T>(T input)
    {
        using (var ms = new MemoryStream())
        {
            new DataContractSerializer(typeof(T)).WriteObject(ms, input);
            ms.Flush();
            return ms.ToArray();
        }
    }
}
