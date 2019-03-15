using Benchmark.Serializers;

public class MsgPackCli : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return MsgPack.Serialization.MessagePackSerializer.Get<T>().UnpackSingleObject((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return MsgPack.Serialization.MessagePackSerializer.Get<T>().PackSingleObject(input);
    }
}
