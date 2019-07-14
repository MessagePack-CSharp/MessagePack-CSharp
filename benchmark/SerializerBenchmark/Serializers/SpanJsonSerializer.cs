// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Benchmark.Serializers;

#pragma warning disable SA1649 // File name should match first type name

public class SpanJson_ : SerializerBase
{
    public override object Serialize<T>(T input)
    {
        return SpanJson.JsonSerializer.Generic.Utf8.Serialize(input);
    }

    public override T Deserialize<T>(object input)
    {
        return SpanJson.JsonSerializer.Generic.Utf8.Deserialize<T>((byte[])input);
    }
}
