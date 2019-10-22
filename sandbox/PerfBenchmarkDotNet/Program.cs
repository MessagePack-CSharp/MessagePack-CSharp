// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[]
            {
                ////typeof(TypelessSerializeBenchmark),
                ////typeof(TypelessDeserializeBenchmark),
                typeof(DeserializeBenchmark),
                typeof(SerializeBenchmark),
                typeof(DictionaryLookupCompare),
                typeof(StringKeyDeserializeCompare),
                typeof(NewVsOld),
                typeof(GuidImprov),
                typeof(ImproveStringKeySerializeBenchmark),
                typeof(MessagePackReaderBenchmark),
                typeof(MessagePackWriterBenchmark),
                typeof(SpanBenchmarks),
            });

            // args = new[] { "0" };
#if !DEBUG
            switcher.Run(args);
#else
            switcher.Run(args, new DebugInProcessConfig());
#endif
        }
    }
}
