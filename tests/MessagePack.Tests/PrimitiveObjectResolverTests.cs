using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
    public class PrimitiveObjectFormatterTests
    {
        private readonly MessagePackSerializer serializer = new MessagePackSerializer(PrimitiveObjectResolver.Instance);

        [Theory]
        [InlineData((sbyte)5)]
        [InlineData((byte)5)]
        [InlineData((short)5)]
        [InlineData((ushort)5)]
        [InlineData(5)]
        [InlineData(5U)]
        [InlineData(5L)]
        [InlineData(5UL)]
        public void CompressibleIntegersRetainTypeInfo<T>(T value)
        {
            var bin = serializer.Serialize<object>(value);
            var result = Assert.IsType<T>(serializer.Deserialize<object>(bin));
            Assert.Equal(value, result);
        }

        [Fact]
        public void EnumRetainsUnderlyingType()
        {
            var bin = serializer.Serialize<object>(SomeEnum.SomeValue);
            var result = (SomeEnum)serializer.Deserialize<object>(bin);
            Assert.Equal(SomeEnum.SomeValue, result);
        }

        public enum SomeEnum : ushort
        {
            None = 0,
            SomeValue = 1,
        }
    }
}
