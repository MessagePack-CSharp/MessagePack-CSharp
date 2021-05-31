// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Benchmark.Serializers;
using OdinSerializer;
using OdinSerializer.Utilities;

#pragma warning disable SA1649 // File name should match first type name

public class OdinSerializer_ : SerializerBase
{
    public override object Serialize<T>(T input)
    {
        using (var ctx = Cache<SerializationContext>.Claim())
        {
            ctx.Value.Config.SerializationPolicy = SerializationPolicies.Everything;
            return SerializationUtility.SerializeValue(input, DataFormat.Binary, ctx.Value);
        }
    }

    public override T Deserialize<T>(object input)
    {
        using (var ctx = Cache<DeserializationContext>.Claim())
        {
            ctx.Value.Config.SerializationPolicy = SerializationPolicies.Everything;
            return SerializationUtility.DeserializeValue<T>((byte[])input, DataFormat.Binary, ctx.Value);
        }
    }
}
