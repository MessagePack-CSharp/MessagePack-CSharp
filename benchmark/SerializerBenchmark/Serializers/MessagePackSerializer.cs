extern alias oldmsgpack;
extern alias newmsgpack;

using Benchmark.Serializers;

public class MessagePack_v1 : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return oldmsgpack::MessagePack.MessagePackSerializer.Serialize<T>(input);
    }
}

public class MessagePack_v2 : SerializerBase
{
    private readonly newmsgpack::MessagePack.MessagePackSerializer serializer = new newmsgpack::MessagePack.MessagePackSerializer();

    public override T Deserialize<T>(object input)
    {
        return this.serializer.Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return this.serializer.Serialize<T>(input);
    }
}

public class MessagePackLz4_v1 : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return oldmsgpack::MessagePack.LZ4MessagePackSerializer.Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return oldmsgpack::MessagePack.LZ4MessagePackSerializer.Serialize<T>(input);
    }
}

public class MessagePackLz4_v2 : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return new newmsgpack::MessagePack.LZ4MessagePackSerializer().Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return new newmsgpack::MessagePack.LZ4MessagePackSerializer().Serialize<T>(input);
    }
}