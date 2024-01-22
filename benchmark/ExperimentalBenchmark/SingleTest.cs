﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias e;

namespace Benchmark;

public class SingleTest
{
    [Params(0, 1, 3, 4, 8, 64, 1024, 16 * 1024 * 1024)]
    public int Size { get; set; }

    private float[] input = [];

    [GlobalSetup]
    public void SetUp()
    {
        input = new float[Size];
        for (var i = 0; i < input.Length; i++)
        {
            input[i] = Random.Shared.NextSingle();
        }
    }

    private ArrayBufferWriter<byte> bufferWriter = default!;

    [IterationSetup]
    public void IterationSetUp()
    {
        bufferWriter = new();
    }

    [Benchmark]
    public ReadOnlyMemory<byte> Simd()
    {
        var writer = new MessagePackWriter(bufferWriter);
        e::MessagePack.Formatters.SingleArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }

    [Benchmark]
    public ReadOnlyMemory<byte> Old()
    {
        var writer = new MessagePackWriter(bufferWriter);
        SingleArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }
}
