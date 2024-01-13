// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using MessagePack;
using MessagePack.Formatters;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Benchmark;

[MessagePackObject]
public struct OldTypeByte(List<byte> value)
{
    public OldTypeByte()
        : this([])
    {
    }

    [Key(0)]
    [MessagePackFormatter(typeof(OldListFormatter<byte>))]
    public List<byte> Value = value;
}

[MessagePackObject]
public struct NewTypeByte(List<byte> value)
{
    public NewTypeByte()
        : this([])
    {
    }

    [Key(0)]
    [MessagePackFormatter(typeof(ListFormatter<byte>))]
    public List<byte> Value = value;
}

public class RandomByteBenchmark
{
    [Params(1, 64, 1024, 16 * 1024 * 1024)]
    public int Size { get; set; }

    private List<byte> input = [];
    private byte[] serialized = [];

    [GlobalSetup]
    public void SetUp()
    {
        input = new(Size);
        for (var i = 0; i < Size; i++)
        {
            input.Add((byte)Random.Shared.Next());
        }

        serialized = MessagePackSerializer.Serialize(new NewTypeByte(input));
    }

    [Benchmark]
    public byte[] SerializeNew()
    {
        return MessagePackSerializer.Serialize(new NewTypeByte(input));
    }

    [Benchmark]
    public byte[] SerializeOld()
    {
        return MessagePackSerializer.Serialize(new OldTypeByte(input));
    }

    [Benchmark]
    public List<byte> DeserializeNew()
    {
        return MessagePackSerializer.Deserialize<NewTypeByte>(serialized).Value;
    }

    [Benchmark]
    public List<byte> DeserializeOld()
    {
        return MessagePackSerializer.Deserialize<OldTypeByte>(serialized).Value;
    }
}
