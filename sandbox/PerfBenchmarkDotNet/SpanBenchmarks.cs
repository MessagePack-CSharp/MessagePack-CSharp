using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace PerfBenchmarkDotNet
{
    public class SpanBenchmarks
    {
        private const string SomeString = "Hi there";
        private static readonly byte[] byteArray = new byte[1];

        [Benchmark]
        public unsafe void PinString()
        {
            fixed (char* pChars = SomeString)
            {
                char ch = pChars[0];
            }
        }

        [Benchmark]
        public unsafe void GetSpanFromString()
        {
            char ch = SomeString.AsSpan()[0];
        }

        [Benchmark]
        public unsafe void PinArray()
        {
            fixed (byte* pBytes = byteArray)
            {
                pBytes[0] = 7;
            }
        }

        [Benchmark]
        public unsafe void PinArray_Indexer()
        {
            fixed (byte* pBytes = &byteArray[0])
            {
                pBytes[0] = 7;
            }
        }

        [Benchmark]
        public unsafe void PinSpan_GetReference()
        {
            Span<byte> span = byteArray;
            fixed (byte* pBytes = &MemoryMarshal.GetReference(span))
            {
                pBytes[0] = 7;
            }
        }

        [Benchmark]
        public void PinSpan()
        {
            PinSpan_Helper(byteArray);
        }

        private static unsafe void PinSpan_Helper(Span<byte> bytes)
        {
            fixed (byte* pBytes = bytes)
            {
                pBytes[0] = 7;
            }
        }

        [Benchmark]
        public void PinSpan_Indexer()
        {
            PinSpan_Indexer_Helper(byteArray);
        }

        private static unsafe void PinSpan_Indexer_Helper(Span<byte> bytes)
        {
            fixed (byte* pBytes = &bytes[0])
            {
                pBytes[0] = 8;
            }
        }
    }
}
