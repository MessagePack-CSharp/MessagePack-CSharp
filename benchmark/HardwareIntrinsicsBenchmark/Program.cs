// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Benchmark;
using BenchmarkDotNet.Running;

namespace HardwareIntrinsicsBenchmark
{
    internal class Program
    {
        private static void Main()
        {
            BenchmarkRunner.Run<BooleanArrayBenchmarkMessagePackNoSimdVsMessagePackSimd>();
            BenchmarkRunner.Run<Int8ArrayBenchmarkMessagePackNoSimdVsMessagePackSimd>();
            BenchmarkRunner.Run<Int16ArrayBenchmarkMessagePackNoSimdVsMessagePackSimd>();
            BenchmarkRunner.Run<Int32ArrayBenchmarkMessagePackNoSimdVsMessagePackSimd>();
            BenchmarkRunner.Run<SingleArrayBenchmarkMessagePackNoSimdVsMessagePackSimd>();
        }
    }
}
