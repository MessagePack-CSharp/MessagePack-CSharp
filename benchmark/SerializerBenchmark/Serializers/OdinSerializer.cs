// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias odin;

using Benchmark.Serializers;
using odin::OdinSerializer;

#pragma warning disable SA1649 // File name should match first type name

public class Odin_ : SerializerBase
{
    public override object Serialize<T>(T input)
    {
        return SerializationUtility.SerializeValue(input, DataFormat.Binary);
    }

    public override T Deserialize<T>(object input)
    {
        return SerializationUtility.DeserializeValue<T>((byte[])input, DataFormat.Binary);
    }
}
