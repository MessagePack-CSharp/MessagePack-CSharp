using Benchmark.Serializers;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
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
    }
}
