using Benchmark.Serializers;
using MBrace.FsPickler;
using System.IO;

public class FsPickler_ : SerializerBase
{
    static readonly BinarySerializer serializer = MBrace.FsPickler.FsPickler.CreateBinarySerializer();

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
            serializer.Serialize<T>(ms, input);
            ms.Flush();
            return ms.ToArray();
        }
    }
}
