// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace MessagePackCompiler.Generator
{
    public class StringKeySorter<T> : IComparer<ValueTuple<byte[], T>>
    {
        public int Compare((byte[], T) x, (byte[], T) y)
        {
            return Compare(x.Item1, y.Item1);
        }

        private static int Compare(byte[] x, byte[] y)
        {
            var c = x.Length.CompareTo(y.Length);
            if (c != 0)
            {
                return c;
            }

            return Compare(x.AsSpan(), y.AsSpan());
        }

        private static int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            var length = x.Length;
            var rest = length - ((length >> 3) << 3);
            ReadOnlySpan<byte> SliceShorten(ReadOnlySpan<byte> span) => span.Slice(0, span.Length - rest);
            var c = CompareShorten(SliceShorten(x), SliceShorten(y));
            if (c != 0)
            {
                return c;
            }

            if (rest == 0)
            {
                return 0;
            }

            ulong xValue = x[length - 1];
            ulong yValue = y[length - 1];

            for (var i = 1; i < rest; i++)
            {
                xValue <<= 8;
                xValue |= x[length - 1 - i];
                yValue <<= 8;
                yValue |= y[length - 1 - i];
            }

            return xValue.CompareTo(yValue);
        }

        private static int CompareShorten(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            while (!x.IsEmpty)
            {
                var xValue = BinaryPrimitives.ReadUInt64LittleEndian(x);
                var yValue = BinaryPrimitives.ReadUInt64LittleEndian(y);
                var c = xValue.CompareTo(yValue);
                if (c != 0)
                {
                    return c;
                }

                x = x.Slice(8);
                y = y.Slice(8);
            }

            return 0;
        }
    }
}
