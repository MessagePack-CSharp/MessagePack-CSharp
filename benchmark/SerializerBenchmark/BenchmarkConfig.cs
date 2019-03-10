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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchmark
{
    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            // run quickly:)
            var baseConfig = Job.ShortRun.WithIterationCount(1).WithWarmupCount(1);

            // Add(baseConfig.With(Runtime.Clr).With(Jit.RyuJit).With(Platform.X64));
            Add(baseConfig.With(Runtime.Core).With(Jit.RyuJit).With(Platform.X64));

            Add(MarkdownExporter.GitHub);
            Add(CsvExporter.Default);
            Add(MemoryDiagnoser.Default);

            Add(new DataSizeColumn());

            this.Set(new CustomOrderer());
        }

        // 0.11.4 has bug of set CustomOrderer https://github.com/dotnet/BenchmarkDotNet/issues/1070
        // so skip update to 0.11.4.

        public class CustomOrderer : IOrderer
        {
            public bool SeparateLogicalGroups => false;

            public IEnumerable<BenchmarkCase> GetExecutionOrder(BenchmarkCase[] benchmarksCase)
            {
                return benchmarksCase;
            }

            public string GetHighlightGroupKey(BenchmarkCase benchmarkCase)
            {
                return benchmarkCase.Descriptor.MethodIndex.ToString();
            }

            public string GetLogicalGroupKey(IConfig config, BenchmarkCase[] allBenchmarksCases, BenchmarkCase benchmarkCase)
            {
                return null;
            }

            public IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups)
            {
                return logicalGroups;
            }

            public IEnumerable<BenchmarkCase> GetSummaryOrder(BenchmarkCase[] benchmarksCases, Summary summary)
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
                return GetValue(summary, benchmarkCase, null);
            }

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style)
            {
                var mi = benchmarkCase.Descriptor.WorkloadMethod;
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

            static string ToHumanReadableSize(long size)
            {
                return ToHumanReadableSize(new Nullable<long>(size));
            }

            static string ToHumanReadableSize(long? size)
            {
                if (size == null) return "NULL";

                double bytes = size.Value;

                if (bytes <= 1024) return bytes.ToString("f2") + " B";

                bytes = bytes / 1024;
                if (bytes <= 1024) return bytes.ToString("f2") + " KB";

                bytes = bytes / 1024;
                if (bytes <= 1024) return bytes.ToString("f2") + " MB";

                bytes = bytes / 1024;
                if (bytes <= 1024) return bytes.ToString("f2") + " GB";

                bytes = bytes / 1024;
                if (bytes <= 1024) return bytes.ToString("f2") + " TB";

                bytes = bytes / 1024;
                if (bytes <= 1024) return bytes.ToString("f2") + " PB";

                bytes = bytes / 1024;
                if (bytes <= 1024) return bytes.ToString("f2") + " EB";

                bytes = bytes / 1024;
                return bytes + " ZB";
            }
        }
    }
}
