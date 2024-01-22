// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Intrinsics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;

namespace Benchmark;

internal class Program
{
    private static void Main(string[] args)
    {
        Job enough = Job.Default
            .WithWarmupCount(1)
            .WithIterationTime(TimeInterval.FromSeconds(0.25))
            .WithMaxIterationCount(20);

        IConfig config = DefaultConfig.Instance
            .HideColumns(Column.EnvironmentVariables, Column.RatioSD, Column.Error)
            .AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(exportGithubMarkdown: true, printInstructionAddresses: false)))
            .AddJob(enough.WithEnvironmentVariable("DOTNET_EnableHWIntrinsic", "0").WithId("Scalar").AsBaseline());

        if (Vector256.IsHardwareAccelerated)
        {
            config = config
                .AddJob(enough.WithId("Vector256"))
                .AddJob(enough.WithEnvironmentVariable("DOTNET_EnableAVX2", "0").WithId("Vector128"));
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            config = config.AddJob(enough.WithId("Vector128"));
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args, config);
    }
}
