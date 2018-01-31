using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class FloatConversionTest
    {
        [Theory]
        [InlineData(-10)]
        [InlineData(-120)]
        [InlineData(10)]
        [InlineData(0.000006f)]
        [InlineData(byte.MaxValue)]
        [InlineData(sbyte.MaxValue)]
        [InlineData(short.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData(ushort.MaxValue)]
        [InlineData(uint.MaxValue)]
        [InlineData(ulong.MaxValue)]
        public void FloatTest<T>(T value)
        {
            var bin = MessagePackSerializer.Serialize(value);
            MessagePackSerializer.Deserialize<float>(bin).Is(Convert.ToSingle(value));
        }

        [Theory]
        [InlineData(-10)]
        [InlineData(-120)]
        [InlineData(10)]
        [InlineData(0.000006)]
        [InlineData(byte.MaxValue)]
        [InlineData(sbyte.MaxValue)]
        [InlineData(short.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData(ushort.MaxValue)]
        [InlineData(uint.MaxValue)]
        [InlineData(ulong.MaxValue)]
        public void DoubleTest<T>(T value)
        {
            var bin = MessagePackSerializer.Serialize(value);
            MessagePackSerializer.Deserialize<double>(bin).Is(Convert.ToDouble(value));
        }
    }
}
