// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace PerfBenchmarkDotNet
{
    public class SpanBenchmarks
    {
        private const string SomeString = "Hi there";
        private static readonly byte[] ByteArray = new byte[1];

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
            fixed (byte* pBytes = ByteArray)
            {
                pBytes[0] = 7;
            }
        }

        [Benchmark]
        public unsafe void PinArray_Indexer()
        {
            fixed (byte* pBytes = &ByteArray[0])
            {
                pBytes[0] = 7;
            }
        }

        [Benchmark]
        public unsafe void PinSpan_GetReference()
        {
            Span<byte> span = ByteArray;
            fixed (byte* pBytes = &MemoryMarshal.GetReference(span))
            {
                pBytes[0] = 7;
            }
        }

        [Benchmark]
        public void PinSpan()
        {
            PinSpan_Helper(ByteArray);
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
            PinSpan_Indexer_Helper(ByteArray);
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
