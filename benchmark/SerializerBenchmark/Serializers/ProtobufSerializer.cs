// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Benchmark.Serializers;
using ProtoBuf;

public class
#pragma warning disable SA1649 // File name should match first type name

ProtobufNet : SerializerBase
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

    public override string ToString()
    {
        return "ProtobufNet";
    }
    }
}
