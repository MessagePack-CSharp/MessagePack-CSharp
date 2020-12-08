// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Benchmark.Serializers;

#pragma warning disable SA1649 // File name should match first type name

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

    public override string ToString()
    {
        return "MsgPackCli";
    }
}
