﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Benchmark.Serializers;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

#pragma warning disable SA1649 // File name should match first type name

public class BsonNet : SerializerBase
{
    private static readonly JsonSerializer Serializer = new JsonSerializer();

    public override T Deserialize<T>(object input)
    {
        using (var ms = new MemoryStream((byte[])input))
        using (var jr = new BsonDataReader(ms))
        {
            return Serializer.Deserialize<T>(jr);
        }
    }

    public override object Serialize<T>(T input)
    {
        object value = input;
        if (typeof(T).IsValueType)
        {
            value = new[] { input };
        }

        using (var ms = new MemoryStream())
        {
            using (var jw = new BsonDataWriter(ms))
            {
                Serializer.Serialize(jw, value);
            }

            ms.Flush();
            return ms.ToArray();
        }
    }
}
