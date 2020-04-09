// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Internal;
using Xunit;

namespace MessagePack.Tests
{
    public class ByteArrayComparerTest
    {
#if !UNITY_2018_3_OR_NEWER

        [Fact]
        public void Compare()
        {
            for (int i = 0; i < 200; i++)
            {
                for (int j = 0; j < Math.Min(10, i); j++)
                {
                    var xs = Enumerable.Range(1, i).Select(x => (byte)x).ToArray();
                    var ys = xs.ToArray();

                    xs.AsSpan(j, xs.Length - j).SequenceEqual(ys.AsSpan(j, ys.Length - j)).IsTrue();

                    if (ys.Length != 0)
                    {
                        ys[ys.Length - 1] = 255;
                        xs.AsSpan(j, xs.Length - j).SequenceEqual(ys.AsSpan(j, ys.Length - j)).IsFalse();
                    }
                }
            }
        }
#endif
    }
}
