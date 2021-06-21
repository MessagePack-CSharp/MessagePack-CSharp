using Benchmark.Serializers;

public class MessagePack_v2 : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return MessagePack.MessagePackSerializer.Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return MessagePack.MessagePackSerializer.Serialize<T>(input);
    }
}

public class MessagePack_v3 : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return MessagePackv3.MessagePackSerializer.Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return MessagePackv3.MessagePackSerializer.Serialize<T>(input);
    }
}