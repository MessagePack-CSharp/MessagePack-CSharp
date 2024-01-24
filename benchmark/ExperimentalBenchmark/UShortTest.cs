﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias e;

namespace Benchmark;

public class UShortTest
{
#pragma warning disable SA1117
    [Params(
        "0",
        "1 rand", "1 127",
        "3 rand", "3 127",
        "8 rand", "8 127",
        "16 rand", "16 127",
        "31 rand", "31 127",
        "64 rand", "64 127",
        "4096 rand", "4096 127",
        "4194304 rand", "4194304 127")]
    public string Setting { get; set; } = string.Empty;
#pragma warning restore SA1117

    private ushort[] input = [];

    [GlobalSetup]
    public void SetUp()
    {
        var span = Setting.AsSpan();
        var firstSpace = span.IndexOf(' ');
        var sizeSpan = span;
        if (firstSpace >= 0)
        {
            sizeSpan = sizeSpan[..firstSpace];
        }

        var size = int.Parse(sizeSpan);
        input = size == 0 ? [] : new ushort[size];
        if (input.Length == 0)
        {
            return;
        }

        span = span[(firstSpace + 1)..];
        switch (span)
        {
            case "rand":
                Random.Shared.NextBytes(MemoryMarshal.AsBytes(input.AsSpan()));
                break;
            default:
                Array.Fill(input, ushort.Parse(span));
                break;
        }
    }

    [Benchmark(Baseline = true)]
    public ReadOnlyMemory<byte> Old()
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        MessagePackWriter writer = new(bufferWriter);
        UInt16ArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }

    [Benchmark]
    public ReadOnlyMemory<byte> Simd()
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        MessagePackWriter writer = new(bufferWriter);
        e::MessagePack.Formatters.UInt16ArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }
}