// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Benchmark;
using BenchmarkDotNet.Running;

namespace HardwareIntrinsicsBenchmark
{
    internal class Program
    {
        private static void Main()
        {
            BenchmarkRunner.Run<BooleanArrayBenchmarkMessagePackNoSingleInstructionMultipleDataVsMessagePackSingleInstructionMultipleData>();
            BenchmarkRunner.Run<Int8ArrayBenchmarkMessagePackNoSingleInstructionMultipleDataVsMessagePackSingleInstructionMultipleData>();
            BenchmarkRunner.Run<Int16ArrayBenchmarkMessagePackNoSingleInstructionMultipleDataVsMessagePackSingleInstructionMultipleData>();
            BenchmarkRunner.Run<Int32ArrayBenchmarkMessagePackNoSingleInstructionMultipleDataVsMessagePackSingleInstructionMultipleData>();
            BenchmarkRunner.Run<SingleArrayBenchmarkMessagePackNoSingleInstructionMultipleDataVsMessagePackSingleInstructionMultipleData>();
            BenchmarkRunner.Run<DoubleArrayBenchmarkMessagePackNoSingleInstructionMultipleDataVsMessagePackSingleInstructionMultipleData>();
        }
    }
}
