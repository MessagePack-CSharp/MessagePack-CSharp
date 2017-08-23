using MessagePack.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class ByteArrayComparerTest
    {
        [Fact]
        public void Compare()
        {
            for (int i = 0; i < 200; i++)
            {
                for (int j = 0; j < Math.Min(10, i); j++)
                {
                    var xs = Enumerable.Range(1, i).Select(x => (byte)x).ToArray();
                    var ys = xs.ToArray();

                    ByteArrayComparer.Equals(xs, j, xs.Length - j, ys, j, ys.Length - j).IsTrue();

                    if (ys.Length != 0)
                    {
                        ys[ys.Length - 1] = 255;
                        ByteArrayComparer.Equals(xs, j, xs.Length - j, ys, j, ys.Length - j).IsFalse();
                    }
                }
            }
        }
    }
}
