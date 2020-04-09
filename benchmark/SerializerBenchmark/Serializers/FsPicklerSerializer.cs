// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Benchmark.Serializers;
using MBrace.FsPickler;

#pragma warning disable SA1649 // File name should match first type name

public class FsPickler_ : SerializerBase
{
    private static readonly BinarySerializer Serializer = MBrace.FsPickler.FsPickler.CreateBinarySerializer();

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
            Serializer.Serialize<T>(ms, input);
            ms.Flush();
            return ms.ToArray();
        }
    }
}
