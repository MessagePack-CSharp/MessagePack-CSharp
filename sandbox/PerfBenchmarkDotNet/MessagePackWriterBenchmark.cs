extern alias newmsgpack;
extern alias oldmsgpack;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Nerdbank.Streams;

namespace PerfBenchmarkDotNet
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class MessagePackWriterBenchmark
    {
        private const int RepsOverArray = 300 * 1024;

        private readonly int[] values = new int[newmsgpack::MessagePack.MessagePackCode.MaxFixInt];
        private readonly byte[] byteValues = new byte[newmsgpack::MessagePack.MessagePackCode.MaxFixInt];

        private byte[] bytes;

        [GlobalSetup]
        public void GlobalSetup()
        {
            int bufferSize = 16 + 4 * RepsOverArray * values.Length;
            bytes = new byte[bufferSize];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = i + 1;
            }
            for (int i = 0; i < byteValues.Length; i++)
            {
                byteValues[i] = (byte)(i + 1);
            }
        }

        [Benchmark(OperationsPerInvoke = RepsOverArray * newmsgpack::MessagePack.MessagePackCode.MaxFixInt)]
        [BenchmarkCategory("2.0")]
        public void Write_Byte()
        {
            var writer = new newmsgpack::MessagePack.MessagePackWriter(this.bytes);
            for (int j = 0; j < RepsOverArray; j++)
            {
                for (int i = 0; i < byteValues.Length; i++)
                {
                    writer.Write(byteValues[i]);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = RepsOverArray * newmsgpack::MessagePack.MessagePackCode.MaxFixInt)]
        [BenchmarkCategory("1.x")]
        public void WriteByte()
        {
            int offset = 0;
            for (int j = 0; j < RepsOverArray; j++)
            {
                for (int i = 0; i < byteValues.Length; i++)
                {
                    offset += oldmsgpack::MessagePack.MessagePackBinary.WriteByte(ref bytes, offset, byteValues[i]);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = RepsOverArray * newmsgpack::MessagePack.MessagePackCode.MaxFixInt)]
        [BenchmarkCategory("2.0")]
        public void Write_UInt32()
        {
            var writer = new newmsgpack::MessagePack.MessagePackWriter(this.bytes);
            for (int j = 0; j < RepsOverArray; j++)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    writer.Write((uint)values[i]);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = RepsOverArray * newmsgpack::MessagePack.MessagePackCode.MaxFixInt)]
        [BenchmarkCategory("2.0")]
        public void Write_Int32()
        {
            var writer = new newmsgpack::MessagePack.MessagePackWriter(this.bytes);
            for (int j = 0; j < RepsOverArray; j++)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    writer.Write(values[i]);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = RepsOverArray * newmsgpack::MessagePack.MessagePackCode.MaxFixInt)]
        [BenchmarkCategory("1.x")]
        public void WriteInt32()
        {
            int offset = 0;
            for (int j = 0; j < RepsOverArray; j++)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    offset += oldmsgpack::MessagePack.MessagePackBinary.WriteInt32(ref bytes, offset, values[i]);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = 5000000)]
        [BenchmarkCategory("2.0")]
        public void Write_String()
        {
            for (int j = 0; j < 5; j++)
            {
                var writer = new newmsgpack::MessagePack.MessagePackWriter(this.bytes);
                for (int i = 0; i < 1000000; i++)
                {
                    writer.Write("Hello!");
                }
                writer.Flush();
            }
        }

        [Benchmark(OperationsPerInvoke = 5000000)]
        [BenchmarkCategory("1.x")]
        public void WriteString()
        {
            for (int j = 0; j < 5; j++)
            {
                int offset = 0;
                for (int i = 0; i < 1000000; i++)
                {
                    offset += oldmsgpack::MessagePack.MessagePackBinary.WriteString(ref bytes, offset, "Hello!");
                }

                offset = 0;
            }
        }
    }
}
