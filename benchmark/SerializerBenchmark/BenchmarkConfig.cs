// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Benchmark.Serializers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Benchmark
{
    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            // run quickly:)
            Job baseConfig = Job.ShortRun.WithIterationCount(1).WithWarmupCount(1);

            // Add(baseConfig.With(Runtime.Clr).With(Jit.RyuJit).With(Platform.X64));
            this.Add(baseConfig.With(CoreRuntime.Core30).With(Jit.RyuJit).With(Platform.X64));

            this.Add(MarkdownExporter.GitHub);
            this.Add(CsvExporter.Default);
            this.Add(MemoryDiagnoser.Default);

            this.Add(new DataSizeColumn());

            this.Orderer = new CustomOrderer();
        }

        public class CustomOrderer : IOrderer
        {
            public bool SeparateLogicalGroups => false;

            public IEnumerable<BenchmarkCase> GetExecutionOrder(ImmutableArray<BenchmarkCase> benchmarksCase)
            {
                return benchmarksCase;
            }

            public string GetHighlightGroupKey(BenchmarkCase benchmarkCase)
            {
                return benchmarkCase.Descriptor.MethodIndex.ToString();
            }

            public string GetLogicalGroupKey(ImmutableArray<BenchmarkCase> allBenchmarksCases, BenchmarkCase benchmarkCase)
            {
                return null;
            }

            public IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups)
            {
                return logicalGroups;
            }

            public IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> benchmarksCases, Summary summary)
            {
                return benchmarksCases
                    .OrderBy(x => x.Descriptor.WorkloadMethod.Name)
                    .ThenBy(x => x.Parameters.Items.Select(y => y.Value).OfType<SerializerBase>().First().GetType().Name);
            }
        }

        public class DataSizeColumn : IColumn
        {
            public string Id => "DataSize";

            public string ColumnName => "DataSize";

            public bool AlwaysShow => true;

            public ColumnCategory Category => ColumnCategory.Custom;

            public int PriorityInCategory => int.MaxValue;

            public bool IsNumeric => true;

            public UnitType UnitType => UnitType.Size;

            public string Legend => null;

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
            {
                return this.GetValue(summary, benchmarkCase, null);
            }

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
            {
                System.Reflection.MethodInfo mi = benchmarkCase.Descriptor.WorkloadMethod;
                if (mi.Name.Contains("Serialize"))
                {
                    var instance = Activator.CreateInstance(mi.DeclaringType);
                    mi.DeclaringType.GetField("Serializer").SetValue(instance, benchmarkCase.Parameters[0].Value);

                    var bytes = (byte[])mi.Invoke(instance, null);
                    return ToHumanReadableSize(bytes.Length);
                }
                else
                {
                    return "-";
                }
            }

            public bool IsAvailable(Summary summary)
            {
                return true;
            }

            public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase)
            {
                return false;
            }

            private static string ToHumanReadableSize(long size)
            {
                return ToHumanReadableSize(new long?(size));
            }

            private static string ToHumanReadableSize(long? size)
            {
                if (size == null)
                {
                    return "NULL";
                }

                double bytes = size.Value;

                if (bytes <= 1024)
                {
                    return bytes.ToString("f2") + " B";
                }

                bytes = bytes / 1024;
                if (bytes <= 1024)
                {
                    return bytes.ToString("f2") + " KB";
                }

                bytes = bytes / 1024;
                if (bytes <= 1024)
                {
                    return bytes.ToString("f2") + " MB";
                }

                bytes = bytes / 1024;
                if (bytes <= 1024)
                {
                    return bytes.ToString("f2") + " GB";
                }

                bytes = bytes / 1024;
                if (bytes <= 1024)
                {
                    return bytes.ToString("f2") + " TB";
                }

                bytes = bytes / 1024;
                if (bytes <= 1024)
                {
                    return bytes.ToString("f2") + " PB";
                }

                bytes = bytes / 1024;
                if (bytes <= 1024)
                {
                    return bytes.ToString("f2") + " EB";
                }

                bytes = bytes / 1024;
                return bytes + " ZB";
            }
        }
    }
}
