// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias e;

namespace Benchmark;

public class DateTimeTest
{
#pragma warning disable SA1117
    [Params(
        "0",
        "1 rand", "1 utc",
        "3 rand", "3 utc",
        "8 rand", "8 utc",
        "16 rand", "16 utc",
        "31 rand", "31 utc",
        "64 rand", "64 utc",
        "4096 rand", "4096 utc",
        "4194304 rand", "4194304 utc")]
    public string Setting { get; set; } = string.Empty;
#pragma warning restore SA1117

    private DateTime[] input = [];

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
        input = size == 0 ? [] : new DateTime[size];
        if (input.Length == 0)
        {
            return;
        }

        span = span[(firstSpace + 1)..];

        switch (span)
        {
            case "utc":
                foreach (ref var item in input.AsSpan())
                {
                    item = DateTime.UnixEpoch.AddSeconds((Random.Shared.NextDouble() - 0.5) * 315537897d);
                }

                break;
            case "utc_around_now":
                foreach (ref var item in input.AsSpan())
                {
                    item = DateTime.UtcNow.AddDays((Random.Shared.NextDouble() - 0.5) * 1000).AddSeconds((Random.Shared.NextDouble() - 0.5) * 10000);
                }

                break;
            case "rand":
                foreach (ref var item in input.AsSpan())
                {
                    item = DateTime.UnixEpoch.AddSeconds((Random.Shared.NextDouble() - 0.5) * 315537897d);
                    if (Random.Shared.Next(0, 2) == 0)
                    {
                        item = item.ToLocalTime();
                    }
                }

                break;
            default:
                foreach (ref var item in input.AsSpan())
                {
                    item = DateTime.UtcNow.AddDays((Random.Shared.NextDouble() - 0.5) * 1000).AddSeconds((Random.Shared.NextDouble() - 0.5) * 10000);
                    if (Random.Shared.Next(0, 2) == 0)
                    {
                        item = item.ToLocalTime();
                    }
                }

                break;
        }
    }

    [Benchmark(Baseline = true)]
    public ReadOnlyMemory<byte> Old()
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        MessagePackWriter writer = new(bufferWriter);
        DateTimeArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }

    [Benchmark]
    public ReadOnlyMemory<byte> Simd()
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        MessagePackWriter writer = new(bufferWriter);
        e::MessagePack.Formatters.DateTimeArrayFormatter.Instance.Serialize(ref writer, input, default!);
        writer.Flush();
        return bufferWriter.WrittenMemory;
    }
}
