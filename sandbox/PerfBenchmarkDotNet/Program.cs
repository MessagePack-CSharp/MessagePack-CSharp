//extern alias oldmsgpack;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PerfBenchmarkDotNet
{
    internal class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.GitHub);
            Add(CsvMeasurementsExporter.Default);
            Add(MemoryDiagnoser.Default);

            // new BenchmarkDotNet.Diagnostics.Windows.MemoryDiagnoser()
            Add(Job.Clr/*, Job.Core*/);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            SetDefaultConfiguration();

            var switcher = new BenchmarkSwitcher(new[]
            {
                typeof(SerializeBenchmark)
            });

            switcher.Run(args);
        }

        static void SetDefaultConfiguration()
        {
            MsgPack.Serialization.SerializationContext.Default = new MsgPack.Serialization.SerializationContext
            {
                SerializationMethod = MsgPack.Serialization.SerializationMethod.Map
            };
        }
    }

    public class SerializerTarget
    {
        public int MyProperty1 { get; set; }
        public int MyProperty2 { get; set; }
        public int MyProperty3 { get; set; }
        public int MyProperty4 { get; set; }
        public int MyProperty5 { get; set; }
        public int MyProperty6 { get; set; }
        public int MyProperty7 { get; set; }
        public int MyProperty8 { get; set; }
        public int MyProperty9 { get; set; }
    }

    [Config(typeof(BenchmarkConfig))]
    public class SerializeBenchmark
    {
        readonly SerializerTarget target;
        public SerializeBenchmark()
        {
            target = new SerializerTarget
            {
                MyProperty1 = 1,
                MyProperty2 = 2,
                MyProperty3 = 3,
                MyProperty4 = 4,
                MyProperty5 = 5,
                MyProperty6 = 6,
                MyProperty7 = 7,
                MyProperty8 = 8,
                MyProperty9 = 9,
            };
        }

        [Benchmark]
        public void MsgPackCli()
        {
            var bytes = MsgPack.Serialization.MessagePackSerializer.Get<SerializerTarget>().PackSingleObject(target);
        }

        //[Benchmark]
        //public void MessagePack_1_3_0()
        //{
        //    var bytes = oldmsgpack::MessagePack.MessagePackSerializer.Serialize(target, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        //}

        //[Benchmark(Baseline = true)]
        //public void MessagePack_1_3_1()
        //{
        //    var bytes = MessagePack.MessagePackSerializer.Serialize(target, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        //}
    }
}
