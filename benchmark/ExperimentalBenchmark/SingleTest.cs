// Copyright (c) All contributors. All rights reserved.
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

        var r = new Random();
        for (var i = 0; i < input.Length; i++)
        {
            input[i] = r.NextSingle();
        }
    }

    [Benchmark]
    public ReadOnlyMemory<byte> Simd()
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(bufferWriter);
        e::MessagePack.Formatters.DoubleArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }

    [Benchmark]
    public ReadOnlyMemory<byte> Old()
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(bufferWriter);
        DoubleArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }
}
