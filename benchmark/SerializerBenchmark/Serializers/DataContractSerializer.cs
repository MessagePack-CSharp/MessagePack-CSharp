// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.Serialization;
using Benchmark.Serializers;

#pragma warning disable SA1649 // File name should match first type name

public class DataContract_ : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        using (var ms = new MemoryStream((byte[])input))
        {
            return (T)new DataContractSerializer(typeof(T)).ReadObject(ms);
        }
    }

    public override object Serialize<T>(T input)
    {
        using (var ms = new MemoryStream())
        {
            new DataContractSerializer(typeof(T)).WriteObject(ms, input);
            ms.Flush();
            return ms.ToArray();
        }
    }

    public override string ToString()
    {
        return "DataContract";
    }
}
