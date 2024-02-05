// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
#else
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#endif
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Benchmark;

internal class Program
{
    private static void Main(string[] args)
    {
        var noDynamicPGO = new EnvironmentVariable("DOTNET_TieredPGO", "0");
        IConfig config = DefaultConfig.Instance
            .HideColumns(Column.EnvironmentVariables, Column.RatioSD, Column.Error)
            .AddJob(Job.Default.WithEnvironmentVariables([
                new("DOTNET_EnableHWIntrinsic", "0"),
                noDynamicPGO
            ]).WithId("Scalar").AsBaseline());

#if NET7_0_OR_GREATER
        if (Vector128.IsHardwareAccelerated)
#else
        if (Sse42.IsSupported || AdvSimd.IsSupported)
#endif
        {
            config = config.AddJob(Job.Default.WithEnvironmentVariable(noDynamicPGO).WithId("Vector"));
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args, config);
    }
}
