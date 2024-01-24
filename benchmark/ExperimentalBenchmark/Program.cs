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
        var noDynamicPGO = new EnvironmentVariable("DOTNET_TieredPGO", "0");
#pragma warning disable SA1515 // Single-line comment should be preceded by blank line
        IConfig config = DefaultConfig.Instance
            .HideColumns(Column.EnvironmentVariables, Column.RatioSD, Column.Error)
            // .AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(exportGithubMarkdown: true, printInstructionAddresses: false)))
            .AddJob(Job.Default.WithEnvironmentVariables([
                new("DOTNET_EnableHWIntrinsic", "0"),
                noDynamicPGO
            ]).WithId("Scalar").AsBaseline());
#pragma warning restore SA1515 // Single-line comment should be preceded by blank line

        // Currently Vector256 Intrinsics is not used so no need to test.
        // if (Vector256.IsHardwareAccelerated)
        // {
        //     config = config
        //         .AddJob(Job.Default.WithEnvironmentVariable(noDynamicPGO).WithId("Vector256"))
        //         .AddJob(Job.Default.WithEnvironmentVariables([
        //             new("DOTNET_EnableAVX2", "0"),
        //             noDynamicPGO
        //         ]).WithId("Vector128"));
        // }
        // else
        if (Vector128.IsHardwareAccelerated)
        {
            config = config.AddJob(Job.Default.WithEnvironmentVariable(noDynamicPGO).WithId("Vector"));
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args, config);
    }
}
