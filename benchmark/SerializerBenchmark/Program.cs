﻿using Benchmark;
using Benchmark.Models;
using BenchmarkDotNet.Running;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            //BenchmarkRunner.Run<ShortRun_AllSerializerBenchmark_BytesInOut>();
        }
    }
}
