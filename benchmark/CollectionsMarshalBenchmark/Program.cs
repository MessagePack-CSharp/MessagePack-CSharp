// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Benchmark;
using BenchmarkDotNet.Running;

namespace CollectionsMarshalBenchmark;

internal class Program
{
    private static void Main()
    {
        BenchmarkRunner.Run<RandomByteBenchmark>();
        BenchmarkRunner.Run<RandomMatrix4x4Benchmark>();
    }
}
