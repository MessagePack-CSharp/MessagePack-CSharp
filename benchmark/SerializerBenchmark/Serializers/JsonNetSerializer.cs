using Benchmark.Serializers;
using Newtonsoft.Json;
using System.IO;
using System.Text;

public class JsonNet : SerializerBase
{
    static readonly JsonSerializer serializer = new JsonSerializer();

    public override T Deserialize<T>(object input)
    {
        using (var ms = new MemoryStream((byte[])input))
        using (var sr = new StreamReader(ms, Encoding.UTF8))
        using (var jr = new JsonTextReader(sr))
        {
            return serializer.Deserialize<T>(jr);
        }
    }

    public override object Serialize<T>(T input)
    {
        using (var ms = new MemoryStream())
        {
            using (var sw = new StreamWriter(ms, Encoding.UTF8))
            using (var jw = new JsonTextWriter(sw))
            {
                serializer.Serialize(jw, input);
            }
            ms.Flush();
            return ms.ToArray();
        }
    }
}
