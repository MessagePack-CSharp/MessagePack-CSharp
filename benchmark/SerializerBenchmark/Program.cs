// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Benchmark;
using Benchmark.Models;
using BenchmarkDotNet.Running;

namespace ConsoleApp1
{
    internal class Program
    {
        private static void Main(string[] args)
        {
#if !DEBUG
            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            BenchmarkRunner.Run<ShortRun_AllSerializerBenchmark_BytesInOut>();
#else
            BenchmarkRunner.Run<ShortRun_AllSerializerBenchmark_BytesInOut>(new BenchmarkDotNet.Configs.DebugInProcessConfig());
#endif
        }
    }
}
