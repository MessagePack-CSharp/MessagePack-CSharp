// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias e;

namespace Benchmark;

public class SByteTest
{
    [Params(0, 1, 2, 3, 4, 8, 16, 64, 1024, 16 * 1024 * 1024)]
    public int Size { get; set; }

    [Params((sbyte)0, (sbyte)-32, (sbyte)-33, (sbyte)-1)]
    public sbyte Value { get; set; }

    private sbyte[] input = [];

    [GlobalSetup]
    public void SetUp()
    {
        input = new sbyte[Size];
        switch (Value)
        {
            case 0:
                break;
            case -1:
                Random.Shared.NextBytes(MemoryMarshal.AsBytes(input.AsSpan()));
                break;
            default:
                Array.Fill(input, Value);
                break;
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
        e::MessagePack.Formatters.SByteArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }

    [Benchmark]
    public ReadOnlyMemory<byte> Old()
    {
        var writer = new MessagePackWriter(bufferWriter);
        SByteArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }
}
