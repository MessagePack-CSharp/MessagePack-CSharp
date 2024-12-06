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

[MessagePack.MessagePackObjectAttribute]
public struct Matrix4x4
{
    [MessagePack.Key(0)] public float M11;
    [MessagePack.Key(1)] public float M12;
    [MessagePack.Key(2)] public float M13;
    [MessagePack.Key(3)] public float M14;
    [MessagePack.Key(4)] public float M21;
    [MessagePack.Key(5)] public float M22;
    [MessagePack.Key(6)] public float M23;
    [MessagePack.Key(7)] public float M24;
    [MessagePack.Key(8)] public float M31;
    [MessagePack.Key(9)] public float M32;
    [MessagePack.Key(10)] public float M33;
    [MessagePack.Key(11)] public float M34;
    [MessagePack.Key(12)] public float M41;
    [MessagePack.Key(13)] public float M42;
    [MessagePack.Key(14)] public float M43;
    [MessagePack.Key(15)] public float M44;

    public Matrix4x4(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24, float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
    {
        M11 = m11;
        M12 = m12;
        M13 = m13;
        M14 = m14;
        M21 = m21;
        M22 = m22;
        M23 = m23;
        M24 = m24;
        M31 = m31;
        M32 = m32;
        M33 = m33;
        M34 = m34;
        M41 = m41;
        M42 = m42;
        M43 = m43;
        M44 = m44;
    }
}

[MessagePackObject]
public struct OldTypeMatrix4x4(List<Matrix4x4> value)
{
    public OldTypeMatrix4x4()
        : this([])
    {
    }

    [Key(0)]
    [MessagePackFormatter(typeof(OldListFormatter<Matrix4x4>))]
    public List<Matrix4x4> Value = value;
}

[MessagePackObject]
public struct NewTypeMatrix4x4(List<Matrix4x4> value)
{
    public NewTypeMatrix4x4()
        : this([])
    {
    }

    [Key(0)]
    [MessagePackFormatter(typeof(ListFormatter<Matrix4x4>))]
    public List<Matrix4x4> Value = value;
}

public class RandomMatrix4x4Benchmark
{
    [Params(1, 64, 1024, 16 * 1024 * 1024)]
    public int Size { get; set; }

    private List<Matrix4x4> input = [];
    private byte[] serialized = [];

    [GlobalSetup]
    public void SetUp()
    {
        input = new(Size);
        var r = new Random();
        for (var i = 0; i < Size; i++)
        {
            input.Add(new((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble()));
        }

        serialized = MessagePackSerializer.Serialize(new NewTypeMatrix4x4(input));
    }

    [Benchmark]
    public byte[] SerializeNew()
    {
        return MessagePackSerializer.Serialize(new NewTypeMatrix4x4(input));
    }

    [Benchmark]
    public byte[] SerializeOld()
    {
        return MessagePackSerializer.Serialize(new OldTypeMatrix4x4(input));
    }

    [Benchmark]
    public List<Matrix4x4> DeserializeNew()
    {
        return MessagePackSerializer.Deserialize<NewTypeMatrix4x4>(serialized).Value;
    }

    [Benchmark]
    public List<Matrix4x4> DeserializeOld()
    {
        return MessagePackSerializer.Deserialize<OldTypeMatrix4x4>(serialized).Value;
    }
}
