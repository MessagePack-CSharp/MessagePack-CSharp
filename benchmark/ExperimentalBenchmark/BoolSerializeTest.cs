// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias e;

namespace Benchmark;

public class BoolSerializeTest
{
    [Params(64, 1024, 16 * 1024 * 1024)]
    public int Size { get; set; }

    [Params(true, false)]
    public bool Canonical { get; set; }

    private bool[] input = [];

    [GlobalSetup]
    public void SetUp()
    {
        input = new bool[Size];
        var span = MemoryMarshal.AsBytes(input.AsSpan());
        Random.Shared.NextBytes(span);
        foreach (ref var item in span)
        {
            if (Canonical)
            {
                item = ((item & 1) == 1) ? (byte)1 : (byte)0;
            }
            else
            {
                if ((item & 1) == 0)
                {
                    item = default;
                }
            }
        }
    }

    [Benchmark]
    public ReadOnlyMemory<byte> Simd()
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        MessagePackWriter writer = new(bufferWriter);
        e::MessagePack.Formatters.BooleanArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }

    [Benchmark]
    public ReadOnlyMemory<byte> Old()
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        MessagePackWriter writer = new(bufferWriter);
        BooleanArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }
}
