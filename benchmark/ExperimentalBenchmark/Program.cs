// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Intrinsics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Benchmark;

internal class Program
{
    private static void Main(string[] args)
    {
        IConfig config = DefaultConfig.Instance
            .HideColumns(Column.EnvironmentVariables, Column.RatioSD, Column.Error)
            .AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(exportGithubMarkdown: true, printInstructionAddresses: false)))
            .AddJob(Job.Default.WithEnvironmentVariables([
                new("DOTNET_EnableHWIntrinsic", "0"),
                new("DOTNET_TieredPGO", "0")
            ]).WithId("Scalar").AsBaseline());

        if (Vector256.IsHardwareAccelerated)
        {
            config = config
                .AddJob(Job.Default.WithEnvironmentVariable(new("DOTNET_TieredPGO", "0")).WithId("Vector256"))
                .AddJob(Job.Default.WithEnvironmentVariables([
                    new("DOTNET_EnableAVX2", "0"),
                    new("DOTNET_TieredPGO", "0")
                ]).WithId("Vector128"));
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            config = config.AddJob(Job.Default.WithEnvironmentVariable(new("DOTNET_TieredPGO", "0")).WithId("Vector128"));
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args, config);
    }
}
