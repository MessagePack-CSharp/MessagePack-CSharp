// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias e;

namespace Benchmark;

public class BoolDeserializeTest
{
#pragma warning disable SA1117
    [Params(
        "0",
        "1 true", "1 false", "1 rand",
        "3 true", "3 false", "3 rand",
        "8 rand",
        "16 rand",
        "31 rand",
        "64 rand",
        "4096 rand",
        "4194304 rand")]
    public string Setting { get; set; } = string.Empty;
#pragma warning restore SA1117

    private byte[] binary = [];

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
        var input = size == 0 ? [] : new bool[size];
        if (input.Length != 0)
        {
            span = span[(firstSpace + 1)..];
            switch (span)
            {
                case "true":
                    Array.Fill(input, true);
                    break;
                case "false":
                    Array.Fill(input, false);
                    break;
                default:
                    foreach (ref var item in input.AsSpan())
                    {
                        item = (Random.Shared.Next() & 1) == 0;
                    }

                    break;
            }
        }

        binary = MessagePackSerializer.Serialize(input);
    }

    [Benchmark(Baseline = true)]
    public bool[]? Old()
    {
        MessagePackReader reader = new(binary);
        return BooleanArrayFormatter.Instance.Deserialize(ref reader, default!);
    }

    [Benchmark]
    public bool[]? Simd()
    {
        MessagePackReader reader = new(binary);
        return e::MessagePack.Formatters.BooleanArrayFormatter.Instance.Deserialize(ref reader, default!);
    }
}
