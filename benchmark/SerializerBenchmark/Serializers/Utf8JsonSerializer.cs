// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Benchmark.Serializers;

#pragma warning disable SA1649 // File name should match first type name

public class Utf8Json_ : SerializerBase
{
    public override object Serialize<T>(T input)
    {
        return Utf8Json.JsonSerializer.Serialize(input);
    }

    public override T Deserialize<T>(object input)
    {
        return Utf8Json.JsonSerializer.Deserialize<T>((byte[])input);

        public override string ToString()
        {
            return "UTF8Json";
        }
    }
}
