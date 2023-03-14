// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    internal class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            this.AddExporter(MarkdownExporter.GitHub);
            this.AddDiagnoser(MemoryDiagnoser.Default);

            this.AddJob(Job.ShortRun.WithPlatform(BenchmarkDotNet.Environments.Platform.X64).WithWarmupCount(1).WithIterationCount(1));

            ////Add(Job.ShortRun.With(BenchmarkDotNet.Environments.Platform.X64).WithWarmupCount(1).WithIterationCount(1),
            ////Job.ShortRun.With(BenchmarkDotNet.Environments.Platform.X86).WithWarmupCount(1).WithIterationCount(1));
        }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

