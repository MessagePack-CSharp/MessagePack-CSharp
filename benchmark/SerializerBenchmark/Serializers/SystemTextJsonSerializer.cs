// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Benchmark.Serializers;

#pragma warning disable SA1649 // File name should match first type name

public class SystemTextJson : SerializerBase
{
    public override object Serialize<T>(T input)
    {
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(input);
    }

    public override T Deserialize<T>(object input)
    {
        var span = (byte[])input;
        return System.Text.Json.JsonSerializer.Deserialize<T>(span);
    }
}
