extern alias oldmsgpack;
extern alias newmsgpack;

using Benchmark.Serializers;

public class MsgPack_v1 : SerializerBase
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

public class MsgPack_v2 : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Serialize<T>(input);
    }
}

public class MsgPackLz4_v1 : SerializerBase
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

public class MsgPackLz4_v2 : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return newmsgpack::MessagePack.LZ4MessagePackSerializer.Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return newmsgpack::MessagePack.LZ4MessagePackSerializer.Serialize<T>(input);
    }
}