// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias e;

namespace Benchmark;

public class LongTest
{
    [Params(0, 1, 2, 3, 4, 8, 16, 64, 1024, 16 * 1024 * 1024)]
    public int Size { get; set; }

    [Params(0L, -1L, long.MinValue, (long)int.MinValue, (long)uint.MaxValue)]
    public long Value { get; set; }

    private long[] input = [];

    [GlobalSetup]
    public void SetUp()
    {
        input = new long[Size];
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

    [Benchmark]
    public ReadOnlyMemory<byte> Simd()
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        MessagePackWriter writer = new(bufferWriter);
        e::MessagePack.Formatters.Int64ArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }

    [Benchmark]
    public ReadOnlyMemory<byte> Old()
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        MessagePackWriter writer = new(bufferWriter);
        Int64ArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }
}
