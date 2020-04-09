// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private readonly Sequence<byte> sequence = new Sequence<byte>();

        private readonly int[] values = new int[newmsgpack::MessagePack.MessagePackCode.MaxFixInt];
        private readonly byte[] byteValues = new byte[newmsgpack::MessagePack.MessagePackCode.MaxFixInt];

        private byte[] bytes;

        [GlobalSetup]
        public void GlobalSetup()
        {
            int bufferSize = 16 + (4 * RepsOverArray * this.values.Length);
            this.bytes = new byte[bufferSize];

            for (int i = 0; i < this.values.Length; i++)
            {
                this.values[i] = i + 1;
            }

            for (int i = 0; i < this.byteValues.Length; i++)
            {
                this.byteValues[i] = (byte)(i + 1);
            }
        }

        [IterationSetup]
        public void IterationSetup() => this.sequence.GetSpan(this.bytes.Length);

        [IterationCleanup]
        public void IterationCleanup() => this.sequence.Reset();

        [Benchmark(OperationsPerInvoke = RepsOverArray * newmsgpack::MessagePack.MessagePackCode.MaxFixInt)]
        [BenchmarkCategory("2.0")]
        public void Write_Byte()
        {
            var writer = new newmsgpack::MessagePack.MessagePackWriter(this.sequence);
            for (int j = 0; j < RepsOverArray; j++)
            {
                for (int i = 0; i < this.byteValues.Length; i++)
                {
                    writer.Write(this.byteValues[i]);
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
                for (int i = 0; i < this.byteValues.Length; i++)
                {
                    offset += oldmsgpack::MessagePack.MessagePackBinary.WriteByte(ref this.bytes, offset, this.byteValues[i]);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = RepsOverArray * newmsgpack::MessagePack.MessagePackCode.MaxFixInt)]
        [BenchmarkCategory("2.0")]
        public void Write_UInt32()
        {
            var writer = new newmsgpack::MessagePack.MessagePackWriter(this.sequence);
            for (int j = 0; j < RepsOverArray; j++)
            {
                for (int i = 0; i < this.values.Length; i++)
                {
                    writer.Write((uint)this.values[i]);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = RepsOverArray * newmsgpack::MessagePack.MessagePackCode.MaxFixInt)]
        [BenchmarkCategory("2.0")]
        public void Write_Int32()
        {
            var writer = new newmsgpack::MessagePack.MessagePackWriter(this.sequence);
            for (int j = 0; j < RepsOverArray; j++)
            {
                for (int i = 0; i < this.values.Length; i++)
                {
                    writer.Write(this.values[i]);
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
                for (int i = 0; i < this.values.Length; i++)
                {
                    offset += oldmsgpack::MessagePack.MessagePackBinary.WriteInt32(ref this.bytes, offset, this.values[i]);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = 5000000)]
        [BenchmarkCategory("2.0")]
        public void Write_String()
        {
            for (int j = 0; j < 5; j++)
            {
                var writer = new newmsgpack::MessagePack.MessagePackWriter(this.sequence);
                for (int i = 0; i < 1000000; i++)
                {
                    writer.Write("Hello!");
                }

                writer.Flush();
                this.sequence.Reset();
                this.sequence.GetSpan(this.bytes.Length);
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
                    offset += oldmsgpack::MessagePack.MessagePackBinary.WriteString(ref this.bytes, offset, "Hello!");
                }

                offset = 0;
            }
        }
    }
}
