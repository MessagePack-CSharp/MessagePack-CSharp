extern alias oldmsgpack;
extern alias newmsgpack;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using MsgPack.Serialization;
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
            Add(MemoryDiagnoser.Default);
            Add(Job.ShortRun);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[]
            {
                typeof(MapDeserializeBenchmark)
            });

            args = new[] { "0" };
            switcher.Run(args);
        }
    }

    [oldmsgpack::MessagePack.MessagePackObject(true)]
    [newmsgpack::MessagePack.MessagePackObject(true)]
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
    public class MapDeserializeBenchmark
    {

        public readonly SerializerTarget target;
        public readonly SerializationContext context;
        public readonly byte[] byteA;
        public readonly byte[] byteB;
        public readonly byte[] byteC;

        public MapDeserializeBenchmark()
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

            context = new MsgPack.Serialization.SerializationContext
            {
                SerializationMethod = MsgPack.Serialization.SerializationMethod.Map
            };

            byteA = context.GetSerializer<SerializerTarget>().PackSingleObject(target);
            byteB = oldmsgpack::MessagePack.MessagePackSerializer.Serialize(target);
            byteC = newmsgpack::MessagePack.MessagePackSerializer.Serialize(target);
        }

        [Benchmark]
        public SerializerTarget MsgPackCli()
        {
            return context.GetSerializer<SerializerTarget>().UnpackSingleObject(byteA);
        }

        [Benchmark]
        public SerializerTarget MessagePack_1_4_3()
        {
            return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<SerializerTarget>(byteB);
        }

        [Benchmark(Baseline = true)]
        public SerializerTarget MessagePack_1_4_4()
        {
            return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<SerializerTarget>(byteC);
        }
    }
}
