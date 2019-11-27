// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using Benchmark.Serializers;

#pragma warning disable SA1649 // File name should match first type name

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
    public override T Deserialize<T>(object input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Serialize<T>(input);
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
    private static readonly newmsgpack::MessagePack.MessagePackSerializerOptions LZ4Standard = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithCompression(newmsgpack::MessagePack.MessagePackCompression.LZ4Block);

    public override T Deserialize<T>(object input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<T>((byte[])input, LZ4Standard);
    }

    public override object Serialize<T>(T input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Serialize<T>(input, LZ4Standard);
    }
}
