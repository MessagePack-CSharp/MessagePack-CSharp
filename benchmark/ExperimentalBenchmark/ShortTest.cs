// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias e;

namespace Benchmark;

public class ShortTest
{
    [Params(0, 1, 2, 3, 4, 8, 16, 64, 1024, 16 * 1024 * 1024)]
    public int Size { get; set; }

    private short[] input = [];

    [GlobalSetup]
    public void SetUp()
    {
        input = new short[Size];
        Random.Shared.NextBytes(MemoryMarshal.AsBytes(input.AsSpan()));
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
        e::MessagePack.Formatters.Int16ArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }

    [Benchmark]
    public ReadOnlyMemory<byte> Old()
    {
        var writer = new MessagePackWriter(bufferWriter);
        Int16ArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }
}
