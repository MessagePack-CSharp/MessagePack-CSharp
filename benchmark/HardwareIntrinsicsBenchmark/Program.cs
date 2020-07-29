// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Benchmark;
using BenchmarkDotNet.Running;

namespace HardwareIntrinsicsBenchmark
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<StringBenchmark_MessagePackNoSimd_Vs_MessagePackSimd>();
        }
    }
}
