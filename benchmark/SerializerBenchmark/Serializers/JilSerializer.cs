// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Benchmark.Serializers;
using Jil;

#pragma warning disable SA1649 // File name should match first type name

public class Jil_ : SerializerBase
{
    public override object Serialize<T>(T input)
    {
        return Encoding.UTF8.GetBytes(Jil.JSON.Serialize(input, Options.ISO8601));
    }

    public override T Deserialize<T>(object input)
    {
        return Jil.JSON.Deserialize<T>(Encoding.UTF8.GetString((byte[])input), Options.ISO8601);
    }
}
